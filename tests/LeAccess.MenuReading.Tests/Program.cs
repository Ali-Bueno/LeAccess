using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.Accessibility.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

int checks = 0;
void Check(bool condition, string message)
{ checks++; if (!condition) throw new InvalidOperationException(message); }
void Reset()
{
    ElementAnnouncer.ResetFocus(); ButtonPromptAnnouncer.Reset();
    SettingsFocusAnnouncer.Reset(); SettingsFocusReader.Current = null; SettingsFocusReader.Details = null;
    ScreenReader.Messages.Clear(); ScreenReader.Speaking = ScreenReader.SpokeThisFrame = false;
    ScreenContentAnnouncer.HasActiveContent = ScreenContentAnnouncer.HasPendingContent = false;
    DialogueAnnouncer.HasActiveContent = DialogueAnnouncer.HasPendingContent = false;
    DialogueAnnouncer.Root = null; DialogueAnnouncer.Continue = null; DialogueAnnouncer.LineToFollow = false;
    ScreenContentAnnouncer.Root = null; LE.UI.LoadingScreen.IsVisible = false;
    Application.isFocused = true; Time.unscaledTime += 1;
    NavigationManager.currentlySelected = null; PlayerFinder.Player = null; GameplayInputState.Owns = true;
}
TMP_Text Label(GameObject root, string text) => root.Child("Label").Add(new TextMeshProUGUI { text = text });
GameObject Button(GameObject root, string text)
{ var button = root.Child("Button"); button.Add(new Button()); Label(button,text); return button; }
void TickPrompts()
{ ScreenReader.SpokeThisFrame = false; Time.frameCount++; ButtonPromptAnnouncer.Update(); }
MultiButtonsActionPrompt Prompt(GameObject root, string text)
{
    var go = root.Child("Prompt");
    return go.Add(new MultiButtonsActionPrompt { _promptDescription = Label(go,text) });
}

Reset();
var merchantTab = new GameObject { name = "Buyback" };
LeAccess.Accessibility.Items.MerchantMenuReader.OwnedTab = merchantTab;
ElementAnnouncer.AnnounceElement(merchantTab);
Check(ScreenReader.Messages.Single().Text == LeAccess.Accessibility.Items.MerchantMenuReader.BuybackText, "Owned merchant tab overrides generic object-name fallback");
LeAccess.Accessibility.Items.MerchantMenuReader.OwnedTab = null;
Reset();
var menu = new GameObject();
var play = Button(menu,"Jugar"); var options = Button(menu,"Opciones");
ScreenReader.Speaking = true;
ElementAnnouncer.AnnounceElement(play); ElementAnnouncer.AnnounceElement(options); ElementAnnouncer.AnnounceElement(play);
Check(ScreenReader.Messages.Select(x => x.Text).SequenceEqual(new[] {"Jugar","Opciones","Jugar"}), "Every actual move, including an immediate return, is read in order");
Check(ScreenReader.Messages.All(x => x.Interrupt), "A new focused option interrupts even when the backend reports busy");
ElementAnnouncer.AnnounceElement(play); Time.unscaledTime += 1; ElementAnnouncer.AnnounceElement(play);
Check(ScreenReader.Messages.Count == 3, "Polling and cursor hooks do not repeat stable focus");
var initializedInBackground = new GameObject { name = "2021 Skill Tree" };
ElementAnnouncer.ResetFocusIfOwned(initializedInBackground); ElementAnnouncer.AnnounceElement(play);
Check(ScreenReader.Messages.Count == 3, "Opening an unrelated internal panel cannot reset foreground speech");
menu.Active = false; ElementAnnouncer.ResetFocusIfOwned(menu); menu.Active = true;
ElementAnnouncer.AnnounceElement(play);
Check(ScreenReader.Messages.Count == 4, "Closing and reopening the owning menu renews initial focus");

