using Rewired;

namespace LeAccess.Accessibility.Exploration;

// L3 as the mod button on pads without a free Create: their gameplay overlay-map bindings are switched
// off while gameplay owns input. Menu maps (item comparison, forge) are never touched.
internal static class LeftStickModifier
{
    private static readonly BindingSuppression<ActionElementMap> Ledger = new();
    private static string _heldFor, _logged;

    // The game's own Rewired hardware ids for its PlayStation pads.
    internal static bool IsPlayStation(Joystick joystick)
    {
        var guid = joystick.hardwareTypeGuid;
        return guid.Equals(EpochInputManager.REWIRED_DUALSENSEGUID) || guid.Equals(EpochInputManager.REWIRED_DUALSHOCK4GUID);
    }

    internal static void Hold(Player player, string signature)
    {
        if (_heldFor == signature) return;
        Release();
        _heldFor = signature;
        var joysticks = player?.controllers?.Joysticks;
        var collection = joysticks?.TryCast<Il2CppSystem.Collections.Generic.ICollection<Joystick>>();
        if (collection is null) return;
        for (int index = 0; index < collection.Count; index++)
        {
            var joystick = joysticks[index];
            if (joystick is null || joystick.WasCollected || IsPlayStation(joystick)) continue;
            var maps = player.controllers.maps.GetMaps(ControllerType.Joystick, joystick.id);
            int mapCount = maps.TryCast<Il2CppSystem.Collections.Generic.ICollection<ControllerMap>>()?.Count ?? 0;
            for (int m = 0; m < mapCount; m++)
            {
                var map = maps[m];
                if (map is null || map.WasCollected) continue;
                var elements = map.AllMaps;
                int elementCount = elements.TryCast<Il2CppSystem.Collections.Generic.ICollection<ActionElementMap>>()?.Count ?? 0;
                for (int e = 0; e < elementCount; e++)
                {
                    var element = elements[e];
                    if (element is null || element.WasCollected || !element.enabled ||
                        element.actionId != (int)EpochInputManager.InputActions.OverlayMap) continue;
                    element.enabled = false;
                    Ledger.Record(element);
                }
            }
        }
        string message = $"[Exploration input] L3 is the mod button; overlay map bindings off in gameplay: {Ledger.Count}";
        if (message != _logged) Plugin.Log.LogInfo(_logged = message);
    }

    internal static void Release()
    {
        _heldFor = null;
        if (Ledger.Count == 0) return;
        Ledger.RestoreAll(element => { if (!element.WasCollected) element.enabled = true; });
    }
}
