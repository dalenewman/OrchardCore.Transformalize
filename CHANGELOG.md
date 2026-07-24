# 0.21.1 <small>2026-07-24</small>

## ⬆️ Dependencies
- Updated `Transformalize.Container.Autofac` (1.4.2 → 1.4.4) and `Transformalize.Transform.Geography` (1.0.0 → 1.4.4) to pick up the updated `Distance` transform from the main Transformalize and Geography libraries.

## 🛠️ Maintenance
- Cleared the nullable-reference warnings (CS8618) in `PipelineViewModel` by marking the always-initialized `Link` and `Glyphicon` properties `required` and typing the null-checked `_label`/`_title` backing fields as nullable.

<!-- CHANGELOG_BOUNDARY -->

# 0.21.0 <small>2026-06-22</small>

## ⬆️ Dependencies
- Upgraded to **OrchardCore 3.0.0** (from 2.2.1). Addressed breaking changes:
  - `OrchardCore.Contents.Permissions.EditContent` moved to `OrchardCore.Contents.CommonPermissions.EditContent` (updated across the affected views).
  - `FileSystemStore` now requires an `ILogger<FileSystemStore>` constructor argument (`CustomFileStore` and its registration in `Startup`).
- Removed the obsolete pinned package overrides (AngleSharp, HtmlSanitizer, Microsoft.Extensions.Http.Resilience, Microsoft.AspNetCore.Authentication.OpenIdConnect) now that OrchardCore 3.0.0 ships compatible versions.
- Aligned shared dependencies with OrchardCore 3.0.0 (NodaTime 3.3.2, Microsoft.NET.Test.Sdk 18.6.0, Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation 10.0.8).
- Updated Transformalize packages to 1.4.3.

## 🔒 Security
- Pinned `SQLitePCLRaw.bundle_e_sqlite3` to `3.0.3` to resolve **CVE-2025-6965** (high-severity SQLite memory-corruption). The transitive 2.1.x native bundle (`SQLitePCLRaw.lib.e_sqlite3`) is deprecated with no fixed release; 3.0.3 uses the patched `SourceGear.sqlite3` (≥ 3.50.4.5).

<!-- CHANGELOG_BOUNDARY -->

# 0.20.4 <small>2026-05-15</small>

## 💅 Improvements
- Upgraded the XML arrangement editor from CodeMirror 5 to CodeMirror 6.
- The editor search is now an inline panel (Ctrl-F / ⌘F) with next/previous/replace buttons.
- Fullscreen mode uses the native browser Fullscreen API (⛶ button, Esc to exit).
- Save shortcut works cross-platform: Ctrl-S on Windows/Linux, ⌘S on Mac.
- Autocomplete triggers on typing `<` in addition to Ctrl-Space.

<!-- CHANGELOG_BOUNDARY -->

# 0.20.3 <small>2026-05-08</small>

## 🐛 Bug Fixes
- Fix `Task/Review` not hiding nav and footer when displayed in a modal.
- Fix shared `Log` view not hiding nav/footer, not showing a Close button, and reserving excess vertical space when displayed in a modal.
- Fix README gif path after `samples` folder was relocated out of `App_Data`.

<!-- CHANGELOG_BOUNDARY -->

# 0.20.2 <small>2026-05-05</small>

## 🐛 Bug Fixes
- Change numeric input `step` from `0.0000001` to `any` to prevent spurious Parsley validation errors on decimal values.

<!-- CHANGELOG_BOUNDARY -->

# 0.20.1 <small>2026-04-30</small>

## 💅 Improvements
- Parameters with an `env` attribute now automatically read their default value from the corresponding environment variable, but only when the caller has not already supplied a value for that parameter. Read more about it [here](./docs/environment-variables.md).

<!-- CHANGELOG_BOUNDARY -->

# 0.20.0 <small>2026-04-29</small>

## 💅 Improvements
- Updated `Transformalize.Provider.Ado.Autofac` and `Transformalize.Provider.PostgreSql.Autofac` to 1.1.0.
- Updated sacramento-crime sample to use `specifykind(utc)` transform on the `cdatetime` field.

<!-- CHANGELOG_BOUNDARY -->

# 0.19.3 <small>2026-04-29</small>

## 🐛 Bug Fixes
- Add forwarded headers middleware to `Site/Program.cs` so that links (e.g. download URLs) are
  generated with `https://` when the app runs behind a reverse proxy such as an AWS ALB or Azure
  Application Gateway that terminates TLS. Without this, the container sees plain HTTP and generates
  `http://` links, which can break downloads due to auth cookies being stripped on redirect.

<!-- CHANGELOG_BOUNDARY -->

# 0.19.2 <small>2026-04-29</small>

## 💅 Improvements
- Move `samples` folder to the same level as `App_Data` so sample files are not nested inside Orchard's managed data directory.

<!-- CHANGELOG_BOUNDARY -->

# 0.19.1 <small>2026-04-28</small>

