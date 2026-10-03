using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;

internal static class Program
{
    private static int _checks;
    private static int Main()
    {
        try
        {
            StableVisibleDeliveryIsPassive();
            HiddenContentStaysSilent();
            NewDeliveryCanRepeatIdenticalText();
            GenericDeliveryCanRepeatIdenticalText();
            SameFrameHooksCoalesce();
            MultipleSourcesAndNestedChildren();
            OverlappingSourceRootsDoNotRepeatText();
            LifecycleInvalidatesStaleContent();
            PopupPriorityDoesNotReplayExpiredNotices();
            DialoguePriorityDoesNotReplayExpiredNotices();
            ScreenReaderAvailabilityDefersVisibleContent();
            MenuFeedbackFollowsBriefSelection();
            EncounterCountdownDoesNotQueueEachSecond();
            DeferredCountdownReadsCurrentVisibleTime();
            CountdownOwnershipAndOrdinaryChanges();
            CountdownCoroutineRestarts();
            UrgentCountdownPrecedesAmbientContent();
            UrgentCountdownRespectsMenuAndTyping();
            RejectedOutputRetriesOnlyCurrentVisibleSources();
            Console.WriteLine($"Notification tests passed: {_checks} checks.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void StableVisibleDeliveryIsPassive()
    {
        Reset();
        var source = Quick("<color=yellow>Provisional</color>");
        NotificationAnnouncer.TrackQuick(source);
        Tick();
        Check(ScreenReader.Messages.Count == 0, "A single frame cannot deliver provisional text");
        source._label.text = "  <color=yellow>Misión   completada</color>  ";
        Tick();
        Check(ScreenReader.Messages.Count == 0, "Changed text starts a new settling period");
        Tick();
        Check(ScreenReader.Messages.Single() == ("Misión completada", false), "Settled text is cleaned and spoken without interrupting");
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Unchanged visible notification is not repeated");
    }

    private static void HiddenContentStaysSilent()
    {
        foreach (string reason in new[] { "inactive", "parent-inactive", "group-zero", "parent-group-zero", "canvas-disabled", "culled", "label-alpha", "label-disabled", "input" })
        {
            Reset();
            var parent = new GameObject();
            var root = parent.Child();
            var source = Quick("Must remain silent", root);
            if (reason == "inactive") root.Active = false;
            if (reason == "parent-inactive") parent.Active = false;
            if (reason == "group-zero") root.Add(new CanvasGroup { alpha = 0 });
            if (reason == "parent-group-zero") parent.Add(new CanvasGroup { alpha = 0 });
            if (reason == "canvas-disabled") parent.Add(new Canvas { Enabled = false });
            if (reason == "culled") source._label.canvasRenderer.cull = true;
            if (reason == "label-alpha") source._label.alpha = 0;
            if (reason == "label-disabled") source._label.Enabled = false;
            if (reason == "input") parent.Add(new TMP_InputField());
            NotificationAnnouncer.TrackQuick(source);
            Tick(); Tick(); Tick();
            Check(ScreenReader.Messages.Count == 0, $"Visibility excludes {reason}");
        }
        Reset();
        var hiddenParent = new GameObject();
        hiddenParent.Add(new CanvasGroup { alpha = 0 });
        var independentRoot = hiddenParent.Child();
        independentRoot.Add(new CanvasGroup { ignoreParentGroups = true });
        NotificationAnnouncer.TrackQuick(Quick("Independent visible group", independentRoot));
        Tick(); Tick();
        Check(ScreenReader.Messages.Single().Text == "Independent visible group", "Unity ignoreParentGroups semantics remain visible");
    }

    private static void NewDeliveryCanRepeatIdenticalText()
    {
        Reset();
        var source = Quick("Inventario lleno");
        NotificationAnnouncer.TrackQuick(source);
        Tick(); Tick();
        NotificationAnnouncer.TrackQuick(source);
        Tick();
        Check(ScreenReader.Messages.Count == 1, "Repeated delivery must settle again");
        Tick();
        Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.All(message => message.Text == "Inventario lleno"),
            "A new event on a reused visible widget can repeat identical text");
        source._label.gameObject.Active = false;
        Tick();
        source._label.gameObject.Active = true;
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 3, "Hide then show also makes identical content a new notification");
    }

    private static void SameFrameHooksCoalesce()
    {
        Reset();
        var source = Quick("Objeto recibido");
        NotificationAnnouncer.TrackQuick(source);
        NotificationAnnouncer.TrackQuick(source);
        NotificationAnnouncer.Update();
        NotificationAnnouncer.TrackQuick(source);
        Tick();
        Check(ScreenReader.Messages.Count == 1, "Repeated display hooks in the same frame do not reset settlement or duplicate delivery");
    }

    private static void GenericDeliveryCanRepeatIdenticalText()
    {
        Reset();
        var holder = new GameObject();
        var notice = holder.Child();
        notice.Add(new TMP_Text { text = "Oro insuficiente" });
        var widget = notice.Add(new LE.UI.NotificationSystem.NotificationBase());
        NotificationAnnouncer.GameplayDelivered(widget);
        NotificationAnnouncer.GameplayDelivered(widget);
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Repeated channel display hooks coalesce into one delivery");
        NotificationAnnouncer.GameplayDelivered(widget);
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.All(message => message == ("Oro insuficiente", false)),
            "Generic notification instances may be reused for an identical new event");
    }

