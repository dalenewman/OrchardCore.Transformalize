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
