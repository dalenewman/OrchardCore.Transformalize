# Report interactivity

Ordinary reports use Razor partials, htmx, and Stimulus. Reports opened with
`edit=1` or containing global arrangement JavaScript retain the existing behavior.
Standard forms and task reviews now use the same approach; see
[form interactivity](form-interactivity.md). Forms and report filters have scoped
widget lifecycles; charts and other report display modes retain their existing
client code. For before-and-after examples, see the
[developer guide](client-interactivity-guide.md).

## Try it

Run the Site project, sign in, and open `/t/report/northwind?size=25` (if the sample
recipe is installed), or another report without global JavaScript.

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
8. Open Action → Edit. The editor continues to use the original client behavior.

## Where the code lives

| File | Responsibility |
| --- | --- |
| `Views/Report/Index.cshtml` | Page title, resources, and the page shell |
| `Views/Report/_Report.cshtml` | Complete refreshable report: query fields, parameters, controls, results, and errors |
| `Views/Report/_ReportTables.cshtml` | Table and field rendering |
| `Views/Shared/_ReportPagination.cshtml` | Shared pagination, with real navigation URLs in the pilot |
| `Views/Shared/_ReportModal.cshtml` | Accessible modal shell, iframe and loading indicator |
| `Views/Shared/_ReportExports.cshtml` | Downloads and Markdown copy controls |
| `wwwroot/Scripts/report-controller.js` | GET requests, filters, navigation, downloads, focus, loading and errors |
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
Repeated facet values, sort expressions, explicit All selections, tenant paths,
and unpaged reports are preserved. Each refresh replaces filters and results
together, so facet counts cannot drift from the table. Row selection resets on
refresh. Ordinary reports no longer load `report.js`, BlockUI, Underscore or the
dragtable plugin. The editor and reports with custom scripts keep those resources.

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

## Dependencies and checks

htmx 2.0.11 and Stimulus 3.2.2 are vendored with their licenses and registered in
`ResourceManifest.cs`. The pilot has no new npm/build requirement or runtime CDN
dependency. Flatpickr 4.6.13 is also vendored with its MIT license. Report date
filters share the scoped calendar lifecycle described in
[form-interactivity.md](form-interactivity.md); date selection still triggers one
search with the existing GET contract. Typed dates remain editable, and calendar
instances are destroyed on replacement. Modern reports no longer load jQuery UI
for calendars. Searchable selection menus now use native selects and Stimulus.
Modern reports load neither jQuery nor Bootstrap-select; legacy reports retain
their existing resources. The native select stays enabled while busy so its
values remain in FormData, while the popup trigger is disabled.

```sh
dotnet build src/Site/Site.csproj
dotnet test test/OrchardCore.Transformalize.Tests/OrchardCore.Transformalize.Tests.csproj
node --test test/client/*.test.cjs
```

The .NET tests cover fragment routing, history requests, failed validation,
compatibility eligibility, and query-preserving links. The dependency-free Node
tests cover request cleanup, repeated facets, explicit All selections, page zero,
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
popup stays open, and Escape restores its trigger. Its editor mode still renders
the legacy Bootstrap-select menus with the same option values and resources.
