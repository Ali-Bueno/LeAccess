using LeAccess.Accessibility;
using LeAccess.Accessibility.CharacterCreation;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class CreationRealmTests
{
    internal static int Run(Action reset)
    {
        int checks = 0;
        void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
        GameObject Path(GameObject root, string path)
        {
            foreach (string name in path.Split('/'))
            {
                var existing = root.Children.FirstOrDefault(child => child.name == name);
                root = existing ?? root.Child(); root.name = name;
            }
            return root;
        }
        TMP_Text Label(GameObject root, string path, string text) => Path(root, path).Add(new TMP_Text { text = text });
        reset();
        var root = new GameObject(); CharacterSelect.instance = root.Add(new CharacterSelect());
        var modal = Path(root, "CycleModal");
        var contents = Path(modal, "Contents");
        var season = Path(contents, "CycleTile"); var legacy = Path(contents, "CycleTile (2)");
        var title = Label(contents, "EterraFrameHeader/Header", "Elige un reino");
        var seasonName = Label(season, "SeasonName", "Temporada");
        Label(legacy, "Name", "Legado");
        Label(legacy, "BorderTitle/Recommended", "Temporada");
        var recommended = Label(season, "Label/Type", "Recomendado");
        var legacyRecommended = Label(legacy, "Label/Type", "Recomendado");
        legacyRecommended.gameObject.Active = false;
        var seasonBody = Label(season, "Message", "Un nuevo comienzo.");
        Label(legacy, "Message", "Conserva tus personajes y alijos anteriores.");
        var footer = Label(contents, "Footer/CycleEndExplanation", "Los personajes pasan a Legado al terminar la temporada.");
        var seasonButton = Path(season, "SeasonSelectionButton").Add(new Button());
        var legacyButton = Path(legacy, "LegacySelectionButton").Add(new Button());
        var seasonChange = Path(season, "chooseCycleInsteadButton").Add(new Button());
        var legacyChange = Path(legacy, "chooseLegacyInsteadButton").Add(new Button());
        foreach (var button in new[] { seasonButton, legacyButton, seasonChange, legacyChange })
            Label(button.gameObject, "TextMeshPro Text", "Elegir");
        var nav = modal.Add(new PrioritizedNavigable { ActiveSelectable = seasonButton });
        NavigationManager.currentlySelected = nav;
        Check(ClassSelectionReader.TryRead(out var choice) && choice.Name == "Elige un reino. Temporada. Recomendado",
            "Season selection reads the realm and recommendation instead of the generic Select child");
        Check(choice.Description == "Un nuevo comienzo. Los personajes pasan a Legado al terminar la temporada.",
            "Season details include its own explanation and the shared migration footer");
        Check(ClassCarouselHandler.TryHandleFocus(seasonButton.gameObject), "Realm focus is owned by the brief choice reader");
        ClassCarouselHandler.CheckPendingClassAnnouncement(); ScreenReader.SpokeThisFrame = false;
        ClassCarouselHandler.CheckPendingClassAnnouncement();
        Check(ScreenReader.Messages.Single().Text == choice.Name, "Realm navigation omits all descriptive paragraphs");
        foreach (var button in new[] { legacyButton, legacyChange })
        {
            nav.ActiveSelectable = button;
            Check(ClassSelectionReader.TryRead(out choice) && choice.Name == "Elige un reino. Legado",
                "Both Legacy actions use their exact title, never the copied Season decoration");
            Check(choice.Description.StartsWith("Conserva tus personajes"), "Legacy actions own only the Legacy explanation");
        }
        nav.ActiveSelectable = seasonChange;
        Check(ClassCarouselHandler.TryReadDetails(out var details) && details.Contains("Temporada") && details.Contains("nuevo comienzo"),
            "Manual details cover the change-realm action before any automatic announcement");
        int before = ScreenReader.Messages.Count;
        ScreenReader.SpokeThisFrame = false;
        ClassCarouselHandler.CheckPendingClassAnnouncement(); ClassCarouselHandler.CheckPendingClassAnnouncement();
        Check(ScreenReader.Messages.Count == before && !ClassCarouselHandler.HasPendingContent,
            "A pending short name cannot interrupt the manual description on the following frame");
        Check(ClassCarouselHandler.TryReadDetails(out _), "The same realm description can be requested repeatedly");
        seasonBody.text = "Texto actualizado";
        Check(ClassCarouselHandler.TryReadDetails(out details) && details.Contains("Texto actualizado"), "Manual details refresh displayed text");
        ScreenContentAnnouncer.HasActiveContent = true; ScreenContentAnnouncer.Root = new GameObject();
        Check(!ClassCarouselHandler.TryReadDetails(out _), "An unrelated modal blocks manual creation details");
        ScreenContentAnnouncer.HasActiveContent = false;
        seasonName.text = "Season"; title.text = "Choose a Realm"; recommended.text = "Recommended";
        Check(ClassSelectionReader.TryRead(out choice) && choice.Name == "Choose a Realm. Season. Recommended", "Realm text follows the current visible locale");
        seasonName.text = "";
        Check(ClassSelectionReader.TryRead(out choice) && choice.Name is null && !ClassCarouselHandler.TryReadDetails(out _),
            "Missing realm title waits instead of announcing Select, a header alone or neighboring text");
        seasonName.text = "Season";
        seasonBody.alpha = 0; footer.canvasRenderer.cull = true;
        Check(!ClassCarouselHandler.TryReadDetails(out _), "Hidden explanation sources are excluded from manual speech");
        seasonBody.alpha = 1; footer.canvasRenderer.cull = false;
        modal.Active = false;
        Check(!ClassSelectionReader.TryRead(out _), "Hidden creation modal cannot retain a stale realm choice"); modal.Active = true;
        seasonChange.WasCollected = true;
        Check(!ClassSelectionReader.TryRead(out _), "Collected realm button is rejected without native access"); seasonChange.WasCollected = false;
        var foreign = new GameObject();
        var imitation = Path(foreign, "CycleModal/Contents/CycleTile/SeasonSelectionButton").Add(new Button());
        Check(!CreationRealmReader.TryRead(imitation.gameObject, out _), "A matching path outside the declared CharacterSelect is rejected");
        var misplaced = Path(legacy, "SeasonSelectionButton").Add(new Button());
        Check(!CreationRealmReader.TryRead(misplaced.gameObject, out _), "Wrong tile ownership cannot borrow a Season label");
        var close = Path(contents, "Close_Button").Add(new Button());
        Check(!CreationRealmReader.TryRead(close.gameObject, out _), "Close action never speaks a nearby realm");
        reset();
        return checks;
    }
}
