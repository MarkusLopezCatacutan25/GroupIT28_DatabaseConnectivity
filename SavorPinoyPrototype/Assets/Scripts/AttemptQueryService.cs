using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

// Queries only: retrieves and validates snapshots; no UI, scoring, or save state.
public sealed class AttemptQueryService
{
    private readonly FirebaseBootstrap bootstrap;
    private readonly IChefIdentity identity;
    public AttemptQueryService(FirebaseBootstrap bootstrap,IChefIdentity identity){this.bootstrap=bootstrap;this.identity=identity;}
    public async Task<List<string>> HistoryAsync()
    {
        string uid=await identity.SignInAsync();await bootstrap.InitializeAsync();
        DataSnapshot snapshot=await FirebaseTaskTimeout.Within(bootstrap.Database.RootReference.Child("users").Child(uid).Child("attempts").OrderByChild("created_at").LimitToLast(20).GetValueAsync());
        if(identity.Uid!=uid)throw new InvalidOperationException("Sign-in changed. Reload history for the current UID.");
        var rows=new List<KeyValuePair<string,AttemptRecord>>();
        foreach(DataSnapshot child in snapshot.Children)
        {
            AttemptRecord item=JsonUtility.FromJson<AttemptRecord>(child.GetRawJsonValue());
            if(!AttemptContract.Valid(item)||child.ChildrenCount!=5||!(child.Child("is_correct").Value is bool)||!AttemptContract.ValidScoreValue(child.Child("score").Value))throw new InvalidOperationException("An attempt has an unexpected database format.");
            rows.Add(new KeyValuePair<string,AttemptRecord>(child.Key,item));
        }
        return rows.OrderByDescending(r=>r.Value.created_at,StringComparer.Ordinal).ThenBy(r=>r.Key,StringComparer.Ordinal).Select(r=>r.Value.score+" pts | "+(r.Value.is_correct?"Correct":"Needs practice")+"\n"+r.Value.dish_id+"\n"+r.Key+" | "+r.Value.created_at).ToList();
    }
    public async Task<List<string>> LeaderboardAsync()
    {
        string uid=await identity.SignInAsync();await bootstrap.InitializeAsync();
        DataSnapshot snapshot=await FirebaseTaskTimeout.Within(bootstrap.Database.RootReference.Child("leaderboard").OrderByChild("score").LimitToLast(10).GetValueAsync());
        if(identity.Uid!=uid)throw new InvalidOperationException("Sign-in changed. Reload the leaderboard.");
        var rows=new List<KeyValuePair<string,LeaderboardRecord>>();
        foreach(DataSnapshot child in snapshot.Children)
        {
            LeaderboardRecord item=JsonUtility.FromJson<LeaderboardRecord>(child.GetRawJsonValue());
            if(item==null||child.ChildrenCount!=5||!(child.Child("uid").Value is string)||!(child.Child("player_name").Value is string)||string.IsNullOrEmpty(item.uid)||!AttemptContract.ValidName(item.player_name)||!RecipePlan.Supported(item.dish_id)||!AttemptContract.ValidScoreValue(child.Child("score").Value)||!AttemptContract.ValidTimestamp(item.created_at))throw new InvalidOperationException("A leaderboard entry has an unexpected database format.");
            rows.Add(new KeyValuePair<string,LeaderboardRecord>(child.Key,item));
        }
        return rows.OrderByDescending(r=>r.Value.score).ThenBy(r=>r.Key,StringComparer.Ordinal).Select((r,i)=>(i+1)+". "+r.Value.player_name+" | "+r.Value.score+" pts\n"+r.Value.dish_id+"\n"+r.Value.created_at).ToList();
    }
}
