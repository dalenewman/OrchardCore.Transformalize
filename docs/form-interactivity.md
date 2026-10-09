# Form and task interactivity

For practical before-and-after examples, start with the
[developer guide](client-interactivity-guide.md).

Standard forms, task reviews, and bulk-action reviews now share the Razor,
htmx, and Stimulus approach introduced for [reports](report-interactivity.md).
The server continues to validate parameters, compute dependent choices, render
previews, and choose the next focused field.
On opening, explicit autofocus or an available server-selected field takes
precedence; otherwise focus falls back to the first visible, enabled, editable
field. Hidden values, readonly displays, and button helpers are skipped.

## Request flow

1. A field change checks that field with its parameter rules and browser constraints. If its
   `data-tfl-post-back` value is `true`, htmx posts the form to the existing Form
   validation endpoint even when a browser check fails. The server transforms and
   validates all posted parameters; other field changes stay local.
2. The endpoint returns `_InteractiveForm` only for supported arrangements and
   htmx requests targeting `tfl-form`. History restoration and ordinary requests
   retain the existing Form view. The fragment response sets
   `X-Transformalize-Form: true` and varies by the htmx request headers.
3. htmx replaces the form with fresh Razor markup. Stimulus disconnects the old
   widgets and connects the new ones. If the user has tabbed ahead or continued
   editing, the current field and caret are retained; otherwise the server-selected
   focus is restored. If that field disappears, the server-selected focus is used.
4. Submit, Update, and Run use native POSTs to their existing routes. The form's
   `hx-trigger="tfl:validate"` handles only validation refreshes. Client validation
   provides advisory feedback before native submission and cannot block server
   transformations or validation. Submission preserves the submitter's name/value.
   Task forms point to Review while invalid, and Run once server validation passes.

Refresh requests never invoke the save/run service. The existing authorization,
antiforgery, server validation, and execution paths continue to govern those
operations. Validation requests include hidden parameters and the antiforgery
token; client validation applies to editable controls.

Ordinary fields remain editable during validation, and Tab/Shift+Tab keep their
normal browser behavior. A response based on older edits is discarded; one
follow-up validation sends the latest values together, including fields with
postback disabled. The fresh response still supplies authoritative server
transformations and dependent fields. Continued edits during that request defer
replacement again, rather than losing text or sending a request per keystroke.

Pending validation prevents Submit/Run and announces progress. These actions stay
in the Tab order with `aria-disabled` until validation finishes. Failed refreshes
keep entries visible and offer a retry button. Successful HTML without the
fragment marker retains the current form and shows a refresh failure. A redirect
to another URL, such as login, causes full navigation. The partial validation
endpoint is never used as a navigation destination. Dirty forms
warn before leaving; intentional submission suppresses that warning. Modal
cancel/confirmation messages use the parent's same-origin protocol.

## Code map

| File | Responsibility |
| --- | --- |
| `ViewModels/FormInteractivityViewModel.cs` | Compatibility decision and validation/submit/run URLs |
| `Ext/FormInteractivityExtensions.cs` | Shared controller setup and fragment response selection |
| `Views/Shared/_InteractiveForm.cshtml` | Refreshable boundary, controller actions, progress and retry feedback |
| `Views/Shared/Form.cshtml` | Form shell, antiforgery, hidden parameters and focus computation |
| `Views/Shared/_FormFields.cshtml` | Shared parameter rendering |
| `Views/Shared/_FormActions.cshtml` | Submit/Update/Run and Cancel controls |
| `Views/Shared/_FormDate.cshtml` | Editable date field and calendar button; separate legacy initializer |
| `Ext/ParameterExtensions.cs` | Encoded client validation rules, with Parsley attributes for legacy forms |
| `wwwroot/Scripts/date-widgets.js` | Shared form/report calendar lifecycle and canonical date selection |
| `Views/Shared/_FormMap.cshtml` | Map and coordinate markup, including a separate legacy initializer |
| `Views/Shared/_FormLocation.cshtml` | Device-location control and feedback |
| `Views/Shared/_FormUpload.cshtml` | Upload/scan markup and canonical stored values; separate legacy partials |
| `Views/Shared/_FormPlaces.cshtml` | Address search and mapped component configuration; separate legacy partial |
| `ViewModels/FormPlacesViewModel.cs` | Existing address component aliases and target field names |
| `wwwroot/Scripts/form-places-controller.js` | Address selection, grouped changes, busy state, and failure feedback |
| `wwwroot/Scripts/form-places-widgets.js` | Shared legacy Google SDK loader and per-input autocomplete adapter |
| `wwwroot/Scripts/form-upload-controller.js` | Progress, Retry/Clear, scan text and upload state events |
| `wwwroot/Scripts/form-upload-widgets.js` | Scoped Blueimp image-processing/upload adapter with abort and teardown |
| `Views/Shared/_FormResources.cshtml` | Shared resources, with modern and legacy branches |
| `wwwroot/Scripts/form-controller.js` | Refresh, validation, focus, submission and dirty-state behavior |
| `wwwroot/Scripts/form-widgets.js` | Scoped browser validation, inline messages, and calendar ownership |
| `wwwroot/Scripts/form-map-controller.js` | Map/marker lifecycle and paired coordinate changes |
| `wwwroot/Scripts/form-location-controller.js` | Device-location requests, status, and stale callback protection |
| `wwwroot/Scripts/form-interactivity.js` | Stimulus registration |
| `wwwroot/Styles/form.css` | Shared appearance and busy state |

