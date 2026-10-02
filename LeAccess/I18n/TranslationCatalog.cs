using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LeAccess.I18n;

/// <summary>Loads UTF-8 catalogs once and falls back per key to bundled English.</summary>
internal sealed class TranslationCatalog
{
    private const string ResourcePrefix = "LeAccess.Translations.";
    private static readonly Regex Parameters = new(@"{{|}}|{(?<index>[0-9]+)[^{}]*}");
    private readonly Dictionary<string, string> _english;
    private readonly Dictionary<string, Dictionary<string, string>> _loaded = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _warnings = new(StringComparer.Ordinal);
    private readonly string _directory;
    private readonly Action<string> _warning;

    internal TranslationCatalog(string directory, Action<string> warning)
    {
        _directory = directory;
        _warning = warning;
        using var stream = typeof(TranslationCatalog).Assembly.GetManifestResourceStream(ResourcePrefix + "en.json");
        _english = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
    }

    internal string Get(CultureInfo culture, string key)
    {
        if (!_english.TryGetValue(key, out string fallback))
        {
            Warn("Unknown translation key: " + key);
            return string.Empty;
        }
        for (var current = culture; !string.IsNullOrEmpty(current.Name); current = current.Parent)
            if (Load(current.Name).TryGetValue(key, out string text)) return text;
        return Load("en").TryGetValue(key, out string english) ? english : fallback;
    }

    internal string Format(CultureInfo culture, string key, object[] arguments)
    {
        try { return string.Format(culture, Get(culture, key), arguments); }
        catch (FormatException)
        {
            Warn("Cannot format translation: " + key);
            // A caller or translator error must not interrupt other accessibility feedback.
            try { return string.Format(CultureInfo.GetCultureInfo("en"), _english[key], arguments); }
            catch (FormatException) { return string.Empty; }
        }
    }

    private Dictionary<string, string> Load(string language)
    {
        if (_loaded.TryGetValue(language, out var catalog)) return catalog;
        catalog = new Dictionary<string, string>(StringComparer.Ordinal);
        _loaded[language] = catalog;
        using (var embedded = typeof(TranslationCatalog).Assembly.GetManifestResourceStream(ResourcePrefix + language + ".json"))
            if (embedded != null) Read(embedded, catalog, "bundled " + language);
        if (string.IsNullOrEmpty(_directory)) return catalog;
        string path = Path.Combine(_directory, language + ".json");
        try
        {
            if (File.Exists(path))
            {
                using var file = File.OpenRead(path);
                Read(file, catalog, path);
            }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        { Warn($"Cannot read translation file {path}: {ex.Message}"); }
        return catalog;
    }

    private void Read(Stream stream, Dictionary<string, string> catalog, string source)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var document = JsonDocument.Parse(reader.ReadToEnd());
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            { Warn("Translation catalog must be a JSON object: " + source); return; }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in document.RootElement.EnumerateObject())
            {
                if (!seen.Add(entry.Name))
                { Warn($"Duplicate translation key {entry.Name} in {source}"); continue; }
                if (!_english.TryGetValue(entry.Name, out string fallback))
                { Warn($"Unknown translation key {entry.Name} in {source}"); continue; }
                if (entry.Value.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(entry.Value.GetString()) ||
                    !HasMatchingParameters(entry.Value.GetString(), fallback))
                { Warn($"Invalid translation {entry.Name} in {source}; keeping fallback"); continue; }
                catalog[entry.Name] = entry.Value.GetString();
            }
        }
        catch (JsonException ex) { Warn($"Cannot parse translation file {source}: {ex.Message}"); }
    }

    private static bool HasMatchingParameters(string text, string fallback)
    {
        try
        {
            var expected = ParameterIndices(fallback);
            int count = expected.Count == 0 ? 0 : expected.Max() + 1;
            _ = string.Format(CultureInfo.InvariantCulture, text, new object[count]);
            return expected.SetEquals(ParameterIndices(text));
        }
        catch (FormatException) { return false; }
        catch (OverflowException) { return false; }
    }

    private static HashSet<int> ParameterIndices(string text)
    {
        var indices = new HashSet<int>();
        foreach (Match match in Parameters.Matches(text))
            if (match.Groups["index"].Success)
                indices.Add(int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture));
        return indices;
    }

    private void Warn(string message)
    {
        if (_warnings.Add(message)) _warning?.Invoke(message);
    }
}
