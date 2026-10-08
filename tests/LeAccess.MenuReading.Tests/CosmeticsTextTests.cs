using LeAccess.Accessibility.Cosmetics;
using LeAccess.I18n;

internal static class CosmeticsTextTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        ModText.SetLanguage("es");
        Check(CosmeticsText.Slot("Casco", "Casco Eterno", false) == "Casco. Casco Eterno", "Equipped slot says the slot then the cosmetic");
        Check(CosmeticsText.Slot("Mascota 1", null, false) == "Mascota 1. vacío", "Empty slot says empty");
        Check(CosmeticsText.Slot("Mascota 1", "  ", false) == "Mascota 1. vacío", "Blank cosmetic counts as empty");
        Check(CosmeticsText.Slot("Casco", "Casco Eterno", true) == "Casco. Casco Eterno. bloqueado", "Locked slot says so");
        Check(CosmeticsText.Slot("Bola de fuego", "Bola de fuego", false) == "Bola de fuego", "Repeated words are spoken once");
        Check(CosmeticsText.ClassFilter("Acólito", true) == "Acólito, filtro activado", "Active class filter");
        Check(CosmeticsText.ClassFilter("Acólito", false) == "Acólito, filtro desactivado", "Inactive class filter");
        Check(CosmeticsText.Tab("Todos los estéticos", true) == "Todos los estéticos. pestaña seleccionada", "Selected category tab");
        Check(CosmeticsText.Tab("Mascotas", false) == "Mascotas. pestaña", "Unselected category tab");
        ModText.SetLanguage("en");
        Check(CosmeticsText.Slot("Helmet", null, false) == "Helmet. empty", "English empty slot");
        Check(CosmeticsText.ClassFilter("Mage", true) == "Mage, filter on", "English class filter");
        return checks;
    }
}
