# Game of Life

[![CI](https://github.com/enonymous1/windows-game-of-life/actions/workflows/ci.yml/badge.svg)](https://github.com/enonymous1/windows-game-of-life/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)
![Windows](https://img.shields.io/badge/Windows-WinForms-0078D6)

Conway's Game of Life for Windows. Paint cells, press Start, and watch
generations unfold: a live cell with two or three live neighbours
survives, a dead cell with exactly three is born, and every other cell
dies or stays dead.

This is a modernization of a 2021 WinForms project (originally
.NET Framework 4.5.2). The simulation rules are unchanged; the
save/load code, edge handling, and project setup around them are not.
See [CHANGELOG.md](CHANGELOG.md) for what was fixed.

## Quick start

Prerequisite: the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
on Windows. (The app is WinForms, so it only runs on Windows. The
simulation core and its tests run anywhere .NET runs.)

```powershell
# Run the app
dotnet run --project src/GameOfLife

# Run the core tests
dotnet test tests/GameOfLife.Core.Tests
```

Or open `GameOfLife.sln` in Visual Studio 2022 and press F5.

## Using it

- **Click** a cell to toggle it. **Click and drag** to paint a run of
  cells in the same state.
- **Start** (F5) runs generations on a timer, **Stop** pauses,
  **Next** (F6) steps once.
- **New** clears the grid. **Randomize** fills it at random; type a
  whole-number seed first and the same pattern comes back every time.
- **Width / Height / Resize** changes the grid (cells that still fit
  are kept). **Interval** sets the timer in milliseconds.
- **Edges** picks the edge rule: **Finite** (outside the grid counts
  as dead) or **Toroidal** (the grid wraps, left to right and top to
  bottom).
- **Cell color** (Game menu) changes the live-cell colour. The picked
  colour actually applies now.
- **File > Open / Save / Save As** reads and writes `.cells` files.
  Two are included to try: `examples/glider.cells` and
  `examples/blinker.cells`.

The status bar shows the generation count, the number of live cells,
the grid size, and the edge rule.

## The `.cells` format

Plain text. Lines starting with `!` are comments and are ignored.
`O` is a live cell, `.` is a dead cell. Rows shorter than the widest
row are padded with dead cells. A loaded pattern starts at
generation 0; the file does not record the edge rule, so it opens
with whichever rule is currently selected.

```
!Glider
.O.
..O
OOO
```

## Project layout

```
src/GameOfLife.Core/     Simulation only, no UI: Universe, BoundaryMode,
                         CellsFormat (.cells read/write). Unit tested.
src/GameOfLife/          WinForms app: MainForm, LifePanel, Program.
                         Built in code, no designer files or .resx.
tests/GameOfLife.Core.Tests/
                         xUnit tests for the rules, both edge modes,
                         .cells round-trips, and the bug fixes.
examples/                Sample .cells patterns.
```

`GameOfLife.Core` has no reference to `System.Windows.Forms`, so the
rules can be tested headlessly and reused by any other front end.

## Building and testing

```powershell
# Whole solution (Windows only, because of the WinForms app)
dotnet build GameOfLife.sln --configuration Release
dotnet test GameOfLife.sln --configuration Release

# Core tests only (any platform)
dotnet test tests/GameOfLife.Core.Tests --configuration Release
```

CI builds and tests the full solution on both Ubuntu and Windows
(the app project sets `EnableWindowsTargeting`, so it compiles on
Ubuntu even though it only runs on Windows), plus a format check.
See `.github/workflows/ci.yml`.

## License

MIT. See [LICENSE](LICENSE).
