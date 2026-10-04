using LeAccess.Accessibility;
using LeAccess.Accessibility.Quests;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.I18n;
using TMPro;
using UnityEngine;

internal static class ModelTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string label) { checks++; if (!condition) throw new Exception(label); }
        string T(string key, params object[] args) => ModText.Format(key, args);
        void Tick() { ScreenReader.SpokeThisFrame = false; QuestAccessibility.Update(); }
        var panel = new GameObject().Add(new QuestPanel());
        var generator = new GameObject().Add(new QuestButtonGenerator { questPanel = panel });
        panel.buttonGenerator = generator;
        var quest = new Quest { id = 1 };
        var step = new QuestStep { quest = quest, id = 2, displayText = "Native localized step description" };
        var tracked = new StatefulQuest { quest = quest, currentStep = step };
        quest.List = new StatefulQuestList { Selected = tracked };
        var active = new ObjectiveAndState
        {
            objective = new Objective { quest = quest, questStep = step, Text = "Native active objective" },
            state = QuestState.Active
        };
        var completed = new ObjectiveAndState
        {
            objective = new Objective { quest = quest, questStep = step, Text = "Prior completed objective" },
            state = QuestState.Completed
        };
        quest.List.Objectives.Add(active);
        quest.List.Objectives.Add(completed);
        var button = new GameObject().Add(new QuestButton { quest = quest, buttonGenerator = generator });
        button.tmp = button.gameObject.Add(new TextMeshProUGUI { text = "Selected quest" });
        var main = new GameObject(); main.transform.parent = button.transform;
        button.buttonRectTransform = main.Add(new RectTransform());
        generator._selectedButton_k__BackingField = button;
        var formatter = new GameObject().Add(new QuestInformation());
        generator.questInformation = formatter;
        // The native formatter is stateless; displayed widget data can lag or be absent.
        formatter.quest = formatter.previousQuest = new Quest();
        formatter.isActiveAndEnabled = false;
        formatter.gameObject.activeInHierarchy = false;
        var map = new GameObject().Add(new MapPanel { questPanel = panel });
        NavigationManager.currentlySelected = new GameObject().Add(new LE.UI.PanelSystem.WorldMapPanel { _mapPanel = map });
        QuestAccessibility.Reset();

        Check(QuestSelectionReader.TryRead(generator, out var selected), "native model reads without rendered information");
        Check(selected.Summary.StartsWith("Selected quest. Localized Active") && selected.Objectives.Contains(T("quest.current_objectives", "Native active objective")),
            "navigation includes authoritative active state and current objective");
        Check(!selected.Objectives.Contains("Prior completed") && !selected.Objectives.Contains("step description"),
            "completed objectives and description deferred to manual gesture");
        Check(selected.Details.Contains("step description") && selected.Details.Contains(T("quest.objective_history", T("quest.objective_completed", "Prior completed objective"))),
            "manual content includes native step and completed objectives");
        Check(selected.Text.Split("Native active objective").Length == 2, "active objective appears once in full read");
        Check(QuestAccessibility.TryHandleFocus(main), "declared main button focus claimed");
        var nestedPin = new GameObject(); nestedPin.transform.parent = button.transform;
        Check(!QuestAccessibility.TryHandleFocus(nestedPin), "other nested controls still retain focus");
        Tick(); Tick();
        Check(ScreenReader.Spoken.Last().Contains("Native active objective") &&
            !ScreenReader.Spoken.Last().Contains("step description"), "model navigation remains brief");
        Check(QuestAccessibility.ReadCurrentDetails() && ScreenReader.Spoken.Last().Contains("step description"),
            "manual request reads native details with hidden widget");
        int spoken = ScreenReader.Spoken.Count;
        Tick(); Tick();
        Check(ScreenReader.Spoken.Count == spoken, "quest polling cannot interrupt manual detail");
        completed.objective.Text = "Changed earlier objective";
        Tick(); Tick();
        Check(ScreenReader.Spoken.Count == spoken, "Changes to model history remain silent during navigation");
        completed.objective.Text = "Prior completed objective";
        var failed = new ObjectiveAndState { objective = new Objective { quest = quest, questStep = step,
            Text = "An earlier failed objective" }, state = QuestState.Failed };
        quest.List.Objectives.Add(failed);
        QuestSelectionReader.TryRead(generator, out var withHistory);
        Check(!withHistory.Objectives.Contains("failed") && withHistory.Details.Contains(T("quest.objective_failed", failed.objective.Text)),
            "Failed objectives are labeled and confined to manual history");
        Check(withHistory.Details.Split(T("quest.objective_history", "").Trim()).Length == 2,
            "One history introduction groups all earlier objectives of the selected quest");
        failed.objective.quest = new Quest();
        QuestSelectionReader.TryRead(generator, out withHistory);
        Check(!withHistory.Text.Contains("An earlier failed objective"),
            "History cannot borrow an objective from an unselected quest");
        Check(QuestSelectionReader.TryRead(generator, out selected) &&
            selected.Objectives.StartsWith(T("quest.current_objectives", "").Trim()) && !selected.Objectives.Contains(T("quest.completed")),
            "The first journal selection distinguishes current objectives from completed history");
        step.displayText = "Changed longer native description";
        Tick(); Tick();
        Check(ScreenReader.Spoken.Count == spoken, "long description changes do not auto speak");
        active.objective.Text = "Changed current objective";
        Tick(); Tick();
        Check(ScreenReader.Spoken.Count == ++spoken && ScreenReader.Spoken.Last().Contains("Changed current objective"),
            "current objective updates announced while selected");
        tracked.State = QuestState.Completed;
        Tick(); Tick();
        Check(ScreenReader.Spoken.Count == ++spoken && ScreenReader.Spoken.Last().Contains("Localized Completed"),
            "native state changes announced independently of stale button completion");
        Check(!ScreenReader.Spoken.Last().Contains("Changed current objective"),
            "completed quest cannot announce a retained active row as an unfinished objective");
        QuestAccessibility.Reset(); Tick();
        Check(QuestAccessibility.HasPendingContent, "model pending brief before manual request");
        QuestAccessibility.ReadCurrentDetails(); spoken = ScreenReader.Spoken.Count;
        Tick(); Tick();
        Check(ScreenReader.Spoken.Count == spoken && !QuestAccessibility.HasPendingContent, "manual read acknowledges pending quest brief");

        var foreignQuest = new Quest();
        tracked.quest = foreignQuest;
        Check(!QuestModelReader.TryRead(quest, formatter, out _, out _, out _), "foreign stateful quest rejected");
        tracked.quest = quest;
        tracked.State = QuestState.Active;
        active.objective.quest = foreignQuest;
        QuestModelReader.TryRead(quest, formatter, out _, out var objectives, out _);
        Check(!objectives.Contains("Changed current"), "foreign objective quest rejected");
        active.objective.quest = quest;
        active.objective.questStep = new QuestStep { quest = foreignQuest };
        QuestModelReader.TryRead(quest, formatter, out _, out objectives, out _);
        Check(!objectives.Contains("Changed current"), "foreign objective step rejected");
        active.objective.questStep = step;
        foreach (var hiddenState in new[] { QuestState.NotStarted, QuestState.NoLongerRelevant })
        {
            active.state = hiddenState;
            QuestModelReader.TryRead(quest, formatter, out _, out objectives, out var details);
            Check(!objectives.Contains("Changed current") && !details.Contains("Changed current"), "native hidden objective state excluded " + hiddenState);
        }
        active.state = QuestState.Active;
        tracked.currentStep = new QuestStep { quest = foreignQuest, displayText = "Foreign description" };
        QuestModelReader.TryRead(quest, formatter, out _, out _, out var modelDetails);
        Check(!modelDetails.Contains("Foreign description"), "foreign current step description rejected");
        tracked.currentStep = step;
        active.WasCollected = true;
        Check(QuestModelReader.TryRead(quest, formatter, out _, out objectives, out _) &&
            !objectives.Contains("Changed current"), "collected objective wrapper ignored");
        active.WasCollected = false;
        quest.List.WasCollected = true;
        Check(!QuestModelReader.TryRead(quest, formatter, out _, out _, out _), "collected stateful list rejected");
        quest.List.WasCollected = false;
        quest.List.Selected = null;
        Check(!QuestModelReader.TryRead(quest, formatter, out _, out _, out _), "missing player quest rejected");
        quest.List.Selected = tracked;
        ScreenContentAnnouncer.HasActiveContent = true;
        Check(!QuestAccessibility.ReadCurrentDetails(), "quest manual request respects modal priority");
        ScreenContentAnnouncer.HasActiveContent = false;
        NavigationManager.currentlySelected = null;
        Check(!QuestAccessibility.ReadCurrentDetails(), "quest manual request cannot reuse closed map");
        Check(LeAccess.Plugin.Log.Information.Last().StartsWith("[Quest details]"), "manual delivery has its own diagnostic");
        return checks;
    }
}
