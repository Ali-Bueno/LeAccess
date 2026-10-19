using LeAccess.Accessibility;
using LeAccess.Accessibility.Quests;
using LeAccess.Accessibility.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class Program
{
    private static int _checks;
    private static readonly List<DynamicObjectiveContent> Content = new();
    private static BepInEx.Configuration.ConfigFile _config;
    private static void Check(bool success, string text) { _checks++; if (!success) throw new Exception(text); }
    private static TMP_Text Label(GameObject root, string text) => root.Child().Add(new TMP_Text { text = text });
    private static QuestTrackerQuestUI Row(QuestTrackerUI tracker, string title, string text, bool modifier = false)
    {
        var root = tracker.questHolder.Child();
        var row = root.Add(new QuestTrackerQuestUI { TracksMonolithObjective = !modifier, TracksMonolithModifiers = modifier });
        row.questName = Label(root,title); row.objectiveObject = new GameObject();
        var objective = root.Child().Add(new QuestTrackerObjectiveUI());
        objective.text = Label(objective.gameObject,text); row.objectivesDisplayed.Add(objective);
        tracker.questsDisplayed.Add(row); return row;
    }
    private static QuestTrackerUI Tracker()
    {
        var root = new GameObject(); var tracker = root.Add(new QuestTrackerUI { currentScene = SceneManager.Active.name });
        tracker.questHolder = root.Child(); QuestTrackerUI.instance = tracker; return tracker;
    }
    private static RageOfMorditasQuestTrackerUI Morditas()
    {
        var root = new GameObject(); var tracker = root.Add(new RageOfMorditasQuestTrackerUI { _encounterScene = SceneManager.Active });
        tracker.questHeader = root.Child().Add(new QuestTrackerQuestUI());
        tracker.questHeader.questName = Label(tracker.questHeader.gameObject,"Rage of Morditas");
        tracker._currentObjective = root.Child().Add(new QuestTrackerObjectiveUI());
        tracker._currentObjective.text = Label(tracker._currentObjective.gameObject,"Return to the gauntlet");
        tracker._activeObjectives.Add(tracker._currentObjective); RageOfMorditasQuestTrackerUI.Instance = tracker; return tracker;
    }
    private static void Read() => DynamicObjectiveProbe.Read(SceneManager.Active,Content);
    private static void Tick(bool menu = false, bool priority = false)
    { Time.unscaledTimeAsDouble += .11; ScreenReader.SpokeThisFrame = false; DynamicObjectiveAccessibility.Update(menu,priority); }
    private static void Reset()
    {
        SceneManager.Active = new() { handle = 1, name = "Echo", isLoaded = true };
        QuestTrackerUI.instance = null; RageOfMorditasQuestTrackerUI.Instance = null; LeAccess.Accessibility.Morditas.MorditasSpeechOwner.End();
        PlayerFinder.player = new(); PlayerFinder.Character = new();
        EpochInputManager.instance = new(); ScreenKeyboardManager.Instance = new();
        PlayerFinder.Provider=new(); _config=new(); QuestUpdateAccessibility.Initialize(_config);
        Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = false; SceneList.Gameplay = true;
        GameplayInputState.Gameplay = true; NavigationAudioState.IsSuspended = false;
        ScreenReader.Available = true; ScreenReader.SpokeThisFrame = false; ScreenReader.Messages.Clear();
        ScreenReader.OutputAccepted = true;
        Time.unscaledTimeAsDouble = 0; LeAccess.Plugin.Log.Warnings.Clear(); DynamicObjectiveAccessibility.Reset();
    }
    private static void Main()
    {
        ProbeTests(); MorditasTests(); ServiceTests(); PendingPriorityTests(); SharedPacingTests(); SharedFairnessTests(); AcceptanceTests();
        Console.WriteLine($"Dynamic objective tests passed: {_checks} checks.");
    }
    private static void ProbeTests()
    {
        Reset(); var tracker = Tracker(); var goal = Row(tracker,"Echo objectives","Seal the gate");
        var danger = Row(tracker,"Enemy modifiers","Enemies deal 35% increased fire damage",true);
        var campaign = Row(tracker,"Campaign quest","Saved campaign objective"); campaign.quest = new();
        var generic = Row(tracker,"Generic retained widget","Old objective"); generic.TracksMonolithObjective = false;
        Read(); Check(Content.Count == 2 && Content[0].Text == "Echo objectives. Seal the gate" && Content[1].Text.Contains("35%"),
            "Exact dynamic/modifier flags read native text; campaign and generic rows are excluded");
        goal.objectivesDisplayed[0].text.text = "Seal the gate: 2/3"; Read();
        Check(Content[0].Text.EndsWith("2/3"),"Authored visible progress counters remain intact");
        danger.gameObject.Active = false; Read(); Check(Content.Count == 1,"Hidden modifier rows are not danger evidence"); danger.gameObject.Active = true;
        tracker.currentScene = "Previous area"; Read(); Check(Content.Count == 0,"Previous-scene retained tracker rows stay silent"); tracker.currentScene = "Echo";
        tracker.initialised = false; Read(); Check(Content.Count == 0,"Uninitialized tracker rejected"); tracker.initialised = true;
        tracker.questsDisplayed.WasCollected = true; Read(); Check(Content.Count == 0,"Collected native row list rejected"); tracker.questsDisplayed.WasCollected = false;
        goal.gameObject.Parent = new GameObject(); Read(); Check(Content.Count == 1,"Foreign dynamic row cannot use this tracker"); goal.gameObject.Parent = tracker.questHolder;
        goal.objectiveObject.Parent = new GameObject(); Read();
        Check(Content.Count == 2,"An external authored objective prefab does not hide the actual owned rendered rows");
        var objective = goal.objectivesDisplayed[0]; objective.gameObject.Parent = new GameObject(); Read();
        Check(Content.Count == 1,"Foreign objective row rejected"); objective.gameObject.Parent = goal.gameObject;
        objective.text.gameObject.Parent = new GameObject(); Read(); Check(Content.Count == 1,"Foreign objective label rejected"); objective.text.gameObject.Parent = objective.gameObject;
        foreach (string reason in new[] { "disabled", "alpha", "culled", "hidden", "collected", "blank" })
        {
            bool oldEnabled = objective.text.Enabled; float oldAlpha = objective.text.alpha; string oldText = objective.text.text;
            if(reason=="disabled") objective.text.Enabled=false;
            if(reason=="alpha") objective.text.alpha=0;
            if(reason=="culled") objective.text.Culled=true;
            if(reason=="hidden") objective.text.gameObject.Visible=false;
            if(reason=="collected") objective.text.WasCollected=true;
            if(reason=="blank") objective.text.text=" ";
            Read(); Check(Content.Count==1,$"{reason} objective cannot announce only its retained title");
            objective.text.Enabled=oldEnabled; objective.text.alpha=oldAlpha; objective.text.text=oldText;
            objective.text.Culled=false; objective.text.gameObject.Visible=true; objective.text.WasCollected=false;
        }
        tracker.questHolder.Parent=new GameObject(); Read(); Check(Content.Count==0,"Foreign global tracker holder rejected");
    }
    private static void MorditasTests()
    {
        Reset(); var tracker=Morditas(); Read();
        Check(Content.Single().Text=="Rage of Morditas. Return to the gauntlet","Morditas speaks its exact current rendered warning");
        tracker._encounterScene.handle=2; Read(); Check(Content.Count==0,"Previous encounter scene cannot replay warning"); tracker._encounterScene.handle=1;
        tracker._activeObjectives.Clear(); Read(); Check(Content.Count==0,"Unlisted retained current warning rejected"); tracker._activeObjectives.Add(tracker._currentObjective);
        tracker._currentObjective.gameObject.Parent=new GameObject(); Read(); Check(Content.Count==0,"Foreign current warning rejected"); tracker._currentObjective.gameObject.Parent=tracker.gameObject;
        tracker._currentObjective.text.text=""; Read(); Check(Content.Count==0,"Empty warning cannot speak just encounter title"); tracker._currentObjective.text.text="Return to the gauntlet";
        tracker._currentObjective.text.Culled=true; Read(); Check(Content.Count==0,"Culled encounter warning remains silent"); tracker._currentObjective.text.Culled=false;
        tracker.Enabled=false; Read(); Check(Content.Count==0,"Disabled encounter tracker remains silent"); tracker.Enabled=true;
        tracker._activeObjectives.WasCollected=true; Read(); Check(Content.Count==0,"Collected encounter objective list rejected");
        Reset(); Morditas(); LeAccess.Accessibility.Morditas.MorditasSpeechOwner.Begin(Array.Empty<string>()); Read();
        Check(Content.Count==0,"The Morditas reader speaks the encounter instruction while it follows the encounter");
        LeAccess.Accessibility.Morditas.MorditasSpeechOwner.End(); Read(); Check(Content.Count==1,"Without it the tracker is read again");
    }
    private static void ServiceTests()
    {
        Reset(); var tracker=Tracker(); var row=Row(tracker,"Echo objectives","Destroy the spires"); Morditas();
        Tick(); Check(ScreenReader.Messages.Count==0&&DynamicObjectiveAccessibility.HasPendingContent,"One sample settles before output");
        DynamicObjectiveAccessibility.Update(); Check(DynamicObjectiveAccessibility.HasPendingContent,"Pending state survives frames between scheduled samples");
        Tick(); Check(ScreenReader.Messages.Single()==("Echo objectives. Destroy the spires. Rage of Morditas. Return to the gauntlet",false),
            "Concurrent verified dynamic goals coalesce without interrupting");
        for(int i=0;i<25;i++) Tick(); Check(ScreenReader.Messages.Count==1,"Unchanged native dynamic goals do not repeat");
        row.objectivesDisplayed[0].text.text="Destroy the spires: 1/3"; Tick(true); Tick(true);
        row.objectivesDisplayed[0].text.text="Destroy the spires: 2/3"; Tick(true); Tick(true);
        Check(ScreenReader.Messages.Count==1,"Menus retain latest visible progression without speech");
        Tick(); Check(ScreenReader.Messages.Count==2&&ScreenReader.Messages.Last().Text.EndsWith("2/3"),"Menu return reports latest authored objective");
        row.objectivesDisplayed[0].text.text="Find the exit"; Tick(); Tick();
        Check(ScreenReader.Messages.Count==2&&DynamicObjectiveAccessibility.HasPendingContent,"Speech pacing defers new objective");
        for(int i=0;i<20;i++) Tick(); Check(ScreenReader.Messages.Count==3&&ScreenReader.Messages.Last().Text.Contains("Find the exit"),"Paced latest objective speaks once");
        foreach(string gate in new[]{"focus","paused","input","keyboard","backend","gameplay","priority","dialogue"})
        {
            Reset(); tracker=Tracker(); row=Row(tracker,"Echo objectives","Current instruction");
            if(gate=="focus") Application.isFocused=false;
            if(gate=="paused") NavigationAudioState.IsSuspended=true;
            if(gate=="input") EpochInputManager.instance.isInputFieldActive=true;
            if(gate=="keyboard") ScreenKeyboardManager.Instance._enabled=true;
            if(gate=="backend") ScreenReader.Available=false;
            if(gate=="gameplay") GameplayInputState.Gameplay=false;
            Tick(gate=="dialogue",gate=="priority"); Tick(gate=="dialogue",gate=="priority");
            Check(ScreenReader.Messages.Count==0&&DynamicObjectiveAccessibility.HasPendingContent,$"{gate} defers verified visible goals");
            row.gameObject.Active=false;
            Application.isFocused=true; NavigationAudioState.IsSuspended=false; EpochInputManager.instance.isInputFieldActive=false;
            ScreenKeyboardManager.Instance._enabled=false; ScreenReader.Available=true; GameplayInputState.Gameplay=true;
            Tick(); Tick(); Check(ScreenReader.Messages.Count==0&&!DynamicObjectiveAccessibility.HasPendingContent,$"Expired {gate}-blocked goals are not replayed");
        }
        Reset(); tracker=Tracker(); row=Row(tracker,"Old echo","Pending instruction"); Tick(true); Tick(true);
        SceneManager.Active=new(){handle=2,name="Dungeon",isLoaded=true}; PlayerFinder.player.scene=SceneManager.Active;
        Tick(); Tick(); Check(ScreenReader.Messages.Count==0,"Scene transition rejects retained old tracker name and text");
        tracker.currentScene="Dungeon"; row.objectivesDisplayed[0].text.text="Open the dungeon door"; Tick(); Tick();
        Check(ScreenReader.Messages.Single().Text.EndsWith("Open the dungeon door"),"New scene's verified current row speaks");
        Reset(); tracker=Tracker(); row=Row(tracker,"Echo objectives","Wait"); Tick(true); Tick(true);
        _config.Enabled.Value=false; Tick(); Check(!DynamicObjectiveAccessibility.HasPendingContent,"Existing automatic quest setting disables dynamic reading");
        _config.Enabled.Value=true; row.objectivesDisplayed[0].text.text="Proceed"; Tick(); Tick();
        Check(ScreenReader.Messages.Single().Text.EndsWith("Proceed"),"Enabling reading consumes current text without replaying disabled-period text");
        Check(LeAccess.Plugin.Log.Warnings.Count==0,"Validated lifecycle probes do not access collected wrappers");
    }

    private static void PendingPriorityTests()
    {
        Reset(); Row(Tracker(),"Echo objectives","Reach the exit"); Tick(priority:true); Tick(priority:true);
        Check(DynamicObjectiveAccessibility.HasPendingContent&&DynamicObjectiveAccessibility.HasPendingAnnouncement,
            "Verified visible dynamic instructions retain priority behind a higher-priority gameplay announcement");
        DynamicObjectiveAccessibility.Update(otherContentActive:true);
        Check(DynamicObjectiveAccessibility.HasPendingContent&&!DynamicObjectiveAccessibility.HasPendingAnnouncement,
            "Inventory immediately receives native notification priority while dynamic text remains queued");
        foreach(string gate in new[]{"typing","keyboard","unfocused","loading","no-gameplay","inactive-player","collected-player"})
        {
            if(gate=="typing") EpochInputManager.instance.isInputFieldActive=true;
            if(gate=="keyboard") ScreenKeyboardManager.Instance._enabled=true;
            if(gate=="unfocused") Application.isFocused=false;
            if(gate=="loading") LE.UI.LoadingScreen.IsVisible=true;
            if(gate=="no-gameplay") GameplayInputState.Gameplay=false;
            if(gate=="inactive-player") PlayerFinder.player.Active=false;
            if(gate=="collected-player") PlayerFinder.player.WasCollected=true;
            DynamicObjectiveAccessibility.Update();
            Check(!DynamicObjectiveAccessibility.HasPendingAnnouncement,$"{gate} releases dynamic speech priority between polls");
            EpochInputManager.instance.isInputFieldActive=false; ScreenKeyboardManager.Instance._enabled=false;
            Application.isFocused=true; LE.UI.LoadingScreen.IsVisible=false; GameplayInputState.Gameplay=true;
            PlayerFinder.player.Active=true; PlayerFinder.player.WasCollected=false;
        }
        DynamicObjectiveAccessibility.Update(higherPriorityPending:true);
        Check(DynamicObjectiveAccessibility.HasPendingAnnouncement,"Returning to gameplay restores current dynamic priority");
        Tick(); Check(ScreenReader.Messages.Single().Text.Contains("Reach the exit"),"Deferred dynamic text speaks after foreground access resumes");
    }

    private static void SharedPacingTests()
    {
        Reset();
        var holder=PlayerFinder.Provider.Campaign;
        var quest=new Quest{id=1,Owner=holder.statefulQuestList};
        var step=new QuestStep{quest=quest,id=1};
        var tracked=new StatefulQuest{quest=quest,currentStep=step};
        var objective=new StatefulObjective{statefulQuest=tracked,objective=new Objective{quest=quest,questStep=step,id=1,isIncremental=true}};
        tracked.objectives.Add(objective); holder.statefulQuestList.trackedQuests.Add(tracked);
        var row=Row(Tracker(),"Dungeon instructions","Open the next door"); row.gameObject.Active=false;
        void Both(double elapsed=.11)
        {
            Time.unscaledTimeAsDouble+=elapsed; ScreenReader.SpokeThisFrame=false;
            QuestUpdateAccessibility.Update();
            DynamicObjectiveAccessibility.Update(higherPriorityPending:QuestUpdateAccessibility.HasPriorityOverDynamic);
        }
        Both(); Both(); objective.progress=1; Both(); Both();
        Check(ScreenReader.Messages.Count==1&&ScreenReader.Messages.Single().Text.Contains("Campaign quest"),"Stateful quest progress receives its automatic speech turn");
        row.gameObject.Active=true; Both(); Both();
        Check(ScreenReader.Messages.Count==1&&DynamicObjectiveAccessibility.HasPendingContent&&DynamicObjectiveAccessibility.HasPendingAnnouncement,
            "Settled dungeon text cannot speak 100 ms after campaign progress and retains its pending priority");
        Both(1.7); Check(ScreenReader.Messages.Count==1,"Both quest readers respect the same minimum speech deadline");
        Both(); Check(ScreenReader.Messages.Count==2&&ScreenReader.Messages.Last().Text.Contains("Open the next door"),"Dynamic instructions receive the shared turn after the interval");
        objective.progress=2; Both(); Both();
        Check(ScreenReader.Messages.Count==2&&QuestUpdateAccessibility.HasPendingAnnouncement,"Campaign progress also waits behind a prior dynamic instruction");
        Both(2.1); Check(ScreenReader.Messages.Count==3&&ScreenReader.Messages.Last().Text.Contains("2/3"),"Campaign progression receives the next shared speech turn");
        DynamicObjectiveAccessibility.Reset(); row.objectivesDisplayed[0].text.text="Take the boss exit"; Both(); Both();
        Check(ScreenReader.Messages.Count==3,"Resetting a dynamic tracker does not erase the shared campaign speech deadline");
        Both(2.1); Check(ScreenReader.Messages.Count==4&&ScreenReader.Messages.Last().Text.Contains("Take the boss exit"),"Reset dynamic instructions settle and wait for the shared turn");
    }

    private static void AcceptanceTests()
    {
        Reset(); var row = Row(Tracker(), "Current objectives", "Old instruction");
        ScreenReader.OutputAccepted = false; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 0 && DynamicObjectiveAccessibility.HasPendingContent && DynamicObjectiveAccessibility.HasPendingAnnouncement,
            "Rejected dynamic instruction remains pending and eligible");
        Check(QuestUpdateAccessibility.CanAnnounceAt(Time.unscaledTimeAsDouble),
            "Rejected dynamic instruction does not spend the shared speech turn");
        row.objectivesDisplayed[0].text.text = "Fresh instruction"; ScreenReader.OutputAccepted = true;
        Tick(); Check(ScreenReader.Messages.Count == 0, "Rejected dynamic history yields while fresh text settles");
        Tick(); Check(ScreenReader.Messages.Single().Text.EndsWith("Fresh instruction") && !DynamicObjectiveAccessibility.HasPendingContent,
            "Recovery consumes only accepted fresh dynamic instructions");
        Check(!QuestUpdateAccessibility.CanAnnounceAt(Time.unscaledTimeAsDouble), "Accepted dynamic output starts the shared deadline");
        Tick(); Check(ScreenReader.Messages.Count == 1 && !DynamicObjectiveAccessibility.HasPendingAnnouncement,
            "Accepted dynamic objective does not reserve another speech turn");
        Reset(); row = Row(Tracker(), "Current objectives", "Old instruction");
        ScreenReader.OutputAccepted = false; Tick(); Tick();
        row.gameObject.Active = false; ScreenReader.OutputAccepted = true; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 0 && !DynamicObjectiveAccessibility.HasPendingAnnouncement,
            "Hidden rejected dynamic instruction cannot replay after recovery");
        Reset(); row = Row(Tracker(), "Current objectives", "Still current instruction");
        ScreenReader.OutputAccepted = false; Tick(); Tick(); ScreenReader.OutputAccepted = true; Tick();
        Check(ScreenReader.Messages.Single().Text.EndsWith("Still current instruction"),
            "An unchanged rejected dynamic instruction retries without spending a failed speech deadline");
        Reset(); var holder = PlayerFinder.Provider.Campaign;
        var quest = new Quest { id = 1, Owner = holder.statefulQuestList };
        var step = new QuestStep { quest = quest, id = 1 };
        var tracked = new StatefulQuest { quest = quest, currentStep = step };
        var objective = new StatefulObjective { statefulQuest = tracked,
            objective = new Objective { quest = quest, questStep = step, id = 1, isIncremental = true } };
        tracked.objectives.Add(objective); holder.statefulQuestList.trackedQuests.Add(tracked);
        row = Row(Tracker(), "Dynamic instructions", "Current dynamic turn"); row.gameObject.Active = false;
        void Both(double elapsed=.11)
        {
            Time.unscaledTimeAsDouble += elapsed; ScreenReader.SpokeThisFrame = false;
            QuestUpdateAccessibility.Update();
            DynamicObjectiveAccessibility.Update(higherPriorityPending:QuestUpdateAccessibility.HasPriorityOverDynamic);
        }
        Both(); Both(); objective.progress = 1; Both(); Both();
        row.gameObject.Active = true; objective.progress = 2; Both(); Both();
        ScreenReader.OutputAccepted = false; Both(2.1);
        Check(ScreenReader.Messages.Count == 1 && DynamicObjectiveAccessibility.HasPendingAnnouncement &&
            !QuestUpdateAccessibility.HasPriorityOverDynamic && QuestUpdateAccessibility.CanAnnounceAt(Time.unscaledTimeAsDouble),
            "Rejected dynamic output preserves its ready fairness turn and the unspent shared clock");
        ScreenReader.OutputAccepted = true; Both();
        Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text.Contains("Current dynamic turn"),
            "Recovery accepts the waiting dynamic turn before the continuously ready campaign counter");
        Both(); Check(ScreenReader.Messages.Count == 2 && QuestUpdateAccessibility.HasPendingAnnouncement,
            "Accepted dynamic recovery starts the shared deadline while campaign progress waits");
        Both(2.1);
        Check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last().Text.Contains("2/3"),
            "Campaign receives its existing next fair turn after accepted dynamic recovery");
    }

    private static void SharedFairnessTests()
    {
        Reset(); var holder=PlayerFinder.Provider.Campaign;
        var quest=new Quest{id=1,Owner=holder.statefulQuestList}; var step=new QuestStep{quest=quest,id=1};
        var tracked=new StatefulQuest{quest=quest,currentStep=step};
        var objective=new StatefulObjective{statefulQuest=tracked,objective=new Objective{quest=quest,questStep=step,id=1,isIncremental=true}};
        tracked.objectives.Add(objective); holder.statefulQuestList.trackedQuests.Add(tracked);
        var row=Row(Tracker(),"Dynamic instructions","First dungeon instruction"); row.gameObject.Active=false;
        void Both(double elapsed=.11)
        {
            Time.unscaledTimeAsDouble+=elapsed; ScreenReader.SpokeThisFrame=false;
            QuestUpdateAccessibility.Update();
            DynamicObjectiveAccessibility.Update(higherPriorityPending:QuestUpdateAccessibility.HasPriorityOverDynamic);
        }
        Both(); Both(); objective.progress=1; Both(); Both(); row.gameObject.Active=true; Both(); Both();
        objective.progress=2; Both(); Both(); Both(2.1);
        Check(ScreenReader.Messages.Count==2&&ScreenReader.Messages.Last().Text.Contains("First dungeon instruction")&&QuestUpdateAccessibility.HasPendingContent,
            "Continuously pending campaign counters cannot starve a settled waiting dynamic instruction");
        row.objectivesDisplayed[0].text.text="Second dungeon instruction"; Both(); Both(); Both(2.1);
        Check(ScreenReader.Messages.Count==3&&ScreenReader.Messages.Last().Text.Contains("2/3")&&DynamicObjectiveAccessibility.HasPendingContent,
            "Continuously pending dynamic instructions cannot starve a waiting campaign update");
        objective.progress=3; Both(); Both(); Both(2.1);
        Check(ScreenReader.Messages.Count==4&&ScreenReader.Messages.Last().Text.Contains("Second dungeon instruction"),
            "Both continuously active streams alternate their available shared speech turns");
        objective.progress=4; Both(); Both(); row.objectivesDisplayed[0].text.text="Changing provisional instruction";
        Both(); row.objectivesDisplayed[0].text.text="Another provisional instruction"; Both();
        Check(!DynamicObjectiveAccessibility.HasPendingAnnouncement&&QuestUpdateAccessibility.HasPriorityOverDynamic,
            "An unsettled dynamic candidate cannot reserve a speech turn ahead of ready campaign progression");
        row.gameObject.Active=false; Both(2.1); Both();
        Check(ScreenReader.Messages.Last().Text.Contains("4/3")&&!DynamicObjectiveAccessibility.HasPendingAnnouncement,
            "Hidden dynamic widgets yield priority and a ready campaign source still speaks");
    }
}
