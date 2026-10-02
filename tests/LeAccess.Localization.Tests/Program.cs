using System.Globalization;
using System.Text.Json;
using BepInEx.Configuration;
using LeAccess;
using LeAccess.I18n;

int checks = 0;
var warnings = new List<string>();
string fixtureRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "LeAccess.Localization.Tests-" + Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(fixtureRoot);
void Equal<T>(T expected, T actual, string name)
{
    checks++;
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{name}: expected '{expected}', got '{actual}'");
}
void Initialize(string json = null, string language = "fr")
{
    string directory = Path.Combine(fixtureRoot, Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    if (json != null) File.WriteAllText(Path.Combine(directory, language + ".json"), json);
    warnings.Clear();
    ModText.Initialize(directory, warnings.Add);
    ModText.SetLanguage(language);
}
try
{
    Equal("en", ModText.Language, "default language");
    Equal("On", ModText.Get("state.on"), "default speech");
    var assembly = typeof(ModText).Assembly;
    using var englishStream = assembly.GetManifestResourceStream("LeAccess.Translations.en.json");
    using var spanishStream = assembly.GetManifestResourceStream("LeAccess.Translations.es.json");
    var english = JsonSerializer.Deserialize<Dictionary<string, string>>(englishStream);
    var spanish = JsonSerializer.Deserialize<Dictionary<string, string>>(spanishStream);
    Equal(true, english.Keys.ToHashSet().SetEquals(spanish.Keys), "complete key parity");
    foreach (var entry in english)
    {
        ModText.SetLanguage("en");
        Equal(entry.Value, ModText.Get(entry.Key), "English " + entry.Key);
        ModText.SetLanguage("es");
        Equal(spanish[entry.Key], ModText.Get(entry.Key), "Spanish " + entry.Key);
    }
    ModText.SetLanguage("es-MX");
    Equal("Menú del mando", ModText.Get("controller.menu"), "regional parent");
    Initialize("{\"state.on\":\"Oui\"}");
    Equal("Oui", ModText.Get("state.on"), "external override");
    Equal("Off", ModText.Get("state.off"), "per-key English fallback");
    Initialize("{\"state.on\":\"Sí personalizado\"}", "es");
    Equal("Sí personalizado", ModText.Get("state.on"), "external bundled override");
    Equal("Desactivado", ModText.Get("state.off"), "partial keeps bundled Spanish");
    foreach (string invalid in new[] { "", "{broken", "[]", "null", "{\"state.on\":null}", "{\"state.on\":5}", "{\"state.on\":\"\"}", "{\"state.on\":\"  \"}" })
    {
        Initialize(invalid);
        Equal("On", ModText.Get("state.on"), "invalid JSON fallback " + invalid);
        Equal(true, warnings.Count > 0, "invalid JSON warning");
        int count = warnings.Count;
        ModText.Get("state.on");
        Equal(count, warnings.Count, "warning dedup");
    }
    Initialize("{\"unknown\":\"ignored\",\"state.on\":\"First\",\"state.on\":\"Second\"}");
    Equal("First", ModText.Get("state.on"), "duplicate keeps first");
    Equal(2, warnings.Count, "unknown and duplicate warnings");
    Equal("", ModText.Get("unknown"), "unknown requested key");
    int warningCount = warnings.Count;
    ModText.Get("unknown");
    Equal(warningCount, warnings.Count, "unknown warning dedup");
    foreach (string template in new[] { "No parameters", "{0}", "{0} {2}", "{0} {1", "{0} {999999999999999999999}", "{0} {{1}}" })
    {
        Initialize(JsonSerializer.Serialize(new Dictionary<string, string> { ["control.field_value"] = template }));
        Equal("Name: Value", ModText.Format("control.field_value", "Name", "Value"), "invalid placeholders " + template);
        Equal(1, warnings.Count, "placeholder warning");
    }
    Initialize("{\"control.field_value\":\"{1} — {0}\",\"state.on\":\"Été 日本語 🐾\"}");
    Equal("Value — Name", ModText.Format("control.field_value", "Name", "Value"), "argument reorder");
    Equal("{1}{bad} — {0}", ModText.Format("control.field_value", "{0}", "{1}{bad}"), "input braces untouched");
    Equal("Été 日本語 🐾", ModText.Get("state.on"), "Unicode");
    Equal("", ModText.Format("control.field_value", "only one"), "missing caller argument fallback");
    Equal("", ModText.Format("control.field_value"), "omitted caller arguments fallback");
    Equal("", ModText.Format("unknown", "anything"), "unknown formatted key");
    Initialize("{\"state.on\":\"Cached\"}");
    Equal("Cached", ModText.Get("state.on"), "initial load");
    // Locate the owned fixture by content, without depending on enumeration order.
    string cachedDirectory = Directory.GetDirectories(fixtureRoot).Single(d => File.Exists(Path.Combine(d, "fr.json")) && File.ReadAllText(Path.Combine(d, "fr.json")) == "{\"state.on\":\"Cached\"}");
    File.WriteAllText(Path.Combine(cachedDirectory, "fr.json"), "{\"state.on\":\"Changed\"}");
    Equal("Cached", ModText.Get("state.on"), "catalog loaded once");
    string bomDirectory = Path.Combine(fixtureRoot, "bom");
    Directory.CreateDirectory(bomDirectory);
    File.WriteAllText(Path.Combine(bomDirectory, "fr.json"), "{\"state.on\":\"Été\"}", new System.Text.UTF8Encoding(true));
    ModText.Initialize(bomDirectory, warnings.Add);
    ModText.SetLanguage("fr");
    Equal("Été", ModText.Get("state.on"), "UTF-8 BOM external catalog");
    ModText.Initialize(Path.Combine(fixtureRoot, "missing"), warnings.Add);
    ModText.SetLanguage("fr");
    Equal("On", ModText.Get("state.on"), "missing directory");
    ModText.SetLanguage("zz-ZZ");
    Equal("On", ModText.Get("state.on"), "unknown culture English lookup");
    foreach (string invalid in new[] { "../es", "..\\es", "C:\\es", "/es", "es.json", "es\0", "es?" })
    {
        ModText.SetLanguage("es");
        ModText.SetLanguage(invalid);
        Equal("en", ModText.Language, "reject unsafe language " + invalid);
    }
    ModText.SetLanguage(" es_MX ");
    Equal("es-MX", ModText.Language, "normalize culture");
    Equal("Activado", ModText.Get("state.on"), "underscore parent fallback");
    ModText.Initialize(null, warnings.Add);
    var config = new ConfigFile();
    Localization._initialized = false;
    Localization.Current = "es";
    GameLocale.Initialize(config);
    Equal("en", ModText.Language, "auto before ready");
    Localization._initialized = true;
    Equal(true, GameLocale.Refresh(), "ready change");
    Equal("es", ModText.Language, "auto ready");
    Equal(false, GameLocale.Refresh(), "unchanged refresh");
    Localization.Current = "en";
    GameLocale.Refresh();
    Equal("en", ModText.Language, "game switch");
    config.Language.Value = "es";
    GameLocale.Refresh();
    Equal("es", ModText.Language, "explicit override");
    Localization.Current = "es-MX";
    GameLocale.Refresh();
    Equal("es", ModText.Language, "override ignores game");
    config.Language.Value = " AUTO ";
    GameLocale.Refresh();
    Equal("es-MX", ModText.Language, "revert auto");
    Localization.Throw = true;
    GameLocale.Refresh();
    Equal("es-MX", ModText.Language, "transient retains language");
    GameLocale.Refresh();
    Equal(1, Plugin.Log.Warnings.Count, "transient warns once");
    Localization.Throw = false;
    Localization.Current = "en";
    GameLocale.Refresh();
    Equal("en", ModText.Language, "recover after exception");
    Localization.Current = "game_spanish";
    GameLocale.Refresh();
    Equal("es", ModText.Language, "game language key mapping");
    Console.WriteLine($"Passed {checks} localization checks.");
}
finally
{
    string tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    if (!fixtureRoot.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) ||
        !Path.GetFileName(fixtureRoot).StartsWith("LeAccess.Localization.Tests-", StringComparison.Ordinal))
        throw new InvalidOperationException("Refusing to clean a non-owned fixture directory.");
    Directory.Delete(fixtureRoot, recursive: true);
}
