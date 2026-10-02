using LeAccess.Accessibility.MapSections;
using UnityEngine;
using UnityEngine.SceneManagement;
using EHG.Multiplayer;
using LE.Services;

int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
MapAnchor[] Anchors() => new[] { new MapAnchor("Root/A",0,0,0,0.1), new MapAnchor("Root/B",10,0,0,0.1), new MapAnchor("Root/C",0,0,10,0.1) };
SectionMap Map(MapAnchor[] anchors = null, string scene = "Fixed", string path = "Assets/Fixed.unity") =>
    new(scene,path,"verified-build",anchors ?? Anchors(),Array.Empty<MapSection>());
GameObject Reset()
{
    MapSectionWorld.Reset(); SceneList.Pcg = SceneList.Echo = GameplayEnvironment.IsOnlineClient = false;
    MapSectionSources.Reset();
    Time.unscaledTimeAsDouble = 0;
    SceneManager.Active = new Scene { handle = 10, name = "Fixed", path = "Assets/Fixed.unity", isLoaded = true, Valid = true };
    SceneManager.RootReads = 0; SceneManager.Roots = new();
    var root = new GameObject("Root") { scene = SceneManager.Active }; SceneManager.Roots.Items.Add(root);
    root.Child("A").transform.position = new Vector3(0,0,0);
    root.Child("B").transform.position = new Vector3(10,0,0);
    root.Child("C").transform.position = new Vector3(0,0,10);
    PlayerFinder.player = new GameObject("Player") { scene = SceneManager.Active };
    PlayerFinder.player.transform.position = new Vector3(2,3,4);
    PlayerFinder.playerActorComponent = PlayerFinder.player.Add(new Actor());
    PlayerFinder.playerActorVisuals = PlayerFinder.player.Add(new ActorVisuals());
    ServiceProvider.SceneVariantService = new VariantService();
    LeAccess.Plugin.Log.Warnings.Clear(); return root;
}
var root = Reset();
Check(MapSectionWorld.TryCapture(out var snapshot) && snapshot.X == 2 && snapshot.Y == 3 && snapshot.Z == 4 &&
    snapshot.BodyRadius == 0.5 && snapshot.Variant == null && snapshot.ScenePath == "Assets/Fixed.unity", "Finite fixed-scene player captured with actual offline body radius");
Check(SceneManager.RootReads == 0, "Capturing player never scans scene roots");
long session = snapshot.Session;
PlayerFinder.player = new GameObject("Replacement") { scene = SceneManager.Active };
PlayerFinder.playerActorComponent = PlayerFinder.player.Add(new Actor());
Check(MapSectionWorld.TryCapture(out snapshot) && snapshot.Session != session, "Player replacement changes session identity");
foreach (string invalid in new[] { "unloaded", "invalid scene", "pcg", "player collected", "player inactive", "player other scene", "actor collected", "actor inactive", "actor foreign", "actor other scene", "death", "nan", "infinite" })
{
    Reset();
    if (invalid == "unloaded") SceneManager.Active.isLoaded = false;
    if (invalid == "invalid scene") SceneManager.Active.Valid = false;
    if (invalid == "pcg") SceneList.Pcg = true;
    if (invalid == "player collected") PlayerFinder.player.WasCollected = true;
    if (invalid == "player inactive") PlayerFinder.player.Active = false;
    if (invalid == "player other scene") PlayerFinder.player.scene.handle++;
    if (invalid == "actor collected") PlayerFinder.playerActorComponent.WasCollected = true;
    if (invalid == "actor inactive") PlayerFinder.playerActorComponent.Enabled = false;
    if (invalid == "actor foreign") PlayerFinder.playerActorComponent = new GameObject { scene = SceneManager.Active }.Add(new Actor());
    if (invalid == "actor other scene") { PlayerFinder.playerActorComponent = PlayerFinder.player.Child("Actor").Add(new Actor()); PlayerFinder.playerActorComponent.gameObject.scene.handle++; }
    if (invalid == "death") PlayerFinder.playerActorComponent.Dying = true;
    if (invalid == "nan") PlayerFinder.player.transform.position = new Vector3(float.NaN,0,0);
    if (invalid == "infinite") PlayerFinder.player.transform.position = new Vector3(0,0,float.PositiveInfinity);
    Check(!MapSectionWorld.TryCapture(out _), $"Capture rejects {invalid}");
    Check(MapSectionWorld.TryCapturePlayer(out _) == (invalid == "pcg"),
        $"Unrestricted capture preserves player guards: {invalid}");
}
Reset(); SceneList.Echo = true;
Check(MapSectionWorld.TryCapture(out snapshot) && snapshot.IsFixedLayout,
    "Authored echo layouts may use source-guarded coordinate profiles");
