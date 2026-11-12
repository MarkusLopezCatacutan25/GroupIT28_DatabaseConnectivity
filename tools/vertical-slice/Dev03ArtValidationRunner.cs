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

// Isolated presentation checks. Never imported into the production Assets folder.
[InitializeOnLoad]
public static class Dev03ArtValidationEntry
{
    private static bool spawned;
    static Dev03ArtValidationEntry(){EditorApplication.update+=Poll;}
    public static void Run()
    {
        EditorSceneManager.playModeStartScene=null;EditorSceneManager.OpenScene("Assets/Scenes/CookingStage.unity");
        UnityEngine.Object.FindFirstObjectByType<SavorPinoyCookingManager>().enabled=false;
        File.WriteAllText("dev03-art-validation.request","run");EditorApplication.EnterPlaymode();
    }
    private static void Poll()
    {
        if(spawned||!EditorApplication.isPlaying||!File.Exists("dev03-art-validation.request"))return;
        spawned=true;File.Delete("dev03-art-validation.request");new GameObject("Dev03ArtValidation").AddComponent<Dev03ArtValidationRunner>().Begin();
    }
}

public sealed class Dev03ArtValidationRunner : MonoBehaviour
{
    private sealed class PreviewIdentity : IChefIdentity { public string Uid=>"editor-art-preview";public Task<string> SignInAsync()=>Task.FromResult(Uid); }
    private sealed class NoNetworkRepository : IAttemptRepository
    {
        public Task WriteAsync(PendingChefAttempt item)=>throw new InvalidOperationException("Art preview must not save records.");
        public Task<AttemptRecord> ReadAsync(string uid,string id)=>throw new InvalidOperationException("Art preview must not query records.");
    }
    [Serializable] private sealed class Report { public string scope="ISOLATED UNITY ART/PRESENTATION CHECKS ONLY; NO AUTH OR DATABASE PROOF";public int passed;public string[] checks;public string error; }
    private readonly List<string> checks=new List<string>();
    private string output;
    private static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    private void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add(label);Debug.Log("DEV03 PASS: "+label);}
    private static void Click(string name){var button=GameObject.Find(name)?.GetComponent<Button>();if(!button||!button.interactable)throw new Exception("Unavailable button: "+name);button.onClick.Invoke();}
    private static void Render(CookingStageController controller)=>controller.GetType().GetMethod("Render",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
    private static void Tick(CookingAttempt a,double seconds){for(int i=0;i<(int)(seconds/.02);i++)a.Advance(.02);}
    private static Vector2 Point(RectTransform rect,float x,float y)=>RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(rect.rect.xMin+x*rect.rect.width,rect.rect.yMin+y*rect.rect.height)));
    private static void Stroke(ChoppingSurface surface,float x,float start=.8f,float end=.2f)
    {
        var rect=(RectTransform)surface.transform;var data=new PointerEventData(EventSystem.current){pointerId=-1,position=Point(rect,x,start)};
        surface.OnPointerDown(data);data.position=Point(rect,x,(start+end)*.5f);surface.OnDrag(data);data.position=Point(rect,x,end);surface.OnPointerUp(data);
    }
    public async void Begin()
    {
        output=Environment.GetEnvironmentVariable("DEV03_ART_OUTPUT");Directory.CreateDirectory(output);var report=new Report();
        try
        {
            var manager=FindFirstObjectByType<SavorPinoyCookingManager>(FindObjectsInactive.Include);Check(manager&&!manager.enabled,"network composition is disabled only in the isolated art preview");
            Canvas legacy=(Canvas)Field(manager,"legacyCanvas");if(legacy)legacy.gameObject.SetActive(false);
            var art=manager.GetComponent<KitchenArt>();Check(art.PreparedGarlic&&art.PreparedTurmeric&&art.PlatedDish,"scene references resolve all three new sprites");
            foreach(string name in new[]{"GarlicPrepared_v02","TurmericPrepared_v02","AdoboDilawPlated_v02"})
            {
                string path="Assets/Sprites/Kitchen/Dev03/"+name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);Sprite sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Check(sprite&&importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single&&importer.alphaIsTransparency&&!importer.mipmapEnabled&&sprite.texture.width<=1024,"sprite import/alpha/size policy: "+name);
            }
            var identity=new PreviewIdentity();ChefAccessSession.Authorize(identity,"ArtPreviewChef");
            var model=new CookingAttempt(true,false);var view=manager.gameObject.AddComponent<CookingStageView>();view.Initialize(model,(TMP_FontAsset)Field(manager,"fontAsset"));
            var resultView=new CookingResultView(view,art);var save=new AttemptSaveCoordinator(identity,new NoNetworkRepository(),new ChefAttemptOutbox(Path.Combine(output,"unused-preview-outbox.json")));
            var presenter=new CookingResultPresenter(resultView,save,new AttemptQueryController(),new AttemptQueryService(null,identity),identity);
            var controller=manager.gameObject.AddComponent<CookingStageController>();controller.Initialize(view,presenter,identity,model,Guid.NewGuid().ToString("N"),true);
            await Task.Delay(100);Click("GuidedPractice");model=controller.Attempt;
            Click("Gather_garlic");Click("Gather_turmeric");Click("Gather_chicken");Click("Knife");var chopping=FindFirstObjectByType<ChoppingSurface>();
            var garlic=(Image)Field(view,"garlicCutPieces");var turmeric=(Image)Field(view,"turmericCutPieces");
            Check(!garlic.gameObject.activeSelf&&!turmeric.gameObject.activeSelf,"uncut ingredients do not reveal prepared artwork");Capture("chopping-before.png");
            Stroke(chopping,.5f,.5f,.4f);Check(model.Cuts==0&&!garlic.gameObject.activeSelf,"invalid stroke cannot reveal prepared garlic");
            Stroke(chopping,.3f);Check(model.Cuts==1&&garlic.gameObject.activeSelf&&garlic.sprite==art.PreparedGarlic&&!turmeric.gameObject.activeSelf,"first accepted cut reveals the prepared garlic sprite only");await Task.Delay(110);Capture("chopping-garlic.png");
            Stroke(chopping,.5f);Stroke(chopping,.7f);
            Check(model.Cuts==3&&garlic.gameObject.activeSelf&&!turmeric.gameObject.activeSelf&&GameObject.Find("BoardIngredient").GetComponent<Image>().sprite==art.Turmeric,"prepared garlic remains while raw turmeric becomes the next ingredient");
            Stroke(chopping,.3f);Check(model.Cuts==4&&turmeric.gameObject.activeSelf&&turmeric.sprite==art.PreparedTurmeric,"turmeric preparation reveals the matching prepared sprite");await Task.Delay(110);Capture("chopping-turmeric.png");
            Stroke(chopping,.5f);Stroke(chopping,.7f);
            Check(model.Cuts==6&&model.Phase==CookingPhase.HeatPot,"six accepted strokes still drive the original preparation rules");
            Check(GameObject.Find("PreparedGarlic").GetComponent<Image>().sprite==art.PreparedGarlic&&GameObject.Find("PreparedTurmeric").GetComponent<Image>().sprite==art.PreparedTurmeric,"the cooking tray uses prepared sprites instead of raw pantry images");Capture("prepared-tray.png");
            Click("StartCooking");Tick(model,8);Render(controller);Click("Add_garlic");Click("Add_turmeric");Click("Add_chicken");Tick(model,4.1);Render(controller);Click("Add_vinegar");Tick(model,13);Render(controller);GameObject.Find("FlameSlider").GetComponent<Slider>().value=0;Click("Plate");
            Check(model.Phase==CookingPhase.Success,"existing cooking sequence still reaches success");
            Check(GameObject.Find("PlatedAdobo").GetComponent<Image>().sprite==art.PlatedDish,"success result uses the golden plated Adobo sa Dilaw artwork");
            Check(!GameObject.Find("ContinueButton").GetComponent<Button>().interactable,"new result artwork does not bypass confirmed-save gating");Capture("result-golden-adobo.png");
            var failure=new CookingAttempt(false,true,0);failure.Advance(1);Check(failure.Phase==CookingPhase.Failure,"failure preview uses a real failed domain result");resultView.Show(failure,"ArtPreviewChef");
            Check(!((Image)Field(resultView,"platedDish")).gameObject.activeSelf,"failed attempts do not display the successful plated dish");Capture("result-failure.png");
            report.passed=checks.Count;report.checks=checks.ToArray();File.WriteAllText(Path.Combine(output,"art-results.json"),JsonUtility.ToJson(report,true));EditorApplication.Exit(0);
        }
        catch(Exception error){report.passed=checks.Count;report.checks=checks.ToArray();report.error=error.ToString();File.WriteAllText(Path.Combine(output,"art-results.json"),JsonUtility.ToJson(report,true));Debug.LogException(error);EditorApplication.Exit(1);}
    }
    private void Capture(string filename)
    {
        var cameraGo=new GameObject("ArtEvidenceCamera");var camera=cameraGo.AddComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.2f,.1f,.04f);
        var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;}
        Canvas.ForceUpdateCanvases();foreach(var view in FindObjectsByType<CookingStageView>(FindObjectsSortMode.None))view.FitIfNeeded();foreach(var text in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.ForceMeshUpdate();Canvas.ForceUpdateCanvases();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;var picture=new Texture2D(1920,1080,TextureFormat.RGB24,false);picture.ReadPixels(new Rect(0,0,1920,1080),0,0);picture.Apply();File.WriteAllBytes(Path.Combine(output,filename),picture.EncodeToPNG());RenderTexture.active=previous;
        foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}
        camera.targetTexture=null;DestroyImmediate(picture);DestroyImmediate(target);DestroyImmediate(cameraGo);Canvas.ForceUpdateCanvases();
    }
}
#endif