Reset();
var description = string.Join(" ", Enumerable.Repeat("La descripción completa permanece disponible.", 25));
var described = Button(new GameObject(),"Sin conexión");
described.Add(new InfoForTooltip { tooltipInfo = new TooltipInfo { nameText = "Sin conexión", mainText = description } });
ElementAnnouncer.AnnounceElement(described);
Check(ScreenReader.Messages.Single().Text == "Sin conexión", "Navigation keeps tooltip descriptions separate");
Check(ElementAnnouncer.TryReadDetails(described, out var manualText) && manualText == "Sin conexión. " + description,
    "Manual inspection retains the complete associated description");
Time.unscaledTime += 1; ElementAnnouncer.AnnounceElement(described);
Check(ScreenReader.Messages.Count == 1, "Manual inspection acknowledges brief focus without a following duplicate");
described.Active = false;
Check(!ElementAnnouncer.TryReadDetails(described, out _), "Hidden previous focus cannot supply manual details");

foreach (string hidden in new[] {"inactive","alpha","culled","disabled","canvas","group","collected"})
{
    Reset(); var root = new GameObject(); var stale = Label(root,"Texto viejo de la forja");
    switch (hidden)
    {
        case "inactive": stale.gameObject.Active = false; break;
        case "alpha": stale.alpha = 0; break;
        case "culled": stale.canvasRenderer.cull = true; break;
        case "disabled": stale.Enabled = false; break;
        case "canvas": stale.gameObject.Add(new Canvas { Enabled = false }); break;
        case "group": stale.gameObject.Add(new CanvasGroup { alpha = 0 }); break;
        case "collected": stale.WasCollected = true; break;
    }
    Label(root,"Etiqueta visible");
    Check(TextUtils.GetTextFromHierarchy(root) == "Etiqueta visible", "Hidden or stale text is skipped: " + hidden);
}

Reset();
var settings = new GameObject();
var first = settings.Child("Row"); Label(first,"Volumen");
var sliderGo = first.Child("Control"); sliderGo.Add(new Slider { value = 40 });
var second = settings.Child("Row"); Label(second,"Contraste");
var otherToggle = second.Child("Control").Add(new Toggle());
Label(second,"Esta descripción corresponde solo a otra opción.");
ElementAnnouncer.AnnounceElement(sliderGo);
Check(ScreenReader.Messages.Single().Text == "Volumen: 40", "Option extraction cannot borrow a neighboring label or description");
Check(!ElementAnnouncer.IsRelatedToFocusedElement(otherToggle.gameObject), "A shared menu ancestor does not own another option's value");
ElementAnnouncer.HandleToggleValueChange(otherToggle, true,"host");
Check(ScreenReader.Messages.Count == 1, "Changing an unfocused toggle remains silent");
Check(ReferenceEquals(TextDetector.FindSingleOptionContainer(sliderGo),first.transform), "Generic search stops at the row before multiple controls");

var numericRow = settings.Child("Numeric row"); Label(numericRow,"Resolución");
numericRow.Add(new LE.Settings.NumericSettingUI { _settingContainer = numericRow });
var numericSlider = numericRow.Child().Add(new Slider()); numericRow.Child().Add(new TMP_InputField());
Check(ReferenceEquals(TextDetector.FindSingleOptionContainer(numericSlider.gameObject),numericRow.transform), "The actual new setting container owns its paired slider and number input");
Reset();
var soundSetting = new GameObject(); Label(soundSetting, "Volumen de música");
var sliderInputGo = soundSetting.Child("Slider Input");
sliderInputGo.Add(new LE.Settings.NumericSettingUI { _settingContainer = soundSetting });
var soundSlider = sliderInputGo.Add(new Slider { value = 0.55f });
var numberInputGo = sliderInputGo.Child("Number Float Input");
var soundNumber = numberInputGo.Add(new TMP_InputField { text = "55 %" });
var floatInput = numberInputGo.Add(new NumberFloatInput { _input = soundNumber });
sliderInputGo.Add(new SliderInput { _slider = soundSlider, _numberInput = floatInput });
ElementAnnouncer.AnnounceElement(sliderInputGo);
Check(ScreenReader.Messages.Single().Text == "Volumen de música: 55 %",
    "A composite slider reads the named setting and game's formatted value instead of entering its child text editor");
