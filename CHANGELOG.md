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
