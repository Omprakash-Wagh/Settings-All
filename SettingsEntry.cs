using System;
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

    private string _uriTokens;
    [JsonIgnore]
    public string UriTokens
    {
        get
        {
            if (_uriTokens == null)
            {
                if (string.IsNullOrEmpty(Uri)) 
                {
                    _uriTokens = "";
                }
                else 
                {
                    var span = Uri.AsSpan(Uri.StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase) ? "ms-settings:".Length : 0);
                    int qMark = span.IndexOf('?');
                    var pathSegment = qMark >= 0 ? span[..qMark] : span;
                    _uriTokens = pathSegment.ToString().Replace('-', ' ').Replace('_', ' ').Replace(':', ' ');
                }
            }
            return _uriTokens;
        }
    }
}
