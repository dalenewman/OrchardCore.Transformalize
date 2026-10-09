# Working with Razor, htmx, and Stimulus

This guide explains the client interactivity changes in `OrchardCore.Transformalize`.
It uses examples from this migration to show where code now belongs and how to
extend it. The snippets are abbreviated from the implementation; surrounding
markup, validation attributes, and error handling remain in the linked files.

## The three responsibilities

**Razor renders application state.** The arrangement and server determine values,
visibility, map choices, validation messages, results, and the available action.
Razor partials turn that state into HTML.

**htmx requests and replaces HTML.** A report search requests a GET fragment. A
form's configured field postback requests a POST validation fragment. The response
replaces the entire relevant boundary, including dependent fields and feedback.

**Stimulus handles browser behavior.** Controllers own focus, dropdowns, keyboard
input, dirty state, modal messages, and widget lifetimes. They find elements
through targets and receive configuration through values. They connect when their
markup appears and disconnect when it leaves the document.

You still write C#, arrangements, Razor, and ordinary JavaScript. There is no new
npm build, SPA router, or client model duplicating the server's process.

| Previously | Now | Example |
| --- | --- | --- |
| Page globals such as `settings` | Razor attributes and view models | `FormInteractivityViewModel`, `_FormMap` |
| `$(document).ready(...)` | A scoped controller's `connect()` | `report-filter-controller.js` |
| Global selectors | `this.element`, targets, or the owning form | `form-map-controller.js` |
| `$.ajax` and `.html(html)` | `hx-get`/`hx-post` and a Razor fragment | `_Report`, `Form` |
| Rebind after every AJAX response | Stimulus lifecycle on replacement | `connect()` / `disconnect()` |
| Inline `onclick` functions | Named `data-action` handlers | `click->modal#open` |
| Parsley controls whether posting is allowed | Advisory browser feedback; server validation on POST | `form-widgets.js` |
| Page-wide upload plugin setup | A scoped adapter behind a controller | `form-upload-widgets.js` |

## 1. A field that refreshes dependent fields

The old [form.js](../src/OrchardCore.Transformalize/wwwroot/Scripts/form.js)
bound change handlers repeatedly and let Parsley decide whether to call the server:

```js
$("input[data-tfl-post-back='true']").change(function () {
   if ($(this).parsley().isValid()) {
      post();
   } else {
      $(this).parsley().validate();
   }
});
```

Now Razor renders the field's existing postback setting. The shared form boundary
routes changes to [form-controller.js](../src/OrchardCore.Transformalize/wwwroot/Scripts/form-controller.js):

```html
<div id="tfl-form" data-controller="tfl-form"
     data-action="change->tfl-form#changed">
   <!-- Shared Form and _FormFields partials render the controls here. -->
</div>
```

```js
changed(event) {
   const field = event.target;
   if (field.form !== this.formTarget || !field.matches('[data-tfl-post-back]')) return;
   this.dirtyValue = true;
   this.widgets.validateField(field);
   if (field.dataset.tflPostBack === 'true') this.validate();
}
```

A standard arrangement field needs no new change handler:

```xml
<add name="Reason" prompt="true" map="Reasons" v="required()" />
<add name="OtherReason" prompt="true" visible="Reason==='Other'" />
```

The parameter renderer supplies the postback and validation attributes. Changing
Reason posts the form; the server decides whether OtherReason appears. To disable
that field's automatic refresh, configure `post-back="false"`. Its value is still
included in the next field's postback and in the native Submit/Run request.

**Every postback can change any field.** Values, visibility, choices, messages,
and the action URL all come from the returned process. For example, selecting
Communication and Scott in Got Up renders the server-only message about Scott
being remote. Blur or a successful browser check must not erase it. Choosing a
valid teammate clears it only when the next server response says so.

Browser checks also cannot block a configured postback: the server may transform
an apparently invalid value into a valid one. This was verified with trimming and
uppercasing text before a real database save.

## 2. Receiving refreshed HTML without rebinding everything

The old refresh path serialized the form and called a function that inserted HTML,
initialized widgets, and attached handlers again:

```js
$.ajax({
   url: settings.ajaxUrl,
   type: 'POST',
   data: $('#id_form').serialize(),
   success: function (html) { bind(html); }
});
// Inside bind:
$('#id_content').html(html);
```

Now [Form.cshtml](../src/OrchardCore.Transformalize/Views/Shared/Form.cshtml)
declares the validation request separately from its save action:

