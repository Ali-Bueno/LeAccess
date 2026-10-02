using LeAccess.Accessibility;
using LeAccess.Accessibility.Dialogue;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;

internal static class BarkAudibilityTests
{
    internal static void Run(Action<bool,string> check)
    {
        FloatingBark bark=null;
        FloatingBarkUI ui=null;
        Canvas canvas=null;
        WorldObjectClickListener listener=null;
        void Reset(bool visible=false, bool withListener=true)
        {
            VisibleSubtitleAnnouncer.Reset(); ScreenReader.Messages.Clear();
            CharacterSelectionVideoManager.instance=null; CinematicSubtitlePanel.Instance=null;
            Application.isFocused=true; LE.UI.LoadingScreen.IsVisible=false;
            DialogueAnnouncer.HasActiveContent=ScreenContentAnnouncer.HasActiveContent=false;
            PlayerFinder.player=new GameObject();
            CameraManager.Main=new GameObject().Add(new Camera());
            Screen.width=1920;Screen.height=1080;
            canvas=new GameObject().Add(new Canvas());
            ui=canvas.gameObject.Child().Add(new FloatingBarkUI());
            ui.textComponent=ui.gameObject.Child().Add(new TMP_Text{text="Grael: Welcome"});
            ui.textComponent.rectTransform.Bounds=visible?new Rect(10,10,100,20):new Rect(2000,10,100,20);
            bark=new GameObject().Add(new FloatingBark{FloatingUI=ui});ui.bark=bark;
            bark.transform.position=new(){x=100,z=1};
            listener=withListener?bark.gameObject.Add(new WorldObjectClickListener()):null;
            VisibleSubtitleAnnouncer.BarkShown(bark);
        }
        void Tick(int count=2){for(int i=0;i<count;i++)VisibleSubtitleAnnouncer.Update();}
        void Silent(string label){Tick();check(ScreenReader.Messages.Count==0&&!VisibleSubtitleAnnouncer.HasPendingContent,label);}
        void Speaks(string label){Tick();check(ScreenReader.Messages.Count==1,label);}

        Reset();Silent("far active uncullled floating UI outside screen stays silent");
        Reset(true);Speaks("far source with truly visible label remains readable");
        Reset();bark.transform.position=new(){x=5,z=1};Speaks("offscreen source at declared native interaction radius reads");
        Reset();bark.transform.position=new(){x=5.01f,z=1};Silent("offscreen source beyond radius stays silent");
        Reset();bark.transform.position=new(){x=1,y=10,z=1};Silent("physical 3D distance counts vertical separation");
        Reset();listener.interactionRange=float.NaN;Silent("nonfinite radius rejected");
        Reset();listener.interactionRange=0;Silent("zero radius rejected");
        Reset();bark.transform.position=new(){x=float.NaN,z=1};Silent("nonfinite source distance rejected");
        Reset();listener.interactionRange=100;listener.isActiveAndEnabled=false;Silent("disabled listener supplies no offscreen radius");
        Reset(true,false);Speaks("visible ambient source without interaction listener reads");
        Reset(false,false);Silent("no invented hearing radius for listenerless NPC");
        Reset(false,false);bark.gameObject.Parent=PlayerFinder.player;Speaks("own authored player subtitle reads offscreen without listener");
        Reset(false,false);PlayerFinder.player.Parent=bark.gameObject;Silent("broad parent of player is not owned player bark");
        Reset(true);bark.gameObject.scene=new(){handle=2,isLoaded=true};Speaks("visible additive-scene source does not require player scene ownership");
        Reset(true);PlayerFinder.player=null;Speaks("visible subtitle reads before player reference is available");
        Reset(true);PlayerFinder.player.Active=false;Speaks("visible subtitle reads while player reference is inactive");
        Reset(true);bark.gameObject.scene=new(){handle=2,isLoaded=false};Silent("unloaded source is stale even with old onscreen geometry");
        Reset();bark.gameObject.scene=new(){handle=2,isLoaded=true};bark.transform.position=new(){x=1,z=1};
        Silent("offscreen range cannot borrow position from different player scene");
        Reset(true);ui.bark=new GameObject().Add(new FloatingBark());Silent("recycled UI owned by another bark rejected");
        Reset(true);ui.textComponent.gameObject.Parent=null;Silent("foreign label cannot borrow owned UI");
        Reset();ui.textComponent.rectTransform.Bounds=new(1910,10,100,20);Speaks("partially visible label reads with offscreen source center");
        Reset();ui.textComponent.rectTransform.Bounds=new(1920,10,100,20);Silent("only touching viewport edge has no visible area");
        Reset();ui.textComponent.rectTransform.Bounds=new(-2000,-1000,5000,3000);Speaks("label surrounding viewport visible though all corners outside");
        Reset();Screen.width=2560;Speaks("actual wider display dimensions used");
        Reset(true);Screen.width=0;Silent("unavailable display dimensions rejected");
        Reset(true);ui.textComponent.rectTransform.Bounds=new(10,10,0,20);Silent("zero-area label rejected");
        Reset(true);ui.textComponent.rectTransform.Bounds=new(float.NaN,10,100,20);Silent("nonfinite label geometry rejected");
        Reset(true);CameraManager.Main=null;Silent("missing main camera does not prove onscreen visibility");
        Reset(true);CameraManager.Main.Projection=p=>new(){x=p.x,y=p.y,z=-1};Silent("behind-camera source cannot use mirrored overlay rectangle");
        Reset(true);canvas.isActiveAndEnabled=false;Silent("disabled canvas rejected");
        Reset(true);canvas.renderMode=RenderMode.ScreenSpaceCamera;Silent("camera canvas requires actual rendering camera");
        Reset(true);canvas.renderMode=RenderMode.ScreenSpaceCamera;
        canvas.worldCamera=new GameObject().Add(new Camera{pixelRect=new(1000,0,500,500)});
        Silent("label outside offset render-camera viewport rejected");
        ui.textComponent.rectTransform.Bounds=new(995,10,100,20);Speaks("partial overlap with offset camera viewport reads");
        Reset(true);canvas.renderMode=RenderMode.WorldSpace;
        canvas.worldCamera=new GameObject().Add(new Camera());ui.textComponent.rectTransform.Depth=-1;
        Silent("world canvas rectangle behind render camera rejected");
        Reset(true);canvas.renderMode=RenderMode.WorldSpace;
        canvas.worldCamera=new GameObject().Add(new Camera());Speaks("front-facing world-space label projected with canvas camera");
        Reset();bark.transform.position=new(){x=1,z=1};CameraManager.Main=null;Speaks("nearby speech does not require onscreen proof");

        Reset(true);Speaks("initial eligible occurrence reads once");
        ui.textComponent.rectTransform.Bounds=new(2000,10,100,20);Tick();
        check(ScreenReader.Messages.Count==1&&!VisibleSubtitleAnnouncer.HasPendingContent,"offscreen change cancels pending without resetting delivered occurrence");
        ui.textComponent.rectTransform.Bounds=new(10,10,100,20);Tick();
        check(ScreenReader.Messages.Count==1,"same occurrence does not repeat on viewport reentry");
        VisibleSubtitleAnnouncer.BarkHidden(bark);VisibleSubtitleAnnouncer.BarkShown(bark);Tick();
        check(ScreenReader.Messages.Count==2,"new bark occurrence can repeat legitimate text");
        Reset();Silent("initially distant occurrence suppressed");
        ui.textComponent.rectTransform.Bounds=new(10,10,100,20);Speaks("still-current occurrence reads when it becomes visible");
        Reset();VisibleSubtitleAnnouncer.NpcMessageSubmitted("Welcome","Grael");Silent("matching queued NPC submission cannot bypass spatial rejection");
        VisibleSubtitleAnnouncer.NpcMessageSubmitted("Welcome","Grael");Silent("late matching submission cannot bypass current rejected bark");
        VisibleSubtitleAnnouncer.NpcMessageSubmitted("My own narrative","Sentinel");Speaks("independent player narrative without floating UI remains readable");
        Reset();ui.textComponent.text="Welcome";VisibleSubtitleAnnouncer.NpcMessageSubmitted("Welcome","Grael");
        Silent("unnamed rendered bark suppresses corresponding named NPC submission");
        Reset();VisibleSubtitleAnnouncer.NpcMessageSubmitted("Welcome","Leena");Speaks("different named actor submission is not consumed by rejected named bark");
        Reset(true);VisibleSubtitleAnnouncer.Update(true);ui.textComponent.rectTransform.Bounds=new(2000,10,100,20);
        Silent("pending visible bark cannot speak after leaving screen out of range");
        Reset(true);bark.secondsLeft=0;Silent("expired source remains excluded with visible rectangle");
    }
}
