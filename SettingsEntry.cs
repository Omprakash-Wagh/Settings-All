using System.Text.Json.Serialization;

namespace SettingsAll;

public class SettingsEntry
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; }

    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("aliases")]
    public string[] Aliases { get; set; }

    [JsonPropertyName("isFromSeedDictionary")]
    public bool IsFromSeedDictionary { get; set; }
}
