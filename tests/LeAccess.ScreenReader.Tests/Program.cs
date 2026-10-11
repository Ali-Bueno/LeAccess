using DavyKager;
using LeAccess.Accessibility;
using UnityEngine;

int checks = 0;
var failures = new List<string>();
void Check(bool condition, string label) { checks++; if (!condition) failures.Add(label); }
void Reset(string name = "NVDA")
{
    ScreenReader.Shutdown();
    Tolk.Reset(); Tolk.Name = name;
    Time.frameCount++; Time.unscaledTimeAsDouble = 0;
    LeAccess.Plugin.Log.Info.Clear(); LeAccess.Plugin.Log.Warnings.Clear(); LeAccess.Plugin.Log.Errors.Clear();
}
void Advance() { Time.unscaledTimeAsDouble += 2; Time.frameCount++; }

if (File.Exists(Path.Combine(AppContext.BaseDirectory, "nvdaControllerClient64.dll")))
    throw new InvalidOperationException("Native NVDA client must be absent beside this source-linked test executable.");
Check(true, "source-linked suite cannot load an NVDA native client beside its executable");
Reset(null);
Check(!ScreenReader.Initialize() && Tolk.Loads == 1, "initialization loads Tolk while no reader is running");
int probes = Tolk.Detections;
for (int i = 0; i < 20; i++) ScreenReader.IsAvailable();
Check(Tolk.Detections == probes, "availability polling within the same interval does not probe per frame");
Tolk.Name = "NVDA"; Advance();
Check(ScreenReader.IsAvailable() && ScreenReader.DetectedScreenReader == "NVDA" && Tolk.Loads == 1,
    "a reader started after initialization is detected without reloading the native library");
ScreenReader.Speak("late reader");
Check(Tolk.Messages.Any(message => message.Text == "late reader"), "late-start reader receives current output");
Tolk.Name = null; Advance();
Check(!ScreenReader.IsAvailable() && ScreenReader.DetectedScreenReader == "None", "periodic probe notices a stopped reader");
Tolk.Name = "NVDA"; Advance();
Check(ScreenReader.IsAvailable() && Tolk.Loads == 1, "restarted NVDA reconnects using the existing Tolk driver");
Tolk.Name = "JAWS"; Advance();
Check(ScreenReader.IsAvailable() && ScreenReader.DetectedScreenReader == "JAWS", "detected reader name follows backend changes");

Reset(null); ScreenReader.Initialize();
probes = Tolk.Detections;
Tolk.Name = "NVDA"; Time.unscaledTimeAsDouble = .5;
Check(!ScreenReader.IsAvailable() && Tolk.Detections == probes, "late reader waits for the bounded probe interval");
Time.unscaledTimeAsDouble = 1;
Check(ScreenReader.IsAvailable() && Tolk.Detections == probes + 1, "reader is detected exactly at the one-second probe deadline");
ScreenReader.Initialize(); ScreenReader.Initialize();
Check(Tolk.Loads == 1 && Tolk.Detections == probes + 1, "repeated initialization preserves the existing connection and deadline");

Reset(null); ScreenReader.Initialize();
int warnings = LeAccess.Plugin.Log.Warnings.Count;
Advance(); ScreenReader.IsAvailable(); Advance(); ScreenReader.IsAvailable();
Check(LeAccess.Plugin.Log.Warnings.Count == warnings, "persistent missing reader emits one status diagnostic");

Reset();
Check(ScreenReader.Initialize(), "existing reader initializes");
Check(!ScreenReader.TrySpeak(null) && !ScreenReader.TrySpeak("") && Tolk.Outputs == 0,
    "empty TrySpeak requests return false without reaching native output");
Check(ScreenReader.TrySpeak("accepted result", interrupt: false) && ScreenReader.SpokeThisFrame &&
    Tolk.Messages.Last() == ("accepted result", false), "TrySpeak reports native acceptance and preserves interrupt flag");
