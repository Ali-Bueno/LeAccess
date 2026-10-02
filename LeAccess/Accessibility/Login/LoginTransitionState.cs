using System;
using System.Runtime.CompilerServices;
using ClientAppState;
using Il2CppInterop.Runtime;

namespace LeAccess.Accessibility.Login;

internal static class LoginTransitionState
{
    private static IntPtr _activeTransitionField;

    internal static bool IsTransitioning()
    {
        if (_activeTransitionField == IntPtr.Zero)
        {
            RuntimeHelpers.RunClassConstructor(typeof(ClientStateManager).TypeHandle);
            var nativeClass = IL2CPP.GetIl2CppClass("LE.dll", typeof(ClientStateManager).Namespace, nameof(ClientStateManager));
            if (nativeClass == IntPtr.Zero) throw new TypeLoadException("ClientAppState.ClientStateManager");
            _activeTransitionField = IL2CPP.GetIl2CppField(nativeClass,
                nameof(ClientStateManager._activeTransition));
            if (_activeTransitionField == IntPtr.Zero) throw new MissingFieldException("ClientStateManager._activeTransition");
        }
        // Nullable boxing returns null when empty; the generated Nullable wrapper rejects that pointer.
        return IL2CPP.il2cpp_field_get_value_object(_activeTransitionField, IntPtr.Zero) != IntPtr.Zero;
    }
}
