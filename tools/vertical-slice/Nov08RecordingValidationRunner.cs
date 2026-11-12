#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class Nov03ValidationEntry
{
    private static bool spawned;
    static Nov03ValidationEntry(){EditorApplication.update+=Poll;}
    public static void Run(){EditorSceneManager.playModeStartScene=null;bool hub=Environment.GetEnvironmentVariable("NOV03_CASE")=="Hub";EditorSceneManager.OpenScene("Assets/Scenes/"+(hub?"RegionalTriviaHub":"CookingStage")+".unity");if(hub)UnityEngine.Object.FindFirstObjectByType<RegionalTriviaManager>().enabled=false;else UnityEngine.Object.FindFirstObjectByType<SavorPinoyCookingManager>().enabled=false;File.WriteAllText("nov03-validation.request","run");EditorApplication.EnterPlaymode();}
    private static void Poll(){if(spawned||!EditorApplication.isPlaying||!File.Exists("nov03-validation.request"))return;spawned=true;File.Delete("nov03-validation.request");new GameObject("Nov03Validation").AddComponent<Nov03InteractionValidationRunner>().Begin();}
}
public sealed class Nov03InteractionValidationRunner : MonoBehaviour
{
    private sealed class Identity : IChefIdentity {public string Uid=>"isolated-ui-test";public Task<string> SignInAsync()=>Task.FromResult(Uid);}
    private sealed class NoNetwork : IAttemptRepository{public Task WriteAsync(PendingChefAttempt p)=>throw new Exception("Network prohibited in UI checks");public Task<AttemptRecord> ReadAsync(string u,string id)=>throw new Exception("Network prohibited in UI checks");}
    [Serializable] private sealed class Report{public string scope="ISOLATED UNITY INTERACTION TEST; NO LIVE AUTH OR DATABASE PROOF";public int passed;public string[] checks;public string error;}
    private readonly List<string> checks=new List<string>();private string output;
    private static object Field(object o,string n)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    private static void Call(object o,string n)=>o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
    private void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add(label);Debug.Log("NOV03 PASS "+label);}
    private static void Click(string name){var b=GameObject.Find(name)?.GetComponent<Button>();if(!b||!b.interactable)throw new Exception("Unavailable button "+name);b.onClick.Invoke();}
    private static Vector2 Point(RectTransform r,float x=.5f,float y=.5f)=>RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(new Vector3(r.rect.xMin+r.rect.width*x,r.rect.yMin+r.rect.height*y)));
    private static void Drag(IngredientDrag drag,bool valid=true){Canvas.ForceUpdateCanvases();var data=new PointerEventData(EventSystem.current){pointerId=-1,position=Point((RectTransform)drag.transform)};drag.OnBeginDrag(data);data.position=valid?Point(drag.Target):new Vector2(-100,-100);drag.OnDrag(data);drag.OnEndDrag(data);}
    private static void Stroke(ChoppingSurface surface,int cut){var r=(RectTransform)surface.transform;float x=.3f+.2f*(cut%3);var d=new PointerEventData(EventSystem.current){pointerId=-1,position=Point(r,x,.8f)};surface.OnPointerDown(d);d.position=Point(r,x,.5f);surface.OnDrag(d);d.position=Point(r,x,.2f);surface.OnPointerUp(d);}
    private static void Tick(CookingAttempt a,double seconds){for(int i=0;i<(int)(seconds/.02);i++)a.Advance(.02);}
    public async void Begin()
    {
        output=Environment.GetEnvironmentVariable("NOV03_OUTPUT");Directory.CreateDirectory(output);var report=new Report();
        try{var identity=new Identity();ChefAccessSession.Authorize(identity,"Preview Chef");
            if(Environment.GetEnvironmentVariable("NOV03_CASE")=="Hub")await Hub();else if(Environment.GetEnvironmentVariable("NOV03_CASE")=="Results")await ResultStates();else await Cooking(identity);
            report.passed=checks.Count;report.checks=checks.ToArray();File.WriteAllText(Path.Combine(output,"results.json"),JsonUtility.ToJson(report,true));EditorApplication.Exit(0);
        }catch(Exception e){report.passed=checks.Count;report.checks=checks.ToArray();report.error=e.ToString();File.WriteAllText(Path.Combine(output,"results.json"),JsonUtility.ToJson(report,true));Debug.LogException(e);EditorApplication.Exit(1);}
    }
    private async Task Hub()
    {
        var owner=FindFirstObjectByType<RegionalTriviaManager>();owner.enabled=true;await Task.Delay(180);
        var zoom=owner.GetComponent<RegionalMapZoomManager>();Check(zoom.State==RegionalMapZoomManager.ViewState.Overview,"overview starts without a trivia timer");Capture("map-overview.png");
        foreach(string province in new[]{"Batangas","Quezon"}){
            zoom.ResetMapZoom();GameSessionState.SelectedProvince=province=="Batangas"?"Quezon":"Batangas";GameSessionState.QueueNextProvince();Click("CalabarzonPin");
            Check(GameObject.Find("TravelCloudCurtain")?.GetComponent<CanvasRenderer>(),"pin starts a renderable cloud curtain: "+province);await Task.Delay(250);Capture("cloud-"+province+".png");await Task.Delay(1400);
            Check(zoom.State==RegionalMapZoomManager.ViewState.Preview&&owner.SelectedProvince==province,"province preview preserves selected location: "+province);if(province=="Quezon")Check(GameObject.Find("QuezonCardFrame")?.GetComponent<CanvasRenderer>(),"Quezon card has a native UI renderer");Capture("preview-"+province+".png");Click("ChooseBatangas");await Task.Delay(1600);
            Check(zoom.State==RegionalMapZoomManager.ViewState.Trivia&&owner.Modal.parent.name=="RightTriviaPanel","trivia follows map transition in the authored right panel: "+province);
            var left=(RectTransform)Field(zoom,"left");var map=(RectTransform)Field(zoom,"map");Check(map.rect.width>=left.rect.width&&map.rect.height>=left.rect.height,"aspect-filled map has no empty blue bands: "+province);
            Capture("trivia-question-"+province+".png");var q=(ProvinceQuestion)Field(owner,"question");var options=(Button[])Field(owner,"optionButtons");options[(q.Correct+1)%4].onClick.Invoke();
            Check(!GameSessionState.TryConsumeKitchen(out _,out _),"wrong answer cannot enter cooking: "+province);Capture("trivia-retry-"+province+".png");
            ((Button)Field(owner,"proceedToKitchenBtn")).onClick.Invoke();Check(!(bool)Field(owner,"resolved"),"retry reopens question: "+province);
            options[q.Correct].onClick.Invoke();Check(GameSessionState.TriviaAnsweredCorrectly&&GameSessionState.TargetDish==RecipePlan.For(province).Dish,"correct answer unlocks matching recipe: "+province);Capture("trivia-correct-"+province+".png");
        }
    }
    private async Task Cooking(Identity identity)
    {
        string province=Environment.GetEnvironmentVariable("NOV03_CASE");GameSessionState.BeginRun(province);GameSessionState.ResolveTrivia("preview","Correct");
        var manager=FindFirstObjectByType<SavorPinoyCookingManager>(FindObjectsInactive.Include);var legacy=(Canvas)Field(manager,"legacyCanvas");if(legacy)legacy.gameObject.SetActive(false);var art=manager.GetComponent<KitchenArt>();
        Check(art.Atlas(0)&&art.Atlas(11),"new preparation atlas resolves at runtime");
        var model=new CookingAttempt(true,false,120,RecipePlan.For(province));var view=manager.gameObject.AddComponent<CookingStageView>();view.Initialize(model,TMP_Settings.defaultFontAsset);
        var resultView=new CookingResultView(view,art);var save=new AttemptSaveCoordinator(identity,new NoNetwork(),new ChefAttemptOutbox(Path.Combine(output,"mock-outbox.json")));var presenter=new CookingResultPresenter(resultView,save,new AttemptQueryController(),null,identity);var controller=manager.gameObject.AddComponent<CookingStageController>();controller.Initialize(view,presenter,identity,model,Guid.NewGuid().ToString("N"),true);
        Check(GameObject.Find("StartingChefAlias").GetComponent<TMP_InputField>().textComponent.alignment==TextAlignmentOptions.Center,"chef name is centered");Capture("learning-mode.png");Click("GuidedPractice");model=controller.Attempt;
        foreach(string id in model.Recipe.Pantry)if(id!="soy")Click("Gather_"+id);Click("Knife");
        model.Cut(1);Check(model.Cuts==0,"knife strokes cannot bypass initial preparation");
        int guard=0;while(model.Phase==CookingPhase.Chop&&guard++<70){var step=model.Preparation.Current;int index=model.Preparation.Index,rep=model.Preparation.Repetition;
            if(guard==1){
                var progress=GameObject.Find("PreparationStepCount").GetComponent<TMP_Text>().rectTransform;
                var title=GameObject.Find("CloseupTitle").GetComponent<TMP_Text>().rectTransform;
                Check(progress.anchoredPosition.y-progress.rect.height/2>title.anchoredPosition.y+title.rect.height/2,"preparation progress clears step heading");
            }
            if(!step.IsCut){var drag=GameObject.Find("PreparationSource").GetComponent<IngredientDrag>();Drag(drag,false);Check(model.Preparation.Index==index&&model.Preparation.Repetition==rep,"outside preparation drop makes no progress: "+step.Id);Drag(drag);}
            else Stroke(FindFirstObjectByType<ChoppingSurface>(),model.Cuts);
            Call(controller,"Render");if(rep==0)Capture("prepare-"+step.Id+".png");
        }
        Check(model.Phase==CookingPhase.HeatPot&&model.Cuts==model.Preparation.RequiredCuts,"all ingredient preparation and cleanup are required");Check(!GameObject.Find("Add_garlic"),"legacy add buttons are removed");Capture("ingredients-ready.png");
        Click("AdjustHeat");var focus=manager.GetComponent<CookingFocusView>();Check(GameObject.Find("CookingFocus"),"heat control opens a centered stove view");Capture("stove-focus.png");Click("CloseCookingFocus");Click("StartCooking");Tick(model,8);Call(controller,"Render");
        while(model.Phase<CookingPhase.Plate&&!model.Terminal){if(model.Phase==CookingPhase.Sear){
                Call(controller,"Render");var instruction=(TMP_Text)Field(view,"objectiveText");
                Check(province=="Quezon"?instruction.text.Contains("Stir-fry")&&!instruction.text.Contains("vinegar"):instruction.text.Contains("before vinegar"),"cooking instruction matches province: "+province);
                Capture("stir-fry-or-sear.png");Tick(model,4.2);Call(controller,"Render");continue;}if(model.Phase==CookingPhase.Simmer){Tick(model,13);Call(controller,"Render");continue;}
            string id=model.ExpectedIngredient;int count=model.AdditionIndex;var drag=GameObject.Find("Drag_"+id).GetComponent<IngredientDrag>();Drag(drag,false);Check(model.AdditionIndex==count,"outside cooking drop adds nothing: "+id);
            var data=new PointerEventData(EventSystem.current){pointerId=-1,position=Point((RectTransform)drag.transform)};drag.OnBeginDrag(data);await Task.Delay(400);Capture("container-focus-"+id+".png");data.position=Point(drag.Target);drag.OnDrag(data);drag.OnEndDrag(data);await Task.Delay(650);Call(controller,"Render");Check(model.AdditionIndex==count+1,"valid animated drop adds once: "+id);
        }
        model.SetFlame(0);Call(controller,"Render");Click("Plate");Check(model.Phase==CookingPhase.Success,"complete recipe reaches successful result: "+province);Check(AttemptContract.From(model).dish_id==model.Recipe.Id,"save contract uses the selected dish");Check(!resultView.ContinueButton.interactable,"Continue still requires verified Firebase save");Capture("result.png");
    }
    private sealed class MutableIdentity : IChefIdentity {public string Current="isolated-result-preview";public string Uid=>Current;public Task<string> SignInAsync()=>Task.FromResult(Current);}
    private sealed class PreviewRepository : IAttemptRepository
    {
        public bool Denied;public int Writes;public PendingChefAttempt Item;
        public TaskCompletionSource<bool> WritePending;
        public TaskCompletionSource<AttemptRecord> ReadPending;
        public Task WriteAsync(PendingChefAttempt item){Writes++;Item=item;if(Denied)throw new Exception("Permission denied");return WritePending?.Task??Task.CompletedTask;}
        public Task<AttemptRecord> ReadAsync(string uid,string id)=>ReadPending?.Task??Task.FromResult(Item.record);
    }
    private static async Task Until(Func<bool> ready){for(int i=0;i<200;i++){if(ready())return;await Task.Delay(20);}throw new TimeoutException("UI state did not settle");}
    private static CookingAttempt Finished(RecipePlan recipe,bool failure=false)
    {
        var a=new CookingAttempt(true,false,120,recipe);if(failure)for(int i=0;i<4;i++)a.Gather("not-in-recipe");foreach(string id in recipe.Pantry)if(id!="soy")a.Gather(id);a.ChooseKnife();
        while(a.Phase==CookingPhase.Chop){if(a.Preparation.Current.IsCut)a.Cut(.896);else a.Prepare(a.Preparation.Current.Id);}
        a.SetFlame(.6);Tick(a,8);while(a.Phase<CookingPhase.Plate&&!a.Terminal){if(a.Phase==CookingPhase.Sear)Tick(a,4.2);else if(a.Phase==CookingPhase.Simmer)Tick(a,13);else a.AddToPot(a.ExpectedIngredient);}a.SetFlame(0);a.Plate();return a;
    }
    private static TMP_Text Label(CookingStageView view,string name){foreach(var t in view.Design.GetComponentsInChildren<TMP_Text>(true))if(t.name==name)return t;throw new Exception("Missing text "+name);}
    private async Task ResultStates()
    {
        var manager=FindFirstObjectByType<SavorPinoyCookingManager>(FindObjectsInactive.Include);var legacy=(Canvas)Field(manager,"legacyCanvas");if(legacy)legacy.gameObject.SetActive(false);var art=manager.GetComponent<KitchenArt>();
        var identity=new MutableIdentity();ChefAccessSession.Authorize(identity,"Preview Chef");GameSessionState.BeginRun("Quezon");
        var model=Finished(RecipePlan.Quezon);var view=manager.gameObject.AddComponent<CookingStageView>();view.Initialize(model,TMP_Settings.defaultFontAsset);
        var resultView=new CookingResultView(view,art);var repository=new PreviewRepository();var save=new AttemptSaveCoordinator(identity,repository,new ChefAttemptOutbox(Path.Combine(output,"result-preview-outbox.json")));var query=new AttemptQueryController();var presenter=new CookingResultPresenter(resultView,save,query,null,identity);
        int routed=0;presenter.RetryRequested+=()=>routed++;presenter.Show(model,Guid.NewGuid().ToString("N"),"Preview Chef");
        Check(!resultView.ContinueButton.interactable,"successful recipe waits for verified save");Check(GameObject.Find("ResultStars").GetComponent<CanvasRenderer>(),"rating stars have a native UI renderer");Capture("result-unsaved.png");
        repository.Denied=true;resultView.SaveButton.onClick.Invoke();await Until(()=>save.State==AttemptSaveState.Error);string id=repository.Item.attemptId;
        Check(!resultView.ContinueButton.interactable&&Label(view,"ResultStatusHeading").text=="SAVE NEEDS ATTENTION","permission failure explains why next province is locked");Capture("result-save-error.png");resultView.ContinueButton.onClick.Invoke();Check(!((RectTransform)Field(resultView,"heritage")).gameObject.activeSelf,"forced Continue event cannot bypass failed save");
        repository.Denied=false;repository.WritePending=new TaskCompletionSource<bool>();repository.ReadPending=new TaskCompletionSource<AttemptRecord>();resultView.SaveButton.onClick.Invoke();resultView.SaveButton.onClick.Invoke();
        await Until(()=>save.State==AttemptSaveState.Saving);Check(repository.Writes==2&&!resultView.SaveButton.interactable,"duplicate save click stays locked while saving");Check(repository.Item.attemptId==id,"retry retains the failed attempt ID");Capture("result-saving.png");
        repository.WritePending.SetResult(true);await Until(()=>save.State==AttemptSaveState.ReadingBack);Check(!resultView.ContinueButton.interactable,"acknowledged write still waits for read-back");Capture("result-reading-back.png");repository.ReadPending.SetResult(repository.Item.record);await Until(()=>save.SaveSucceeded);
        Check(resultView.ContinueButton.interactable&&Label(view,"ResultStatusHeading").text=="NEXT PROVINCE READY","matching read-back unlocks a clear next action");Capture("result-verified.png");
        resultView.ContinueButton.onClick.Invoke();Check(((RectTransform)Field(resultView,"heritage")).gameObject.activeSelf,"verified Continue opens heritage");Capture("heritage-complete.png");Click("CloseHeritage");resultView.ContinueButton.onClick.Invoke();Check(((RectTransform)Field(resultView,"heritage")).gameObject.activeSelf,"heritage can reopen after closing it");Click("NextProvince");Check(routed==1&&GameSessionState.NextProvince=="Batangas","verified Next province requests the other location through presenter");Click("CloseHeritage");
        var open=resultView.GetType().GetMethod("OpenQuery",BindingFlags.Instance|BindingFlags.NonPublic);open.Invoke(resultView,new object[]{"MY RECENT ATTEMPTS"});
        var pending=new TaskCompletionSource<List<string>>();var loading=query.LoadAsync(()=>pending.Task);Check(!resultView.HistoryButton.interactable&&!GameObject.Find("ReloadAttempts").GetComponent<Button>().interactable,"query loading disables both outer and inline reload actions");Capture("history-loading.png");pending.SetResult(new List<string>());await loading;Check(Label(view,"AttemptQueryRows").text=="No chef attempts recorded yet","empty history remains an explicit valid state");Capture("history-empty.png");
        await query.LoadAsync(()=>Task.FromException<List<string>>(new Exception("network unavailable")));Check(GameObject.Find("ReloadAttempts").GetComponent<Button>().interactable,"query failure leaves an accessible retry action inside overlay");Capture("history-error.png");await query.LoadAsync(()=>Task.FromResult(new List<string>{"Pancit Habhab / Quezon\n9,000 pts / Correct\n3 November 2026"}));Capture("history-loaded.png");Click("CloseHistory");
        identity.Current="different-preview-profile";presenter.IdentityChanged();Check(!resultView.ContinueButton.interactable,"UID change revokes result progression");resultView.ContinueButton.onClick.Invoke();Check(!((RectTransform)Field(resultView,"heritage")).gameObject.activeSelf,"changed UID cannot reopen heritage");Capture("result-profile-changed.png");presenter.Dispose();
        DestroyImmediate(view.Design.GetComponentInParent<Canvas>().gameObject);DestroyImmediate(view);identity.Current="isolated-result-preview";ChefAccessSession.Authorize(identity,"Preview Chef");GameSessionState.BeginRun("Batangas");
        model=Finished(RecipePlan.Batangas,true);view=manager.gameObject.AddComponent<CookingStageView>();view.Initialize(model,TMP_Settings.defaultFontAsset);resultView=new CookingResultView(view,art);repository=new PreviewRepository();save=new AttemptSaveCoordinator(identity,repository,new ChefAttemptOutbox(Path.Combine(output,"practice-preview-outbox.json")));presenter=new CookingResultPresenter(resultView,save,new AttemptQueryController(),null,identity);presenter.Show(model,Guid.NewGuid().ToString("N"),"Preview Chef");
        Check(model.FinalScore==7300&&model.Stars==0,"high-score failure reproduces four-mistake screenshot case");Check(Label(view,"SaveStatus").text.Contains("4 mistakes"),"practice feedback explains the completion failure");Capture("practice-feedback.png");resultView.SaveButton.onClick.Invoke();await Until(()=>save.SaveSucceeded);Check(!resultView.ContinueButton.interactable&&Label(view,"SaveStatus").text.Contains("practice result is saved"),"saving a failed practice attempt does not claim level completion");Capture("practice-saved.png");Click("RetryStage");Check(GameSessionState.NextProvince=="Batangas","retry preserves the current recipe instead of randomly switching province");presenter.Dispose();
    }

    private void Capture(string filename)
    {
        var go=new GameObject("EvidenceCamera");var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.2f,.1f,.04f);var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=10;}Canvas.ForceUpdateCanvases();foreach(var t in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))t.ForceMeshUpdate();camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var pic=new Texture2D(1920,1080,TextureFormat.RGB24,false);pic.ReadPixels(new Rect(0,0,1920,1080),0,0);pic.Apply();File.WriteAllBytes(Path.Combine(output,filename),pic.EncodeToPNG());RenderTexture.active=old;foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}camera.targetTexture=null;DestroyImmediate(pic);DestroyImmediate(target);DestroyImmediate(go);Canvas.ForceUpdateCanvases();
    }
}
#endif
