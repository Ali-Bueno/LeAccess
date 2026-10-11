using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Inventory;
using LeAccess.I18n;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Vitals;

/// <summary>Speaks each gold gain ("50 de oro") from the game's gold counter, outside vendors.</summary>
/// <remarks>See reference/vitals-api.md#gold-pickups-2026-10-08.</remarks>
internal static class GoldPickupAnnouncer
{
    private static readonly GoldPickupState State = new();
    private static ConfigEntry<bool> _enabled;
    private static string _lastFailure;

    internal static void Initialize(ConfigFile config)
    {
        _enabled = config.Bind("Vitals", "AnnounceGoldPickups", true,
            "Say the amount of each gold pickup or reward, such as \"50 gold\". Shops keep announcing their new total instead.");
        State.Reset();
    }

    internal static void Update()
    {
        if (_enabled == null) return;
        try
        {
            if (!_enabled.Value || !Application.isFocused || LE.UI.LoadingScreen.IsVisible) { State.Reset(); return; }
            int? gold = GoldReport.TryRead(out int amount, out long tracker) ? amount : null;
            var ui = UIBase.instance;
            bool vendor = IsAlive(ui) && InventoryAccessibility.VendorOpen(ui);
            int gain = State.Observe(tracker, gold, vendor);
            if (gain <= 0) return;
            string text = ModText.Format("gold.pickup", gain);
            if (!ScreenReader.TrySpeakPickup(text)) return;
            State.Acknowledge();
            Plugin.Log.LogInfo($"[Gold] {text}");
        }
        catch (Exception ex)
        {
            State.Reset();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Gold pickup feedback unavailable: {ex}");
        }
    }

    internal static void Cleanup()
    {
        State.Reset();
        _enabled = null;
        _lastFailure = null;
    }
}