Time.unscaledTime += 1;
ElementAnnouncer.AnnounceElement(numberInputGo);
Check(ScreenReader.Messages.Last().Text == "Volumen de música: 55 %" && ScreenReader.Messages.Count == 2,
    "The separately navigable numeric editor retains its own setting name");
var unrelatedNumber = new GameObject().Add(new TMP_InputField { text = "Valor ajeno" });
floatInput._input = unrelatedNumber;
Check(SettingsValueReader.ReadSlider(soundSlider) == null, "Foreign numeric value cannot label the focused slider");
floatInput._input = soundNumber; numberInputGo.Active = false;
Check(SettingsValueReader.ReadSlider(soundSlider) == null, "Hidden numeric editor cannot expose a stale displayed value");
foreach (bool enumSetting in new[] { true, false })
{
    Reset();
    var rows = new GameObject();
    var row = rows.Child("Dropdown row");
    var labels = row.Child("Input Labels");
    string title = enumSetting ? "Calidad de sombras" : "Resolución de pantalla";
    string current = enumSetting ? "Baja" : "1920 x 1080";
    Label(labels, title);
    Label(labels, "Esta descripción debe leerse únicamente al pedir los detalles.");
    var control = row.Child("Dropdown Input");
    if (enumSetting) control.Add(new LE.Settings.EnumSettingUI { _settingContainer = row });
    else control.Add(new LE.Settings.StringDropdownSettingUI { _settingContainer = row });
    var dropdown = control.Add(new TMP_Dropdown { captionText = Label(control, current) });
    var template = control.Child("Template"); template.Active = false;
    template.Child("Item").Add(new Toggle());
    var neighbor = rows.Child("Neighbor"); Label(neighbor, "Calidad de texturas");
    neighbor.Child().Add(new TMP_Dropdown());
    ElementAnnouncer.AnnounceElement(control);
    Check(ScreenReader.Messages.Single().Text == title + ": " + current,
        "Typed dropdown row reads its own label and value despite the inactive template: " + enumSetting);
    Check(ElementAnnouncer.TryReadDetails(control, out string details) && details.StartsWith(title + ": " + current + ". Esta descripción"),
        "Dropdown description remains manual and belongs to its declared row: " + enumSetting);
    row.Active = false;
    Check(!ElementAnnouncer.TryReadDetails(control, out _), "Inactive dropdown cannot expose old row text");
}
var hiddenRow = settings.Child("Hidden row"); hiddenRow.Add(new CanvasGroup { alpha = 0 });
Label(hiddenRow,"Invisible"); var hiddenControl = hiddenRow.Child().Add(new Toggle());
string hiddenLabel = null, hiddenDescription = null;
TextDetector.FindLabelInImmediateContainer(hiddenControl.gameObject,ref hiddenLabel,ref hiddenDescription,null);
Check(hiddenLabel is null, "Fallback label search respects canvas opacity");

Reset();
var modal = new GameObject(); var accept = Button(modal,"Aceptar"); var cancel = Button(modal,"Cancelar");
ScreenContentAnnouncer.Root = modal; ScreenContentAnnouncer.HasActiveContent = ScreenContentAnnouncer.HasPendingContent = true;
ElementAnnouncer.AnnounceElement(accept);
Check(ScreenReader.Messages.Count == 0, "Initial popup focus waits for the coherent body");
ScreenContentAnnouncer.HasPendingContent = false; ElementAnnouncer.FlushPendingFocus();
Check(ScreenReader.Messages.Single() == ("Aceptar",false), "Initial focus follows popup text without cutting it off");
ElementAnnouncer.AnnounceElement(cancel);
Check(ScreenReader.Messages.Last() == ("Cancelar",true), "Deliberate movement within the popup interrupts immediately");
ElementAnnouncer.AnnounceElement(play);
Check(ScreenReader.Messages.Count == 2, "Underlying menu focus cannot interrupt the foreground popup");
accept.Add(new InfoForTooltip { tooltipInfo = new TooltipInfo { nameText = "Aceptar", mainText = "Confirma la acción seleccionada." } });
DialogueAnnouncer.Root = new GameObject(); DialogueAnnouncer.HasActiveContent = true;
Check(ElementAnnouncer.TryReadDetails(accept, out var foregroundPopupDetails) &&
    foregroundPopupDetails == "Aceptar. Confirma la acción seleccionada.",
    "Manual popup details use the same foreground precedence as focus when a conversation remains underneath");
