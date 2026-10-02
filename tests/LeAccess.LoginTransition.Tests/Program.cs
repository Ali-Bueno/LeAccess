using System.Reflection;
using ClientAppState;
using LE.UI.Login;
using LE.UI.Login.UnityUI;
using LeAccess.Accessibility;
using LeAccess.Accessibility.Login;
using LeAccess.Accessibility.Patches;
using LeAccess.I18n;
using UnityEngine;
using UnityEngine.UI;

int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
bool Prefix(Type patch, LandingZonePanel panel) => (bool)patch.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)!
    .Invoke(null, new object[] { panel });
LandingZonePanel.OfflinePrefix = panel => Prefix(typeof(OfflineStartupClickPatch), panel);
LandingZonePanel.OnlinePrefix = panel => Prefix(typeof(OnlineStartupClickPatch), panel);

LandingZonePanel Setup()
{
    LoginTransitionGuard.Reset();
    ScreenReader.Spoken.Clear();
    ScreenReader.Throw = false;
    LeAccess.Plugin.Log.Messages.Clear();
    ClientStateManager.CurrentClientAppStateType = ClientAppStateType.SystemLoading;
    ClientStateManager._activeTransition = new() { Active = true };
    ClientStateManager.ThrowSnapshotRead = false;
    ClientStateManager.GeneratedGetterReads = 0;
    Application.isFocused = true;
    LE.UI.LoadingScreen.IsVisible = false;
    var panel = new LandingZonePanel { gameObject = new(), loginController = new() };
    panel.loginController.landingZonePanel = panel;
    Button ChildButton() => new() { gameObject = new() { transform = new() { Parent = panel.transform } } };
    panel.playOfflineButton = ChildButton();
    panel.playOnlineButton = ChildButton();
    return panel;
}
void Ready()
{
    ClientStateManager.CurrentClientAppStateType = ClientAppStateType.Login;
    ClientStateManager.NativeTransition.Active = false;
}
void Drain() { LoginTransitionGuard.Update(); LoginTransitionGuard.Update(); LoginTransitionGuard.Update(); }

ModText.Initialize(null, null);
ModText.SetLanguage("es");
var panel = Setup();
Drain();
Ready();
Drain();
Check(panel.OfflineStarts == 0 && panel.OnlineStarts == 0 && ScreenReader.Spoken.Count == 0,
    "Finishing startup without a user click never starts login or speaks a queued-selection message");
panel = Setup();
panel.OnPlayOfflineClicked();
Check(panel.OfflineStarts == 0 && panel.playOfflineButton.interactable && panel.playOnlineButton.interactable,
    "The real offline prefix defers before native code can disable both buttons or enter the failing transition");
Check(ScreenReader.Spoken.Count == 1 && ScreenReader.Spoken[0].StartsWith("Cargando."),
    "Pending selection is announced once using the Spanish production catalog");
panel.OnPlayOfflineClicked();
panel.OnPlayOfflineClicked();
Drain();
Check(panel.OfflineStarts == 0 && ScreenReader.Spoken.Count == 1,
    "Repeated early presses and update ticks neither start nor repeat pending speech");
ClientStateManager.CurrentClientAppStateType = ClientAppStateType.Login;
Drain();
Check(panel.OfflineStarts == 0, "Login type alone is insufficient while the native transition is still active");
Ready();
Drain();
Check(panel.OfflineStarts == 1 && panel.OnlineStarts == 0,
    "Startup completion replays precisely one offline click through the production prefix without recursion");
Check(!panel.playOfflineButton.interactable && ScreenReader.Spoken.Count == 1,
    "Original handler resumes normal native button state changes without new speech or action duplication");

panel = Setup();
panel.OnPlayOfflineClicked();
panel.OnPlayOnlineClicked();
Ready(); Drain();
Check(panel.OnlineStarts == 1 && panel.OfflineStarts == 0 && ScreenReader.Spoken.Count == 1,
    "The latest actual mode choice replaces pending offline; only the chosen online action runs");
panel = Setup();
panel.OnPlayOnlineClicked();
panel.OnPlayOfflineClicked();
Ready(); Drain();
Check(panel.OfflineStarts == 1 && panel.OnlineStarts == 0, "Latest-choice replacement also works from online to offline");
panel = Setup();
panel.OnPlayOfflineClicked();
Ready();
panel.OnPlayOnlineClicked();
Drain();
Check(panel.OnlineStarts == 1 && panel.OfflineStarts == 0,
    "A ready manual click executes normally and clears a prior queued action before Update can replay it");

