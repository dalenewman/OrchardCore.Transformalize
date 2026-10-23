# Report interactivity

All report displays share the same shell, controls, and Stimulus controllers,
including `edit=1` and reports containing global arrangement JavaScript. Table
reports use htmx HTML replacement; maps, charts, and calendars use full GET
navigation through the same query and control code.
Standard forms and task reviews now use the same approach; see
[form interactivity](form-interactivity.md). Forms and report filters have scoped
widget lifecycles. Maps, charts, and calendars retain only their display-specific
rendering code. For before-and-after examples, see the
[developer guide](client-interactivity-guide.md).

Report maps include a Map style control with Streets (the default), Satellite +
streets, Satellite, Outdoors, Light, and Dark. Switching styles keeps the camera
and restores the current report points without fetching the report again. The
choice lasts for the current page; form location maps use their existing style.

## Try it

Run the Site project, sign in, and open `/t/report/northwind?size=25` (if the sample
recipe is installed), or another table report.

1. Enter a text filter and press Enter. The report refreshes in place, returns to
   its first page, updates facet counts, and restores focus to the field.
2. Select a single facet. It refreshes immediately. Multiple facets wait for
   their Search button or the report Search button; clearing the selection refreshes immediately.
3. Try pagination, page size, sorting, and Clear. Filters remain in the URL, so
   links can be bookmarked and opened directly.
4. Use Back, Forward, and Reload. History navigation deliberately loads a full
   page to initialize widgets cleanly; report updates replace only the report.
5. Select individual rows, then Shift-click to select a range. The action badges
   count selected rows. The header checkbox explicitly selects the entire filtered
   result, including other pages; unchecking a row returns to individual selection.
6. Open a bulk action for review and cancel it. Modal actions retain the report's
   selection on cancellation. Closing a confirmed result refreshes the report.
7. Use Formats → MD to copy the displayed table, including links and boolean
   badges. Selection columns are omitted. Clipboard failures are announced.
8. Open Action → Edit on an editable report. Select fields for the editor actions;
   Shift-click selects a range. Drag a column header, or focus it and press
   Alt+Left/Right, to reorder columns. Filters and sorting still refresh in place.

## Where the code lives

| File | Responsibility |
| --- | --- |
| `Views/Shared/Report.cshtml` | Shared page title, resources, and shell for all report modes |
| `Views/Shared/_MapDisplay.cshtml`, `_CalendarDisplay.cshtml`, `Chart.cshtml` | Display-specific rendering |
| `Views/Shared/_ArrangementResources.cshtml` | Page-level custom scripts and styles, also shared by forms |
| `Views/Shared/_Report.cshtml` | Complete refreshable report: query fields, parameters, controls, results, and errors |
| `Views/Shared/_ReportTables.cshtml` | Table and field rendering |
| `Views/Shared/_ReportPagination.cshtml` | Shared pagination, with real navigation URLs in the pilot |
| `Views/Shared/_ReportModal.cshtml` | Accessible modal shell, iframe and loading indicator |
| `Views/Shared/_ReportExports.cshtml` | Downloads and Markdown copy controls |
| `wwwroot/Scripts/report-controller.js` | GET requests, filters, navigation, downloads, focus, loading and errors |
| `wwwroot/Scripts/report-editor-controller.js` | Field editing and native/keyboard column reordering |
| `wwwroot/Scripts/report-selection-controller.js` | Row/range selection, action counts and native bulk POSTs |
| `wwwroot/Scripts/report-modal-controller.js` | Bootstrap modal lifecycle and iframe messages |
| `wwwroot/Scripts/report-clipboard-controller.js` | Markdown conversion and clipboard feedback |
| `wwwroot/Scripts/report-interactivity.js` | Registers the controllers with one Stimulus application |
| `Views/Shared/_ReportFilter.cshtml` | Native canonical select and accessible searchable popup shell |
| `Views/Shared/_ReportFilterOptions.cshtml` | Shared server-rendered options, labels/counts, and selections |
| `wwwroot/Scripts/report-filter-controller.js` | Scoped search, matching On/Off, selection, keyboard and popup lifecycle |
| `wwwroot/Scripts/report-widgets.js` | Shared calendar adapter lifecycle and native selection reset |
| `wwwroot/Styles/report.css` | Report styles formerly embedded in Index |

`ReportController.Index` validates and runs the same report service for both full
pages and fragment requests. It returns `_Report` only when the request targets
`tfl-report`, is an htmx request, and is not history restoration. `Vary` distinguishes
these response representations. Authorization remains in the existing pipeline.

The form's `hx-get`, `hx-target`, `hx-swap`, `hx-push-url`, and `hx-sync` attributes
describe the request and replacement. Named Stimulus actions describe browser
behavior. They do not embed executable application logic in the Razor markup.