    private static void MultipleSourcesAndNestedChildren()
    {
        Reset();
        var popup = new GameObject();
        popup.Child().Add(new TMP_Text { text = "Conexión" });
        popup.Child().Add(new UnityEngine.UI.Text { text = "Restablecida" });
        NotificationAnnouncer.TrackController(new GameObject().Add(new ControllerNotification { notification = popup.transform }));
        NotificationAnnouncer.TrackChat(new GameObject().Add(new OverChatNotification { notification = popup.transform }));
        var location = new GameObject();
        location.Add(new TMP_Text { text = "Campamento" });
        var locationOwner = new GameObject().Add(new LocationNotificationManager { holder = location });
        NotificationAnnouncer.TrackLocation(locationOwner);
        Check(ReferenceEquals(LeAccess.Accessibility.Zones.ZoneAnnouncer.LastTracked, locationOwner),
            "Location deliveries transfer to the scene reader instead of the passive notification queue");
        var holder = new GameObject();
        holder.Child().Child().Add(new TMP_Text { text = "Nivel aumentado" });
        holder.Child().Add(new TMP_Text { text = "Nuevo punto" });
        var template = holder.Child();
        template.Active = false;
        template.Add(new TMP_Text { text = "Invisible template" });
        NotificationAnnouncer.TrackGameplay(new GameObject().Add(new Notifications { holder = holder }));
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Concurrent sources combine into one passive utterance");
        Check(ScreenReader.Messages[0] == ("Conexión. Restablecida. Nivel aumentado. Nuevo punto", false),
            "Shared roots deduplicate; location remains separately owned and hidden templates are excluded");
        holder.Children[0].Active = false;
        holder.Children[1].GetComponent<TMP_Text>().text = "Punto asignado";
        Tick(); Tick();
        Check(ScreenReader.Messages.Last().Text == "Punto asignado", "Updating one child does not repeat unrelated source content");
    }

    private static void LifecycleInvalidatesStaleContent()
    {
        foreach (string reason in new[] { "loading", "focus", "destroyed-root", "collected-root", "destroyed-owner", "collected-owner" })
        {
            Reset();
            var source = Quick("Expired");
            NotificationAnnouncer.TrackQuick(source);
            Tick();
            if (reason == "loading") LE.UI.LoadingScreen.IsVisible = true;
            if (reason == "focus") Application.isFocused = false;
            if (reason == "destroyed-root") source._label.gameObject.Destroyed = true;
            if (reason == "collected-root") source._label.gameObject.WasCollected = true;
            if (reason == "destroyed-owner") source.Destroyed = true;
            if (reason == "collected-owner") source.WasCollected = true;
            Tick();
            source._label.gameObject.Active = false;
            LE.UI.LoadingScreen.IsVisible = false;
            Application.isFocused = true;
            Tick(); Tick();
            Check(ScreenReader.Messages.Count == 0, $"{reason} prevents an expired notification from speaking");
            Check(LeAccess.Plugin.Log.Warnings.Count == 0, $"{reason} requires no invalid native-wrapper access");
        }
    }

