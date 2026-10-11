using LeAccess.Accessibility.Updates;
using LeAccess.I18n;

int checks = 0;
void Check(bool condition, string label) { checks++; if (!condition) throw new Exception(label); }

// Tags as releases are published ("v0.1.0") against the running plugin version ("0.2.0").
Check(UpdateRelease.IsNewer("v0.2.0", "0.1.0"), "a higher tag is newer");
Check(!UpdateRelease.IsNewer("v0.1.0", "0.1.0"), "the same version is not newer");
Check(!UpdateRelease.IsNewer("v0.1.0", "0.2.0"), "an older release is not newer");
Check(UpdateRelease.IsNewer("v0.10.0", "0.9.0"), "components compare as numbers, not text");
Check(!UpdateRelease.IsNewer("v0.2", "0.2.0"), "a missing patch number counts as zero");
Check(UpdateRelease.IsNewer("V1", "0.9.9"), "a bare major version parses");
Check(UpdateRelease.IsNewer("v0.2.1-test", "0.2.0") && !UpdateRelease.IsNewer("0.2.0+build5", "0.2.0"),
    "a suffix after the numbers is ignored");
foreach (string bad in new[] { null, "", "latest", "v", "version 2", "-1" })
    Check(!UpdateRelease.IsNewer(bad, "0.1.0"), $"an unreadable tag ({bad ?? "null"}) is never newer");
Check(!UpdateRelease.IsNewer("v9.0.0", "dev"), "an unreadable running version announces nothing");

Check(UpdateRelease.ReadTag("{\"tag_name\":\"v0.2.0\",\"draft\":false,\"prerelease\":false}") == "v0.2.0",
    "the tag of a published release is read");
Check(UpdateRelease.ReadTag("{\"tag_name\":\"v0.3.0\",\"prerelease\":true}") == null, "a pre-release is ignored");
Check(UpdateRelease.ReadTag("{\"tag_name\":\"v0.3.0\",\"draft\":true}") == null, "a draft is ignored");
foreach (string bad in new[] { null, "", "not json", "[]", "{\"message\":\"Not Found\"}", "{\"tag_name\":5}" })
    Check(UpdateRelease.ReadTag(bad) == null, $"an unexpected reply ({bad ?? "null"}) has no tag");

Check(UpdateRelease.Display("v0.2.0") == "0.2.0" && UpdateRelease.Display("0.2.0") == "0.2.0", "the v prefix is not spoken");
Check(UpdateRelease.Display("version") == "version", "a word starting with v is kept");

ModText.Initialize(null, null);
foreach (string language in new[] { "en", "es", "fr", "de", "pt", "pl", "ru", "ko", "zh", "ja" })
{
    ModText.SetLanguage(language);
    string text = ModText.Format("update.available", "0.2.0", "0.1.0");
    Check(text.Contains("0.2.0") && text.Contains("0.1.0"), $"{language}: the notice names both versions");
}
Console.WriteLine($"Update tests passed: {checks} checks");
