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
ScreenReader.Shutdown();
Check(!ScreenReader.IsAvailable() && ScreenReader.DetectedScreenReader == "None" && !ScreenReader.SpokeThisFrame,
    "shutdown clears detection and frame ownership");
probes = Tolk.Detections; Advance(); ScreenReader.IsAvailable(); ScreenReader.Speak("after shutdown");
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
ScreenReader.Shutdown();
if (failures.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
Console.WriteLine($"Screen-reader lifecycle tests passed: {checks} checks.");
