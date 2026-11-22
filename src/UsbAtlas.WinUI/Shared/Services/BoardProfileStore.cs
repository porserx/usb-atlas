using System.IO;
using System.Text.Json;
using UsbAtlas.Models;

namespace UsbAtlas.Services;

internal static class BoardProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static BoardProfile? Find(BoardInfo board)
    {
        foreach (string path in CandidatePaths())
        {
            if (!File.Exists(path))
                continue;

            try
            {
                string json = File.ReadAllText(path);
                var file = JsonSerializer.Deserialize<BoardProfileFile>(json, JsonOptions);
                if (file?.Profiles is null)
                    continue;

                var match = file.Profiles.FirstOrDefault(profile => profile.Matches(board.Manufacturer, board.Product));
                if (match is not null)
                    return match;
            }
            catch
            {
                // A broken optional profile must never prevent the live USB scan from loading.
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidatePaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Assets", "board-profiles.json");
        yield return Path.Combine(AppContext.BaseDirectory, "board-profiles.json");
    }
}
