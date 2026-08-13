using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SettingsAll;

public static class CacheManager
{
    private const int CacheSchemaVersion = 3;
    
    public class CacheModel
    {
        [JsonPropertyName("cacheSchemaVersion")]
        public int CacheSchemaVersion { get; set; }

        [JsonPropertyName("dllVersion")]
        public string DllVersion { get; set; }

        [JsonPropertyName("dllLastWriteUtc")]
        public DateTime DllLastWriteUtc { get; set; }

        [JsonPropertyName("seedDictionaryHash")]
        public string SeedDictionaryHash { get; set; }

        [JsonPropertyName("entries")]
        public List<SettingsEntry> Entries { get; set; }
    }

    public static bool TryLoadCache(string cachePath, string dllPath, string seedHash, out List<SettingsEntry> entries)
    {
        entries = null;
        if (!File.Exists(cachePath)) return false;

        try
        {
            var dllInfo = FileVersionInfo.GetVersionInfo(dllPath);
            string currentDllVersion = dllInfo.FileVersion ?? "";
            DateTime currentDllLastWrite = File.GetLastWriteTimeUtc(dllPath);

            byte[] bytes = File.ReadAllBytes(cachePath);
            var cache = JsonSerializer.Deserialize<CacheModel>(bytes);

            if (cache != null &&
                cache.CacheSchemaVersion == CacheSchemaVersion &&
                cache.DllVersion == currentDllVersion &&
                cache.DllLastWriteUtc == currentDllLastWrite &&
                cache.SeedDictionaryHash == seedHash)
            {
                entries = cache.Entries;
                return true;
            }
        }
        catch (Exception)
        {
            // Corrupted JSON or unexpected error -> delete cache and rebuild
            try { File.Delete(cachePath); } catch { }
        }

        return false;
    }

    public static void SaveCache(string cachePath, string dllPath, string seedHash, List<SettingsEntry> entries)
    {
        try
        {
            var dllInfo = FileVersionInfo.GetVersionInfo(dllPath);
            string currentDllVersion = dllInfo.FileVersion ?? "";
            DateTime currentDllLastWrite = File.GetLastWriteTimeUtc(dllPath);

            var cache = new CacheModel
            {
                CacheSchemaVersion = CacheSchemaVersion,
                DllVersion = currentDllVersion,
                DllLastWriteUtc = currentDllLastWrite,
                SeedDictionaryHash = seedHash,
                Entries = entries
            };

            string tempPath = cachePath + ".tmp";
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(cache, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllBytes(tempPath, bytes);

            File.Move(tempPath, cachePath, overwrite: true);
        }
        catch (Exception)
        {
            // If we fail to write cache, we just run without it next time.
        }
    }
}
