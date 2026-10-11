using System;
using System.Text.Json;

namespace LeAccess.Accessibility.Updates;

/// <summary>Reads GitHub's latest-release reply and compares release versions (reference/updates.md).</summary>
internal static class UpdateRelease
{
    private static readonly char[] SuffixStarts = { '-', '+', ' ' };

    /// <summary>Parses a tag such as "v0.2.0", "0.2" or "0.2.0-test" into a four-part version; a suffix is ignored.</summary>
    internal static bool TryParseVersion(string text, out Version version)
    {
        version = null;
        string core = text?.Trim();
        if (string.IsNullOrEmpty(core)) return false;
        if (core[0] is 'v' or 'V') core = core.Substring(1);
        int suffix = core.IndexOfAny(SuffixStarts);
        if (suffix >= 0) core = core.Substring(0, suffix);
        if (int.TryParse(core, out int major) && major >= 0) core += ".0";
        if (!Version.TryParse(core, out var parsed)) return false;
        version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0), Math.Max(parsed.Revision, 0));
        return true;
    }

    /// <summary>The tag of a published release that is neither a draft nor a pre-release, or null.</summary>
    internal static string ReadTag(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || IsTrue(root, "draft") || IsTrue(root, "prerelease")) return null;
            return root.TryGetProperty("tag_name", out var tag) && tag.ValueKind == JsonValueKind.String ? tag.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static bool IsNewer(string latestTag, string currentVersion) =>
        TryParseVersion(latestTag, out var latest) && TryParseVersion(currentVersion, out var current) && latest > current;

    /// <summary>The tag as it is spoken: "v0.2.0" becomes "0.2.0".</summary>
    internal static string Display(string tag)
    {
        string text = tag?.Trim() ?? "";
        return text.Length > 1 && text[0] is 'v' or 'V' && char.IsDigit(text[1]) ? text.Substring(1) : text;
    }

    private static bool IsTrue(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
}