SceneList.Pcg = true;
Check(!MapSectionWorld.TryCapture(out _) && MapSectionWorld.TryCapturePlayer(out snapshot) && !snapshot.IsFixedLayout,
    "Procedural echo cannot consume a fixed coordinate profile");
foreach (float radius in new[] { 0f,-1f,float.NaN,float.PositiveInfinity })
{ Reset(); PlayerFinder.playerActorComponent.Radius = radius; Check(MapSectionWorld.TryCapture(out snapshot) && snapshot.BodyRadius == 0, "Invalid radius does not guess a body size"); }
foreach (string online in new[] { "alive", "dead", "foreign", "collected", "other scene" })
{
    Reset(); GameplayEnvironment.IsOnlineClient = true;
    if (online == "dead") PlayerFinder.playerActorVisuals.dead = true;
    if (online == "foreign") PlayerFinder.playerActorVisuals = new GameObject { scene = SceneManager.Active }.Add(new ActorVisuals());
    if (online == "collected") PlayerFinder.playerActorVisuals.WasCollected = true;
    if (online == "other scene") { PlayerFinder.playerActorVisuals = PlayerFinder.player.Child("Visuals").Add(new ActorVisuals()); PlayerFinder.playerActorVisuals.gameObject.scene.handle++; }
    bool captured = MapSectionWorld.TryCapture(out snapshot);
    Check(online == "alive" ? captured && snapshot.BodyRadius == 0 : !captured, $"Online {online} visual ownership and death gate");
}
Reset(); ServiceProvider.SceneVariantService.HasVariant = true; ServiceProvider.SceneVariantService.Index = 2;
Check(MapSectionWorld.TryCapture(out snapshot) && snapshot.Variant == 2, "Actual scene variant is captured");
ServiceProvider.SceneVariantService.WasCollected = true;
Check(MapSectionWorld.TryCapture(out snapshot) && snapshot.Variant == null, "Unavailable variant remains unknown");

