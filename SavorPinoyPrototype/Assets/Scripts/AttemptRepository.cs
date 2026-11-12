using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;


// RTDB persistence only; a single atomic update writes the profile, attempt and optional board row.
public sealed class AttemptRepository : IAttemptRepository
{
    private readonly FirebaseBootstrap bootstrap;
    private readonly IChefIdentity identity;
    public AttemptRepository(FirebaseBootstrap bootstrap,IChefIdentity identity){this.bootstrap=bootstrap;this.identity=identity;}
    private void RequireOwner(string uid){if(string.IsNullOrEmpty(uid)||identity.Uid!=uid)throw new InvalidOperationException("Sign-in changed. This attempt remains bound to its original UID.");}
    public async Task WriteAsync(PendingChefAttempt item)
    {
        RequireOwner(item.uid);
        if(!AttemptContract.Valid(item.record)||!AttemptContract.ValidId(item.attemptId)||!AttemptContract.ValidName(item.displayName))throw new InvalidOperationException("The attempt does not match the database contract.");
        await bootstrap.InitializeAsync();RequireOwner(item.uid);
        string prefix="users/"+item.uid+"/";
        var updates=new Dictionary<string,object>{{prefix+"display_name",item.displayName},{prefix+"updated_at",AttemptContract.UtcNow()},{prefix+"attempts/"+item.attemptId,item.record.Payload()}};
        if(item.publishToLeaderboard&&item.record.is_correct)
        {
            var row=new LeaderboardRecord{uid=item.uid,player_name=item.displayName,dish_id=item.record.dish_id,score=item.record.score,created_at=item.record.created_at};
            updates["leaderboard/"+item.attemptId]=row.Payload();
        }
        // This task, not a local ValueChanged event, is the write acknowledgement.
        await bootstrap.Database.RootReference.UpdateChildrenAsync(updates);
        RequireOwner(item.uid);
    }
    public async Task<AttemptRecord> ReadAsync(string uid,string attemptId)
    {
        RequireOwner(uid);await bootstrap.InitializeAsync();
        DataSnapshot snapshot=await bootstrap.Database.RootReference.Child("users").Child(uid).Child("attempts").Child(attemptId).GetValueAsync();
        RequireOwner(uid);
        if(snapshot.Exists&&(snapshot.ChildrenCount!=5||!(snapshot.Child("is_correct").Value is bool)||!AttemptContract.ValidScoreValue(snapshot.Child("score").Value)))throw new InvalidOperationException("Read-back has an unexpected database format.");
        return snapshot.Exists?JsonUtility.FromJson<AttemptRecord>(snapshot.GetRawJsonValue()):null;
    }
}
