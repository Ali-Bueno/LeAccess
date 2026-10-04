using System.Reflection;
using System.Text.Json;
using LeAccess.Accessibility.MapSections;
using LeAccess.I18n;

int checks = 0;
void Check(bool value,string message) { checks++; if (!value) throw new Exception(message); }
using var embedded = Assembly.GetExecutingAssembly().GetManifestResourceStream("LeAccess.MapSections.campaign.json");
Check(embedded is not null, "Actual shipped campaign profile is embedded with production resource name");
string json = new StreamReader(embedded).ReadToEnd();
using var document = JsonDocument.Parse(json);
Check(document.RootElement.GetProperty("maps").GetArrayLength() == 1, "Pilot ships exactly one evidence-backed fixed map");
var catalog = MapSectionCatalog.Parse(json);
const string build = SceneEvidence.BuildHash;
var map = catalog.Find("Z12",build);
Check(map is not null && map.Scene == "Z12" && map.ScenePath == "Assets/BundledScenes/Z12.unity" && map.BuildHash == build,
    "Actual profile matches independently recorded scene, serialized asset path and installed build");
Check(catalog.Find("Z12","other-build") == null && catalog.Find("OtherScene",build) == null,
    "Actual profile does not match another scene or build");
Check(catalog.Find("Z12",SceneEvidence.PreviousBuildHash) == null,
    "Refreshed pilot does not silently accept the previous installation build");
Check(map.Sections.Count == 2 && map.Sections.Select(s=>s.Id).Distinct().Count() == 2,
    "Actual data contains two distinct bridge union sections");
Check(map.Anchors.Length == 3 && map.Anchors.Select(a=>a.Path).Distinct().Count() == 3,
    "Three distinct static transform paths anchor the actual map");
Check(map.Anchors[0].Path.EndsWith("/Deco Folder/BoardedBridge") &&
    Math.Abs(map.Anchors[0].X-(-26.770000457763672)) < map.Anchors[0].Tolerance &&
    Math.Abs(map.Anchors[0].Y-(-0.17000000178813934)) < map.Anchors[0].Tolerance &&
    Math.Abs(map.Anchors[0].Z-22.040008544921875) < map.Anchors[0].Tolerance,
    "First bridge anchor agrees with extracted world transform");
Check(map.Anchors[1].Path.EndsWith("/Deco Folder/BoardedBridge (1)") &&
    Math.Abs(map.Anchors[1].X-(-31.970001220703125)) < map.Anchors[1].Tolerance &&
    Math.Abs(map.Anchors[1].Y-(-2.1700000762939453)) < map.Anchors[1].Tolerance &&
    Math.Abs(map.Anchors[1].Z-63.80999755859375) < map.Anchors[1].Tolerance,
    "Second bridge anchor agrees with extracted world transform");
Check(map.Anchors[2].Path.EndsWith("/MicroVerse/Terrain") && map.Anchors[2].X == -91 && map.Anchors[2].Y == -6 &&
    Math.Abs(map.Anchors[2].Z-(-76.699997)) < map.Anchors[2].Tolerance,
    "Third anchor uses persistent terrain instead of removable quest item");
var a = map.Anchors[0]; var b = map.Anchors[1]; var c = map.Anchors[2];
Check(Math.Abs((b.X-a.X)*(c.Z-a.Z)-(b.Z-a.Z)*(c.X-a.X)) > 0 && map.Anchors.All(v=>v.Tolerance == 0.01),
    "Actual three-anchor basis is non-collinear with tight per-anchor tolerance");
foreach (string locale in new[] { "en","es" })
{
    ModText.SetLanguage(locale);
    string first = ModText.Get("map.section.oldroad_bridge_1"), second = ModText.Get("map.section.oldroad_bridge_2");
    Check(!first.Contains("map.section") && !second.Contains("map.section") && first != second,
        $"Actual {locale} descriptive section keys resolve distinctly");
    Check(first.EndsWith("1") && second.EndsWith("2"), $"Actual {locale} descriptions keep their bridge numbering");
}
ModText.SetLanguage("en");
foreach (var p in SceneEvidence.FirstDeck.Concat(SceneEvidence.FirstSeams))
    Check(map.FindSection(p.X,p.Y,p.Z)?.Id == "oldroad_bridge_1", "Extracted first-deck floor/overlap point selects first bridge");
