# BootswatchTheme — Design Notes & Maintenance Guide

## What this theme does

`OrchardCore.BootswatchTheme` extends **TheTheme** (OrchardCore's default Bootstrap 5
theme).  When no Bootswatch style is selected in admin settings it behaves exactly like
TheTheme.  When a Bootswatch style *is* selected the theme:

1. Loads the selected variant of Bootstrap, using jsDelivr or the bundled copy
   according to the site's CDN setting. The custom **Codex** style is always local.
2. Suppresses TheTheme's compiled stylesheet (`theme.min.css`) so Bootstrap is not
   loaded twice.
3. Restores the handful of layout rules that `theme.min.css` would have provided via
   an inline `<style>` block (see the section "Inline layout CSS" below).
4. Colours the navbar with `bg-primary navbar-dark` for the published Bootswatch
   styles. Codex uses a neutral navbar that follows the light/dark palette.

The companion module `OrchardCore.BootswatchTheme.Settings` provides the admin UI
that lets site owners pick a Bootswatch theme (or revert to the Bootstrap default).

## Codex

Select **Design → Bootswatch → Codex** in the admin site, then save. This changes
the site's frontend theme; Orchard's admin theme is independent.

Codex is a custom Bootstrap **5.3.8** build, matching the existing bundled Bootswatch
styles. It uses warm neutral surfaces, teal actions, system fonts, restrained
borders and shadows, and tabular numerals in data tables. Every Bootstrap component
is included. No remote fonts or CSS imports are needed.

Use the appearance dropdown (sun, moon, or half-circle icon) at the right of the
frontend navbar to choose **Light**, **Dark**, or **Auto**. On small screens, open
the navigation menu first. Auto follows the operating system's appearance, and
Orchard remembers the choice in this browser for the current tenant.

`Drivers/ToggleThemeNavbarDisplayDriver.cs` exposes Orchard's inherited toggle view
for BootswatchTheme; TheTheme's own driver only renders for TheTheme itself.
Orchard's scripts control the palette through `data-bs-theme`. Dark mode has its
own button, utility, and semantic table colours.
The layout's `data-bootswatch="codex"` attribute scopes the navbar, footer, and
Transformalize-specific adjustments. Report tables retain their compact density and
horizontal scrolling. Forms preserve validation states and visible keyboard focus.
Bootstrap's reduced-motion support is retained, and print styles clear the fixed
navbar and render data tables on white.

The Codex resources deliberately have **no CDN URL**: both values of **Use CDN**
serve the local files. RTL cultures use the generated `bootstrap.rtl` files;
other cultures use the regular files. Existing Bootswatch styles keep their
current resource paths and behaviour.

### Rebuilding Codex

```bash
cd src/OrchardCore.BootswatchTheme/Assets
npm ci
npm run build
```

Edit `Assets/codex.scss`, then rebuild and commit all four generated files under
`wwwroot/styles/codex/`. `Assets/package-lock.json` pins the build dependencies.
The .NET build and deployed site do not require Node.js. `download-bootswatch.sh`
refreshes only the published styles and never overwrites Codex.

To preview components with the real report/form CSS, serve `src` from the repo root:

```bash
python3 -m http.server 5099 --bind 127.0.0.1 --directory src
```

Open `http://localhost:5099/OrchardCore.BootswatchTheme/Assets/preview.html`.
Its controls switch light/dark and LTR/RTL. Check keyboard focus, validation,
dropdowns, dialogs, accordion, and a narrow viewport. Bootstrap's JavaScript for this
development preview comes from `Assets/node_modules`, so run `npm ci` first.

---

## File map

```
OrchardCore.BootswatchTheme/
├── Views/
│   ├── Layout.cshtml          ← only file that differs from TheTheme
│   └── _ViewImports.cshtml
├── Assets/                    ← Codex Sass source, pinned build and preview
├── Drivers/                   ← frontend light/dark/auto appearance selector
├── wwwroot/styles/codex/      ← generated CSS, including RTL, and Bootstrap license
├── ResourceManifest.cs        ← local/CDN resource registrations
└── THEMING.md                 ← this file

OrchardCore.BootswatchTheme.Settings/
├── Models/BootswatchSettings.cs
├── Constants.cs               ← list of available Bootswatch theme names
├── Drivers/BootswatchSettingsDisplayDriver.cs
├── Navigation/BootswatchSettingsAdminMenu.cs
├── Permissions.cs
├── Startup.cs
└── Views/BootswatchSettings.Edit.cshtml
```

All other views (Menu, MenuItem, Branding, NavbarUserMenu, Pager, Widget…) are
**inherited from TheTheme** at runtime and require no duplication here.

---

## Navbar layout

```html
<nav class="navbar navbar-expand-md fixed-top [bg-primary navbar-dark when Bootswatch]">
  <div class="container">
    <Branding />          <!-- site logo / name -->
    <toggler-button />    <!-- hamburger for mobile -->
    <div class="collapse navbar-collapse">
      <div class="d-flex w-100 align-items-center flex-column flex-md-row">
        <menu alias="alias:main-menu" ... />   <!-- main nav: grows from the left -->
        <div class="ms-md-auto">@navbar</div>  <!-- user menu: pinned to the right -->
      </div>
    </div>
  </div>
</nav>
```

Key decisions:
- `align-items-center` — vertically centres the main menu and user menu inside the
  navbar (better than `align-items-end` for compact navbars).
- `ms-md-auto` on the user-menu wrapper — pushes it to the far right on desktop
  regardless of whether the main menu has any items.  Without this, an empty main menu
  would cause the user menu to drift left.

---

## Inline layout CSS (Bootswatch path only)