## 🐛 Bug Fixes
- Remove dependency on location partial so a task using the shared form control doesn't error out when handling hidden fields.

<!-- CHANGELOG_BOUNDARY -->

# 0.19.0 <small>2026-04-26</small>

## 🚀 Features
- Add map picker for forms (`input-type="map"`). Configure latitude and longitude parameters with
  `input-type="map"` to render an interactive MapBox map inline in the form. Users can click or drag
  a pin to set coordinates, which sync to the editable numeric inputs. Typing directly in the inputs
  repositions the pin. Works in standard and modal form modes. See `docs/map-picker.md`.

<!-- CHANGELOG_BOUNDARY -->

# 0.18.2 <small>2026-04-12</small>

## 💅 Improvements
- Updated to Transformalize 1.1.0 to get _bucketize_ transform 🪣👀.

<!-- CHANGELOG_BOUNDARY -->

# 0.18.1 <small>2026-04-08</small>

## 💅 Improvements
- Added [AutoSetup](https://github.com/OrchardCMS/OrchardCore/blob/v2.2.1/src/docs/reference/modules/AutoSetup/README.md) for bypassing setup screen and configuring with environment variables instead (usually passed into container)
- Added _DatabaseShells_ for similar reasons

<!-- CHANGELOG_BOUNDARY -->

# 0.18.1 <small>2026-04-08</small>

## 💅 Improvements
- Remove need for Nito.Async package by switching a whole bunch of methods to async

## 🐞 Bug Fixes
- Fix bool handling in markdown to clipboard feature

<!-- CHANGELOG_BOUNDARY -->

# 0.17.8 <small>2026-04-06</small>

## 🐞 Bug Fixes
- Check for content item permission before adding them to Transformalize types
- Also get back to 0.* versions (accidently went to 1.*)

<!-- CHANGELOG_BOUNDARY -->

# 0.17.7 <small>2026-04-05</small>

## 🐞 Bug Fixes
- Remove chart flash before animation
- Add content item permissions part to Transformalize types

<!-- CHANGELOG_BOUNDARY -->

# 0.17.6 <small>2026-04-05</small>

## 🚀 Features
- Add report title and record count to chart view

## 🐞 Bug Fixes
- Fix chart animations

<!-- CHANGELOG_BOUNDARY -->

# 0.17.5 <small>2026-04-04</small>

## 🚀 Features
- Charts are now interactive; click a slice or bar to filter the report by that value.

## 🐞 Bug Fixes
- Fix single-quoted values appearing doubled in chart labels and legends.

<!-- CHANGELOG_BOUNDARY -->

# 0.17.4 <small>2026-04-04</small>

## 🚀 Features
- Incorporate Vlad's chart feature with modifications.

<!-- CHANGELOG_BOUNDARY -->

# 0.17.3 <small>2026-04-02</small>

## 🐞 Bug Fixes
- Respect report filter when selecting all records and using modal bulk actions
- Don't add dragging to report headers unless edit mode is enabled

<!-- CHANGELOG_BOUNDARY -->

# 0.17.2 <small>2026-03-30</small>

## 🚀 Features
- Add a search button inside the facets multi-select

<!-- CHANGELOG_BOUNDARY -->

# 0.17.2 <small>2026-03-28</small>

## 🛠️ Maintenance
- Update content permissions to 1.1.0
- Override vulnerable HtmlSanitizer package

<!-- CHANGELOG_BOUNDARY -->

# 0.17.0 <small>2026-03-28</small>

## 🚀 Features
- Added Bootswatch Theme
- Added Markdown copy option on report page

<!-- CHANGELOG_BOUNDARY -->

# 0.15.1 <small>2026-03-17</small>

## 🐞 Bug Fixes
- All parameters are displayed on map, calendar pages so you don't lose filters when you press search

## 🛠️ Maintenance
- Update transformalize packages and dependencies

<!-- CHANGELOG_BOUNDARY -->

# 0.15.0 <small>2026-03-07</small>

## 🐞 Bug Fixes
- Replace synchronous `Run` with async methods to prevent deadlocks when a `SynchronizationContext` is present (Azure/IIS)

<!-- CHANGELOG_BOUNDARY -->

# 0.14.0 <small>2026-02-17</small>

## 🚀 Features
- To open bulk actions and forms up in a modal instead of another page, add `modal="true"` to the `action`. See [crime](src/Site/App_Data/samples/sacramento-crime/report.xml) and [gotup](src/Site/App_Data/samples/gotup/got-up-report.xml) for examples.

## 🐞 Bug Fixes
- Disable `invalid-characters` for batch fail tasks (in recipes) in order to see error messages better.
- Load `jquery-ui` in _head_ rather than _foot_ to avoid a flurry of console errors
- Switch from `\` (windows) to `/` (linux) in sample file paths
- Update some `badge-warning` to `text-bg-warning` in some samples (bootstrap css)

<!-- CHANGELOG_BOUNDARY -->
