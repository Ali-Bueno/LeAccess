using LeAccess.Accessibility;
using LeAccess.Accessibility.CharacterCreation;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

internal static class CreationTabTests
{
    internal static int Run(Action reset)
    {
        int checks = 0;
        void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
        TextMeshProUGUI Source(GameObject parent, string key, string fallback)
        {
            var go = parent.Child();
            go.Add(new LocalizeStringEvent { StringReference = new() { TableEntryReference = new() { Key = key } } });
            return go.Add(new TextMeshProUGUI { text = fallback });
        }

        reset();
        var root = new GameObject();
        var character = root.Add(new CharacterSelect()); CharacterSelect.instance = character;
        var controller = root.Child().Add(new TabUIController());
        var baseContent = root.Child(); var masteryContent = root.Child();
        var baseTab = controller.gameObject.Child().Add(new TabUIElement { controller = controller, elementID = 0 });
        var masteryTab = controller.gameObject.Child().Add(new TabUIElement { controller = controller, elementID = 1 });
        controller.tabContents = new(baseContent, masteryContent);
        Source(baseTab.gameObject, "Class_Tab_Base_Label", "Base class");
        Source(baseTab.gameObject, "Class_Mage", "Mage");
        Source(masteryTab.gameObject, "Class_Tab_Mastery_Label", "Mastery class");
        var masteryName = Source(masteryTab.gameObject, "Mastery_Sorcerer", "Sorcerer");
        var baseDescription = Source(baseContent, "Class_Description_Mage", "Mage English fallback");
        var masteryDescription = Source(masteryContent, "Mastery_Description_Mage_Sorcerer", "Sorcerer English fallback");
        Source(masteryContent, "Skills_Title", "Unrelated skills heading");
        Localization.Texts["Class_Mage"] = "Mago";
        Localization.Texts["Mastery_Sorcerer"] = "Hechicero";
        Localization.Texts["Class_Description_Mage"] = "Descripción completa del mago";
        Localization.Texts["Mastery_Description_Mage_Sorcerer"] = "Descripción completa de hechicero";

        var creation = new GameObject().Add(new CharacterCreationSelection
        { characterClass = new() { classID = CharacterClassID.Mage, className = "Mage" } });
        character.currentState = CharacterSelect.CharacterSelectState.ClassSelection;
        Localization.Texts["class:Mage"] = "Mago";
        baseContent.Active = false; masteryContent.Active = false;
        Check(ClassSelectionReader.TryReadControl(creation.gameObject, out var selection) &&
            selection.Description == "Descripción completa del mago", "Carousel reads installed hidden class tab definition without MasteryBanner");

        character.currentState = CharacterSelect.CharacterSelectState.NameCreation;
        var navigation = root.Add(new PrioritizedNavigable { ActiveSelectable = masteryTab });
        NavigationManager.currentlySelected = navigation;
        Check(ClassSelectionReader.TryRead(out selection) && selection.Name == "Hechicero" &&
            selection.Description == "Descripción completa de hechicero", "Focused mastery owns its content while another content panel is selected");
        Check(selection.Root == masteryTab.gameObject, "Typed tab is the exact speech owner");
        ClassCarouselHandler.CheckPendingClassAnnouncement();
        ClassCarouselHandler.CheckPendingClassAnnouncement();
        Check(ScreenReader.Messages.Single().Text == "Hechicero. Descripción completa de hechicero", "Creation mastery announces full localized description without section labels");
        ClassCarouselHandler.CheckPendingClassAnnouncement();
        Check(ScreenReader.Messages.Count == 1, "Stable creation mastery stays silent");
        ScreenReader.SpokeThisFrame = false;
        navigation.ActiveSelectable = baseTab;
        ClassCarouselHandler.CheckPendingClassAnnouncement(); ClassCarouselHandler.CheckPendingClassAnnouncement();
        Check(ScreenReader.Messages.Last().Text == "Mago. Descripción completa del mago", "Returning to base tab reads its own description");
        Localization.Texts["Class_Description_Mage"] = "New locale content";
        Check(ClassSelectionReader.TryRead(out selection) && selection.Description == "New locale content", "Cached references resolve current language on every reading");
        Localization.Texts.Remove("Class_Description_Mage");
        Check(ClassSelectionReader.TryRead(out selection) && selection.Description == "Mage English fallback", "Missing translation uses the owned serialized fallback");
        Check(!CreationClassTabReader.TryRead(root.Child(), out _), "Ordinary creation fields never read neighboring tabs");
        var unrelatedTab = root.Child().Add(new TabUIElement { controller = controller, elementID = 0 });
        Check(!CreationClassTabReader.TryRead(unrelatedTab.gameObject, out _), "An unrecognized tab cannot reuse another tab's description");
        navigation.ActiveSelectable = masteryTab;
        masteryTab.elementID = 7;
        Check(!ClassSelectionReader.TryRead(out _), "Invalid changed tab index expires ownership");
        masteryTab.elementID = -1;
        Check(!ClassSelectionReader.TryRead(out _), "Negative index rejected");
        masteryTab.elementID = 1;
        controller.tabContents[1] = new GameObject();
        Check(!ClassSelectionReader.TryRead(out _), "Replaced or external content cannot borrow cached description");
        controller.tabContents[1] = masteryContent;
        controller.WasCollected = true;
        Check(!ClassSelectionReader.TryRead(out _), "Collected controller rejected"); controller.WasCollected = false;
        masteryTab.gameObject.Active = false;
        Check(!ClassSelectionReader.TryRead(out _), "Invisible focus never reads hidden definition"); masteryTab.gameObject.Active = true;
        masteryDescription.WasCollected = true;
        Check(ClassSelectionReader.TryRead(out selection) && selection.Description == null, "Collected text source safely omitted");
        masteryDescription.WasCollected = false;
        masteryName.WasCollected = true;
        ScreenReader.Messages.Clear(); ClassCarouselHandler.Reset(); ScreenReader.SpokeThisFrame = false;
        ClassCarouselHandler.CheckPendingClassAnnouncement(); ClassCarouselHandler.CheckPendingClassAnnouncement();
        Check(ScreenReader.Messages.Count == 0, "Missing typed name cannot become generic mastery-label speech");
        masteryName.WasCollected = false; CreationClassTabReader.Reset();
        Check(ClassSelectionReader.TryRead(out selection) && selection.Name == "Hechicero", "Rebuilt cache recovers a restored source");
        Source(masteryContent, "Mastery_Description_Mage_Spellblade", "Wrong adjacent description");
        CreationClassTabReader.Reset();
        Check(!ClassSelectionReader.TryRead(out _), "Multiple different descriptions under one content owner are rejected");
        character.currentState = CharacterSelect.CharacterSelectState.CharacterSelection;
        Check(!CreationClassTabReader.TryRead(baseTab.gameObject, out _), "Creation tab cannot speak outside the name/mastery screen");
        reset();
        return checks;
    }
}
