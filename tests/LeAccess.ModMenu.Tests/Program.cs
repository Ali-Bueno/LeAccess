using System.Text.Json;
using System.Text.RegularExpressions;
using LeAccess.Accessibility.ModMenu;
using LeAccess.I18n;

int checks = 0;
void Check(bool condition, string label) { checks++; if (!condition) throw new Exception(label); }
void Equal<T>(T expected, T actual, string label) =>
    Check(EqualityComparer<T>.Default.Equals(expected, actual), $"{label}: expected {expected}, got {actual}");

ModText.Initialize(null, _ => { });
ModText.SetLanguage("en");
ListPosition.Enabled = true;

// Navigation: no wrapping, a page opens on Cross, Back returns to the entry that opened it, Back on the first page closes.
var inner = new MenuPage("Inner", new MenuItem[] { new Plain("a"), new Plain("b") });
var root = new MenuPage("Root", new MenuItem[] { new Plain("first"), new PageItem(inner), new Plain("last") });
var navigator = new MenuNavigator(root);
Equal(ModText.Format("list.position", 1, 3), navigator.Focus()[(navigator.Focus().IndexOf(", ") + 2)..], "position follows the label");
navigator.Handle(MenuCommand.Previous);
Equal(0, navigator.Index, "no wrap at the top");
navigator.Handle(MenuCommand.Next);
Equal(ModText.Format("menu.submenu", "Inner"), navigator.Focus().Split(", ")[0] + ", " + navigator.Focus().Split(", ")[1], "a page entry says it is a submenu");
var opened = navigator.Handle(MenuCommand.Activate);
Check(opened.Speech.StartsWith("Inner. a"), "opening a page says its title and first entry");
Equal(1, navigator.Depth, "page pushed");
navigator.Handle(MenuCommand.Last);
Equal(1, navigator.Index, "Last");
navigator.Handle(MenuCommand.Next);
Equal(1, navigator.Index, "no wrap at the bottom");
var path = navigator.Path;
Equal("1,1", string.Join(",", path), "path from the first page");
var back = navigator.Handle(MenuCommand.Back);
Equal(0, navigator.Depth, "Back pops");
Equal(1, navigator.Index, "Back returns to the entry that opened the page");
Check(!back.Close && back.Speech.Contains("Inner"), "Back speaks the entry again");
Check(navigator.Handle(MenuCommand.Back).Close, "Back on the first page closes");
var rebuilt = new MenuNavigator(root);
rebuilt.Follow(path);
Equal(1, rebuilt.Depth, "Follow re-enters the page");
Equal(1, rebuilt.Index, "Follow restores the entry");
rebuilt.Follow(new[] { 9, 9 });
Check(rebuilt.Index >= 0, "Follow clamps a stale path");
Check(new MenuNavigator(new MenuPage("Empty", Array.Empty<MenuItem>())).Handle(MenuCommand.Next).Speech is null, "empty page is quiet");
ListPosition.Enabled = false;
Equal("first", new MenuNavigator(root).Focus(), "positions can be switched off");
ListPosition.Enabled = true;

// Booleans flip with Cross or either direction.
var flag = new FakeSetting(typeof(bool), true, true);
var toggle = new SettingItem(flag, new SettingSpec("Enabled", "x"), "Toggle", null);
Equal(ModText.Get("state.on"), toggle.Value, "on");
Equal(ModText.Get("state.off"), toggle.Activate().Speech, "Cross toggles");
toggle.Adjust(-1);
Equal(true, (bool)flag.Value, "left toggles too");

// Integers step and clamp to the option's own range.
var volume = new FakeSetting(typeof(int), 100, 100, (0, 100));
var percent = new SettingItem(volume, new SettingSpec("Volume", "v", SettingUnit.Percent, 5), "Volume", "Loudness");
Check(!percent.Adjust(1), "at the maximum nothing changes");
percent.Adjust(-1);
Equal(95, (int)volume.Value, "steps by five");
Equal(ModText.Format("menu.unit.percent", 95), percent.Value, "percent unit");
volume.Value = 3;
percent.Adjust(-1);
Equal(0, (int)volume.Value, "clamps at the minimum");
Check(percent.Reset() && (int)volume.Value == 100, "Triangle restores the default");
Check(percent.Details.Contains(ModText.Format("menu.default", ModText.Format("menu.unit.percent", 100))), "details give the default");
Check(percent.Details.Contains(ModText.Format("menu.range", ModText.Format("menu.unit.percent", 0), ModText.Format("menu.unit.percent", 100))), "details give the range");
Check(percent.Details.StartsWith("Loudness"), "details start with the description");

