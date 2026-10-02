internal class NativeObject
{
    internal bool WasCollected;
    internal T TryCast<T>() where T : class => this as T;
}
namespace UnityEngine
{
    internal class Object : NativeObject { }
    internal enum KeyCode { None, R, LeftControl, RightControl, LeftAlt, RightAlt, LeftShift, RightShift }
    internal static class Input
    {
        internal static readonly HashSet<KeyCode> Held = new(), Down = new();
        internal static bool Throw;
        internal static int Reads;
        internal static bool GetKey(KeyCode key) { Reads++; if (Throw) throw new Exception("Keyboard failed"); return Held.Contains(key); }
        internal static bool GetKeyDown(KeyCode key) { Reads++; if (Throw) throw new Exception("Keyboard failed"); return Down.Contains(key); }
    }
}
namespace Il2CppSystem.Collections.Generic
{
    internal class ICollection<T> : NativeObject { internal virtual int Count => 0; }
    internal sealed class IList<T> : ICollection<T>
    {
        internal readonly System.Collections.Generic.List<T> Items = new();
        internal override int Count => Items.Count;
        internal T this[int index] => Items[index];
    }
}
namespace Rewired
{
    internal sealed class IControllerTemplateButton : NativeObject { internal bool justPressed; internal bool Held; }
    internal sealed class IGamepadTemplate : NativeObject { internal IControllerTemplateButton back; }
    internal sealed class Joystick : NativeObject
    {
        internal IGamepadTemplate Template;
        internal bool Throw;
        internal int Reads;
        internal T GetTemplate<T>() where T : class
        { Reads++; if (Throw) throw new Exception("Controller failed"); return Template as T; }
    }
    internal sealed class Player : NativeObject
    {
        internal ControllerHelper controllers;
        internal sealed class ControllerHelper : NativeObject
        { internal Il2CppSystem.Collections.Generic.IList<Joystick> Joysticks; }
    }
}
internal sealed class EpochInputManager : UnityEngine.Object
{ internal static EpochInputManager instance; internal Rewired.Player rewiredPlayer; }
namespace LeAccess
{
    internal static class Plugin { internal static Logger Log = new(); }
    internal sealed class Logger
    { internal readonly List<string> Warnings = new(); internal void LogWarning(string text) => Warnings.Add(text); }
}
