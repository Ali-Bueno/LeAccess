using LeAccess.Accessibility.Endgame;
using LeAccess.I18n;

internal static class EndgameTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        foreach (string language in new[] { "en", "es" })
        {
            ModText.SetLanguage(language);
            var pending = new[] { new VoteEntry("Ana", VoteState.Pending), new VoteEntry("Luis", VoteState.Pending) };
            var oneIn = new[] { new VoteEntry("Ana", VoteState.Accepted), new VoteEntry("Luis", VoteState.Pending) };
            var oneOut = new[] { new VoteEntry("Ana", VoteState.Accepted), new VoteEntry("Luis", VoteState.Declined) };
            string state(VoteState s) => Speech.T(s switch
            { VoteState.Accepted => "endgame.state_accepted", VoteState.Declined => "endgame.state_declined", _ => "endgame.state_pending" });
            Check(EndgameText.Roster(oneIn) == Speech.Join(Speech.F("endgame.vote_roster", "Ana", state(VoteState.Accepted)),
                Speech.F("endgame.vote_roster", "Luis", state(VoteState.Pending))), "Roster lists every player with the vote");
            Check(EndgameText.VoteChanges(pending, pending) == null, "An unchanged vote says nothing");
            Check(EndgameText.VoteChanges(pending, oneIn) == Speech.F("endgame.vote_accepted", "Ana"), "Only the player who accepted is named");
            Check(EndgameText.VoteChanges(oneIn, oneOut) == Speech.F("endgame.vote_declined", "Luis"), "A decline is named alone");
            Check(EndgameText.VoteChanges(oneOut, pending) == Speech.Join(Speech.F("endgame.vote_pending_again", "Ana"),
                Speech.F("endgame.vote_pending_again", "Luis")), "Withdrawn votes are named");
            Check(EndgameText.VoteChanges(Array.Empty<VoteEntry>(), pending) == null, "Players joining without a vote are not announced");
            Check(EndgameText.VoteChanges(pending, new[] { new VoteEntry("Eva", VoteState.Accepted) }) == Speech.F("endgame.vote_accepted", "Eva"),
                "A new player who already accepted is named");
            Check(EndgameText.Option("Cleanse 50", false, true, 2, 3) == Speech.Join("Cleanse 50", Speech.F("list.position", 2, 3)),
                "Option label with its place");
            Check(EndgameText.Option("Cleanse 50", true, true, 2, 3) == Speech.Join("Cleanse 50", Speech.T("endgame.chosen"), Speech.F("list.position", 2, 3)),
                "The picked option says so");
            Check(EndgameText.Option("Cleanse 5", false, false, 1, 1) == Speech.F("control.disabled", "Cleanse 5"), "A disabled single option has no position");
            Check(EndgameText.Button("Confirm", false) == Speech.F("control.disabled", "Confirm") && EndgameText.Button("Confirm", true) == "Confirm",
                "Buttons report availability");
            Check(EndgameText.Island("Echo of Fire", IslandState.Available, false) == "Echo of Fire", "An open echo is just its tooltip");
            Check(EndgameText.Island("Echo of Fire", IslandState.Completed, true) == Speech.Join("Echo of Fire", Speech.T("endgame.island_completed"),
                Speech.T("endgame.island_reward")), "A completed echo with a waiting reward");
            Check(EndgameText.Island("Echo", IslandState.Unreachable, false) == Speech.Join("Echo", Speech.T("endgame.island_unreachable")), "Unreachable echo");
            Check(EndgameText.Blessing(BlessingRole.Option, "Fury", null, false, 2, 3, null) == Speech.Join("Fury", Speech.F("list.position", 2, 3)),
                "A blessing offer with name and place");
            Check(EndgameText.Blessing(BlessingRole.Option, "Fury", null, false, 2, 3, "Fury: +10% damage") == Speech.Join("Fury: +10% damage", Speech.F("list.position", 2, 3)),
                "The game's tooltip replaces the bare name");
            Check(EndgameText.Blessing(BlessingRole.Option, null, "Locked by corruption", true, 4, 5, null) ==
                Speech.Join(Speech.T("endgame.locked"), "Locked by corruption", Speech.F("list.position", 4, 5)), "A locked blessing says why");
            Check(EndgameText.Blessing(BlessingRole.Current, "Fury", null, false, 0, 3, null) == Speech.Join(Speech.T("endgame.blessing_current"), "Fury"),
                "The kept blessing has a role and no place");
            Check(EndgameText.Blessing(BlessingRole.New, null, null, false, 0, 3, null) == Speech.Join(Speech.T("endgame.blessing_new"), Speech.T("endgame.slot_empty")),
                "An empty new-blessing slot");
            Check(EndgameText.Tier(2, true, false, 2, 4) == Speech.Join(Speech.F("endgame.tier", 2), Speech.T("endgame.selected"), Speech.F("list.position", 2, 4)),
                "A selected tier tab");
            Check(EndgameText.Tier(4, false, true, 4, 4) == Speech.Join(Speech.F("endgame.tier", 4), Speech.T("endgame.locked"), Speech.F("list.position", 4, 4)),
                "A locked tier tab");
            var plain = new RewardSlotFacts(false, null, false);
            Check(EndgameText.RewardSlot("Ring, Ring", plain, 2, 5) == Speech.Join("Ring, Ring", Speech.F("list.position", 2, 5)),
                "A plain reward slot is its item and place");
            Check(EndgameText.RewardSlot("Ring", plain with { Target = true }, 1, 3) ==
                Speech.Join("Ring", Speech.T("morditas.reward_target"), Speech.F("list.position", 1, 3)), "The ritual target says so");
            Check(EndgameText.RewardSlot("Ring", new RewardSlotFacts(false, "Blood ritual", true), 3, 3) == Speech.Join("Ring",
                Speech.F("morditas.reward_sacrificed", "Blood ritual"), Speech.T("morditas.reward_upgradable"), Speech.F("list.position", 3, 3)),
                "A sacrificed, upgradable slot names the ritual");
            Check(EndgameText.RewardSlot("Ring", plain with { SacrificedFor = " " }, 1, 2) == Speech.Join("Ring", Speech.F("list.position", 1, 2)),
                "A blank ritual name is not spoken");
        }
        ModText.SetLanguage("en");
        return checks;
    }
}
