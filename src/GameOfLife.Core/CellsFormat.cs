using System.Text;

namespace GameOfLife.Core;

/// <summary>The <c>.cells</c> plain-text format: lines starting with
/// <c>!</c> are comments and are ignored on load, <c>O</c> is a live cell
/// and <c>.</c> is a dead cell. Rows shorter than the widest row are
/// padded with dead cells. The generation count is not stored: a loaded
/// pattern always starts at generation 0.</summary>
public static class CellsFormat
{
    private const int MaxCells = 1_000_000;

    /// <summary>Serializes a universe: one comment line, then one row
    /// per grid row.</summary>
    public static string Serialize(Universe universe)
    {
        ArgumentNullException.ThrowIfNull(universe);

        var builder = new StringBuilder();
        builder.AppendLine("!Conway's Game of Life");
        for (int y = 0; y < universe.Height; y++)
        {
            for (int x = 0; x < universe.Width; x++)
            {
                builder.Append(universe[x, y] ? 'O' : '.');
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>Writes a universe to a text writer. The caller owns the writer.</summary>
    public static void Save(Universe universe, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(universe);
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(Serialize(universe));
    }

    /// <summary>Parses <c>.cells</c> text. The edge rule of the result
    /// is the caller's choice: the file format does not record it.</summary>
    public static Universe Parse(string text, BoundaryMode boundary = BoundaryMode.Finite)
    {
        ArgumentNullException.ThrowIfNull(text);
        using var reader = new StringReader(text);
        return Load(reader, boundary);
    }

    /// <summary>Reads a <c>.cells</c> document from a text reader.
    /// The caller owns the reader. Throws
    /// <see cref="InvalidDataException"/> when the document holds no
    /// cell rows, when a row holds any character other than <c>O</c>
    /// or <c>.</c>, or when the document holds more than 1,000,000
    /// cells: without a cap, a hostile or accidental file could ask for
    /// an absurd grid before anything validates it.</summary>
    public static Universe Load(TextReader reader, BoundaryMode boundary = BoundaryMode.Finite)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var rows = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            string row = line.TrimEnd();
            if (row.Length == 0 || row.StartsWith('!'))
            {
                continue;
            }

            foreach (char c in row)
            {
                if (c is not 'O' and not '.')
                {
                    throw new InvalidDataException($"Invalid character '{c}' in .cells row: \"{row}\".");
                }
            }

            rows.Add(row);
        }

        if (rows.Count == 0)
        {
            throw new InvalidDataException("The .cells document holds no cell rows.");
        }

        int width = rows.Max(row => row.Length);
        long cellCount = (long)width * rows.Count;
        if (cellCount > MaxCells)
        {
            throw new InvalidDataException(
                $"The .cells document is {width}x{rows.Count} ({cellCount:N0} cells), over the {MaxCells:N0}-cell limit.");
        }

        var universe = new Universe(width, rows.Count, boundary);
        for (int y = 0; y < rows.Count; y++)
        {
            for (int x = 0; x < rows[y].Length; x++)
            {
                universe[x, y] = rows[y][x] == 'O';
            }
        }

        return universe;
    }
}
