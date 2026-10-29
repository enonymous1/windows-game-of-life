namespace GameOfLife.Core;

/// <summary>How cells at the edge of the universe count their neighbours.</summary>
public enum BoundaryMode
{
    /// <summary>Cells outside the grid do not exist and are never counted.</summary>
    Finite,

    /// <summary>The grid wraps: the left edge neighbours the right edge,
    /// and the top edge neighbours the bottom edge.</summary>
    Toroidal,
}