```razor
<form method="post" action="@interactivity.ActionUrl"
      hx-post="@interactivity.ValidationUrl"
      hx-trigger="tfl:validate" hx-target="#tfl-form"
      hx-swap="outerHTML" hx-sync="this:replace"
      hx-encoding="multipart/form-data" novalidate>
   @await Html.PartialAsync("_FormFields", Model)
   @await Html.PartialAsync("_FormActions", Model)
</form>
```

The controller requests validation with `htmx.trigger(this.formTarget,
 'tfl:validate')`. The existing Form endpoint validates/transforms and returns
`_InteractiveForm`, with `X-Transformalize-Form: true`. Stimulus disposes the old
widgets and connects the new ones automatically.

The separate `action` is important: clicking Submit/Update/Run performs the existing
native POST. A validation refresh never writes a form record or executes a task.
The native action validates again before saving/running, preserves antiforgery and
the submitter's name/value, and returns server errors when invalid.

Failures retain the current form and show Try again. Successful HTML without the
fragment marker is also treated as a failure when it comes from the same validation
URL; it must not navigate the browser to a bare partial. Redirects to another URL,
such as login, use full navigation.

## 3. Report dropdowns without Bootstrap-select

The old [report.js](../src/OrchardCore.Transformalize/wwwroot/Scripts/report.js)
initialized Bootstrap-select, listened to plugin events, and inserted a Search
button into the plugin's generated menu:

```js
$('#id_report select').selectpicker({ liveSearch: true, showTick: true });
$('#id_report select').on('changed.bs.select', function () {
   lastFilter = this.name;
   controls.setPage(1);
   if (!this.multiple || $(this).val().length === 0) controls.submit(1);
});
// Multiple-choice Search was appended to $select.data('selectpicker').$menu.
```

Now [Parameter.cshtml](../src/OrchardCore.Transformalize/Views/Shared/Parameter.cshtml)
selects `_ReportFilter` for supported reports:

```razor
@await Html.PartialAsync("_ReportFilter", Model)
```

[_ReportFilter.cshtml](../src/OrchardCore.Transformalize/Views/Shared/_ReportFilter.cshtml)
keeps the native named select as the submitted value, renders the popup shell,
and names its controller actions:

```razor
<div data-controller="report-filter" data-action="change->report-filter#changed">
   <select name="@Model.Parameter.Name" multiple="@Model.Parameter.Multiple"
           data-report-filter-target="select">
      @await Html.PartialAsync("_ReportFilterOptions", Model)
   </select>
   <button type="button" data-report-filter-target="trigger"
           data-action="report-filter#toggle" hidden>...</button>
   <!-- Search field, options list, On/Off, and Search button. -->
</div>
```

[report-filter-controller.js](../src/OrchardCore.Transformalize/wwwroot/Scripts/report-filter-controller.js)
builds safe text options from the native select, mirrors selection, and dispatches
a native `change`. It hides the select only after connecting successfully. The
unnamed search field and popup buttons never contribute extra query parameters.

Single choices refresh immediately. Multiple choices stay local until popup
Search or report Search; clearing the last choice refreshes immediately. On/Off
applies to enabled choices matching the search. Arrow keys move through matching
choices, Enter selects, Escape closes, and focus returns to the replacement trigger.
Selection summaries retain labels/counts and use a count above two selections.

The owning report listens to `report-filter:search->report#filterSearch`. Its GET
uses repeated values such as `Category=Alpha&Category=Beta`. Razor returns the
filters and results together, so options and counts are recomputed from the server.
Modern reports load neither jQuery nor Bootstrap-select.

## 4. Opening and closing a modal

Existing arrangements emitted links like this:

```html
<a data-url="/t/form/got-up-edit?modal=1&GotUpId=1"
   onclick="loadModal(this)">Edit</a>
```

New links should have a real navigation destination and a named action:

```html
<a href="/t/form/got-up-edit?modal=1&amp;GotUpId=1"
   data-action="click->modal#open" title="Edit Got Up">Edit</a>
```

This action belongs inside the report's `modal` controller boundary.
[Controls.cshtml](../src/OrchardCore.Transformalize/Views/Shared/Controls.cshtml)
already renders this pattern for arrangement actions with `type="open"` and
`modal="true"`. The modal controller also adapts the exact old `loadModal(this)`
contract each time the report connects, so existing Got Up row links still work.

