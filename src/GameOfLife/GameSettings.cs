using System.Text.Json;
using System.Text.Json.Serialization;
using GameOfLife.Core;

namespace GameOfLife;

/// <summary>Choices that survive between runs: grid size, timer interval,
/// and edge rule. Loaded at startup and saved on close. Any unreadable,
/// malformed, or out-of-range file falls back to
/// <see cref="Default"/> instead of failing startup, and a failed save
/// never blocks closing: settings are a convenience, not a requirement.</summary>
internal sealed record GameSettings(int Width, int Height, int IntervalMs, BoundaryMode Boundary)
{
    public static GameSettings Default => new(20, 20, 100, BoundaryMode.Finite);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "windows-game-of-life",
        "settings.json");

    public static GameSettings Load()
    {
        try
        {
            string json = File.ReadAllText(FilePath);
            GameSettings? loaded = JsonSerializer.Deserialize<GameSettings>(json, JsonOptions);
            return loaded is null ? Default : Validate(loaded);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return Default;
        }
    }

    public static void Save(GameSettings settings)
    {
        try
        {
            string path = FilePath;
            string? directory = Path.GetDirectoryName(path);
            if (directory is not null)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Settings are a convenience, never a reason to fail closing.
        }
    }

    // A hand-edited or corrupt file must not create an absurd grid at
    // startup, so values are clamped and an unknown edge rule resets to
    // Finite instead of throwing.
    private static GameSettings Validate(GameSettings settings)
    {
        int width = Math.Clamp(settings.Width, 1, 1000);
        int height = Math.Clamp(settings.Height, 1, 1000);
        int interval = Math.Clamp(settings.IntervalMs, 10, 2000);
        BoundaryMode boundary = settings.Boundary is BoundaryMode.Finite or BoundaryMode.Toroidal
            ? settings.Boundary
            : BoundaryMode.Finite;
        return new GameSettings(width, height, interval, boundary);
    }
}