root = Reset(); MapSectionWorld.TryCapture(out snapshot); var map = Map();
Check(MapSectionWorld.ValidateAnchors(map,snapshot) && SceneManager.RootReads == 1, "Three exact non-collinear world anchors validate");
for (int i=0;i<20;i++) Check(MapSectionWorld.ValidateAnchors(map,snapshot), "Unchanged map uses cached validation");
Check(SceneManager.RootReads == 1, "Repeated frames do not scan scene roots");
Check(MapSectionWorld.ValidateAnchors(Map(),snapshot) && SceneManager.RootReads == 2, "Different profile identity validates independently");
map = Map(); MapSectionWorld.ValidateAnchors(map,snapshot); int scans = SceneManager.RootReads;
Check(MapSectionWorld.ValidateAnchors(map,snapshot with { Variant = 1 }) && SceneManager.RootReads == scans+1, "Variant change invalidates anchor cache");
MapSectionWorld.Reset(); Check(MapSectionWorld.ValidateAnchors(map,snapshot) && SceneManager.RootReads == scans+2, "Reset invalidates anchor cache");
foreach (string invalid in new[] { "scene name", "scene path", "snapshot stale", "missing root", "duplicate root", "missing child", "duplicate child", "wrong position", "wrong height", "inactive", "collected target", "collected roots", "few", "collinear", "duplicate profile path", "empty path segment" })
{
    root = Reset(); MapSectionWorld.TryCapture(out snapshot); map = Map();
    if (invalid == "scene name") map = Map(scene:"Foreign");
    if (invalid == "scene path") map = Map(path:"Assets/Other.unity");
    if (invalid == "snapshot stale") SceneManager.Active.handle++;
    if (invalid == "missing root") SceneManager.Roots.Items.Clear();
    if (invalid == "duplicate root") SceneManager.Roots.Items.Add(new GameObject("Root") { scene = SceneManager.Active });
    if (invalid == "missing child") root.Children.RemoveAt(1);
    if (invalid == "duplicate child") root.Child("A");
    if (invalid == "wrong position") root.Children[0].transform.position = new Vector3(1,0,0);
    if (invalid == "wrong height") root.Children[0].transform.position = new Vector3(0,1,0);
    if (invalid == "inactive") root.Active = false;
    if (invalid == "collected target") root.Children[0].transform.WasCollected = true;
    if (invalid == "collected roots") SceneManager.Roots.WasCollected = true;
    if (invalid == "few") map = Map(Anchors().Take(2).ToArray());
    if (invalid == "collinear") { var a = Anchors(); a[2] = new MapAnchor("Root/C",20,0,0,0.1); map = Map(a); }
    if (invalid == "duplicate profile path") { var a = Anchors(); a[2] = a[2] with { Path = "Root/A" }; map = Map(a); }
    if (invalid == "empty path segment") { var a = Anchors(); a[2] = a[2] with { Path = "Root//C" }; map = Map(a); }
    Check(!MapSectionWorld.ValidateAnchors(map,snapshot), $"Anchor validation rejects {invalid}");
}
root = Reset(); root.Children[0].transform.position = new Vector3(0.05f,0,0); MapSectionWorld.TryCapture(out snapshot);
Check(MapSectionWorld.ValidateAnchors(Map(),snapshot), "Each anchor accepts its own bounded tolerance");
root = Reset(); root.Children[0].transform.position = new Vector3(0.05f,0,0); MapSectionWorld.TryCapture(out snapshot);
var strict = Anchors(); strict[0] = strict[0] with { Tolerance = 0.01 };
Check(!MapSectionWorld.ValidateAnchors(Map(strict),snapshot), "Tolerances are per anchor, not a shared loose threshold");
root = Reset(); MapSectionWorld.TryCapture(out snapshot); map = Map();
var lateChild = root.Children[2]; root.Children.RemoveAt(2);
Check(!MapSectionWorld.ValidateAnchors(map,snapshot), "Incomplete asynchronous roots initially fail closed");
root.Children.Add(lateChild); Time.unscaledTimeAsDouble = 0.2;
Check(!MapSectionWorld.ValidateAnchors(map,snapshot) && SceneManager.RootReads == 1, "Failed discovery waits before rescanning");
Time.unscaledTimeAsDouble = 1;
Check(MapSectionWorld.ValidateAnchors(map,snapshot) && SceneManager.RootReads == 2, "Late anchor arrival retries and succeeds without scene or variant changes");
foreach (string mutation in new[] { "moved", "inactive", "destroyed", "collected", "renamed", "reparented" })
{
    root = Reset(); MapSectionWorld.TryCapture(out snapshot); map = Map(); MapSectionWorld.ValidateAnchors(map,snapshot);
    if (mutation == "moved") root.Children[0].transform.position = new Vector3(1,0,0);
    if (mutation == "inactive") root.Children[0].Active = false;
    if (mutation == "destroyed") root.Children[0].transform.Destroyed = true;
    if (mutation == "collected") root.Children[0].transform.WasCollected = true;
    if (mutation == "renamed") root.Children[0].name = "Foreign";
    if (mutation == "reparented") root.Children[0].Parent = new GameObject("Root").Child("Wrong level");
    Check(!MapSectionWorld.ValidateAnchors(map,snapshot) && SceneManager.RootReads == 1,
        $"Cached {mutation} anchor invalidates immediately without recursive discovery");
}
MapSection Section(string sourcePath = "Root/A") => new("surface","map.section.stairs",0,0,1,
    new[]{new MapPoint(0,0),new MapPoint(1,0),new MapPoint(1,1),new MapPoint(0,1)},source:
    new MapSectionSource(sourcePath,new double[]{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1},.01));
