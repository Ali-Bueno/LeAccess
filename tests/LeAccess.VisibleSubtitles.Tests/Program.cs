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
}
void Tick(bool blocked=false)=>VisibleSubtitleAnnouncer.Update(blocked);
FloatingBark Bark(string text)
{
    var ui=new GameObject().Add(new FloatingTextLineUI{textComponent=Label(text)});
    var bark=new GameObject().Add(new FloatingBark{FloatingUI=ui});
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
Console.WriteLine($"Passed {checks} visible subtitle checks.");
