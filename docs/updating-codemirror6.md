# Updating CodeMirror 6

CodeMirror 6 is shipped as a pre-built IIFE bundle committed to `wwwroot/Scripts/`. This document explains how to rebuild the bundle when upgrading CM6 packages.

## Prerequisites

- Node.js 20+ (`node --version`)
- npm (included with Node.js)

These are only needed when rebuilding the bundle — not at .NET build time.

## Directory layout

```
src/OrchardCore.Transformalize/
  cm6-build/
    package.json    ← npm dependencies + build script entry
    build.mjs       ← esbuild runner
    entry.js        ← CM6 initialization source (IIFE)
    node_modules/   ← git-ignored, created by npm install
  wwwroot/Scripts/
    codemirror6-bundle.js      ← committed, produced by npm run build
    codemirror6-bundle.min.js  ← committed, produced by npm run build
```

## How to upgrade a CM6 package

1. Open `cm6-build/package.json` and update the version of the desired package in the `dependencies` section. Example:

   ```json
   "@codemirror/view": "^6.37.0"
   ```

2. Run the build from inside the `cm6-build/` directory:

   ```sh
   cd src/OrchardCore.Transformalize/cm6-build
   npm install
   npm run build
   ```

3. Verify the two output files were updated:

   ```sh
   ls -lh ../wwwroot/Scripts/codemirror6-bundle*.js
   ```

4. Open `src/OrchardCore.Transformalize/Common.cs` and increment the version string:

   ```csharp
   public const string CodeMirrorVersion = "6.x.y";
   ```

   This busts the browser cache for users who have the old bundle cached. Use a meaningful string like the date or a sequence number — it does not need to match any npm package version.

5. Test by running the OrchardCore host and editing a Transformalize arrangement or settings. Verify:
   - XML syntax highlighting
   - Code folding: **Ctrl-Q** folds/unfolds
   - Search panel: **Ctrl-F**
   - Autocomplete: **Ctrl-Space** (also triggers on `<`)
   - Fullscreen: **F11** enters, **Esc** exits
   - Save: **Ctrl-S** submits the form

6. Commit the updated files:
   - `cm6-build/package.json`
   - `cm6-build/package-lock.json`
   - `wwwroot/Scripts/codemirror6-bundle.js`
   - `wwwroot/Scripts/codemirror6-bundle.min.js`
   - `Common.cs`

## Modifying editor behaviour

Edit `cm6-build/entry.js` then re-run `npm run build`. The entry file is the source of truth for:
- Editor extensions (syntax, folding, search, autocomplete, etc.)
- Custom keybindings (Ctrl-Q, Ctrl-S, F11, Tab, etc.)
- Fullscreen implementation
- Textarea sync logic

## CM6 package → feature mapping

| Feature | Package |
|---|---|
| XML syntax highlighting | `@codemirror/lang-xml` |
| Line numbers | `@codemirror/view` |
| Code folding + gutter | `@codemirror/language` |
| Search / replace panel | `@codemirror/search` |
| Autocomplete | `@codemirror/autocomplete` |
| History / undo / redo | `@codemirror/commands` |
| State management | `@codemirror/state` |
| Editor view + keymaps | `@codemirror/view` |

## Further reading

- [CodeMirror 6 reference manual](https://codemirror.net/docs/ref/)
- [CodeMirror 6 extensions guide](https://codemirror.net/docs/guide/)
- [CodeMirror 6 migration guide from v5](https://codemirror.net/docs/migration/)
