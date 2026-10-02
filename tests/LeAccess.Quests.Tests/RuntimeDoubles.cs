namespace UnityEngine
{
    public class Object
    {
        private static int next;
        public IntPtr Pointer { get; } = (IntPtr)(++next);
        public bool WasCollected;
        public T TryCast<T>() where T : class => this as T;
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
    }
    public class Behaviour : Component { public bool isActiveAndEnabled = true; }
    public class GameObject : Object
    {
        public bool activeInHierarchy = true;
        public Transform transform;
        private readonly List<Component> components = new();
        public GameObject() { transform = new Transform { gameObject = this }; components.Add(transform); }
        public T Add<T>(T component) where T : Component { component.gameObject = this; components.Add(component); return component; }
        public T GetComponentInParent<T>() where T : class
        {
            for (var t = transform; t != null; t = t.parent)
            { var result = t.gameObject.components.OfType<T>().FirstOrDefault(); if (result != null) return result; }
            return null;
        }
        public T GetComponent<T>() where T : class => components.OfType<T>().FirstOrDefault();
        public T[] GetComponents<T>() where T : class => components.OfType<T>().ToArray();
    }
    public class Transform : Component
    {
        public Transform parent;
        public bool IsChildOf(Transform owner)
        { for (Transform t = this; t != null; t = t.parent) if (t.Pointer == owner.Pointer) return true; return false; }
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        public T[] GetComponents<T>() where T : class => gameObject.GetComponents<T>();
    }
    public class RectTransform : Transform { }
    public class Canvas : Behaviour { }
    public class CanvasGroup : Behaviour { public float alpha = 1; public bool ignoreParentGroups; }
    public class CanvasRenderer : Object { public bool cull; }
    public static class Application { public static bool isFocused = true; }
}
namespace TMPro
{
    public class TextMeshProUGUI : UnityEngine.Behaviour
    {
        public string text;
        public float alpha = 1;
        public UnityEngine.CanvasRenderer canvasRenderer = new();
    }
}
public sealed class GameList<T> : List<T> { public bool WasCollected; }
public class Quest : UnityEngine.Object
{
    public int id;
    public StatefulQuestList List;
    public StatefulQuestList getLocalStatefulQuestList() => List;
}
public enum QuestState { NotStarted, Active, Completed, Failed, NoLongerRelevant }
public class QuestStep : UnityEngine.Object { public Quest quest; public int id; public string displayText; }
public class Objective : UnityEngine.Object { public Quest quest; public QuestStep questStep; public string Text; }
public class ObjectiveAndState : UnityEngine.Object { public Objective objective; public QuestState state; }
public class StatefulQuest : UnityEngine.Object
{
    public Quest quest;
    public QuestStep currentStep;
    public QuestState State = QuestState.Active;
    public QuestState getState() => State;
}
public class StatefulQuestList : UnityEngine.Object
{
    public StatefulQuest Selected;
    public GameList<ObjectiveAndState> Objectives = new();
    public StatefulQuest getStatefulQuest(Quest quest) => Selected;
    public GameList<ObjectiveAndState> getObjectiveStatesForQuest(Quest quest) => Objectives;
}
public static class Localization { public static QuestLocalization Quests = new(); }
public class QuestLocalization : UnityEngine.Object
{ public string GetStepDescription(int quest, int step, string fallback) => fallback; }
public class QuestPanel : UnityEngine.Behaviour { public QuestButtonGenerator buttonGenerator; public bool questsHidden; }
public class QuestButtonGenerator : UnityEngine.Behaviour
{
    public QuestButton _selectedButton_k__BackingField;
    public QuestInformation questInformation;
    public QuestPanel questPanel;
}
public class QuestButton : UnityEngine.Behaviour
{
    public UnityEngine.RectTransform buttonRectTransform;
    public Quest quest;
    public QuestButtonGenerator buttonGenerator;
    public TMPro.TextMeshProUGUI tmp, chapterText;
    public bool completed, pinned;
}
public class QuestInformation : UnityEngine.Behaviour
{
    public string getNameForState(QuestState state) => "Localized " + state;
    public string getObjectiveStateText(Objective objective, QuestState state) =>
        state is QuestState.NotStarted or QuestState.NoLongerRelevant ? "" : objective.Text;
    public Quest quest, previousQuest;
    public UnityEngine.GameObject allContent, rewardsHolder;
    public UnityEngine.Transform objectivesHolder;
    public TMPro.TextMeshProUGUI questName, status, details;
    public GameList<QuestViewObjectiveUI> objectives = new();
    public GameList<RewardViewObjectUI> rewards = new();
}
public class QuestViewObjectiveUI : UnityEngine.Behaviour { public TMPro.TextMeshProUGUI text; public UnityEngine.GameObject check; }
public class RewardViewObjectUI : UnityEngine.Behaviour { public TMPro.TextMeshProUGUI text; }
public class PrioritizedNavigable : UnityEngine.Behaviour { public UnityEngine.Behaviour ActiveSelectable; }
public class MapPanel : UnityEngine.Behaviour { public QuestPanel questPanel; }
public enum NavigableID { WORLD_MAP_PANEL }
public class NavigationManager : UnityEngine.Behaviour
{
    public static UnityEngine.Object currentlySelected;
    public static NavigationManager instance;
    public UnityEngine.Object RegisteredMap;
    public bool GetNavigableFromID(NavigableID id, out UnityEngine.Object navigation)
    { navigation = RegisteredMap; return navigation != null; }
}
namespace LE.UI.PanelSystem { public class WorldMapPanel : UnityEngine.Behaviour { public MapPanel _mapPanel; } }
namespace LE.UI { public static class LoadingScreen { public static bool IsVisible; } }
namespace LeAccess.Accessibility.Utils
{
    internal static class TextUtils { internal static string CleanText(string text) => text?.Trim(); }
}
namespace LeAccess.Accessibility
{
    internal static class ScreenReader
    {
        internal static bool Available = true, SpokeThisFrame;
        internal static readonly List<string> Spoken = new();
        internal static bool IsAvailable() => Available;
        internal static void Speak(string text, bool interrupt) { Spoken.Add(text); SpokeThisFrame = true; }
    }
}
namespace LeAccess.Accessibility.ScreenContent { internal static class ScreenContentAnnouncer { internal static bool HasActiveContent; } }
namespace LeAccess.Accessibility.Dialogue { internal static class DialogueAnnouncer { internal static bool HasActiveContent; } }
namespace LeAccess.Accessibility.ControllerMenus { internal static class ControllerMenuAccessibility { internal static bool HasActiveContent; } }
namespace LeAccess
{
    internal static class Plugin { internal static TestLog Log = new(); }
    internal class TestLog
    {
        internal readonly List<string> Warnings = new();
        internal readonly List<string> Information = new();
        internal void LogWarning(string text) => Warnings.Add(text);
        internal void LogInfo(string text) => Information.Add(text);
    }
}
