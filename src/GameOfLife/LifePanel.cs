using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using GameOfLife.Core;

namespace GameOfLife;

/// <summary>Owns rendering and mouse input for the grid, and nothing else:
/// the simulation itself lives in <see cref="SimulationController"/> and
/// the menus and controls live in <see cref="MainForm"/>. Double-buffered
/// with redraw on resize, so the grid does not flicker while the
/// simulation runs.</summary>
internal sealed class LifePanel : Panel
{
    private readonly Color _gridColor = Color.DarkGray;
    private bool _isPainting;
    private bool _paintValue;

    public LifePanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = SystemColors.Window;
        Paint += OnPanelPaint;
        MouseDown += OnPanelMouseDown;
        MouseMove += OnPanelMouseMove;
        MouseUp += OnPanelMouseUp;
    }

    /// <summary>The universe this panel draws. The form reassigns it when
    /// the universe object is replaced (Open, edge change); edits in
    /// place (step, resize, randomize) need no reassignment.</summary>
    /// <remarks>Runtime-only: set in code by MainForm, never by a
    /// designer, so the designer must not try to serialize it.</remarks>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Universe? Grid { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color CellColor { get; set; } = Color.Black;

    /// <summary>Raised after a mouse edit changes a cell, so the form can
    /// refresh the status bar. Repainting itself is already done here.</summary>
    public event EventHandler? GridChanged;

    private void OnPanelPaint(object? sender, PaintEventArgs e)
    {
        _ = sender;
        if (Grid is null)
        {
            return;
        }

        float cellWidth = (float)ClientSize.Width / Grid.Width;
        float cellHeight = (float)ClientSize.Height / Grid.Height;

        // Grid lines are invisible once cells shrink below a few pixels,
        // and drawing one per cell is what dominates painting on large
        // grids, so they are skipped there instead of drawn pointlessly.
        bool drawGrid = cellWidth >= 4 && cellHeight >= 4;

        using Pen gridPen = new(_gridColor, 1);
        using Brush cellBrush = new SolidBrush(CellColor);

        for (int y = 0; y < Grid.Height; y++)
        {
            for (int x = 0; x < Grid.Width; x++)
            {
                float left = x * cellWidth;
                float top = y * cellHeight;
                if (Grid[x, y])
                {
                    e.Graphics.FillRectangle(cellBrush, left, top, cellWidth, cellHeight);
                }

                if (drawGrid)
                {
                    e.Graphics.DrawRectangle(gridPen, left, top, cellWidth, cellHeight);
                }
            }
        }
    }

    private (int X, int Y)? CellAt(Point location)
    {
        if (Grid is null)
        {
            return null;
        }

        // Float division, not integer division: the panel size is rarely
        // an exact multiple of the grid size, and the bounds check below
        // is what keeps a click on the far edge from indexing past the
        // grid.
        float cellWidth = (float)ClientSize.Width / Grid.Width;
        float cellHeight = (float)ClientSize.Height / Grid.Height;
        if (cellWidth <= 0 || cellHeight <= 0)
        {
            return null;
        }

        int x = (int)(location.X / cellWidth);
        int y = (int)(location.Y / cellHeight);
        if (x < 0 || x >= Grid.Width || y < 0 || y >= Grid.Height)
        {
            return null;
        }

        return (x, y);
    }

    // The rectangle a single cell paints into, used so a mouse edit
    // repaints only that cell instead of the whole panel. The extra pixel
    // covers the grid line shared with the neighbouring cell.
    private Rectangle CellRect(int x, int y)
    {
        Universe? grid = Grid;
        if (grid is null)
        {
            return Rectangle.Empty;
        }

        float cellWidth = (float)ClientSize.Width / grid.Width;
        float cellHeight = (float)ClientSize.Height / grid.Height;
        return new Rectangle(
            (int)(x * cellWidth),
            (int)(y * cellHeight),
            (int)Math.Ceiling(cellWidth) + 1,
            (int)Math.Ceiling(cellHeight) + 1);
    }

    private void OnPanelMouseDown(object? sender, MouseEventArgs e)
    {
        _ = sender;
        if (e.Button != MouseButtons.Left || Grid is null)
        {
            return;
        }

        (int X, int Y)? cell = CellAt(e.Location);
        if (cell is null)
        {
            return;
        }

        // Remember the state the drag started with, so dragging paints
        // every cell it passes over the same way instead of toggling
        // each one back and forth as the mouse moves.
        _paintValue = Grid.Toggle(cell.Value.X, cell.Value.Y);
        _isPainting = true;
        Capture = true;
        Invalidate(CellRect(cell.Value.X, cell.Value.Y));
        GridChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPanelMouseMove(object? sender, MouseEventArgs e)
    {
        _ = sender;
        if (!_isPainting || e.Button != MouseButtons.Left || Grid is null)
        {
            return;
        }

        (int X, int Y)? cell = CellAt(e.Location);
        if (cell is null)
        {
            return;
        }

        if (Grid[cell.Value.X, cell.Value.Y] != _paintValue)
        {
            Grid[cell.Value.X, cell.Value.Y] = _paintValue;
            Invalidate(CellRect(cell.Value.X, cell.Value.Y));
            GridChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnPanelMouseUp(object? sender, MouseEventArgs e)
    {
        _ = sender;
        _ = e;
        _isPainting = false;
        Capture = false;
    }
}
