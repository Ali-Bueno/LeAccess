using System;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.CharacterCreation;

/// <summary>Speaks typed or erased characters of the owned name field, whatever input changed it.</summary>
internal static class CharacterNameEcho
{
    private static readonly FieldEcho Values = new();
    private static int _fieldId;
    private static int _echoSpeechCount = -1;
    private static string _lastFailure;

    internal static void Update()
    {
        try
        {
            var input = CharacterNameAccessibility.OwnedInput;
            if (!IsAlive(input) || !UiVisibility.IsVisible(input.gameObject)) { Reset(); return; }
            int id = input.GetInstanceID();
            if (id != _fieldId) { Values.Clear(); _fieldId = id; }
            string edit = Values.Observe(id, input.text);
            if (edit == null) return;
            // Only the previous echo is cut off; any other speech since then finishes first.
            bool interrupt = ScreenReader.SpeechCount == _echoSpeechCount;
            if (ScreenReader.TrySpeak(edit, interrupt)) _echoSpeechCount = ScreenReader.SpeechCount;
        }
        catch (Exception ex)
        {
            Reset();
            if (_lastFailure == ex.Message) return;
            _lastFailure = ex.Message;
            Plugin.Log.LogWarning($"Character name echo unavailable: {ex}");
        }
    }

    internal static void Reset()
    {
        Values.Clear();
        _fieldId = 0;
        _echoSpeechCount = -1;
    }
}