// Floats step on the step grid without drift; the catalog bounds apply when the option has none.
var multiplier = new FakeSetting(typeof(float), 1f, 1f);
var times = new SettingItem(multiplier, new SettingSpec("M", "m", SettingUnit.Times, 0.25, 0.5, 4), "Range", null);
for (int i = 0; i < 3; i++) times.Adjust(-1);
Equal(0.5f, (float)multiplier.Value, "float clamps to catalog minimum");
for (int i = 0; i < 5; i++) times.Adjust(1);
Equal(1.75f, (float)multiplier.Value, "float steps exactly");
var seconds = new FakeSetting(typeof(double), 0.2, 0.2);
var poll = new SettingItem(seconds, new SettingSpec("P", "p", SettingUnit.Seconds, 0.1, 0.1, 2), "Poll", null);
poll.Adjust(1);
Equal(0.3, (double)seconds.Value, "double avoids 0.30000000000000004");
var range = new FakeSetting(typeof(int), 12, 12, (0, 30));
var zero = new SettingItem(range, new SettingSpec("R", "r", SettingUnit.Metres, 1, ZeroText: "menu.value.game_range"), "Range", null);
range.Value = 0;
Equal(ModText.Get("menu.value.game_range"), zero.Value, "zero has its own words");

// Choices move without wrapping; the language list starts with automatic.
var language = new FakeSetting(typeof(string), "auto", "auto");
var choice = new SettingItem(language, new SettingSpec("Language", "language", Choices: ModMenuCatalog.Languages), "Language", null,
    code => "name:" + code);
Equal("name:auto", choice.Value, "choice text");
choice.Adjust(-1);
Equal("auto", (string)language.Value, "no wrap before the first");
choice.Activate();
Equal("en", (string)language.Value, "Cross moves to the next choice");
language.Value = "pt-BR";
choice.Adjust(1);
Equal("auto", (string)language.Value, "an unknown value restarts at the first");
var text = new FakeSetting(typeof(string), "{0}", "{0}");
var free = new SettingItem(text, new SettingSpec("F", "f"), "Format", null);
Check(!free.Adjust(1), "free text does not step");
Equal(ModText.Get("menu.edit_in_file"), free.Activate().Speech, "free text points to the file");

// The catalog covers every option the mod binds, and every text key exists.
string repo = AppContext.BaseDirectory;
while (!File.Exists(Path.Combine(repo, "LeAccess", "LeAccess.csproj"))) repo = Path.GetDirectoryName(repo)!;
var english = JsonSerializer.Deserialize<Dictionary<string, string>>(
    File.ReadAllText(Path.Combine(repo, "LeAccess", "Translations", "en.json")))!;
var listed = new HashSet<string>();
var ids = new HashSet<string>();
foreach (var section in ModMenuCatalog.Sections)
{
    Check(english.ContainsKey($"menu.section.{section.Id}"), $"section title {section.Id}");
    foreach (var spec in section.Options)
    {
        listed.Add($"{section.Section}/{spec.Key}");
        Check(ids.Add(spec.Id), $"unique id {spec.Id}");
        Check(english.ContainsKey($"menu.option.{spec.Id}") && english.ContainsKey($"menu.option.{spec.Id}.details"),
            $"texts for {spec.Id}");
    }
}
Check(english.ContainsKey($"menu.option.{ModMenuCatalog.MasterVolume.Id}.details"), "master volume texts");
foreach (string code in ModMenuCatalog.Languages)
    Check(english.ContainsKey($"menu.language.{code}"), $"language name {code}");
var bind = new Regex(@"\.Bind\(\s*([A-Za-z_]\w*|""[^""]*"")\s*,\s*""([^""]+)""");
int binds = 0;
foreach (string file in Directory.EnumerateFiles(Path.Combine(repo, "LeAccess"), "*.cs", SearchOption.AllDirectories))
{
    if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
    string source = File.ReadAllText(file);
    foreach (Match match in bind.Matches(source))
    {
        string section = match.Groups[1].Value, key = match.Groups[2].Value;
        if (section.StartsWith('"')) section = section.Trim('"');
        else
        {
            var constant = Regex.Match(source, $@"const string {section} = ""([^""]+)""");
            section = constant.Success ? constant.Groups[1].Value : null;
        }
        if (section != null && ModMenuCatalog.SoundPageSections.Contains(section)) continue;
        binds++;
        bool found = section != null ? listed.Contains($"{section}/{key}") : listed.Any(entry => entry.EndsWith("/" + key));
        Check(found, $"{section ?? "?"}/{key} ({Path.GetFileName(file)}) is in the menu catalog");
    }
}
Check(binds >= 50, $"found the mod's options ({binds})");

Console.WriteLine($"ModMenu tests passed: {checks} checks");

sealed class Plain : MenuItem
{
    private readonly string _label;
    internal Plain(string label) => _label = label;
    internal override string Label => _label;
}

sealed class FakeSetting : ISetting
{
    internal FakeSetting(Type type, object value, object defaultValue, (double, double)? range = null)
    { Type = type; Value = value; DefaultValue = defaultValue; Range = range; }
    public Type Type { get; }
    public object Value { get; set; }
    public object DefaultValue { get; }
    public (double Minimum, double Maximum)? Range { get; }
}
