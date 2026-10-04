using LeAccess.Accessibility.Quests;
using LeAccess.Accessibility;
using LeAccess.Accessibility.Navigation;
using LeAccess.I18n;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

static class Program
{
    private static int _checks;
    private static void Check(bool success, string label)
    { _checks++; if (!success) throw new Exception(label); }
    private static QuestProgressSnapshot Q(string revision = "a", string key = "1", QuestProgressStatus status = QuestProgressStatus.Active) =>
        new(key, "Forest", status, revision, "Go to camp");
    private static void Settle(QuestUpdateState state, params QuestProgressSnapshot[] quests)
    { state.Observe(quests); state.Observe(quests); }

    static void Main()
    {
        StateTests(); ProbeTests(); ServiceTests(); SourceFairnessTests(); PendingPriorityTests(); AcceptanceTests(); RejectedFairnessTests();
        Console.WriteLine($"Quest updates: {_checks} checks passed.");
    }

    private static void StateTests()
    {
        var state = new QuestUpdateState();
        Settle(state, Q(), Q("complete", "2", QuestProgressStatus.Completed));
        Check(!state.HasPendingContent, "saved quests establish a silent baseline");
        for (int i = 0; i < 15; i++) state.Observe(new[] { Q(), Q("complete", "2", QuestProgressStatus.Completed) });
        Check(!state.TryTake(true, out _), "unchanged histories stay silent");
        state.Observe(new[] { Q("b") });
        Check(!state.HasPendingContent, "one observation settles first");
        state.Observe(new[] { Q("b") });
        Check(state.HasPendingContent, "changed current objective queues");
        Check(!state.TryTake(false, out _), "speech unavailable retains update");
        Check(state.TryTake(true, out var change) && change.Kind == QuestUpdateKind.Updated, "deferred objective delivered");
        Check(!state.TryTake(true, out _), "delivered only once");
        Settle(state, Q("b"), Q("new", "3"));
        Settle(state, Q("b"), Q("new2", "3"));
        Check(state.TryTake(true, out change) && change.Kind == QuestUpdateKind.Started && change.Quest.Revision == "new2", "new quest coalesces latest objective");
        Settle(state, Q("c"));
        state.Observe(new[] { Q("d") });
        Check(!state.TryTake(true, out _), "never deliver stale pending during newer candidate");
        state.Observe(new[] { Q("d") });
        Check(state.TryTake(true, out change) && change.Quest.Revision == "d", "counter burst coalesces");
        Settle(state, Q("complete", status: QuestProgressStatus.Completed));
        Check(state.TryTake(true, out change) && change.Kind == QuestUpdateKind.Completed, "completion state announces");
        Settle(state, Q("failed", status: QuestProgressStatus.Failed));
        Check(state.TryTake(true, out change) && change.Kind == QuestUpdateKind.Failed, "failure state announces");
        Settle(state, Q("hidden", status: QuestProgressStatus.Hidden));
        Check(!state.HasPendingContent, "hidden relevance change is silent");
        Settle(state, Q("active"));
        Check(state.TryTake(true, out change) && change.Kind == QuestUpdateKind.Started, "activated known quest starts");
        Settle(state, Q("changed"));
        state.Observe(Array.Empty<QuestProgressSnapshot>());
        Check(!state.HasPendingContent, "removal cancels stale pending without claiming completion");
        Settle(state, Q("changed"));
        Check(!state.HasPendingContent, "restored same record does not replay");
        Settle(state, Q("changed"), Q("old", "4", QuestProgressStatus.Completed));
        Check(!state.HasPendingContent, "newly synchronized historical completion silent");
        Settle(state, Q("fresh"));
        state.Reset();
        Settle(state, Q("fresh"));
        Check(!state.HasPendingContent, "character reset drops pending and rebaselines");
        state.Reset();
        state.Observe(new[] { Q() });
        state.Observe(new[] { Q("sync") });
        state.Observe(new[] { Q("sync") });
        Check(!state.HasPendingContent, "initial synchronization settling is silent");
        Settle(state, Q("sync") with { Title = "Bosque", Objectives = "Ve al campamento" });
        Check(!state.HasPendingContent, "translation does not change native progress");
        state.Reset(); Settle(state);
        Settle(state, Q("a", "8"), Q("a", "9"));
        Check(state.TryTake(true, out _) && state.TryTake(true, out _) && !state.TryTake(true, out _), "independent quests retained exactly once");
        state.Reset();
        state.Observe(new[] { Q() }, 100); state.Observe(new[] { Q() }, 100);
        state.Observe(new[] { Q("area-progress"), Q("synced", "2") }, 101);
        state.Observe(new[] { Q("area-progress"), Q("synced", "2") }, 101);
        Check(state.TryTake(true, out change) && change.Quest.Key == "1" && !state.TryTake(true, out _), "list replacement reports known progress and silently seeds restored history");
        state.Observe(new[] { Q("area-progress"), Q("synced", "2"), Q("new", "3") }, 101);
        state.Observe(new[] { Q("area-progress"), Q("synced", "2"), Q("new", "3") }, 101);
        Check(state.TryTake(true, out change) && change.Kind == QuestUpdateKind.Started && change.Quest.Key == "3", "new quest after source sync still announced");
        Settle(state, Q("damaged") with { Available = false }, Q("changed", "2"));
        Check(state.TryTake(true, out change) && change.Quest.Key == "2", "one damaged quest cannot silence another");
        state.Reset(); Settle(state, Q() with { Available = false }); Settle(state, Q());
        Check(!state.HasPendingContent, "initially unavailable quest restoration not mistaken for new quest");
        Settle(state, Q("b")); Settle(state, Q("b") with { Available = false });
        Check(!state.TryTake(true, out _), "invalid pending objective never delivered");
        Settle(state, Q("b")); Check(state.TryTake(true, out _), "valid pending survives unavailable sample");
        state.Reset(); Settle(state, Q("baseline", "1"), Q("baseline", "2"));
        Settle(state, Q("first", "1"), Q("first", "2"));
        Check(state.TryTake(true, out change) && change.Quest.Key == "1", "the oldest first quest starts its pending batch");
        Settle(state, Q("second", "1"), Q("first", "2"));
        Check(state.TryTake(true, out change) && change.Quest.Key == "2",
            "a rapidly requeued quest cannot reuse a dictionary slot ahead of an older pending quest");
        Check(state.TryTake(true, out change) && change.Quest.Key == "1" && change.Quest.Revision == "second",
            "the requeued quest retains its latest revision for the following turn");
        state.Reset(); Settle(state, Q()); Settle(state, Q("pending"));
        Check(state.TryTake(true, out var attempted, acknowledge:false) && state.HasReadyContent,
            "Preparing a progression preserves its current queue position until acceptance");
        state.Observe(new[] { Q("replacement") });
        Check(!state.Acknowledge(attempted) && !state.TryTake(true, out _, acknowledge:false),
            "An attempted revision cannot be acknowledged while its replacement is unsettled");
        state.Observe(new[] { Q("replacement") });
        Check(!state.Acknowledge(attempted) && state.TryTake(true, out attempted, acknowledge:false),
            "A stale accepted result cannot consume the newer settled progression");
        var current = attempted;
        state.Observe(new[] { Q("replacement") with { Available=false } });
        Check(!state.Acknowledge(current), "Unavailable current native state cannot acknowledge an earlier attempted result");
        state.Observe(new[] { Q("replacement") });
        Check(state.Acknowledge(current) && !state.HasPendingContent,
            "Only the matching restored current progression is acknowledged after acceptance");
    }

