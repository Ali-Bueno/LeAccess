using LeAccess.Accessibility.Settings;
using TMPro;
using UnityEngine;

int checks=0;
void Check(bool result,string label){checks++;if(!result)throw new Exception(label);}
SettingsPanelTabNavigable owner=null;SettingsUIManager manager=null;SettingsPanelTabUIController controller=null;
TabUIElement tab=null;TMP_Text title=null;RectTransform rect=null;Component component=null;
void Setup(int tabId=2)
{
    manager=new GameObject().Add(new SettingsUIManager());
    controller=manager.gameObject.Child().Add(new SettingsPanelTabUIController{SelectedTabId=tabId,_settingsUIManager=manager,isActiveAndEnabled=false});
    owner=manager.gameObject.Child().Add(new SettingsPanelTabNavigable{_tabUIController=controller});
    tab=controller.gameObject.Child().Add(new TabUIElement{elementID=tabId,controller=controller});
    title=tab.gameObject.Child().Add(new TMP_Text{text="Sonido"});controller.tabElements=new(tab);
    switch(tabId){case 0:manager.gameplayTab=owner.gameObject;break;case 1:manager.GraphicsTab=owner.gameObject;break;
        case 2:manager.soundTab=owner.gameObject;break;case 3:manager.socialTab=owner.gameObject;break;case 4:manager.challengesTab=owner.gameObject;break;}
    var selected=owner.gameObject.Child();rect=selected.Add(new RectTransform());component=selected.Add(new Component());
    owner._selectables=new(rect);owner._selectableComponents=new(component);
    NavigationManager.currentlySelected=owner;
}
bool Read(out SettingsFocusSnapshot snapshot)=>SettingsFocusReader.TryRead(out snapshot);
void Rejected(string label)=>Check(!Read(out _),label);
void NoSelection(string label)=>Check(Read(out var result)&&result.Selected==null&&result.OwnerId==owner.GetInstanceID(),label);
Setup();Check(Read(out var result)&&result.Selected==rect.gameObject&&result.TabName=="Sonido"&&result.TabId==2,
    "actual MonoBehaviour settings navigable exposes RectTransform without Selectable");