Reset();
DialogueAnnouncer.Root = modal; DialogueAnnouncer.HasActiveContent = true;
ScreenReader.Speak("Grael: Sigue el camino.", true);
ElementAnnouncer.AnnounceElement(accept);
Check(ScreenReader.Messages.Count == 1, "Response focus arriving with the NPC body cannot interrupt it");
ElementAnnouncer.FlushPendingFocus();
Check(ScreenReader.Messages.Last() == ("Aceptar", false), "Same-frame initial dialogue response queues after its body");
ScreenReader.SpokeThisFrame = false;
ElementAnnouncer.AnnounceElement(cancel);
Check(ScreenReader.Messages.Last() == ("Cancelar", true), "Later deliberate response navigation still interrupts");
Reset();
DialogueAnnouncer.Root = modal; DialogueAnnouncer.HasActiveContent = true;
DialogueAnnouncer.LineToFollow = true;
ElementAnnouncer.AnnounceElement(accept);
Check(ScreenReader.Messages.Single() == ("Aceptar", false), "The game's automatic response focus a frame after the line queues behind it");
ScreenReader.SpokeThisFrame = false;
ElementAnnouncer.AnnounceElement(cancel);
Check(ScreenReader.Messages.Last() == ("Cancelar", true), "Navigation after the automatic focus interrupts");
Reset();
var continueButton = modal.Child("NPC Subtitle Continue Button"); continueButton.Add(new Button());
DialogueAnnouncer.Root = modal; DialogueAnnouncer.HasActiveContent = true; DialogueAnnouncer.Continue = continueButton;
DialogueAnnouncer.LineToFollow = true;
ElementAnnouncer.AnnounceElement(continueButton);
Check(ScreenReader.Messages.Single() == (Speech.T("dialogue.continue"), false), "The icon-only continue button is named and follows the line");
Reset(); Application.isFocused = false; ElementAnnouncer.AnnounceElement(play);
Check(ScreenReader.Messages.Count == 0, "Unfocused application does not announce cursor callbacks");
Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = true; ElementAnnouncer.AnnounceElement(play);
Check(ScreenReader.Messages.Count == 0, "Loading discards stale menu focus");

