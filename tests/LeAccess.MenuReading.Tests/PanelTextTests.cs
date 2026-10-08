using LeAccess.Accessibility.Panels;
using LeAccess.I18n;

internal static class PanelTextTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        foreach (string language in new[] { "es", "en" })
        {
            ModText.SetLanguage(language);
            string Get(string key) => ModText.Get(key);
            string Position(int place, int count) => ModText.Format("list.position", place, count);

            Check(PanelText.Tab("Armas", false, 2, 4) == $"Armas. {Get("panels.tab")}. {Position(2, 4)}", language + ": tab with its place");
            Check(PanelText.Tab("Armas", true, 1, 3) == $"Armas. {Get("panels.tab_selected")}. {Position(1, 3)}", language + ": selected tab says so");
            Check(PanelText.Tab("Solo", false, 1, 1) == $"Solo. {Get("panels.tab")}", language + ": a lone tab has no place");
            Check(PanelText.Refresh("Actualizar. 100", true) == "Actualizar. 100", language + ": available refresh is the game's text");
            Check(PanelText.Refresh("Actualizar. 100", false) == ModText.Format("control.disabled", "Actualizar. 100"), language + ": unavailable refresh");
            Check(PanelText.YouHave("929610 huesos") == ModText.Format("panels.you_have", "929610 huesos"), language + ": currency line");
            Check(PanelText.YouHave(" ") == null, language + ": no currency, no line");
            Check(PanelText.Banner("Gremio. Alineado") == $"Gremio. Alineado. {Get("panels.faction_banner")}", language + ": banner follows the faction summary");
            Check(PanelText.Banner(null) == Get("panels.faction_banner"), language + ": banner alone");

            string unlocked = PanelText.RankReward(3, 12, "Comerciar", "Objetos normales", true, true, "gasta favor");
            Check(unlocked == $"{ModText.Format("panels.rank_of", 3, 12)}. {Get("panels.rank_current")}. Comerciar. Objetos normales. {Get("panels.reward_unlocked")}",
                language + ": current unlocked rank omits the unlock hint");
            string locked = PanelText.RankReward(5, 12, "Comerciar", "Objetos raros", false, false, "gasta favor");
            Check(locked == $"{ModText.Format("panels.rank_of", 5, 12)}. Comerciar. Objetos raros. {Get("panels.reward_locked")}. gasta favor",
                language + ": locked rank says what unlocks it");

            Check(PanelText.Evolution("Cuerno. Carga", true, 2, 3) == $"Cuerno. Carga. {Get("panels.chosen")}. {Position(2, 3)}", language + ": chosen evolution with place");
            Check(PanelText.Evolution("Cuerno", false, 0, 0) == "Cuerno", language + ": unplaced evolution");
            Check(PanelText.Rule(2, 9, "Ocultar cascos", true) == $"{ModText.Format("panels.rule", 2, 9)}. Ocultar cascos", language + ": enabled rule");
            Check(PanelText.Rule(2, 9, "Ocultar cascos", false) == $"{ModText.Format("panels.rule", 2, 9)}. Ocultar cascos. {Get("panels.rule_off")}", language + ": disabled rule says so");
            Check(PanelText.RuleSwitch("Ocultar cascos", true) == $"Ocultar cascos. {Get("panels.rule_on")}", language + ": rule switch on");
            Check(PanelText.RuleSwitch("Ocultar cascos", false) == $"Ocultar cascos. {Get("panels.rule_off")}", language + ": rule switch off");
            Check(PanelText.KeySlot(2, 3) == ModText.Format("panels.key_slot", 2, 3), language + ": key slot place");
            Check(PanelText.Cost(new[] { "100 oro", PanelText.MerchantTokens("1") }) ==
                ModText.Format("panels.cost", $"100 oro. {ModText.Format("panels.merchant_tokens", "1")}"), language + ": bazaar cost lists each currency");
        }

        var changes = new CurrencyChanges();
        KeyValuePair<string, string> Line(string key, string text) => new(key, text);
        Check(changes.Observe(new[] { Line("a", "10 huesos") }) == null, "The opening amounts are the baseline, not news");
        Check(changes.Observe(new[] { Line("a", "10 huesos") }) == null, "Unchanged amounts stay silent");
        Check(changes.Observe(new[] { Line("a", "7 huesos") }) == "7 huesos", "A spent currency is read once");
        Check(changes.Observe(new[] { Line("a", "7 huesos") }) == null, "The same new amount is not read again");
        Check(changes.Observe(new[] { Line("a", "7 huesos"), Line("b", "3 ámbar") }) == "3 ámbar", "A currency appearing later is read");
        Check(changes.Observe(new[] { Line("a", "9 huesos"), Line("b", "1 ámbar") }) == "9 huesos. 1 ámbar", "Several changes are read together");
        changes.Reset();
        Check(changes.Observe(new[] { Line("a", "1 huesos") }) == null, "A new opening starts from a fresh baseline");
        ModText.SetLanguage("en");
        return checks;
    }
}
