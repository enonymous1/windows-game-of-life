using System.Drawing;
using System.Windows.Forms;
using GameOfLife.Core;

namespace GameOfLife;

/// <summary>The main window. All simulation state lives in
/// <see cref="Universe"/>; this form only presents it, so every menu
/// and toolbar item is wired to a real action. The user interface is
/// built in code rather than a designer file, so the project has no
/// binary resources to keep in sync.</summary>
public sealed class MainForm : Form
{
    private Universe _universe = new(20, 20);
    private readonly System.Windows.Forms.Timer _timer = new();
    private Color _cellColor = Color.Black;
    private readonly Color _gridColor = Color.DarkGray;
    private string? _currentFile;
    private bool _isPainting;
    private bool _paintValue;

    private readonly LifePanel _lifePanel = new();
    private readonly ToolStripMenuItem _startMenuItem = new("&Start");
    private readonly ToolStripMenuItem _stopMenuItem = new("Sto&p");
    private readonly ToolStripMenuItem _finiteMenuItem = new("&Finite edges");
    private readonly ToolStripMenuItem _toroidalMenuItem = new("&Toroidal edges");
    private readonly ToolStripButton _startButton = new("Start");
    private readonly ToolStripButton _stopButton = new("Stop");
    private readonly ToolStripStatusLabel _statusGenerations = new();
    private readonly ToolStripStatusLabel _statusAlive = new();
    private readonly ToolStripStatusLabel _statusSize = new();
    private readonly ToolStripStatusLabel _statusEdges = new();
    private readonly NumericUpDown _widthUpDown = new();
    private readonly NumericUpDown _heightUpDown = new();
    private readonly NumericUpDown _intervalUpDown = new();
    private readonly ComboBox _edgesComboBox = new();
    private readonly TextBox _seedTextBox = new();

    public MainForm()
    {
        Text = "Game of Life";
        ClientSize = new Size(720, 520);
        MinimumSize = new Size(560, 420);
        StartPosition = FormStartPosition.CenterScreen;

        BuildMenu();
        BuildToolStrip();
        BuildControlPanel();
        BuildStatusStrip();

        _lifePanel.Dock = DockStyle.Fill;
        _lifePanel.Paint += OnLifePanelPaint;
        _lifePanel.MouseDown += OnLifePanelMouseDown;
        _lifePanel.MouseMove += OnLifePanelMouseMove;
        _lifePanel.MouseUp += OnLifePanelMouseUp;

        // Dock layout follows the original form: the fill panel first,
        // then the bottom strip, then the top strips in reverse order.
        Controls.Add(_lifePanel);
        Controls.Add(_statusStrip);
        Controls.Add(_controlPanel);
        Controls.Add(_toolStrip);
        Controls.Add(_menuStrip);

        _timer.Interval = 100;
        _timer.Tick += OnTimerTick;

        UpdateStatus();
        UpdateRunningState();
        SyncBoundaryControls();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }

    private MenuStrip _menuStrip = null!;
    private ToolStrip _toolStrip = null!;
    private FlowLayoutPanel _controlPanel = null!;
    private StatusStrip _statusStrip = null!;