root=Reset(); MapSectionWorld.TryCapture(out snapshot); var section=Section();
Check(MapSectionSources.IsAvailable(section,snapshot),"live source with exact world matrix accepted");
Check(MapSectionSources.IsAvailable(section,snapshot) && SceneManager.RootReads==1,"section source cached without root scans");
foreach(string mutation in new[]{"inactive","destroyed","collected","renamed","moved","rotated","scaled","nonfinite","other scene","reparented"})
{
    root=Reset(); MapSectionWorld.TryCapture(out snapshot); section=Section(); MapSectionSources.IsAvailable(section,snapshot);
    var target=root.Children[0];
    if(mutation=="inactive")target.Active=false;
    if(mutation=="destroyed")target.transform.Destroyed=true;
    if(mutation=="collected")target.transform.WasCollected=true;
    if(mutation=="renamed")target.name="Changed";
    if(mutation=="moved")target.transform.localToWorldMatrix[0,3]=1;
    if(mutation=="rotated")target.transform.localToWorldMatrix[0,2]=1;
    if(mutation=="scaled")target.transform.localToWorldMatrix[0,0]=2;
    if(mutation=="nonfinite")target.transform.localToWorldMatrix[1,1]=float.NaN;
    if(mutation=="other scene")target.scene.handle++;
    if(mutation=="reparented")target.Parent=PlayerFinder.player;
    Check(!MapSectionSources.IsAvailable(section,snapshot),"cached source invalidates: "+mutation);
    Check(SceneManager.RootReads==1,"mutated source not scanned every observation: "+mutation);
}
root=Reset(); MapSectionWorld.TryCapture(out snapshot); section=Section(); root.Child("A");
Check(!MapSectionSources.IsAvailable(section,snapshot),"ambiguous duplicate source path rejected");
root=Reset(); MapSectionWorld.TryCapture(out snapshot); section=Section(); var delayed=root.Children[0];root.Children.RemoveAt(0);
Check(!MapSectionSources.IsAvailable(section,snapshot),"missing source rejected");root.Children.Add(delayed);
Time.unscaledTimeAsDouble=.2;
Check(!MapSectionSources.IsAvailable(section,snapshot)&&SceneManager.RootReads==1,"source retry throttled");
Time.unscaledTimeAsDouble=1;
Check(MapSectionSources.IsAvailable(section,snapshot)&&SceneManager.RootReads==2,"late source recovers");
snapshot=snapshot with{Variant=5};
Check(MapSectionSources.IsAvailable(section,snapshot)&&SceneManager.RootReads==3,"variant invalidates source lookup");
snapshot=snapshot with{ScenePath="Other"};
Check(!MapSectionSources.IsAvailable(section,snapshot),"source rejects stale scene snapshot");
foreach(string mutation in new[]{"valid","disabled","trigger","child inactive","child moved","resized","recentered","duplicate collider","missing collider","collected collider","child renamed"})
{
    root=Reset();MapSectionWorld.TryCapture(out snapshot);
    var floor=root.Children[0].Child("Floor");var box=floor.Add(new BoxCollider());
    var identity=new double[]{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1};
    var childSource=new MapSectionCollider(new MapSectionSource("Root/A/Floor",identity,.01),new double[]{0,0,0},new double[]{1,1,1});
    section=new MapSection("stairs","map.section.stairs",0,0,1,
        new[]{new MapPoint(0,0),new MapPoint(1,0),new MapPoint(1,1),new MapPoint(0,1)},source:
        new MapSectionSource("Root/A",identity,.01,new[]{childSource}));
    if(mutation=="valid")
    {
        SceneList.Echo=true;
        var echoMap=new SectionMap("Fixed","Assets/Fixed.unity","verified-build",Anchors(),new[]{section});
        Check(MapSectionWorld.ValidateAnchors(echoMap,snapshot),"Echo profile accepts complete structural and collider guards");
        var noColliderMap=new SectionMap("Fixed","Assets/Fixed.unity","verified-build",Anchors(),new[]{Section()});
        Check(!MapSectionWorld.ValidateAnchors(noColliderMap,snapshot),"Echo profile rejects transform-only guards");
        var unguarded=new MapSection("legacy","map.section.bridge",0,0,1,
            new[]{new MapPoint(0,0),new MapPoint(1,0),new MapPoint(1,1),new MapPoint(0,1)});
        var legacyMap=new SectionMap("Fixed","Assets/Fixed.unity","verified-build",Anchors(),new[]{unguarded});
        Check(!MapSectionWorld.ValidateAnchors(legacyMap,snapshot),"Echo profile rejects unguarded coordinate labels");
        Check(MapSectionWorld.ValidateAnchors(echoMap,snapshot),"Rejected weaker profiles do not poison guarded echo validation");
    }
    if(mutation=="duplicate collider")floor.Add(new BoxCollider());
    if(mutation=="missing collider")floor.Components.Clear();
    if(mutation=="duplicate collider"||mutation=="missing collider")
    {Check(!MapSectionSources.IsAvailable(section,snapshot),"discovery rejects "+mutation);continue;}
    Check(MapSectionSources.IsAvailable(section,snapshot),"source and collider initially match");
    if(mutation=="disabled")box.enabled=false;
    if(mutation=="trigger")box.isTrigger=true;
    if(mutation=="child inactive")floor.Active=false;
    if(mutation=="child moved")floor.transform.localToWorldMatrix[1,3]=1;
    if(mutation=="resized")box.size=new Vector3(2,1,1);
    if(mutation=="recentered")box.center=new Vector3(1,0,0);
    if(mutation=="collected collider")box.WasCollected=true;
    if(mutation=="child renamed")floor.name="Changed";
    Check(MapSectionSources.IsAvailable(section,snapshot)==(mutation=="valid"),"unchanged root cannot hide collider mutation: "+mutation);
}
Console.WriteLine($"Map section world tests passed: {checks} checks.");
