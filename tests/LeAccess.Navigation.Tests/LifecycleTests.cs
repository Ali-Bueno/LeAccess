using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Navigation;
using UnityEngine;
using UnityEngine.AI;

internal static class LifecycleTests
{
    public static void Run()
    {
        ProbeReacquiresCollectedObjects();
        SuspensionAndQueryFailureRecover();
        CollectedConversationsDoNotLatchAudioOff();
        PrimitiveFilterArgumentsPreserved();
        BodySizeSetsWarningReach();
        BodySizeJitterPreservesSettings();
        BodySizeJitterStillAllowsCues();
        DiagnosticHeartbeatIsBounded();
        SampledOriginAnchorsReadings();
        GameplayOwnershipAndDeathSuspendRadar();
        PlayerReplacementRejectsForeignNavigationData();
        OnlineVisualRadiusSetsWarningReach();
        StaleSceneAndInactiveCameraSuspendRadar();
        OwnedDetachedVisualsRemainPlayable();
    }

    private static void ProbeReacquiresCollectedObjects()
    {
        ResetHost();
        LocalPlayer.instance = new LocalPlayer { navMeshAgent = PlayerFinder.Player.Agent };
        var probe = new RadarWorldProbe();
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var first), "Initial world capture");
        PlayerFinder.Player.WasCollected = true;
        PlayerFinder.Player.Agent.WasCollected = true;
        Camera.main.WasCollected = true;
        PlayerFinder.Player = new GameObject();
        Camera.main = new Camera();
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var second), "A collected player and camera can be replaced");
        Program.Check(first.Session != second.Session, "Player replacement starts a new radar session");

        PlayerFinder.Player.Agent.WasCollected = true;
        PlayerFinder.Player.Agent = new NavMeshAgent();
        Camera.main.WasCollected = true;
        Camera.main = new Camera();
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _), "Collected agent and camera refresh within the same scene");
        NavMesh.OriginAvailable = false;
        Program.Check(!probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _), "Missing walkable origin suspends readings");
        Program.Check(probe.UnavailableReason == "no walkable NavMesh near player", "Missing NavMesh has a specific diagnostic");
        NavMesh.OriginAvailable = true;
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _), "Walkable origin becoming available resumes readings");
        PlayerFinder.Player.Agent.isOnNavMesh = false;
        PlayerFinder.Player.Agent.isActiveAndEnabled = false;
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _), "Valid walkable ground is probed when controller movement disables its agent");
    }

    private static void SuspensionAndQueryFailureRecover()
    {
        ResetHost();
        ReactiveRadar.Initialize(new ConfigFile());
        Tick(0);
        Program.Check(RadarAudioOutput.Played == 0, "Initial world baseline is silent");
        Program.Check(RadarAudioOutput.ConfiguredBufferMilliseconds == 100,
            "Reactive radar uses the same default buffering margin as the existing quest ping.");
        NavMesh.Depth = 1;
        Tick(0.25);
        LE.UI.LoadingScreen.IsVisible = true;
        Tick(0.30);
        int queries = NavMesh.Queries;
        Tick(0.35);
        Program.Check(NavMesh.Queries == queries, "Loading screen prevents world queries");
        Program.Check(LeAccess.Plugin.Log.Information.Count(line => line.Contains("scene loading")) == 1,
            "Repeated loading polls log the suspension reason only once");
        PlayerFinder.Player.WasCollected = true;
        PlayerFinder.Player = new GameObject();
        LE.UI.LoadingScreen.IsVisible = false;
        Tick(0.40);
        Program.Check(RadarAudioOutput.Played == 0, "Loading clears unconfirmed cues and resumes with a silent baseline");

        NavMesh.ThrowOnQuery = true;
        Tick(0.50);
        Program.Check(LeAccess.Plugin.Log.Warnings.Count == 1, "A probe failure is logged");
        PlayerFinder.Player.Agent.radius = 0;
        PlayerFinder.Player.Agent = new NavMeshAgent();
        NavMesh.ThrowOnQuery = false;
        NavMesh.Depth = 20;
        Tick(0.60);
        Program.Check(RadarAudioOutput.Played == 0, "Recovery discards stale agent and silently rebases");
        NavMesh.Depth = 1;
        Tick(0.80);
        Tick(0.95);
        Program.Check(RadarAudioOutput.Played > 0, "Radar emits a confirmed wall cue after loading and a failed query");
        Program.Check(LeAccess.Plugin.Log.Information.Count(line => line.Contains("radar ready")) == 3,
            "Initial availability and both recoveries are logged");
        ReactiveRadar.Cleanup();
    }

    private static void CollectedConversationsDoNotLatchAudioOff()
    {
        ResetHost();
        var dialogue = new DialogueSystemNavigable();
        NavigationAudioState.ConversationStarted(dialogue);
        Program.Check(NavigationAudioState.IsSuspended, "Conversation suspends navigation audio");
        dialogue.WasCollected = true;
        NavigationAudioState.Refresh();
        Program.Check(!NavigationAudioState.IsSuspended, "Collected dialogue is safely removed from suspension gate");
    }

    private static void Tick(double now)
    {
        Time.unscaledTimeAsDouble = now;
        ReactiveRadar.Update();
    }

    private static void PrimitiveFilterArgumentsPreserved()
    {
        ResetHost();
        var agent = PlayerFinder.Player.Agent;
        agent.agentTypeID = 42;
        agent.areaMask = 8;
        var probe = new RadarWorldProbe();
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _),
            "Primitive filter route captures the world without constructing NavMeshQueryFilter");
        Program.Check(NavMesh.Filters.Count == 1 + RadarEngine.DirectionCount,
            "Origin sampling and all directional rays use the primitive filter route");
        Program.Check(NavMesh.Filters.All(filter => filter.Type == agent.agentTypeID && filter.Mask == agent.areaMask),
            "Every query retains the actual player's agent type and walkable area mask");

        NavMesh.Filters.Clear();
        agent.agentTypeID = 99;
        agent.areaMask = 16;
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _), "Changed agent filter captures");
        Program.Check(NavMesh.Filters.All(filter => filter.Type == 99 && filter.Mask == 16),
            "Filter changes are read from the current agent rather than a stale boxed filter");
    }

    private static void ResetHost()
    {
        ReactiveRadar.Cleanup();
        WorldAudioReservation.Reset();
        PlayerFinder.Player = new GameObject();
        PlayerFinder.Actor = null;
        PlayerFinder.playerActorVisuals = null;
        GameplayInputState.Owns = true;
        LocalPlayer.instance = null;
        Camera.main = new Camera();
        NavMesh.Depth = 20;
        NavMesh.OriginAvailable = true;
        NavMesh.SampleOffset = default;
        NavMesh.ThrowOnQuery = false;
        NavMesh.Queries = 0;
        NavMesh.Filters.Clear();
        LE.UI.LoadingScreen.IsVisible = false;
        RadarAudioOutput.Played = 0;
        LeAccess.Plugin.Log.Warnings.Clear();
        LeAccess.Plugin.Log.Information.Clear();
    }

    private static void SampledOriginAnchorsReadings()
    {
        ResetHost();
        PlayerFinder.Player.transform.position = new(8, 1, 12);
        NavMesh.SampleOffset = new(0.2f, -1, -0.3f);
        NavMesh.Depth = 2;
        var probe = new RadarWorldProbe();
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var snapshot),
            "A player displaced from its projected walkable origin captures");
        Program.Near(snapshot.Position.X, 8.2f, "Wall history anchors to the same X origin as the rays");
        Program.Near(snapshot.Position.Y, 11.7f, "Wall history anchors to the same Z origin as the rays");
        foreach (var reading in snapshot.Readings)
            Program.Near(reading.Depth, 2, "Depth remains relative to sampled origin without double correction");
        ResetHost();
    }

    private static void GameplayOwnershipAndDeathSuspendRadar()
    {
        ResetHost();
        ReactiveRadar.Initialize(new ConfigFile());
        Tick(0);
        int queries = NavMesh.Queries;
        GameplayInputState.Owns = false;
        Tick(0.1);
        Program.Check(NavMesh.Queries == queries, "A non-pausing menu suspends terrain probing");
        GameplayInputState.Owns = true;
        NavMesh.Depth = 1;
        Tick(0.2);
        Program.Check(NavMesh.Queries > queries && RadarAudioOutput.Played == 0,
            "Closing the menu resumes with a new silent baseline");
        PlayerFinder.playerActorVisuals = new ActorVisuals { dead = true };
        var probe = new RadarWorldProbe();
        Program.Check(!probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _), "Dead player suppresses terrain cues");
        PlayerFinder.playerActorVisuals.WasCollected = true;
        Program.Check(!probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _), "Collected player visuals are checked before Unity null");
        PlayerFinder.playerActorVisuals = new ActorVisuals();
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _), "A live replacement resumes terrain probing");
        ResetHost();
    }

    private static void BodySizeSetsWarningReach()
    {
        ResetHost();
        PlayerFinder.Player.Agent.radius = 0.1f;
        PlayerFinder.Actor = new Actor { Radius = 0.5f };
        var probe = new RadarWorldProbe();
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var snapshot), "Real body and helper agent are captured");
        Program.Near(snapshot.Settings.OpenDepth, RadarSettings.FromBodyWidth(1).OpenDepth,
            "D2 warning reach scales to the actor's body rather than the narrower helper agent");
        Program.Near(NavMesh.SampleRadius, 0.1f, "Origin sampling still uses the actual NavMesh agent radius");
        Program.Check(snapshot.Settings.CloseDepth > RadarSettings.FromBodyWidth(0.2f).CloseDepth,
            "Body-sized warning distance detects obstacles before reaching the tiny agent boundary");
        PlayerFinder.Actor.WasCollected = true;
        PlayerFinder.Actor = new Actor { Radius = float.NaN };
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out snapshot), "Invalid actor size falls back safely");
        Program.Near(snapshot.BodyRadius, 0.1f, "Invalid actor size cannot disable or corrupt radar distances");
    }

    private static void PlayerReplacementRejectsForeignNavigationData()
    {
        ResetHost();
        var formerPlayer = PlayerFinder.Player;
        formerPlayer.Agent.radius = 4;
        formerPlayer.Agent.agentTypeID = 42;
        formerPlayer.Agent.areaMask = 8;
        LocalPlayer.instance = new LocalPlayer { navMeshAgent = formerPlayer.Agent };
        PlayerFinder.Player = new GameObject();
        PlayerFinder.Player.Agent.radius = 0.1f;
        PlayerFinder.Actor = new Actor { gameObject = formerPlayer, Radius = 5 };
        var probe = new RadarWorldProbe();
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var snapshot),
            "An old live LocalPlayer reference does not prevent the new player's capture.");
        Program.Near(snapshot.AgentRadius, 0.1f, "A foreign agent cannot change the new player's sample radius.");
        Program.Near(snapshot.BodyRadius, 0.1f, "An old player's Actor cannot enlarge the new player's warning distances.");
        Program.Check(NavMesh.Filters.All(filter => filter.Type == 0 && filter.Mask == -1),
            "Only the new player's own agent filters reach NavMesh queries.");

        var detachedAgent = PlayerFinder.Player.Agent;
        detachedAgent.gameObject = formerPlayer;
        PlayerFinder.Player.Agent = new NavMeshAgent { radius = 0.2f, agentTypeID = 99 };
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out snapshot),
            "A still-live cached agent that left the player is reacquired.");
        Program.Near(snapshot.AgentRadius, 0.2f, "Reacquisition uses the newly owned agent's radius.");
        Program.Check(snapshot.AgentTypeId == 99, "Reacquisition uses the newly owned agent's navigation type.");

        var child = new GameObject();
        child.transform.parent = PlayerFinder.Player.transform;
        child.Agent.radius = 0.3f;
        child.Agent.isActiveAndEnabled = false;
        PlayerFinder.Player.Agent = child.Agent;
        LocalPlayer.instance.navMeshAgent = child.Agent;
        probe.Reset();
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out snapshot),
            "An owned child agent still provides queries while native controller movement disables it.");
        Program.Near(snapshot.AgentRadius, 0.3f, "The child's actual native radius is retained.");
        child.activeInHierarchy = false;
        Program.Check(!probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _),
            "An inactive agent owner cannot supply navigation data.");
        ResetHost();
    }

    private static void OnlineVisualRadiusSetsWarningReach()
    {
        ResetHost();
        PlayerFinder.Player.Agent.radius = 0.1f;
        PlayerFinder.playerActorVisuals = new ActorVisuals { Radius = 0.5f };
        var probe = new RadarWorldProbe();
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var snapshot),
            "An online player with visuals and no Actor still provides actual body size.");
        Program.Near(snapshot.BodyRadius, 0.5f, "The helper agent cannot shrink the online player's known body.");
        Program.Near(snapshot.Settings.OpenDepth, RadarSettings.FromBodyWidth(1).OpenDepth,
            "Online visual radius gives the same warning reach as an offline Actor with that radius.");
        Program.Near(NavMesh.SampleRadius, 0.1f, "Visual radius does not enlarge origin sampling into another floor.");

        PlayerFinder.Actor = new Actor { Radius = float.NaN };
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out snapshot),
            "Unready Actor radius still permits the owned live visual radius.");
        Program.Near(snapshot.BodyRadius, 0.5f, "Unready Actor does not override the ready visual size.");
        PlayerFinder.playerActorVisuals.Radius = float.NaN;
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out snapshot),
            "Both body sources being unavailable safely leaves the native agent radius.");
        Program.Near(snapshot.BodyRadius, 0.1f, "No invalid body radius escapes into radar thresholds.");

        PlayerFinder.playerActorVisuals = new ActorVisuals { gameObject = new GameObject(), Radius = 5 };
        Program.Check(!probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _),
            "A foreign player's live visual object does not establish a playable local snapshot.");
        ResetHost();
    }

    private static void StaleSceneAndInactiveCameraSuspendRadar()
    {
        ResetHost();
        var probe = new RadarWorldProbe();
        PlayerFinder.Player.scene = new UnityEngine.SceneManagement.Scene { handle = 2 };
        Program.Check(!probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _),
            "A local player retained in an old scene cannot anchor current terrain cues.");
        Program.Check(NavMesh.Queries == 0, "Stale scene ownership is rejected before NavMesh sampling.");
        PlayerFinder.Player.scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Camera.main.isActiveAndEnabled = false;
        Program.Check(!probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _),
            "An inactive Camera.main returned during a transition cannot orient cues.");
        Camera.main.isActiveAndEnabled = true;
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _),
            "Valid scene and live camera restore probing.");
        ResetHost();
    }

    private static void OwnedDetachedVisualsRemainPlayable()
    {
        ResetHost();
        PlayerFinder.Player.Agent.radius = 0.1f;
        var actor = new Actor { Radius = float.NaN };
        PlayerFinder.Actor = actor;
        var visuals = new ActorVisuals { gameObject = new GameObject(), Radius = 0.6f };
        var sync = new ActorSync { gameObject = new GameObject(), gameplayActor = actor, actorVisuals = visuals };
        actor.actorSync = sync;
        PlayerFinder.playerActorVisuals = visuals;
        var probe = new RadarWorldProbe();
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var snapshot),
            "The local Actor's exact sync authorizes detached visuals without a hierarchy guess.");
        Program.Near(snapshot.BodyRadius, 0.6f, "Detached owned visuals provide their actual body size.");
        PlayerFinder.playerActorVisuals = new ActorVisuals { gameObject = new GameObject(), Radius = 5, dead = true };
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out snapshot),
            "A stale dead legacy visual cache cannot override the currently owned sync visual.");
        Program.Near(snapshot.BodyRadius, 0.6f, "Legacy cached size cannot replace the proven sync visual size.");
        PlayerFinder.playerActorVisuals = null;
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out snapshot),
            "An available local sync supplies visuals before the optional legacy cache is populated.");
        Program.Near(snapshot.BodyRadius, 0.6f, "A missing legacy cache cannot shrink the known sync-owned body.");
        sync.gameplayActor = new Actor { gameObject = new GameObject() };
        Program.Check(!probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _),
            "A sync that lost its local Actor backlink cannot authorize navigation.");
        sync.gameplayActor = actor;
        visuals.dead = true;
        Program.Check(!probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _),
            "The actual sync-owned player's death suspends navigation.");
        visuals.dead = false;
        visuals.gameObject.activeInHierarchy = false;
        Program.Check(!probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _),
            "Inactive owned visual roots do not supply playable radar state.");
        visuals.gameObject.activeInHierarchy = true;
        visuals.WasCollected = true;
        Program.Check(!probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _),
            "Collected owned visuals are rejected before native access.");
        visuals.WasCollected = false;
        sync.gameObject.scene = new UnityEngine.SceneManagement.Scene { handle = 2 };
        Program.Check(!probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out _),
            "A sync from another terrain scene cannot authorize the local radar.");
        ResetHost();
    }

    private static void DiagnosticHeartbeatIsBounded()
    {
        ResetHost();
        ReactiveRadar.Initialize(new ConfigFile());
        for (int poll = 0; poll <= 110; poll++) Tick(poll * RadarSettings.PollSeconds);
        var reports = LeAccess.Plugin.Log.Information.Where(line => line.Contains("radar status:")).ToArray();
        Program.Check(reports.Length == 2, "A stationary radar emits only one diagnostic heartbeat per five seconds");
        Program.Check(reports[1].Contains("generated=0 submitted=0") && reports[1].Contains("body_radius=")
            && reports[1].Contains("depths_ULRD="), "Heartbeat distinguishes unchanged terrain from unavailable geometry");
        NavMesh.Depth = 1;
        Tick(5.65);
        Tick(5.80);
        Program.Check(LeAccess.Plugin.Log.Information.Any(line => line.Contains("radar cue: direction=Up state=Blocked")),
            "Generated terrain cue submission is recorded independently of quiet heartbeats");
        ReactiveRadar.Cleanup();
    }

    private static void BodySizeJitterPreservesSettings()
    {
        ResetHost();
        PlayerFinder.Player.Agent.radius = 0.1f;
        PlayerFinder.Actor = new Actor { Radius = 0.5f };
        var probe = new RadarWorldProbe();
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var initial), "Initial body settings captured");
        foreach (float radius in new[] { MathF.BitIncrement(0.5f), MathF.BitDecrement(0.5f), 0.5f })
        {
            PlayerFinder.Actor.Radius = radius;
            Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var jittered), "Jittered radius captures");
            Program.Check(ReferenceEquals(initial.Settings, jittered.Settings), "Neighboring float body sizes retain the same engine settings reference");
        }

        PlayerFinder.Actor.Radius = 0.75f;
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths, out var enlarged), "Real body size change captures");
        Program.Check(!ReferenceEquals(initial.Settings, enlarged.Settings), "A real body size change updates radar settings");
        Program.Near(enlarged.Settings.OpenDepth, 5f, "Actual larger body uses its authoritative warning range");
        Program.Check(probe.TryCapture(RadarSettings.DefaultOpenDepthInBodyWidths + 1f, out var configured), "Configuration change captures");
        Program.Check(!ReferenceEquals(enlarged.Settings, configured.Settings), "User depth configuration changes still refresh settings");
    }

    private static void BodySizeJitterStillAllowsCues()
    {
        ResetHost();
        PlayerFinder.Player.Agent.radius = 0.1f;
        PlayerFinder.Actor = new Actor { Radius = 0.5f };
        ReactiveRadar.Initialize(new ConfigFile());
        Tick(0);
        NavMesh.Depth = 1f;
        for (int poll = 1; poll <= 30; poll++)
        {
            PlayerFinder.Actor.Radius = poll % 2 == 0 ? MathF.BitIncrement(0.5f) : MathF.BitDecrement(0.5f);
            Tick(poll * 0.06);
        }
        Program.Check(RadarAudioOutput.Played > 0, "A persistent wall is confirmed and submitted despite body-radius jitter on every poll");
        ReactiveRadar.Cleanup();
    }
}
