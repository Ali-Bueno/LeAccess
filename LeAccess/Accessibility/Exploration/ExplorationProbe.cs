using System;
using System.Collections.Generic;
using System.Globalization;
using EHG.Multiplayer;
using ItemFiltering;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.Accessibility.Players;
using LeAccess.Accessibility.Utils;
using LeAccess.Accessibility.WorldCues;
using LeAccess.I18n;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using Vector3 = System.Numerics.Vector3;

namespace LeAccess.Accessibility.Exploration;

// A place logged for the first time: D2R "the radar has logged a new exit/place".
internal readonly record struct ExplorationFind(ExplorationCategory Category, string Name);

// Builds the scanner's entries in the D2R categories: seen places from the click-listener registry, the quest
// markers the game shows, live enemies and other players, seen ground items, the last death place and the rooms.
internal static class ExplorationProbe
{
    private const string DeathPrefix = "death|";
    private static readonly List<GuidanceTarget> Targets = new();
    private static readonly List<EnemyContact> Contacts = new();
    private static readonly List<OtherPlayer> Others = new();
    private static readonly ExplorationSeenMemory Seen = new();
    private static readonly List<string> Unseen = new();
    private static readonly List<MinimapRoom> Rooms = new();
    private static readonly List<MinimapRoom> Unvisited = new();
    private static readonly List<MinimapRoom> Visited = new();
    private static readonly List<MinimapRoom> Entered = new();
    private static readonly List<FrontierArea> Frontier = new();
    private static readonly List<ExplorationFind> Finds = new();
    private static readonly RoomTracker Tracker = new();
    private static readonly ExplorationArrival Arrival = new();
    private static bool _offsetLogged;
    private static readonly HashSet<int> MovedMarkers = new();
    private const int UnseenLogged = 6;
    private const float MinimumArrival = 1.5f;
    // Right beside the player a place counts as seen even without walkable line of sight: the
    // 2026-10-03 log shows Guardiana Leena, in the active scene and close enough to talk to, never
    // passing the NavMesh line-of-sight test (NPCs behind counters, on platforms or off the mesh).
    private const float CloseSeenRange = 6f;
    // A few objects have very long interaction ranges; those still need line of sight beyond this.
    private const float MaxInteractionSeenRange = 10f;

    internal static ExplorationSeenMemory SeenMemory => Seen;
    internal static string LastFailure { get; private set; }
    internal static string LastStatus { get; private set; }
    // From the last refresh: rooms not yet visited (the room loop), rooms just entered and places just logged.
    internal static IReadOnlyList<MinimapRoom> UnvisitedRooms => Unvisited;
    // Rooms visited or revealed by the game, and the scene session they belong to.
    internal static IReadOnlyList<MinimapRoom> VisitedRooms => Visited;
    internal static long VisitedSession { get; private set; }
    internal static IReadOnlyList<MinimapRoom> EnteredRooms => Entered;
    internal static IReadOnlyList<ExplorationFind> NewFinds => Finds;

    // cameFrom is the scene the player arrived from: its exit is the way back, not a find (D2R).
    internal static bool TryRefresh(MapSectionWorldSnapshot world, IWalkability nav, ExploredMap explored,
        float seenRange, double now, string cameFrom, List<ExplorationEntry> entries)
    {
        entries.Clear();
        Finds.Clear();
        Entered.Clear();
        Unvisited.Clear();
        Visited.Clear();
        VisitedSession = world.Session;
        LastFailure = null;
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || (int)scene.handle != world.SceneHandle) return false;
        Seen.Enter(ExplorationSeenMemory.Scope(world.SceneName, world.Session, world.IsFixedLayout));
        var player = new Vector3((float)world.X, (float)world.Y, (float)world.Z);
        Arrival.Observe(world.Session, player);
        Vector3 origin = default;
        bool hasOrigin = nav is not null && nav.TrySample(player, out origin);
        var view = UnityEngine.Camera.main;
        bool CanSeeAt(Vector3 target, float interactionRange, bool listsOnScreen) =>
            CanSee(player, origin, hasOrigin, target, nav, seenRange, interactionRange) ||
            listsOnScreen && view != null && BossLifeProbe.OnScreen(view, ToUnity(target));
        bool CanSeeHere(Vector3 target, float interactionRange, ExplorationCategory category) =>
            CanSeeAt(target, interactionRange, ExplorationSight.ListsOnScreen(category));