When Bootswatch is active, TheTheme's `theme.min.css` is suppressed.  That file is
compiled from these SCSS sources in TheTheme:

| Source file | Rules we restore inline |
|-------------|--------------------------|
| `Assets/scss/main/_layout.scss` | `body > .container` top-padding, `#togglePassword` width |
| `Assets/scss/themes/light/_index.scss` | `footer` background for light mode |
| `Assets/scss/themes/dark/_index.scss`  | `footer` background for dark mode |

`body > .container` uses **50 px** of top-padding to clear the compact navbar.
If a future Bootswatch update changes navbar height significantly, adjust this value
in `Layout.cshtml`.

The shared layout block applies to both Bootswatch and the default Bootstrap style.
It replaces TheTheme's absolute footer and 60 px body margin with a flex column:
the main content grows to fill short pages, and the footer follows it in normal flow.
Long reports therefore push the footer below the table. Footer height is automatic
so wrapped or multi-line widgets also reserve their full height.

---

## _ViewImports.cshtml — critical tag helpers

`_ViewImports.cshtml` must mirror TheTheme's registrations, or features silently break.
The most important entry is `@addTagHelper *, OrchardCore.Menu` — without it, the
`<menu alias="alias:main-menu">` tag in `Layout.cshtml` is treated as a plain HTML
`<menu>` element and outputs **nothing**, causing the main nav to vanish entirely and
the user menu to lose its right-side anchor.

Current required set (keep in sync with `TheTheme/Views/_ViewImports.cshtml`):
```
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, OrchardCore.DisplayManagement
@addTagHelper *, OrchardCore.ResourceManagement
@addTagHelper *, OrchardCore.Menu          ← main-menu tag helper lives here
```

---

## Keeping in sync with TheTheme

TheTheme is developed upstream in the OrchardCore repository.  When OrchardCore
releases a new version, diff the following files against our `Layout.cshtml`:

```
orchardcore/OrchardCore/src/OrchardCore.Themes/TheTheme/Views/Layout.cshtml
orchardcore/OrchardCore/src/OrchardCore.Themes/TheTheme/Assets/scss/main/_layout.scss
orchardcore/OrchardCore/src/OrchardCore.Themes/TheTheme/Assets/scss/themes/
```

Things to watch for:

| Change in TheTheme | Action needed here |
|--------------------|--------------------|
| New `<using>` or `@inject` in Layout | Copy into our Layout.cshtml |
| New resource (`<style>` or `<script>` tag) | Add matching tag in both the Bootswatch and non-Bootswatch branches |
| Change to navbar HTML structure | Mirror in our Layout.cshtml |
| New rule added to `_layout.scss` | Check whether it belongs in the Bootswatch restore block or shared layout block; preserve the footer's normal flow |
| New light/dark theme CSS variables | Add matching declarations to the inline `<style>` block |
| `$navbar-padding-y` or navbar height changes in Bootstrap | Adjust the `50px` value in `body > .container { padding: 50px 15px 0; }` |

### Quick diff command

```bash
git -C /path/to/orchardcore diff <old-tag> <new-tag> \
  -- src/OrchardCore.Themes/TheTheme/Views/Layout.cshtml \
     src/OrchardCore.Themes/TheTheme/Assets/scss/main/_layout.scss \
     src/OrchardCore.Themes/TheTheme/Assets/scss/themes/
```

---

## CDN vs local fallback

The Bootswatch stylesheet is registered in `ResourceManifest.cs` with both a CDN
URL (jsDelivr) and a local path under `wwwroot/styles/<theme>/`.  OrchardCore's
**Use CDN** toggle (Configuration → Settings → General) controls which one is served:

| CDN toggle | Source |
|------------|--------|
| On (default) | `https://cdn.jsdelivr.net/npm/bootswatch@5/dist/<theme>/bootstrap.min.css` |
| Off | `~/BootswatchTheme/styles/<theme>/bootstrap.min.css` |

Codex is the exception: it is always served from `~/BootswatchTheme/styles/codex/`.

**To populate the local files**, run the download script from the project directory:

```bash
chmod +x download-bootswatch.sh
./download-bootswatch.sh
```

This downloads both the minified and debug CSS for every theme into `wwwroot/styles/`.
Commit the resulting files so they're available in production without network access.

When Bootswatch releases a new minor version, re-run the script and commit the
updated CSS. The CDN URL uses `bootswatch@5` (latest 5.x), so local and CDN will
stay in sync as long as you refresh after each Bootswatch release.

---

## Adding a new Bootswatch theme

Bootswatch publishes new themes occasionally.  To add one:

1. Verify the theme name exists at `https://cdn.jsdelivr.net/npm/bootswatch@5/dist/`.
2. Add the name (lowercase) to the array in
   `OrchardCore.BootswatchTheme.Settings/Constants.cs`.
3. Rebuild the Settings project.

Also add the published theme to `download-bootswatch.sh` and download its local
CSS. `ResourceManifest.cs` constructs its local and CDN paths from the theme name.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| Content hidden behind navbar | Inline `body > .container` padding too small | Increase the `50px` value |
| User menu appears on the left | `ms-md-auto` missing or `<menu>` renders no DOM node | Verify the `<div class="ms-md-auto">` wrapper exists |
| Menu items invisible | Navbar text colour wrong for the selected Bootswatch theme | Adjust `navbar-dark`/`navbar-light` class on `<nav>` (line 87) |
| Bootstrap loaded twice | `ResourceManager.NotRequired("stylesheet","TheTheme")` not called | Ensure the suppression call is inside the `useBootswatch` block |
| Bootswatch CDN fails offline | CDN unavailable | Consider bundling preferred themes locally via npm/yarn |
