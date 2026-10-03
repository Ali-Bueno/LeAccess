namespace UnityEngine
{
    public class Object
    {
        private static int next;
        public IntPtr Pointer { get; } = (IntPtr)(++next);
        public bool WasCollected;
    }
    public class GameObject : Object
    { public bool activeInHierarchy = true; public SceneManagement.Scene scene = SceneManagement.SceneManager.Active; }
    public static class Application { public static bool isFocused = true; }
    public static class Time { public static double unscaledTimeAsDouble; }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name; public int handle; public bool isLoaded; }
    public static class SceneManager
    {
        public static Scene Active = new() { name = "Map", handle = 1, isLoaded = true };
        public static Scene GetActiveScene() => Active;
    }
}
namespace BepInEx.Configuration
{
    public class ConfigEntry<T> { public T Value; }
    public class ConfigFile
    {
        public ConfigEntry<bool> Enabled;
        public ConfigEntry<double> Interval;
        public ConfigEntry<T> Bind<T>(string section, string key, T value, string help)
        {
            var entry = new ConfigEntry<T> { Value = value };
            if (entry is ConfigEntry<bool> enabled) Enabled = enabled;
            if (entry is ConfigEntry<double> interval) Interval = interval;
            return entry;
        }
    }
}
namespace LE.UI { public static class LoadingScreen { public static bool IsVisible; } }
namespace LeAccess.Accessibility.Navigation
{
    public static class NavigationAudioState { public static bool IsSuspended; }
    public static class GameplayInputState { public static bool Owns = true; public static bool OwnsInput() => Owns; }
}
namespace LeAccess.Accessibility.Utils
{ public static class TextUtils { public static string CleanText(string text) => text?.Trim(); } }
namespace LeAccess.Accessibility
{
    public static class ScreenReader
    {
        public static bool SpokeThisFrame, Speaking, Available = true;
        public static List<string> Spoken = new();
        public static bool IsAvailable() => Available;
        public static bool IsSpeaking() => Speaking;
        public static void Speak(string text, bool interrupt)
        { if (interrupt) throw new Exception("Quest updates must queue without interrupting"); Spoken.Add(text); SpokeThisFrame = true; }
    }
}
namespace LeAccess
{
    public static class Plugin { public static Logger Log = new(); }
    public class Logger { public void LogInfo(string text) { } public void LogWarning(string text) { } }
}
public class GameList<T> : List<T> { public bool WasCollected; }
public enum QuestState { NotStarted, Active, Completed, Failed, NoLongerRelevant }
public class Quest : UnityEngine.Object
{
    public int id;
    public string displayName = "Forest";
    public bool playerVisible = true;
    public StatefulQuestList Owner;
    public StatefulQuestList getLocalStatefulQuestList() => Owner;
}
public class QuestStep : UnityEngine.Object { public Quest quest; public int id; }
public class Objective : UnityEngine.Object
{ public Quest quest; public QuestStep questStep; public int id; public bool isIncremental; }
public class StatefulObjective : UnityEngine.Object
{
    public Objective objective;
    public StatefulQuest statefulQuest;
    public QuestState state = QuestState.Active;
    public int progress;
    public string Text = "Reach the camp";
    public string GetObjectiveActiveText() => Text + (objective.isIncremental ? $" {progress}/10" : "");
}
public class StatefulQuest : UnityEngine.Object
{
    public Quest quest;
    public QuestStep currentStep;
    public GameList<StatefulObjective> objectives = new();
    public QuestState Status = QuestState.Active;
    public QuestState getState() => Status;
}
public class StatefulQuestList : UnityEngine.Object { public GameList<StatefulQuest> trackedQuests = new(); }
public class StatefulQuestListHolder : UnityEngine.Object
{ public bool initialised = true, hasBeenNullified; public StatefulQuestList statefulQuestList = new(); }
public class Provider : UnityEngine.Object
{
    public StatefulQuestListHolder Campaign = new(), Monolith = new(), Stash = new();
    public StatefulQuestListHolder getPlayerQuestListHolder() => Campaign;
    public StatefulQuestListHolder getPlayerMonolithQuestListHolder() => Monolith;
    public StatefulQuestListHolder getPlayerStashQuestListHolder() => Stash;
}
public static class PlayerFinder
{
    public static UnityEngine.GameObject player = new();
    public static UnityEngine.Object Character = new();
    public static Provider Provider = new();
    public static UnityEngine.Object getPlayerData() => Character;
    public static Provider getLocalStatefulQuestListProvider() => Provider;
}
public static class SceneList
{
    public static bool Gameplay = true;
    public static bool TryGetNonDefaultSceneDetails(string name, out object details) { details = new(); return Gameplay; }
}
public static class Localization { public static QuestLocalization Quests = new(); }
public class QuestLocalization : UnityEngine.Object
{ public string GetQuestName(int id, string fallback) => fallback; }
public class EpochInputManager : UnityEngine.Object
{ public static EpochInputManager instance = new(); public bool isInputFieldActive; }
public class ScreenKeyboardManager : UnityEngine.Object
{ public static ScreenKeyboardManager Instance = new(); public bool _enabled; }