void Cancels(Action<LandingZonePanel> change, string reason)
{
    var candidate = Setup();
    candidate.OnPlayOfflineClicked();
    change(candidate);
    LoginTransitionGuard.Update();
    Ready(); Drain();
    Check(candidate.OfflineStarts == 0 && candidate.OnlineStarts == 0, reason);
}
Cancels(p => p.gameObject.activeInHierarchy = false, "Hidden panel cancels its pending user action");
Cancels(p => p.gameObject.Visible = false, "Native visibility loss cancels pending action");
Cancels(p => p.isActiveAndEnabled = false, "Disabled landing panel cancels pending action");
Cancels(p => p._destroyed = true, "Destroyed lifecycle state cancels before replay");
Cancels(p => p.WasCollected = true, "Collected panel is discarded before unsafe Unity pointer comparison");
Cancels(p => p.gameObject.scene.isLoaded = false, "Unloaded login scene discards pending selection");
Cancels(p => p.gameObject.scene.handle++, "Panel moved to a different scene cannot inherit the original request");
Cancels(p => p.loginController.FSM.CurrentState = LoginController.LoginUIState.None, "Back or leaving LandingZone cancels intent");
Cancels(p => p.loginController.FSM.CurrentState = LoginController.LoginUIState.CharacterSelect, "Already completed native UI state cancels replay");
Cancels(p => p.loginController.WasCollected = true, "Collected login controller is never dereferenced");
Cancels(p => p.loginController = new() { landingZonePanel = p }, "Replacement login controller does not inherit pending intent");
Cancels(p => p.loginController.landingZonePanel = new() { gameObject = new() }, "Replaced controller panel invalidates old owner");
Cancels(p => p.loginController.FSM.WasCollected = true, "Collected FSM is discarded before reading state");
Cancels(p => p.loginController.FSM = new() { CurrentState = LoginController.LoginUIState.LandingZone }, "Replacement FSM cancels old request");
Cancels(p => p.playOfflineButton.WasCollected = true, "Collected button is never replayed");
Cancels(p => p.playOfflineButton.interactable = false, "A native-disabled button cannot be reactivated by replay");
Cancels(p => p.playOfflineButton = new() { gameObject = new() { transform = new() { Parent = p.transform } } },
    "Replacement selected button cannot inherit pending click");
Cancels(p => p.playOfflineButton.transform.Parent = new(), "A button reparented to unrelated UI is rejected");
Cancels(p => ClientStateManager.CurrentClientAppStateType = ClientAppStateType.InGame, "Gameplay transition cancels pending login action");
Cancels(p => ClientStateManager.CurrentClientAppStateType = ClientAppStateType.CharacterSelect, "Character selection transition cancels pending login action");

panel = Setup();
panel.OnPlayOfflineClicked();
Ready();
Application.isFocused = false;
Drain();
Check(panel.OfflineStarts == 0, "Focus loss cannot dispatch a retained user click");
Application.isFocused = true;
LE.UI.LoadingScreen.IsVisible = true;
Drain();
Check(panel.OfflineStarts == 0, "Remaining loading overlay delays action even after native transition completes");
LE.UI.LoadingScreen.IsVisible = false;
Drain();
Check(panel.OfflineStarts == 1, "Returning to the same ready visible context resumes once");
panel = Setup();
panel.OnPlayOfflineClicked();
LoginTransitionGuard.Reset();
Ready(); Drain();
Check(panel.OfflineStarts == 0, "Plugin or application cleanup discards pending requests");
panel = Setup();
panel.OnPlayOfflineClicked();
panel.ThrowOnStart = true;
Ready(); Drain();
Check(panel.OfflineStarts == 1 && LeAccess.Plugin.Log.Messages.Count(x => x.Contains("Native login failed")) == 1,
    "A native handler failure is reported once and never retried as a new automated action");
