using LeAccess.Accessibility.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

internal static class PanelGridTests
{
    internal static void Run(Action<bool, string> check)
    {
        SkillAccessibility.Reset();
        Localization.Abilities = new AbilityLocalization();
        var panel = new GameObject().Add(new SkillsPanelManager());
        TMP_Text Label(string text) => panel.gameObject.Child().Add(new TMP_Text { text = text });
        panel.baseClassName = Label("Pícara");
        panel.mastery1ClassName = Label("Hojadanzante");
        panel.mastery2ClassName = Label("Tiradora");
        panel.mastery3ClassName = Label("Halconera");
        AbilityPanelIcon Icon(string name)
        {
            return panel.gameObject.Child().Add(new AbilityPanelIcon { ability = new Ability { abilityName = name } });
        }
        SpecialisedAbilityIcon Slot(bool locked)
        {
            var slot = panel.gameObject.Child().Add(new SpecialisedAbilityIcon { locked = locked });
            slot.levelLockedText = slot.gameObject.Child().Add(new TMP_Text { text = "Nivel 20" });
            return slot;
        }
        var free = Slot(false);
        var locked = Slot(true);
        var flurry = Icon("Ráfaga");
        var puncture = Icon("Herida punzante");
        var acid = Icon("Frasco de ácido");
        var cascade = Icon("Catarata sombría");
        var hidden = Icon("Oculta");
        hidden.gameObject.Active = false;
        var arrow = Icon("Flecha explosiva");
        var heartseeker = Icon("Buscador del corazón");
        panel.baseClassPanelIcons = new(); panel.baseClassPanelIcons.Add(flurry); panel.baseClassPanelIcons.Add(puncture);
        panel.baseClassTreePanelIcons = new(); panel.baseClassTreePanelIcons.Add(acid);
        panel.masteryClass1PanelIcons = new(); panel.masteryClass1PanelIcons.Add(cascade); panel.masteryClass1PanelIcons.Add(hidden);
        panel.masteryClass2PanelIcons = new(); panel.masteryClass2PanelIcons.Add(heartseeker);
        panel.masteryAbilityPanelIcon2 = arrow;
        // As in the game, base-class and mastery skills share the native rows; y=0 is the slot row.
        var cells = new Component[4, 3];
        cells[1, 0] = free; cells[2, 0] = locked;
        cells[0, 1] = flurry; cells[1, 1] = puncture; cells[2, 1] = hidden; cells[3, 1] = cascade;
        cells[0, 2] = acid; cells[2, 2] = heartseeker; cells[3, 2] = arrow;
        panel.selectables = new Il2CppSystem.Array(cells);
        EventSystem.current = new GameObject().Add(new EventSystem { Selected = new GameObject() });

        void At(int x, int y) => panel.currentlySelectedIdx = new Vector2Int { x = x, y = y };
        bool Is(int x, int y) => panel.currentlySelectedIdx.x == x && panel.currentlySelectedIdx.y == y;
        bool Move(Direction direction) => SkillPanelGrid.TryNavigate(panel, direction);

        At(0, 1);
        check(Move(Direction.RIGHT) && Is(1, 1) && panel.FocusedCells.Count == 1 && panel.HiddenTooltips == 1 &&
            EventSystem.current.Selected is null, "A step focuses the next skill the native way after clearing the old tooltip and selection");
        check(Move(Direction.RIGHT) && Is(0, 2), "The base class reads as one row across its native rows, never into a mastery");
        int focused = panel.FocusedCells.Count;
        check(Move(Direction.RIGHT) && Is(0, 2) && panel.FocusedCells.Count == focused, "The end of a row stays put instead of wrapping");
        check(Move(Direction.DOWN) && Is(3, 1), "Down reaches the next class row, clamped to its length and skipping hidden skills");
        check(Move(Direction.DOWN) && Is(2, 2) && Move(Direction.DOWN) && Is(2, 2), "Down from the last row stays put");
        check(Move(Direction.RIGHT) && Is(3, 2) && Move(Direction.UP) && Is(3, 1) && Move(Direction.UP) && Is(0, 1),
            "Up returns through the rows at the same place");
        check(Move(Direction.UP) && Is(1, 0) && Move(Direction.RIGHT) && Is(2, 0), "The specialization slots form the top row");
        check(!Move(Direction.UP) && Is(2, 0), "Up from the slots leaves to the buttons above through the game's own move");
        check(Move(Direction.LEFT) && Is(1, 0) && Move(Direction.LEFT) && Is(1, 0), "The start of a row stays put");
        At(2, 1);
        check(Move(Direction.DOWN) && Is(0, 1), "A cursor left on a hidden skill restarts on the first skill");
        panel.IsSkillTreeOpen = true;
        check(!Move(Direction.DOWN), "An open specialization tree keeps its own movement");
        panel.IsSkillTreeOpen = false;

        NavigationManager.currentlySelected = panel;
        At(0, 1);
        SkillPanelGrid.Forget();
        check(SkillSelectionReader.TryRead(out var selection, includeDetails: false) && selection.Name == "Ráfaga" &&
            selection.Context == "Pícara" && selection.Position == Speech.F("list.position", 1, 3),
            "The first skill names its class row and its place in the row: " + selection.Status);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Context == "Pícara",
            "The row name stays while the same skill is selected");
        Move(Direction.RIGHT);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Context is null &&
            selection.Position == Speech.F("list.position", 2, 3), "A step inside the row does not repeat its name");
        Move(Direction.DOWN);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Name == "Catarata sombría" &&
            selection.Context == "Hojadanzante" && selection.Position is null,
            "Entering a mastery row names the mastery");
        At(1, 0);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Name == Speech.T("skill.specialization_empty") &&
            selection.Context == Speech.T("skill.specialization_slots"), "An empty slot is named instead of staying silent");
        At(2, 0);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Name == Speech.T("skill.specialization_locked") &&
            selection.Status == "Nivel 20" && selection.Position == Speech.F("list.position", 2, 2), "A locked slot reads its unlock text");
        At(1, 0);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) &&
            selection.Status.StartsWith(Speech.T("skill.slot_help")), "A free slot says confirming chooses a skill for it");
        // After A on a free slot the game marks eligible skills with the slot and specialised ones without.
        puncture.specMode = true; puncture.slotToSpec = 0;
        flurry.specMode = true;
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) &&
            selection.Status.StartsWith(Speech.T("skill.choose_to_specialise")), "The free slot then asks for a skill: " + selection.Status);
        At(1, 1);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Status.Contains(Speech.T("skill.confirm_to_specialise")),
            "An eligible skill says confirming specialises it");
        At(0, 1);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && selection.Status.Contains(Speech.T("skill.already_specialised")) &&
            !selection.Status.Contains(Speech.T("skill.confirm_to_specialise")), "An already specialised skill says so");
        puncture.locked = true; At(1, 1);
        check(SkillSelectionReader.TryRead(out selection, includeDetails: false) && !selection.Status.Contains(Speech.T("skill.confirm_to_specialise")),
            "A locked skill is not offered");
        puncture.specMode = flurry.specMode = puncture.locked = false; puncture.slotToSpec = -1;
        NavigationManager.currentlySelected = null;
        EventSystem.current = null;
        SkillAccessibility.Reset();
    }
}
