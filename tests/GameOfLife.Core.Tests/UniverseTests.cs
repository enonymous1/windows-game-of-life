using GameOfLife.Core;
using Xunit;

namespace GameOfLife.Core.Tests;

public class UniverseTests
{
    [Fact]
    public void BlockStillLife_IsStable()
    {
        Universe universe = WithCells(6, 6, (2, 2), (3, 2), (2, 3), (3, 3));
        string before = CellsFormat.Serialize(universe);

        universe.Step();

        Assert.Equal(before, CellsFormat.Serialize(universe));
        Assert.Equal(1, universe.Generation);
    }

    [Fact]
    public void Blinker_OscillatesWithPeriodTwo()
    {
        Universe universe = WithCells(5, 5, (1, 2), (2, 2), (3, 2));

        universe.Step();
        Assert.True(universe[2, 1]);
        Assert.True(universe[2, 2]);
        Assert.True(universe[2, 3]);
        Assert.Equal(3, universe.AliveCount);

        universe.Step();
        Assert.True(universe[1, 2]);
        Assert.True(universe[2, 2]);
        Assert.True(universe[3, 2]);
        Assert.Equal(3, universe.AliveCount);
    }

    [Fact]
    public void Glider_MovesOneCellDiagonallyEveryFourGenerations()
    {
        Universe universe = WithCells(10, 10, (2, 1), (3, 2), (1, 3), (2, 3), (3, 3));

        for (int i = 0; i < 4; i++)
        {
            universe.Step();
        }

        Assert.True(universe[3, 2]);
        Assert.True(universe[4, 3]);
        Assert.True(universe[2, 4]);
        Assert.True(universe[3, 4]);
        Assert.True(universe[4, 4]);
        Assert.Equal(5, universe.AliveCount);
    }

    [Fact]
    public void SingleCell_DiesAndStaysDead()
    {
        // Regression test for the original scratch-pad bug: a dead cell
        // was never written back, so a lone cell could resurrect from
        // the previous generation's value after the buffer swap.
        Universe universe = WithCells(5, 5, (2, 2));

        universe.Step();
        Assert.Equal(0, universe.AliveCount);

        universe.Step();
        Assert.Equal(0, universe.AliveCount);
        Assert.False(universe[2, 2]);
        Assert.Equal(2, universe.Generation);
    }

    [Fact]
    public void CellWithOneNeighbour_Dies()
    {
        Universe universe = WithCells(5, 5, (1, 1), (2, 1));

        universe.Step();

        Assert.Equal(0, universe.AliveCount);
    }

    [Fact]
    public void CellWithFourNeighbours_Dies()
    {
        Universe universe = WithCells(5, 5, (2, 2), (2, 1), (2, 3), (1, 2), (3, 2));

        universe.Step();

        Assert.False(universe[2, 2]);
    }

    [Fact]
    public void DeadCellWithThreeNeighbours_IsBorn()
    {
        Universe universe = WithCells(5, 5, (1, 1), (2, 1), (1, 2));

        universe.Step();

        Assert.True(universe[2, 2]);
    }

    [Fact]
    public void DeadCellWithTwoNeighbours_StaysDead()
    {
        Universe universe = WithCells(5, 5, (1, 1), (2, 1));

        universe.Step();

        Assert.False(universe[2, 2]);
    }

    [Fact]
    public void FiniteEdge_DoesNotWrapToOppositeEdge()
    {
        Universe universe = WithCells(5, 5, (4, 2));

        Assert.Equal(0, universe.CountNeighbors(0, 2));
    }

    [Fact]
    public void ToroidalEdge_WrapsToOppositeEdge()
    {
        Universe universe = WithCells(5, 5, BoundaryMode.Toroidal, (4, 2));

        Assert.Equal(1, universe.CountNeighbors(0, 2));
    }

    [Fact]
    public void ToroidalEdge_CellIsBornAcrossTheWrap()
    {
        Universe universe = WithCells(5, 5, BoundaryMode.Toroidal, (4, 2), (0, 1), (0, 3));

        universe.Step();

        Assert.True(universe[0, 2]);
    }

