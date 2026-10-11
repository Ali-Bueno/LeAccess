using LeAccess.Accessibility;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;

int checks=0;
void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
TMP_Text Label(string text)=>new GameObject().Add(new TMP_Text{text=text});
void Reset()
{
    VisibleSubtitleAnnouncer.Reset(); ScreenReader.Messages.Clear();
    CinematicSubtitlePanel.Instance=null; Application.isFocused=true; LE.UI.LoadingScreen.IsVisible=false;
    DialogueAnnouncer.HasActiveContent=ScreenContentAnnouncer.HasActiveContent=false;
    PlayerFinder.player=new GameObject(); CameraManager.Main=new GameObject().Add(new Camera());
    Screen.width=1920; Screen.height=1080;
}
void Tick(bool blocked=false)=>VisibleSubtitleAnnouncer.Update(blocked);
FloatingBark Bark(string text)
{
    var canvas=new GameObject();canvas.Add(new Canvas());
    var ui=canvas.Child().Add(new FloatingBarkUI{textComponent=Label(text)});
    ui.textComponent.gameObject.Parent=ui.gameObject;
    var bark=new GameObject().Add(new FloatingBark{FloatingUI=ui});
    ui.bark=bark;
    VisibleSubtitleAnnouncer.BarkShown(bark);return bark;
}
Reset();var bark=Bark("Grae: ¡Sigamos!");Tick();
Check(VisibleSubtitleAnnouncer.HasPendingContent&&ScreenReader.Messages.Count==0,"visible bark settles");
Tick();Check(ScreenReader.Messages.Single().Text=="Grae: ¡Sigamos!","actual displayed localized bark");
Check(!ScreenReader.Messages[0].Interrupt&&!VisibleSubtitleAnnouncer.HasPendingContent,"queued delivery clears pending");
Tick();Check(ScreenReader.Messages.Count==1,"continuous bark dedup");
VisibleSubtitleAnnouncer.BarkHidden(bark);Tick();Check(!VisibleSubtitleAnnouncer.HasPendingContent,"hide clears source");
VisibleSubtitleAnnouncer.BarkShown(bark);Tick();Tick();Check(ScreenReader.Messages.Count==2,"new occurrence same text rearmed");
foreach(string gate in new[]{"owner","label","alpha","hierarchy","cull","ui","timer","collected"})
{
    Reset();bark=Bark("Hidden line");
    switch(gate){case "owner":bark.isActiveAndEnabled=false;break;case "label":bark.FloatingUI.textComponent.isActiveAndEnabled=false;break;
    case "alpha":bark.FloatingUI.textComponent.alpha=0;break;case "hierarchy":bark.FloatingUI.textComponent.gameObject.Visible=false;break;
    case "cull":bark.FloatingUI.textComponent.canvasRenderer.cull=true;break;case "ui":bark.FloatingUI.isActiveAndEnabled=false;break;
    case "timer":bark.secondsLeft=0;break;case "collected":bark.WasCollected=true;break;}
    Tick();Tick();Check(ScreenReader.Messages.Count==0&&!VisibleSubtitleAnnouncer.HasPendingContent,"bark rendered gate "+gate);
}
Reset();bark=Bark("Delayed UI");bark.FloatingUI.textComponent.alpha=0;Tick();
bark.FloatingUI.textComponent.alpha=1;Tick();Tick();Check(ScreenReader.Messages.Count==1,"UI created/shown after bark hook");
Reset();bark=Bark("Old");Tick(true);Tick(true);bark.FloatingUI.textComponent.text="Current";Tick();
Check(ScreenReader.Messages.Count==0,"changed deferred source settles current text");Tick();
Check(ScreenReader.Messages.Single().Text=="Current","deferred old line never spoken");
Reset();bark=Bark("Expired");Tick(true);bark.secondsLeft=0;Tick();Tick();
Check(ScreenReader.Messages.Count==0&&!VisibleSubtitleAnnouncer.HasPendingContent,"expired deferred bark discarded");
Reset();bark=Bark("Floating");var panel=new GameObject().Add(new CinematicSubtitlePanel{
    _subtitleText=Label("Cinematic localized"),_canvasGroup=new GameObject().Add(new CanvasGroup())});
