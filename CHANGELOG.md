# 0.14.0 <small>2026-02-17</small>

## 🚀 Features
- To open bulk actions and forms up in a modal instead of another page, add `modal="true"` to the `action`. See [crime](src/Site/App_Data/samples/sacramento-crime/report.xml) and [gotup](src/Site/App_Data/samples/gotup/got-up-report.xml) for examples.

## 🐞 Bug Fixes
- Disable `invalid-characters` for batch fail tasks (in recipes) in order to see error messages better.
- Load `jquery-ui` in _head_ rather than _foot_ to avoid a flurry of console errors
- Switch from `\` (windows) to `/` (linux) in sample file paths
- Update some `badge-warning` to `text-bg-warning` in some samples (bootstrap css)

<!-- CHANGELOG_BOUNDARY -->
