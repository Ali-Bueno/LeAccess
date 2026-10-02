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
    private static void Reset()
    {
        NotificationAnnouncer.Reset();
        Time.frameCount = 0;
        Application.isFocused = true;
        LE.UI.LoadingScreen.IsVisible = false;
        ScreenContentAnnouncer.HasActiveContent = false;
        ScreenReader.Available = true;
        ScreenReader.Messages.Clear();
        Notifications.instance = null;
        LeAccess.Plugin.Log.Warnings.Clear();
    }
}
