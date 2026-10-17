using System;
using BepInEx.Configuration;
using LeAccess.Accessibility.Audio;
using LeAccess.I18n;
using UnityEngine;

namespace LeAccess.Accessibility.ModMenu;

/// <summary>The mod's own settings menu: speech only, opened anywhere with a chord (reference/mod-menu.md).</summary>
internal static class ModMenu
{
    private static ConfigFile _config;
    private static ConfigEntry<bool> _controller;
    private static ConfigEntry<KeyCode> _openKey;
    private static ConfigEntryBase _leftStickOnOtherPads;
    private static MenuNavigator _navigator;
    private static KeyItem _capturing;
    private static string _language, _failure;
    private static int _heldBackBefore;

    internal static bool IsOpen => _navigator is not null;

    internal static void Initialize(ConfigFile config)
    {
        _config = config;
        _controller = config.Bind("Mod menu", "ControllerShortcut", true,
            "Open the mod settings menu with L1 while Create, L3 or the mod button is held; the same chord closes it.");
        _openKey = config.Bind("Mod menu", "OpenKey", KeyCode.F1,
            "Keyboard key that opens and closes the mod settings menu together with Ctrl.");
    }

    /// <summary>Runs first each frame, so other speech this frame already sees whether the menu is open.</summary>
    internal static void Update()
    {
        if (_config is null) return;
        try
        {
            _leftStickOnOtherPads ??= ModMenuPages.Find(_config, "Exploration", "LeftStickOnOtherPads");
            bool leftStick = _leftStickOnOtherPads?.BoxedValue is not bool value || value;
            bool chord = ModMenuInput.ChordPressed(_controller.Value, _openKey.Value, leftStick);
            if (!IsOpen)
            {
                InputBlock.EndWhenIdle();
                if (chord) Open();
                return;
            }
            InputBlock.Hold();
            if (chord) { Close(); return; }
            if (_language != ModText.Language) Rebuild();
            if (_capturing is not null) { Capture(); return; }
            var command = ModMenuInput.ReadCommand(Time.unscaledTimeAsDouble);
            if (command == MenuCommand.None) return;
            var reply = _navigator.Handle(command);
            if (reply.Close) { Close(); return; }
            if (reply.CaptureKey) _capturing = _navigator.Current as KeyItem;
            Say(reply.Speech);
        }
        catch (Exception ex)
        {
            string failure = $"{ex.GetType().Name}: {ex.Message}";
            if (_failure != failure) { _failure = failure; Plugin.Log.LogWarning($"Mod menu unavailable: {ex}"); }
            if (IsOpen) Close();
        }
    }

    internal static void Cleanup()
    {
        if (IsOpen) Close();
        InputBlock.End();
        ShoulderGuard.Release();
        _config = null;
    }

    private static void Open()
    {
        _navigator = new MenuNavigator(Build());
        _language = ModText.Language;
        _heldBackBefore = ScreenReader.HeldBack;
        ModMenuInput.Reset();
        InputBlock.Begin();
        ScreenReader.MenuOwnsSpeech = true;
        Say(_navigator.Opening());
        Plugin.Log.LogInfo("[Mod menu] opened");
    }

    private static void Close()
    {
        _navigator = null;
        _capturing = null;
        InputBlock.Close();
        SoundPreview.Stop();
        ScreenReader.MenuOwnsSpeech = false;
        ScreenReader.Speak(ModText.Get("menu.closed"));
        Plugin.Log.LogInfo($"[Mod menu] closed; other announcements held back: {ScreenReader.HeldBack - _heldBackBefore}");
    }

    private static MenuPage Build() =>
        ModMenuPages.Build(_config, () => new MenuReply(null, Close: true), section => ModMenuSoundPages.Extras(_config, section));

    // A language change rebuilds the labels and keeps the cursor where it was.
    private static void Rebuild()
    {
        var path = _navigator.Path;
        _navigator = new MenuNavigator(Build());
        _navigator.Follow(path);
        _language = ModText.Language;
    }

    private static void Capture()
    {
        if (!ModMenuInput.TryReadKey(out var key)) return;
        var item = _capturing;
        _capturing = null;
        if (key != KeyCode.None) item.Set(key);
        Say(key == KeyCode.None ? ModText.Format("menu.capture_cancelled", item.Value) : item.Value);
    }

    private static void Say(string text)
    {
        if (!string.IsNullOrEmpty(text)) ScreenReader.SpeakMenu(text);
    }
}
