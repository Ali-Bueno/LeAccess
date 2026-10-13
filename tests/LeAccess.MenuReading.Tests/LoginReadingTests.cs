using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Login;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class LoginReadingTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException("Login: " + message); }
        string Read(GameObject go)
        {
            ElementAnnouncer.ResetFocus(); ScreenReader.Messages.Clear(); Time.unscaledTime += 1;
            ElementAnnouncer.AnnounceElement(go);
            return ScreenReader.Messages.Last().Text;
        }
        GameObject ButtonNamed(string text, out Button button)
        {
            var go = new GameObject(); button = go.Add(new Button());
            go.Child("Label").Add(new TextMeshProUGUI { text = text });
            return go;
        }
        LeAccess.I18n.ModText.SetLanguage("en");
        EpochInputManager.IsControllerActive = true;

        var locked = ButtonNamed("Submit", out var lockedButton); LoginPrompts.LockedButton = lockedButton;
        Check(Read(locked) == Speech.F("control.disabled", "Submit"), "A locked Submit reads as disabled");
        lockedButton.Interactable = false;
        Check(Read(locked) == Speech.F("control.disabled", "Submit"), "Interactable state alone does not change the locked wording");
        var open = ButtonNamed("Submit", out _);
        Check(Read(open) == "Submit", "An interactable Submit reads plainly");
        LoginPrompts.LockedButton = null;
        Check(Read(locked) == "Submit", "A button that is not locked reads plainly");

        var fieldGo = new GameObject();
        var field = fieldGo.Add(new TMP_InputField { text = "" });
        field.placeholder = fieldGo.Child("Placeholder").Add(new TextMeshProUGUI { text = "Display name" });
        string plain = Speech.F("control.empty_field", "Display name");
        string hint = Speech.T("social.field_hint");
        Check(Read(fieldGo) == plain, "An ordinary field never gets the controller hint");
        LoginPrompts.PromptInput = field;
        Check(Read(fieldGo) == Speech.Join(plain, hint), "A login field in controller mode ends with the Cross hint");
        field.text = "Maga";
        Check(Read(fieldGo) == Speech.Join(Speech.F("control.field_value", "Display name", "Maga"), hint),
            "A filled login field keeps its value before the hint");
        EpochInputManager.IsControllerActive = false;
        Check(Read(fieldGo) == Speech.F("control.field_value", "Display name", "Maga"), "Keyboard mode drops the hint");
        EpochInputManager.IsControllerActive = true; LoginPrompts.PromptInput = null;
        return checks;
    }
}
