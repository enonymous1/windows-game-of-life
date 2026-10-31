# Changelog

## 2.0.0 — 2026-10-29

Modernization of the 2021 WinForms project. The Game of Life rules
themselves are unchanged; everything around them was rebuilt.

- **.NET 10, SDK-style projects**, one solution, shared build
  properties. The old `.NET Framework 4.5.2` project, `App.config`,
  `AssemblyInfo.cs`, designer files, and `.resx` resources are gone.
  The UI is built in code (`MainForm.cs`), so there are no binary
  resources to keep in sync.
- **Split into two projects:** `GameOfLife.Core` (grid, rules, edge
  modes, `.cells` read/write — all unit-testable, no UI code) and
  `GameOfLife` (WinForms UI only, Windows-only as WinForms requires).
- **Bugs fixed:**
  - Save As wrote only a comment line and lost the grid. Saving now
    writes every row, and Save (menu and toolbar) works, remembering
    the current file.
  - Open read the file but changed nothing. Opening now resizes the
    grid to the file and loads its cells, and reports unreadable or
    malformed files instead of silently doing nothing.
  - The Color dialog's result was discarded. The chosen colour now
    applies to live cells.
  - The toroidal neighbour counter was dead code and wrong (it
    mutated the grid dimensions instead of wrapping coordinates).
    Toroidal mode now wraps correctly with modular arithmetic, is
    selectable from the menu and the Edges control, and is tested.
  - The next-generation step reused a scratch buffer without
    clearing it, so a dead cell could keep the previous generation's
    value. Each generation is now computed into a fresh grid.
  - Clicking the far edge of the panel could index past the grid.
    Clicks and drags are bounds-checked, and dragging paints cells.
  - The dead template menu items (Print, Print Preview, Tools,
    Help contents/index/search) and the unwired toolbar Open/Save
    buttons are gone. Every remaining menu and toolbar item works.
  - The duplicated New-clear code (menu and toolbar handlers) is
    one path now.
- **New controls:** grid width/height with Resize, timer interval,
  edge-mode selector, and a seed box for reproducible random fills
  (blank means random). The status bar now also shows live-cell
  count, grid size, and edge rule.
- **Tests:** xUnit coverage for still lifes, oscillators, gliders,
  under/overpopulation, birth, both edge modes, `.cells` round-trips
  and malformed files, resizing, seeding, and the scratch-buffer
  regression above.
- **Repo hygiene:** `.gitignore`, MIT LICENSE, GitHub Actions CI
  (Ubuntu + Windows matrix: full solution build, tests, and format
  check on both), and a rewritten README.
- **Unchanged on purpose:** the B3/S23 rules, the 20x20 default
  grid, the 100 ms default interval, click-to-toggle, the
  generations counter, and the `.cells` `O`/`.` format, so the
  program still feels like the original.
- **Review fixes (2026-10-31, pre-release):** the Open crash for
  patterns outside the 5–100 size range is fixed (spinners accept 1
  and widen to fit a loaded file); `MainForm` no longer owns the
  timer, file path, or painting (a new `SimulationController` owns
  the simulation and file, and `LifePanel` owns rendering and mouse
  input); grid size, interval, and edge rule persist between runs;
  unhandled UI-thread exceptions show a message instead of crashing
  outright.
- **Review fixes, round 2 (2026-10-31, pre-release):** painting skips
  grid lines for tiny cells and repaints only the changed cell on
  drags; `AliveCount` is maintained incrementally instead of
  rescanned; `.cells` files over 1,000,000 cells are rejected; the
  SDK version lives in `global.json` (CI reads it, no version in the
  workflow); `.gitattributes` normalizes line endings; the README is
  rewritten with Description, Prerequisites, Installation, Local
  Development, and Deployment sections, with screenshot and GIF slots
  left commented until real Windows captures exist.
- **Tag-triggered releases (2026-10-31, pre-release):**
  `.github/workflows/release.yml` runs on `v*` tags: it builds,
  tests, publishes a framework-dependent win-x64 build, and attaches
  `game-of-life-win-x64.zip` plus a SHA-256 checksum to the GitHub
  release.
- **CI line-ending fix (2026-10-31, pre-release):** `.gitattributes`
  now pins `eol=lf` on checkout. Without it, a Windows checkout
  converted text files to CRLF, and since `.editorconfig` demands
  LF, the CI format check failed on windows-latest (ENDOFLINE)
  while ubuntu-latest stayed green. Build and tests passed on both
  throughout; only the format step was affected.
