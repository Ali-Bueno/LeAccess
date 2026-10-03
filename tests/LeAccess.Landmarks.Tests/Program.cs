using BepInEx.Configuration;
using LeAccess;
using LeAccess.Accessibility;
using LeAccess.Accessibility.Landmarks;
using LeAccess.Accessibility.Navigation;
using LeAccess.I18n;
using UnityEngine;

int checks = 0;
ConfigFile config = null;
void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
void Start()
{
    LandmarkAccessibility.Cleanup();
    ModText.Initialize(null,null);
    config = new ConfigFile();
    Application.isFocused = GameplayInputState.Gameplay = ScreenReader.Available = LandmarkProbe.Found = true;
    LE.UI.LoadingScreen.IsVisible = NavigationAudioState.IsSuspended = LandmarkProbe.Throw = false;
    LandmarkProbe.Reads = 0; LandmarkProbe.LastFailure = null; LandmarkProbe.LastStatus = null;
    LandmarkProbe.Target = new(1,10,"The Keeper's Bridge");
    Time.unscaledTimeAsDouble = 0;
    ScreenReader.Spoken.Clear(); Plugin.Log.Warnings.Clear(); Plugin.Log.Information.Clear();
    LandmarkAccessibility.Initialize(config);
}
void Tick(double time, bool menu = false, bool pending = false, bool spoke = false)
{
    Time.unscaledTimeAsDouble = time;
    ScreenReader.SpokeThisFrame = spoke;
    LandmarkAccessibility.Update(menu,pending);
}
void Last(string text, string name) => Check(ScreenReader.Spoken.Count > 0 &&
    ScreenReader.Spoken.Last() == (text,false), name);
