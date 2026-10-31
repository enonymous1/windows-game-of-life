using GameOfLife.Core;

namespace GameOfLife;

/// <summary>Owns the simulation: the universe, the timer, and the current
/// file. The form builds controls and the panel draws; neither of them
/// owns the timer or the file path anymore, so simulation state has one
/// home and one change notification.</summary>
internal sealed class SimulationController : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer = new();
    private Universe _universe;

    public SimulationController(int width, int height, BoundaryMode boundary, int intervalMs)
    {
        if (intervalMs < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalMs), "Interval must be at least 1 ms.");
        }

        _universe = new Universe(width, height, boundary);
        _timer.Interval = intervalMs;
        _timer.Tick += OnTimerTick;
    }

    public Universe Universe => _universe;

    public string? CurrentFile { get; private set; }

    public bool IsRunning => _timer.Enabled;

    public int Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    /// <summary>Raised after any change to the grid: a step, a resize,
    /// a boundary change, a random fill, a clear, or a load. Saving does
    /// not raise it, because saving changes no cell.</summary>
    public event EventHandler? Changed;

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public void Step()
    {
        _universe.Step();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _timer.Stop();
        _universe.Clear();
        CurrentFile = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Resize(int width, int height)
    {
        _universe.Resize(width, height);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyBoundary(BoundaryMode boundary)
    {
        if (_universe.Boundary == boundary)
        {
            return;
        }

        _universe = _universe.WithBoundary(boundary);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Randomize(int? seed)
    {
        _universe.Randomize(seed);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Loads a .cells file, replacing the universe. The timer is
    /// stopped only after the file reads successfully, so a failed load
    /// leaves the running simulation untouched.</summary>
    public void Load(string path)
    {
        using var reader = new StreamReader(path);
        Universe loaded = CellsFormat.Load(reader, _universe.Boundary);
        _timer.Stop();
        _universe = loaded;
        CurrentFile = path;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Save(string path)
    {
        using var writer = new StreamWriter(path);
        CellsFormat.Save(_universe, writer);
        CurrentFile = path;
    }

    public void Dispose() => _timer.Dispose();

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        Step();
    }
}
