using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SettingsAll;

public static partial class DllScanner
{
    private static readonly byte[] AsciiPattern = "ms-settings:"u8.ToArray();
    private static readonly byte[] Utf16Pattern = Encoding.Unicode.GetBytes("ms-settings:");

    private static readonly string[] RedundantKeywords =
    {
        "a9",
        "agenttools",
        "holographic",
        "quiethours",
        "quietmoments",
        "wheel",
        "surfacehub",
        "oobe",
        "cellular",
        "dialup",
        "proximity",
        "crossdevice",
        "customdevices",
        "addphone-direct"
    };

    [GeneratedRegex(@"^[a-zA-Z0-9\-_/]+$")]
    private static partial Regex PathRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9\-_=&%.]+$")]
    private static partial Regex QueryRegex();

    public static bool IsValidUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri) || !uri.StartsWith("ms-settings:"))
            return false;

        var span = uri.AsSpan("ms-settings:".Length);
        int qMark = span.IndexOf('?');

        ReadOnlySpan<char> pathSegment;
        ReadOnlySpan<char> querySegment = default;

        if (qMark >= 0)
        {
            pathSegment = span[..qMark];
            querySegment = span[(qMark + 1)..];
        }
        else
        {
            pathSegment = span;
        }

        if (pathSegment.Length == 0 || !PathRegex().IsMatch(pathSegment.ToString()))
            return false;

        if (querySegment.Length > 0 && !QueryRegex().IsMatch(querySegment.ToString()))
            return false;

        return true;
    }

    public static HashSet<string> ScanFile(string dllPath, CancellationToken token)
    {
        try
        {
            byte[] buffer = File.ReadAllBytes(dllPath);
            return ExtractFromBytes(buffer, token);
        }
        catch (Exception)
        {
            return new HashSet<string>();
        }
    }

    public static HashSet<string> ExtractFromBytes(byte[] buffer, CancellationToken token)
    {
        var rawMatches = new List<string>();

        // ASCII pass
        ReadOnlySpan<byte> span = buffer;
        int offset = 0;
        while (offset < span.Length)
        {
            token.ThrowIfCancellationRequested();

            int idx = span[offset..].IndexOf(AsciiPattern);
            if (idx < 0) break;

            int matchPos = offset + idx;
            offset = matchPos + AsciiPattern.Length;

            // Boundary check: previous byte must be outside 0x21-0x7E
            if (matchPos > 0)
            {
                byte prevByte = buffer[matchPos - 1];
                if (prevByte >= 0x21 && prevByte <= 0x7E)
                    continue;
            }

            // Extract forward
            int length = AsciiPattern.Length;
            while (matchPos + length < buffer.Length)
            {
                byte b = buffer[matchPos + length];
                if (b < 0x21 || b > 0x7E) // boundary
                    break;
                length++;
            }

            if (matchPos + length == buffer.Length)
            {
                // EOF without hitting a valid boundary terminator - reject (Test 14)
                continue;
            }

            if (length >= 13 && length <= 200)
            {
                string match = Encoding.ASCII.GetString(buffer, matchPos, length);
                if (IsValidCandidate(match))
                {
                    rawMatches.Add(match);
                }
            }
        }

        // UTF-16LE pass
        offset = 0;
        while (offset < span.Length - 1)
        {
            token.ThrowIfCancellationRequested();

            int idx = span[offset..].IndexOf(Utf16Pattern);
            if (idx < 0) break;
            
            int matchPos = offset + idx;
            offset = matchPos + Utf16Pattern.Length;

            // Boundary check: previous 2 bytes (code unit) must be outside URI range.
            if (matchPos >= 2)
            {
                byte low = buffer[matchPos - 2];
                byte high = buffer[matchPos - 1];
                if (high == 0x00 && low >= 0x21 && low <= 0x7E)
                    continue;
            }

            // Extract forward in 2-byte chunks
            int lengthBytes = Utf16Pattern.Length;
            while (matchPos + lengthBytes + 1 < buffer.Length)
            {
                byte low = buffer[matchPos + lengthBytes];
                byte high = buffer[matchPos + lengthBytes + 1];
                
                if (high != 0x00 || low < 0x21 || low > 0x7E)
                    break;
                    
                lengthBytes += 2;
            }

            if (matchPos + lengthBytes >= buffer.Length - 1)
            {
                // EOF without boundary
                continue;
            }

            int charLength = lengthBytes / 2;
            if (charLength >= 13 && charLength <= 200)
            {
                string match = Encoding.Unicode.GetString(buffer, matchPos, lengthBytes);
                if (IsValidCandidate(match))
                {
                    rawMatches.Add(match);
                }
            }
        }

        return DedupMatches(rawMatches);
    }

    private static bool IsValidCandidate(string match)
    {
        if (match.Contains("%s") || match.Contains("%d") || match.Contains("%1") || 
            match.Contains("{0}") || match.Contains("{1}"))
            return false;

        if (match.Contains('{') && match.Contains('}'))
            return false;

        foreach (var keyword in RedundantKeywords)
        {
            if (match.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return IsValidUri(match);
    }

    private static HashSet<string> DedupMatches(List<string> rawMatches)
    {
        var dict = new Dictionary<string, string>();

        foreach (var match in rawMatches)
        {
            var span = match.AsSpan("ms-settings:".Length);
            int qMark = span.IndexOf('?');
            string pathKey = (qMark >= 0 ? span[..qMark] : span).ToString().ToLowerInvariant();

            if (!dict.TryGetValue(pathKey, out string currentBest))
            {
                dict[pathKey] = match;
            }
            else
            {
                bool isCurrentBare = !currentBest.Contains('?');
                bool isNewBare = !match.Contains('?');

                if (isNewBare && !isCurrentBare)
                {
                    dict[pathKey] = match;
                }
                else if (!isNewBare && !isCurrentBare)
                {
                    if (match.Length < currentBest.Length)
                        dict[pathKey] = match;
                }
            }
        }

        return new HashSet<string>(dict.Values);
    }
}