Query cleanup operates on request FormData instead of disabling live controls.
Empty values and whole-value `*` wildcards are always omitted, including after
choosing All or Clear. Generated navigation links and chart clicks also omit them.
Repeated concrete facet values, sort expressions, tenant paths,
and unpaged reports are preserved. Each refresh replaces filters and results
together, so facet counts cannot drift from the table. Row selection resets on
refresh. Table reports no longer load `report.js`, BlockUI, Underscore or the
dragtable plugin. The editor and all other display modes use the same scoped controllers. Map
polygon selections add canonical record targets to the shared selection controller.

Bulk actions use a temporary native POST form outside the GET report form. It
carries repeated filters, the antiforgery token, current ReturnUrl, action name,
and selected keys. The existing server contract uses `ActionCount=0` for the
entire filtered result; choosing every visible row individually retains a numeric
count. The server's authorization and review/run workflow are unchanged.

The modal accepts close messages only from its own iframe and the same origin.
Its accessible title is visually hidden, so the wrapper adds no title bar or
top close button; form and review dialogs use their bottom Cancel action.
Once both the iframe and the opening transition are ready, the modal focuses
the iframe and emits `tfl-form:focus`. The form restores its chosen field if
Bootstrap blurred it or initialization ran while the modal was hidden. A field
the user has already selected keeps focus and its caret on repeated openings.
Cancellation closes it without refreshing; confirmation refreshes after the
modal finishes closing. Disconnecting removes the Bootstrap instance, widgets,
and their listeners. The loading indicator is Razor markup rather than HTML
written into the iframe document. Task and form pages use the same postMessage
protocol, including the new shared form controller.

Existing arrangement-generated anchors/buttons with `data-url` and the exact
`onclick="loadModal(this)"` handler are adapted when the modal controller connects,
including after report refreshes. Their inline handler is removed and replaced
with `click->modal#open`; anchors receive the real URL as their href. New
arrangements should emit a real `href` and `data-action="click->modal#open"`
directly. Unrelated custom handlers are left intact.

Unexpected successful HTML, such as a login redirect or a diagnostic view, causes
full navigation instead of being inserted into the report. Failed requests leave
the current report visible, reenable Search, and display a retry/reload message.

The htmx history cache is disabled: reports are not saved in localStorage, and
widget-generated markup is not replayed from a cache. Back/Forward use a full
reload. This intentionally avoids replaying widget-generated markup.

## Custom arrangement scripts

Global JavaScript loads once in the page shell, outside `#tfl-report`. Both inline
scripts (`language="js"`) and script files (`language="js"`, or a `.js` file with
the default language) use this path. Functions such as `window.showLightbox`
remain available to calls from raw data rows after filtering, sorting, or paging.
Script presence never selects a different report renderer.

Forms use the same page-resource partial and lifecycle outside `#tfl-form`.
Scripts must provide their own library dependencies. A script that used the old
report globals (`server`, `controls`, `loadModal`, or `edit`) needs adapting;
modal row links can use `data-action="click->modal#open"` as described above.

For widgets that need initialization on new rows, listen for `htmx:load` once in
the global script. It runs for initial content and after HTML replacement:

```js
document.body.addEventListener('htmx:load', event => {
   const root = event.detail.elt;
   if (root.id === 'tfl-report' || root.querySelector?.('#tfl-report')) {
      initializeLightboxes(root); // Make initialization safe to repeat.
   }
});
```

Use `htmx:beforeCleanupElement` to dispose widget instances before their elements
are removed, or put initialization and cleanup in a custom Stimulus controller.
Global scripts are not replayed on every report refresh.

## Retired implementations

The repository no longer contains `report.js`, `form.js`, `file.handler.js`, their
minified bundles, or the `_LegacyForm*` partials. Shared markup no longer branches
between old/new handlers. `InteractiveReport`, form eligibility checks, and
Parsley-attribute output have been removed.

Unused Bootstrap-select, Parsley, Moment, Pickadate, BlockUI, dragtable,
AreYouSure, custom-file-input, and iframe-upload transport resources/assets have
also been removed from the manifest and bundle configuration. Remaining vendor
libraries serve active features: Blueimp handles image/upload processing;
Mapbox/Turf handle maps; Bootstrap Calendar uses jQuery/Underscore; Chart.js
renders charts. These are dependencies of the single application implementation.

Full GET navigation for non-table displays deliberately rebuilds their visualization;
it uses the report controller's same parameter cleanup, filter, clear, action,
and modal behavior. It does not maintain another general-purpose report script.
Map-specific scripts can observe `report-map:ready` with `event.detail.map`.

## Dependencies and checks

htmx 2.0.11 and Stimulus 3.2.2 are vendored with their licenses and registered in
`ResourceManifest.cs`. The pilot has no new npm/build requirement or runtime CDN
dependency. Flatpickr 4.6.13 is also vendored with its MIT license. Report date
filters share the scoped calendar lifecycle described in
[form-interactivity.md](form-interactivity.md); date selection still triggers one
search with the existing GET contract. Typed dates remain editable, and calendar
instances are destroyed on replacement. Modern reports no longer load jQuery UI
for calendars. Searchable selection menus now use native selects and Stimulus.
Table and chart reports load no jQuery by default. Maps and calendars retain
jQuery for their display code; calendars also retain Underscore for their plugin.
Custom scripts must explicitly load any libraries they need. The native select stays enabled
while busy so its values remain in FormData, while the popup trigger is disabled.

