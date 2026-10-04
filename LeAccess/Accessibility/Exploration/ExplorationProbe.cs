using System;
using System.Collections.Generic;
using EHG.Multiplayer;
using LeAccess.Accessibility.Combat;
using LeAccess.Accessibility.Landmarks;
using LeAccess.Accessibility.MapSections;
using LeAccess.Accessibility.Navigation.Guidance;
using LeAccess.Accessibility.Npcs;
using LeAccess.I18n;
using UnityEngine.SceneManagement;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;
using Vector3 = System.Numerics.Vector3;

namespace LeAccess.Accessibility.Exploration;

// Builds the scanner's entries: seen places from the click-listener registry, the quest markers and
// encounter arrow the game shows, and live enemies in skill-targeting range.
internal static class ExplorationProbe
{
    private static readonly List<GuidanceTarget> Targets = new();
    private static readonly List<EnemyContact> Contacts = new();
    private static readonly ExplorationSeenMemory Seen = new();
    private static readonly List<string> Unseen = new();
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

    internal static bool TryRefresh(MapSectionWorldSnapshot world, IWalkability nav, float seenRange,
        List<ExplorationEntry> entries)
    {
        entries.Clear();
        LastFailure = null;
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || (int)scene.handle != world.SceneHandle) return false;
        Seen.Enter(ExplorationSeenMemory.Scope(world.SceneName, world.Session, world.IsFixedLayout));
        var player = new Vector3((float)world.X, (float)world.Y, (float)world.Z);
        Vector3 origin = default;
        bool hasOrigin = nav is not null && nav.TrySample(player, out origin);
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
                    if (!TryDescribe(listener, out var category, out string name)) continue;
                    var position = GuidanceRouteProbe.Convert(listener.transform.position);
                    if (listener.controllerTargetingUsePlayerY) position.Y = player.Y;
                    string memory = ExplorationSeenMemory.Key(category, name, position);
                    float range = listener.interactionRange;
                    if (!Seen.Contains(memory))
                    {
                        if (!CanSee(player, origin, hasOrigin, position, nav, seenRange, range))
                        {
                            if (unseen++ < UnseenLogged) Unseen.Add(name);
                            continue;
                        }
                        Seen.Add(memory);
                    }
                    entries.Add(new ExplorationEntry($"listener:{listener.GetInstanceID()}", name, category, position,
                        float.IsFinite(range) && range > MinimumArrival ? range : MinimumArrival));
                    places++;
                }
                catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
            }
        }

        int objectives = 0;
        try
        {
            Targets.Clear();
            GuidanceTargetReader.TryRead(world, Targets, includeExits: false, requireMinimapVisible: false);
            if (GuidanceTargetReader.LastFailure is { } failure) LastFailure ??= failure;
            foreach (var target in Targets)
            {
                entries.Add(new ExplorationEntry($"objective:{target.Id}", target.Name, ExplorationCategory.Objectives,
                    GuidanceRouteProbe.Convert(target.Position),
                    target.ArrivalRadius > MinimumArrival ? target.ArrivalRadius : MinimumArrival));
                objectives++;
            }
        }
        catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
        finally { Targets.Clear(); }

        int enemies = 0;
        try
        {
            Contacts.Clear();
            float range = ControllerTargetingParadigm._SkillTargetMaxDistance;
            if (float.IsFinite(range) && range > 0 &&
                EnemyScanProbe.TryRead(GameplayEnvironment.IsOnlineClient, range, Contacts, out _))
            {
                foreach (var contact in Contacts)
                {
                    string name = string.IsNullOrWhiteSpace(contact.Name) ? ModText.Get("enemy_scan.unnamed") : contact.Name;
                    entries.Add(new ExplorationEntry($"enemy:{contact.Id}", name, ExplorationCategory.Enemies,
                        contact.Position, MinimumArrival, contact.Boss));
                    enemies++;
                }
            }
        }
        catch (Exception ex) { LastFailure ??= $"{ex.GetType().Name}: {ex.Message}"; }
        finally { Contacts.Clear(); }

        LastStatus = $"places={places} unseen={unseen} remembered={Seen.Count} objectives={objectives} enemies={enemies} origin={hasOrigin} not_yet_seen=[{string.Join("; ", Unseen)}]";
        return true;
    }

    // Same identities as the NPC, landmark and service readers; chests and loot keep their own loops (D2R).
    // Like the NPC probe, any loaded scene counts: quest NPCs can live in an additive scene.
    private static bool TryDescribe(WorldObjectClickListener listener, out ExplorationCategory category, out string name)
    {
        category = default;
        name = null;
        if (!IsAlive(listener) || !listener.isActiveAndEnabled || !IsAlive(listener.gameObject) ||
            !listener.gameObject.activeInHierarchy || !listener.gameObject.scene.IsValid() ||
            !listener.gameObject.scene.isLoaded || !listener.CanInteract()) return false;
        string service = WorldServiceReader.ReadServiceKey(listener);
        // The stash and waypoints also pass the NPC test; objects belong with services and travel.
        if (service is "service.stash" or "service.forge" or "service.waypoint")
        {
            category = service == "service.waypoint" ? ExplorationCategory.Travel : ExplorationCategory.Services;
            name = ModText.Get(service);
        }
        else if (WorldServiceReader.IsNpc(listener))
        {
            category = ExplorationCategory.Characters;
            name = NpcNameReader.Read(listener, out _);
        }
        else if (service is not null)
        {
            category = ExplorationCategory.Services;
            name = ModText.Get(service);
        }
        else
        {
            name = LandmarkProbe.ReadLabel(listener, listener.gameObject.scene, out bool portal, out bool transition);
            if (transition) { category = ExplorationCategory.Exits; name = ModText.Format("guidance.exit_to", name); }
            else if (portal) category = ExplorationCategory.Travel;
            else return false;
        }
        return !string.IsNullOrWhiteSpace(name);
    }

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
        Targets.Clear();
        Contacts.Clear();
        LastFailure = LastStatus = null;
    }
}