try
{
    Start();
    Check(config.Enabled.Value,"enabled by default");
    Tick(0);
    Check(ScreenReader.Spoken.Count == 0 && LandmarkAccessibility.HasPendingContent,"first observation pending");
    Tick(.199);
    Check(LandmarkProbe.Reads == 1 && ScreenReader.Spoken.Count == 0,"200ms poll interval");
    Tick(.2);
    Last("Nearby: The Keeper's Bridge","two matching observations deliver queued speech");
    Check(!LandmarkAccessibility.HasPendingContent && ScreenReader.Spoken.Count == 1,"delivery clears pending");
    Tick(.4); Tick(.61);
    Check(ScreenReader.Spoken.Count == 1,"same target deduplicated");
    Check(Plugin.Log.Information.Count == 1 && Plugin.Log.Information[0].Contains("[Landmark] target=10:"),"one delivered diagnostic");
    LandmarkProbe.Found = false; Tick(.82);
    Check(!LandmarkAccessibility.HasPendingContent,"leaving range drops pending");
    LandmarkProbe.Found = true; Tick(1.03);
    Check(ScreenReader.Spoken.Count == 1 && LandmarkAccessibility.HasPendingContent,"reapproach settles again");
    Tick(1.24);
    Check(ScreenReader.Spoken.Count == 2,"reapproach repeats once");

    Start(); Tick(0);
    LandmarkProbe.Target = new(1,20,"Eastern Gate"); Tick(.2);
    Check(ScreenReader.Spoken.Count == 0,"changed target replaces pending");
    Tick(.4);
    Last("Nearby: Eastern Gate","only newest target announced");
    Check(ScreenReader.Spoken.Count == 1,"old target never delivered");
    LandmarkProbe.Target = new(1,20,"Renamed Gate"); Tick(.61); Tick(.82);
    Last("Nearby: Renamed Gate","changed owned name announced");

    foreach (long session in new[] { 2L, (2L << 32) | 1 })
    {
        Start(); Tick(0); Tick(.2);
        LandmarkProbe.Target = LandmarkProbe.Target with { Session = session };
        Tick(.4);
        Check(ScreenReader.Spoken.Count == 1 && LandmarkAccessibility.HasPendingContent,"new player/scene session settles " + session);
        Tick(.61);
        Check(ScreenReader.Spoken.Count == 2,"new player/scene session delivers " + session);
    }

    foreach (string gate in new[] { "menu", "pause", "input" })
    {
        Start(); Tick(0); Tick(.2);
        NavigationAudioState.IsSuspended = gate == "pause";
        GameplayInputState.Gameplay = gate != "input";
        Tick(.4,menu:gate == "menu");
        Check(!LandmarkAccessibility.HasPendingContent && LandmarkProbe.Reads == 2,"no background pending/scan " + gate);
        NavigationAudioState.IsSuspended = false; GameplayInputState.Gameplay = true;
        Tick(.41); Tick(.62);
        Check(ScreenReader.Spoken.Count == 1,"delivered name preserved across " + gate);
    }

    Start(); Tick(0);
    Tick(.01,menu:true);
    Check(!LandmarkAccessibility.HasPendingContent,"menu suppresses unsettled background pending");
    LandmarkProbe.Target = new(1,20,"Eastern Gate"); Tick(.02);
    Check(ScreenReader.Spoken.Count == 0,"returned changed target revalidates before speech");
    Tick(.23);
    Last("Nearby: Eastern Gate","no stale target after menu");
    Start(); Tick(0); Tick(.01,menu:true); LandmarkProbe.Found = false; Tick(.02); Tick(.23);
    Check(ScreenReader.Spoken.Count == 0 && !LandmarkAccessibility.HasPendingContent,"disappeared target never deferred");
    Start(); Tick(0); Tick(.01,menu:true); Tick(.02);
    Last("Nearby: The Keeper's Bridge","still current pending target revalidated on return");

    foreach (string gate in new[] { "focus", "loading", "disabled" })
    {
        Start(); Tick(0); Tick(.2);
        Application.isFocused = gate != "focus";
        LE.UI.LoadingScreen.IsVisible = gate == "loading";
        config.Enabled.Value = gate != "disabled";
        Tick(.4);
        Check(!LandmarkAccessibility.HasPendingContent && LandmarkProbe.Reads == 2,"hard gate clears state " + gate);
        Application.isFocused = config.Enabled.Value = true; LE.UI.LoadingScreen.IsVisible = false;
        Tick(.41);
        Check(ScreenReader.Spoken.Count == 1,"hard gate return settles " + gate);
        Tick(.62);
        Check(ScreenReader.Spoken.Count == (gate == "focus" ? 1 : 2),
            gate == "focus" ? "alt-tab keeps the delivered name silent" : "hard gate reset allows current name " + gate);
    }

    Start(); Tick(0); Tick(.2,pending:true); Tick(.41,spoke:true);
    Check(ScreenReader.Spoken.Count == 0 && LandmarkAccessibility.HasPendingContent,"other pending/actual speech gets priority");
    LandmarkProbe.Target = new(1,20,"Eastern Gate"); Tick(.62);
    Check(ScreenReader.Spoken.Count == 0,"priority-delayed obsolete target discarded");
    Tick(.83);
    Last("Nearby: Eastern Gate","latest priority-delayed target queued");
    Start(); ScreenReader.Available = false; Tick(0); Tick(.2);
    Check(ScreenReader.Spoken.Count == 0 && LandmarkAccessibility.HasPendingContent,"reader unavailable preserves current candidate");
    ScreenReader.Available = true; Tick(.4);
    Last("Nearby: The Keeper's Bridge","available reader delivers current target");
    ModText.SetLanguage("es"); Tick(.61); Tick(.82);
    Last("Cerca: The Keeper's Bridge","Spanish phrase preserves game-owned name");

    Start(); LandmarkProbe.LastStatus="tags=0"; Tick(0); Tick(.2);
    Check(Plugin.Log.Information.Count(x=>x.StartsWith("[LandmarkStatus]"))==1,"unchanged status logged once");
    LandmarkProbe.LastStatus="tags=1"; Tick(.41);
    Check(Plugin.Log.Information.Count(x=>x.StartsWith("[LandmarkStatus]"))==1,"changed status rate limited");
    Tick(5.1);
    Check(Plugin.Log.Information.Count(x=>x.StartsWith("[LandmarkStatus]"))==2,"latest status logged after bounded interval");
    Tick(10.2);
    Check(Plugin.Log.Information.Count(x=>x.StartsWith("[LandmarkStatus]"))==2,"stable status never repeats");
    Start(); LandmarkProbe.LastFailure = "One invalid registry entry"; Tick(0); Tick(.2);
    Check(Plugin.Log.Warnings.Count == 1 && ScreenReader.Spoken.Count == 1,"probe warning dedup still allows valid target");
    LandmarkProbe.LastFailure = "A different invalid entry"; Tick(.41);
    Check(Plugin.Log.Warnings.Count == 2,"new probe warning reported");
    Start(); Tick(0); LandmarkProbe.Throw = true; Tick(.2); Tick(.41);
    Check(Plugin.Log.Warnings.Count == 1 && !LandmarkAccessibility.HasPendingContent,"exceptions reset pending and dedup diagnostics");
    LandmarkProbe.Throw = false; Tick(.42); Tick(.63);
    Check(ScreenReader.Spoken.Count == 1,"exception recovery settles current target");
    LandmarkAccessibility.Cleanup(); int reads = LandmarkProbe.Reads;
    Check(!LandmarkAccessibility.HasPendingContent,"cleanup clears pending");
    Tick(.84);
    Check(LandmarkProbe.Reads == reads && ScreenReader.Spoken.Count == 1,"cleanup disables polling");
    Console.WriteLine($"Passed {checks} landmark service checks.");
}
finally { LandmarkAccessibility.Cleanup(); }