    private static void OverlappingSourceRootsDoNotRepeatText()
    {
        foreach (bool childFirst in new[] { false, true })
        {
            Reset();
            var root = new GameObject();
            root.Child().Add(new TMP_Text { text = "Conexión" });
            var body = root.Child().Add(new TMP_Text { text = "Restablecida" });
            var controller = new GameObject().Add(new ControllerNotification { notification = root.transform });
            var quick = new GameObject().Add(new UIQuickNotificationText { _label = body });
            if (childFirst) NotificationAnnouncer.TrackQuick(quick);
            NotificationAnnouncer.TrackController(controller);
            if (!childFirst) NotificationAnnouncer.TrackQuick(quick);
            Tick(); Tick();
            Check(ScreenReader.Messages.Single().Text == "Conexión. Restablecida",
                $"Overlapping notification roots must not repeat child text (child first={childFirst}): actual '{ScreenReader.Messages.Single().Text}'");
            NotificationAnnouncer.TrackQuick(quick);
            Tick(); Tick();
            Check(ScreenReader.Messages.Count == 2,
                "A new delivery on a suppressed child must rearm the covering ancestor");
        }
    }

    private static void PopupPriorityDoesNotReplayExpiredNotices()
    {
        Reset();
        ScreenContentAnnouncer.HasActiveContent = true;
        var expired = Quick("Disappeared while popup was open");
        var current = Quick("Still visible");
        NotificationAnnouncer.TrackQuick(expired);
        NotificationAnnouncer.TrackQuick(current);
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 0, "Active popup content takes priority over passive notifications");
        expired._label.gameObject.Active = false;
        Tick();
        ScreenContentAnnouncer.HasActiveContent = false;
        Tick();
        Check(ScreenReader.Messages.Single() == ("Still visible", false), "Only a still-visible notification is delivered after the popup clears");
        current._label.gameObject.Active = false;
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Expired popup-blocked text is never replayed later");
    }

    private static void ScreenReaderAvailabilityDefersVisibleContent()
    {
        Reset();
        ScreenReader.Available = false;
        NotificationAnnouncer.TrackQuick(Quick("Current notice"));
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 0, "Unavailable speech backend cannot consume a pending visible notice");
        ScreenReader.Available = true;
        Tick();
        Check(ScreenReader.Messages.Single().Text == "Current notice", "Pending visible notice can speak when the backend is available");
    }

    private static void DialoguePriorityDoesNotReplayExpiredNotices()
    {
        Reset();
        var notice = Quick("Visible during conversation");
        NotificationAnnouncer.TrackQuick(notice);
        Tick(true); Tick(true);
        Check(ScreenReader.Messages.Count == 0, "Dialogue priority defers passive notifications");
        notice._label.gameObject.Active = false;
        Tick(true);
        Tick();
        Check(ScreenReader.Messages.Count == 0, "A notice that expired during dialogue is not queued for later speech");
    }

    private static UIQuickNotificationText Quick(string text, GameObject root = null)
    {
        root ??= new GameObject();
        return new GameObject().Add(new UIQuickNotificationText { _label = root.Add(new TMP_Text { text = text }) });
    }
    private static void MenuFeedbackFollowsBriefSelection()
    {
        Reset();
        var notice = Quick("No puedes equipar este objeto: requiere nivel 12");
        NotificationAnnouncer.TrackQuick(notice);
        Tick(true); Tick(true);
        Check(ScreenReader.Messages.Count == 0, "Pending menu selection has priority over equipment rejection text");
        Tick(false);
        Check(ScreenReader.Messages.Single().Text.Contains("requiere nivel 12"),
            "Once the brief settles, the still-visible rejection speaks without closing the menu");
        Tick(false);
        Check(ScreenReader.Messages.Count == 1, "An unchanged equipment rejection is not repeated every frame");
    }
    private static void Tick(bool otherContentActive = false)
    {
        Time.frameCount++;
        NotificationAnnouncer.Update(otherContentActive);
    }
    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static LE.UI.NotificationSystem.RageOfMorditasOutOfGauntletNotification Countdown(GameObject root = null)
    {
        root ??= new GameObject();
        var source = root.Add(new LE.UI.NotificationSystem.RageOfMorditasOutOfGauntletNotification());
        source._countdownText = root.Child().Add(new TextMeshProUGUI { text = "Abandonando evento en 10s" });
        return source;
    }

    private static void EncounterCountdownDoesNotQueueEachSecond()
    {
        Reset();
        var source = Countdown();
        NotificationAnnouncer.GameplayDelivered(source);
        Tick(); Tick();
        Check(ScreenReader.Messages.Single().Text == "Abandonando evento en 10s", "Opening countdown reads the full localized rendered instruction");
        foreach (int second in new[] { 9, 8, 7 })
        {
            source._countdownText.text = $"Abandonando evento en {second}s";
            Tick(); Tick();
        }
        Check(ScreenReader.Messages.Count == 1, "Native one-second countdown changes do not queue the full instruction each second");
        NotificationAnnouncer.GameplayDelivered(source);
        source._countdownText.text = "Abandonando evento en 20s";
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text.EndsWith("20s"), "Explicit delivery restarts a still-visible countdown");
        source.gameObject.Active = false;
        Tick();
        source.gameObject.Active = true;
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 3, "A hide/show lifecycle rearms identical countdown instructions");
        Application.isFocused = false;
        Tick();
        Application.isFocused = true;
        source._countdownText.text = "Abandonando evento en 15s";
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 4 && ScreenReader.Messages.Last().Text.EndsWith("15s"), "Focus recovery reads the current countdown once");
    }

    private static void DeferredCountdownReadsCurrentVisibleTime()
    {
        Reset();
        var source = Countdown();
        NotificationAnnouncer.GameplayDelivered(source);
        Tick(true); Tick(true);
        source._countdownText.text = "Abandonando evento en 7s";
        Tick(true); Tick(true);
        Check(ScreenReader.Messages.Count == 0, "A deferred countdown never marks an unspoken initial timer as delivered");
        Tick();
        Check(ScreenReader.Messages.Single().Text.EndsWith("7s"), "A deferred countdown reads the latest visible time when priority clears");
        source._countdownText.text = "Abandonando evento en 6s";
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "The delivered current countdown then stays quiet");
        Reset();
        source = Countdown();
        NotificationAnnouncer.GameplayDelivered(source);
        Tick(true); Tick(true);
        source.gameObject.Active = false;
        Tick(true); Tick();
        Check(ScreenReader.Messages.Count == 0, "An expired deferred countdown is discarded");
    }

    private static void CountdownOwnershipAndOrdinaryChanges()
    {
        Reset();
        var holder = new GameObject();
        var source = Countdown(holder.Child());
        var other = source.gameObject.Child().Add(new TextMeshProUGUI { text = "Regresa a la arena" });
        var gameplay = new GameObject().Add(new Notifications { holder = holder });
        NotificationAnnouncer.TrackGameplay(gameplay);
        NotificationAnnouncer.GameplayDelivered(source);
        Tick(); Tick();
        source._countdownText.text = "Abandonando evento en 9s";
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 1, "Holder discovery and direct delivery share countdown ownership without repeating");
        other.text = "Evento cancelado";
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text.Contains("Evento cancelado"), "Other changed labels on a countdown widget remain readable");
        NotificationAnnouncer.GameplayDelivered(source);
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 3 && ScreenReader.Messages.Last().Text.Contains("9s"), "Rearming through an overlapping holder clears the frozen timer");
        Reset();
        var ordinary = Quick("Abandonando evento en 10s");
        NotificationAnnouncer.TrackQuick(ordinary);
        Tick(); Tick();
        ordinary._label.text = "Abandonando evento en 9s";
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2, "Matching prose without the native countdown type remains an ordinary changing notice");
        Reset();
        var owner = Countdown();
        var foreign = new GameObject().Add(new TextMeshProUGUI { text = "Aviso externo 10" });
        owner._countdownText = foreign;
        var quick = foreign.gameObject.Add(new UIQuickNotificationText { _label = foreign });
        NotificationAnnouncer.GameplayDelivered(owner);
        NotificationAnnouncer.TrackQuick(quick);
        Tick(); Tick();
        foreign.text = "Aviso externo 9";
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last().Text.Contains("Aviso externo 9"), "A foreign label assigned to a countdown owner never acquires countdown suppression");
    }

    private static void CountdownCoroutineRestarts()
    {
        Reset();
        var source = Countdown();
        NotificationAnnouncer.GameplayDelivered(source);
        Tick(); Tick();
        source._currentCountdownCoroutine = new();
        Tick();
        Check(ScreenReader.Messages.Count == 1, "A new native countdown coroutine must settle before delivery");
        Tick();
        Check(ScreenReader.Messages.Count == 2, "A StartCountdown-only restart reads even identical text without a new notification hook");
        source._countdownText.gameObject.Active = false;
        Tick();
        source._countdownText.gameObject.Active = true;
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 3, "Hiding just the countdown label invalidates its delivered identity");
        Reset();
        source = Countdown();
        source._currentCountdownCoroutine.WasCollected = true;
        NotificationAnnouncer.GameplayDelivered(source);
        Tick(); Tick();
        source._countdownText.text = "Aviso actualizado";
        Tick(); Tick();
        Check(ScreenReader.Messages.Count == 2, "An unauthenticated collected coroutine falls back to normal visible feedback");
    }

    private static void UrgentCountdownPrecedesAmbientContent()
    {
        Reset();
        var source = Countdown();
        NotificationAnnouncer.GameplayDelivered(source);
        NotificationAnnouncer.TrackQuick(Quick("Notificación normal"));
        for (int frame = 0; frame < 2; frame++)
        {
            Time.frameCount++;
            NotificationAnnouncer.UpdateUrgent();
            NotificationAnnouncer.Update(true);
            if (frame == 0) Check(ScreenReader.Messages.Count == 0, "Urgent and normal passes cannot settle a timer twice in one frame");
        }
        Check(ScreenReader.Messages.Count == 1 && ScreenReader.Messages.Single().Text.EndsWith("10s") &&
            ScreenReader.Messages.Single().Interrupt, "Urgent pass interrupts prior passive speech for the typed countdown while ambient priority continues blocking normal notices");
        Tick();
        Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last() == ("Notificación normal", false), "Urgent discovery leaves ordinary notice settlement, passive delivery and priority intact");
        foreach (string gate in new[] { "foreground", "same-frame", "modal", "loading", "focus" })
        {
            Reset();
            source = Countdown();
            NotificationAnnouncer.GameplayDelivered(source);
            for (int frame = 0; frame < 3; frame++)
            {
                Time.frameCount++;
                ScreenReader.SpokeThisFrame = gate == "same-frame";
                ScreenContentAnnouncer.HasActiveContent = gate == "modal";
                LE.UI.LoadingScreen.IsVisible = gate == "loading";
                Application.isFocused = gate != "focus";
                NotificationAnnouncer.UpdateUrgent(gate == "foreground");
                NotificationAnnouncer.Update(true);
            }
            Check(ScreenReader.Messages.Count == 0, $"Urgent countdown honors {gate} priority");
            ScreenReader.SpokeThisFrame = false;
            ScreenContentAnnouncer.HasActiveContent = false;
            LE.UI.LoadingScreen.IsVisible = false;
            Application.isFocused = true;
            Time.frameCount++;
            NotificationAnnouncer.UpdateUrgent();
            Time.frameCount++;
            NotificationAnnouncer.UpdateUrgent();
            Check(ScreenReader.Messages.Count == 1, $"A still-visible urgent countdown delivers once after {gate} clears");
        }
        Reset();
        source = Countdown();
        NotificationAnnouncer.GameplayDelivered(source);
        Time.frameCount++;
        NotificationAnnouncer.UpdateUrgent(true);
        Time.frameCount++;
        NotificationAnnouncer.UpdateUrgent(true);
        source.gameObject.Active = false;
        Time.frameCount++;
        NotificationAnnouncer.UpdateUrgent();
        Time.frameCount++;
        NotificationAnnouncer.UpdateUrgent();
        Check(ScreenReader.Messages.Count == 0, "An urgent countdown hidden behind foreground speech never replays after expiring");
    }

    private static void UrgentCountdownRespectsMenuAndTyping()
    {
        foreach (string gate in new[] { "paused", "menu", "chat", "keyboard", "ownership-query", "input-query", "keyboard-query" })
        {
            Reset();
            var source = Countdown();
            NotificationAnnouncer.GameplayDelivered(source);
            var input = new GameObject().Add(new EpochInputManager());
            var keyboard = new GameObject().Add(new ScreenKeyboardManager());
            EpochInputManager.instance = input;
            ScreenKeyboardManager.Instance = keyboard;
            LeAccess.Accessibility.Navigation.NavigationAudioState.IsSuspended = gate == "paused";
            LeAccess.Accessibility.Navigation.GameplayInputState.Owns = gate != "menu";
            LeAccess.Accessibility.Navigation.GameplayInputState.ThrowOnQuery = gate == "ownership-query";
            input.Typing = gate == "chat"; input.ThrowOnQuery = gate == "input-query";
            keyboard.Enabled = gate == "keyboard"; keyboard.ThrowOnQuery = gate == "keyboard-query";
            void Frame()
            {
                Time.frameCount++;
                NotificationAnnouncer.UpdateUrgent();
                NotificationAnnouncer.Update();
            }
            Frame(); Frame();
            Check(ScreenReader.Messages.Count == 0, $"Urgent countdown fails closed for {gate}, including the later normal pass");
            NotificationAnnouncer.TrackQuick(Quick("No puedes equipar este objeto"));
            Frame(); Frame();
            Check(ScreenReader.Messages.Single() == ("No puedes equipar este objeto", false),
                $"Ordinary menu-action notices retain passive delivery while {gate} blocks the urgent countdown");
            LeAccess.Accessibility.Navigation.NavigationAudioState.IsSuspended = false;
            LeAccess.Accessibility.Navigation.GameplayInputState.Owns = true;
            LeAccess.Accessibility.Navigation.GameplayInputState.ThrowOnQuery = false;
            input.Typing = input.ThrowOnQuery = false;
            keyboard.Enabled = keyboard.ThrowOnQuery = false;
            source._countdownText.text = "Abandonando evento en 6s";
            Frame(); Frame();
            Check(ScreenReader.Messages.Count == 2 && ScreenReader.Messages.Last() == ("Abandonando evento en 6s", true),
                $"The current visible urgent countdown delivers after {gate} releases ownership");
        }
    }

    private static void RejectedOutputRetriesOnlyCurrentVisibleSources()
    {
        Reset();
        var quick = Quick("Aviso actual");
        NotificationAnnouncer.TrackQuick(quick);
        ScreenReader.OutputAccepted = false; Tick(); Tick();
        ScreenReader.OutputAccepted = true; Tick();
        Check(ScreenReader.Messages.Single() == ("Aviso actual", false), "Rejected unchanged ordinary notification retries after recovery");
        Tick(); Check(ScreenReader.Messages.Count == 1, "Accepted ordinary notification is acknowledged once");
        Reset(); quick = Quick("Anterior"); NotificationAnnouncer.TrackQuick(quick);
        ScreenReader.OutputAccepted = false; Tick(); Tick();
        quick._label.text = "Actual"; ScreenReader.OutputAccepted = true; Tick(); Tick();
        Check(ScreenReader.Messages.Single().Text == "Actual", "Changed rejected notification rereads current visible text");
        Reset(); quick = Quick("Expired"); NotificationAnnouncer.TrackQuick(quick);
        ScreenReader.OutputAccepted = false; Tick(); Tick();
        quick._label.gameObject.Active = false; ScreenReader.OutputAccepted = true; Tick(); Tick();
        Check(ScreenReader.Messages.Count == 0, "Expired rejected notification never replays");
        Reset(); var countdown = Countdown(); NotificationAnnouncer.GameplayDelivered(countdown);
        void UrgentFrame() { Time.frameCount++; NotificationAnnouncer.UpdateUrgent(); NotificationAnnouncer.Update(true); }
        ScreenReader.OutputAccepted = false; UrgentFrame(); UrgentFrame();
        countdown._countdownText.text = "Abandonando evento en 6s";
        ScreenReader.OutputAccepted = true; UrgentFrame(); UrgentFrame();
        Check(ScreenReader.Messages.Single() == ("Abandonando evento en 6s", true), "Rejected urgent countdown retries the latest live caption after recovery");
        UrgentFrame(); Check(ScreenReader.Messages.Count == 1, "Accepted recovered countdown is acknowledged once");
        Reset(); countdown = Countdown(); NotificationAnnouncer.GameplayDelivered(countdown);
        ScreenReader.OutputAccepted = false; UrgentFrame(); UrgentFrame();
        countdown.gameObject.Active = false; ScreenReader.OutputAccepted = true; UrgentFrame(); UrgentFrame();
        Check(ScreenReader.Messages.Count == 0, "Rejected countdown hidden before recovery is discarded");
        Reset();
        quick = Quick("Primer aviso");
        var second = Quick("Segundo aviso");
        NotificationAnnouncer.TrackQuick(quick); NotificationAnnouncer.TrackQuick(second);
        ScreenReader.OutputAccepted = false; Tick(); Tick();
        second._label.gameObject.Active = false;
        ScreenReader.OutputAccepted = true; Tick();
        Check(ScreenReader.Messages.Single().Text == "Primer aviso", "Recovery rebuilds a rejected batch from only current visible sources");
        Tick(); Check(ScreenReader.Messages.Count == 1, "Acceptance acknowledges each surviving batch source");
        Reset(); countdown = Countdown(); NotificationAnnouncer.GameplayDelivered(countdown);
        ScreenReader.OutputAccepted = false; UrgentFrame(); UrgentFrame();
        countdown._currentCountdownCoroutine = new();
        countdown._countdownText.text = "Abandonando evento en 20s";
        ScreenReader.OutputAccepted = true; UrgentFrame(); UrgentFrame();
        Check(ScreenReader.Messages.Single().Text.EndsWith("20s"), "Rejected countdown recovery belongs to the current restarted coroutine");
        Reset(); quick = Quick("Modal diferido"); NotificationAnnouncer.TrackQuick(quick);
        ScreenReader.OutputAccepted = false; Tick(); Tick();
        ScreenReader.OutputAccepted = true; ScreenContentAnnouncer.HasActiveContent = true; Tick();
        Check(ScreenReader.Messages.Count == 0, "Recovered output does not bypass current modal priority");
        ScreenContentAnnouncer.HasActiveContent = false; Tick();
        Check(ScreenReader.Messages.Single().Text == "Modal diferido", "A current rejected notice resumes after foreground ownership releases");
        Reset();
    }

    private static void Reset()
    {
        NotificationAnnouncer.Reset();
        Time.frameCount = 0;
        Application.isFocused = true;
        LE.UI.LoadingScreen.IsVisible = false;
        ScreenContentAnnouncer.HasActiveContent = false;
        ScreenReader.Available = true;
        ScreenReader.OutputAccepted = true;
        ScreenReader.SpokeThisFrame = false;
        ScreenReader.Messages.Clear();
        Notifications.instance = null;
        EpochInputManager.instance = null;
        ScreenKeyboardManager.Instance = null;
        LeAccess.Accessibility.Navigation.NavigationAudioState.IsSuspended = false;
        LeAccess.Accessibility.Navigation.GameplayInputState.Owns = true;
        LeAccess.Accessibility.Navigation.GameplayInputState.ThrowOnQuery = false;
        LeAccess.Plugin.Log.Warnings.Clear();
    }
}