Tolk.OutputAccepted = false;
Check(!ScreenReader.TrySpeak("same-frame rejection") && ScreenReader.SpokeThisFrame,
    "rejection after accepted output preserves earlier frame ownership while returning false");
Tolk.OutputAccepted = true; Advance();
Tolk.Messages.Clear();
ScreenReader.Speak("normal", interrupt: true);
Check(ScreenReader.SpokeThisFrame && Tolk.Messages.Single() == ("normal", true), "accepted output claims this frame");
Time.frameCount++; Tolk.OutputAccepted = false;
ScreenReader.Speak("rejected");
Check(!ScreenReader.SpokeThisFrame, "rejected native output does not claim this speech frame");
Check(!ScreenReader.IsAvailable(), "failed output suspends availability until a new probe");
int outputs = Tolk.Outputs;
for (int i = 0; i < 20; i++) ScreenReader.Speak("rejected");
Check(Tolk.Outputs == outputs, "failed transport is not retried on every caller in the same interval");
int errors = LeAccess.Plugin.Log.Errors.Count;
Advance(); ScreenReader.Speak("rejected");
Check(LeAccess.Plugin.Log.Errors.Count == errors, "repeated native rejection produces one diagnostic");
Tolk.OutputAccepted = true; Advance(); ScreenReader.SpeakQueued("recovered");
Check(ScreenReader.SpokeThisFrame && Tolk.Messages.Last() == ("recovered", false), "queued output recovers and preserves interrupt semantics");
Time.frameCount++; ScreenReader.Speak("");
Check(!ScreenReader.SpokeThisFrame, "empty text cannot claim speech");
ScreenReader.Silence(); ScreenReader.Braille("braille");
Check(Tolk.Silences == 1 && Tolk.Brailles == 1, "existing silence and braille interfaces remain usable");
Check(!ScreenReader.IsSpeaking(), "unsupported backend status remains false");
Tolk.OutputFailure = new InvalidOperationException("native output failure");
ScreenReader.Speak("exception");
Check(!ScreenReader.SpokeThisFrame && !ScreenReader.IsAvailable(), "output exception releases availability without claiming speech");
Tolk.OutputFailure = null; Advance(); ScreenReader.Speak("after exception");
Check(ScreenReader.SpokeThisFrame, "transport recovers after an output exception");
Time.frameCount++; Tolk.OutputFailure = new InvalidOperationException("TrySpeak failure");
Check(!ScreenReader.TrySpeak("throw result") && !ScreenReader.SpokeThisFrame,
    "TrySpeak catches output exceptions and returns false without claiming the frame");
outputs = Tolk.Outputs;
Check(!ScreenReader.TrySpeak("unavailable result") && Tolk.Outputs == outputs,
    "TrySpeak returns false while the unavailable transport waits for a probe");
Tolk.OutputFailure = null; Advance();
Check(ScreenReader.TrySpeak("recovered result"), "TrySpeak resumes acceptance after a successful reconnect probe");
ScreenReader.Shutdown();
Check(!ScreenReader.IsAvailable() && ScreenReader.DetectedScreenReader == "None" && !ScreenReader.SpokeThisFrame,
    "shutdown clears detection and frame ownership");
probes = Tolk.Detections; Advance(); ScreenReader.IsAvailable(); ScreenReader.Speak("after shutdown");
Check(!ScreenReader.TrySpeak("after shutdown result"), "TrySpeak does not acknowledge output after shutdown");
Check(Tolk.Detections == probes && Tolk.Unloads == 1, "shutdown prevents implicit reconnection");
ScreenReader.Shutdown(); Check(Tolk.Unloads == 1, "repeated shutdown does not unload twice");
Check(ScreenReader.Initialize() && Tolk.Loads == 2, "explicit initialization after shutdown reconnects");