Reset();
PlayerFinder.Player = new GameObject(); GameplayInputState.Owns = false;
var panel = new GameObject(); var nav = panel.Add(new Navigable { Id = 7 }); NavigationManager.currentlySelected = nav;
var forge = Prompt(new GameObject(),"La forja contiene un objeto"); ButtonPromptAnnouncer.Track(forge);
var owned = Prompt(panel,"Más información"); ButtonPromptAnnouncer.Track(owned);
TickPrompts(); TickPrompts();
Check(ScreenReader.Messages.Single() == ("Más información",false), "A retained player does not make background gameplay hints part of the active menu");
owned.gameObject.Active = false; TickPrompts();
var replacement = Prompt(panel,"Más información"); ButtonPromptAnnouncer.Track(replacement);
TickPrompts(); TickPrompts();
Check(ScreenReader.Messages.Count == 1, "Replacing the hint widget while moving options does not repeat its text");
var remote = Prompt(new GameObject(),"Comparar"); remote._activePromptNavigables = new NativeArray<int>(new[]{7});
ButtonPromptAnnouncer.Track(remote); TickPrompts(); TickPrompts();
Check(ScreenReader.Messages.Last() == ("Comparar",false), "An explicit navigable association allows prompts outside the menu's transform");
var excluded = Prompt(new GameObject(),"Aviso ajeno"); excluded._activePromptNavigables = new NativeArray<int>(new[]{7});
excluded._exceptSelectedNavigables = true; ButtonPromptAnnouncer.Track(excluded); TickPrompts(); TickPrompts();
Check(ScreenReader.Messages.Count == 2, "An exception list does not claim the selected menu");
Reset();
LeAccess.I18n.ModText.SetLanguage("es-MX");
var nameField = new GameObject();
var input = nameField.Add(new TMP_InputField { text = "" });
ElementAnnouncer.AnnounceElement(nameField);
Check(ScreenReader.Messages.Single().Text == Speech.F("control.empty_field", Speech.T("control.text_field")), "Spanish regional locale reads an empty input in Spanish");
input.placeholder = Label(nameField, "Nombre del personaje");
Time.unscaledTime += 1;
ElementAnnouncer.AnnounceElement(nameField);
Check(ScreenReader.Messages.Last().Text == Speech.F("control.empty_field", "Nombre del personaje"), "Game placeholder is retained inside the localized empty-field format");
input.text = "Maga {0}";
Time.unscaledTime += 1;
ElementAnnouncer.AnnounceElement(nameField);
Check(ScreenReader.Messages.Last().Text == Speech.F("control.field_value", "Nombre del personaje", "Maga {0}"), "User input including format braces remains literal");

Reset();
var optionRow = new GameObject(); Label(optionRow, "Sonido");
var optionControl = optionRow.Child("Control");
var localizedToggle = optionControl.Add(new Toggle { isOn = true });
ElementAnnouncer.AnnounceElement(optionControl);
Check(ScreenReader.Messages.Single().Text == Speech.F("control.field_value", "Sonido", Speech.T("state.on")), "Boolean value is localized alongside the game's own label");
ElementAnnouncer.HandleToggleValueChange(localizedToggle, false, "test");
Check(ScreenReader.Messages.Last().Text == Speech.T("state.off"), "Toggle changes use the selected mod language");
ElementAnnouncer.HandleToggleValueChange(localizedToggle, false, "test");
Check(ScreenReader.Messages.Count == 2, "Localized toggle changes remain deduplicated");

Reset();
var disabledRow = new GameObject { name = "DisabledSetting" }; Label(disabledRow, "Brillo");
var disabledControl = disabledRow.Child("Control"); disabledControl.Add(new Slider { value = 40 });
ElementAnnouncer.AnnounceElement(disabledControl);
Check(ScreenReader.Messages.Single().Text == Speech.F("control.disabled", Speech.F("control.field_value", "Brillo", 40)), "Unavailable options use a translated full-message wrapper");

Reset();
LeAccess.I18n.ModText.SetLanguage("en");
input.placeholder = null; input.text = "";
ElementAnnouncer.AnnounceElement(nameField);
Check(ScreenReader.Messages.Single().Text == Speech.F("control.empty_field", Speech.T("control.text_field")), "Returning to English replaces the mod-owned field text");
foreach (bool passive in new[] { true, false })
{
    Reset();
    var specialized = Button(new GameObject(), "Título genérico");
    if (passive) LeAccess.Accessibility.Passives.PassiveAccessibility.OwnedRoot = specialized;
    else LeAccess.Accessibility.Quests.QuestAccessibility.OwnedRoot = specialized;
    ElementAnnouncer.AnnounceElement(specialized);
    Check(ScreenReader.Messages.Count == 0, "Dedicated tree/journal reading suppresses duplicate generic labels");
    var unrelated = Button(new GameObject(), "Cerrar");
    ElementAnnouncer.AnnounceElement(unrelated);
    Check(ScreenReader.Messages.Single().Text == "Cerrar", "Dedicated tree/journal ownership does not swallow other controls");
    LeAccess.Accessibility.Passives.PassiveAccessibility.OwnedRoot = null;
    LeAccess.Accessibility.Quests.QuestAccessibility.OwnedRoot = null;
}
Reset();
LeAccess.I18n.ModText.SetLanguage("es");
var soundRow = new GameObject(); Label(soundRow, "Volumen principal");
var volume = soundRow.Child("Slider").Add(new Slider { value = 50 });
var settingsRoot = new GameObject();
SettingsFocusReader.Current = new(10, 2, volume.gameObject, "Sonido", null, settingsRoot);
bool TickSettings()
{ ScreenReader.SpokeThisFrame = false; Time.unscaledTime += 1; return SettingsFocusAnnouncer.Update(); }
Check(TickSettings() && ScreenReader.Messages.Single().Text == "Sonido. Volumen principal: 50",
    "Settings opening combines tab and first control into one announcement");