foreach (var p in SceneEvidence.SecondDeck.Concat(SceneEvidence.SecondSeams))
    Check(map.FindSection(p.X,p.Y,p.Z)?.Id == "oldroad_bridge_2", "Extracted second-deck floor/overlap point selects second bridge");
foreach (var p in new[] { (-35d,0.07484054565429688,23d),(-18d,0.07484054565429688,23d),
    (-38d,-1.9251594543457031,64d),(-27d,-1.9251594543457031,64d),(-30d,0d,45d) })
    Check(map.FindSection(p.Item1,p.Item2,p.Item3) == null, "Banks beside decks and space between bridges remain unlabeled");
foreach (var p in new[] { SceneEvidence.FirstDeck[0],SceneEvidence.SecondDeck[0] })
{
    Check(map.FindSection(p.X,p.Y-1,p.Z) == null, "Floor below actual bridge is rejected");
    Check(map.FindSection(p.X,p.Y+1,p.Z) == null, "Floor above actual bridge is rejected");
}
var state = new MapSectionState(); var messages = new List<string>();
void Visit((double X,double Y,double Z) point)
{
    var section = map.FindSection(point.X,point.Y,point.Z);
    for (int i=0;i<2;i++) if (state.Observe(1,section,true,0,out string key)) messages.Add(ModText.Get(key));
}
Visit((-35,0.07484054565429688,23));
foreach (var point in SceneEvidence.FirstDeck.Concat(SceneEvidence.FirstSeams)) Visit(point);
Check(messages.SequenceEqual(new[] { ModText.Get("map.section.oldroad_bridge_1") }), "Crossing all first bridge collider seams produces one announcement");
Visit((-30,0,45));
foreach (var point in SceneEvidence.SecondDeck.Concat(SceneEvidence.SecondSeams)) Visit(point);
Check(messages.SequenceEqual(new[] { ModText.Get("map.section.oldroad_bridge_1"),ModText.Get("map.section.oldroad_bridge_2") }), "Approach, first deck, distant exit and second deck produce exactly two announcements");
Visit((-30,0,45)); Visit(SceneEvidence.FirstDeck[0]);
Check(messages.SequenceEqual(new[] { ModText.Get("map.section.oldroad_bridge_1"),ModText.Get("map.section.oldroad_bridge_2"),ModText.Get("map.section.oldroad_bridge_1") }),
    "Returning after a stable outside interval reannounces the actual first bridge exactly once");
// Re-express the same extracted decks as components; migrating the format must preserve evidence.
var multipartData = System.Text.Json.Nodes.JsonNode.Parse(json);
foreach (var profileNode in multipartData["maps"].AsArray())
    foreach (var sectionNode in profileNode["sections"].AsArray())
    {
        if (sectionNode["components"] != null) continue;
        var component = new System.Text.Json.Nodes.JsonObject();
        foreach (string field in new[]{"minY","maxY","polygon"})
        {
            var value = sectionNode[field];
            sectionNode.AsObject().Remove(field);
            component[field] = value;
        }
        sectionNode["components"] = new System.Text.Json.Nodes.JsonArray(component);
    }
var multipartMap = MapSectionCatalog.Parse(multipartData.ToJsonString()).Find("Z12",build);
foreach (var point in SceneEvidence.FirstDeck.Concat(SceneEvidence.FirstSeams).Concat(SceneEvidence.SecondDeck).Concat(SceneEvidence.SecondSeams))
    Check(multipartMap.FindSection(point.X,point.Y,point.Z)?.Id == map.FindSection(point.X,point.Y,point.Z)?.Id,
        "Migrating authored geometry to components preserves extracted deck/overlap selection");
GeneratedData.Check(Check);
Console.WriteLine($"Map section data tests passed: {checks} checks.");