Check(!controller.isActiveAndEnabled,"installed disabled tab controller still authorizes its data");
foreach(int tabId in new[]{0,1,2,3,4})
{Setup(tabId);Check(Read(out result)&&result.TabId==tabId,"verified native settings manager tab dispatch "+tabId);}
Setup();title.text="<b>Audio</b>";Check(Read(out result)&&result.TabName=="Audio","current localized tab text cleaned");
var second=owner.gameObject.Child();var secondRect=second.Add(new RectTransform());var secondComponent=second.Add(new Component());
owner._selectables=new(rect,secondRect);owner._selectableComponents=new(component,secondComponent);owner._currentlySelectedIdx=1;
Check(Read(out result)&&result.Selected==second,"index movement read without cursor callback");
Setup();NavigationManager.currentlySelected=new GameObject();Rejected("unrelated navigation cannot read background settings");
Setup();owner.isActiveAndEnabled=false;Rejected("disabled current owner rejected");
Setup();owner.gameObject.Visible=false;Rejected("hidden current tab rejected");
Setup();owner.WasCollected=true;Rejected("collected current owner rejected");
Setup();owner.Destroyed=true;Rejected("destroyed current owner rejected");
Setup();manager.gameObject.Visible=false;Rejected("hidden settings panel rejected");
Setup();controller.gameObject.Visible=false;Rejected("hidden tabs rejected");
Setup();controller.WasCollected=true;Rejected("collected controller rejected");
Setup();controller._settingsUIManager=new GameObject().Add(new SettingsUIManager());Rejected("foreign manager cannot claim active owner");
Setup();controller.gameObject.Parent=new GameObject();Rejected("reparented controller rejected");
Setup();manager.soundTab=manager.gameObject.Child();Rejected("current owner must match native selected content exactly");
Setup();controller.SelectedTabId=4;manager.challengesTab=manager.gameObject.Child();Rejected("stale navigation from previous tab rejected");
Setup();controller.SelectedTabId=999;Rejected("unknown native tab ID rejected");
Setup();controller.tabElements=null;Rejected("missing tab registry rejected");
Setup();controller.tabElements.WasCollected=true;Rejected("collected tab registry rejected");
Setup();tab.elementID=1;Rejected("tab title cannot come from nonselected ID");
Setup();controller.tabElements=new(tab,tab);Rejected("ambiguous duplicate selected tab ID rejected");
Setup();tab.controller=new TabUIController();Rejected("foreign element-controller association rejected");
Setup();tab.gameObject.Parent=manager.gameObject;Rejected("foreign element outside controller hierarchy rejected");
Setup();tab.gameObject.Visible=false;Rejected("invisible selected tab rejected");
Setup();title.alpha=0;Check(Read(out result)&&result.TabName==null,"transparent tab label is never read");
Setup();title.canvasRenderer.cull=true;Check(Read(out result)&&result.TabName==null,"culled tab label is never read");
Setup();tab.gameObject.Child().Add(new TMP_Text{text="Other unrelated title"});
Check(Read(out result)&&result.TabName==null,"ambiguous tab text not guessed");
Setup();owner._currentlySelectedIdx=-1;NoSelection("negative selection index retains visible tab snapshot");
Setup();owner._currentlySelectedIdx=1;NoSelection("out-of-bounds index never selects first control");
Setup();owner._selectables=null;NoSelection("missing rect cache retains tab identity only");
Setup();owner._selectables.WasCollected=true;NoSelection("collected rect cache rejected");
Setup();owner._selectableComponents=null;NoSelection("not initialized native component cache cannot invent focus");
owner._selectableComponents=new(component);
Check(Read(out result)&&result.Selected==rect.gameObject,"late native component initialization recovers through read-only polling");
Setup();owner._selectableComponents=new();NoSelection("mismatched cache lengths rejected");
Setup();owner._selectableComponents[0]=new GameObject();NoSelection("noncomponent cache entry rejected");
Setup();owner._selectableComponents[0]=new GameObject().Add(new Component());NoSelection("foreign component cannot borrow selected rectangle");
Setup();rect.gameObject.Parent=manager.gameObject;NoSelection("control outside current tab rejected");
Setup();rect.gameObject.Visible=false;NoSelection("hidden current control does not erase tab");
Setup();rect.Destroyed=true;NoSelection("destroyed current rectangle rejected");
Setup();component.WasCollected=true;NoSelection("collected selected component rejected");

CharacterChallengeSetting Challenge(string name,bool enabled)
{
    var challenge=owner.gameObject.Child().Add(new CharacterChallengeSetting{navigable=owner});
    challenge.challengeNameText=challenge.gameObject.Child().Add(new TMP_Text{text=name});
    challenge.challengeEnabledObj=challenge.gameObject.Child();challenge.challengeEnabledObj.Active=enabled;
    challenge.challengeEnabledDescription=challenge.challengeEnabledObj.Add(new TMP_Text{text="Activado<size=94%><color=#C3C0B2><br>Explicación larga"});
    challenge.challengeDisabledObj=challenge.gameObject.Child();challenge.challengeDisabledObj.Active=!enabled;
    challenge.challengeDisabledObj.Add(new TMP_Text{text="Desactivado<size=94%><color=#C3C0B2><br>Explicación larga"});
    return challenge;
}
void EmptySummary(string label)=>Check(Read(out result)&&string.IsNullOrEmpty(result.TabSummary),label);
Setup(4);title.text="Desafíos";owner._selectables=new();owner._selectableComponents=new();
var a=Challenge("Solo por cuenta",false);var b=Challenge("Solo por personaje",false);
Check(Read(out result)&&result.Selected==null&&result.TabName=="Desafíos"&&
    result.TabSummary=="Solo por cuenta: Desactivado. Solo por personaje: Desactivado",
    "installed challenge tab with no active buttons still reads both visible brief states");
