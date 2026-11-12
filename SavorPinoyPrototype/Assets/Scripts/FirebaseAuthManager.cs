using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;


// Identity only. Nicknames are never passed to an authentication API.
public sealed class FirebaseAuthManager : IChefIdentity
{
    private static readonly HashSet<string> emulatorConfigured=new HashSet<string>();
    private readonly FirebaseBootstrap bootstrap;
    private FirebaseAuth auth;
    private Task<string> pending;
    public string Uid=>auth?.CurrentUser?.UserId;
    public FirebaseAuthManager(FirebaseBootstrap bootstrap){this.bootstrap=bootstrap;}
    public Task<string> SignInAsync()
    {
        if(pending==null||pending.IsCompleted)pending=SignInCoreAsync();
        return pending;
    }
    private async Task<string> SignInCoreAsync()
    {
        await bootstrap.InitializeAsync();
        if(auth==null)
        {
            auth=FirebaseAuth.GetAuth(bootstrap.App);
            if(bootstrap.IsEmulator&&emulatorConfigured.Add(bootstrap.App.Name))auth.UseEmulator("127.0.0.1",9099);
        }
        if(auth.CurrentUser==null)
        {
            await auth.SignInAnonymouslyAsync();
            // Resume on Unity's next synchronization-context turn, outside native Auth polling.
            await Task.Yield();
        }
        if(string.IsNullOrEmpty(Uid))throw new InvalidOperationException("Sign-in returned no UID. Enable Anonymous Authentication and retry.");
        return Uid;
    }
}