Reset();
Tolk.LoadFailure = new DllNotFoundException("missing native library");
Check(!ScreenReader.Initialize(), "native load failure remains unavailable");
for (int i = 0; i < 20; i++) ScreenReader.IsAvailable();
Check(Tolk.Loads == 1, "load failures are not retried on every availability query");
Advance(); ScreenReader.IsAvailable();
Check(Tolk.Loads == 2 && LeAccess.Plugin.Log.Errors.Count == 1, "load failure retries are paced and diagnostics deduplicated");
Tolk.LoadFailure = null; Advance();
Check(ScreenReader.IsAvailable(), "a later availability probe recovers a failed initialization");

Reset(); Tolk.DetectionFailure = new InvalidOperationException("detection failed");
Check(!ScreenReader.Initialize(), "detection exception remains unavailable");
Tolk.DetectionFailure = null; Advance();
Check(ScreenReader.IsAvailable() && Tolk.Loads == 1, "detection failure retries without reloading a successfully initialized Tolk");
// Pickup lines cut each other and give way to life, mana and combat, but never cut anything else.
Reset(); ScreenReader.Initialize();
bool Last(string text, bool interrupt) => Tolk.Messages.Count > 0 && Tolk.Messages[^1] == (text, interrupt);
ScreenReader.TrySpeakPickup("25 gold");
Check(Last("25 gold", false), "a first pickup line queues");
ScreenReader.TrySpeakPickup("30 gold");
Check(Last("30 gold", true), "the next pickup line cuts the previous one");
ScreenReader.TrySpeakOverPickup("Life 50", false);
Check(Last("Life 50", true), "life cuts a pickup line");
ScreenReader.TrySpeakPickup("Potions 1 of 3");
Check(Last("Potions 1 of 3", false), "a pickup line never cuts life");
ScreenReader.TrySpeak("Quest updated", false);
ScreenReader.TrySpeakPickup("Mace");
Check(Last("Mace", false), "a pickup line never cuts other speech");
ScreenReader.TrySpeakOverPickup("Boss 40", true);
Check(Last("Boss 40", true), "an interrupting line stays interrupting");
ScreenReader.TrySpeakPickup("Shard");
Time.unscaledTimeAsDouble += 5;
ScreenReader.TrySpeakOverPickup("Mana 30", false);
Check(Last("Mana 30", false), "a pickup line heard long ago is not cut");
// Life first (2026-10-11, user request): a life line cuts anything, and other speech queues behind it for a moment.
ScreenReader.TrySpeak("Quest updated", false);
ScreenReader.TrySpeakLife("Life 40");
Check(Last("Life 40", true), "life cuts any speech");
ScreenReader.TrySpeak("Menu item", true);
Check(Last("Menu item", false), "an interrupting line queues behind life");
ScreenReader.TrySpeakOverPickup("Boss 30", true);
Check(Last("Boss 30", false), "combat lines queue behind life");
ScreenReader.TrySpeakLife("Life 30");
Check(Last("Life 30", true), "a newer life line cuts the older one");
Time.unscaledTimeAsDouble += 3;
ScreenReader.TrySpeak("Menu item", true);
Check(Last("Menu item", true), "once heard, life holds nothing back");
ScreenReader.MenuOwnsSpeech = true;
int count = Tolk.Messages.Count;
Check(ScreenReader.TrySpeakPickup("Gold") && Tolk.Messages.Count == count, "pickup lines are held back by the menu");
Check(ScreenReader.TrySpeakLife("Life 20") && Tolk.Messages.Count == count, "life is held back by the menu too");
ScreenReader.MenuOwnsSpeech = false;
ScreenReader.SpeakMenu("Volume", true);
Check(Last("Volume", true), "a life line held back by the menu holds nothing back after it");
ScreenReader.MenuOwnsSpeech = false;
ScreenReader.Shutdown();
if (failures.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
Console.WriteLine($"Screen-reader lifecycle tests passed: {checks} checks.");
