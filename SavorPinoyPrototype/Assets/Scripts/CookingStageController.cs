using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Owns one cooking session and routes user actions to its domain model.
public sealed class CookingStageController : MonoBehaviour
{
    private CookingStageView view;
    private CookingResultPresenter result;
    private IChefIdentity identity;
    private CookingAttempt attempt;
    private string runId,alias,lastUid;
    private bool correctTrivia,choosing=true,paused,shown,leaving;
    public CookingAttempt Attempt=>attempt;
    public void Initialize(CookingStageView view,CookingResultPresenter result,IChefIdentity identity,CookingAttempt attempt,string runId,bool trivia)
    {
        this.view=view;this.result=result;this.identity=identity;this.attempt=attempt;this.runId=runId;correctTrivia=trivia;
        view.ActionRequested+=Act;view.ModeRequested+=Choose;view.CutRequested+=Cut;view.FlameChanged+=Flame;
        result.RetryRequested+=Retry;result.ExitRequested+=Exit;Render();
    }
    private void Choose(bool timed)
    {
        if(!choosing)return;
        string display=(view.StartingAlias??"").Trim();
        if(!AttemptContract.ValidName(display)){view.SetModeFeedback("Enter a 1-24 character chef nickname. It is a display name, not your sign-in identity.");return;}
        alias=display;PlayerPrefs.SetString("SavorPinoy.ChefAlias",alias);PlayerPrefs.Save();
        attempt=new CookingAttempt(correctTrivia,timed,timed?240:120,RecipePlan.For(GameSessionState.SelectedProvince));choosing=false;view.ShowMode(false);Render();
    }
    private void Act(string action)
    {
        if(action=="exit"){Exit();return;}if(action=="pause"){Pause(true);return;}if(action=="resume"){Pause(false);return;}
        if(paused||choosing||attempt.Terminal)return;
        if(action.StartsWith("gather:",StringComparison.Ordinal))attempt.Gather(action.Substring(7));
        else if(action.StartsWith("prepare:",StringComparison.Ordinal))attempt.Prepare(action.Substring(8));
        else if(action.StartsWith("add:",StringComparison.Ordinal))attempt.AddToPot(action.Substring(4));
        else switch(action)
        {
            case "knife":attempt.ChooseKnife();break;case "spoon":attempt.ChooseWrongTool();break;
            case "warm":attempt.SetFlame(.6);view.SetFlame(.6f);break;case "stir":attempt.Stir();break;case "plate":attempt.Plate();break;
        }
        Render();
    }
    private void Cut(double accuracy){if(paused||choosing)return;attempt.Cut(accuracy);Render();}
    private void Flame(float value){if(paused||choosing||attempt.Phase<CookingPhase.HeatPot)return;attempt.SetFlame(value);Render();}
    private void Update()
    {
        if(view==null||attempt==null)return;view.FitIfNeeded();
        if(!paused&&!choosing&&!attempt.Terminal){attempt.Advance(Time.unscaledDeltaTime);Render();}
        if(lastUid!=identity.Uid){lastUid=identity.Uid;view.SetIdentity(lastUid);result.IdentityChanged();}
    }
    private void Render()
    {
        view.Render(attempt,paused,choosing);
        if(attempt.Terminal&&!shown){shown=true;paused=false;view.ShowTerminal();result.Show(attempt,runId,alias);}
    }
    private void Pause(bool value){if(view==null||attempt==null||attempt.Terminal)return;paused=value;view.ShowPause(value);Render();}
    private void OnApplicationPause(bool value){if(value)Pause(true);}
    private void OnApplicationFocus(bool value){if(!value)Pause(true);}
    private void Retry(){Leave("RegionalTriviaHub");}private void Exit(){Leave("MainMenu");}
    private void Leave(string scene){if(leaving)return;leaving=true;SceneManager.LoadScene(scene);}
    private void OnDestroy()
    {
        if(view!=null){view.ActionRequested-=Act;view.ModeRequested-=Choose;view.CutRequested-=Cut;view.FlameChanged-=Flame;}
        if(result!=null){result.RetryRequested-=Retry;result.ExitRequested-=Exit;result.Dispose();}
    }
}
