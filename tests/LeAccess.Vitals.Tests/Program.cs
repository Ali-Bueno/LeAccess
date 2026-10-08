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
string T(string key, params object[] args) => ModText.Format(key, args);
string Join(params string[] fragments) => string.Join(". ", fragments);
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
    Last(Join(T("vitals.health", 74), T("vitals.mana", 74)),false,"health first ordinary queue");
    Tick(.2);
    Check(ScreenReader.Spoken.Count == 1, "same values silent");
    Values(9,9);
    Tick(.31,ui:true,alreadySpoke:true);
    Last(Join(T("vitals.health", 9), T("vitals.mana", 9)),true,"critical interrupts despite UI and same frame speech");
    Check(ScreenReader.Spoken.Count == 2, "combined critical exactly once");
    Values(20,20);
    Tick(.42,ui:true,alreadySpoke:true);
    Last(Join(T("vitals.health", 20), T("vitals.mana", 20)),false,"recovery queues despite UI");
    Values(19,29);
    Tick(.43,keyboard:true);
    Last(Join(T("vitals.health", 19), T("vitals.mana", 29)),true,"manual bypass throttle and preserves exact integer percent");
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
    Last(Join(T("vitals.health", 4), T("vitals.mana", 4)),true,"manual works automatic off");
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
    Last(T("vitals.health", 3),true,"new session subsequent loss; routine low mana step quiet");
    VitalsProbe.Available = false; Tick(.32);
    Values(1,1,2); VitalsProbe.Available = true; Tick(.43);
    Check(ScreenReader.Spoken.Count == 1,"missing probe resets baseline");
    VitalsProbe.Available = false; Tick(.44,keyboard:true);
    Last(T("vitals.unavailable"),true,"manual missing probe feedback");
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
    Last(Join(T("vitals.health_unavailable"), T("vitals.mana", 50)),true,"partial mana report");
    Values(50,double.NaN); Tick(.11,keyboard:true);
    Last(Join(T("vitals.health", 50), T("vitals.mana_unavailable")),true,"partial life report");
    Values(50,double.NaN,replaced:true); Tick(.12,keyboard:true);
    Last(Join(T("vitals.health", 50), T("vitals.mana_replaced")),true,"rage report");
    Values(50,-10); Tick(.13,keyboard:true);
    Last(Join(T("vitals.health", 50), T("vitals.mana", 0)),true,"mana debt reported zero");
    ModText.SetLanguage("es"); Tick(.14,keyboard:true);
    Last(Join(T("vitals.health", 50), T("vitals.mana", 0)),true,"real Spanish localization");
    Start(); Tick(0);
    Values(double.NaN,74); Tick(.1);
    Last(T("vitals.mana", 74),false,"automatic partial mana independently announces");
    Values(74,double.NaN); Tick(.21);
    Last(T("vitals.health", 74),false,"automatic missing health preserves earlier baseline");
    Values(74,double.NaN,replaced:true); Tick(.32);
    Check(ScreenReader.Spoken.Count == 2,"automatic rage does not invent a mana percentage");
    Values(9,double.NaN,replaced:true); Tick(.43);
    Last(T("vitals.health", 9),true,"critical health while rage replaces mana");
    Values(9,4); Tick(.54);
    Last(T("vitals.mana", 4),false,"resource resumes last known mana baseline; mana never interrupts");

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
    Last(Join(T("vitals.health", 100), T("vitals.mana", 100)),true,"native menu edge after gameplay accepted");
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
    Last(Join(T("vitals.health", 8), T("vitals.mana", 8)),true,"recovered critical loss reads fresh percentages with critical priority");
    Tick(.32); Check(ScreenReader.Spoken.Count == 1,"accepted recovered band is acknowledged once");
    Start(); Tick(0); Values(9,9); ScreenReader.OutputAccepted = false; Tick(.1);
    Values(99,99); ScreenReader.OutputAccepted = true; Tick(.21);
    Check(ScreenReader.Spoken.Count == 0,"recovery to the acknowledged band discards old rejected critical values");
    Start(); Tick(0); Values(9,9); ScreenReader.OutputAccepted = false; Tick(.1);
    Values(74,74); ScreenReader.OutputAccepted = true; Tick(.21);
    Last(Join(T("vitals.health", 74), T("vitals.mana", 74)),false,"current ordinary band replaces rejected critical reading without stale interruption");
    Start(); Tick(0); Values(9,9); ScreenReader.OutputAccepted = false; Tick(.1);
    Values(double.NaN,8); ScreenReader.OutputAccepted = true; Tick(.21);
    Last(T("vitals.mana", 8),false,"recovery excludes a currently unavailable resource from rejected combined values");
    Values(100,8); Tick(.32);
    Check(ScreenReader.Spoken.Count == 1,"restored health in original band cannot replay rejected health history");
    Start(); Tick(0); Values(9,9); ScreenReader.OutputAccepted = false; Tick(.1,keyboard:true);
    ScreenReader.OutputAccepted = true; Tick(.21);
    Last(Join(T("vitals.health", 9), T("vitals.mana", 9)),true,"rejected manual report does not consume a current automatic crossing");
    Start(); config.Set("AutomaticAnnouncements",false); Values(9,9); ScreenReader.OutputAccepted = false; Tick(0,keyboard:true);
    Values(8,8); ScreenReader.OutputAccepted = true; Tick(.11);
    Check(ScreenReader.Spoken.Count == 0,"rejected manual-only input is never deferred into a later frame");
    Tick(.12,keyboard:true);
    Last(Join(T("vitals.health", 8), T("vitals.mana", 8)),true,"a later explicit manual edge retries fresh current values");
    Start(); Tick(0); Values(9,9); ScreenReader.Available = false; Tick(.1);
    Check(VitalsProbe.Reads == 2 && ScreenReader.Spoken.Count == 0,"reader outage keeps live source validation and sampling active");
    Values(8,8); ScreenReader.Available = true; Tick(.21);
    Last(Join(T("vitals.health", 8), T("vitals.mana", 8)),true,"reader reconnect preserves acknowledged baseline and reads fresh critical loss");
    Start(); Tick(0); Values(9,9); ScreenReader.OutputAccepted = false; Tick(.1);
    VitalsProbe.Available = false; Tick(.21); VitalsProbe.Available = true;
    Values(8,8); ScreenReader.OutputAccepted = true; Tick(.32);
    Check(ScreenReader.Spoken.Count == 0,"invalid player discards rejected crossings before recovery");
    Values(4,4); Tick(.43);
    Last(T("vitals.health", 4),true,"revalidated player establishes baseline before a new critical crossing; routine low mana quiet");
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
    Last(T("vitals.potions", 2, 4),false,"used potion queues remaining charges");
    Tick(.2); Check(ScreenReader.Spoken.Count == 1,"unchanged charges stay silent");
    Potions(74,3); Tick(.31);
    Last(Join(T("vitals.health", 74), T("vitals.potions", 3, 4)),false,"vitals crossing and picked-up potion share one utterance");
    Tick(.42,keyboard:true);
    Last(Join(T("vitals.health", 74), T("vitals.mana", 100), T("vitals.potions", 3, 4)),true,"report includes potion charges");
    Potions(74,0,-1); Tick(.53);
    Last(T("vitals.potions_count", 0),false,"unknown maximum reads the count alone");
    ScreenReader.OutputAccepted = false; Potions(74,1); Tick(.64);
    ScreenReader.OutputAccepted = true; Tick(.75);
    Last(T("vitals.potions", 1, 4),false,"rejected potion count retries with current charges");
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
    Last(Join(T("vitals.health", 74), T("vitals.mana", 100), T("vitals.potions", 1, 4)),true,"report reads potions while automatic potion speech is off");
    Start(); Tick(0);
    VitalsEffectsProbe.Effects.AddRange(new[] { "Poison 3", "Chill" });
    Tick(.1); Check(ScreenReader.Spoken.Count == 0,"harmful effects are never announced automatically");
    Tick(.2,keyboard:true);
    Last(Join(T("vitals.health", 100), T("vitals.mana", 100), T("vitals.effects", "Poison 3, Chill")),true,"report lists harmful effects last");
    VitalsEffectsProbe.Effects.Clear(); Tick(.3,keyboard:true);
    Last(Join(T("vitals.health", 100), T("vitals.mana", 100)),true,"report omits effects when none are active");
    ModText.SetLanguage("es"); VitalsEffectsProbe.Effects.Add("Veneno"); Tick(.4,keyboard:true);
    Last(Join(T("vitals.health", 100), T("vitals.mana", 100), T("vitals.effects", "Veneno")),true,"Spanish effects label");
    ModText.SetLanguage("en"); VitalsEffectsProbe.Effects.Clear();
    // Gold: Create+Triangle or Ctrl+G reads it at once, on its own.
    Start(); Tick(0);
    VitalsInput.Gold = true; Tick(.05);
    Last(T("gold.amount", 1234), true, "The gold command reads the character's gold, interrupting");
    Check(ScreenReader.Spoken.Count == 1, "Gold is spoken alone, without the vitals report");
    GoldReport.Amount = null; VitalsInput.Gold = true; Tick(.06);
    Last(ModText.Get("gold.unavailable"), true, "Missing gold data says so instead of staying silent");
    GoldReport.Amount = 1234;
    EpochInputManager.instance.isInputFieldActive = true; VitalsInput.Gold = true; int spokenGold = ScreenReader.Spoken.Count; Tick(.07);
    Check(ScreenReader.Spoken.Count == spokenGold, "Typing never reads gold");
    EpochInputManager.instance.isInputFieldActive = false;
    Start();
    VitalsAccessibility.Cleanup(); int reads = VitalsProbe.Reads;
    Tick(.4,keyboard:true,controller:true);
    Check(VitalsProbe.Reads == reads && ScreenReader.Spoken.Count == 0,"cleanup disables update and input");
    Check(!VitalsInput.KeyboardPressed && !VitalsInput.ControllerPressed,"cleanup clears edge state");
    // Gold pickups: the counter's rises, after a silent baseline, never at a vendor.
    var pickups = new GoldPickupState();
    Check(pickups.Observe(1, 100, false) == 0, "The first gold reading is a silent baseline");
    Check(pickups.Observe(1, 100, false) == 0, "Unchanged gold says nothing");
    Check(pickups.Observe(1, 103, false) == 3, "A pickup of 3 gold is reported as 3");
    pickups.Acknowledge();
    Check(pickups.Observe(1, 153, false) == 50, "Each pickup is its own amount");
    Check(pickups.Observe(1, 233, false) == 130, "A gain not yet spoken adds up with the next one");
    pickups.Acknowledge();
    Check(pickups.Observe(1, 200, false) == 0, "Spending or dropping gold is not a find");
    Check(pickups.Observe(1, 280, false) == 80, "A gain after spending counts from the lower amount");
    pickups.Acknowledge();
    Check(pickups.Observe(1, 500, true) == 0 && pickups.Observe(1, 500, false) == 0, "Selling at a vendor stays silent here");
    Check(pickups.Observe(1, 510, true) == 0 && pickups.Pending == 0, "A vendor also clears a gain still pending");
    Check(pickups.Observe(2, 9000, false) == 0, "Another character's counter is a new baseline, not a gain");
    Check(pickups.Observe(2, null, false) == 0 && pickups.Observe(2, 9100, false) == 0,
        "An unreadable counter resets the baseline");
    Check(pickups.Observe(2, 9150, false) == 50, "Pickups resume after the new baseline");
    Console.WriteLine($"Passed {checks} vitals service checks.");
}
finally { VitalsAccessibility.Cleanup(); }