a.challengeDisabledObj.Active=false;a.challengeEnabledObj.Active=true;
Check(Read(out result)&&result.TabSummary.StartsWith("Solo por cuenta: Activado."),"current challenge visible state changes reflected");
Check(!result.TabSummary.Contains("Explicación"),"navigation summary omits long authored explanation");
Check(SettingsFocusReader.TryReadTabDetails(out string manual) && manual.StartsWith("Desafíos.") &&
    manual.Contains("Activado Explicación larga") && manual.Contains("Desactivado Explicación larga"),
    "manual inspection retains complete visible challenge explanations even without a selected button");
owner.gameObject.Visible=false;
Check(!SettingsFocusReader.TryReadTabDetails(out _), "hidden challenges cannot supply stale manual details");
Setup(4);a=Challenge("Mode",true);a.challengeEnabledDescription.text="Enabled\nLong description";
Check(Read(out result)&&result.TabSummary=="Mode: Enabled","native newline separates brief status from description");
Setup(4);a=Challenge("Mode",true);a.challengeEnabledDescription.text="Enabled<BR/>Long description";
Check(Read(out result)&&result.TabSummary=="Mode: Enabled","rich break spelling preserves localized status");
Setup(4);a=Challenge("Hidden",true);a.gameObject.Active=false;EmptySummary("inactive challenge excluded");
Setup(4);a=Challenge("Foreign",true);a.navigable=new SettingsPanelTabNavigable();EmptySummary("foreign challenge navigable rejected");
Setup(4);a=Challenge("Ambiguous",true);a.challengeDisabledObj.Active=true;EmptySummary("both states visible does not guess enabled status");
Setup(4);a=Challenge("Neither",true);a.challengeEnabledObj.Active=false;EmptySummary("both states hidden remain silent");
Setup(4);a=Challenge("Foreign",true);a.challengeEnabledDescription=new GameObject().Add(new TMP_Text{text="Enabled"});
EmptySummary("typed enabled label still must belong to declared state owner");
Setup(4);a=Challenge("Foreign",false);a.challengeDisabledObj=new GameObject();a.challengeDisabledObj.Add(new TMP_Text{text="Disabled"});
EmptySummary("foreign disabled-state object cannot supply text");
Setup(4);a=Challenge("Ambiguous",false);a.challengeDisabledObj.Child().Add(new TMP_Text{text="Unrelated"});
EmptySummary("multiple distinct disabled labels fail closed");
Setup(4);a=Challenge("Invisible",true);a.challengeNameText.alpha=0;EmptySummary("invisible challenge title excluded");
Setup(4);a=Challenge("Invisible",true);a.challengeEnabledDescription.canvasRenderer.cull=true;EmptySummary("culled challenge state excluded");
Setup(2);a=Challenge("Hidden challenge under wrong tab",true);EmptySummary("challenge state read only in native challenges tab");
Check(!SettingsFocusReader.TryReadTabDetails(out _), "other tabs cannot read background challenge details");

var heading=new SettingsHeadingTracker();
Check(heading.Update(1,2,"Gráficos"),"first heading is spoken");
Check(!heading.Update(1,2,"Gráficos"),"unchanged heading is not repeated");
heading.Retain(true);Check(!heading.Update(1,2,"Gráficos"),"dropdown/popup gap with visible panel keeps heading");
heading.Retain(false);Check(heading.Update(1,2,"Gráficos"),"heading re-spoken after panel close and reopen");
Check(heading.Update(1,3,"Sonido"),"tab change speaks new heading");
Check(SettingsValueSpeech.Plan(true,"Volumen","50","Volumen","60","Volumen: 60")=="60","same control value burst speaks value only");
Check(SettingsValueSpeech.Plan(true,"Volumen","60","Volumen","70","Volumen: 70")=="70","burst ends with the final value");
Check(SettingsValueSpeech.Plan(false,"Volumen","60","Música","30","Música: 30")=="Música: 30","different control speaks label and value");
Check(SettingsValueSpeech.Plan(true,"Volumen","60","Música","30","Música: 30")=="Música: 30","changed label speaks everything");
Check(SettingsValueSpeech.Plan(true,null,null,null,"On","On")=="On","unlabelled control falls back to full text");
Console.WriteLine($"Passed {checks} settings focus checks.");
