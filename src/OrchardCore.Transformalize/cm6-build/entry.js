import { EditorState } from '@codemirror/state';
import {
  EditorView,
  keymap,
  lineNumbers,
  drawSelection,
  highlightActiveLine,
  highlightActiveLineGutter,
} from '@codemirror/view';
import {
  defaultKeymap,
  history,
  historyKeymap,
  indentWithTab,
} from '@codemirror/commands';
import { xml } from '@codemirror/lang-xml';
import {
  foldGutter,
  foldKeymap,
  indentOnInput,
  syntaxHighlighting,
  defaultHighlightStyle,
  bracketMatching,
} from '@codemirror/language';
import {
  search,
  searchKeymap,
  highlightSelectionMatches,
} from '@codemirror/search';
import {
  autocompletion,
  completionKeymap,
  closeBrackets,
  closeBracketsKeymap,
  completeAnyWord,
} from '@codemirror/autocomplete';

// ── styles (injected once) ──────────────────────────────────────────────────

(function injectStyles() {
  if (document.getElementById('cm6-styles')) return;
  const style = document.createElement('style');
  style.id = 'cm6-styles';
  style.textContent = `
    .cm-editor { position: relative; }
    .cm-editor:fullscreen,
    .cm-editor:-webkit-full-screen {
      height: 100vh !important;
      background: var(--bs-body-bg, #fff);
    }
    .cm-editor:fullscreen .cm-scroller,
    .cm-editor:-webkit-full-screen .cm-scroller {
      height: 100vh !important;
      overflow: auto !important;
    }
    .cm-fullscreen-btn {
      position: absolute; top: 4px; right: 4px; z-index: 20;
      padding: 2px 6px; font-size: 14px; line-height: 1; cursor: pointer;
      background: rgba(255,255,255,0.7); border: 1px solid #ccc;
      border-radius: 3px; color: #333;
    }
    .cm-fullscreen-btn:hover { background: rgba(255,255,255,0.95); }
  `;
  document.head.appendChild(style);
}());

// ── smart tab: indent selection or insert spaces ────────────────────────────

function smartTab(view) {
  const { state } = view;
  if (state.selection.ranges.some(r => !r.empty)) {
    return indentWithTab.run(view);
  }
  const spaces = ' '.repeat(state.tabSize);
  view.dispatch(state.replaceSelection(spaces));
  return true;
}

// ── public API ──────────────────────────────────────────────────────────────

window.TransformalizeCodeMirror = {
  init(textAreaId, portion) {
    const textArea = document.getElementById(textAreaId);
    if (!textArea) return null;

    const height = Math.round(window.innerHeight * portion);

    const customKeymap = keymap.of([
      {
        key: 'Mod-s',
        run(view) {
          const form = view.dom.closest('form');
          if (form) {
            const submit = form.querySelector('[type="submit"]');
            if (submit) submit.click();
          }
          return true;
        },
        preventDefault: true,
      },
      {
        key: 'Tab',
        run: smartTab,
      },
    ]);

    const heightTheme = EditorView.theme({
      '&': { height: height + 'px' },
      '.cm-scroller': { overflow: 'auto' },
    });

    const view = new EditorView({
      state: EditorState.create({
        doc: textArea.value,
        extensions: [
          // language
          xml(),
          syntaxHighlighting(defaultHighlightStyle, { fallback: true }),
          bracketMatching(),

          // editing helpers
          history(),
          drawSelection(),
          indentOnInput(),
          closeBrackets(),

          // display
          lineNumbers(),
          highlightActiveLineGutter(),
          foldGutter(),
          highlightActiveLine(),
          highlightSelectionMatches(),
          EditorView.lineWrapping,
          heightTheme,

          // search panel (Ctrl-F / Cmd-F to open)
          search({ top: false }),

          // autocomplete — any-word source + XML completions from lang-xml
          autocompletion({
            override: [completeAnyWord],
            activateOnTyping: true,
          }),

          // sync textarea on every change (keeps validators like Parsley happy)
          EditorView.updateListener.of(update => {
            if (update.docChanged) {
              textArea.value = update.state.doc.toString();
              textArea.dispatchEvent(new Event('input', { bubbles: true }));
            }
          }),

          // keymaps — custom first so they shadow conflicting defaults
          customKeymap,
          keymap.of([
            ...closeBracketsKeymap,
            ...defaultKeymap,
            ...historyKeymap,
            ...foldKeymap,
            ...searchKeymap,
            ...completionKeymap,
          ]),
        ],
      }),
      parent: textArea.parentNode,
    });

    // insert editor after textarea, hide textarea
    textArea.parentNode.insertBefore(view.dom, textArea.nextSibling);
    textArea.style.display = 'none';

    // fullscreen button — uses native browser Fullscreen API
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'cm-fullscreen-btn';
    btn.textContent = '⛶';
    btn.title = 'Toggle fullscreen (Esc to exit)';
    btn.addEventListener('click', () => {
      if (!document.fullscreenElement) {
        view.dom.requestFullscreen().catch(err => console.warn('Fullscreen:', err));
      } else {
        document.exitFullscreen();
      }
    });
    // keep button text in sync when fullscreen changes (including Esc)
    document.addEventListener('fullscreenchange', () => {
      btn.textContent = document.fullscreenElement === view.dom ? '✕' : '⛶';
      view.requestMeasure();
    });
    view.dom.appendChild(btn);

    return view;
  },
};
