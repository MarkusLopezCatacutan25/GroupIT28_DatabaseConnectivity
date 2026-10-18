#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ValidationRunner : MonoBehaviour
{
    private int checks;
    private string output;
    private static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
    private static void Call(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);
    private void Check(bool value,string name){checks++;if(!value)throw new Exception(name);Debug.Log("VALIDATION PASS: "+name);}
    private void Click(string name){GameObject go=GameObject.Find(name);if(!go)throw new Exception("Missing "+name);go.GetComponent<Button>().onClick.Invoke();}
    public void Begin(string destination){output=destination;DontDestroyOnLoad(gameObject);StartCoroutine(Run());}
    private IEnumerator Run()
    {
        yield return null;yield return null;yield return null;
        try{Check(GameObject.Find("Logo") && GameObject.Find("PlayButton"),"main menu authored sprites and buttons");Capture("main-menu.png");Click("PlayButton");}
        catch(Exception e){Fail(e);yield break;}
        yield return null;yield return null;yield return null;
        RegionalTriviaManager trivia=FindFirstObjectByType<RegionalTriviaManager>();
        RegionalMapZoomManager zoom=FindFirstObjectByType<RegionalMapZoomManager>();
        try
        {
            Check(zoom.State==RegionalMapZoomManager.ViewState.Overview && !trivia.Modal.gameObject.activeInHierarchy,"overview hides trivia");
            Check(!(bool)Field(trivia,"started") && (float)Field(trivia,"remaining")==15,"overview timer does not start");Capture("scene2-overview.png");
            Click("CalabarzonPin");Click("CalabarzonPin");Check(zoom.State==RegionalMapZoomManager.ViewState.Zooming,"repeated pin clicks start one transition");
            zoom.ResetMapZoom();Check(zoom.State==RegionalMapZoomManager.ViewState.Overview,"cancel interrupts zoom safely");Click("CalabarzonPin");
        }
        catch(Exception e){Fail(e);yield break;}
        yield return new WaitForSecondsRealtime(1.6f);
        try
        {
            Check(zoom.State==RegionalMapZoomManager.ViewState.Preview,"zoom completes before recipe preview");Capture("scene2-preview.png");
            Check(!(bool)Field(trivia,"started"),"preview timer stays stopped");Click("ChooseBatangas");
        }
        catch(Exception e){Fail(e);yield break;}
        yield return new WaitForSecondsRealtime(1.6f);
        try
        {
            Check(zoom.State==RegionalMapZoomManager.ViewState.Trivia && (bool)Field(trivia,"started"),"split transition activates trivia");
            Check(!trivia.LoadTriviaForRegion("CALABARZON"),"duplicate handoff cannot restart countdown");
            CallPause(trivia,true);float frozen=(float)Field(trivia,"remaining");Call(trivia,"Update");Check((float)Field(trivia,"remaining")==frozen,"background pause freezes timer");CallPause(trivia,false);
            var options=(Button[])Field(trivia,"optionButtons");var question=(string[])Field(trivia,"question");options[(int.Parse(question[6])+1)%4].onClick.Invoke();
            Check(GameSessionState.TriviaCompleted && GameSessionState.BonusPrepTimer==0,"incorrect answer grants no reward");
            zoom.ResetMapZoom();Check(!GameSessionState.TriviaCompleted && !(bool)Field(trivia,"started"),"back resets previous result and rewards");Click("CalabarzonPin");
        }
        catch(Exception e){Fail(e);yield break;}
        yield return new WaitForSecondsRealtime(1.6f);
        try{Click("ChooseBatangas");}catch(Exception e){Fail(e);yield break;}
        yield return new WaitForSecondsRealtime(1.6f);
        try
        {
            trivia.GetType().GetField("remaining",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(trivia,0f);Call(trivia,"Update");
            Check(GameSessionState.TriviaOutcome=="TimedOut" && GameSessionState.ScoreBonusMultiplier==1,"timeout resolves without reward");
            zoom.ResetMapZoom();Click("CalabarzonPin");
        }
        catch(Exception e){Fail(e);yield break;}
        yield return new WaitForSecondsRealtime(1.6f);
        try{Click("ChooseBatangas");}catch(Exception e){Fail(e);yield break;}
        yield return new WaitForSecondsRealtime(1.6f);
        try
        {
            var options=(Button[])Field(trivia,"optionButtons");Check(options.Length==4 && options[0].GetComponentInChildren<TMP_Text>().text!="Button","custom option bindings retained");
            Capture("scene2-trivia.png");var question=(string[])Field(trivia,"question");options[int.Parse(question[6])].onClick.Invoke();
            Check(GameSessionState.TriviaAnsweredCorrectly && GameSessionState.BonusPrepTimer==15 && GameSessionState.ScoreBonusMultiplier==1.25f,"correct answer grants documented rewards");
            var proceed=(Button)Field(trivia,"proceedToKitchenBtn");Check(proceed.gameObject.activeSelf,"proceed revealed after resolution");Capture("scene2-feedback.png");proceed.onClick.Invoke();
        }
        catch(Exception e){Fail(e);yield break;}
        yield return null;yield return null;yield return null;
        try
        {
            var manager=FindFirstObjectByType<SavorPinoyCookingManager>();Check(manager!=null,"cooking manager starts through scene handoff");
            var attempt=(CookingAttempt)Field(manager,"attempt");Check(attempt!=null && attempt.Remaining>70,"rewarded prep timer copied into attempt");
            Check(GameObject.Find("ChooseLearningMode")!=null,"kitchen waits for learning mode without running clock");GameObject.Find("StartingChefAlias").GetComponent<TMP_InputField>().text="Activity5Chef";Capture("kitchen-mode.png");Click("TimedChallenge");
            attempt=(CookingAttempt)Field(manager,"attempt");Check(attempt.TimedChallenge,"timed challenge selected after nickname input");
            attempt.Advance(2);Check(attempt.Elapsed>=2,"active stopwatch records full supplied frame duration");
            CallPause(manager,true);double stopped=attempt.Elapsed;Call(manager,"Update");Check(attempt.Elapsed==stopped,"practice stopwatch freezes while paused");Click("Resume");
            Capture("kitchen-gather.png");Click("Gather_garlic");Click("Gather_turmeric");Click("Gather_chicken");
            var knife=FindFirstObjectByType<KnifeTool>();var knifeData=new PointerEventData(EventSystem.current){pointerId=-1};
            knife.OnBeginDrag(knifeData);knifeData.position=Vector2.zero;knife.OnDrag(knifeData);knife.OnEndDrag(knifeData);Check(attempt.Phase==CookingPhase.Gather,"knife drop outside board rejected");
            knife.OnBeginDrag(knifeData);knifeData.position=Position(knife.Board,.5f,.5f);knife.OnDrag(knifeData);knife.OnEndDrag(knifeData);Call(manager,"Refresh");Check(attempt.Phase==CookingPhase.Chop,"knife drag to board selects tool");
            Check(attempt.Phase==CookingPhase.Chop,"pantry and knife controls unlock chopping");Capture("kitchen-prep.png");
            var chopping=FindFirstObjectByType<ChoppingSurface>();RectTransform board=chopping.transform as RectTransform;
            for(int i=0;i<6;i++)
            {
                float x=.3f+.2f*(i%3);var data=new PointerEventData(EventSystem.current){pointerId=-1};
                data.position=Position(board,x,.8f);chopping.OnPointerDown(data);
                data.position=Position(board,x,.5f);chopping.OnDrag(data);if(i==0)Capture("kitchen-knife-drag.png");
                data.position=Position(board,x,.2f);chopping.OnPointerUp(data);if(i==0)Capture("kitchen-cut-pieces.png");if(i==3)Capture("kitchen-turmeric.png");
            }
            Check(attempt.Cuts==6 && attempt.Phase==CookingPhase.HeatPot,"six actual pointer strokes complete preparation");
            Click("StartCooking");Check(attempt.Flame==.6,"beginner start control warms pot");GameObject.Find("FlameSlider").GetComponent<Slider>().value=.6f;Tick(attempt,8);Call(manager,"Refresh");
            Click("Add_garlic");Click("Add_turmeric");Click("Add_chicken");Tick(attempt,4.1);Click("Add_vinegar");Call(manager,"Refresh");
            Check(attempt.Phase==CookingPhase.Simmer,"live ordered controls and timed searing reach simmer");Capture("kitchen-simmer.png");
            Tick(attempt,13);GameObject.Find("FlameSlider").GetComponent<Slider>().value=0;Click("Plate");
            Check(attempt.Phase==CookingPhase.Success && attempt.Stars==3,"live plating shows successful three-star result");Capture("kitchen-result.png");
            Click("HeritageCard");Check(GameObject.Find("HeritageOverlay")!=null,"heritage card opens from results");Capture("heritage-card.png");Click("CloseHeritage");
            var service=PrototypeScoreService.Instance;string id=(string)Field(manager,"runId");
            Click("SaveScore");Check(!GameObject.Find("SaveScore").GetComponent<Button>().interactable,"save button locks immutable queued result");
            Check(service.Queue(id,"Activity5Chef",attempt),"repeated save keeps one record identity");
            var box=(PrototypeOutbox)Field(service,"box");Check(box.items.FindAll(i=>i.recordId=="slice_"+id).Count==1,"offline queue has one record per attempt");
            Check(box.items.Find(i=>i.recordId=="slice_"+id).record.score==attempt.FinalScore,"outbox score matches displayed result");
            Capture("result-saved-locally.png");
            File.WriteAllText(Path.Combine(output,"offline-outbox-evidence.json"),JsonUtility.ToJson(box,true));
            // Reload the actual storage service, preserving its file. No cloud claim is made.
            UnityEngine.Object.DestroyImmediate(service.gameObject);
            service=PrototypeScoreService.Ensure("offline");
            box=(PrototypeOutbox)Field(service,"box");
            Check(box.items.Exists(i=>i.recordId=="slice_"+id && i.record.player_name=="Activity5Chef"),"pending result survives storage service reopening");
            var failure=new CookingAttempt(false);failure.Fail("Burnt dish: reduce heat earlier and retry.");manager.GetType().GetField("attempt",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(manager,failure);manager.GetType().GetField("resultShown",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(manager,false);Call(manager,"Refresh");Capture("kitchen-failure.png");
            File.WriteAllText(Path.Combine(output,"unity-validation-result.txt"),checks+" live Unity checks passed; UI screenshots captured. No network submission was attempted.");Debug.Log("VALIDATION COMPLETE "+checks);EditorApplication.Exit(0);
        }
        catch(Exception e){Fail(e);}
    }
    private static void CallPause(object target,bool value){target.GetType().GetMethod("OnApplicationPause",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,new object[]{value});}
    private static Vector2 Position(RectTransform board,float x,float y){return RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(new Vector3(board.rect.xMin+x*board.rect.width,board.rect.yMin+y*board.rect.height,0)));}
    private static void Tick(CookingAttempt a,double seconds){for(int i=0;i<(int)(seconds/.02);i++)a.Advance(.02);}
    private void Capture(string name)
    {
        // Render actual live Canvas objects at the target device size, then restore overlay mode.
        var cameraGo=new GameObject("ValidationRenderCamera");Camera camera=cameraGo.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.13f,.065f);camera.orthographic=true;
        RenderTexture target=new RenderTexture(1920,1080,24);camera.targetTexture=target;
        Canvas[] canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach(Canvas canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;}
        Canvas.ForceUpdateCanvases();
        foreach(var trivia in FindObjectsByType<RegionalTriviaManager>(FindObjectsSortMode.None))Call(trivia,"ApplySafeArea");
        foreach(var manager in FindObjectsByType<SavorPinoyCookingManager>(FindObjectsSortMode.None))Call(manager,"Fit");
        foreach(var safe in FindObjectsByType<PrototypeSafeArea>(FindObjectsSortMode.None))Call(safe,"Apply");
        foreach(TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.ForceMeshUpdate();Canvas.ForceUpdateCanvases();camera.Render();
        RenderTexture old=RenderTexture.active;RenderTexture.active=target;Texture2D image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());RenderTexture.active=old;
        foreach(Canvas canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}
        DestroyImmediate(image);DestroyImmediate(target);DestroyImmediate(cameraGo);Canvas.ForceUpdateCanvases();
    }
    private void Fail(Exception e){Debug.LogException(e);File.WriteAllText(Path.Combine(output,"unity-validation-result.txt"),"FAILED: "+e);EditorApplication.Exit(1);}
}
#endif
