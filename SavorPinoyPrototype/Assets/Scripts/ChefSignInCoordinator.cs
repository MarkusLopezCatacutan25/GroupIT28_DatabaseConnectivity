using System;
using System.Threading.Tasks;

public enum ChefSignInState { Idle,SigningIn,SignedIn,Error }

// Explicit sign-in workflow; display names never supply or replace an authenticated UID.
public sealed class ChefSignInCoordinator
{
    private readonly IChefIdentity identity;
    private readonly int timeoutMilliseconds;
    private int generation;
    private bool disposed;
    public ChefSignInState State { get; private set; }
    public string DisplayName { get; private set; }
    public string Uid { get; private set; }
    public string Message { get; private set; }="No password needed. Your guest profile stays on this device.";
    public bool Busy=>State==ChefSignInState.SigningIn;
    public bool CanEnter=>!disposed&&State==ChefSignInState.SignedIn&&!string.IsNullOrEmpty(Uid)&&identity.Uid==Uid;
    public event Action Changed;
    public ChefSignInCoordinator(IChefIdentity identity,int timeoutMilliseconds=15000){this.identity=identity;this.timeoutMilliseconds=timeoutMilliseconds;}
    public async Task SignInAsync(string displayName)
    {
        if(disposed||Busy||CanEnter)return;
        string name=string.IsNullOrWhiteSpace(displayName)?"Chef":displayName.Trim();
        if(!AttemptContract.ValidName(name)){State=ChefSignInState.Error;Message="Use up to 24 letters or numbers for your chef name. It is display text only.";Changed?.Invoke();return;}
        int token=++generation;Uid=null;DisplayName=null;State=ChefSignInState.SigningIn;Message="Signing in... Please wait.";Changed?.Invoke();
        try
        {
            // Give the loading state a frame even when the SDK already has a cached user.
            await Task.Yield();if(disposed||token!=generation)return;
            string uid=await FirebaseTaskTimeout.Within(identity.SignInAsync(),timeoutMilliseconds);
            if(disposed||token!=generation)return;
            if(string.IsNullOrEmpty(uid)||identity.Uid!=uid)throw new InvalidOperationException("Sign-in changed before it could be confirmed. Please retry.");
            Uid=uid;DisplayName=name;State=ChefSignInState.SignedIn;Message="Signed in. Opening recipe selection...";
        }
        catch(Exception error){if(disposed||token!=generation)return;State=ChefSignInState.Error;Message="Sign-in was not confirmed. "+AttemptSaveCoordinator.Friendly(error)+" Please retry.";}
        Changed?.Invoke();
    }
    public void Cancel(){if(disposed)return;generation++;Uid=null;DisplayName=null;State=ChefSignInState.Idle;Message="No password needed. Your guest profile stays on this device.";Changed?.Invoke();}
    public void Dispose(){disposed=true;generation++;Changed=null;}
}
