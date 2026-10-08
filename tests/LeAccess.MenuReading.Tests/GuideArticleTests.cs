using LeAccess.Accessibility.Guide;
using LeAccess.Accessibility.Items;
using LeAccess.I18n;

internal static class GuideArticleTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        ModText.SetLanguage("es");
        Check(GuideArticleText.Compose("Desbloquear habilidades", new[] { "Primer párrafo.", "Segundo párrafo" }) ==
            "Desbloquear habilidades. Primer párrafo. Segundo párrafo", "Title then paragraphs are joined into one utterance");
        Check(GuideArticleText.Compose("<b>Título</b>", new[] { "Texto con <link=x>enlace</link>  y   espacios", "", "  " }) ==
            "Título. Texto con enlace y espacios", "Markup, spacing and empty paragraphs are removed");
        Check(GuideArticleText.Compose(null, new[] { "Solo texto" }) == "Solo texto", "A missing title still reads the body");
        Check(GuideArticleText.Compose("Mismo", new[] { "Mismo", "Otro" }) == "Mismo. Otro", "A paragraph repeating the title is not read twice");
        Check(GuideArticleText.Category("Habilidades", true) == "Habilidades, desplegado", "Expanded category says so");
        Check(GuideArticleText.Category("objetos", false) == "objetos, plegado", "Collapsed category says so");
        Check(StashTabText.Tab("Pestaña 1", 12, true, false) == "Pestaña 1. 12 objetos. pestaña seleccionada", "Selected stash tab with items");
        Check(StashTabText.Tab("Armas", 1, false, true) == "Armas. 1 objeto. con prioridad", "Single item and priority");
        Check(StashTabText.Tab("Vacía", 0, false, false) == "Vacía. 0 objetos", "Empty tab says so");
        Check(StashTabText.Folder("Equipo", false, 3) == "Equipo. carpeta. plegada. 3 pestañas", "Collapsed folder with tabs");
        Check(StashTabText.Folder("Equipo", true, 1) == "Equipo. carpeta. desplegada. 1 pestaña", "Expanded folder with one tab");
        ModText.SetLanguage("en");
        Check(GuideArticleText.Category("Skills", false) == "Skills, collapsed", "English category state");
        Check(StashTabText.Tab("Tab 2", 2, false, false) == "Tab 2. 2 items", "English stash tab");
        return checks;
    }
}
