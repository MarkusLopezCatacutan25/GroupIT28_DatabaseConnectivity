using UnityEngine;
using UnityEngine.SceneManagement;

// Routes visible sign-in intent, cancellation and the single guarded scene transition.
public sealed class ChefSignInController : MonoBehaviour
{
    private ChefSignInView view;
    private ChefSignInCoordinator signIn;
    private IChefIdentity identity;
    private bool leaving,disposed;
    public void Initialize(Canvas canvas,IChefIdentity identity)
    {
        this.identity=identity;view=new ChefSignInView(canvas);signIn=new ChefSignInCoordinator(identity);
        view.SubmitRequested+=Submit;view.BackRequested+=Back;signIn.Changed+=Render;Render();
    }
    public void Show(){if(disposed||leaving)return;signIn.Cancel();view.Show();Render();}
    private async void Submit()
    {
        await signIn.SignInAsync(view.Alias);
        if(disposed||leaving||!view.Visible||!signIn.CanEnter)return;
        ChefAccessSession.Authorize(identity,signIn.DisplayName);PlayerPrefs.SetString("SavorPinoy.ChefAlias",signIn.DisplayName);PlayerPrefs.Save();
        leaving=true;GameSessionState.ResetBuffs();SceneManager.LoadScene("RegionalTriviaHub");
    }
    private void Back(){if(leaving)return;signIn.Cancel();view.Hide();}
    private void Render(){view.Render(signIn);}
    private void Update(){if(view!=null&&view.Visible)view.Fit();}
    private void OnDestroy()
    {
        disposed=true;if(signIn!=null){signIn.Changed-=Render;signIn.Dispose();}
        if(view!=null){view.SubmitRequested-=Submit;view.BackRequested-=Back;}
    }
}