    private void BuildMenu()
    {
        var newItem = new ToolStripMenuItem("&New", null, OnNew)
        {
            ShortcutKeys = Keys.Control | Keys.N,
        };
        var openItem = new ToolStripMenuItem("&Open...", null, OnOpen)
        {
            ShortcutKeys = Keys.Control | Keys.O,
        };
        var saveItem = new ToolStripMenuItem("&Save", null, OnSave)
        {
            ShortcutKeys = Keys.Control | Keys.S,
        };
        var saveAsItem = new ToolStripMenuItem("Save &As...", null, OnSaveAs);
        var exitItem = new ToolStripMenuItem("E&xit", null, OnExit);

        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            newItem, openItem, saveItem, saveAsItem,
            new ToolStripSeparator(), exitItem,
        });

        _startMenuItem.ShortcutKeys = Keys.F5;
        _startMenuItem.Click += OnStart;
        _stopMenuItem.Click += OnStop;
        var nextItem = new ToolStripMenuItem("&Next generation", null, OnNext)
        {
            ShortcutKeys = Keys.F6,
        };
        var randomizeItem = new ToolStripMenuItem("&Randomize", null, OnRandomize)
        {
            ShortcutKeys = Keys.Control | Keys.R,
        };
        _finiteMenuItem.Click += OnFiniteEdges;
        _toroidalMenuItem.Click += OnToroidalEdges;
        var colorItem = new ToolStripMenuItem("&Cell color...", null, OnPickColor);

        var gameMenu = new ToolStripMenuItem("&Game");
        gameMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            _startMenuItem, _stopMenuItem, nextItem,
            new ToolStripSeparator(), randomizeItem,
            new ToolStripSeparator(), _finiteMenuItem, _toroidalMenuItem,
            new ToolStripSeparator(), colorItem,
        });

        var aboutItem = new ToolStripMenuItem("&About...", null, OnAbout);
        var helpMenu = new ToolStripMenuItem("&Help");
        helpMenu.DropDownItems.Add(aboutItem);

        _menuStrip = new MenuStrip();
        _menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, gameMenu, helpMenu });
        _menuStrip.Dock = DockStyle.Top;
    }

    private void BuildToolStrip()
    {
        var newButton = new ToolStripButton("New", null, OnNew)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        var openButton = new ToolStripButton("Open", null, OnOpen)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        var saveButton = new ToolStripButton("Save", null, OnSave)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        _startButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _startButton.Click += OnStart;
        _stopButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _stopButton.Click += OnStop;
        var nextButton = new ToolStripButton("Next", null, OnNext)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };

        _toolStrip = new ToolStrip();
        _toolStrip.Items.AddRange(new ToolStripItem[]
        {
            newButton, openButton, saveButton,
            new ToolStripSeparator(), _startButton, _stopButton, nextButton,
        });
        _toolStrip.Dock = DockStyle.Top;
    }

    private void BuildControlPanel()
    {
        _widthUpDown.Minimum = 5;
        _widthUpDown.Maximum = 100;
        _widthUpDown.Value = _universe.Width;
        _widthUpDown.Width = 56;
        _heightUpDown.Minimum = 5;
        _heightUpDown.Maximum = 100;
        _heightUpDown.Value = _universe.Height;
        _heightUpDown.Width = 56;
        _intervalUpDown.Minimum = 10;
        _intervalUpDown.Maximum = 2000;
        _intervalUpDown.Increment = 10;
        _intervalUpDown.Value = 100;
        _intervalUpDown.Width = 64;
        _intervalUpDown.ValueChanged += OnIntervalChanged;

        _edgesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _edgesComboBox.Items.AddRange(new object[] { "Finite edges", "Toroidal edges" });
        _edgesComboBox.SelectedIndex = 0;
        _edgesComboBox.SelectedIndexChanged += OnEdgesComboBoxChanged;

        _seedTextBox.Width = 80;

        var resizeButton = new Button { Text = "Resize", AutoSize = true };
        resizeButton.Click += OnResizeUniverse;
        var randomizeButton = new Button { Text = "Randomize", AutoSize = true };
        randomizeButton.Click += OnRandomize;

        _controlPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Padding = new Padding(6, 4, 6, 4),
        };
        _controlPanel.Controls.AddRange(new Control[]
        {
            new Label { Text = "Width:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft },
            _widthUpDown,
            new Label { Text = "Height:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft },
            _heightUpDown,
            resizeButton,
            new Label { Text = "Interval (ms):", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft },
            _intervalUpDown,
            new Label { Text = "Edges:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft },
            _edgesComboBox,
            new Label { Text = "Seed (blank = random):", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft },
            _seedTextBox,
            randomizeButton,
        });
    }

    private void BuildStatusStrip()
    {
        _statusStrip = new StatusStrip();
        _statusStrip.Items.AddRange(new ToolStripItem[]
        {
            _statusGenerations, _statusAlive, _statusSize, _statusEdges,
        });
        _statusStrip.Dock = DockStyle.Bottom;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _ = sender;
        Step();
    }

    private void Step()
    {
        _universe.Step();
        UpdateStatus();
        _lifePanel.Invalidate();
    }

    private void OnStart(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _timer.Start();
        UpdateRunningState();
    }

    private void OnStop(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _timer.Stop();
        UpdateRunningState();
    }

    private void OnNext(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        Step();
    }

    private void OnNew(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _timer.Stop();
        _universe.Clear();
        _currentFile = null;
        UpdateTitle();
        UpdateStatus();
        UpdateRunningState();
        _lifePanel.Invalidate();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        Close();
    }

    private void OnOpen(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        using var dialog = new OpenFileDialog
        {
            Filter = "Cells files (*.cells)|*.cells|All files (*.*)|*.*",
            FilterIndex = 1,
            DefaultExt = "cells",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            using var reader = new StreamReader(dialog.FileName);
            Universe loaded = CellsFormat.Load(reader, _universe.Boundary);
            _timer.Stop();
            _universe = loaded;
            _currentFile = dialog.FileName;
            _widthUpDown.Value = _universe.Width;
            _heightUpDown.Value = _universe.Height;
            UpdateTitle();
            UpdateStatus();
            UpdateRunningState();
            _lifePanel.Invalidate();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, $"Could not open that file:\n{ex.Message}", "Open failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        if (_currentFile is null)
        {
            OnSaveAs(sender, e);
            return;
        }

        SaveToFile(_currentFile);
    }

    private void OnSaveAs(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        using var dialog = new SaveFileDialog
        {
            Filter = "Cells files (*.cells)|*.cells|All files (*.*)|*.*",
            FilterIndex = 1,
            DefaultExt = "cells",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        SaveToFile(dialog.FileName);
    }

    private void SaveToFile(string path)
    {
        try
        {
            using var writer = new StreamWriter(path);
            CellsFormat.Save(_universe, writer);
            _currentFile = path;
            UpdateTitle();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Could not save that file:\n{ex.Message}", "Save failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnResizeUniverse(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _universe.Resize((int)_widthUpDown.Value, (int)_heightUpDown.Value);
        UpdateStatus();
        _lifePanel.Invalidate();
    }

    private void OnIntervalChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _timer.Interval = (int)_intervalUpDown.Value;
    }

    private void OnFiniteEdges(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ApplyBoundary(BoundaryMode.Finite);
    }

    private void OnToroidalEdges(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ApplyBoundary(BoundaryMode.Toroidal);
    }

    private void OnEdgesComboBoxChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        var selected = (BoundaryMode)_edgesComboBox.SelectedIndex;
        if (selected != _universe.Boundary)
        {
            ApplyBoundary(selected);
        }
    }

    private void ApplyBoundary(BoundaryMode boundary)
    {
        if (_universe.Boundary == boundary)
        {
            SyncBoundaryControls();
            return;
        }

        _universe = _universe.WithBoundary(boundary);
        SyncBoundaryControls();
        UpdateStatus();
        _lifePanel.Invalidate();
    }

    private void SyncBoundaryControls()
    {
        _finiteMenuItem.Checked = _universe.Boundary == BoundaryMode.Finite;
        _toroidalMenuItem.Checked = _universe.Boundary == BoundaryMode.Toroidal;
        _edgesComboBox.SelectedIndex = (int)_universe.Boundary;
    }

    private void OnRandomize(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        int? seed = null;
        string seedText = _seedTextBox.Text.Trim();
        if (seedText.Length > 0)
        {
            if (!int.TryParse(seedText, out int parsedSeed))
            {
                MessageBox.Show(this, "The seed must be a whole number, or blank for a random pattern.",
                    "Invalid seed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            seed = parsedSeed;
        }

        _universe.Randomize(seed);
        UpdateStatus();
        _lifePanel.Invalidate();
    }

    private void OnPickColor(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        using var dialog = new ColorDialog { Color = _cellColor };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _cellColor = dialog.Color;
            _lifePanel.Invalidate();
        }
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        MessageBox.Show(this,
            "Conway's Game of Life.\n\n" +
            "A live cell with two or three live neighbours survives, a dead cell " +
            "with exactly three live neighbours is born, and all other cells die.\n\n" +
            "Click or drag to paint cells. Save and open .cells files from the File menu.",
            "About Game of Life", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnLifePanelPaint(object? sender, PaintEventArgs e)
    {
        _ = sender;
        float cellWidth = (float)_lifePanel.ClientSize.Width / _universe.Width;
        float cellHeight = (float)_lifePanel.ClientSize.Height / _universe.Height;

        using Pen gridPen = new(_gridColor, 1);
        using Brush cellBrush = new SolidBrush(_cellColor);

        for (int y = 0; y < _universe.Height; y++)
        {
            for (int x = 0; x < _universe.Width; x++)
            {
                float left = x * cellWidth;
                float top = y * cellHeight;
                if (_universe[x, y])
                {
                    e.Graphics.FillRectangle(cellBrush, left, top, cellWidth, cellHeight);
                }

                e.Graphics.DrawRectangle(gridPen, left, top, cellWidth, cellHeight);
            }
        }
    }

    private (int X, int Y)? CellAt(Point location)
    {
        float cellWidth = (float)_lifePanel.ClientSize.Width / _universe.Width;
        float cellHeight = (float)_lifePanel.ClientSize.Height / _universe.Height;
        if (cellWidth <= 0 || cellHeight <= 0)
        {
            return null;
        }

        int x = (int)(location.X / cellWidth);
        int y = (int)(location.Y / cellHeight);
        if (x < 0 || x >= _universe.Width || y < 0 || y >= _universe.Height)
        {
            return null;
        }

        return (x, y);
    }

    private void OnLifePanelMouseDown(object? sender, MouseEventArgs e)
    {
        _ = sender;
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        (int X, int Y)? cell = CellAt(e.Location);
        if (cell is null)
        {
            return;
        }

        _paintValue = _universe.Toggle(cell.Value.X, cell.Value.Y);
        _isPainting = true;
        _lifePanel.Capture = true;
        _lifePanel.Invalidate();
    }

    private void OnLifePanelMouseMove(object? sender, MouseEventArgs e)
    {
        _ = sender;
        if (!_isPainting || e.Button != MouseButtons.Left)
        {
            return;
        }

        (int X, int Y)? cell = CellAt(e.Location);
        if (cell is null)
        {
            return;
        }

        if (_universe[cell.Value.X, cell.Value.Y] != _paintValue)
        {
            _universe[cell.Value.X, cell.Value.Y] = _paintValue;
            _lifePanel.Invalidate();
        }
    }

    private void OnLifePanelMouseUp(object? sender, MouseEventArgs e)
    {
        _ = sender;
        _ = e;
        _isPainting = false;
        _lifePanel.Capture = false;
    }

    private void UpdateRunningState()
    {
        _startMenuItem.Enabled = !_timer.Enabled;
        _startButton.Enabled = !_timer.Enabled;
        _stopMenuItem.Enabled = _timer.Enabled;
        _stopButton.Enabled = _timer.Enabled;
    }

    private void UpdateStatus()
    {
        _statusGenerations.Text = $"Generations = {_universe.Generation}";
        _statusAlive.Text = $"Alive = {_universe.AliveCount}";
        _statusSize.Text = $"Size = {_universe.Width} x {_universe.Height}";
        _statusEdges.Text = _universe.Boundary == BoundaryMode.Finite ? "Edges = Finite" : "Edges = Toroidal";
    }

    private void UpdateTitle()
    {
        Text = _currentFile is null
            ? "Game of Life"
            : $"Game of Life - {Path.GetFileName(_currentFile)}";
    }
}
