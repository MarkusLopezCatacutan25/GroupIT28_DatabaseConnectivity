using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Scene composition root only. Existing scene GUID is retained for backwards compatibility.
public sealed class SavorPinoyCookingManager : MonoBehaviour
{
    [SerializeField] private FirebaseSettings firebaseSettings;
    [SerializeField] private Canvas legacyCanvas;
    [SerializeField] private TMP_FontAsset fontAsset;
    private async void Start()
    {
        if(!ChefAccessSession.HasAccess){SceneManager.LoadScene("MainMenu");return;}
        if(!GameSessionState.TryConsumeKitchen(out string runId,out bool correct)){SceneManager.LoadScene("RegionalTriviaHub");return;}
        if(legacyCanvas)legacyCanvas.gameObject.SetActive(false);
        var model=new CookingAttempt(correct,false);
        var view=gameObject.AddComponent<CookingStageView>();view.Initialize(model,fontAsset);
        var firebase=new FirebaseBootstrap(firebaseSettings);
        var identity=new FirebaseAuthManager(firebase);
        var repository=new AttemptRepository(firebase,identity);
        ChefAttemptOutbox outbox;
        try{outbox=new ChefAttemptOutbox(Path.Combine(Application.persistentDataPath,"savorpinoy-owned-attempts-v2.json"));}
        catch(Exception error){view.SetModeFeedback(error.Message);Debug.LogError("Pending attempt storage is unavailable; existing data preserved.");return;}
        var save=new AttemptSaveCoordinator(identity,repository,outbox);
        var query=new AttemptQueryController();var queries=new AttemptQueryService(firebase,identity);
        var resultView=new CookingResultView(view,GetComponent<KitchenArt>());
        var presenter=new CookingResultPresenter(resultView,save,query,queries,identity);
        gameObject.AddComponent<CookingStageController>().Initialize(view,presenter,identity,model,runId,correct);
        try{await FirebaseTaskTimeout.Within(identity.SignInAsync());if(this)view.SetIdentity(identity.Uid);}
        catch(Exception error){if(this)view.SetModeFeedback("Sign-in is not confirmed. You can practice; saving will retry. "+AttemptSaveCoordinator.Friendly(error));}
    }
}
