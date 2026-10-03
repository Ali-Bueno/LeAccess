using System.Collections;

namespace UnityEngine
{
    internal class Object
    {
        private static int _id;
        internal readonly IntPtr Pointer=new(++_id);
        internal bool WasCollected, Destroyed;
        internal int GetInstanceID()=>Pointer.ToInt32();
        internal T TryCast<T>() where T:class => this as T;
        public static bool operator ==(Object a,Object b)=>
            (a is null||a.Destroyed)&&(b is null||b.Destroyed)||ReferenceEquals(a,b);
        public static bool operator !=(Object a,Object b)=>!(a==b);
        public override bool Equals(object value)=>ReferenceEquals(this,value);
        public override int GetHashCode()=>GetInstanceID();
    }
    internal class Component:Object
    {
        internal GameObject gameObject;
        internal Transform transform=>gameObject.transform;
        internal NativeArray<T> GetComponentsInChildren<T>(bool includeInactive) where T:Component=>
            gameObject.GetComponentsInChildren<T>(includeInactive);
    }
    internal class Behaviour:Component { internal bool isActiveAndEnabled=true; }
    internal class Transform:Component
    {
        internal bool IsChildOf(Transform other)
        {for(var go=gameObject;go!=null;go=go.Parent)if(go==other.gameObject)return true;return false;}
    }
    internal class RectTransform:Transform { }
    internal class GameObject:Object
    {
        internal bool Active=true,Visible=true;
        internal GameObject Parent;
        internal readonly Transform transform;
        internal readonly List<Component> Components=new();
        internal readonly List<GameObject> Children=new();
        internal bool activeInHierarchy=>Active&&(Parent==null||Parent.activeInHierarchy);
        internal GameObject()=>transform=new(){gameObject=this};
        internal GameObject Child(){var child=new GameObject{Parent=this};Children.Add(child);return child;}
        internal T Add<T>(T component) where T:Component {component.gameObject=this;Components.Add(component);return component;}
        internal NativeArray<T> GetComponentsInChildren<T>(bool includeInactive) where T:Component
        {
            var result=new List<T>();
            if(includeInactive||activeInHierarchy)
            {
                result.AddRange(Components.OfType<T>());
                foreach(var child in Children)result.AddRange(child.GetComponentsInChildren<T>(includeInactive));
            }
            return new(result.ToArray());
        }
    }
    internal class CanvasRenderer:Object { internal bool cull; }
}
namespace TMPro
{
    internal class TMP_Text:UnityEngine.Behaviour
    {internal string text;internal float alpha=1;internal UnityEngine.CanvasRenderer canvasRenderer=new();}
}
internal class NativeArray<T>:IEnumerable<T>
{
    internal bool WasCollected;
    internal T[] Values;
    internal int Length=>Values.Length;
    internal NativeArray(params T[] values)=>Values=values;
    internal T this[int index] {get=>Values[index];set=>Values[index]=value;}
    public IEnumerator<T> GetEnumerator()=>((IEnumerable<T>)Values).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
}
internal static class NavigationManager { internal static UnityEngine.Object currentlySelected; }
internal class TabUIController:UnityEngine.Behaviour
{ internal int SelectedTabId;internal NativeArray<TabUIElement> tabElements=new(); }
internal class SettingsPanelTabUIController:TabUIController {internal SettingsUIManager _settingsUIManager;}
internal class SettingsUIManager:UnityEngine.Behaviour
{internal UnityEngine.GameObject gameplayTab,GraphicsTab,soundTab,socialTab,challengesTab;}
internal class SettingsPanelTabNavigable:UnityEngine.Behaviour
{
    internal TabUIController _tabUIController;
    internal NativeArray<UnityEngine.RectTransform> _selectables=new();
    internal NativeArray<UnityEngine.Object> _selectableComponents=new();
    internal int _currentlySelectedIdx;
}
internal class TabUIElement:UnityEngine.Behaviour {internal int elementID;internal TabUIController controller;}
internal class CharacterChallengeSetting:UnityEngine.Behaviour
{
    internal SettingsPanelTabNavigable navigable;
    internal TMPro.TMP_Text challengeNameText,challengeEnabledDescription;
    internal UnityEngine.GameObject challengeEnabledObj,challengeDisabledObj;
    internal bool ChallengeEnabled()=>throw new Exception("Reader must not query hidden setting state");
}
namespace LeAccess.Accessibility.Announcements
{
    internal static class UiVisibility
    {
        internal static bool IsVisible(UnityEngine.GameObject go)
        {
            for(var root=go;root is not null;root=root.Parent)
                if(root.WasCollected||root.Destroyed||!root.Active||!root.Visible)return false;
            return go is not null;
        }
    }
}
namespace LeAccess.Accessibility.Utils
{
    internal static class TextUtils
    {
        internal static bool IsVisibleLabel(TMPro.TMP_Text label) => label is not null && !label.WasCollected &&
            !label.Destroyed && label.isActiveAndEnabled && float.IsFinite(label.alpha) && label.alpha > 0 &&
            LeAccess.Accessibility.Announcements.UiVisibility.IsVisible(label.gameObject) &&
            (label.canvasRenderer is null || !label.canvasRenderer.cull);
        internal static string CleanText(string value)=>value is null?null:
            System.Text.RegularExpressions.Regex.Replace(System.Text.RegularExpressions.Regex.Replace(value,"<[^>]+>",""),@"\s+"," ").Trim();
    }
}