## Compatibility and remaining work

Arrangements with global JavaScript or raw parameter HTML containing scripts,
inline handlers, or JavaScript URLs also use the legacy path. Passive raw HTML,
such as the Color task's preview, can refresh with the form. This compatibility
check is not an HTML sanitizer.

Forms with Google Places, uploads/scanning, maps, and device location can now use fragments,
including the local Got Up arrangements. They no longer load `form.js`,
`file.handler.js`, BlockUI, jquery-are-you-sure, Parsley, Moment, or pickadate.
jQuery remains behind the Blueimp upload adapter; jQuery UI and the image/upload
bundle load only when uploads/scanning are present. Report selection menus now
use scoped Stimulus controls and native selects. Replacing the Blueimp image
pipeline and adding a lifecycle contract for custom global arrangement scripts
are separate future phases; the current compatibility paths preserve them.

## Validation and calendars

`ToValidation` emits an HTML-encoded JSON attribute for the client rules previously
sent to Parsley: required, exact length, numeric/integer/digits/alphanumeric types,
email, URL, pattern, min/max, and date/datetime. Legacy forms retain encoded Parsley
attributes. Client validation skips ordinary hidden parameters, file choosers,
unnamed helpers, excluded display controls, and disabled controls. Canonical
hidden upload IDs remain eligible for required validation. Server-only rules,
such as map membership, remain on the server.

Errors appear beside each field with `aria-invalid`, an alert, and a linked
description that preserves existing hints. Validation runs on change, blur, and
submission; correcting a previously invalid value updates feedback while typing
without requesting a server refresh. Submission checks all eligible controls and
focuses the first locally invalid control, but its findings do not block a
configured postback or native save/run request. Server transformations may fix
or otherwise change any submitted value, including fields with postback disabled
when a later request includes them. All server authorization and validation still
run on native save/run requests.

Browser feedback has its own message node and never removes Razor's server
messages or marks a server-invalid group valid. Those messages and invalid states
last until the next server response replaces the fragment or explicitly supplies
new feedback for that field. A refresh rejected by a parameter's
`invalid-characters` setting keeps the typed values and reports the field and
restricted character inline. The setting remains enforced; correcting the text
and retrying refreshes the form. Every replacement
uses the returned values, visibility, maps, feedback, and action URL. Uploads
also render the transformed server file ID rather than the raw posted `_Old` ID.
Arrangement visibility expressions still run together on the server with the
complete parameter set; individual widget lifecycles do not isolate dependencies.

Email and URL syntax checks use browser input validation; scheme-less URLs are
still accepted by the client without altering the posted value. Date validation
checks calendar ranges for ISO and US numeric dates and uses the browser date
parser for other date/time text. Browser parsing may differ from server culture;
the server is authoritative. JavaScript-compatible patterns keep the previous
whole-value behavior; unsupported .NET-only or invalid patterns defer to server
validation. This client check does not replace server validation.

