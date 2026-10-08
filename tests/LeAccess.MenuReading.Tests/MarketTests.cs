using LeAccess.Accessibility.Market;
using LeAccess.I18n;

internal static class MarketTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        foreach (string language in new[] { "en", "es" })
        {
            ModText.SetLanguage(language);
            var slot = new ProphecySlotInfo("North", false, null, "Kill bosses", "Rare item", "Drops a rare item", "Lens: Celerity",
                2, 40, 150, true);
            Check(MarketText.ProphecySlot(slot) == Speech.Join("North", Speech.F("market.prophecy_target", "Kill bosses"),
                Speech.F("market.prophecy_reward", "Rare item"), "Drops a rare item", "Lens: Celerity", Speech.F("market.prophecy_charges", 2),
                Speech.F("market.prophecy_progress", 40), Speech.F("market.prophecy_favor", 150), Speech.T("market.prophecy_ready")),
                "A slot reads region, goal, reward, lens, charges, progress, favor and readiness");
            var bare = new ProphecySlotInfo("South", false, null, null, null, null, null, 0, 0, 0, false);
            Check(MarketText.ProphecySlot(bare) == Speech.Join("South", Speech.T("market.prophecy_reward_none"),
                Speech.T("market.prophecy_lens_none"), Speech.F("market.prophecy_charges", 0)), "An empty slot says so and omits zero values");
            Check(MarketText.ProphecySlot(slot with { Locked = true, LockText = "Rank 5" }) == Speech.Join("North", Speech.T("market.locked"), "Rank 5"),
                "A locked slot gives the game's reason and nothing else");
            Check(MarketText.Lens("Celerity", null) == Speech.F("market.prophecy_lens", "Celerity"), "A lens without effect text is its name");
            Check(MarketText.Lens("Celerity", "Celerity: faster charges") == Speech.F("market.prophecy_lens", "Celerity: faster charges"),
                "An effect that names the lens is not preceded by the name again");
            Check(MarketText.Lens("Celerity", "Faster charges") == Speech.F("market.prophecy_lens", Speech.Join("Celerity", "Faster charges")),
                "Otherwise name and effect are both read");
            Check(MarketText.RegionStep(true, "East") == Speech.F("market.prophecy_next", "East") &&
                MarketText.RegionStep(false, "West") == Speech.F("market.prophecy_previous", "West") && MarketText.RegionStep(true, " ") == null,
                "Region buttons name the neighbouring region");
            Check(MarketText.RewardOption("Gold", "Drops gold", 90, null, false, true, 1, 4) == Speech.Join("Gold", Speech.T("market.current"),
                "Drops gold", Speech.F("market.prophecy_favor", 90), Speech.F("list.position", 1, 4)), "A picker reward with price and place");
            Check(MarketText.RewardOption("Gold", "Drops gold", 90, "Needs rank 3", true, false, 2, 4) == Speech.Join("Gold", Speech.T("market.locked"),
                "Needs rank 3", Speech.F("list.position", 2, 4)), "A locked picker reward gives the reason, not the price");
            Check(MarketText.LensOption(null, false, 1, 3) == Speech.Join(Speech.T("market.prophecy_lens_none"), Speech.F("list.position", 1, 3)),
                "The empty lens option has a name");

            var members = new[] { new LadderMember("Ana", "Rogue", "92", false), new LadderMember("Luis", "Mage", "Lvl 80", true) };
            Check(MarketText.LadderRow("3", "12,500", members, true, 0, 0) == Speech.Join(Speech.F("market.ladder_rank", "3"), Speech.T("market.ladder_own"),
                Speech.Join("Ana", "Rogue", Speech.F("market.ladder_level", "92")) + ". " +
                Speech.Join("Luis", "Mage", "Lvl 80", Speech.T("market.ladder_dead")), Speech.F("market.ladder_score", "12,500")),
                "A party row lists each character; numeric levels get a word, formatted ones stay as the game wrote them");
            Check(string.IsNullOrEmpty(MarketText.LadderRow(null, null, new LadderMember[0], false, 0, 0)), "A row without data reads nothing");

            var card = new ListingSummary("Ring of Doom", "Ring", "Level 50", "1,200", "30", 2, 8);
            Check(MarketText.Listing(card, null) == Speech.Join("Ring of Doom", "Ring", "Level 50",
                Speech.Join(Speech.F("market.price_gold", "1,200"), Speech.F("market.price_favor", "30")), Speech.F("list.position", 2, 8)),
                "A listing reads name, type, level, price and place");
            Check(MarketText.Listing(card with { Gold = null, Favor = null, Count = 1 }, "+10 life") == Speech.Join("Ring of Doom", "Ring", "Level 50", "+10 life"),
                "Details follow the price; a lone listing has no place");
            Check(MarketText.Potential("market.forging_potential", "12") == Speech.F("market.forging_potential", "12") &&
                MarketText.Potential("market.weavers_will", "") == null, "Potentials need a value");
            Check(MarketText.Page(2, 5, true) == Speech.Join(Speech.F("market.page_of", 2, 5), Speech.T("market.current")), "The current page");
            Check(MarketText.Page(3, 0, false) == Speech.F("market.page", 3), "Page without a known total");
            Check(MarketText.PageStep(true) == Speech.T("market.page_next") && MarketText.PageStep(false) == Speech.T("market.page_previous"),
                "Previous and next page buttons");
            Check(MarketText.Results(0, 0, 0) == Speech.T("market.no_results") &&
                MarketText.Results(37, 2, 4) == Speech.F("market.results", 37, 2, 4), "Search outcome");
            Check(MarketText.LadderEmpty() == Speech.T("market.ladder_empty"), "Empty ladder notice");
        }
        ModText.SetLanguage("en");
        return checks;
    }
}