panel = Setup();
ScreenReader.Throw = true;
panel.OnPlayOfflineClicked();
Check(panel.OfflineStarts == 0, "Speech failure cannot release an early click into the known native race");
Ready(); Drain();
Check(panel.OfflineStarts == 1, "Speech failure preserves the captured user request for safe replay");
panel = Setup();
panel.OnPlayOfflineClicked();
var replacement = new LandingZonePanel { gameObject = new(), loginController = new() };
replacement.loginController.landingZonePanel = replacement;
replacement.playOfflineButton = new() { gameObject = new() { transform = new() { Parent = replacement.transform } } };
replacement.playOnlineButton = new() { gameObject = new() { transform = new() { Parent = replacement.transform } } };
panel.WasCollected = true;
replacement.OnPlayOfflineClicked();
Ready(); Drain();
Check(panel.OfflineStarts == 0 && replacement.OfflineStarts == 1,
    "A new explicit click can replace a collected pending panel without reading its stale pointer");

foreach (var phase in new[] { ClientAppStateType.None, ClientAppStateType.CharacterSelect, ClientAppStateType.InGame })
{
    panel = Setup();
    ClientStateManager.CurrentClientAppStateType = phase;
    Check(LoginTransitionGuard.MayExecute(panel, false) && ScreenReader.Spoken.Count == 0,
        $"Unrelated native {phase} behavior is not intercepted");
}
panel = Setup(); Ready();
Check(LoginTransitionGuard.MayExecute(panel, false), "Ready Login preserves normal offline behavior");
Check(LoginTransitionGuard.MayExecute(panel, true), "Ready Login preserves normal online behavior");
panel = Setup(); panel.loginController.landingZonePanel = null;
Check(LoginTransitionGuard.MayExecute(panel, false), "Unowned native panel is outside the guard's verified scope");
panel = Setup(); panel.loginController.FSM.CurrentState = LoginController.LoginUIState.EmailPrompt;
Check(LoginTransitionGuard.MayExecute(panel, false), "Other login substates retain native behavior");
panel = Setup(); ClientStateManager._activeTransition = null;
Check(!LoginTransitionGuard.MayExecute(panel, false), "Empty boxed transition is valid no-transition data, while SystemLoading still defers");
panel = Setup(); ClientStateManager.CurrentClientAppStateType = ClientAppStateType.Login;
panel.OnPlayOfflineClicked();
Check(panel.OfflineStarts == 0, "Login with an outstanding transition uses the same safe deferral as SystemLoading");
ModText.SetLanguage("en");
panel = Setup(); panel.OnPlayOfflineClicked();
Check(ScreenReader.Spoken.Single() == "Loading. Your selection will continue when the game is ready.",
    "English production catalog provides the same once-only pending indication");

panel = Setup();
Check(LoginTransitionState.IsTransitioning(), "Native nullable with a value produces an active transition");
Ready();
bool emptyGetterFailed = false;
try { _ = ClientStateManager._activeTransition; } catch (NullReferenceException) { emptyGetterFailed = true; }
Check(emptyGetterFailed, "Runtime double reproduces generated Nullable wrapper failure after native boxing returns zero");
int generatedReads = ClientStateManager.GeneratedGetterReads;
Check(!LoginTransitionState.IsTransitioning() && ClientStateManager.GeneratedGetterReads == generatedReads,
    "Production transition reader accepts empty boxing without constructing the faulty nullable wrapper");
Check(Il2CppInterop.Runtime.IL2CPP.FieldLookups == 1, "Native field metadata is resolved and cached once");

panel = Setup(); panel.OnPlayOfflineClicked();
ClientStateManager.ThrowSnapshotRead = true; Ready(); Drain();
Check(panel.OfflineStarts == 0 && LeAccess.Plugin.Log.Messages.Count(x => x.Contains("Transient native state observation failure")) == 1,
    "Transient readiness observation failure is deduplicated and never starts native login");
ClientStateManager.ThrowSnapshotRead = false; Drain();
Check(panel.OfflineStarts == 1 && panel.OnlineStarts == 0 && ScreenReader.Spoken.Count == 1,
    "Recovered readiness observation resumes the original offline click once without a second press");
Drain(); Check(panel.OfflineStarts == 1, "Observation recovery does not become repeated native login attempts");

panel = Setup(); panel.OnPlayOfflineClicked();
ClientStateManager.ThrowSnapshotRead = true; Ready(); Drain();
panel.loginController.FSM.CurrentState = LoginController.LoginUIState.EmailPrompt; Drain();
ClientStateManager.ThrowSnapshotRead = false;
panel.loginController.FSM.CurrentState = LoginController.LoginUIState.LandingZone; Drain();
Check(panel.OfflineStarts == 0, "A real ownership change during a transient observation failure still cancels the old request");
Console.WriteLine($"Login transition tests passed: {checks} checks.");