The iframe receives a normal full form page with `modal=1`. It uses the same field
partials, validation endpoint, and native save action as whole-page mode. After a
successful save the server redirects to `modal=1&close=1`; the form controller sends:

```js
window.parent.postMessage(
   { action: 'closeModal', reason: 'confirmed' }, window.location.origin
);
```

The parent accepts messages only from its own iframe and origin. Confirmation
closes the modal, resets the iframe to `about:blank`, and refreshes the report.
Cancel closes without refreshing and asks before losing dirty entries.

## 5. Calendars belong to the field lifecycle

Previously each date partial emitted an initializer:

```js
$(document).ready(function () {
   var picker = $('#id_@parameter.Name').pickadate({
      editable: true, format: 'yyyy-mm-dd'
   }).pickadate('picker');
});
```

Now [_FormDate.cshtml](../src/OrchardCore.Transformalize/Views/Shared/_FormDate.cshtml)
renders an editable named field and a calendar button:

```html
<button type="button" data-form-date-toggle aria-label="Choose date">...</button>
<input type="text" name="Date" data-form-date data-tfl-post-back="true">
```

The owning widget collection connects the shared
[date-widgets.js](../src/OrchardCore.Transformalize/wwwroot/Scripts/date-widgets.js)
adapter and calls `disconnect()` on replacement. Flatpickr owns an unnamed helper;
the Razor input owns the submitted value. Opening or cancelling preserves typed
text. Selecting a date writes `yyyy-MM-dd` and emits one change. Both reports and
forms use this adapter; its popup is destroyed with the old fragment.

## 6. Maps and addresses update groups together

The old map initializer placed configuration in global variables and triggered
one change per coordinate:

```js
if (latEl) { latEl.value = lngLat.lat.toFixed(7); $(latEl).trigger('change'); }
if (lngEl) { lngEl.value = lngLat.lng.toFixed(7); $(lngEl).trigger('change'); }
```

Now [_FormMap.cshtml](../src/OrchardCore.Transformalize/Views/Shared/_FormMap.cshtml)
passes field names through encoded values:

```razor
<div data-controller="tfl-map"
     data-tfl-map-latitude-value="@parameter.Name"
     data-tfl-map-longitude-value="@Model.LongitudeName"
     data-tfl-map-token-value="@settings.MapBoxToken">
   <div data-tfl-map-target="canvas"></div>
</div>
```

The map controller sets both fields before notifying the form:

```js
this.latitude.value = position.lat.toFixed(7);
this.longitude.value = longitude.toFixed(7);
this.dispatch('changed', { detail: { fields: [this.latitude, this.longitude] } });
```

`groupChanged` requests one configured validation postback after the whole group is
ready. Google Places uses the same idea for an entire address. Missing address
components clear earlier values; a removed form cannot be changed by a late callback.
The existing Google legacy API setup is preserved. Widgets show failure feedback
and retain manual entry. See [map-picker.md](map-picker.md) and
[form-interactivity.md](form-interactivity.md) for configuration details.

## 7. Uploads have a submitted value and a pending operation

Previously file markup and handlers depended on page-wide upload initialization
and the global form bind cycle. Now `_FormUpload` renders a controller boundary
with an unnamed chooser and a canonical hidden ID:

```html
<div data-controller="tfl-upload" data-tfl-upload-name-value="Evidence">
   <input type="file" data-tfl-upload-target="input">
   <input type="hidden" name="Evidence_Old" data-tfl-upload-target="value"
          data-tfl-post-back="true">
   <button type="button" data-action="tfl-upload#retry">Retry</button>
   <button type="button" data-action="tfl-upload#clear">Clear</button>
</div>
```

Processing/uploading reports `tfl-upload:state` to the owning form. A pending or
failed selection blocks refresh and native submission until it finishes, succeeds
on Retry, or is cleared. Deferred field refreshes coalesce into one request.
Finishing an upload never saves the form automatically.

Only the stored ID is posted during validation/save. The chooser and filename
are unnamed. The server transfers `_Old` before transformations, and Razor renders
the resulting server ID after every response. Clear posts an empty ID; it does not
delete stored content. The existing form writer currently keeps the old attachment
when an update supplies an empty ID, so Clear is not a database attachment-delete
operation.

