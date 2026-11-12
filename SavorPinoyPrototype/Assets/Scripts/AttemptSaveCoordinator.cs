using System;
using System.Threading.Tasks;

public enum AttemptSaveState { Idle,Authenticating,Saving,ReadingBack,Confirmed,Error }

// Save workflow only. Continue is derived from correctness and the confirmed round trip.
public sealed class AttemptSaveCoordinator
{
    private readonly IChefIdentity identity;
    private readonly IAttemptRepository repository;
    private readonly ChefAttemptOutbox outbox;
    private readonly int timeoutMilliseconds;
    private bool disposed;
    public AttemptSaveState State { get; private set; }
    public string Message { get; private set; }="Finish cooking, then save your attempt to Firebase.";
    public string OwnerUid { get; private set; }
    public AttemptRecord LoadedRecord { get; private set; }
    public bool IsCorrect { get; private set; }
    public bool Busy=>State==AttemptSaveState.Authenticating||State==AttemptSaveState.Saving||State==AttemptSaveState.ReadingBack;
    public bool SaveSucceeded=>State==AttemptSaveState.Confirmed && !string.IsNullOrEmpty(OwnerUid)&&identity.Uid==OwnerUid;
    public bool CanContinue=>IsCorrect&&SaveSucceeded;
    public event Action Changed;
    public AttemptSaveCoordinator(IChefIdentity identity,IAttemptRepository repository,ChefAttemptOutbox outbox,int timeoutMilliseconds=15000){this.identity=identity;this.repository=repository;this.outbox=outbox;this.timeoutMilliseconds=timeoutMilliseconds;}
    public void SetResult(bool correct){if(State!=AttemptSaveState.Idle)throw new InvalidOperationException("This workflow is already bound to a result.");IsCorrect=correct;Notify();}
    public async Task SaveAsync(string id,string name,AttemptRecord record,bool ranked)
    {
        if(disposed||Busy||SaveSucceeded)return;
        LoadedRecord=null;State=AttemptSaveState.Authenticating;Message="Signing in with Firebase...";Notify();
        try
        {
            if(!AttemptContract.ValidId(id)||!AttemptContract.ValidName(name)||!AttemptContract.Valid(record)||record.is_correct!=IsCorrect)throw new InvalidOperationException("Check your chef nickname and completed attempt.");
            string uid=await FirebaseTaskTimeout.Within(identity.SignInAsync(),timeoutMilliseconds);if(disposed)return;
            PendingChefAttempt item=outbox.Prepare(uid,id,name,record,ranked);OwnerUid=uid;
            State=AttemptSaveState.Saving;Message="Saving to Firebase... waiting for acknowledgement.";Notify();
            await FirebaseTaskTimeout.Within(repository.WriteAsync(item),timeoutMilliseconds);if(disposed)return;
            State=AttemptSaveState.ReadingBack;Message="Write acknowledged. Reading your attempt back...";Notify();
            AttemptRecord loaded=await FirebaseTaskTimeout.Within(repository.ReadAsync(uid,id),timeoutMilliseconds);if(disposed)return;
            if(identity.Uid!=uid||!record.Matches(loaded))throw new InvalidOperationException("Read-back did not match this UID and attempt. Retry verification.");
            outbox.Acknowledge(item);LoadedRecord=loaded;State=AttemptSaveState.Confirmed;Message="Saved to Firebase and read back successfully.";
        }
        catch(Exception error)
        {
            if(disposed)return;State=AttemptSaveState.Error;
            Message="Could not confirm the Firebase save. "+Friendly(error)+" Retry keeps the same attempt ID.";
        }
        Notify();
    }
    public void Dispose(){disposed=true;Changed=null;}
    private void Notify(){if(!disposed)Changed?.Invoke();}
    public static string Friendly(Exception error)
    {
        string reason=error.GetBaseException().Message;
        if(reason.IndexOf("permission",StringComparison.OrdinalIgnoreCase)>=0||reason.IndexOf("denied",StringComparison.OrdinalIgnoreCase)>=0)return "Check sign-in and the deployed database rules.";
        if(reason.IndexOf("network",StringComparison.OrdinalIgnoreCase)>=0)return "Check your connection.";
        return reason.Length<=180?reason:"Check Firebase configuration and retry.";
    }
}