    [Fact]
    public void CountNeighbors_CountsDiagonals()
    {
        var universe = new Universe(3, 3);
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                universe[x, y] = true;
            }
        }

        Assert.Equal(8, universe.CountNeighbors(1, 1));
    }

    [Fact]
    public void Resize_PreservesCellsThatStillFit()
    {
        Universe universe = WithCells(5, 5, (0, 0), (4, 4), (2, 2));

        universe.Resize(3, 3);

        Assert.Equal(3, universe.Width);
        Assert.Equal(3, universe.Height);
        Assert.True(universe[0, 0]);
        Assert.True(universe[2, 2]);

        universe.Resize(6, 6);
        Assert.True(universe[0, 0]);
        Assert.True(universe[2, 2]);
        Assert.False(universe[4, 4]);
        Assert.Equal(2, universe.AliveCount);
    }

    [Fact]
    public void Resize_PreservesGenerationCount()
    {
        Universe universe = WithCells(5, 5, (2, 2), (3, 2), (2, 3), (3, 3));
        universe.Step();

        universe.Resize(4, 4);

        Assert.Equal(1, universe.Generation);
    }

    [Fact]
    public void Clear_KillsAllCellsAndResetsGeneration()
    {
        Universe universe = WithCells(5, 5, (1, 1), (2, 2));
        universe.Step();

        universe.Clear();

        Assert.Equal(0, universe.AliveCount);
        Assert.Equal(0, universe.Generation);
    }

    [Fact]
    public void Randomize_WithSameSeed_IsDeterministic()
    {
        var first = new Universe(10, 10);
        var second = new Universe(10, 10);

        first.Randomize(42);
        second.Randomize(42);

        Assert.Equal(CellsFormat.Serialize(first), CellsFormat.Serialize(second));
    }

    [Fact]
    public void Randomize_WithDifferentSeeds_Differs()
    {
        var first = new Universe(20, 20);
        var second = new Universe(20, 20);

        first.Randomize(1);
        second.Randomize(2);

        Assert.NotEqual(CellsFormat.Serialize(first), CellsFormat.Serialize(second));
    }

    [Fact]
    public void Randomize_DensityZeroKillsAll_DensityOneFillsAll()
    {
        var empty = new Universe(5, 5);
        empty.Randomize(seed: 7, density: 0);
        Assert.Equal(0, empty.AliveCount);

        var full = new Universe(5, 5);
        full.Randomize(seed: 7, density: 1);
        Assert.Equal(25, full.AliveCount);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void Randomize_InvalidDensity_Throws(double density)
    {
        var universe = new Universe(5, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => universe.Randomize(density: density));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(5, 0)]
    public void Constructor_NonPositiveSize_Throws(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Universe(width, height));
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        var universe = new Universe(3, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() => universe[3, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => universe[0, -1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => universe[-1, 0] = true);
    }

    [Fact]
    public void Toggle_FlipsCellAndReturnsNewState()
    {
        var universe = new Universe(3, 3);

        Assert.True(universe.Toggle(1, 1));
        Assert.True(universe[1, 1]);
        Assert.False(universe.Toggle(1, 1));
        Assert.False(universe[1, 1]);
    }

    [Fact]
    public void WithBoundary_KeepsCellsAndGeneration()
    {
        Universe universe = WithCells(5, 5, (2, 2), (3, 2), (2, 3), (3, 3));
        universe.Step();

        Universe toroidal = universe.WithBoundary(BoundaryMode.Toroidal);

        Assert.Equal(BoundaryMode.Toroidal, toroidal.Boundary);
        Assert.Equal(BoundaryMode.Finite, universe.Boundary);
        Assert.Equal(1, toroidal.Generation);
        Assert.Equal(CellsFormat.Serialize(universe), CellsFormat.Serialize(toroidal));
    }

    [Fact]
    public void Clone_IsIndependent()
    {
        Universe universe = WithCells(5, 5, (2, 2));
        universe.Step();

        Universe clone = universe.Clone();
        clone[0, 0] = true;

        Assert.False(universe[0, 0]);
        Assert.True(clone[0, 0]);
        Assert.Equal(1, clone.Generation);
    }

    [Fact]
    public void Generation_IncrementsOnlyOnStep()
    {
        var universe = new Universe(3, 3);

        universe.Toggle(1, 1);
        Assert.Equal(0, universe.Generation);

        universe.Step();
        Assert.Equal(1, universe.Generation);
        universe.Step();
        Assert.Equal(2, universe.Generation);
    }

    [Fact]
    public void AliveCount_CountsLiveCells()
    {
        Universe universe = WithCells(4, 4, (0, 0), (1, 1), (3, 3));

        Assert.Equal(3, universe.AliveCount);
    }

    private static Universe WithCells(int width, int height, params (int X, int Y)[] cells)
    {
        return WithCells(width, height, BoundaryMode.Finite, cells);
    }

    private static Universe WithCells(int width, int height, BoundaryMode boundary, params (int X, int Y)[] cells)
    {
        var universe = new Universe(width, height, boundary);
        foreach ((int x, int y) in cells)
        {
            universe[x, y] = true;
        }

        return universe;
    }
}
