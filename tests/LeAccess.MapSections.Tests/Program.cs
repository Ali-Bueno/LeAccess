using BepInEx.Configuration;
using LeAccess;
using LeAccess.Accessibility;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation;
using LeAccess.I18n;
using UnityEngine;

int checks = 0;
ConfigFile config = null;
string fixture = Path.GetFullPath(Path.Combine(Path.GetTempPath(),"LeAccess.MapSections.Tests-"+Guid.NewGuid().ToString("N")));
string originalDataPath = Application.dataPath;
Directory.CreateDirectory(fixture);
void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
void Start(string hash = "verified-build", bool overview = false, bool surfaces = false)
{
    MapSectionAccessibility.Cleanup();
    Application.dataPath = fixture;
    string hashFile = Path.Combine(fixture,"build_hash.txt");
    if (hash == null) File.Delete(hashFile); else File.WriteAllText(hashFile,hash);
    ModText.Initialize(null,null);
    config = new ConfigFile();
    Application.isFocused = ScreenReader.Available = GameplayInputState.Gameplay = MapSectionWorld.Available = MapSectionWorld.AnchorsValid = true;
    LE.UI.LoadingScreen.IsVisible = NavigationAudioState.IsSuspended = MapSectionWorld.Throw = false;
    MapSectionWorld.Captures = MapSectionWorld.Validations = MapSectionWorld.Resets = 0;
    PcgMapSections.Available = false;
    RuntimeMapPlaces.Available = false;
    RuntimeMapPlaces.Captures = 0;
    RuntimeMapPlaces.Place = new("bridge-owner", "map.section.bridge", "map.section.bridge");
    RuntimeSceneryPlaces.Available = false;
    RuntimeSceneryPlaces.Captures = RuntimeSceneryPlaces.Resets = 0;
    RuntimeSceneryPlaces.Place = new("gate-owner", "map.place.gate", "map.place.gate");
    MapSectionSources.Available = true;
    MapSurfaceProbe.Available = false;
    MapSurfaceProbe.Key = "map.surface.wood";
    PcgMapSections.Snapshot = new(1,"room-0",1,true,false,null,"map.section.cave_room");
    MapSectionWorld.Snapshot = new(1,1,"ControlledTown","Assets/ControlledTown.unity",2,5,2,.5,0);
    Time.unscaledTimeAsDouble = 0;
    ScreenReader.Spoken.Clear(); Plugin.Log.Warnings.Clear(); Plugin.Log.Information.Clear();
    MapSectionAccessibility.Initialize(config);
    if (overview) config.Describe.Value = true;
    if (surfaces) config.Surfaces.Value = true;
}
void Tick(double time, bool menu = false, bool pending = false, bool spoke = false)
{
    Time.unscaledTimeAsDouble = time;
    ScreenReader.SpokeThisFrame = spoke;
    MapSectionAccessibility.Update(menu,pending);
}
void Move(double x, double y = 5, double z = 2) => MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {X=x,Y=y,Z=z};
void Last(string text, string name) => Check(ScreenReader.Spoken.Count > 0 && ScreenReader.Spoken.Last() == (text,false),name);
try
{
    Start(" verified-build \n");
    Check(config.Enabled.Value && !config.Describe.Value && !config.Surfaces.Value,"defaults announce local sections only");
    Tick(0);
    Check(ScreenReader.Spoken.Count == 0 && MapSectionAccessibility.HasPendingContent,"first section observation settles");
    Tick(.199);
    Check(MapSectionWorld.Captures == 1 && ScreenReader.Spoken.Count == 0,"200ms throttle");
    Tick(.2);
    Last("Wooden bridge 1","matching observations queued delivery English");
    Check(!MapSectionAccessibility.HasPendingContent && Plugin.Log.Information.Count == 1 &&
        Plugin.Log.Information[0].Contains("scene=ControlledTown section=bridge1"),"delivery-only diagnostic");
    Tick(.4); Tick(.61);
    Check(ScreenReader.Spoken.Count == 1,"unchanged section deduplicated");
    Move(4.1); Tick(.82); Tick(1.03);
    Check(ScreenReader.Spoken.Count == 1,"body radius retains previous region near boundary");
    Move(4.3); Tick(1.24);
    Check(ScreenReader.Spoken.Count == 1 && MapSectionAccessibility.HasPendingContent,"strict different region overrides old margin");
    Tick(1.45);
    Last("Wooden bridge 2","second region delivered");
    Move(10); Tick(1.66); Tick(1.87);
    Check(!MapSectionAccessibility.HasPendingContent,"confirmed outside clears current section");
    Move(2); Tick(2.08); Tick(2.29);
    Check(ScreenReader.Spoken.Count == 3,"confirmed outside rearms section return");

    Start(); Tick(0); Move(6); Tick(.2); Tick(.41);
    Check(ScreenReader.Spoken.Count == 1,"only newest pending section speaks");
    Last("Wooden bridge 2","old pending bridge discarded");
    Start(); Tick(0,pending:true); Tick(.2,pending:true); Move(6); Tick(.41); Tick(.62);
    Last("Wooden bridge 2","deferred section revalidated before speech");
    Check(ScreenReader.Spoken.Count == 1,"no older deferred bridge");

    foreach (string gate in new[] {"menu","pause","input"})
    {
        Start(); Tick(0); Tick(.2);
        NavigationAudioState.IsSuspended = gate == "pause"; GameplayInputState.Gameplay = gate != "input";
        Tick(.4,menu:gate == "menu");
        Check(!MapSectionAccessibility.HasPendingContent && MapSectionWorld.Captures == 2,"no background pending/capture " + gate);
        NavigationAudioState.IsSuspended = false; GameplayInputState.Gameplay = true;
        Tick(.41); Tick(.62);
        Check(ScreenReader.Spoken.Count == 1,"delivered section preserved on " + gate);
    }
    Start(); Tick(0); Tick(.01,menu:true); Move(6); Tick(.02);
    Check(ScreenReader.Spoken.Count == 0,"return from menu settles latest candidate");
    Tick(.23);
    Last("Wooden bridge 2","menu never resumes old section");
    Start(); Tick(0); Tick(.01,menu:true); Move(10); Tick(.02); Tick(.23);
    Check(ScreenReader.Spoken.Count == 0 && !MapSectionAccessibility.HasPendingContent,"outside after menu clears deferred section");

    Start(); Tick(0); Tick(.2);
    MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {Session=2}; Tick(.41); Tick(.62);
    Check(ScreenReader.Spoken.Count == 2,"new player session announces current region anew");
    MapSectionWorld.Snapshot = new(3,2,"ControlledOther","Assets/ControlledOther.unity",2,5,2,.5,0);
    Tick(.83); Tick(1.04);
    Check(ScreenReader.Spoken.Count == 2 && !MapSectionAccessibility.HasPendingContent,"new profile does not reuse previous polygon");
    Move(22,z:22); Tick(1.25); Tick(1.46);
    Last("Wooden bridge 2","new profile uses own current section");

    foreach (string mismatch in new[] {"build","scene","path"})
    {
        Start(mismatch == "build" ? "wrong-build" : "verified-build");
        if (mismatch == "scene") MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {SceneName="controlledtown"};
        if (mismatch == "path") MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {ScenePath="Assets/Wrong.unity"};
        Tick(0); Tick(.2);
        Check(ScreenReader.Spoken.Count == 0 && !MapSectionAccessibility.HasPendingContent,"exact profile guard " + mismatch);
    }
    Start(null); Tick(0); Tick(.2);
    Check(ScreenReader.Spoken.Count == 0 && MapSectionWorld.Validations == 0 && Plugin.Log.Warnings.Count == 1,"missing build hash disables fixed catalog with one warning");

    Start(); Tick(0); MapSectionWorld.AnchorsValid = false; Tick(.2); Tick(.41);
    Check(ScreenReader.Spoken.Count == 0 && !MapSectionAccessibility.HasPendingContent && Plugin.Log.Warnings.Count == 1,"failed anchors clear pending and dedup warning");
    MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {Variant=1}; Tick(.62);
    Check(Plugin.Log.Warnings.Count == 2,"new variant guard logged");
    MapSectionWorld.AnchorsValid = true; Tick(.83); Tick(1.04);
    Last("Wooden bridge 1","anchors recovery settles current section");
    MapSectionWorld.Available = false; Tick(1.25);
    Check(!MapSectionAccessibility.HasPendingContent,"missing player/world clears pending");
    MapSectionWorld.Available = true; Tick(1.46); Tick(1.67);
    Check(ScreenReader.Spoken.Count == 2,"world return starts fresh section state");

    foreach (string gate in new[] {"disabled","loading","focus"})
    {
        Start(); Tick(0); Tick(.2);
        config.Enabled.Value = gate != "disabled"; LE.UI.LoadingScreen.IsVisible = gate == "loading"; Application.isFocused = gate != "focus";
        Tick(.4);
        Check(!MapSectionAccessibility.HasPendingContent && MapSectionWorld.Captures == 2,"hard gate clears state " + gate);
        config.Enabled.Value = Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = false;
        Tick(.41); Check(ScreenReader.Spoken.Count == 1,"hard gate recovery settles " + gate);
        Tick(.62); Check(ScreenReader.Spoken.Count == (gate == "focus" ? 1 : 2),
            gate == "focus" ? "alt-tab keeps delivered places silent" : "hard gate recovery delivers " + gate);
    }
    Start(); Tick(0); Tick(.2,pending:true); Tick(.41,spoke:true);
    Check(ScreenReader.Spoken.Count == 0 && MapSectionAccessibility.HasPendingContent,"zone/current-frame speech gets priority");
    Tick(.62);
    Last("Wooden bridge 1","priority-delayed current section queued");
    Start(); ScreenReader.Available = false; Tick(0); Tick(.2);
    Check(ScreenReader.Spoken.Count == 0 && MapSectionAccessibility.HasPendingContent,"reader unavailable defers current section");
    ScreenReader.Available = true; Tick(.41);
    Last("Wooden bridge 1","available reader receives current section");
    Start(); ModText.SetLanguage("es"); Tick(0); Tick(.2);
    Last("Puente de madera 1","actual Spanish section label");
    Move(6); Tick(.41); Tick(.62);
    Last("Puente de madera 2","second Spanish section label");
    Start(); MapSectionWorld.Throw = true; Tick(0); Tick(.2);
    Check(Plugin.Log.Warnings.Count == 1 && !MapSectionAccessibility.HasPendingContent,"repeated capture failure warning dedup/reset");
    MapSectionWorld.Throw = false; Tick(.21); Tick(.42);
    Last("Wooden bridge 1","capture exception recovery");
    MapSectionAccessibility.Cleanup(); int captures = MapSectionWorld.Captures;
    Check(!MapSectionAccessibility.HasPendingContent,"cleanup clears pending");
    Tick(.63);
    Check(MapSectionWorld.Captures == captures && ScreenReader.Spoken.Count == 1,"cleanup disables future captures");

    Start(); ModText.SetLanguage("es"); PcgMapSections.Available = true;
    MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {IsFixedLayout=false};
    Tick(0); Tick(.2,pending:true);
    Check(ScreenReader.Spoken.Count == 0 && MapSectionAccessibility.HasPendingContent,"generated room yields to zone speech");
    Tick(.41);
    Last("Cámara de la cueva 1. Entrada","generated room and entrance are localized");
    Check(MapSectionWorld.Validations == 0,"generated layout does not consult fixed anchors");
    Tick(.62,menu:true); Tick(.63); Tick(.84);
    Check(ScreenReader.Spoken.Count == 1,"generated room not repeated after menu");
    PcgMapSections.Available = false; Tick(1.05);
    Check(!MapSectionAccessibility.HasPendingContent,"unclassified cell cancels pending");
    PcgMapSections.Available = true; Tick(1.26); Tick(1.47);
    Check(ScreenReader.Spoken.Count == 1,"one uncertain floor sample does not repeat room");
    PcgMapSections.Available = false; Tick(1.68); Tick(1.89);
    PcgMapSections.Available = true; Tick(2.10); Tick(2.31);
    Check(ScreenReader.Spoken.Count == 1,"brief confirmed departure does not repeat generated room");
    PcgMapSections.Available = false; Tick(2.52); Tick(2.73);
    PcgMapSections.Available = true; Tick(8.0); Tick(8.21);
    Check(ScreenReader.Spoken.Count == 2,"departure of at least the repeat gap rearms generated room");
    PcgMapSections.Snapshot = new(1,"room-1",2,false,true,null,"map.section.tomb_room");
    Tick(8.42); Tick(8.63);
    Last("Cámara de la tumba 2. Salida","new room and exit spoken once");
    Check(!ScreenReader.Spoken.Last().Text.Contains("Conexiones"),"unverified connection count remains absent");
    PcgMapSections.Snapshot = PcgMapSections.Snapshot with {Session=2}; Tick(8.94); Tick(9.15);
    Check(ScreenReader.Spoken.Count == 3,"same text in a new generation stays silent without a world session change");
    PcgMapSections.Snapshot = new(2,"room-2",3,false,false,3,null);
    Tick(9.36,pending:true); Tick(9.57,pending:true);
    PcgMapSections.Snapshot = new(2,"room-3",4,false,false,null,null);
    Tick(9.78); Tick(9.99);
    Last("Sala 4","latest generated location replaces pending room");
    Check(ScreenReader.Spoken.All(s => !s.Text.Contains("Sala 3")),"no stale generated room delivered");
    ModText.SetLanguage("en"); Tick(10.20); Tick(10.41);
    Last("Room 4","language changes settle current room text");
    PcgMapSections.Snapshot=PcgMapSections.Snapshot with{ConnectedPassages=3};
    Tick(10.62);Tick(10.83);
    Last("Room 4. Connections: 3","verified generated links use neutral wording without claiming open passages");

    Start(null); PcgMapSections.Available = true;
    MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {IsFixedLayout=false};
    Tick(0); Tick(.2);
    Last("Cave chamber 1. Entrance","procedural data remains available without fixed build catalog");
    Start(); Tick(0); Tick(.2); PcgMapSections.Available = true;
    MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {IsFixedLayout=false};
    Tick(.41); Tick(.62);
    Last("Cave chamber 1. Entrance","fixed to generated transition chooses current source");
    PcgMapSections.Available = false;
    MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {IsFixedLayout=true};
    Tick(.83); Tick(1.04);
    Last("Wooden bridge 1","return to fixed layout resets generated identity");

    Start(); MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {SceneName="GeneratedCatalog",ScenePath="Assets/GeneratedCatalog.unity"};
    Tick(0); Tick(.2);
    Last("Stairs","all map resource files are loaded");
    Start(overview:true); ModText.SetLanguage("es");
    Check(config.Describe.Value,"map overview can be explicitly enabled");
    Move(20); Tick(0,pending:true); Tick(.2,pending:true);
    Check(ScreenReader.Spoken.Count == 0 && MapSectionAccessibility.HasPendingContent,"map overview waits for scene name");
    Tick(.41);
    Last("Referencias del mapa: Puente","overview describes verified features even outside any section");
    Tick(.62); Tick(.83,menu:true); Tick(.84); Tick(1.05);
    Check(ScreenReader.Spoken.Count == 1,"overview not repeated when standing or closing menu");
    Move(2); Tick(1.26); Tick(1.47);
    Last("Puente de madera 1","section follows one-time overview");
    Start(overview:true); Tick(0); Tick(.2);
    Last("Map landmarks: Bridge","overview has priority when loading inside a section");
    Check(MapSectionAccessibility.HasPendingContent,"section remains pending after overview");
    Tick(.41);
    Last("Wooden bridge 1","section delivered after overview without interruption");
    Start(overview:true); MapSectionWorld.AnchorsValid = false; Tick(0); Tick(.2);
    Check(ScreenReader.Spoken.Count == 0,"unverified layout has no overview");
    Start(); Tick(0); MapSectionSources.Available=false; Tick(.2); Tick(.41);
    Check(ScreenReader.Spoken.Count==0&&!MapSectionAccessibility.HasPendingContent,"hidden or changed source cancels pending section");
    Start(); Tick(0); Tick(.2); MapSectionSources.Available=false; Move(4.1); Tick(.41); Tick(.62);
    Check(!MapSectionAccessibility.HasPendingContent,"hysteresis cannot retain an invalid source");
    Start(overview:true); MapSectionSources.Available=false; Tick(0); Tick(.2);
    Check(ScreenReader.Spoken.Count==0,"overview lists only available source categories");

    Start(surfaces:true); ModText.SetLanguage("es"); MapSurfaceProbe.Available=true;
    MapSectionWorld.Snapshot=MapSectionWorld.Snapshot with{SceneName="Unmapped"};
    Check(config.Surfaces.Value,"surface descriptions can be explicitly enabled");
    Tick(0); Tick(.2);
    Last("Suelo de madera","ground works on maps without fixed profiles");
    Tick(.41); MapSurfaceProbe.Available=false; Tick(.62); Tick(.83);
    Check(!MapSectionAccessibility.HasPendingContent,"unknown ground cancels stale pending description");
    MapSurfaceProbe.Available=true; Tick(1.04); Tick(1.25);
    Check(ScreenReader.Spoken.Count==1,"unknown foot samples do not repeat previous ground");
    MapSurfaceProbe.Key="map.surface.water"; Tick(1.46,pending:true); Tick(1.67,pending:true);
    MapSurfaceProbe.Key="map.surface.grass"; Tick(1.88); Tick(2.09);
    Last("Suelo de hierba","newest surface replaces stale queued water");
    Check(ScreenReader.Spoken.All(s=>s.Text!="Agua bajo los pies"),"old surface never delivered");
    Tick(2.30,menu:true); Tick(2.31); Tick(2.52);
    Check(ScreenReader.Spoken.Count==2,"menu close does not repeat ground");
    config.Surfaces.Value=false; MapSurfaceProbe.Key="map.surface.snow"; Tick(2.73); Tick(2.94);
    Check(ScreenReader.Spoken.Count==2&&!MapSectionAccessibility.HasPendingContent,"surface option disables only surface speech");
    Start(overview:true,surfaces:true); MapSurfaceProbe.Available=true; Tick(0); Tick(.2);
    Last("Map landmarks: Bridge","overview precedes section and surface");
    Tick(.41); Last("Wooden bridge 1","section precedes surface");
    Tick(.62); Last("Wooden ground","surface follows higher-priority map descriptions");
    Check(ScreenReader.Spoken.Count==3&&!MapSectionAccessibility.HasPendingContent,"all current descriptions delivered once");
    foreach (string sourceFailure in new[] {"unmapped", "build", "path", "anchors", "source", "procedural"})
    {
        Start(sourceFailure == "build" ? null : "verified-build");
        RuntimeMapPlaces.Available = true;
        ModText.SetLanguage("es");
        if (sourceFailure == "unmapped") MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {SceneName="Unmapped"};
        if (sourceFailure == "path") MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {ScenePath="Assets/Other.unity"};
        if (sourceFailure == "anchors") MapSectionWorld.AnchorsValid = false;
        if (sourceFailure == "source") MapSectionSources.Available = false;
        if (sourceFailure == "procedural") MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {IsFixedLayout=false};
        Tick(0); Tick(.2);
        Last("Puente", "live source independent of unavailable catalog: " + sourceFailure);
        Tick(.41); Tick(.62);
        Check(ScreenReader.Spoken.Count == 1, "live structure remains quiet: " + sourceFailure);
    }
    Start(); RuntimeMapPlaces.Available = true; Tick(0); Tick(.2);
    Last("Wooden bridge 1", "specific verified name wins over generic live category");
    Check(RuntimeMapPlaces.Captures == 2, "live contacts still inspect parts inside a broad mapped structure");
    RuntimeMapPlaces.Place = new("bridge-stairs", "map.section.stairs", "map.section.stairs");
    Tick(.41); Tick(.62);
    Last("Stairs", "live stairs inside a broader fixed bridge have their own place announcement");
    RuntimeMapPlaces.Place = new("bridge-floor", "map.section.bridge", "map.section.bridge");
    Tick(.83); Tick(1.04); Last("Wooden bridge 1", "matching bridge contact preserves the specific verified name");
    Move(4.1); RuntimeMapPlaces.Place = new("stairs-owner", "map.section.stairs", "map.section.stairs");
    Tick(1.25); Tick(1.46);
    Last("Stairs", "actual new live place wins over old polygon hysteresis");

    Start(); MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {SceneName="GeneratedCatalog",ScenePath="Assets/GeneratedCatalog.unity"};
    Tick(0); Tick(.2); MapSectionSources.Available = false; RuntimeMapPlaces.Available = true;
    RuntimeMapPlaces.Place = new("live-stair", "map.section.stairs", "map.section.stairs");
    Tick(.41); Tick(.62);
    Check(ScreenReader.Spoken.Count == 1, "matching live and fixed sections share speech identity");
    RuntimeMapPlaces.Place = RuntimeMapPlaces.Place with {Identity="next-stair-piece"};
    Tick(.83); Tick(1.04);
    Check(ScreenReader.Spoken.Count == 1, "seams between matching runtime structures stay silent");
    RuntimeMapPlaces.Available = false; Tick(1.25);
    RuntimeMapPlaces.Available = true; Tick(1.46); Tick(1.67);
    Check(ScreenReader.Spoken.Count == 1, "single missing foot observation does not repeat place");
    RuntimeMapPlaces.Available = false; Tick(1.88); Tick(2.09);
    RuntimeMapPlaces.Available = true; Tick(2.30); Tick(2.51);
    Check(ScreenReader.Spoken.Count == 1, "brief confirmed departure does not repeat live structure");
    RuntimeMapPlaces.Available = false; Tick(2.72); Tick(2.93);
    RuntimeMapPlaces.Available = true; Tick(8.0); Tick(8.21);
    Check(ScreenReader.Spoken.Count == 2, "departure of at least the repeat gap rearms live structure");

    Start(); MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {SceneName="ControlledCourtyard",ScenePath="Assets/ControlledCourtyard.unity"};
    ModText.SetLanguage("es"); RuntimeMapPlaces.Available = true;
    RuntimeMapPlaces.Place = new("courtyard", "map.section.courtyard", "live:courtyard");
    Tick(0); Tick(.2); Last("Patio", "explicit live place owner overrides generic walkway floor pieces");
    RuntimeMapPlaces.Available = false; Tick(.41); Tick(.62);
    Last("Pasarela", "verified walkway remains available when contact reading is absent");

    Start(); MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {IsFixedLayout=false};
    RuntimeMapPlaces.Available = PcgMapSections.Available = true;
    Tick(0); Tick(.2); Last("Bridge", "live structure is read within a generated room");
    RuntimeMapPlaces.Available = false; Tick(.41); Tick(.62);
    Last("Cave chamber 1. Entrance", "leaving structure exposes current native room");
    ModText.SetLanguage("es");
    PcgMapSections.Snapshot = new(1, "hallway-1", 5, false, false, null, "map.section.passage");
    Tick(.83); Tick(1.04); Last("Pasillo", "native hallway speaks concrete place without room number");
    PcgMapSections.Snapshot = PcgMapSections.Snapshot with {SectionId="hallway-2", Number=6};
    Tick(1.25); Tick(1.46);
    Check(ScreenReader.Spoken.Count == 3, "adjoining hallway pieces share speech identity");
    PcgMapSections.Snapshot = PcgMapSections.Snapshot with {Session=2};
    Tick(1.67); Tick(1.88);
    Check(ScreenReader.Spoken.Count == 3, "regenerated hallway with identical text stays silent");

    Start(); MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {SceneName="Unmapped"};
    RuntimeMapPlaces.Available = true;
    Tick(0,pending:true); Tick(.2,pending:true);
    Check(ScreenReader.Spoken.Count == 0 && MapSectionAccessibility.HasPendingContent, "live place yields to pending zone speech");
    RuntimeMapPlaces.Place = new("walkway-owner", "map.section.walkway", "map.section.walkway");
    Tick(.41); Tick(.62); Last("Walkway", "live location replaces stale pending place");
    Check(ScreenReader.Spoken.Count == 1, "old deferred bridge never spoken");
    int liveCaptures = RuntimeMapPlaces.Captures;
    Tick(.83,menu:true); Check(RuntimeMapPlaces.Captures == liveCaptures, "no live probes in menus");
    Tick(.84); Tick(1.05); Check(ScreenReader.Spoken.Count == 1, "menu return does not repeat live place");
    MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with {Session=2};
    Tick(1.26); Tick(1.47); Check(ScreenReader.Spoken.Count == 2, "new player session rearms live place");
    ModText.SetLanguage("es"); Tick(1.68); Tick(1.89); Last("Pasarela", "live place follows game language");
    Start(); ModText.SetLanguage("es"); RuntimeSceneryPlaces.Available = true;
    Tick(0); Tick(.2); Last("Puente de madera 1", "actual floor announcement precedes nearby scenery");
    Check(MapSectionAccessibility.HasPendingContent, "nearby scenery waits while physical section speaks");
    Tick(.41); Last("Cerca: Pórtico", "scenery complements existing map and physical floor coverage");
    Check(ScreenReader.Spoken.Count == 2 && !MapSectionAccessibility.HasPendingContent, "nearby scenery delivered once without interrupting section");
    RuntimeSceneryPlaces.Place = RuntimeSceneryPlaces.Place with { Identity = "next-gate" };
    Tick(.62); Tick(.83); Check(ScreenReader.Spoken.Count == 2, "adjacent gateway pieces do not repeat category");
    RuntimeSceneryPlaces.Place = new("wagon", "map.place.wagon", "map.place.wagon");
    Tick(1.04, pending:true); Tick(1.25, pending:true);
    Check(MapSectionAccessibility.HasPendingContent && ScreenReader.Spoken.Count == 2, "nearby changes yield to pending gameplay speech");
    RuntimeSceneryPlaces.Available = false; Tick(1.46);
    Check(!MapSectionAccessibility.HasPendingContent && ScreenReader.Spoken.Count == 2, "leaving range cancels deferred nearby text");
    RuntimeSceneryPlaces.Available = true; Tick(1.67); Tick(1.88); Last("Cerca: Carreta", "new current prop replaces stale deferred prop");
    int sceneryCaptures = RuntimeSceneryPlaces.Captures;
    Tick(2.09, menu:true); Check(RuntimeSceneryPlaces.Captures == sceneryCaptures, "no geometry queries while in menus");
    Tick(2.1); Tick(2.31); Check(ScreenReader.Spoken.Count == 3, "closing menu beside same prop does not repeat it");
    ModText.SetLanguage("en"); Tick(2.52); Tick(2.73); Tick(2.94); Last("Nearby: Wagon", "nearby descriptions follow changed localization after current section");
    LE.UI.LoadingScreen.IsVisible = true; Tick(3.15);
    Check(RuntimeSceneryPlaces.Resets > 0 && !MapSectionAccessibility.HasPendingContent, "loading clears scenery cache and pending speech");
    Start(); MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with { SceneName="Unmapped" };
    RuntimeSceneryPlaces.Available = true; Tick(0); Last("Nearby: Gateway", "a fully validated footprint speaks on a brief pass without waiting a second geometry sample");
    Tick(.2); Check(ScreenReader.Spoken.Count == 1, "unchanged confirmed scenery does not repeat");
    RuntimeSceneryPlaces.Available = false; Tick(.41);
    RuntimeSceneryPlaces.Available = true; Tick(.62);
    Check(ScreenReader.Spoken.Count == 1, "one missing scenery sample does not rearm an already spoken place");
    RuntimeSceneryPlaces.Available = false; Tick(.83); Tick(1.04);
    RuntimeSceneryPlaces.Available = true; Tick(1.25);
    Check(ScreenReader.Spoken.Count == 1, "confirmed departure shorter than the repeat gap stays silent");
    RuntimeSceneryPlaces.Available = false; Tick(1.46); Tick(1.67);
    RuntimeSceneryPlaces.Available = true; Tick(7.0);
    Check(ScreenReader.Spoken.Count == 2, "confirmed departure of at least the repeat gap rearms next valid arrival immediately");
    Start(); MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with { SceneName="Unmapped" };
    RuntimeSceneryPlaces.Available = true; Tick(0, pending:true);
    Check(ScreenReader.Spoken.Count == 0 && MapSectionAccessibility.HasPendingContent, "immediate scenery still yields to higher priority speech");
    RuntimeSceneryPlaces.Available = false; Tick(.2);
    Check(ScreenReader.Spoken.Count == 0 && !MapSectionAccessibility.HasPendingContent, "a deferred brief pass is canceled when its physical evidence is gone");
    Start(); ModText.SetLanguage("es");
    MapSectionWorld.Snapshot = MapSectionWorld.Snapshot with { SceneName="Unmapped" };
    RuntimeSceneryPlaces.Place = new("tent-a", "map.place.tent", "tent-a");
    RuntimeSceneryPlaces.Available = true; Tick(0); Last("Tienda de campaña", "actual tent presence omits nearby qualifier");
    Tick(.2); Check(ScreenReader.Spoken.Count==1, "unchanged exact presence is quiet");
    RuntimeSceneryPlaces.HasValidObservation=false;RuntimeSceneryPlaces.Available=false;
    Tick(.41);Tick(.62);Check(ScreenReader.Spoken.Count==1, "missing support observation never claims exit");
    RuntimeSceneryPlaces.HasValidObservation=true;RuntimeSceneryPlaces.Available=true;
    Tick(.83);Check(ScreenReader.Spoken.Count==1, "same tent recovery does not repeat entry");
    RuntimeSceneryPlaces.Available=false;Tick(1.04);Tick(1.25);
    Last("Has salido de la tienda de campaña", "verified tent departure speaks once");
    Tick(1.46);Check(ScreenReader.Spoken.Count==2, "remaining outside is quiet");
    RuntimeSceneryPlaces.Available=true;RuntimeSceneryPlaces.Place=new("tent-b","map.place.tent","tent-b");
    Tick(1.67);Last("Tienda de campaña", "new tent has independent arrival identity");
    RuntimeSceneryPlaces.Place=new("tent-c","map.place.tent","tent-c");
    Tick(1.88);Check(ScreenReader.Spoken.Count==3, "neighboring tents with the same text stay silent");
    Tick(2.09,menu:true);Tick(2.10);Check(ScreenReader.Spoken.Count==3, "menu return preserves actual tent entry");
    RuntimeSceneryPlaces.Available=false;Tick(2.31,pending:true);Tick(2.52,pending:true);
    Check(MapSectionAccessibility.HasPendingContent, "tent exit defers to other reader content");
    RuntimeSceneryPlaces.Available=true;Tick(2.73);
    Last("Tienda de campaña", "return cancels stale deferred exit");
    Console.WriteLine($"Passed {checks} map section service checks.");
}
finally
{
    MapSectionAccessibility.Cleanup(); Application.dataPath = originalDataPath;
    string tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    if (!fixture.StartsWith(tempRoot,StringComparison.OrdinalIgnoreCase) ||
        !Path.GetFileName(fixture).StartsWith("LeAccess.MapSections.Tests-",StringComparison.Ordinal))
        throw new InvalidOperationException("Refusing to delete an unowned map-section fixture directory.");
    Directory.Delete(fixture,true);
}
