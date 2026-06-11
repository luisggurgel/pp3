using Godot;
using System.Collections.Generic;
using System.Text.Json;

namespace PP3.Minigame;

public class BeatmapNote
{
    public float TimeSeconds { get; set; }
    public FoodType Type { get; set; }
    public int PositionIndex { get; set; }
}

public class BeatmapData
{
    public string SongName { get; set; } = "";
    public float Bpm { get; set; }
    public float OffsetSeconds { get; set; }
    public float TimeoutSeconds { get; set; } = 3.0f;
    public List<BeatmapNote> Notes { get; set; } = new();
}

public static class BeatmapLoader
{
    public static BeatmapData Load(string resourcePath)
    {
        string json = FileAccess.GetFileAsString(resourcePath);
        if (string.IsNullOrEmpty(json))
        {
            GD.PrintErr($"BeatmapLoader: Failed to read file at {resourcePath}");
            return new BeatmapData();
        }

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var data = new BeatmapData
        {
            SongName = root.TryGetProperty("song_name", out var sn) ? sn.GetString() ?? "" : "",
            Bpm = root.TryGetProperty("bpm", out var bpm) ? bpm.GetSingle() : 120f,
            OffsetSeconds = root.TryGetProperty("offset_seconds", out var off) ? off.GetSingle() : 0f,
            TimeoutSeconds = root.TryGetProperty("timeout_seconds", out var to) ? to.GetSingle() : 3.0f,
            Notes = new List<BeatmapNote>()
        };

        if (root.TryGetProperty("notes", out var notesArray))
        {
            foreach (var note in notesArray.EnumerateArray())
            {
                string typeStr = note.GetProperty("type").GetString() ?? "";
                FoodType foodType = typeStr.ToLower() switch
                {
                    "bread" => FoodType.Bread,
                    "cookie" => FoodType.Cookie,
                    "donut" => FoodType.Donut,
                    _ => throw new System.Exception($"Unknown food type: {typeStr}")
                };

                data.Notes.Add(new BeatmapNote
                {
                    TimeSeconds = note.GetProperty("time").GetSingle(),
                    Type = foodType,
                    PositionIndex = note.GetProperty("position").GetInt32()
                });
            }
        }

        // Sort notes by time
        data.Notes.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));

        GD.Print($"BeatmapLoader: Loaded {data.Notes.Count} notes for '{data.SongName}'");
        return data;
    }
}