TickSettings();
Check(ScreenReader.Messages.Count == 1, "Header removal does not repeat a stable first control");
volume.value = 35; TickSettings();
Check(ScreenReader.Messages.Last().Text == "35" && ScreenReader.Messages.Count == 2,
    "Settings values refresh with only the value, without a new focus-cursor event");
volume.value = 36; ScreenReader.SpokeThisFrame = false; SettingsFocusAnnouncer.Update();
volume.value = 37; Time.unscaledTime += 0.01f; SettingsFocusAnnouncer.Update();
Check(ScreenReader.Messages.Count == 4 && ScreenReader.Messages.Last().Text == "37", "A fast value burst is not throttled and ends on the final value");
var musicRow = new GameObject(); Label(musicRow, "Música");
var music = musicRow.Child("Slider").Add(new Slider { value = 20 });
SettingsFocusReader.Current = new(10, 2, music.gameObject, "Sonido", null, settingsRoot); TickSettings();
Check(ScreenReader.Messages.Last().Text == "Música: 20", "A different control speaks label and value");
ScreenReader.Messages.Clear();
SettingsFocusReader.Current = null; TickSettings();
SettingsFocusReader.Current = new(10, 2, music.gameObject, "Sonido", null, settingsRoot); TickSettings();
Check(ScreenReader.Messages.Count == 0, "Heading is not repeated after a dropdown/popup gap while the panel stays visible");
SettingsFocusReader.Current = null; settingsRoot.Active = false; TickSettings(); settingsRoot.Active = true;
SettingsFocusReader.Current = new(10, 2, music.gameObject, "Sonido", null, settingsRoot); TickSettings();
Check(ScreenReader.Messages.Last().Text == "Sonido. Música: 20", "Heading is spoken again after the panel closed and reopened");
ScreenReader.Messages.Clear(); ScreenReader.Messages.Add(("", false)); ScreenReader.Messages.Add(("", false)); // keep the count the next checks expect
SettingsFocusReader.Current = new(11, 4, null, "Desafíos", "Modo extremo. Desactivado");
TickSettings();
Check(ScreenReader.Messages.Last().Text == "Desafíos. Modo extremo. Desactivado",
    "A tab without an active button still announces its visible state");
TickSettings();
Check(ScreenReader.Messages.Count == 3, "Buttonless tab summary stays silent while unchanged");
SettingsFocusReader.Current = new(11, 4, null, "Desafíos", "Modo extremo. Activado");
TickSettings();
Check(ScreenReader.Messages.Count == 4 && ScreenReader.Messages.Last().Text.EndsWith("Activado"),
    "Visible challenge state changes refresh even without a selectable");
Check(SettingsFocusAnnouncer.ReadCurrentDetails() && ScreenReader.Messages.Count == 4,
    "A buttonless tab cannot read details from old focus");
