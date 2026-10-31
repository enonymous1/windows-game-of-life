using System.Drawing;
using System.Windows.Forms;
using GameOfLife.Core;

namespace GameOfLife;

/// <summary>The main window. Simulation state lives in
/// <see cref="SimulationController"/>, rendering and mouse input live in
/// <see cref="LifePanel"/>, and persisted choices live in
/// <see cref="GameSettings"/>; this form only builds the menus and
/// controls and wires the three together, so every menu and toolbar item
/// is wired to a real action. The user interface is built in code rather
/// than a designer file, so the project has no binary resources to keep
/// in sync.</summary>
public sealed class MainForm : Form
{
    private readonly SimulationController _controller;
    private readonly LifePanel _lifePanel = new();
    private readonly ToolStripMenuItem _startMenuItem = new("&Start");
    private readonly ToolStripMenuItem _stopMenuItem = new("S&top");
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
    private MenuStrip _menuStrip = null!;
    private ToolStrip _toolStrip = null!;
    private FlowLayoutPanel _controlPanel = null!;
    private StatusStrip _statusStrip = null!;

    private const string CellsFilter = "Cells files (*.cells)|*.cells|All files (*.*)|*.*";

    public MainForm()
    {
        GameSettings settings = GameSettings.Load();
        _controller = new SimulationController(
            settings.Width, settings.Height, settings.Boundary, settings.IntervalMs);

        Text = "Game of Life";
        ClientSize = new Size(720, 520);
        MinimumSize = new Size(560, 420);
        StartPosition = FormStartPosition.CenterScreen;

        BuildMenu();
        BuildToolStrip();
        BuildControlPanel();
        BuildStatusStrip();

        _lifePanel.Grid = _controller.Universe;
        _lifePanel.Dock = DockStyle.Fill;
        _lifePanel.GridChanged += (sender, e) =>
        {
            _ = sender;
            _ = e;
            UpdateStatus();
        };

        // The controller owns the simulation; the form only refreshes
        // the view when the simulation reports a change.
        _controller.Changed += (sender, e) =>
        {
            _ = sender;
            _ = e;
            _lifePanel.Grid = _controller.Universe;
            UpdateStatus();
            _lifePanel.Invalidate();
        };

        // Dock layout follows the original form: the fill panel first,
        // then the bottom strip, then the top strips in reverse order.
        Controls.Add(_lifePanel);
        Controls.Add(_statusStrip);
        Controls.Add(_controlPanel);
        Controls.Add(_toolStrip);
        Controls.Add(_menuStrip);

        FormClosing += SaveSettingsOnClosing;

        UpdateStatus();
        UpdateRunningState();
        SyncBoundaryControls();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _controller.Dispose();
        }