    private static StatefulQuest AddQuest(StatefulQuestListHolder holder, int id = 1)
    {
        var quest = new Quest { id = id, Owner = holder.statefulQuestList };
        var step = new QuestStep { quest = quest, id = 1 };
        var tracked = new StatefulQuest { quest = quest, currentStep = step };
        tracked.objectives.Add(new StatefulObjective { statefulQuest = tracked,
            objective = new Objective { quest = quest, questStep = step, id = 7 } });
        holder.statefulQuestList.trackedQuests.Add(tracked);
        return tracked;
    }

    private static void ProbeTests()
    {
        var holder = new StatefulQuestListHolder();
        var tracked = AddQuest(holder);
        var objective = tracked.objectives[0];
        Check(QuestUpdateProbe.TryRead(holder, out var read) && read.Count == 1 && read[0].Objectives == "Reach the camp", "native current objective");
        string initial = read[0].Revision;
        objective.progress = 2;
        Check(QuestUpdateProbe.TryRead(holder, out read) && read[0].Revision == initial, "non incremental progress has no semantic change");
        objective.objective.isIncremental = true;
        Check(QuestUpdateProbe.TryRead(holder, out read) && read[0].Objectives.EndsWith("2/10") && read[0].Revision != initial, "native incremental counter retained");
        objective.state = QuestState.Completed;
        Check(QuestUpdateProbe.TryRead(holder, out read) && read[0].Objectives == "", "completed history excluded");
        objective.state = QuestState.Failed;
        Check(QuestUpdateProbe.TryRead(holder, out read) && read[0].Objectives == "", "failed history excluded");
        objective.state = QuestState.Active;
        tracked.Status = QuestState.Completed;
        Check(QuestUpdateProbe.TryRead(holder, out read) && read[0].Status == QuestProgressStatus.Completed && read[0].Objectives == "", "completed quest cannot report retained task");
        string completedRevision = read[0].Revision;
        tracked.currentStep.id++;
        Check(QuestUpdateProbe.TryRead(holder, out read) && read[0].Revision == completedRevision,
            "terminal bookkeeping cannot repeat completed announcement");
        tracked.Status = QuestState.Active;
        holder.initialised = false; Check(!QuestUpdateProbe.TryRead(holder, out _), "uninitialized ignored"); holder.initialised = true;
        holder.hasBeenNullified = true; Check(!QuestUpdateProbe.TryRead(holder, out _), "nullified ignored"); holder.hasBeenNullified = false;
        holder.WasCollected = true; Check(!QuestUpdateProbe.TryRead(holder, out _), "collected holder ignored"); holder.WasCollected = false;
        holder.statefulQuestList.WasCollected = true; Check(!QuestUpdateProbe.TryRead(holder, out _), "collected list ignored"); holder.statefulQuestList.WasCollected = false;
        tracked.quest.playerVisible = false; Check(QuestUpdateProbe.TryRead(holder, out read) && read.Count == 0, "hidden quest not exposed"); tracked.quest.playerVisible = true;
        tracked.quest.Owner = new(); Check(Unavailable(holder), "foreign local list rejected"); tracked.quest.Owner = holder.statefulQuestList;
        tracked.currentStep.quest = new(); Check(Unavailable(holder), "foreign current step rejected"); tracked.currentStep.quest = tracked.quest;
        objective.statefulQuest = new(); Check(Unavailable(holder), "foreign stateful owner rejected"); objective.statefulQuest = tracked;
        objective.objective.quest = new(); Check(Unavailable(holder), "foreign objective rejected"); objective.objective.quest = tracked.quest;
        objective.objective.questStep = new() { quest = new() }; Check(Unavailable(holder), "foreign objective step rejected"); objective.objective.questStep = tracked.currentStep;
        tracked.objectives.WasCollected = true; Check(Unavailable(holder), "incomplete observation not treated as removal"); tracked.objectives.WasCollected = false;
        AddQuest(holder); Check(!QuestUpdateProbe.TryRead(holder, out _), "duplicate native identity rejected");
        holder.statefulQuestList.trackedQuests.RemoveAt(1);
        tracked.quest.displayName = " "; Check(Unavailable(holder), "empty title does not leak identity"); tracked.quest.displayName = "Forest";
        Check(QuestUpdateProbe.TryRead(holder, out _), "valid after temporary stale fields");
        var second = AddQuest(holder, 2);
        var savedStep = tracked.currentStep; tracked.currentStep = null;
        Check(QuestUpdateProbe.TryRead(holder, out read) && !read[0].Available && read[1].Available,
            "missing historical step cannot silence unrelated quest");
        tracked.currentStep = savedStep;
        second.Status = QuestState.NoLongerRelevant;
        Check(QuestUpdateProbe.TryRead(holder, out read) && read[1].Status == QuestProgressStatus.Hidden,
            "no longer relevant quests not narrated as active");
    }

