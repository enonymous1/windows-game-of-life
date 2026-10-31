using GameOfLife.Core;
using Xunit;

namespace GameOfLife.Core.Tests;

public class CellsFormatTests
{
    [Fact]
    public void RoundTrip_PreservesPatternAndSize()
    {
        Universe original = Parse("!Glider\n.O.\n..O\nOOO\n");

        string serialized = CellsFormat.Serialize(original);
        Universe parsed = CellsFormat.Parse(serialized);

        Assert.Equal(original.Width, parsed.Width);
        Assert.Equal(original.Height, parsed.Height);
        Assert.Equal(serialized, CellsFormat.Serialize(parsed));
    }

    [Fact]
    public void Parse_IgnoresCommentsAndBlankLines()
    {
        Universe universe = Parse("!A comment\n\nO..\n!Another comment\n.O.\n\n..O\n");

        Assert.Equal(3, universe.Width);
        Assert.Equal(3, universe.Height);
        Assert.True(universe[0, 0]);
        Assert.True(universe[1, 1]);
        Assert.True(universe[2, 2]);
        Assert.Equal(3, universe.AliveCount);
    }

    [Fact]
    public void Parse_PadsRaggedRowsWithDeadCells()
    {
        Universe universe = Parse("O\nOO\n");

        Assert.Equal(2, universe.Width);
        Assert.Equal(2, universe.Height);
        Assert.True(universe[0, 0]);
        Assert.False(universe[1, 0]);
        Assert.True(universe[0, 1]);
        Assert.True(universe[1, 1]);
    }

    [Fact]
    public void Parse_HandlesWindowsLineEndings()
    {
        Universe universe = Parse("O.\r\n.O\r\n");

        Assert.Equal(2, universe.Width);
        Assert.Equal(2, universe.Height);
        Assert.True(universe[0, 0]);
        Assert.True(universe[1, 1]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("!Only comments\n!And nothing else\n")]
    public void Parse_NoCellRows_Throws(string text)
    {
        Assert.Throws<InvalidDataException>(() => CellsFormat.Parse(text));
    }

    [Fact]
    public void Parse_InvalidCharacter_Throws()
    {
        Assert.Throws<InvalidDataException>(() => CellsFormat.Parse("OX\n"));
    }

    [Fact]
    public void Parse_OversizedDocument_Throws()
    {
        // 1,001 rows of 1,000 cells: 1,001,000 cells, over the cap.
        string row = new string('.', 1000);
        string text = string.Join("\n", Enumerable.Repeat(row, 1001));

        Assert.Throws<InvalidDataException>(() => CellsFormat.Parse(text));
    }

    [Fact]
    public void Parse_AtCellLimit_Loads()
    {
        // Exactly 1,000,000 cells: at the cap, so it loads.
        string row = new string('.', 1000);
        string text = string.Join("\n", Enumerable.Repeat(row, 1000));

        Universe universe = CellsFormat.Parse(text);

        Assert.Equal(1000, universe.Width);
        Assert.Equal(1000, universe.Height);
        Assert.Equal(0, universe.AliveCount);
    }

    [Fact]
    public void Parse_NullText_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CellsFormat.Parse(null!));
    }

    [Fact]
    public void Serialize_WritesCommentThenRows()
    {
        var universe = new Universe(2, 2);
        universe[0, 0] = true;

        string text = CellsFormat.Serialize(universe).Replace("\r\n", "\n");

        Assert.Equal("!Conway's Game of Life\nO.\n..\n", text);
    }

    [Fact]
    public void SaveAndLoad_ViaReaders_RoundTrip()
    {
        Universe original = Parse("O.\n.O\n");

        using var writer = new StringWriter();
        CellsFormat.Save(original, writer);
        using var reader = new StringReader(writer.ToString());
        Universe loaded = CellsFormat.Load(reader);

        Assert.Equal(CellsFormat.Serialize(original), CellsFormat.Serialize(loaded));
    }

    [Fact]
    public void Parse_UsesRequestedBoundaryMode()
    {
        Universe toroidal = CellsFormat.Parse("O.\n", BoundaryMode.Toroidal);
        Universe finite = CellsFormat.Parse("O.\n");

        Assert.Equal(BoundaryMode.Toroidal, toroidal.Boundary);
        Assert.Equal(BoundaryMode.Finite, finite.Boundary);
    }

    [Fact]
    public void Load_NullReader_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CellsFormat.Load(null!));
    }

    private static Universe Parse(string text) => CellsFormat.Parse(text);
}