        int places = 0, unseen = 0;
        Unseen.Clear();
        var listeners = WorldObjectClickListener.allListener;
        if (listeners is not null && !listeners.WasCollected)
        {
            for (int index = 0; index < listeners.Count; index++)
            {
                try
                {
                    var listener = listeners[index];
                    // A generated cave keeps its parent scene's objects loaded, and the reverse.
                    if (!IsAlive(listener) || (int)listener.gameObject.scene.handle != world.SceneHandle ||
                        !ExplorationClassifier.TryDescribe(listener, out var place)) continue;
                    var position = GuidanceRouteProbe.Convert(listener.transform.position);
                    if (listener.controllerTargetingUsePlayerY) position.Y = player.Y;
                    string memory = ExplorationSeenMemory.Key(place.Category, place.Name, position);
                    float range = listener.interactionRange;
                    if (!Seen.Contains(memory))
                    {
                        if (!Arrival.IsWayBack(place.Category, place.Destination, cameFrom, position, seenRange) &&
                            !CanSeeAt(position, range, ExplorationSight.ListsOnScreen(place.Category, place.ListsOnScreen)))
                        {
                            if (unseen++ < UnseenLogged)
                                Unseen.Add($"{place.Name} {ExploredMap.Horizontal(player, position):F0}m");
                            continue;
                        }
                        Seen.Add(memory);
                        if (IsFind(place, cameFrom)) Finds.Add(new ExplorationFind(place.Category, place.Found));
                    }
                    entries.Add(new ExplorationEntry($"listener:{listener.GetInstanceID()}", place.Name, place.Category,
                        position, float.IsFinite(range) && range > MinimumArrival ? range : MinimumArrival));
                    places++;
                }
                catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
            }
        }

        int quests = 0;
        try
        {
            Targets.Clear();
            GuidanceTargetReader.TryRead(world, Targets, includeExits: false, requireMinimapVisible: false);
            if (GuidanceTargetReader.LastFailure is { } failure) LastFailure ??= failure;
            foreach (var target in Targets)
            {
                entries.Add(new ExplorationEntry($"objective:{target.Id}", target.Name, ExplorationCategory.Quest,
                    OnGround(target.Id, GuidanceRouteProbe.Convert(target.Position), nav),
                    target.ArrivalRadius > MinimumArrival ? target.ArrivalRadius : MinimumArrival));
                quests++;
            }
        }
        catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
        finally { Targets.Clear(); }

