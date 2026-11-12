using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;

// Initializes SDK dependencies and resolves one explicit backend; never fabricates a live URL.
public sealed class FirebaseBootstrap
{
    private static readonly HashSet<string> configuredEmulators=new HashSet<string>();
    private readonly FirebaseSettings settings;
    private Task initialization;
    private static Task<DependencyStatus> dependencyCheck;
    public FirebaseApp App { get; private set; }
    private FirebaseDatabase database;
    private string resolvedDatabaseUrl;
    // Resolve the connection after Auth has initialized, when persistence first needs it.
    public FirebaseDatabase Database
    {
        get
        {
            if(App==null)throw new InvalidOperationException("Initialize Firebase before opening the database.");
            if(database==null)
            {
                database=FirebaseDatabase.GetInstance(App,resolvedDatabaseUrl);
                if(IsEmulator&&configuredEmulators.Add(App.Name))database.SetPersistenceEnabled(false);
            }
            return database;
        }
    }
    public bool IsEmulator { get; private set; }
    public FirebaseBootstrap(FirebaseSettings settings){this.settings=settings;}
    public Task InitializeAsync()
    {
        if(initialization==null || initialization.IsFaulted || initialization.IsCanceled)initialization=InitializeCoreAsync();
        return initialization;
    }
    private async Task InitializeCoreAsync()
    {
        if(dependencyCheck==null||dependencyCheck.IsFaulted||dependencyCheck.IsCanceled)dependencyCheck=FirebaseApp.CheckAndFixDependenciesAsync();
        if(await dependencyCheck!=DependencyStatus.Available){dependencyCheck=null;throw new InvalidOperationException("Firebase dependencies are unavailable. Check the installed SDK.");}
        if(!settings)throw new InvalidOperationException("Firebase settings are missing from CookingStage.");
#if UNITY_EDITOR
        IsEmulator=settings.useLocalEmulators;
#endif
        if(IsEmulator)
        {
            if(!settings.emulatorProjectId.StartsWith("demo-",StringComparison.Ordinal))throw new InvalidOperationException("Local testing requires a demo- project ID.");
            App=FirebaseApp.GetInstance("SavorPinoyEmulator")??FirebaseApp.Create(new AppOptions{ApiKey="fake-emulator-key",AppId="1:123456789:android:123456789abcdef0",ProjectId=settings.emulatorProjectId},"SavorPinoyEmulator");
            // Omit an explicit port: the desktop SDK uses the host as a directory name.
            // Local SDK testing uses port 80; a host:9000 directory is invalid on Windows.
            resolvedDatabaseUrl="http://127.0.0.1/?ns="+settings.emulatorProjectId;
        }
        else
        {
            if(!Uri.TryCreate(settings.databaseUrl,UriKind.Absolute,out Uri uri)||uri.Scheme!="https"||!uri.Host.EndsWith(".asia-southeast1.firebasedatabase.app",StringComparison.Ordinal)||uri.AbsolutePath!="/"||!string.IsNullOrEmpty(uri.Query))throw new InvalidOperationException("Configure the exact Singapore Realtime Database URL in Firebase settings.");
            App=FirebaseApp.DefaultInstance;resolvedDatabaseUrl=uri.ToString();
        }
    }
}