ScreenContentAnnouncer.HasActiveContent = true;
Check(!TickSettings() && ScreenReader.Messages.Count == 4, "Foreground popup suppresses background settings");
ScreenContentAnnouncer.HasActiveContent = false; TickSettings();
Check(ScreenReader.Messages.Count == 5, "Returning from foreground content renews the tab context");
Application.isFocused = false;
Check(!TickSettings() && ScreenReader.Messages.Count == 5, "Unfocused settings remain silent");
Application.isFocused = true; TickSettings();
Check(ScreenReader.Messages.Count == 6, "Focus restoration renews context once");
SettingsFocusReader.Current = null;
Check(!TickSettings() && !SettingsFocusAnnouncer.ReadCurrentDetails(), "Closing settings releases focus and manual detail ownership");
var settingsPanel = new GameObject();
SettingsFocusReader.Current = new(10, 2, volume.gameObject, "Sonido", null, settingsPanel); TickSettings();
Check(ScreenReader.Messages.Last().Text == "Sonido. Volumen principal: 37",
    "Reopening renews the same selected control with current value");
var boolRow = new GameObject(); Label(boolRow, "Filtro de lenguaje");
var boolSetting = boolRow.Child().Add(new Toggle { isOn = true });
SettingsFocusReader.Current = new(12, 3, boolSetting.gameObject, "Social", null); TickSettings();
int beforeToggleChange = ScreenReader.Messages.Count;
boolSetting.isOn = false; ElementAnnouncer.HandleToggleValueChange(boolSetting, false, "native");
Check(ScreenReader.Messages.Count == beforeToggleChange, "Settings polling owns the value instead of duplicating the native toggle callback");
TickSettings();
Check(ScreenReader.Messages.Count == beforeToggleChange + 1 && ScreenReader.Messages.Last().Text == Speech.T("state.off"),
    "A settings toggle change is announced exactly once as its value only");
Reset();
checks += VisibilityTests.Run();
Reset();
var mediator = new GameObject().Add(new NavigableMediator());
var mediatedPanel = new GameObject().Add(new LE.UI.PanelSystem.Panel());
var panelButton = Button(mediatedPanel.gameObject, "Entrar");
var foreignButton = Button(new GameObject(), "Fondo");
mediator.FocusedPanel = mediatedPanel;
NavigationManager.currentlySelected = mediator;
Check(UiFocusOwnership.Allows(panelButton), "Native mediator grants callbacks to its separately rooted focused panel");
Check(!UiFocusOwnership.Allows(foreignButton), "Native mediator rejects callbacks outside its focused panel");
Check(!UiFocusOwnership.TryGetSelected(out _), "A panel owner alone does not invent a polled control selection");
ElementAnnouncer.AnnounceElement(panelButton);
Check(ScreenReader.Messages.Single().Text == "Entrar", "Focused native panel button retains accessible cursor reading");
var nextPanel = new GameObject().Add(new LE.UI.PanelSystem.Panel());
var nextButton = Button(nextPanel.gameObject, "Cancelar");
mediator.FocusedPanel = nextPanel;
Check(!UiFocusOwnership.Allows(panelButton) && UiFocusOwnership.Allows(nextButton), "Mediator panel change immediately rejects retained callbacks");
mediator.FocusedPanel = null;
Check(!UiFocusOwnership.Allows(nextButton), "Unassigned mediator panel cannot retain focus");
mediator.FocusedPanel = nextPanel;
foreach (string gate in new[] { "collected", "disabled", "hidden", "mediator disabled" })
{
    nextPanel.WasCollected = gate == "collected";
    nextPanel.Enabled = gate != "disabled";
    nextPanel.gameObject.Active = gate != "hidden";
    mediator.Enabled = gate != "mediator disabled";
    Check(!UiFocusOwnership.Allows(nextButton), "Mediator focus rejects " + gate);
}
Reset();
checks += CraftingTests.Run(Reset);
checks += FilterRangeTests.Run(Reset);
checks += CraftingSelectionTests.Run(Reset);
checks += ForgeSummaryTests.Run(Reset);
checks += GuideArticleTests.Run();
checks += EndgameTests.Run();
checks += PanelTextTests.Run();
checks += FactionTreeTextTests.Run();
Console.WriteLine($"Menu reading tests passed: {checks} checks.");
