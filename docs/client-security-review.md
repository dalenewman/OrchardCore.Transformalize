# PR #75 security review

The Semgrep findings on PR #75 include application changes and audit warnings in
vendored browser libraries. Application code and all three libraries remain in
the scan; only the reviewed expressions carry rule-specific `nosemgrep` comments.
Recheck these decisions when updating the bundles or their configuration.

## Application changes

- All workflow actions use full commit SHAs, resolved from their existing version
  tags on 2026-10-21. Comments retain the version family for updates. For CodeQL's
  annotated tag, the pin is the underlying commit rather than the tag object.
- Custom form patterns run through the existing server validation on refresh and
  save/run. `form-widgets.js` no longer constructs arbitrary regexes on the main
  thread. Required, type, length, range, date, and native input checks remain local.
  Server pattern feedback still appears inline; pattern-only failures no longer
  produce immediate blur feedback.
- Both interactivity entry points set `htmx.config.allowEval = false` before
  starting controllers. Keep event logic in controllers; htmx expression filters,
  `hx-on`, `js:` values, and `htmx._()` evaluation are unavailable. This setting
  governs htmx expression evaluation, not script tags or arrangement HTML.

## Reviewed library findings

### Flatpickr 4.6.13 (alerts 132–136)

`date-widgets.js` creates a fresh unnamed hidden input with no `data-*` options and
passes application-owned options to Flatpickr. The named parameter input is never
passed to the library. The wrapper supplies only validated ISO calendar dates and
uses a fixed `Y-m-d` format and the built-in English locale.

- Dynamic regexes are built from library date-token expressions and the fixed
  English AM/PM locale, rather than arrangement validation patterns. Typed form
  text is not passed to Flatpickr's parser.
- `insertAdjacentHTML` renders week numbers only when `weekNumbers` is enabled.
  Our options leave it disabled and never supply a custom `getWeek` callback.
- `Object.assign` combines configuration from the fresh helper, library defaults,
  and application options. It is not used to merge submitted parameter objects.

The minified bundle has one executable line, so its preceding suppression lists
only these three audit rules. Do not pass parameter JSON, custom locales, date
formats, or arbitrary element datasets into this library without revisiting it.

### htmx 2.0.11 (alerts 139–140)

- The upstream `eval` call is behind `maybeEval`, which checks `allowEval`. Both
  application entry points disable it. The suppression is local to that call;
  other eval findings remain visible.
- The status matching regex comes from htmx's own `responseHandling`
  configuration and matches the three-digit HTTP status, not response content
  or form input. The application does not configure custom status patterns.

### Stimulus 3.2.2 (alerts 141–142)

The two console calls now use constant format strings and separate arguments.
Controller/outlet identifiers containing format tokens cannot consume the element
argument as a formatter. These are local patches to the published bundle; retain
them or confirm equivalent upstream fixes when upgrading.

## Vendor provenance

Before editing, all three files matched the published distributions byte for
byte. Original SHA-256 checksums:

| File | Published distribution | Original SHA-256 |
| --- | --- | --- |
| `flatpickr.js` | [flatpickr 4.6.13](https://cdn.jsdelivr.net/npm/flatpickr@4.6.13/dist/flatpickr.min.js) | `1eeab1cb779471a0b0aaa93dd91c2eb1aa537d696f01ab05ea9dabc55e8525a1` |
| `htmx.js` | [htmx 2.0.11](https://cdn.jsdelivr.net/npm/htmx.org@2.0.11/dist/htmx.js) | `30612ca7e3d3baf407481f1798281c4481dcb93390a9722d328ce07290250426` |
| `stimulus.umd.js` | [Stimulus 3.2.2](https://cdn.jsdelivr.net/npm/@hotwired/stimulus@3.2.2/dist/stimulus.umd.js) | `60c23dc9e99519da0f7ce35512f0d1d1b0ab307d548e39f21e061cfd12bb2ff2` |

Flatpickr and htmx changes are review comments only. Stimulus changes are the two
console formatting fixes. Do not treat this source audit as a dependency
vulnerability scan or as sanitization of raw arrangement HTML.

## Verification

```sh
node --test test/client/*.test.cjs
dotnet test test/OrchardCore.Transformalize.Tests/OrchardCore.Transformalize.Tests.csproj
semgrep scan --config auto --sarif -o semgrep-results.sarif
```

The client regressions check that custom patterns never construct browser
regexes (including a catastrophic-backtracking example), server errors still
appear inline, and both entry points disable expression evaluation before
controller startup. Review any remaining scan findings separately; these changes
do not dismiss unrelated historical alerts in GitHub.
