using BepInEx.Configuration;
using LeAccess;
using LeAccess.Accessibility;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.Vitals;
using LeAccess.I18n;
using UnityEngine;

int checks = 0;
void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
ConfigFile config = null;
void Start()
{
    VitalsAccessibility.Cleanup();
    ModText.Initialize(null,null);
    config = new ConfigFile();
    Application.isFocused = ScreenReader.Available = VitalsProbe.Available = GameplayInputState.Gameplay = true;
    ScreenReader.OutputAccepted = true;
    LE.UI.LoadingScreen.IsVisible = NavigationAudioState.IsSuspended = VitalsProbe.Throw = false;
    EpochInputManager.instance.isInputFieldActive = ScreenKeyboardManager.Instance._enabled = false;
    VitalsProbe.Reads = VitalsInput.Calls = 0;
    VitalsProbe.Snapshot = new(1,100,100,100,100,false);
    ScreenReader.Spoken.Clear(); Plugin.Log.Information.Clear(); Plugin.Log.Warnings.Clear();
    Time.unscaledTimeAsDouble = 0;
    VitalsAccessibility.Initialize(config);
}
void Tick(double time, bool keyboard = false, bool controller = false, bool ui = false, bool alreadySpoke = false)
{
    Time.unscaledTimeAsDouble = time;
    VitalsInput.KeyboardPressed = keyboard; VitalsInput.ControllerPressed = controller;
    ScreenReader.SpokeThisFrame = alreadySpoke;
    VitalsAccessibility.Update(ui);
    // Unity/Rewired input exposes only this frame's edge, not a deferred request.
    VitalsInput.Reset();
}
void Values(double health, double mana, long session = 1, bool replaced = false) =>
    VitalsProbe.Snapshot = new(session,health,100,mana,100,replaced);
