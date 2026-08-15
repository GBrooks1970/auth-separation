# Vendored libraries

`auth-separation_implementation-kanban_v1.html` is the plan of record for all 51 implementation tickets.
It previously loaded React, ReactDOM and Babel from `cdnjs.cloudflare.com`, which meant it rendered a blank
page with no network — while the README advertised it as self-contained. These copies fix that (`AS-02`).

| File | Package | Version | Source path in package |
|---|---|---|---|
| `react-18.2.0.production.min.js` | `react` | 18.2.0 | `umd/react.production.min.js` |
| `react-dom-18.2.0.production.min.js` | `react-dom` | 18.2.0 | `umd/react-dom.production.min.js` |
| `babel-standalone-7.23.9.min.js` | `@babel/standalone` | 7.23.9 | `babel.min.js` |

Obtained with `npm pack <package>@<version>` and extracted from the published tarball, so the provenance is
the npm registry rather than a CDN mirror. The versions are exactly those the file referenced before —
this change makes the board work offline, it does not upgrade anything.

## Why Babel is here at all

The board's UI is written as inline JSX inside the HTML, so `@babel/standalone` transforms it in the
browser at load time. It is 2.8 MB, which is most of this directory's weight.

That cost was accepted deliberately. The alternative — pre-compiling the JSX and dropping Babel — saves the
2.8 MB but introduces a build step to a file whose entire value is that you can open it from disk and it
works, and it puts compiled output where readable, editable JSX is today. If repository weight ever becomes
a real concern, pre-compiling remains available; nothing here forecloses it.

## Rules

- **Do not repoint the script tags at a CDN.** Offline rendering is the point.
- These files are third-party code, unmodified. Do not edit them.
- To change a version, re-run `npm pack` at the new version, replace the file, update this table, and
  confirm the board still renders with the network disabled.