CinematicSubtitlePanel.Instance=panel;Tick();Tick();
Check(ScreenReader.Messages.Single().Text=="Cinematic localized","cinematic takes precedence");Tick();
Check(ScreenReader.Messages.Count==1,"floating bark cannot interrupt visible cinematic");panel.IsActive=false;Tick();
Check(ScreenReader.Messages.Last().Text=="Floating","still rendered bark follows cinematic");
foreach(string gate in new[]{"inactive","disabled","canvas","label","empty","collected"})
{
    Reset();CinematicSubtitlePanel.Instance=panel;panel.IsActive=true;panel.isActiveAndEnabled=true;panel.WasCollected=false;
    panel._canvasGroup.alpha=1;panel._subtitleText.alpha=1;panel._subtitleText.text="Visible";
    switch(gate){case "inactive":panel.IsActive=false;break;case "disabled":panel.isActiveAndEnabled=false;break;
    case "canvas":panel._canvasGroup.alpha=0;break;case "label":panel._subtitleText.alpha=0;break;
    case "empty":panel._subtitleText.text=" ";break;case "collected":panel.WasCollected=true;break;}
    Tick();Tick();Check(ScreenReader.Messages.Count==0&&!VisibleSubtitleAnnouncer.HasPendingContent,"cinematic rendering gate "+gate);
}
foreach(string gate in new[]{"dialogue","modal","focus","loading"})
{
    Reset();Bark("Suppressed");DialogueAnnouncer.HasActiveContent=gate=="dialogue";
    ScreenContentAnnouncer.HasActiveContent=gate=="modal";Application.isFocused=gate!="focus";LE.UI.LoadingScreen.IsVisible=gate=="loading";
    Tick();Tick();Check(ScreenReader.Messages.Count==0,"priority/lifecycle "+gate);
    Application.isFocused=true;LE.UI.LoadingScreen.IsVisible=false;DialogueAnnouncer.HasActiveContent=ScreenContentAnnouncer.HasActiveContent=false;
    Tick();Check(ScreenReader.Messages.Count==(gate=="dialogue"||gate=="modal"?1:0),"current vs reset source "+gate);
}
Reset();Bark("A");Bark("B");Tick();Tick();Check(ScreenReader.Messages.Count==1,"one bark delivered per update");
Tick();Check(ScreenReader.Messages.Count==2,"second current bark delivered subsequently");
VisibleSubtitleAnnouncer.Reset();Tick();Check(!VisibleSubtitleAnnouncer.HasPendingContent,"cleanup pending reset");

Reset();VisibleSubtitleAnnouncer.NpcMessageSubmitted("No hay vuelta atrás.","Centinela");Tick();
Check(ScreenReader.Messages.Single().Text=="Centinela: No hay vuelta atrás.","localized player narrative without floating UI or online chat");
Check(!ScreenReader.Messages[0].Interrupt&&!VisibleSubtitleAnnouncer.HasPendingContent,"submitted narrative queued without interruption");
Tick();Check(ScreenReader.Messages.Count==1,"submitted event consumed once");
Reset();VisibleSubtitleAnnouncer.NpcMessageSubmitted("Sigamos.","Grael");Tick(true);
Check(ScreenReader.Messages.Count==0&&VisibleSubtitleAnnouncer.HasPendingContent,"submitted line yields to current speech");
Tick();Check(ScreenReader.Messages.Count==1,"submitted line resumes when speech priority permits");
foreach(string gate in new[]{"dialogue","modal","focus","loading"})
{
    Reset();VisibleSubtitleAnnouncer.NpcMessageSubmitted("Old line","Grael");
    DialogueAnnouncer.HasActiveContent=gate=="dialogue";ScreenContentAnnouncer.HasActiveContent=gate=="modal";
    Application.isFocused=gate!="focus";LE.UI.LoadingScreen.IsVisible=gate=="loading";Tick();
    Check(ScreenReader.Messages.Count==0&&!VisibleSubtitleAnnouncer.HasPendingContent,"submitted lifecycle gate "+gate);
    Application.isFocused=true;LE.UI.LoadingScreen.IsVisible=false;
    DialogueAnnouncer.HasActiveContent=ScreenContentAnnouncer.HasActiveContent=false;Tick();
    Check(ScreenReader.Messages.Count==0,"discarded submission does not become history "+gate);
}
Reset();bark=Bark("Grael: Sigamos.");Tick();
VisibleSubtitleAnnouncer.NpcMessageSubmitted("Sigamos.","Grael");Tick();Tick();
Check(ScreenReader.Messages.Count==1,"bark and submitted NPC text delivered once");
VisibleSubtitleAnnouncer.NpcMessageSubmitted("Sigamos.","Grael");Tick();
Check(ScreenReader.Messages.Count==1,"late submission matches current delivered bark");
VisibleSubtitleAnnouncer.BarkHidden(bark);VisibleSubtitleAnnouncer.NpcMessageSubmitted("Sigamos.","Grael");Tick();
Check(ScreenReader.Messages.Count==2,"hidden prior bark permits new repeated line");
Reset();VisibleSubtitleAnnouncer.NpcMessageSubmitted("Sigamos.","Grael");Tick();bark=Bark("Grael: Sigamos.");Tick();Tick();
Check(ScreenReader.Messages.Count==1,"late floating label does not repeat submitted narration");
VisibleSubtitleAnnouncer.BarkHidden(bark);VisibleSubtitleAnnouncer.BarkShown(bark);Tick();Tick();
Check(ScreenReader.Messages.Count==2,"duplicate suppression consumes only one bark occurrence");
Reset();VisibleSubtitleAnnouncer.NpcMessageSubmitted(" ","Grael");Tick();
Check(ScreenReader.Messages.Count==0,"empty event never announces speaker alone");
VisibleSubtitleAnnouncer.NpcMessageSubmitted("Aquí.",null);Tick();
Check(ScreenReader.Messages.Single().Text=="Aquí.","unnamed authored speech preserves body");

