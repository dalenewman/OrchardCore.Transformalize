# BootswatchTheme — Design Notes & Maintenance Guide

## What this theme does

`OrchardCore.BootswatchTheme` extends **TheTheme** (OrchardCore's default Bootstrap 5
theme).  When no Bootswatch style is selected in admin settings it behaves exactly like
TheTheme.  When a Bootswatch style *is* selected the theme:

1. Loads the Bootswatch variant of Bootstrap from the jsDelivr CDN instead of the
   locally-bundled Bootstrap copy.
2. Suppresses TheTheme's compiled stylesheet (`theme.min.css`) so Bootstrap is not
   loaded twice.
3. Restores the handful of layout rules that `theme.min.css` would have provided via
   an inline `<style>` block (see the section "Inline layout CSS" below).
4. Colours the navbar with `bg-primary navbar-dark` so the brand bar picks up the
   Bootswatch theme's primary colour with white navigation text.

The companion module `OrchardCore.BootswatchTheme.Settings` provides the admin UI
that lets site owners pick a Bootswatch theme (or revert to the Bootstrap default).

---

## File map

```
OrchardCore.BootswatchTheme/
├── Views/
│   ├── Layout.cshtml          ← only file that differs from TheTheme
│   └── _ViewImports.cshtml
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
| `Assets/scss/main/_layout.scss` | sticky footer, `body > .container` top-padding, `#togglePassword` width |
| `Assets/scss/themes/light/_index.scss` | `footer` background for light mode |
| `Assets/scss/themes/dark/_index.scss`  | `footer` background for dark mode |

The one intentional difference: `body > .container` uses **80 px** of top-padding
instead of TheTheme's 60 px, because several Bootswatch themes render a taller navbar
(observed ~72 px).  If a future Bootswatch update changes navbar height significantly,
adjust this value in `Layout.cshtml`.

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
| New rule added to `_layout.scss` | Add the same rule to the inline `<style>` block inside the `useBootswatch` branch |
| New light/dark theme CSS variables | Add matching declarations to the inline `<style>` block |
| `$navbar-padding-y` or navbar height changes in Bootstrap | Adjust the `80px` value in `body > .container { padding: 80px 15px 0; }` |

### Quick diff command

```bash
git -C /path/to/orchardcore diff <old-tag> <new-tag> \
  -- src/OrchardCore.Themes/TheTheme/Views/Layout.cshtml \
     src/OrchardCore.Themes/TheTheme/Assets/scss/main/_layout.scss \
     src/OrchardCore.Themes/TheTheme/Assets/scss/themes/
```

---

## Adding a new Bootswatch theme

Bootswatch publishes new themes occasionally.  To add one:

1. Verify the theme name exists at `https://cdn.jsdelivr.net/npm/bootswatch@5/dist/`.
2. Add the name (lowercase) to the array in
   `OrchardCore.BootswatchTheme.Settings/Constants.cs`.
3. Rebuild the Settings project.

No CSS changes are needed — the CDN URL in `Layout.cshtml` uses the theme name
directly:
```html
https://cdn.jsdelivr.net/npm/bootswatch@5/dist/@bsTheme/bootstrap.min.css
```

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| Content hidden behind navbar | Inline `body > .container` padding too small | Increase the `80px` value |
| User menu appears on the left | `ms-md-auto` missing or `<menu>` renders no DOM node | Verify the `<div class="ms-md-auto">` wrapper exists |
| Menu items invisible | Navbar text colour wrong for the selected Bootswatch theme | Adjust `navbar-dark`/`navbar-light` class on `<nav>` (line 87) |
| Bootstrap loaded twice | `ResourceManager.NotRequired("stylesheet","TheTheme")` not called | Ensure the suppression call is inside the `useBootswatch` block |
| Bootswatch CDN fails offline | CDN unavailable | Consider bundling preferred themes locally via npm/yarn |