Blueimp's jQuery image pipeline remains inside
[form-upload-widgets.js](../src/OrchardCore.Transformalize/wwwroot/Scripts/form-upload-widgets.js).
This preserves resize/orientation/metadata behavior while the controller manages
state and teardown. Replacing that pipeline is a future phase. Core/process/image
resources are pinned to the same version and load in order: loading another core
after the extensions was the cause of the image-preparation regression.
Barcode decoding is also deferred; the scan request contract and failure feedback
remain in place.

## 8. Adding behavior without another global initializer

Start with the existing arrangement renderer. Standard parameters already get
postback, validation, focus, and native submission. Extract repeated Razor markup
into a typed partial when it represents a reusable control or a clear responsibility.
The shared `_FormFields` and `_FormActions` serve forms, task reviews, and bulk reviews.

For a genuinely new browser behavior:

1. Render its markup, targets, actions, and encoded values in a partial. Keep named
   inputs for submitted values and unnamed helpers for presentation.
2. Add a controller that queries only its own element or owning form. Use `connect()`
   for setup and `disconnect()` for listeners, popups, plugin instances, and requests.
3. Use one grouped event when several fields change together. Let the form controller
   decide whether a configured postback is needed.
4. Register it in `form-interactivity.js` or `report-interactivity.js` and declare
   resource dependencies in `ResourceManifest.cs`. Owned assets use content-version
   query strings so a deployment does not reuse stale JavaScript/CSS.
5. Add focused checks for the new contract, then exercise it after several fragment
   replacements in both display modes.

A typical lifecycle follows the map controller's pattern:

```js
connect() {
   this.form = this.element.closest('form');
   this.onFieldChange = event => { /* handle this control's fields */ };
   this.form.addEventListener('change', this.onFieldChange);
   // Construct the widget using this.canvasTarget and this.*Value configuration.
}
disconnect() {
   this.form.removeEventListener('change', this.onFieldChange);
   this.widget?.remove(); // Use the actual library's disposal API.
}
```

Do not keep references to controls from an old fragment. Guard asynchronous
callbacks after disconnect and abort owned requests when possible. Avoid emitting
scripts inside refreshable markup or calling page-wide setup functions after swaps.

## Finding and debugging the code

All paths below are under `src/OrchardCore.Transformalize`:

| Concern | Start here |
| --- | --- |
| Report refresh and browser state | `Views/Report/_Report.cshtml`, `wwwroot/Scripts/report-controller.js` |
| Report option labels/counts and selection | `Views/Shared/_ReportFilterOptions.cshtml`, `_ReportFilter.cshtml`, `wwwroot/Scripts/report-filter-controller.js` |
| Form field rendering | `Views/Shared/_FormFields.cshtml` and `_Form*` partials |
| Validation versus native action | `ViewModels/FormInteractivityViewModel.cs`, `Controllers/FormController.cs`, `TaskController.cs` |
| Fragment marker/selection | `Ext/FormInteractivityExtensions.cs`, `Controllers/ReportController.cs` |
| Field messages and calendars | `wwwroot/Scripts/form-widgets.js`, `date-widgets.js` |
| Modal close/refresh protocol | `wwwroot/Scripts/report-modal-controller.js`, `form-controller.js` |
| Resource order and legacy branching | `ResourceManifest.cs`, `Views/Shared/_FormResources.cshtml`, `Views/Report/Index.cshtml` |

Use the browser Network panel to distinguish validation POSTs from native saves.
A successful fragment should have its `X-Transformalize-Form` or
`X-Transformalize-Report` marker and the matching replacement boundary. Inspect
returned HTML to understand a changed value, message, or choice. Inspect the native
named inputs to understand what will be submitted. If a widget stops working after
a swap, check lifecycle cleanup and resource order before adding another initializer.

Reports in editor mode or with global arrangement JavaScript retain the legacy
path. Forms with global scripts or active raw HTML also retain it. Charts and other
report display modes keep their existing client behavior. Passive raw previews can
use modern forms. This compatibility decision does not sanitize arbitrary HTML.

Run the established checks:

```sh
dotnet build src/Site/Site.csproj
dotnet test test/OrchardCore.Transformalize.Tests/OrchardCore.Transformalize.Tests.csproj
node --test test/client/*.test.cjs
```

The Node tests exercise controller contracts without npm dependencies. .NET tests
cover routing, validation, authorization results, and resource resolution. Browser
checks verify real HTML replacement, keyboard operation, and widget lifecycle.
See [report-interactivity.md](report-interactivity.md) and
[form-interactivity.md](form-interactivity.md) for verification details and the
remaining external-service limitations.
