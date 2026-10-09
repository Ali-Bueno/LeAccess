namespace UnityEngine
{
    internal class Object
    {
        private static int _next;
        private readonly int _id = ++_next;
        internal bool WasCollected;
        internal IntPtr Pointer { get { Guard(); return new(_id); } }
        internal int GetInstanceID() { Guard(); return _id; }
        private void Guard() { if (WasCollected) throw new InvalidOperationException("Collected object dereference"); }
        public static bool operator ==(Object a, Object b) { a?.Guard(); b?.Guard(); return ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => _id;
        internal T TryCast<T>() where T : Object => this as T;
    }
    internal class GameObject : Object
    {
        internal readonly List<Behaviour> Components = new();
        internal GameObject Parent;
        internal bool activeInHierarchy = true;
        internal UnityEngine.SceneManagement.Scene scene = new(1);
        internal Transform transform;
        internal GameObject() => transform = new Transform { Owner = this };
    }
    internal class Transform : Object
    {
        internal GameObject Owner;
        internal bool IsChildOf(Transform parent)
        {
            for (var current = Owner; current is not null; current = current.Parent)
                if (ReferenceEquals(current.transform, parent)) return true;
            return false;
        }
        internal Vector3 position;
        internal Vector3 right = new(1, 0, 0), forward = new(0, 0, 1);
    }
    internal class Behaviour : Object
    {
        internal GameObject gameObject = new();
        internal Behaviour() { gameObject.Components.Add(this); }
        internal T GetComponent<T>() where T : Behaviour => gameObject.Components.OfType<T>().FirstOrDefault();
        internal T GetComponentInParent<T>() where T : Behaviour
        {
            for (var current = gameObject; current is not null; current = current.Parent)
            { var value = current.Components.OfType<T>().FirstOrDefault(); if (value is not null) return value; }
            return null;
        }
        internal Transform transform => gameObject.transform;
        internal bool isActiveAndEnabled = true;
    }
    internal class MonoBehaviour : Behaviour { }
    internal class Camera : MonoBehaviour { internal static Camera main = new(); }
    internal class CanvasGroup : Object { internal float alpha = 1; }
    internal class CanvasRenderer : Object { internal bool cull; }
    internal struct Vector3
    {
        internal float x, y, z;
        internal Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        internal float magnitude => MathF.Sqrt(x * x + y * y + z * z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        internal static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
    }
    internal static class Time { internal static double unscaledTimeAsDouble; }
}
namespace UnityEngine.SceneManagement
{
    internal readonly record struct Scene(int handle)
    {
        internal bool isLoaded => handle != 0; internal bool IsValid() => handle != 0;
        internal string name => SceneManager.Names.TryGetValue(handle, out var value) ? value : null;
    }
    internal static class SceneManager
    {
        internal static int Handle = 1;
        internal static readonly Dictionary<int, string> Names = new();
        internal static Scene GetActiveScene() => new(Handle);
    }
}
internal sealed class Registry<T> : List<T> { internal bool WasCollected; }
internal static class PlayerFinder
{
    internal static UnityEngine.GameObject player = new();
    internal static ActorVisuals playerActorVisuals = new();
}
internal sealed class ActorVisuals : UnityEngine.MonoBehaviour { internal bool dead; internal ActorSync actorSync; }
internal sealed class ActorSync : UnityEngine.MonoBehaviour { internal ActorVisuals actorVisuals; }
internal sealed class LocalPlayer : UnityEngine.MonoBehaviour { internal static LocalPlayer instance; internal ActorSync actorSync; }
namespace EHG.Multiplayer { internal static class GameplayEnvironment { internal static bool IsOnlineClient; } }
internal sealed class TextLabel : UnityEngine.MonoBehaviour
{ internal float alpha = 1; internal UnityEngine.CanvasRenderer canvasRenderer = new(); }
internal class PickupableGroundLabel : UnityEngine.MonoBehaviour { internal TextLabel itemText = new(); }
internal sealed class GroundGoldLabel : PickupableGroundLabel { }
internal sealed class GroundItemLabel : PickupableGroundLabel
{
    internal UnityEngine.CanvasGroup canvasGroup = new();
    internal ItemFiltering.Rule.RuleOutcome lastFilterResult;
}
namespace ItemFiltering { internal class Rule { internal enum RuleOutcome { SHOW, HIDE, HIGHLIGHT_DEPRECATED } } }
internal sealed class ItemData
{
    private static int _nextPointer;
    internal readonly IntPtr Pointer = new(++_nextPointer);
    internal int Classified;
    internal bool WasCollected, Equipment = true, Idol, UniqueSetOrLegendary, Rare, Exalted, Magic, Rune, Glyph, AffixShard;
    internal bool isEquipment() { Classified++; return Equipment; }
    internal bool isIdol() => Idol;
    internal bool isUniqueSetOrLegendary() => UniqueSetOrLegendary;
    internal bool isRare() => Rare;
    internal bool isExaltedItem() => Exalted;
    internal bool isMagic() => Magic;
    internal bool IsRune() => Rune;
    internal bool IsGlyph() => Glyph;
    internal bool IsAffixShard() => AffixShard;
}
internal sealed class GroundGoldVisuals : UnityEngine.MonoBehaviour
{
    internal static Registry<GroundGoldVisuals> all = new();
    internal int goldValue = 10;
    internal GroundGoldLabel label = new();
    internal WorldObjectClickListener worldObjectClickListener = new();
}
internal sealed class GroundPotionVisuals : UnityEngine.MonoBehaviour
{
    internal static Registry<GroundPotionVisuals> all = new();
    internal WorldObjectClickListener worldObjectClickListener = new();
}
internal sealed class GroundXPTomeVisuals : UnityEngine.MonoBehaviour
{
    internal static Registry<GroundXPTomeVisuals> all = new();
    internal WorldObjectClickListener worldObjectClickListener = new();
}
internal sealed class GroundFavorTomeVisuals : UnityEngine.MonoBehaviour
{
    internal static Registry<GroundFavorTomeVisuals> all = new();
    internal WorldObjectClickListener worldObjectClickListener = new();
}
internal sealed class GroundItemVisuals : UnityEngine.MonoBehaviour
{
    internal static Registry<GroundItemVisuals> all = new();
    internal ItemData itemData = new();
    internal GroundItemLabel label = new();
    internal WorldObjectClickListener worldObjectClickListener = new();
}
internal sealed class WorldObjectClickListener : UnityEngine.MonoBehaviour
{
    internal static Registry<WorldObjectClickListener> allListener = new();
    internal bool requiresClick = true, Available = true, Npc, Readable, Throw, controllerTargetingUsePlayerY;
    internal float interactionRange = 5;
    internal string ServiceKey;
    internal HoverTagOnMouseOver hoverTagOnMouseOver;
    internal bool CanInteract() { if (Throw) throw new InvalidOperationException("Native availability failed"); return Available; }
}
internal sealed class Targetable
{ internal bool WasCollected; internal UnityEngine.MonoBehaviour item; }
internal sealed class Targeting
{ internal bool WasCollected; internal Targetable ActiveTarget; }
internal sealed class EpochInputManager : UnityEngine.MonoBehaviour
{
    internal static EpochInputManager instance = new();
    internal Targeting InteractableTargeting = new();
    internal TargetingConfiguration targetingConfiguration = new();
}
internal sealed class TargetingConfiguration { internal bool WasCollected; internal float itemsRange = 5; }
namespace LeAccess.Accessibility.Npcs
{
    internal static class NpcWorldProbe { internal static bool IsNpc(WorldObjectClickListener listener) => listener.Npc; }
    internal static class WorldServiceReader
    {
        internal static string ReadServiceKey(WorldObjectClickListener listener) => listener.ServiceKey;
        internal static bool IsReadable(WorldObjectClickListener listener) => listener.Readable;
    }
}
internal sealed class HoverTagOnMouseOver : UnityEngine.MonoBehaviour
{
    internal enum HoverTargetType { OTHER, ZONE_TRANSITION, DUNGEON_TRANSITION, SIGNPOST }
    internal HoverTargetType hoverTargetType;
    internal string hoverText, localizationKey;
    internal TransitionOrGateInteraction sceneInteraction, dungeonTransitionInteraction;
}
internal class TransitionOrGateInteraction : UnityEngine.MonoBehaviour { internal string sceneName; }
internal sealed class UsePlayerSpawnInteraction : TransitionOrGateInteraction { internal string ToScene; }
internal sealed class CaveLobbyExitInteraction : UnityEngine.MonoBehaviour { }
internal sealed class SceneDetails : UnityEngine.Object { internal string LocalizedName; }
internal static class SceneList
{
    internal static readonly Dictionary<string, SceneDetails> Scenes = new();
    internal static bool TryGetNonDefaultSceneDetails(string name, out SceneDetails details) => Scenes.TryGetValue(name, out details);
}
namespace LE.PCG
{
    internal sealed class Level : UnityEngine.MonoBehaviour { internal UnityEngine.SceneManagement.Scene ParentScene; }
    internal static class LevelUtility
    {
        internal static readonly Dictionary<int, Level> Levels = new();
        internal static bool TryGetLevel(UnityEngine.SceneManagement.Scene scene, out Level level) =>
            Levels.TryGetValue(scene.handle, out level);
    }
}
internal sealed class ChestSync : UnityEngine.MonoBehaviour { }
internal sealed class Shrine : UnityEngine.MonoBehaviour { }
internal sealed class ShrineSync : UnityEngine.MonoBehaviour { }
internal sealed class ShrineVisualsCreator : UnityEngine.MonoBehaviour { }
internal sealed class ShrinesManager : UnityEngine.MonoBehaviour
{
    internal static readonly Dictionary<string, ShrinesManager> Scenes = new();
    internal readonly Registry<UnityEngine.GameObject> Objects = new();
    internal static bool TryGetForScene(string sceneName, out ShrinesManager manager) =>
        Scenes.TryGetValue(sceneName ?? "", out manager);
    internal Registry<UnityEngine.GameObject> GetAllShrineObjects() => Objects;
}
internal sealed class TimeRiftInteraction : UnityEngine.MonoBehaviour { }
internal sealed class ActivateTimeRift : UnityEngine.MonoBehaviour { }
internal sealed class TimeBeastRift : UnityEngine.MonoBehaviour { }
internal sealed class MonolithPortal : UnityEngine.MonoBehaviour { }
internal sealed class ShatteredRoadPortalInteractable : UnityEngine.MonoBehaviour { }
internal sealed class NemesisEncounterInteractableObject : UnityEngine.MonoBehaviour { }
internal sealed class OneShotCacheInteractableObject : UnityEngine.MonoBehaviour { }
internal static class Localization
{
    internal static readonly Dictionary<string, string> Texts = new();
    internal static string GetText(string key, string fallback = "") => Texts.TryGetValue(key, out var value) ? value : string.IsNullOrEmpty(fallback) ? key : fallback;
}
namespace LeAccess.I18n
{
    internal static class ModText
    {
        internal static string Get(string key) => key switch
        {
            "exploration.shrine" => "santuario", "exploration.time_rift" => "fisura temporal",
            "exploration.nemesis" => "Némesis", "exploration.chest" => "cofre", _ => key
        };
    }
}
namespace LeAccess.Accessibility.Utils
{
    internal static class TextUtils { internal static string CleanText(string text) => text?.Trim(); }
}
internal sealed class VaultsOfUncertainFateChest : UnityEngine.MonoBehaviour { internal bool hasBeenOpened; }
namespace LE.Gameplay.Monolith { internal sealed class MonolithChestInteraction : UnityEngine.MonoBehaviour { } }
namespace LE.Gameplay.PrimalHunt { internal sealed class CaveTreasureChest : UnityEngine.MonoBehaviour { } }
namespace LE.Gameplay.Tombs { internal sealed class TombRewardChestInteraction : UnityEngine.MonoBehaviour { } }
namespace Networking.Multiplayer.Interactables.Portals
{
    internal sealed class PortalVisualController : UnityEngine.MonoBehaviour
    {
        internal UnityEngine.GameObject mouseListenerObj;
        internal bool Blocked;
        internal bool isNonInteractableByPlayer() => Blocked;
    }
}
namespace LeAccess.Accessibility.Navigation
{
    internal static class NavigationAudioState { internal static bool IsSuspended; }
    internal static class GameplayInputState { internal static bool Owns = true; internal static bool OwnsInput() => Owns; }
}
namespace LeAccess.Accessibility.ScreenContent
{ internal static class ScreenContentAnnouncer { internal static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Dialogue
{ internal static class DialogueAnnouncer { internal static bool HasActiveContent; } }
namespace BepInEx.Configuration
{
    internal sealed class ConfigEntry<T> { internal T Value; }
    internal sealed class ConfigFile
    {
        private readonly Dictionary<string, object> _values = new();
        internal ConfigEntry<T> Bind<T>(string section, string key, T value, object description)
        { var entry = new ConfigEntry<T> { Value = value }; _values[key] = entry; return entry; }
        internal void Set<T>(string key, T value) => ((ConfigEntry<T>)_values[key]).Value = value;
    }
    internal sealed class ConfigDescription { internal ConfigDescription(string text, object range) { } }
    internal sealed class AcceptableValueRange<T> { internal AcceptableValueRange(T from, T to) { } }
}
namespace LeAccess
{
    internal static class Plugin { internal static Logger Log = new(); }
    internal sealed class Logger
    {
        internal readonly List<string> Info = new(), Warnings = new();
        internal void LogInfo(string value) => Info.Add(value);
        internal void LogWarning(string value) => Warnings.Add(value);
    }
}
namespace LeAccess.Accessibility.Audio
{
    internal sealed class RadarAudioOutput : IDisposable
    {
        internal static readonly List<RadarAudioOutput> Instances = new();
        internal static bool ThrowInitialize;
        internal static int Creates;
        internal bool IsRunning = true, IsPlaying, Disposed;
        internal int Plays, Silences, Recoveries;
        internal string Diagnostics => $"plays={Plays}";
        internal RadarAudioOutput(string name, int latency)
        { Creates++; if (ThrowInitialize) throw new InvalidOperationException("Device unavailable"); Instances.Add(this); }
        internal bool TryRecover(out string status) { Recoveries++; status = "recovered"; IsRunning = true; return true; }
        internal void Play(float[] samples) { Plays++; }
        internal void Silence() { Silences++; }
        public void Dispose() { Disposed = true; }
    }
    internal static class WorldLoopBank
    {
        internal static bool Available;
        internal static string LastFailure;
        internal static bool Has(NaturalCueKind kind) => Available && kind != NaturalCueKind.Discovery;
    }
    internal sealed class LoopAudioOutput : IDisposable
    {
        internal static readonly List<LoopAudioOutput> Instances = new();
        internal bool IsRunning = true, Disposed;
        internal int Silences;
        internal List<LoopVoice> Voices = new();
        internal string Diagnostics => $"voices={Voices.Count}";
        internal LoopAudioOutput(string name, int latency, double glide) => Instances.Add(this);
        internal bool TryRecover(out string status) { status = null; IsRunning = true; return true; }
        internal void SetVoices(IReadOnlyList<LoopVoice> voices) => Voices = voices.ToList();
        internal void Silence() { Silences++; Voices.Clear(); }
        public void Dispose() { Disposed = true; }
    }
}
