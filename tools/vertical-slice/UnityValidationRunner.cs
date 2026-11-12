#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Firebase.Auth;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class VerticalSliceValidationEntry
{
    private static bool spawned;
    static VerticalSliceValidationEntry(){EditorApplication.update+=Poll;}
    public static void Run(){File.WriteAllText("vertical-validation.request","run");EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.EnterPlaymode();}
    private static void Poll()
    {
        if(spawned||!EditorApplication.isPlaying||!File.Exists("vertical-validation.request"))return;
        spawned=true;File.Delete("vertical-validation.request");if(UnityEngine.Object.FindFirstObjectByType<VerticalSliceValidationRunner>())return;new GameObject("VerticalSliceValidationRunner").AddComponent<VerticalSliceValidationRunner>().Begin();
    }
}

public sealed class VerticalSliceValidationRunner : MonoBehaviour
{
    [Serializable] private sealed class Evidence { public string scope="UNITY SDK + LOCAL AUTH/RTDB EMULATORS ONLY";public int passed;public string uid,attemptId;public AttemptRecord record;public string[] checks;public string error; }
    private readonly List<string> checks=new List<string>();
    private string output;
    private static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
    private void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add(label);Debug.Log("VERTICAL PASS: "+label);}
    private static void Click(string name){var go=GameObject.Find(name);if(!go)throw new Exception("Missing active button: "+name);var button=go.GetComponent<Button>();if(!button.interactable)throw new Exception("Disabled button: "+name);button.onClick.Invoke();}
    private static void Render(CookingStageController controller)=>controller.GetType().GetMethod("Render",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(controller,null);
    private static void Tick(CookingAttempt a,double seconds){for(int i=0;i<(int)(seconds/.02);i++)a.Advance(.02);}
    private static Vector2 Point(RectTransform rect,float x,float y)=>RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(rect.rect.xMin+x*rect.rect.width,rect.rect.yMin+y*rect.rect.height,0)));
    private async Task Wait(Func<bool> ready,string label,int milliseconds=30000)
    {
        float start=Time.realtimeSinceStartup;while(!ready()){if((Time.realtimeSinceStartup-start)*1000>milliseconds)throw new TimeoutException(label+" | "+(GameObject.Find("SaveStatus")?.GetComponent<TMP_Text>().text??GameObject.Find("SignInStatus")?.GetComponent<TMP_Text>().text??""));await Task.Delay(50);}
    }
    public async void Begin()
    {
        DontDestroyOnLoad(gameObject);output=Environment.GetEnvironmentVariable("VERTICAL_UNITY_OUTPUT");Directory.CreateDirectory(output);
        var evidence=new Evidence{scope=Environment.GetEnvironmentVariable("VERTICAL_BACKEND_SCOPE")??"UNITY SDK + LOCAL AUTH/RTDB EMULATORS ONLY"};
        try
        {
            await Wait(()=>GameObject.Find("PlayButton")!=null,"Main Menu becomes active");
            Check(!ChefAccessSession.HasAccess,"recipe access starts locked before visible sign-in");
            UnityEngine.SceneManagement.SceneManager.LoadScene("RegionalTriviaHub");await Task.Delay(300);
            await Wait(()=>GameObject.Find("PlayButton")!=null,"unauthorized hub returns to Main Menu");Check(!ChefAccessSession.HasAccess,"direct hub launch cannot bypass sign-in");
            UnityEngine.SceneManagement.SceneManager.LoadScene("CookingStage");await Task.Delay(300);
            await Wait(()=>GameObject.Find("PlayButton")!=null,"unauthorized kitchen returns to Main Menu");Check(!ChefAccessSession.HasAccess,"direct kitchen launch cannot bypass sign-in");
            Click("PlayButton");Check(GameObject.Find("ChefSignInOverlay")!=null,"Play opens a visible sign-in screen before recipe selection");Capture("sign-in-ready.png");
            GameObject.Find("SignInChefAlias").GetComponent<TMP_InputField>().text="VerticalSliceChef";Click("GuestSignInButton");
            Check(!GameObject.Find("GuestSignInButton").GetComponent<Button>().interactable&&!ChefAccessSession.HasAccess,"sign-in disables duplicate clicks and keeps gameplay locked");Capture("sign-in-loading.png");
            Click("BackFromSignIn");await Task.Delay(200);Check(GameObject.Find("PlayButton")!=null&&!ChefAccessSession.HasAccess,"Back prevents late authentication from entering gameplay");
            // Restarted local Auth emulators do not retain prior users; clear only that test account cache.
            var loginConfig=(FirebaseSettings)Field(FindFirstObjectByType<MainMenuManager>(),"firebaseSettings");
            if(loginConfig.useLocalEmulators){var resetBackend=new FirebaseBootstrap(loginConfig);await resetBackend.InitializeAsync();FirebaseAuth.GetAuth(resetBackend.App).SignOut();}
            Click("PlayButton");GameObject.Find("SignInChefAlias").GetComponent<TMP_InputField>().text="VerticalSliceChef";Click("GuestSignInButton");
            await Wait(()=>GameObject.Find("CalabarzonPin")!=null,"authenticated recipe selection",45000);Check(ChefAccessSession.HasAccess&&ChefAccessSession.DisplayName=="VerticalSliceChef","confirmed Firebase sign-in unlocks recipe selection");
            Click("CalabarzonPin");await Task.Delay(1800);Click("ChooseBatangas");await Task.Delay(1800);
            var trivia=FindFirstObjectByType<RegionalTriviaManager>();var question=(string[])Field(trivia,"question");var options=(Button[])Field(trivia,"optionButtons");options[int.Parse(question[6])].onClick.Invoke();
            ((Button)Field(trivia,"proceedToKitchenBtn")).onClick.Invoke();
            await Wait(()=>FindFirstObjectByType<CookingStageController>()!=null,"scene handoff");await Task.Delay(100);
            var controller=FindFirstObjectByType<CookingStageController>();
            Check(controller!=null&&GameObject.Find("StartingChefAlias"),"real scene handoff composes separated components");
            var chefName=GameObject.Find("StartingChefAlias").GetComponent<TMP_InputField>();Check(chefName.text=="VerticalSliceChef"&&!chefName.interactable,"chef name carries forward as display text without re-entry");Click("TimedChallenge");
            var attempt=controller.Attempt;Check(attempt.TimedChallenge&&attempt.Remaining>120,"challenge retains one-time trivia reward");
            Capture("kitchen-mode-ready.png");Click("Gather_garlic");Click("Gather_turmeric");Click("Gather_chicken");Click("Knife");
            var chopping=FindFirstObjectByType<ChoppingSurface>();var board=(RectTransform)chopping.transform;
            Capture("kitchen-preparation.png");
            for(int i=0;i<6;i++)
            {
                float x=.3f+.2f*(i%3);var pointer=new PointerEventData(EventSystem.current){pointerId=-1,position=Point(board,x,.8f)};chopping.OnPointerDown(pointer);pointer.position=Point(board,x,.5f);chopping.OnDrag(pointer);pointer.position=Point(board,x,.2f);chopping.OnPointerUp(pointer);
            }
            Check(attempt.Cuts==6&&attempt.Phase==CookingPhase.HeatPot,"actual pointer cuts advance the domain model");
            Click("StartCooking");Tick(attempt,8);Render(controller);Click("Add_garlic");Click("Add_turmeric");Click("Add_chicken");Tick(attempt,4.1);Render(controller);Click("Add_vinegar");Tick(attempt,13);Render(controller);GameObject.Find("FlameSlider").GetComponent<Slider>().value=0;Click("Plate");
            Check(attempt.Phase==CookingPhase.Success&&attempt.FinalScore==9375,"correct end-to-end cooking produces expected rubric score");
            var next=GameObject.Find("ContinueButton").GetComponent<Button>();Check(!next.interactable,"correct cooking alone does not allow stage completion");Capture("result-before-save.png");
            var saveTracker=(AttemptSaveCoordinator)Field((CookingResultPresenter)Field(controller,"result"),"save");saveTracker.Changed+=()=>Debug.Log("SAVE FLOW STATE: "+saveTracker.State);
            Click("SaveAttemptButton");Check(!next.interactable&&!GameObject.Find("SaveAttemptButton").GetComponent<Button>().interactable,"save disables duplicate clicks and Continue");Capture("result-saving.png");
            await Wait(()=>next.interactable,"Firebase acknowledgement + read-back",45000);
            Check(GameObject.Find("SaveStatus").GetComponent<TMP_Text>().text.Contains("read back successfully"),"real Firebase Unity SDK write and read-back confirm save");Capture("result-confirmed.png");
            var settings=UnityEngine.Object.FindFirstObjectByType<SavorPinoyCookingManager>();var config=(FirebaseSettings)Field(settings,"firebaseSettings");
            var backend=new FirebaseBootstrap(config);var auth=new FirebaseAuthManager(backend);string uid=await auth.SignInAsync();
            Check(!string.IsNullOrEmpty(uid)&&uid!="VerticalSliceChef","Firebase Auth UID is independent of nickname");
            Click("LoadAttemptsButton");Check(!GameObject.Find("LoadAttemptsButton").GetComponent<Button>().interactable,"history shows Loading and disables duplicate clicks");Capture("history-loading.png");
            await Wait(()=>GameObject.Find("LoadAttemptsButton").GetComponent<Button>().interactable,"history query");string text=GameObject.Find("AttemptQueryRows").GetComponent<TMP_Text>().text;
            Match match=Regex.Match(text,@"attempt_[a-f0-9]{32}");Check(match.Success&&text.Contains("9375")&&text.Contains(AttemptContract.DishId),"UID-partitioned history maps the stored fields back to UI");
            string id=match.Value;var repo=new AttemptRepository(backend,auth);AttemptRecord loaded=await repo.ReadAsync(uid,id);Check(loaded!=null&&loaded.score==9375&&loaded.is_correct,"a new repository instance retrieves the same actual attempt");Capture("history-loaded.png");
            Click("LoadLeaderboard");await Wait(()=>GameObject.Find("LoadLeaderboard").GetComponent<Button>().interactable,"leaderboard query");Check(GameObject.Find("AttemptQueryRows").GetComponent<TMP_Text>().text.Contains("VerticalSliceChef"),"atomic leaderboard row is retrieved and displayed");Capture("leaderboard-loaded.png");
            Click("ContinueButton");Check(GameObject.Find("HeritageOverlay")!=null,"confirmed correct attempt unlocks Continue");Capture("stage-completed.png");Click("CloseHeritage");
            FirebaseAuth.GetAuth(backend.App).SignOut();await auth.SignInAsync();await Task.Delay(100);
            Check(auth.Uid!=uid&&!next.interactable,"identity change revokes Continue for the previous owner's attempt");
            Click("LoadAttemptsButton");await Wait(()=>GameObject.Find("LoadAttemptsButton").GetComponent<Button>().interactable,"empty new UID history");Check(GameObject.Find("AttemptQueryRows").GetComponent<TMP_Text>().text=="No chef attempts recorded yet","real empty RTDB query renders valid Empty state");Capture("history-empty.png");
            var presenter=(CookingResultPresenter)Field(controller,"result");var query=(AttemptQueryController)Field(presenter,"query");
            // A fresh foreign path prevents GetValueAsync's documented cache fallback
            // from returning the previous owner's already-cached data after denial.
            await query.LoadAsync(async()=>{await backend.Database.RootReference.Child("users").Child("foreign_"+Guid.NewGuid().ToString("N")).Child("attempts").OrderByChild("created_at").LimitToLast(20).GetValueAsync();return new List<string>();});
            Check(query.State==AttemptQueryState.Error&&GameObject.Find("LoadAttemptsButton").GetComponent<Button>().interactable,"real cross-UID permission denial renders Error with retry");Capture("history-error.png");
            Click("LoadAttemptsButton");await Wait(()=>GameObject.Find("LoadAttemptsButton").GetComponent<Button>().interactable,"retry query");Check(GameObject.Find("AttemptQueryRows").GetComponent<TMP_Text>().text=="No chef attempts recorded yet","load retry recovers after an actual permission error");
            evidence.uid=uid;evidence.attemptId=id;evidence.record=loaded;evidence.passed=checks.Count;evidence.checks=checks.ToArray();
            File.WriteAllText(Path.Combine(output,"unity-results.json"),JsonUtility.ToJson(evidence,true));Debug.Log("VERTICAL COMPLETE "+checks.Count);EditorApplication.Exit(0);
        }
        catch(Exception error){evidence.error=error.ToString()+" | ACTIVE SCENE: "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+" | ROOTS: "+string.Join(",",Array.ConvertAll(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(),g=>g.name));evidence.passed=checks.Count;evidence.checks=checks.ToArray();File.WriteAllText(Path.Combine(output,"unity-results.json"),JsonUtility.ToJson(evidence,true));Debug.LogException(error);EditorApplication.Exit(1);}
    }
    private void Capture(string filename)
    {
        var cameraGo=new GameObject("EvidenceCamera");var camera=cameraGo.AddComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.2f,.1f,.04f);
        var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;}
        Canvas.ForceUpdateCanvases();foreach(var view in FindObjectsByType<CookingStageView>(FindObjectsSortMode.None))view.FitIfNeeded();foreach(var text in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.ForceMeshUpdate();Canvas.ForceUpdateCanvases();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,filename),image.EncodeToPNG());RenderTexture.active=previous;
        foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}
        camera.targetTexture=null;DestroyImmediate(image);DestroyImmediate(target);DestroyImmediate(cameraGo);Canvas.ForceUpdateCanvases();
    }
}
#endif