        base.Dispose(disposing);
    }

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
        _widthUpDown.Minimum = 1;
        _widthUpDown.Maximum = Math.Max(100, _controller.Universe.Width);
        _widthUpDown.Value = _controller.Universe.Width;
        _widthUpDown.Width = 56;
        _heightUpDown.Minimum = 1;
        _heightUpDown.Maximum = Math.Max(100, _controller.Universe.Height);
        _heightUpDown.Value = _controller.Universe.Height;
        _heightUpDown.Width = 56;
        _intervalUpDown.Minimum = 10;
        _intervalUpDown.Maximum = 2000;
        _intervalUpDown.Increment = 10;
        _intervalUpDown.Value = _controller.Interval;
        _intervalUpDown.Width = 64;
        _intervalUpDown.ValueChanged += OnIntervalChanged;

        _edgesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _edgesComboBox.Items.AddRange(new object[] { "Finite edges", "Toroidal edges" });
        _edgesComboBox.SelectedIndex = (int)_controller.Universe.Boundary;
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

    private void OnStart(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _controller.Start();
        UpdateRunningState();
    }

    private void OnStop(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _controller.Stop();
        UpdateRunningState();
    }

    private void OnNext(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _controller.Step();
    }

    private void OnNew(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _controller.Clear();
        UpdateTitle();
        UpdateRunningState();
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
            Filter = CellsFilter,
            FilterIndex = 1,
            DefaultExt = "cells",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _controller.Load(dialog.FileName);
            SyncSizeControls();
            UpdateTitle();
            UpdateRunningState();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentOutOfRangeException)
        {
            MessageBox.Show(this, $"Could not open that file:\n{ex.Message}", "Open failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        if (_controller.CurrentFile is null)
        {
            OnSaveAs(sender, e);
            return;
        }

        SaveToFile(_controller.CurrentFile);
    }

    private void OnSaveAs(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        using var dialog = new SaveFileDialog
        {
            Filter = CellsFilter,
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
            _controller.Save(path);
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
        _controller.Resize((int)_widthUpDown.Value, (int)_heightUpDown.Value);
    }

    private void OnIntervalChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        _controller.Interval = (int)_intervalUpDown.Value;
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
        // Setting the combo inside SyncBoundaryControls fires this
        // handler. The controller ignores a request that changes nothing,
        // and Sync is idempotent, so the round trip terminates instead
        // of looping.
        ApplyBoundary((BoundaryMode)_edgesComboBox.SelectedIndex);
    }

    private void ApplyBoundary(BoundaryMode boundary)
    {
        _controller.ApplyBoundary(boundary);
        SyncBoundaryControls();
    }

    private void SyncBoundaryControls()
    {
        _finiteMenuItem.Checked = _controller.Universe.Boundary == BoundaryMode.Finite;
        _toroidalMenuItem.Checked = _controller.Universe.Boundary == BoundaryMode.Toroidal;
        _edgesComboBox.SelectedIndex = (int)_controller.Universe.Boundary;
    }

    // The size controls are input, not display: they are synced only
    // after Open (and at startup), never on every controller change, or
    // typing a new size while the simulation runs would be overwritten
    // by the next generation. The range is widened before the value is
    // assigned, because assigning a NumericUpDown value outside its
    // range throws.
    private void SyncSizeControls()
    {
        _widthUpDown.Maximum = Math.Max(_widthUpDown.Maximum, _controller.Universe.Width);
        _heightUpDown.Maximum = Math.Max(_heightUpDown.Maximum, _controller.Universe.Height);
        _widthUpDown.Value = _controller.Universe.Width;
        _heightUpDown.Value = _controller.Universe.Height;
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

        _controller.Randomize(seed);
    }

    private void OnPickColor(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        using var dialog = new ColorDialog { Color = _lifePanel.CellColor };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _lifePanel.CellColor = dialog.Color;
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

    private void UpdateRunningState()
    {
        _startMenuItem.Enabled = !_controller.IsRunning;
        _startButton.Enabled = !_controller.IsRunning;
        _stopMenuItem.Enabled = _controller.IsRunning;
        _stopButton.Enabled = _controller.IsRunning;
    }

    private void UpdateStatus()
    {
        _statusGenerations.Text = $"Generations = {_controller.Universe.Generation}";
        _statusAlive.Text = $"Alive = {_controller.Universe.AliveCount}";
        _statusSize.Text = $"Size = {_controller.Universe.Width} x {_controller.Universe.Height}";
        _statusEdges.Text = _controller.Universe.Boundary == BoundaryMode.Finite ? "Edges = Finite" : "Edges = Toroidal";
    }

    private void UpdateTitle()
    {
        Text = _controller.CurrentFile is null
            ? "Game of Life"
            : $"Game of Life - {Path.GetFileName(_controller.CurrentFile)}";
    }

    private void SaveSettingsOnClosing(object? sender, FormClosingEventArgs e)
    {
        _ = sender;
        _ = e;
        GameSettings.Save(new GameSettings(
            _controller.Universe.Width,
            _controller.Universe.Height,
            _controller.Interval,
            _controller.Universe.Boundary));
    }
}