void Last(string text, bool interrupt, string name)
{
    Check(ScreenReader.Spoken.Count > 0 && ScreenReader.Spoken.Last() == (text,interrupt), name);
}
try
{
    Start();
    Check(config.Get<bool>("AutomaticAnnouncements") && config.Get<bool>("ReportWithControllerView") &&
        config.Get<KeyCode>("ReportKey") == KeyCode.R, "D2R default automatic and manual controls");
    Tick(0);
    Check(VitalsProbe.Reads == 1 && ScreenReader.Spoken.Count == 0, "silent first baseline");
    Values(74,74);
    Tick(.099);
    Check(VitalsProbe.Reads == 1 && ScreenReader.Spoken.Count == 0, "100ms throttle");
    Tick(.1);
    Check(VitalsProbe.Reads == 2 && ScreenReader.Spoken.Count == 1, "one combined crossing");
    Last("Life 74 percent. Mana 74 percent",false,"health first ordinary queue");
    Tick(.2);
    Check(ScreenReader.Spoken.Count == 1, "same values silent");
    Values(9,9);
    Tick(.31,ui:true,alreadySpoke:true);
    Last("Life 9 percent. Mana 9 percent",true,"critical interrupts despite UI and same frame speech");
    Check(ScreenReader.Spoken.Count == 2, "combined critical exactly once");
    Values(20,20);
    Tick(.42,ui:true,alreadySpoke:true);
    Last("Life 20 percent. Mana 20 percent",false,"recovery queues despite UI");
    Values(19,29);
    Tick(.43,keyboard:true);
    Last("Life 19 percent. Mana 29 percent",true,"manual bypass throttle and preserves exact integer percent");
    Check(VitalsProbe.Reads == 6 && ScreenReader.Spoken.Count == 4,"manual crossing not duplicated automatically");
    Tick(.44);
    Check(ScreenReader.Spoken.Count == 4,"manual updates next poll time");
    Check(Plugin.Log.Information.Count == ScreenReader.Spoken.Count &&
        Plugin.Log.Information.All(line => line.StartsWith("[Vitals] ")),"only actual deliveries logged");
    config.Set("AutomaticAnnouncements",false);
    Values(4,4);
    Tick(.7);
    Check(ScreenReader.Spoken.Count == 4,"automatic disabled silent");
    Tick(.71,controller:true);
    Last("Life 4 percent. Mana 4 percent",true,"manual works automatic off");
    config.Set("ReportWithControllerView",false);
    Tick(.72,controller:true);
    Check(ScreenReader.Spoken.Count == 5,"controller shortcut disabled");
    config.Set("ReportKey",KeyCode.None);
    Tick(.73,keyboard:true);
    Check(ScreenReader.Spoken.Count == 5,"keyboard shortcut disabled");

    Start(); Tick(0);
    Values(4,4,2); Tick(.1);
    Check(ScreenReader.Spoken.Count == 0,"new session establishes baseline");
    Values(3,3,2); Tick(.21);
    Last("Life 3 percent. Mana 3 percent",true,"new session subsequent loss");
    VitalsProbe.Available = false; Tick(.32);
    Values(1,1,2); VitalsProbe.Available = true; Tick(.43);
    Check(ScreenReader.Spoken.Count == 1,"missing probe resets baseline");
    VitalsProbe.Available = false; Tick(.44,keyboard:true);
    Last("Life and mana unavailable",true,"manual missing probe feedback");
    VitalsProbe.Available = true;

    foreach (string gate in new[] {"focus","loading"})
    {
        Start(); Tick(0); Values(4,4);
        Application.isFocused = gate != "focus";
        LE.UI.LoadingScreen.IsVisible = gate == "loading";
        ScreenReader.Available = gate != "reader";
        Tick(.1,keyboard:true);
        Check(ScreenReader.Spoken.Count == 0 && VitalsProbe.Reads == 1,"blocked update " + gate);
        Application.isFocused = ScreenReader.Available = true; LE.UI.LoadingScreen.IsVisible = false;
        Tick(.11);
        Check(ScreenReader.Spoken.Count == 0 && VitalsProbe.Reads == 2,"reset baseline after " + gate);
    }

    Start(); Tick(0);
    Values(double.NaN,50); Tick(.1,keyboard:true);
    Last("Life unavailable. Mana 50 percent",true,"partial mana report");
    Values(50,double.NaN); Tick(.11,keyboard:true);
    Last("Life 50 percent. Mana unavailable",true,"partial life report");
    Values(50,double.NaN,replaced:true); Tick(.12,keyboard:true);
    Last("Life 50 percent. Mana replaced by rage",true,"rage report");
    Values(50,-10); Tick(.13,keyboard:true);
    Last("Life 50 percent. Mana 0 percent",true,"mana debt reported zero");
    ModText.SetLanguage("es"); Tick(.14,keyboard:true);
    Last("Vida 50 por ciento. Maná 0 por ciento",true,"real Spanish localization");
    Start(); Tick(0);
    Values(double.NaN,74); Tick(.1);
    Last("Mana 74 percent",false,"automatic partial mana independently announces");
    Values(74,double.NaN); Tick(.21);
    Last("Life 74 percent",false,"automatic missing health preserves earlier baseline");
    Values(74,double.NaN,replaced:true); Tick(.32);
    Check(ScreenReader.Spoken.Count == 2,"automatic rage does not invent a mana percentage");
    Values(9,double.NaN,replaced:true); Tick(.43);
    Last("Life 9 percent",true,"critical health while rage replaces mana");
    Values(9,4); Tick(.54);
    Last("Mana 4 percent",true,"resource resumes last known mana baseline");

    foreach (string typing in new[] {"field","keyboard"})
    {
        Start(); Tick(0);
        EpochInputManager.instance.isInputFieldActive = typing == "field";
        ScreenKeyboardManager.Instance._enabled = typing == "keyboard";
        Tick(.01,keyboard:true,controller:true);
        Check(ScreenReader.Spoken.Count == 0,"typing blocks manual " + typing);
        EpochInputManager.instance.isInputFieldActive = ScreenKeyboardManager.Instance._enabled = false;
        Tick(.02);
        Check(ScreenReader.Spoken.Count == 0,"typing press never deferred " + typing);
    }

    Start(); Tick(0);
    GameplayInputState.Gameplay = false; NavigationAudioState.IsSuspended = true;
    Tick(.01,controller:true,ui:true);
    Last("Life 100 percent. Mana 100 percent",true,"native menu edge after gameplay accepted");
    Tick(.02,controller:true,ui:true);
    Check(ScreenReader.Spoken.Count == 1,"later menu press rejected");
    GameplayInputState.Gameplay = true; NavigationAudioState.IsSuspended = false;
    Tick(.03);
    Check(ScreenReader.Spoken.Count == 1,"menu press never deferred");
    Start(); Tick(0);
    GameplayInputState.Gameplay = false; NavigationAudioState.IsSuspended = true;
    Tick(.01,keyboard:true,ui:true);
    Check(ScreenReader.Spoken.Count == 0,"keyboard cannot use previous-gameplay menu grace");
    Start(); GameplayInputState.Gameplay = false;
    Tick(0,keyboard:true,controller:true,ui:true);
    Check(ScreenReader.Spoken.Count == 0,"cold menu does not allow manual report");

    Start(); Tick(0);
    VitalsProbe.Throw = true; Tick(.1); Tick(.2);
    Check(Plugin.Log.Warnings.Count == 1,"same probe failure logged once");
    VitalsProbe.Throw = false; Values(5,5); Tick(.3);
    Check(ScreenReader.Spoken.Count == 0,"exception resets baseline");
    Start(); Tick(0); Values(9,9); ScreenReader.OutputAccepted = false; Tick(.1);
    Check(ScreenReader.Spoken.Count == 0 && Plugin.Log.Information.Count == 0, "rejected vitals output is not logged as delivery");
    ScreenReader.OutputAccepted = true; Tick(.15);
    Check(VitalsProbe.Reads == 2 && ScreenReader.Spoken.Count == 0,"rejected automatic output retains the100ms sample interval without replaying between polls");
    Values(8,8); ScreenReader.OutputAccepted = true; Tick(.21);
    Last("Life 8 percent. Mana 8 percent",true,"recovered critical loss reads fresh percentages with critical priority");
    Tick(.32); Check(ScreenReader.Spoken.Count == 1,"accepted recovered band is acknowledged once");
    Start(); Tick(0); Values(9,9); ScreenReader.OutputAccepted = false; Tick(.1);
    Values(99,99); ScreenReader.OutputAccepted = true; Tick(.21);
    Check(ScreenReader.Spoken.Count == 0,"recovery to the acknowledged band discards old rejected critical values");
    Start(); Tick(0); Values(9,9); ScreenReader.OutputAccepted = false; Tick(.1);
    Values(74,74); ScreenReader.OutputAccepted = true; Tick(.21);
    Last("Life 74 percent. Mana 74 percent",false,"current ordinary band replaces rejected critical reading without stale interruption");
    Start(); Tick(0); Values(9,9); ScreenReader.OutputAccepted = false; Tick(.1);
    Values(double.NaN,8); ScreenReader.OutputAccepted = true; Tick(.21);
    Last("Mana 8 percent",true,"recovery excludes a currently unavailable resource from rejected combined values");
    Values(100,8); Tick(.32);
    Check(ScreenReader.Spoken.Count == 1,"restored health in original band cannot replay rejected health history");
    Start(); Tick(0); Values(9,9); ScreenReader.OutputAccepted = false; Tick(.1,keyboard:true);
    ScreenReader.OutputAccepted = true; Tick(.21);
    Last("Life 9 percent. Mana 9 percent",true,"rejected manual report does not consume a current automatic crossing");
    Start(); config.Set("AutomaticAnnouncements",false); Values(9,9); ScreenReader.OutputAccepted = false; Tick(0,keyboard:true);
    Values(8,8); ScreenReader.OutputAccepted = true; Tick(.11);
    Check(ScreenReader.Spoken.Count == 0,"rejected manual-only input is never deferred into a later frame");
    Tick(.12,keyboard:true);
    Last("Life 8 percent. Mana 8 percent",true,"a later explicit manual edge retries fresh current values");
    Start(); Tick(0); Values(9,9); ScreenReader.Available = false; Tick(.1);
    Check(VitalsProbe.Reads == 2 && ScreenReader.Spoken.Count == 0,"reader outage keeps live source validation and sampling active");
    Values(8,8); ScreenReader.Available = true; Tick(.21);
    Last("Life 8 percent. Mana 8 percent",true,"reader reconnect preserves acknowledged baseline and reads fresh critical loss");
    Start(); Tick(0); Values(9,9); ScreenReader.OutputAccepted = false; Tick(.1);
    VitalsProbe.Available = false; Tick(.21); VitalsProbe.Available = true;
    Values(8,8); ScreenReader.OutputAccepted = true; Tick(.32);
    Check(ScreenReader.Spoken.Count == 0,"invalid player discards rejected crossings before recovery");
    Values(4,4); Tick(.43);
    Last("Life 4 percent. Mana 4 percent",true,"revalidated player establishes baseline before a new critical crossing");
    Start(); Tick(0); Values(9,9); ScreenReader.Available = false; Tick(.1);
    Values(8,8,2); Tick(.21); ScreenReader.Available = true; Tick(.32);
    Check(ScreenReader.Spoken.Count == 0,"new session during reader outage discards previous player crossings");
    Start(); Tick(0); ScreenReader.Available = false; Values(98,98); Tick(.1);
    ScreenReader.Available = true; Values(97,97); Tick(.21);
    Check(ScreenReader.Spoken.Count == 0,"reader recovery without a crossed band cannot fabricate a full report");
    Start(); ScreenReader.Available = false; Values(9,9); Tick(0);
    ScreenReader.Available = true; Values(8,8); Tick(.11);
    Check(ScreenReader.Spoken.Count == 0,"first live observation during reader outage remains a true silent baseline");
    Start(); Tick(0); ScreenReader.Available = false; Values(9,9); Tick(.1);
    Values(100,100); Tick(.21); ScreenReader.Available = true; Tick(.32);
    Check(ScreenReader.Spoken.Count == 0,"full recovery before reconnect discards earlier critical values without replay");
    void Potions(double health, int potions, int max = 4, long session = 1) =>
        VitalsProbe.Snapshot = new(session,health,100,100,100,false,potions,max);
    Start(); Check(config.Get<bool>("AnnouncePotionCharges"),"potion charges announced by default");
    Potions(100,3); Tick(0);
    Check(ScreenReader.Spoken.Count == 0,"first potion count is a silent baseline");
    Potions(100,2); Tick(.1);
    Last("Potions 2 of 4",false,"used potion queues remaining charges");
    Tick(.2); Check(ScreenReader.Spoken.Count == 1,"unchanged charges stay silent");
    Potions(74,3); Tick(.31);
    Last("Life 74 percent. Potions 3 of 4",false,"vitals crossing and picked-up potion share one utterance");
    Tick(.42,keyboard:true);
    Last("Life 74 percent. Mana 100 percent. Potions 3 of 4",true,"report includes potion charges");
    Potions(74,0,-1); Tick(.53);
    Last("Potions 0",false,"unknown maximum reads the count alone");
    ScreenReader.OutputAccepted = false; Potions(74,1); Tick(.64);
    ScreenReader.OutputAccepted = true; Tick(.75);
    Last("Potions 1 of 4",false,"rejected potion count retries with current charges");
    Check(ScreenReader.Spoken.Count == 5,"each potion change speaks once");
    Potions(74,2,4,2); Tick(.86);
    Check(ScreenReader.Spoken.Count == 5,"new session re-baselines potion charges");
    Potions(74,-1,-1,2); Tick(.97); Potions(74,2,4,2); Tick(1.08);
    Check(ScreenReader.Spoken.Count == 5,"unavailable charges keep the baseline");
    config.Set("AnnouncePotionCharges",false); Potions(74,1,4,2); Tick(1.19);
    Check(ScreenReader.Spoken.Count == 5,"disabled potion announcements stay silent");
    config.Set("AnnouncePotionCharges",true); Tick(1.3);
    Check(ScreenReader.Spoken.Count == 5,"re-enabling does not replay a change made while disabled");
    Tick(1.41,keyboard:true);
    Last("Life 74 percent. Mana 100 percent. Potions 1 of 4",true,"report reads potions while automatic potion speech is off");
    Start(); Tick(0);
    VitalsEffectsProbe.Effects.AddRange(new[] { "Poison 3", "Chill" });
    Tick(.1); Check(ScreenReader.Spoken.Count == 0,"harmful effects are never announced automatically");
    Tick(.2,keyboard:true);
    Last("Life 100 percent. Mana 100 percent. Effects: Poison 3, Chill",true,"report lists harmful effects last");
    VitalsEffectsProbe.Effects.Clear(); Tick(.3,keyboard:true);
    Last("Life 100 percent. Mana 100 percent",true,"report omits effects when none are active");
    ModText.SetLanguage("es"); VitalsEffectsProbe.Effects.Add("Veneno"); Tick(.4,keyboard:true);
    Last("Vida 100 por ciento. Maná 100 por ciento. Efectos: Veneno",true,"Spanish effects label");
    ModText.SetLanguage("en"); VitalsEffectsProbe.Effects.Clear();
    Start();
    VitalsAccessibility.Cleanup(); int reads = VitalsProbe.Reads;
    Tick(.4,keyboard:true,controller:true);
    Check(VitalsProbe.Reads == reads && ScreenReader.Spoken.Count == 0,"cleanup disables update and input");
    Check(!VitalsInput.KeyboardPressed && !VitalsInput.ControllerPressed,"cleanup clears edge state");
    Console.WriteLine($"Passed {checks} vitals service checks.");
}
finally { VitalsAccessibility.Cleanup(); }
