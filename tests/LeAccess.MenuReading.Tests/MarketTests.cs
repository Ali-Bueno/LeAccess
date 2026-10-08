using LeAccess.Accessibility.Announcements;
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
            Check(MarketText.LensOption(null, null, false, 1, 3) == Speech.Join(Speech.T("market.prophecy_lens_none"), Speech.F("list.position", 1, 3)),
                "The empty lens option has a name");
            Check(MarketText.LensOption("Celerity", "+50% charge rate", true, 2, 3) == Speech.Join("Celerity", Speech.T("market.current"),
                "+50% charge rate", Speech.F("list.position", 2, 3)), "A lens option reads its name, equipped state, effect and place");

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

            var own = card with { State = MarketText.ListingStatus(ListingState.Sold), Action = MarketText.ListingAction("Redeem"), Position = 1, Count = 1 };
            Check(MarketText.Listing(own, null) == Speech.Join("Ring of Doom", "Ring", "Level 50",
                Speech.Join(Speech.F("market.price_gold", "1,200"), Speech.F("market.price_favor", "30")), Speech.T("market.listing_sold"),
                Speech.F("market.listing_action", "Redeem")), "An own listing adds its state and the action of its button");
            Check(MarketText.ListingStatus(ListingState.Active) == Speech.T("market.listing_active") &&
                MarketText.ListingStatus(ListingState.Redeemed) == Speech.T("market.listing_redeemed") &&
                MarketText.ListingStatus(ListingState.Cancelled) == Speech.T("market.listing_cancelled"), "Listing states");
            Check(MarketText.StallView(false, 3, 1, 1) == Speech.Join(Speech.T("market.stall_listed"), Speech.F("market.results", 3, 1, 1)) &&
                MarketText.StallView(true, 0, 0, 0) == Speech.Join(Speech.T("market.stall_sold"), Speech.T("market.stall_empty")),
                "The stall view says which list it is and how many entries, or that it is empty");
            Check(MarketText.InputField("Level", "5", "lvl") == Speech.F("control.field_value", "Level", "5 lvl") &&
                MarketText.InputField("Level", " ") == Speech.F("control.empty_field", "Level") &&
                MarketText.InputField("Price", "900", gold: true) == Speech.F("control.field_value", "Price", Speech.F("market.price_gold", "900")),
                "Fields read title and value, empty fields say so, prices get the currency");
            Check(MarketText.RangeField("Level", true, "9") == Speech.F("control.field_value", Speech.F("market.range_max", "Level"), "9") &&
                MarketText.RangeField("Level", false, null) == Speech.F("control.empty_field", Speech.F("market.range_min", "Level")), "Min and max fields");
            Check(MarketText.Checkbox("Lower price", true) == Speech.F("control.field_value", "Lower price", Speech.T("state.on")), "Checkbox state");
            Check(MarketText.ListItem("Ring", "+10 life", "500") == Speech.Join(Speech.F("market.list_item", "Ring"), "+10 life",
                Speech.F("market.list_suggested", "500")) && MarketText.ListItem(null, null, "500") == Speech.F("market.list_suggested", "500"),
                "The chosen item, then its suggested price");
            Check(MarketText.ListEmpty(null) == Speech.T("market.list_empty") && MarketText.ListEmpty("Place an item") == "Place an item", "Empty form");
            Check(MarketText.ListButton("List", "12", "Fee 5%", null) == Speech.Join("List", Speech.F("market.list_favor_cost", "12"), "Fee 5%") &&
                MarketText.ListButton(null, null, null, null) == Speech.T("market.list_action"), "The confirm button names its cost and fees");
            Check(MarketText.SourceButton(true, null) == Speech.T("market.list_stash") && MarketText.SourceButton(false, "Bag") == "Bag", "Source buttons");

            Check(DropdownReader.Option("Mage", true, 2, 5) == Speech.Join("Mage", Speech.T("market.dropdown_selected"), Speech.F("list.position", 2, 5)) &&
                DropdownReader.Option("Any", false, 1, 1) == "Any", "Expanded options give state and place");
            Check(DropdownReader.Control("Class", "Mage") == Speech.F("control.field_value", "Class", "Mage") && DropdownReader.Control(null, "Mage") == "Mage",
                "A dropdown reads title and value");
            var list = new UnityEngine.GameObject().Child("Dropdown List").Child("Content");
            
            foreach (string name in new[] { "Any", "Mage", "Rogue" })
            { var item = list.Child("Item"); item.Add(new UnityEngine.UI.Toggle { isOn = name == "Mage" }); item.Child().Add(new TMPro.TextMeshProUGUI { text = name }); }
            list.Child("Item").Active = false;
            list.Children[0].Add(new UnityEngine.UI.Toggle());
            Check(DropdownReader.ReadOption(list.Children[1]) == Speech.Join("Mage", Speech.T("market.dropdown_selected"), Speech.F("list.position", 2, 3)),
                "An expanded option reads text, selected state and place among the active options");
            Check(DropdownReader.ReadOption(list.Children[2]) == Speech.Join("Rogue", Speech.F("list.position", 3, 3)), "An unselected option");
            var row = new UnityEngine.GameObject();
            var field = row.Child("Dropdown"); var dd = field.Add(new TMPro.TMP_Dropdown { value = 1 });
            row.Child("FilterTitle").Add(new TMPro.TextMeshProUGUI { text = "Rarity" });
            Check(DropdownReader.Title(dd) == "Rarity", "The title is the visible label beside the control");
            DropdownReader.NoteSpoken(dd);
            Check(!DropdownReader.ChangeSettled(dd, 10f), "A dropdown whose value was announced has no change to say");
            dd.value = 2;
            Check(!DropdownReader.ChangeSettled(dd, 10f) && !DropdownReader.ChangeSettled(dd, 10f + DropdownReader.ChangeSettleSeconds / 2) &&
                DropdownReader.ChangeSettled(dd, 10f + DropdownReader.ChangeSettleSeconds), "A new value is said once it has settled");
            DropdownReader.NoteSpoken(dd);
            Check(!DropdownReader.ChangeSettled(dd, 20f), "Once said it is not said again");
        }
        ModText.SetLanguage("en");
        return checks;
    }
}