var submittedTracker=new SubmittedSubtitleTracker();
submittedTracker.Submit("Expired","Grael",0);
Check(!submittedTracker.TryTake(true,SubmittedSubtitleTracker.MaximumDelaySeconds,out var submittedText)&&submittedText==null,
    "expired priority-deferred narration discarded");
submittedTracker.Submit("Again","Grael",20);submittedTracker.Submit("Again","Grael",20);
Check(!submittedTracker.TryTake(true,20,out submittedText)&&submittedText=="Grael: Again","duplicate pending callback collapsed");
submittedTracker.Submit("Again","Grael",21);
Check(!submittedTracker.TryTake(true,21,out submittedText)&&submittedText=="Grael: Again","new spoken occurrence is not globally suppressed");
submittedTracker.Submit("Again","Grael",22);
Check(!submittedTracker.ObserveRendered(1,"Grael: Again",22),"old delivery cannot suppress a new submitted-and-rendered occurrence");
Check(!submittedTracker.TryTake(true,22,out submittedText)&&submittedText==null,"new rendered occurrence consumes only its submission");
submittedTracker.Reset();submittedTracker.Submit("Line","A",0);submittedTracker.TryTake(true,0,out _);
Check(!submittedTracker.ObserveRendered(1,"B: Line",0),"different named actor not suppressed");
Check(submittedTracker.ObserveRendered(2,"A: Line",0),"matching named actor suppressed once");
submittedTracker.ForgetRendered(2);submittedTracker.Submit("Line","A",1);
submittedTracker.TryTake(true,1,out submittedText);Check(submittedText=="A: Line","forgotten rendered owner permits repeat");
submittedTracker.Reset();submittedTracker.Submit("Old","A",0);submittedTracker.TryTake(true,0,out _);
Check(!submittedTracker.ObserveRendered(1,"A: Old",SubmittedSubtitleTracker.MaximumDelaySeconds),"old submitted occurrence cannot suppress later bark");
submittedTracker.Reset();
for(int i=0;i<=SubmittedSubtitleTracker.MaximumPendingLines;i++)submittedTracker.Submit("Line "+i,null,0);
Check(submittedTracker.TryTake(true,0,out submittedText)&&submittedText=="Line 1","bounded backlog discards oldest pending line");
submittedTracker.Reset();Check(!submittedTracker.TryTake(true,0,out submittedText)&&submittedText==null,"reset discards pending and dedup state");
VideoSubtitleTests.Run(Check);
NpcLines.Use(()=>false);
Reset();bark=Bark("Grae: ¡Sigamos!");VisibleSubtitleAnnouncer.NpcMessageSubmitted("Sigamos.","Grael");Tick();Tick();Tick();
Check(ScreenReader.Messages.Count==0&&!VisibleSubtitleAnnouncer.HasPendingContent,"silenced barks and NPC messages are neither spoken nor retried");
NpcLines.Use(()=>true);
BarkAudibilityTests.Run(Check);
RejectedOutputTests.Run(Check);
Console.WriteLine($"Passed {checks} visible subtitle checks.");
