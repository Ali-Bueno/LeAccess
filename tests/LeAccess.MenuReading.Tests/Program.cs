using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.Navigation;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

int checks = 0;
void Check(bool condition, string message)
{ checks++; if (!condition) throw new InvalidOperationException(message); }
void Reset()
{
    ElementAnnouncer.ResetFocus(); ButtonPromptAnnouncer.Reset();
    ScreenReader.Messages.Clear(); ScreenReader.Speaking = ScreenReader.SpokeThisFrame = false;
    ScreenContentAnnouncer.HasActiveContent = ScreenContentAnnouncer.HasPendingContent = false;
    DialogueAnnouncer.HasActiveContent = DialogueAnnouncer.HasPendingContent = false;
    DialogueAnnouncer.Root = null;
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
Check(ScreenReader.Messages.Single().Text == "Sin conexión. " + description, "A button keeps its complete associated description in the same utterance");

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
Check(ScreenReader.Messages.Single().Text == "Campo de texto: vacío", "Spanish regional locale reads an empty input in Spanish");
input.placeholder = Label(nameField, "Nombre del personaje");
Time.unscaledTime += 1;
ElementAnnouncer.AnnounceElement(nameField);
Check(ScreenReader.Messages.Last().Text == "Nombre del personaje: vacío", "Game placeholder is retained inside the localized empty-field format");
input.text = "Maga {0}";
Time.unscaledTime += 1;
ElementAnnouncer.AnnounceElement(nameField);
Check(ScreenReader.Messages.Last().Text == "Nombre del personaje: Maga {0}", "User input including format braces remains literal");

Reset();
var optionRow = new GameObject(); Label(optionRow, "Sonido");
var optionControl = optionRow.Child("Control");
var localizedToggle = optionControl.Add(new Toggle { isOn = true });
ElementAnnouncer.AnnounceElement(optionControl);
Check(ScreenReader.Messages.Single().Text == "Sonido: Activado", "Boolean value is localized alongside the game's own label");
ElementAnnouncer.HandleToggleValueChange(localizedToggle, false, "test");
Check(ScreenReader.Messages.Last().Text == "Desactivado", "Toggle changes use the selected mod language");
ElementAnnouncer.HandleToggleValueChange(localizedToggle, false, "test");
Check(ScreenReader.Messages.Count == 2, "Localized toggle changes remain deduplicated");

Reset();
var disabledRow = new GameObject { name = "DisabledSetting" }; Label(disabledRow, "Brillo");
var disabledControl = disabledRow.Child("Control"); disabledControl.Add(new Slider { value = 40 });
ElementAnnouncer.AnnounceElement(disabledControl);
Check(ScreenReader.Messages.Single().Text == "Brillo: 40 (No disponible)", "Unavailable options use a translated full-message wrapper");

Reset();
LeAccess.I18n.ModText.SetLanguage("en");
input.placeholder = null; input.text = "";
ElementAnnouncer.AnnounceElement(nameField);
Check(ScreenReader.Messages.Single().Text == "Text field: empty", "Returning to English replaces the mod-owned field text");
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
Console.WriteLine($"Menu reading tests passed: {checks} checks.");