Flatpickr 4.6.13 is vendored with its MIT license. Both form and report calendars
use its [documented lifecycle](https://flatpickr.js.org/instance-methods-properties-elements/).
The named Razor field remains editable text; an unnamed hidden helper owns the
picker. Opening or cancelling the calendar never clears or normalizes typed text.
A selection writes `yyyy-MM-dd` and emits one change event. Keyboard selection
works, busy requests close/disable the picker, and replacement destroys its
popup, helper, listeners, and ARIA links. Modern reports no longer load jQuery UI
for date filters; legacy reports and forms keep their previous calendars.

## Google Places lifecycle

The address control preserves the site's existing Google API key, legacy
`Autocomplete`, US address restriction, and requested address/geometry fields.
It does not require enabling Places API (New). `_FormPlaces` renders encoded
configuration; the shared loader loads the SDK once per document. Each control
owns its autocomplete instance, listeners, and prediction popup, including when
several address controls share a form. Replacement removes the old instances
and popups; late SDK and selection callbacks cannot change removed forms.

Selection fills all mapped fields before one grouped validation event. A
configured postback therefore sends the complete address in one request.
Missing components clear old values, such as a previous postal-code suffix.
Enter selects a suggestion without submitting or advancing the form. During a
validation request the address search is readonly, preserving its posted value.
Loading, authentication, and incomplete-result failures show feedback and leave
manual entry available. Retry addresses transient loading failures; an invalid
site key still needs correction in site settings.

Existing map aliases and first-match precedence are preserved. Historically,
`county` maps to `administrative_area_level_1` and `state` to
`administrative_area_level_2`. New arrangements should use the explicit Google
component names to avoid this ambiguity. Target lookup stays within the current
form; disabled controls are not overwritten.

## Upload and scan lifecycle

`_FormUpload` renders a chooser, image thumbnail, progress, Retry/Clear, and status feedback.
Selecting an image shows a local object-URL preview immediately, before image
processing or the upload response. It stays visible on success without needing
a form postback. After replacement, Razor serves the thumbnail through the
existing authenticated file endpoint using the transformed stored content ID.
Non-image files have no thumbnail. Preview URLs are released on retry, Clear,
preview failure, and controller teardown.
If the arrangement already renders a raw image with the same stored file URL,
the uploader reuses it and preserves its styling instead of showing a second
thumbnail. New selections and Clear update that same preview.
Upload and Scan use native buttons with a visible keyboard focus indicator;
Enter or Space opens the file picker. The picker input and readonly filename
are excluded from the Tab order. Upload and Clear remain keyboard reachable
during validation while their actions wait for the response. Stable control IDs
and field focus metadata keep focus on the upload field across replacement,
without adding file contents or button metadata to the posted form values.
File attachments post their stored content ID as `{Name}_Old`; filenames are
unnamed display controls. Scan fields post decoded or manually entered text as
`{Name}`. Choosers are unnamed, so selected images never leak into validation or
native save/run requests. Upload requests use the original parameter name and
include the form's antiforgery token. A posted empty `_Old` value means removal
and cannot fall back to the previous attachment. Clear asks for confirmation;
cancellation leaves the ID, preview, and upload unchanged. Confirming empties
the canonical value and preview without deleting the stored content item. When
Clear held keyboard focus, focus returns to Upload (or the scan text field).

The scoped Blueimp adapter preserves the existing 1920×1080 maximum size,
orientation handling, JPEG metadata, and non-image behavior. It starts blocking
before image processing, and tears down the widget and aborts unfinished work on
disconnect. Retry processes the original selected image, rather than resizing a
processed blob again. Server upload responses use JSON serialization so quoted
and Unicode filenames remain valid JSON.

The upload adapter's resource dependencies pin the core and its process/image
extensions to 10.31.0 and declare their load order. Keep these versions aligned
when upgrading the pipeline. Orchard also registers `jquery-fileupload` at a
newer version; an unversioned dependency can load that core a second time after
the extensions, removing their processing methods.

Pending or failed selections block native Submit/Run and fragment replacement.
Field refreshes wait until all selections finish or are cleared, then coalesce
into one request. Choosers are disabled during validation; a late response is
also prevented from replacing a newly selected upload. Finishing an upload
never submits the form automatically. Scan text is readonly while its selected
image is unresolved, preventing a late result from overwriting manual entry.

The scan control preserves the existing `File.Scan` request/response contract
(`id` plus `message`), with failure feedback and manual entry after Clear. This
repository has no Scan action or barcode-decoding service. Decoding remains a
separate future phase; this migration does not add it.

## Map and location lifecycle

The map controller receives its field names and token as Razor-encoded data
attributes, without page globals. Each instance owns its map, marker, and field
listeners; disconnecting removes all three. Clicking the map or dragging the
marker sets both coordinates before dispatching one change event, so a refresh
cannot send half of a coordinate pair. Typed coordinates keep the normal field
validation/postback behavior. Missing tokens and map errors display feedback
while leaving the coordinate inputs usable. See [map-picker.md](map-picker.md).

Device location uses the form's configured accuracy, maximum age, and timeout.
It acquires a position on the initial GET, matching the previous behavior, and
can be requested again with the location button. POST fragments retain the
submitted coordinates instead of automatically asking for another position.
Callbacks from removed forms are ignored. Errors retain existing coordinates
and display an explanation beside the retryable location button. Capturing a
position marks the form dirty; it does not submit or refresh the form.

Owned form scripts and CSS use Orchard's content-version query strings to avoid
reusing stale browser assets after a build/deployment.

## Checks

```sh
dotnet build src/Site/Site.csproj
dotnet test test/OrchardCore.Transformalize.Tests/OrchardCore.Transformalize.Tests.csproj
node --test test/client/*.test.cjs
```

Server tests cover eligibility, submit/run URL choice, fragment routing, failed
validation, and the validation-only service boundary. Client tests cover field
postbacks, pending/duplicate submission protection, failure feedback, focus,
dirty-state warnings, cancellation, and unexpected HTML responses.
Regression checks also cover edits during an in-flight validation, coalesced
follow-up requests, failure/retry, Tab focus and caret retention, and a dependent
field disappearing. A held-response Chrome fixture verifies continued typing,
rejection of stale HTML, authoritative server transformations, Tab/Shift+Tab,
and keyboard access to Submit while premature execution remains blocked.

Browser verification includes the real Crime report's Color bulk review:
changing Color refreshes the preview and focuses Run while the batch remains in
Review. An isolated fixture checks dependent options, required/date validation,
calendar selection, and native POST contents without executing report tasks or
writing application data.

Map/location checks use the vendored Mapbox renderer with an offline style and
simulated device coordinates. They cover map clicks, dragging after repeated
swaps, one request per coordinate pair, map/marker cleanup, denied location,
late callbacks after replacement, and native submission of retained values.
This does not verify live Mapbox tiles or a device's GPS accuracy.

Upload checks cover processing-before-upload, concurrent refresh/save protection,
coalesced requests, abort/stale callbacks, required stored IDs, Retry/Clear, scan
failure/manual entry, and JSON filenames. Browser checks use a disposable fixture
with the real adapter and vendored image pipeline, including a held request,
failed request, retry, rotated large JPEG, and native POST contents. The real
Got Up edit form retains its existing attachment through dependent-field swaps.
Chrome extension file selection was unavailable because file URL access was
disabled; the fixture supplies its own disposable sample through Blueimp's add
API. No report data was saved during these checks.

The Add Got Up image-preparation regression was reproduced with the real
Blueimp scripts: loading Orchard's core after the image extensions produced
"The image could not be prepared. Retry or clear it." on both selection and
Retry. A resource-resolver regression test now asserts one core and the correct
extension order even when a newer core is registered. The corrected actual Add
form loads one core in both whole-page and modal modes. The browser fixture
verifies resizing a 2400×1600 PNG to 1620×1080, successful processing/upload,
retained stored ID after validation, Submit becoming enabled, Clear after a
preparation failure, and Retry after a failed upload. Actual file selection in
Chrome remains unavailable with the extension's current file-access setting.

Places checks cover shared SDK loading, failure/retry, popup ownership, teardown,
keyboard selection, grouped postbacks, component clearing, and form isolation.
Browser checks use a disposable two-address fixture with the actual adapter and
simulated Google responses. The live SDK loaded, but the configured key returned
`InvalidKeyMapError`; real prediction and selection results remain unverified.
No Google project settings or API key were changed.
After the local SQL Server containers were started, the Got Up report and both
edit/task-review links loaded successfully again, including the map and existing
attachment. No report records were saved.

Validation/calendar checks cover rule encoding, existing rule mappings, required
and optional values, numeric steps, ranges, patterns, invalid/leap dates, grouped
choices, hidden upload IDs, accessible messages, and teardown. A disposable
browser fixture checks required/date feedback, editable invalid date preservation,
mouse and keyboard calendar selection, one postback per selection, failure/retry,
repeated replacement with one active calendar, and native POST contents. It loads
no jQuery, Parsley, Moment, or pickadate. Google prediction checks still have the
key limitation described above.

## Completion and display-mode verification

Server-feedback regression checks cover the actual Add Got Up form in whole-page
and modal modes. Communication with Scott displays the arrangement's server-only
message after postback, blur, and another field's postback. Choosing Jeremy clears
it on the next response; choosing Other hides Teammate and displays the Other
Reason message. Invalid native submissions in both modes return the same server
feedback and a usable styled form without saving a record.

A disposable browser fixture confirms that a postback updates even postback-disabled
fields with server-transformed values (` alice ` → `ALICE`, quantity `6` → `5`),
and browser-invalid date text still reaches the validation endpoint. An unmarked
partial response leaves the current URL, entries, and styles in place with retry
feedback. The Bootswatch navbar override also no longer contains a resource tag
helper inside a CSS comment; that helper was executing a second resource flush
inside the style element and could expose CSS/comment text as page content.

The scoped migration for standard reports, forms, and reviews is implemented,
including searchable report filters and the requested developer guide. The
Blueimp image pipeline remains behind its scoped adapter. Custom global
arrangement scripts retain their deliberate compatibility path; barcode decoding,
replacing that image pipeline, and a lifecycle contract for global custom scripts
remain separate future work. Google Places preserves the existing legacy API.

Explicit browser checks on the current build covered these display modes:

| Workflow | Whole page | Modal |
| --- | --- | --- |
| Got Up edit loads with a map and existing attachment | Verified | Verified |
| Changing Reason to Other renders the dependent field and server validation message | Verified | Verified |
| Refresh retains both coordinates and the stored attachment | Verified | Verified |
| Validation and native submission URLs preserve the display mode | Verified | Verified |
| Task review loads with its native Run action | Verified | Verified |
| Cancel closes the parent dialog and resets the iframe | Not applicable | Verified on unedited edit and task-review forms |

These Got Up checks did not save its records or run its stored procedure.
Successful database form insert/update was subsequently checked through the
actual local ASP.NET endpoints using a dedicated form/report and temporary SQLite
database. Both whole-page and modal inserts and updates persisted transformed
values. Whole-page saves redirected to ReturnUrl; modal saves/updates closed the
dialog, reset the iframe to `about:blank`, and refreshed the parent report.
Database inspection confirmed two records after two inserts and two updates,
with no duplicate insert.

The local test content is retained as two unpublished drafts,
`Interactivity Check (local test)` and `Interactivity Check Report (local test)`.
Their database is `/private/tmp/tfl-interactivity-save.db`; they are not linked
in the site menu. No Got Up records were changed for these successful-save tests.
This verifies the real form writer and save/close pipeline, rather than a mocked
save endpoint, but does not execute every arrangement-specific rule, provider,
or task/stored procedure. Positive task execution remains unverified. Dirty-cancel behavior is covered by client tests; accepting its
browser confirmation could not be completed through the browser-control tool.
This matrix covers the listed arrangements, not every custom arrangement or
every browser. Bulk review has prior preview/refresh coverage but is not included
in this two-mode verification matrix.

Modal forms handle `tfl-form:focus` after their iframe and opening transition
are ready, restoring the chosen field only if focus has left the form. The
location helper uses `tabindex="-1"` so Tab navigation follows the form fields.

Tab and Shift+Tab remain native during validation. Movement to an earlier field,
including returning to the field where a request began, retains the current
field and caret across replacement instead of following an older server focus
hint. Regression tests cover both forward and backward navigation.
