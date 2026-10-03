using System;
using System.IO;
using System.Collections.Generic;
using BepInEx.Configuration;
using LeAccess.Accessibility.Navigation;
using LeAccess.I18n;
using UnityEngine;

namespace LeAccess.Accessibility.MapSections;

internal static class MapSectionAccessibility
{
    // Match the nearby landmark reader's five observations per second.
    private const double PollSeconds = 0.2;
    private static readonly MapSectionState State = new();
    private static readonly MapSectionState OverviewState = new();
    private static readonly MapSectionState SurfaceState = new();
    // An owned, reachable model is already validated; waiting another tick loses short passes.
    private static readonly SceneryPresenceState SceneryState = new();
    private static ConfigEntry<bool> _enabled, _describeMaps, _surfaces;
    private static MapSectionCatalog _catalog;
    private static string _buildHash;
    private static MapSection _previousSection;
    private static long _session;
    private static double _nextPoll;
    private static string _lastFailure, _lastGuard, _sectionIdentity;
    internal static bool HasPendingContent { get; private set; }

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind("Map sections", "AnnounceSections", true,
            "Announce structures at your feet, nearby authored scenery, and verified map sections or generated rooms as you reach them.");
        _describeMaps = config.Bind("Map sections", "DescribeMapOnEntry", false,
            "Briefly list the verified landmark types in a mapped scene after its name is announced.");
        _surfaces = config.Bind("Map sections", "AnnounceSurfaceChanges", false,
            "Describe confirmed changes in the ground under the player's feet, such as wood, snow or water.");
        Reset();
        try
        {
            _buildHash = File.ReadAllText(Path.Combine(Application.dataPath, "build_hash.txt")).Trim();
            RuntimeSceneryPlaces.Initialize(_buildHash);
            var assembly = typeof(MapSectionAccessibility).Assembly;
            var resources = assembly.GetManifestResourceNames();
            Array.Sort(resources, StringComparer.Ordinal);
            var catalogs = new List<string>();
            foreach (string resource in resources)
            {
                if (!resource.StartsWith("LeAccess.MapSections.", StringComparison.Ordinal) ||
                    !resource.EndsWith(".json", StringComparison.Ordinal)) continue;
                using var stream = assembly.GetManifestResourceStream(resource);
                using var reader = new StreamReader(stream);
                catalogs.Add(reader.ReadToEnd());
            }
            if (catalogs.Count == 0) throw new InvalidOperationException("Bundled map-section profiles unavailable.");
            _catalog = MapSectionCatalog.Parse(catalogs);
        }
        catch (Exception ex)
        {
            _catalog = null;
            ReportFailure(ex);
        }
    }

    internal static void Update(bool menuActive = false, bool otherSpeechPending = false)
    {
        if (_enabled == null) return;
        try
        {
            if (!_enabled.Value || LE.UI.LoadingScreen.IsVisible) { Reset(); return; }
            // Losing focus keeps delivered memory so alt-tab does not repeat places.
            if (menuActive || !Application.isFocused || NavigationAudioState.IsSuspended || !GameplayInputState.OwnsInput())
            {
                HasPendingContent = false;
                _nextPoll = 0;
                return;
            }
            double now = Time.unscaledTimeAsDouble;
            if (now < _nextPoll) return;
            _nextPoll = now + PollSeconds;
            if (!MapSectionWorld.TryCapturePlayer(out var world)) { Reset(); return; }
            ResolveSection(world, out string id, out string text, out string overview);
            bool canSpeak = ScreenReader.IsAvailable() && !ScreenReader.SpokeThisFrame && !otherSpeechPending;
            bool describe = _describeMaps.Value && overview != null;
            bool summarySpoken = describe && OverviewState.Observe(_session, "overview", overview, canSpeak, now, out _);
            if (summarySpoken) ScreenReader.Speak(overview, interrupt: false);
            bool summaryPending = describe && OverviewState.HasPendingContent;
            bool announce = State.Observe(_session, id, text, canSpeak && !summarySpoken && !summaryPending, now, out string announcement);
            HasPendingContent = State.HasPendingContent || summaryPending;
            if (announce)
            {
                ScreenReader.Speak(announcement, interrupt: false);
                Plugin.Log.LogInfo($"[Map section] scene={world.SceneName} section={_sectionIdentity ?? id}: {announcement}");
            }
            bool sceneryKnown = RuntimeSceneryPlaces.TryRead(world, out var scenery);
            string sceneryText = sceneryKnown ? scenery.TextKey == "map.place.tent" ? ModText.Get(scenery.TextKey) :
                ModText.Format("landmark.nearby", ModText.Get(scenery.TextKey)) : null;
            bool scenerySpoken = SceneryState.Observe(world.Session, sceneryKnown ? scenery.SpeechGroup : null, sceneryText,
                sceneryKnown && scenery.TextKey == "map.place.tent",
                canSpeak && !HasPendingContent && !summarySpoken && !announce, now,
                ModText.Get("map.place.left_tent"), out string nearbyText, RuntimeSceneryPlaces.HasValidObservation);
            HasPendingContent |= SceneryState.HasPendingContent;
            if (scenerySpoken)
            {
                ScreenReader.Speak(nearbyText, interrupt: false);
                Plugin.Log.LogInfo($"[Map scenery] scene={world.SceneName} source={scenery.Identity}: {nearbyText}");
            }
            else if (sceneryKnown && SceneryState.HasPendingContent)
                MapPlaceDiagnostics.Report("scenery-deferred", $"source={scenery.Identity}, canSpeak={canSpeak}, otherSpeech={otherSpeechPending}, sectionPending={State.HasPendingContent}, sectionSpoken={announce}, overviewPending={summaryPending}");
            string surface = _surfaces.Value && MapSurfaceProbe.TryRead(world, out string key) ? ModText.Get(key) : null;
            // Unknown samples cancel pending text but retain the last known surface to avoid footstep chatter.
            bool groundSpoken = SurfaceState.Observe(world.Session, "surface", surface,
                canSpeak && !HasPendingContent && !summarySpoken && !announce && !scenerySpoken, now, out string groundText);
            HasPendingContent |= SurfaceState.HasPendingContent;
            if (groundSpoken)
            {
                ScreenReader.Speak(groundText, interrupt: false);
                Plugin.Log.LogInfo($"[Map surface] scene={world.SceneName}: {groundText}");
            }
        }
        catch (Exception ex) { Reset(); ReportFailure(ex); }
    }

    private static void ResolveSection(MapSectionWorldSnapshot world, out string id, out string text, out string overview)
    {
        id = text = overview = null;
        if (_session != world.Session) { ClearSection(); _session = world.Session; }
        _sectionIdentity = null;
        var map = ResolveFixedMap(world);
        if (map != null)
            overview = _describeMaps.Value ? MapSectionOverview.Describe(map, world) : null;
        var section = map?.FindSection(world.X, world.Y, world.Z,
            candidate => MapSectionSources.IsAvailable(candidate, world));
        if (RuntimeMapPlaces.TryRead(world, out var place))
        {
            if (section != null && section.KindKey == place.TextKey)
            {
                SetFixedSection(section, out id, out text);
                return;
            }
            // Use the same category across live pieces and fixed polygons, avoiding seam chatter.
            id = place.SpeechGroup;
            text = ModText.Get(place.TextKey);
            _sectionIdentity = "live:" + place.Identity;
            _previousSection = null;
            return;
        }
        if (section != null)
        {
            SetFixedSection(section, out id, out text);
            return;
        }
        if (map != null && _previousSection != null &&
            _previousSection.Contains(world.X, world.Y, world.Z, world.BodyRadius) &&
            MapSectionSources.IsAvailable(_previousSection, world))
        {
            SetFixedSection(_previousSection, out id, out text);
            return;
        }
        _previousSection = null;
        if (PcgMapSections.TryCapture(world, out var room))
        {
            _sectionIdentity = $"pcg:{room.Session}:{room.SectionId}";
            id = room.KindKey == "map.section.passage" ? $"pcg:{room.Session}:passage" : _sectionIdentity;
            text = ModText.Format(room.KindKey ?? "map.section.room", room.Number);
            if (room.IsEntrance) text += ". " + ModText.Get("map.section.entrance");
            if (room.IsExit) text += ". " + ModText.Get("map.section.exit");
            if (room.ConnectedPassages.HasValue)
                text += ". " + ModText.Format("map.section.passages", room.ConnectedPassages.Value);
        }
        // Unknown positions confirm departure through State, without erasing the last spoken place.
    }

    private static SectionMap ResolveFixedMap(MapSectionWorldSnapshot world)
    {
        var map = world.IsFixedLayout ? _catalog?.Find(world.SceneName, _buildHash) : null;
        if (map == null) { _previousSection = null; return null; }
        if (!MapSectionWorld.ValidateAnchors(map, world))
        {
            string guard = $"{world.SceneHandle}:{world.SceneName}:{world.Variant}";
            if (_lastGuard != guard)
            {
                _lastGuard = guard;
                Plugin.Log.LogWarning($"[Map section] Layout did not match verified anchors: {world.SceneName}");
            }
            _previousSection = null;
            return null;
        }
        _lastGuard = null;
        return map;
    }

    private static void SetFixedSection(MapSection section, out string id, out string text)
    {
        _previousSection = section;
        _sectionIdentity = section.Id;
        id = section.SpeechGroup;
        text = ModText.Get(section.TextKey);
    }

    private static void ReportFailure(Exception ex)
    {
        string failure = $"{ex.GetType().Name}: {ex.Message}";
        if (_lastFailure == failure) return;
        _lastFailure = failure;
        Plugin.Log.LogWarning($"Map section feedback unavailable: {failure}");
    }

    private static void ClearSection()
    {
        State.Reset();
        OverviewState.Reset();
        SceneryState.Reset();
        _session = 0;
        _previousSection = null;
        _sectionIdentity = null;
        HasPendingContent = false;
    }

    private static void Reset()
    {
        ClearSection();
        MapSectionWorld.Reset();
        MapSectionSources.Reset();
        PcgMapSections.Reset();
        MapSectionOverview.Reset();
        MapSurfaceProbe.Reset();
        RuntimeMapPlaces.Reset();
        RuntimeSceneryPlaces.Reset();
        SurfaceState.Reset();
        _nextPoll = 0;
    }

    internal static void Cleanup()
    {
        Reset();
        _enabled = null;
        _describeMaps = null;
        _surfaces = null;
        _catalog = null;
        _buildHash = _lastFailure = _lastGuard = null;
    }
}