    private static bool Unavailable(StatefulQuestListHolder holder) =>
        QuestUpdateProbe.TryRead(holder, out var read) && read.Count == 1 && !read[0].Available;

    private static void SourceFairnessTests()
    {
        ModText.SetLanguage("en"); ScreenReader.Spoken.Clear(); Time.unscaledTimeAsDouble = 0;
        QuestUpdateAccessibility.Initialize(new ConfigFile());
        PlayerFinder.Provider = new();
        var campaign = AddQuest(PlayerFinder.Provider.Campaign, 1);
        var monolith = AddQuest(PlayerFinder.Provider.Monolith, 2);
        var stash = AddQuest(PlayerFinder.Provider.Stash, 3);
        campaign.quest.displayName = "Campaign";
        monolith.quest.displayName = "Monolith";
        stash.quest.displayName = "Stash";
        foreach (var tracked in new[] { campaign, monolith, stash }) tracked.objectives[0].objective.isIncremental = true;
        Poll(); Poll();
        foreach (var tracked in new[] { campaign, monolith, stash }) tracked.objectives[0].progress = 1;
        Poll(menu: true); Poll(menu: true); Poll(elapsed: 2);
        Check(ScreenReader.Spoken.Count == 1 && ScreenReader.Spoken.Last().Contains("Campaign"),
            "the first ready quest list retains the established initial order");
        campaign.objectives[0].progress = 2;
        Poll(pending: true, elapsed: .11); Poll(pending: true, elapsed: .11); Poll(elapsed: 2);
        Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken.Last().Contains("Monolith"),
            "continuous campaign counters cannot starve an already queued monolith update");
        campaign.objectives[0].progress = 3;
        Poll(pending: true, elapsed: .11); Poll(pending: true, elapsed: .11); Poll(elapsed: 2);
        Check(ScreenReader.Spoken.Count == 3 && ScreenReader.Spoken.Last().Contains("Stash"),
            "a queued stash quest receives its turn before a recurring campaign update");
        Poll(elapsed: 2);
        Check(ScreenReader.Spoken.Count == 4 && ScreenReader.Spoken.Last().Contains("Campaign") && ScreenReader.Spoken.Last().Contains("3/10"),
            "campaign updates still coalesce to their newest counter while the other native lists speak");
        QuestUpdateAccessibility.Cleanup();
    }

    private static void Poll(bool menu = false, bool pending = false, double elapsed = 1)
    { Time.unscaledTimeAsDouble += elapsed; ScreenReader.SpokeThisFrame = false; QuestUpdateAccessibility.Update(menu, pending); }

    private static void PendingPriorityTests()
    {
        ScreenReader.Spoken.Clear(); ScreenReader.Available = true; ScreenReader.SpokeThisFrame = false;
        Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = false; GameplayInputState.Owns = true;
        NavigationAudioState.IsSuspended = false; EpochInputManager.instance.isInputFieldActive = false;
        ScreenKeyboardManager.Instance._enabled = false; SceneList.Gameplay = true;
        PlayerFinder.player = new(); PlayerFinder.Character = new(); PlayerFinder.Provider = new();
        QuestUpdateAccessibility.Initialize(new ConfigFile());
        var tracked = AddQuest(PlayerFinder.Provider.Campaign); tracked.objectives[0].objective.isIncremental = true;
        Poll(); Poll(); tracked.objectives[0].progress = 1; Poll(pending: true); Poll(pending: true);
        Check(QuestUpdateAccessibility.HasPendingContent && QuestUpdateAccessibility.HasPendingAnnouncement,
            "A ready deferred update retains priority behind a higher-priority gameplay announcement");
        QuestUpdateAccessibility.Update(otherContentActive: true);
        Check(QuestUpdateAccessibility.HasPendingContent && !QuestUpdateAccessibility.HasPendingAnnouncement,
            "A nonpausing inventory releases notification priority immediately between polls while retaining the quest queue");
        foreach (string gate in new[] { "typing", "keyboard", "unfocused", "loading", "no-gameplay", "inactive-player", "collected-player" })
        {
            if(gate=="typing") EpochInputManager.instance.isInputFieldActive = true;
            if(gate=="keyboard") ScreenKeyboardManager.Instance._enabled = true;
            if(gate=="unfocused") Application.isFocused = false;
            if(gate=="loading") LE.UI.LoadingScreen.IsVisible = true;
            if(gate=="no-gameplay") GameplayInputState.Owns = false;
            if(gate=="inactive-player") PlayerFinder.player.activeInHierarchy = false;
            if(gate=="collected-player") PlayerFinder.player.WasCollected = true;
            QuestUpdateAccessibility.Update();
            Check(QuestUpdateAccessibility.HasPendingContent && !QuestUpdateAccessibility.HasPendingAnnouncement,
                $"{gate} cannot block native menu notices while quest speech remains queued");
            EpochInputManager.instance.isInputFieldActive=false; ScreenKeyboardManager.Instance._enabled=false;
            Application.isFocused=true; LE.UI.LoadingScreen.IsVisible=false; GameplayInputState.Owns=true;
            PlayerFinder.player.activeInHierarchy=true; PlayerFinder.player.WasCollected=false;
        }
        QuestUpdateAccessibility.Update(higherPriorityPending:true);
        Check(QuestUpdateAccessibility.HasPendingAnnouncement,"Foreground gameplay restores pending priority without resampling");
        var provider=PlayerFinder.Provider;
        PlayerFinder.Provider=null; QuestUpdateAccessibility.Update();
        Check(QuestUpdateAccessibility.HasPendingContent&&!QuestUpdateAccessibility.HasPendingAnnouncement,
            "An absent provider immediately releases priority between samples without deleting known quest progression");
        PlayerFinder.Provider=provider; provider.WasCollected=true; QuestUpdateAccessibility.Update();
        Check(QuestUpdateAccessibility.HasPendingContent&&!QuestUpdateAccessibility.HasPendingAnnouncement,
            "A collected provider cannot reserve priority for known queued progression");
        provider.WasCollected=false; provider.Campaign.initialised=false; Poll(pending:true);
        Check(QuestUpdateAccessibility.HasPendingContent&&!QuestUpdateAccessibility.HasPendingAnnouncement,
            "Unready pending source reads yield world/notification priority without erasing their queue");
        provider.Campaign.initialised=true; Poll(pending:true);
        Check(QuestUpdateAccessibility.HasPendingAnnouncement,"A recovered ready source restores queued announcement eligibility");
        Poll(elapsed:2);
        Check(ScreenReader.Spoken.Count==1&&ScreenReader.Spoken.Single().Contains("1/10"),"Retained progress still delivers once after menus/typing end");
        QuestUpdateAccessibility.Cleanup();
    }

    private static void AcceptanceTests()
    {
        ModText.SetLanguage("en"); ScreenReader.Spoken.Clear(); ScreenReader.OutputAccepted = false;
        Time.unscaledTimeAsDouble = 0; QuestUpdateAccessibility.Initialize(new ConfigFile());
        PlayerFinder.Provider = new();
        var tracked = AddQuest(PlayerFinder.Provider.Campaign);
        tracked.objectives[0].objective.isIncremental = true;
        Poll(elapsed:.11); Poll(elapsed:.11);
        tracked.objectives[0].progress = 1; Poll(elapsed:.11); Poll(elapsed:.11);
        Check(ScreenReader.Spoken.Count == 0 && QuestUpdateAccessibility.HasPendingAnnouncement && QuestUpdateAccessibility.HasPendingContent,
            "Rejected automatic quest progression remains current and ready");
        Check(QuestUpdateAccessibility.CanAnnounceAt(Time.unscaledTimeAsDouble),
            "Rejected quest output does not consume the shared speech clock");
        tracked.objectives[0].progress = 2; ScreenReader.OutputAccepted = true;
        Poll(elapsed:.11);
        Check(ScreenReader.Spoken.Count == 0, "Old rejected progression cannot speak while a newer counter settles");
        Poll(elapsed:.11);
        Check(ScreenReader.Spoken.Single().Contains("2/10") && !QuestUpdateAccessibility.HasPendingContent,
            "Recovery delivers only fresh settled quest progression");
        Check(!QuestUpdateAccessibility.CanAnnounceAt(Time.unscaledTimeAsDouble),
            "Accepted quest progression starts the existing shared speech deadline");
        Poll(elapsed:2.1); Check(ScreenReader.Spoken.Count == 1, "Accepted recovered progression does not repeat");
        ScreenReader.OutputAccepted = false; tracked.objectives[0].progress = 3;
        Poll(elapsed:.11); Poll(elapsed:.11);
        ScreenReader.OutputAccepted = true; Poll(elapsed:.11);
        Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken.Last().Contains("3/10"),
            "Unchanged rejected current progression retries on recovery without an invented cooldown");
        Poll(elapsed:2.1); ScreenReader.OutputAccepted = false; tracked.objectives[0].progress = 4;
        Poll(elapsed:.11); Poll(elapsed:.11);
        PlayerFinder.Provider.Campaign.statefulQuestList.trackedQuests.Clear();
        ScreenReader.OutputAccepted = true; Poll(elapsed:.11); Poll(elapsed:.11);
        Check(ScreenReader.Spoken.Count == 2 && !QuestUpdateAccessibility.HasPendingContent,
            "Removed rejected progression expires instead of replaying after recovery");
        QuestUpdateAccessibility.Cleanup(); ScreenReader.OutputAccepted = true;
    }

    private static void RejectedFairnessTests()
    {
        QuestUpdateAccessibility.Initialize(new ConfigFile()); PlayerFinder.Provider = new();
        ScreenReader.Spoken.Clear(); ScreenReader.OutputAccepted = false; Time.unscaledTimeAsDouble = 0;
        var campaign = AddQuest(PlayerFinder.Provider.Campaign, 1);
        var monolith = AddQuest(PlayerFinder.Provider.Monolith, 2);
        campaign.quest.displayName = "Campaign"; monolith.quest.displayName = "Monolith";
        campaign.objectives[0].objective.isIncremental = monolith.objectives[0].objective.isIncremental = true;
        Poll(elapsed:.11); Poll(elapsed:.11);
        campaign.objectives[0].progress = monolith.objectives[0].progress = 1;
        Poll(menu:true, elapsed:.11); Poll(menu:true, elapsed:.11); Poll(elapsed:.11);
        Check(ScreenReader.Spoken.Count == 0 && QuestUpdateAccessibility.HasPendingContent,
            "Rejected first source retains both waiting quest lists");
        ScreenReader.OutputAccepted = true; Poll(elapsed:.11);
        Check(ScreenReader.Spoken.Single().Contains("Campaign"),
            "A rejected source attempt cannot advance campaign/monolith fairness order");
        Poll(elapsed:2.1);
        Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken.Last().Contains("Monolith"),
            "Accepted recovery advances the fairness turn to the next ready quest list");
        QuestUpdateAccessibility.Cleanup();
    }

    private static void ServiceTests()
    {
        ModText.SetLanguage("en");
        var config = new ConfigFile();
        QuestUpdateAccessibility.Initialize(config);
        PlayerFinder.Provider = new();
        var tracked = AddQuest(PlayerFinder.Provider.Campaign);
        Poll(); Poll();
        Check(ScreenReader.Spoken.Count == 0, "login silent");
        tracked.objectives[0].objective.isIncremental = true;
        tracked.objectives[0].progress = 1;
        Poll(menu: true); Poll(menu: true);
        Check(QuestUpdateAccessibility.HasPendingContent && ScreenReader.Spoken.Count == 0, "dialogue quest change deferred");
        tracked.objectives[0].progress = 3;
        Poll(menu: true); Poll(menu: true); Poll();
        Check(ScreenReader.Spoken.Count == 1 && ScreenReader.Spoken[0].Contains("3/10"), "latest change once after dialogue");
        Check(ScreenReader.Spoken[0].StartsWith(ModText.Format("quest.update_changed", tracked.quest.displayName)), "English heading");
        Poll(); Check(ScreenReader.Spoken.Count == 1, "no polling spam");
        ScreenReader.Speaking = true;
        for (int counter = 4; counter < 7; counter++)
        {
            tracked.objectives[0].progress = counter; Poll(elapsed: .11); Poll(elapsed: .11);
            Check(ScreenReader.Spoken.Count == 1, "speech interval retains incremental changes in mod queue");
        }
        Poll(elapsed: 2);
        Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken[1].Contains("6/10"), "interval delivers latest counter despite persistent backend speaking flag");
        ScreenReader.Speaking = false;
        ScreenReader.Spoken.RemoveAt(1);
        tracked.objectives[0].progress = 4;
        Poll(pending: true); Poll(pending: true);
        Check(QuestUpdateAccessibility.HasPendingContent, "zone and subtitles get priority");
        ScreenReader.Available = false; Poll(); Check(ScreenReader.Spoken.Count == 1, "unavailable speech is not consumed");
        ScreenReader.Available = true;
        Application.isFocused = false; Poll(); Check(ScreenReader.Spoken.Count == 1, "unfocused retains update"); Application.isFocused = true;
        GameplayInputState.Owns = false; Poll(); Check(ScreenReader.Spoken.Count == 1, "menu navigation retains update"); GameplayInputState.Owns = true;
        EpochInputManager.instance.isInputFieldActive = true; Poll(); Check(ScreenReader.Spoken.Count == 1, "chat typing delays quest speech"); EpochInputManager.instance.isInputFieldActive = false;
        ScreenKeyboardManager.Instance._enabled = true; Poll(); Check(ScreenReader.Spoken.Count == 1, "screen keyboard delays quest speech"); ScreenKeyboardManager.Instance._enabled = false;
        LE.UI.LoadingScreen.IsVisible = true; Poll(); Check(ScreenReader.Spoken.Count == 1, "loading retains same session"); LE.UI.LoadingScreen.IsVisible = false;
        var changedScene = new Scene { name = "NextMap", handle = 2, isLoaded = true };
        SceneManager.Active = changedScene; PlayerFinder.player.scene = changedScene;
        Poll(); Check(ScreenReader.Spoken.Count == 2 && ScreenReader.Spoken[1].Contains("4/10"), "area transition retains confirmed progress");
        ModText.SetLanguage("es"); tracked.Status = QuestState.Completed;
        Poll(); Poll();
        Check(ScreenReader.Spoken.Count == 3 && ScreenReader.Spoken[2] == ModText.Format("quest.update_completed", tracked.quest.displayName), "Spanish completed state has no outdated objective");
        tracked.Status = QuestState.Active; tracked.objectives[0].progress = 5; Poll(true); Poll(true);
        SceneList.Gameplay = false; Poll(); SceneList.Gameplay = true; Poll(); Poll();
        Check(ScreenReader.Spoken.Count == 3 && !QuestUpdateAccessibility.HasPendingContent, "return to menu cancels and rebaselines");
        tracked.objectives[0].progress = 6; Poll(true); Poll(true);
        PlayerFinder.Character = new(); Poll(); Poll();
        Check(ScreenReader.Spoken.Count == 3 && !QuestUpdateAccessibility.HasPendingContent, "new character does not inherit queue");
        config.Enabled.Value = false; tracked.objectives[0].progress = 7; Poll(); Poll();
        Check(ScreenReader.Spoken.Count == 3, "config disables automatic updates");
        config.Enabled.Value = true; Poll(); Poll();
        Check(ScreenReader.Spoken.Count == 3, "reenabling establishes quiet baseline");
        config.Interval.Value = 5;
        tracked.objectives[0].progress = 8; Poll(); Poll();
        Check(ScreenReader.Spoken.Count == 4, "first update after reenable immediate after settling");
        tracked.objectives[0].progress = 9; Poll(); Poll();
        Check(ScreenReader.Spoken.Count == 4, "custom speech interval respected");
        Poll(elapsed: 3);
        Check(ScreenReader.Spoken.Count == 5 && ScreenReader.Spoken[4].Contains("9/10"), "custom interval releases latest update");
        config.Interval.Value = double.NaN;
        tracked.objectives[0].progress = 10; Poll(elapsed: 5); Poll();
        Check(ScreenReader.Spoken.Count == 6, "invalid interval does not prevent future speech");
        tracked.objectives[0].progress = 11; Poll(); Poll();
        Check(ScreenReader.Spoken.Count == 7, "invalid interval falls back to existing two second pacing");
        QuestUpdateAccessibility.Cleanup(); tracked.objectives[0].progress = 12; Poll();
        Check(!QuestUpdateAccessibility.HasPendingContent && ScreenReader.Spoken.Count == 7, "cleanup disables service");
    }
}
