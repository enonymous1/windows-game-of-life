namespace GameOfLife.Core;

/// <summary>Conway's Game of Life on a rectangular grid: a live cell with
/// two or three live neighbours survives, a dead cell with exactly three
/// live neighbours is born, and every other cell dies or stays dead.
/// No user-interface code lives here, so the rules can be unit tested
/// without a window.</summary>
public sealed class Universe
{
    private bool[,] _cells;
    private int _aliveCount;

    public Universe(int width, int height, BoundaryMode boundary = BoundaryMode.Finite)
    {
        if (width < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be at least 1.");
        }

        if (height < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be at least 1.");
        }

        Width = width;
        Height = height;
        Boundary = boundary;
        _cells = new bool[width, height];
    }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public BoundaryMode Boundary { get; }

    /// <summary>Generations computed by <see cref="Step"/>. Reset by
    /// <see cref="Clear"/> and <see cref="Randomize(int?, double)"/>;
    /// preserved by <see cref="Resize"/> and <see cref="WithBoundary"/>.</summary>
    public int Generation { get; private set; }

    /// <summary>Live-cell count, maintained incrementally by every
    /// mutation instead of rescanned: the status bar reads it on every
    /// generation, so a full-grid scan per tick would grow with the grid
    /// for no benefit.</summary>
    public int AliveCount => _aliveCount;

    public bool this[int x, int y]
    {
        get => _cells[CheckX(x), CheckY(y)];
        set
        {
            int checkedX = CheckX(x);
            int checkedY = CheckY(y);
            if (_cells[checkedX, checkedY] != value)
            {
                _cells[checkedX, checkedY] = value;
                _aliveCount += value ? 1 : -1;
            }
        }
    }

    /// <summary>Flips one cell and returns its new state.</summary>
    public bool Toggle(int x, int y)
    {
        this[x, y] = !this[x, y];
        return this[x, y];
    }

    /// <summary>Kills every cell and resets the generation count.</summary>
    public void Clear()
    {
        Array.Clear(_cells);
        _aliveCount = 0;
        Generation = 0;
    }

    /// <summary>Resizes the grid, keeping the cells that still fit and
    /// dropping the rest. The generation count is preserved.</summary>
    public void Resize(int width, int height)
    {
        if (width < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be at least 1.");
        }

        if (height < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be at least 1.");
        }

        var resized = new bool[width, height];
        int copyWidth = Math.Min(width, Width);
        int copyHeight = Math.Min(height, Height);
        for (int y = 0; y < copyHeight; y++)
        {
            for (int x = 0; x < copyWidth; x++)
            {
                resized[x, y] = _cells[x, y];
            }
        }

        _cells = resized;
        Width = width;
        Height = height;

        // The copy above drops cells outside the new bounds, so the count
        // is rebuilt from the resized grid rather than carried over.
        _aliveCount = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_cells[x, y])
                {
                    _aliveCount++;
                }
            }
        }
    }

    /// <summary>Returns a copy with the same cells and generation count
    /// but a different edge rule.</summary>
    public Universe WithBoundary(BoundaryMode boundary)
    {
        var copy = new Universe(Width, Height, boundary);
        Array.Copy(_cells, copy._cells, _cells.Length);
        copy._aliveCount = _aliveCount;
        copy.Generation = Generation;
        return copy;
    }

    /// <summary>Returns an independent copy: changing one universe never
    /// changes the other.</summary>
    public Universe Clone()
    {
        var copy = new Universe(Width, Height, Boundary);
        Array.Copy(_cells, copy._cells, _cells.Length);
        copy._aliveCount = _aliveCount;
        copy.Generation = Generation;
        return copy;
    }

    /// <summary>Advances one generation. The next state is computed into
    /// a fresh grid first, so no cell is ever read after it has been
    /// overwritten: a dead cell with the wrong neighbour count stays dead
    /// instead of keeping the previous generation's value.</summary>
    public void Step()
    {
        var next = new bool[Width, Height];
        int alive = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int count = CountNeighbors(x, y);
                next[x, y] = _cells[x, y] ? count is 2 or 3 : count == 3;
                if (next[x, y])
                {
                    alive++;
                }
            }
        }

        _cells = next;
        _aliveCount = alive;
        Generation++;
    }

    public int CountNeighbors(int x, int y)
    {
        CheckX(x);
        CheckY(y);

        int count = 0;
        for (int yOffset = -1; yOffset <= 1; yOffset++)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                if (xOffset == 0 && yOffset == 0)
                {
                    continue;
                }

                if (IsAliveNeighbor(x + xOffset, y + yOffset))
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>Fills the grid at random: each cell is alive with the
    /// given probability (0.5 by default, as in the original). A seed
    /// makes the pattern reproducible. Resets the generation count.</summary>
    public void Randomize(int? seed = null, double density = 0.5)
    {
        if (density < 0 || density > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(density), "Density must be between 0 and 1.");
        }

        Random random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        int alive = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _cells[x, y] = random.NextDouble() < density;
                if (_cells[x, y])
                {
                    alive++;
                }
            }
        }

        _aliveCount = alive;
        Generation = 0;
    }

    private bool IsAliveNeighbor(int x, int y)
    {
        if (Boundary == BoundaryMode.Toroidal)
        {
            int wrappedX = ((x % Width) + Width) % Width;
            int wrappedY = ((y % Height) + Height) % Height;
            return _cells[wrappedX, wrappedY];
        }

        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return false;
        }

        return _cells[x, y];
    }

    private int CheckX(int x)
    {
        if (x < 0 || x >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        return x;
    }

    private int CheckY(int y)
    {
        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        return y;
    }
}