        // Where the Rage of Morditas hunt's blood trails last pointed (reference/rage-of-morditas.md#blood-trails).
        int trails = 0;
        try { trails = Morditas.MorditasAccessibility.AddTrailEntries(entries); }
        catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }

        int enemies = 0;
        try
        {
            Contacts.Clear();
            // Every enemy on screen, as a sighted player sees them, plus any within skill reach off screen.
            float range = ControllerTargetingParadigm._SkillTargetMaxDistance;
            if (float.IsFinite(range) && range > 0 &&
                EnemyScanProbe.TryRead(GameplayEnvironment.IsOnlineClient, range, Contacts, out _, view: UnityEngine.Camera.main))
            {
                foreach (var contact in Contacts)
                {
                    string name = string.IsNullOrWhiteSpace(contact.Name) ? ModText.Get("enemy_scan.unnamed") : contact.Name;
                    name = EnemyClassifier.Decorate(name, contact.Class, ModText.Format);
                    entries.Add(new ExplorationEntry($"enemy:{contact.Id}", name, ExplorationCategory.Enemy,
                        contact.Position, MinimumArrival, contact.Boss));
                    enemies++;
                }
            }
        }
        catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
        finally { Contacts.Clear(); }

        // Other players are listed in sight or not: finding a teammate is the point (D2R).
        int players = 0;
        try
        {
            Others.Clear();
            if (NearbyPlayerProbe.TryReadAll(Others))
                foreach (var other in Others)
                {
                    entries.Add(new ExplorationEntry($"player:{other.InstanceId}", PlayerIdentityText.Compose(other.Name, other.Character, other.Level), ExplorationCategory.Player,
                        GuidanceRouteProbe.Convert(other.Position), MinimumArrival));
                    players++;
                }
            if (NearbyPlayerProbe.LastFailure is { } failure) LastFailure ??= failure;
        }
        catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
        finally { Others.Clear(); }

        int items = 0;
        try { items = AddItems(CanSeeHere, entries) + AddGold(CanSeeHere, entries); }
        catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }

        int breakables = 0;
        try { breakables = BreakableProbe.Add(GameplayEnvironment.IsOnlineClient, CanSeeHere, entries); }
        catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
        if (BreakableProbe.LastFailure is { } breakableFailure) LastFailure ??= breakableFailure;

        bool death = AddDeathPlace(entries);

        int unexplored = 0, rooms = 0, visited = 0, areas = 0;
        string source = "none", roomGap = "none";
        bool hasRooms = false;
        // A failed room read falls back to the frontier rather than listing nothing.
        try { hasRooms = MinimapRoomReader.TryRead(world, now, Rooms); }
        catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
        try
        {
            if (hasRooms)
            {
                source = "rooms";
                rooms = Rooms.Count;
                unexplored = Tracker.AddRooms(Rooms, MinimapRoomReader.Layout, player, Seen, explored, RoomName,
                    UnexploredRoomName, entries, Entered, out visited);
                // The minimap's fog also ends where no room volume reaches (reference/exploration-mode.md#ground-no-room-covers-2026-10-19).
                areas = Tracker.AddFrontier(explored, Frontier, UnexploredAreaName, entries, Rooms);
                unexplored += areas;
                foreach (var room in Rooms)
                    if (!room.Revealed && !Seen.Contains(RoomTracker.VisitKey(MinimapRoomReader.Layout, room.Index)))
                        Unvisited.Add(room);
                    else Visited.Add(room);
                // Diagnostic: distance to the nearest room the player is not in.
                if (RoomTracker.TryNearest(Rooms, player, Vector3.UnitZ, Vector3.UnitX, out float gap, out _, out _))
                    roomGap = gap.ToString("F0", CultureInfo.InvariantCulture);
            }
            else if (explored is not null)
            {
                source = "frontier";
                unexplored = Tracker.AddFrontier(explored, Frontier, UnexploredRoomName, entries);
            }
        }
        catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
        finally { Rooms.Clear(); }

        LastStatus = $"places={places} unseen={unseen} remembered={Seen.Count} quests={quests} trails={trails} enemies={enemies} " +
            $"players={players} items={items} breakables={breakables} death={death} unexplored={unexplored} source={source} rooms={rooms} " +
            $"visited_rooms={visited} unexplored_areas={areas} room_gap={roomGap} frontier_pending={explored?.FrontierPending ?? 0} origin={hasOrigin} " +
            $"came_from={cameFrom ?? "none"} not_yet_seen=[{string.Join("; ", Unseen)}]";
        return true;
    }

    // A quest marker is the minimap icon, which floats above its floor; its ground point is where one walks. One past
    // the mesh edge stands at the nearest point within sight, as the ways' guide approaches it.
    private static Vector3 OnGround(int id, Vector3 marker, IWalkability nav)
    {
        var ground = ExploredMap.MarkerGround(marker, ExplorationBeacon.ExploreRadius, nav);
        float moved = ExploredMap.Horizontal(marker, ground);
        if (ground != marker && (moved > 0 ? MovedMarkers.Add(id) : !_offsetLogged))
        {
            _offsetLogged |= moved <= 0;
            Plugin.Log.LogInfo(string.Create(CultureInfo.InvariantCulture,
                $"[Exploration] quest marker icon_y={marker.Y:F2} ground_y={ground.Y:F2} offset={marker.Y - ground.Y:F2} moved={moved:F2} icon={marker.X:F1},{marker.Z:F1} ground={ground.X:F1},{ground.Z:F1}"));
        }
        return ground;
    }

    // Kept per scene with the seen places until the next death there (D2R keeps the last death place per map).
    internal static void RecordDeath(Vector3 position)
    {
        if (!ExploredMap.Finite(position)) return;
        Seen.RemovePrefix(DeathPrefix);
        Seen.Add(string.Create(CultureInfo.InvariantCulture, $"{DeathPrefix}{position.X:F1}|{position.Y:F1}|{position.Z:F1}"));
    }

    // Exits, waypoints and the stash are the D2R fixed places announced when first logged.
    private static bool IsFind(ExplorationPlace place, string cameFrom) => place.Category switch
    {
        ExplorationCategory.Exit => place.Destination is null || place.Destination != cameFrom,
        ExplorationCategory.Waypoint or ExplorationCategory.Stash => true,
        _ => false
    };

    // Loot the game labels and the loot filter shows, in sight now; never remembered.
    private static int AddItems(Func<Vector3, float, ExplorationCategory, bool> canSee, List<ExplorationEntry> entries)
    {
        var all = GroundItemVisuals.all;
        if (all is null || all.WasCollected) return 0;
        int count = 0;
        for (int index = 0; index < all.Count; index++)
        {
            var item = all[index];
            if (!IsAlive(item) || !item.isActiveAndEnabled) continue;
            var label = item.label;
            if (!IsAlive(label) || label.lastFilterResult == Rule.RuleOutcome.HIDE) continue;
            var text = label.itemText;
            if (!IsAlive(text) || !text.isActiveAndEnabled || text.alpha <= 0) continue;
            string name = TextUtils.CleanText(text.text);
            if (string.IsNullOrWhiteSpace(name)) continue;
            var position = GuidanceRouteProbe.Convert(item.transform.position);
            if (!canSee(position, 0, ExplorationCategory.Item)) continue;
            entries.Add(new ExplorationEntry($"item:{item.id}", name, ExplorationCategory.Item, position, MinimumArrival));
            count++;
        }
        return count;
    }

    // Gold piles have their own registry and an optional label; the amount is the name.
    private static int AddGold(Func<Vector3, float, ExplorationCategory, bool> canSee, List<ExplorationEntry> entries)
    {
        var all = GroundGoldVisuals.all;
        if (all is null || all.WasCollected) return 0;
        int count = 0;
        for (int index = 0; index < all.Count; index++)
        {
            var gold = all[index];
            if (!IsAlive(gold) || !gold.isActiveAndEnabled || gold.goldValue <= 0) continue;
            var position = GuidanceRouteProbe.Convert(gold.transform.position);
            if (!canSee(position, 0, ExplorationCategory.Item)) continue;
            entries.Add(new ExplorationEntry($"gold:{gold.id}", ModText.Format("gold.pickup", gold.goldValue),
                ExplorationCategory.Item, position, MinimumArrival));
            count++;
        }
        return count;
    }

    private static UnityEngine.Vector3 ToUnity(Vector3 point) => new(point.X, point.Y, point.Z);

    private static bool AddDeathPlace(List<ExplorationEntry> entries)
    {
        foreach (string key in Seen.WithPrefix(DeathPrefix))
        {
            var parts = key.Substring(DeathPrefix.Length).Split('|');
            if (parts.Length != 3 || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) continue;
            entries.Add(new ExplorationEntry("death", ModText.Get("exploration.death_place"), ExplorationCategory.LastDeath,
                new Vector3(x, y, z), MinimumArrival));
            return true;
        }
        return false;
    }

    private static string RoomName(int number) => ModText.Format("exploration.room", number);
    private static string UnexploredRoomName(int number) => ModText.Format("exploration.unexplored_room", number);
    private static string UnexploredAreaName(int number) => ModText.Format("exploration.unexplored_area", number);

    // Seen = within range and in NavMesh line of sight, or right beside the player. Doors and exits may
    // stand off the walkable mesh, so those count only within half the range.
    private static bool CanSee(Vector3 player, Vector3 origin, bool hasOrigin, Vector3 target, IWalkability nav,
        float range, float interactionRange)
    {
        float dx = target.X - player.X, dz = target.Z - player.Z;
        float squared = dx * dx + dz * dz;
        // Within the game's own interaction range (where the NPC name is announced) it has been met.
        float close = float.IsFinite(interactionRange) && interactionRange > CloseSeenRange
            ? MathF.Min(interactionRange, MaxInteractionSeenRange) : CloseSeenRange;
        if (!float.IsFinite(squared)) return false;
        if (squared <= close * close) return true;
        if (squared > range * range) return false;
        if (!hasOrigin) return false;
        if (nav.TrySample(target, out var onMesh)) return nav.Clear(origin, onMesh);
        return squared <= range * range / 4;
    }

    internal static void Reset()
    {
        Seen.Clear();
        Rooms.Clear();
        Unvisited.Clear();
        Visited.Clear();
        VisitedSession = 0;
        Entered.Clear();
        Finds.Clear();
        Frontier.Clear();
        Tracker.Clear();
        Arrival.Clear();
        MinimapRoomReader.Reset();
        ShrineObjects.Reset();
        BreakableProbe.Reset();
        Targets.Clear();
        Contacts.Clear();
        Others.Clear();
        LastFailure = LastStatus = null;
        _offsetLogged = false;
        MovedMarkers.Clear();
    }
}