```sh
dotnet build src/Site/Site.csproj
dotnet test test/OrchardCore.Transformalize.Tests/OrchardCore.Transformalize.Tests.csproj
node --test test/client/*.test.cjs
```

The .NET tests resolve the shared shell and nested partials through the real Razor
view engine from Report, Chart, Map, and Calendar controller contexts. They also
cover fragment routing, history requests, failed validation,
editor/custom-script fragment routing, and query-preserving links. The
dependency-free Node tests cover request cleanup, repeated facets, wildcard omission after All/Clear, page zero,
error recovery, login redirects, selection counts, bulk POST data, modal messages,
Markdown output and clipboard rejection. They test controller contracts with form
stubs; widget lifecycle and real HTML replacement also require browser checks.

Current browser checks cover single-choice selection/search, immediate refresh,
Clear, and focus restoration on the real Got Up report, which loads no jQuery or
Bootstrap-select. A separate local report backed by temporary SQLite data covers
multiple-choice staging, repeated `Category=Alpha&Category=Beta` parameters,
matching-only Off, server filtering, and immediate refresh when the last selection
is cleared. Successful real form saves/updates in a modal close and reset the
iframe and refresh that report. The report resource-resolver test also verifies
that the modern resource graph excludes the jQuery adapters. Keyboard checks on
the real Northwind report confirm that Tab reaches On, Off, and Search while the
popup stays open, and Escape restores its trigger. The editor now uses the same
native filters; its field actions and column ordering are covered by the
controller tests. A temporary browser fixture using the production controllers
verifies editor refreshes, keyboard reordering and hiding, plus global lightbox
calls after replacement. That fixture also verifies one page-script load and
repeated `htmx:load` widget initialization. The local site required sign-in, so
these new browser checks did not exercise an authenticated arrangement.

The consolidation browser fixture uses production form/report/selection controllers
with isolated endpoints. It verifies form validation replacement with global and
raw script callers, one page-script load, native save submitter data, full GET
filter cleanup with repeated facets, and dynamic map record counts/bulk POSTs.
It does not exercise authenticated arrangements or live Mapbox/Google services.

Wildcard cleanup browser checks use the production report controller and widgets:
starting with the crime-report wildcard query, Clear yields only
`?sort=Id.d` for both htmx and full-page navigation when the configured default
page size is 25. Selecting a concrete
facet includes it; returning to All removes it while retaining focus metadata.

## Paging and migration parity audit

`ReportPaging` derives choices from the report's override or module settings. Its
smallest configured size is the default, matching the existing loader behavior.
Only a nondefault choice belongs in the URL. Invalid/unsupported sizes fall back
to that default; values above the maximum retain the existing maximum cap.

The hidden navigation size comes from the validated requested preference, never
`entity.Size`. Charts still fetch one entity row for efficiency, since their data
comes from facet maps; that internal size must not appear in navigation state.
A table → chart → filter → table round trip retains an explicit size such as 50,
otherwise returns to the configured table default. Changing view resets its page
and returning to a table removes chart-specific display options. Unpaged map and
calendar displays retain the user's size preference for a later return to a table.
Exports and visualization data URLs receive the same size policy.

The parity review compared the removed scripts and views against the current
controllers. It does not establish that every custom arrangement is compatible.

| Behavior | Regression coverage |
| --- | --- |
| Inherited/overridden/default/invalid page sizes | `ReportPagingTests`, `ArrangementLoadServiceTests`, client request tests |
| Chart internal size and round trips | Model navigation tests, shared controller tests, and authenticated crime report browser check |
| Clean URLs, repeated facets, sort and first/unpaged requests | Navigation/link tests and client request tests |
| Controller-specific Razor lookup | `ReportViewLookupTests` for all four modes |
| Search staging, All, Clear, keyboard/focus | Filter, widget and report controller tests |
| Row/range/all-result and map selection; bulk request metadata | Selection tests |
| Modal review/cancel/confirmation, origin/source checks | Modal tests |
| Modal links inserted after connection, including map popups | Observer adaptation/teardown regression test |
| Form validation, native action, dirty state, widget lifecycle | Form controller and widget tests |

The audit found and fixed a chart fetch-size leak, missing lower-size validation,
default-size URL noise, destination paging cleanup, loss of concrete repeated
filters when cleaning mixed wildcard values, and late-inserted modal links.

Authenticated browser checks on the live crime report confirmed the inherited
25-row default, clean links when selecting that default, and a 50-row table →
chart → district/beat slice filters → table round trip (450 matching records).
A further grid slice reduced the chart to 12 records; returning to the table kept
25 selected and omitted `size`, wildcard filters, and chart display options from
the URL. An incoming `size=1` rendered 25 rows with 25 selected; choosing that
default removed the invalid parameter. No browser console errors were observed
during these checks.

Live provider writes, arbitrary custom scripts, and external map/Places services
still require arrangement-specific checks; controller tests do not replace them.
