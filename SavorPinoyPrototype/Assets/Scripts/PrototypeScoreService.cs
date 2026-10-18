using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

[Serializable]
public sealed class PendingPrototypeScore
{
    public string recordId;
    public PlayerScoreRecord record;
    public string rubric;
    public int rawScore, cuttingScore, sequenceScore, thermalScore, mistakes;
    public float cuttingAccuracy;
    public bool synced;
    public string lastError;
}
[Serializable]
public sealed class PrototypeOutbox
{
    public int version=1;
    public long revision;
    public List<PendingPrototypeScore> items=new List<PendingPrototypeScore>();
    public string leaderboardCache="";
    public string cacheSavedUtc="";
}

// Six gameplay fields plus authenticated owner_uid. score_id is the node key.
// Deploy database.rules.json and enable Anonymous Authentication before online testing.
public sealed class PrototypeScoreService : MonoBehaviour
{
    public static PrototypeScoreService Instance { get; private set; }
    public string Status { get; private set; }="Scores are stored locally until Firebase is configured.";
    public string Leaderboard { get; private set; }="No leaderboard loaded.";
    public bool Reading { get; private set; }
    public event Action Changed;
    private PrototypeOutbox box=new PrototypeOutbox();
    public const int MaximumScore = 9375; // 7500 culinary points * 1.25 trivia multiplier.
    private FirebaseAuth auth;
    private DatabaseReference root;
    public string LastRecordId { get; private set; } = "";
    private string path, configuredUrl;
    private bool initializing, ready, storageReady=true;
    private double retryAt;
    private Task activeWrite;
    private PendingPrototypeScore activeItem;
    private float writeStarted,readStarted;
    private bool writeSlow;
    private int attempts,readGeneration;

    public static PrototypeScoreService Ensure(string url)
    {
        if(!Instance){GameObject go=new GameObject("PrototypeScoreService");go.AddComponent<PrototypeScoreService>();}
        if(!string.IsNullOrWhiteSpace(url) && !url.Contains("PASTE_YOUR"))Instance.configuredUrl=url;
        Instance.Initialize();return Instance;
    }
    private void Awake()
    {
        if(Instance && Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);
        path=Path.Combine(Application.persistentDataPath,"savorpinoy-prototype-outbox.json");LoadLocal();
        Leaderboard=string.IsNullOrEmpty(box.leaderboardCache)?"No cached leaderboard. Connect to load scores.":"Cached ("+box.cacheSavedUtc+")\n"+box.leaderboardCache;
    }
    public static bool IsValidAlias(string alias)
    {
        return !string.IsNullOrWhiteSpace(alias) && alias.Length<=24 &&
            System.Text.RegularExpressions.Regex.IsMatch(alias,@"\A[a-zA-Z0-9 _ñÑáéíóúÁÉÍÓÚ.\-]+\z") &&
            System.Text.RegularExpressions.Regex.IsMatch(alias,@"[a-zA-Z0-9ñÑáéíóúÁÉÍÓÚ]");
    }
    private void LoadLocal()
    {
        PrototypeOutbox best=null;
        foreach(string candidate in new[]{path,path+".bak",path+".tmp"})
        {
            if(!File.Exists(candidate))continue;
            try{PrototypeOutbox data=JsonUtility.FromJson<PrototypeOutbox>(File.ReadAllText(candidate));if(data!=null && data.version==1 && data.items!=null && data.items.All(i=>i!=null && i.record!=null && !string.IsNullOrEmpty(i.recordId)) && (best==null || data.revision>best.revision))best=data;}
            catch(Exception e){Debug.LogWarning("Could not load score cache: "+e.GetType().Name);}
        }
        if(best!=null)box=best;
        else if(File.Exists(path)||File.Exists(path+".bak")||File.Exists(path+".tmp")){storageReady=false;Status="Local score storage needs recovery. Existing files were preserved.";}
    }
    private bool Persist()
    {
        if(!storageReady)return false;
        try
        {
            box.revision++;
            File.WriteAllText(path+".tmp",JsonUtility.ToJson(box,true));
            if(File.Exists(path))
            {
                try{File.Replace(path+".tmp",path,path+".bak");}
                catch(PlatformNotSupportedException){File.Copy(path,path+".bak",true);File.Copy(path+".tmp",path,true);File.Delete(path+".tmp");}
            }
            else File.Move(path+".tmp",path);
            return true;
        }
        catch(Exception e){Status="Could not save locally. Free device storage and retry.";Debug.LogWarning("Local score write failed: "+e.GetType().Name);return false;}
    }
    public bool Queue(string runId,string alias,CookingAttempt attempt)
    {
        if(attempt==null || !attempt.TimedChallenge || attempt.Phase!=CookingPhase.Success || string.IsNullOrEmpty(runId))return false;
        alias=(alias??"").Trim();if(!IsValidAlias(alias)){Status="Use 1-24 letters/numbers; spaces, period, underscore and hyphen are allowed.";Notify();return false;}
        if(attempt.FinalScore<0 || attempt.FinalScore>MaximumScore){Status="Invalid score. Result was not queued.";Notify();return false;}
        string id="slice_"+runId; LastRecordId=id;
        if(box.items.Any(i=>i.recordId==id)){Status="This attempt is already saved locally.";Notify();return true;}
        PendingPrototypeScore item=new PendingPrototypeScore{recordId=id,rubric=CookingAttempt.Rubric,rawScore=attempt.RawScore,cuttingScore=attempt.CuttingScore,sequenceScore=attempt.SequenceScore,thermalScore=attempt.ThermalScore,mistakes=attempt.Mistakes,cuttingAccuracy=(float)attempt.CuttingAccuracy,
            record=new PlayerScoreRecord(alias,attempt.FinalScore,"Adobo sa Dilaw","Batangas",CookingAttempt.Rubric+" | "+attempt.Stars+" stars",DateTime.UtcNow.ToString("o"))};
        box.items.Add(item);
        if(!Persist()){box.items.Remove(item);Notify();return false;}
        Status="Saved on this device. "+(ready?"Waiting for cloud acknowledgement.":"Online sync will wait for Firebase configuration/connectivity.");retryAt=0;Notify();return true;
    }
    private void Initialize()
    {
        if(ready||initializing)return;

        initializing=true;
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task=>
        {
            if(!this)return;initializing=false;
            if(task.IsCanceled||task.IsFaulted){Status="Firebase initialization failed; local play remains available.";Notify();return;}
            if(task.Result!=DependencyStatus.Available){Status="Firebase dependencies are unavailable; scores remain local.";Notify();return;}
            Uri url;
            string candidate=configuredUrl;
            if(string.IsNullOrWhiteSpace(candidate))candidate=FirebaseApp.DefaultInstance.Options.DatabaseUrl?.ToString();
            if(!Uri.TryCreate(candidate,UriKind.Absolute,out url)||url.Scheme!="https"){Status="Database URL is not configured. Scores can still be saved on this device.";Notify();return;}
            try
            {
                root=FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,url.ToString()).RootReference;
                auth=FirebaseAuth.DefaultInstance;
                initializing=true;Status="Signing in for score ownership...";Notify();
                Authenticate().ContinueWithOnMainThread(login=>
                {
                    if(!this)return;initializing=false;
                    ready=!login.IsCanceled&&!login.IsFaulted&&auth.CurrentUser!=null;
                    Status=ready?"Signed in. Pending scores will sync; success requires server acknowledgement.":"Sign-in failed. Enable Anonymous Authentication; results remain saved for Retry sync.";
                    Notify();
                });
            }
            catch(Exception e){initializing=false;Status="Database setup failed; scores remain local.";Debug.LogWarning(e.GetType().Name);Notify();}
        });
    }
    private async Task Authenticate()
    {
        if(auth.CurrentUser==null)await auth.SignInAnonymouslyAsync();
    }
    private void Update()
    {
        if(Reading && Time.realtimeSinceStartup-readStarted>10){Reading=false;readGeneration++;Leaderboard=string.IsNullOrEmpty(box.leaderboardCache)?"Leaderboard timed out. Try again when online.":"Offline/cached ("+box.cacheSavedUtc+")\n"+box.leaderboardCache;Notify();}
        if(activeWrite!=null)
        {
            if(!writeSlow && Time.realtimeSinceStartup-writeStarted>10){writeSlow=true;Status="Score is pending. A slow connection may still acknowledge it later.";Notify();}
            return;
        }
        if(ready && (auth==null || auth.CurrentUser==null)){ready=false;Status="Signed out. Results remain local; use Retry sync.";Notify();}
        if(!ready||!storageReady||Time.realtimeSinceStartupAsDouble<retryAt)return;
        PendingPrototypeScore item=box.items.FirstOrDefault(i=>!i.synced && i.lastError!="Rejected");if(item==null)return;
        if(string.IsNullOrEmpty(item.record.owner_uid))
        {
            item.record.owner_uid=auth.CurrentUser.UserId;
            if(!Persist()){item.record.owner_uid=null;retryAt=Time.realtimeSinceStartupAsDouble+10;Notify();return;}
        }
        if(item.record.owner_uid!=auth.CurrentUser.UserId){item.lastError="Rejected";Status="Pending score belongs to another sign-in. Original result preserved.";Persist();Notify();return;}
        Status="Saving to Firebase: "+item.recordId+". Waiting for server acknowledgement.";Notify();
        activeItem=item;writeStarted=Time.realtimeSinceStartup;writeSlow=false;
        // Read same ID before writing: restart after lost acknowledgement must not create another row.
        activeWrite=ReconcileAndWrite(item);
        activeWrite.ContinueWithOnMainThread(task=>
        {
            if(!this)return;
            if(task.IsCanceled||task.IsFaulted)
            {
                string reason=task.Exception?.GetBaseException().Message??"Cancelled";
                bool rejected=reason.IndexOf("permission",StringComparison.OrdinalIgnoreCase)>=0 || reason.IndexOf("denied",StringComparison.OrdinalIgnoreCase)>=0 || reason.IndexOf("conflict",StringComparison.OrdinalIgnoreCase)>=0;
                item.lastError=rejected?"Rejected":"Retry";Status=rejected?"Cloud rejected this record. It is preserved locally; check database rules.":"Sync interrupted. The same record will retry; it remains saved locally.";
                attempts++;retryAt=Time.realtimeSinceStartupAsDouble+Math.Min(60,Math.Pow(2,Math.Min(attempts,5)))+UnityEngine.Random.value;
            }
            else{item.synced=true;item.lastError="";attempts=0;Status="Saved successfully to Firebase: "+item.recordId;}
            activeWrite=null;activeItem=null;Persist();Notify();
        });
    }
    private async Task ReconcileAndWrite(PendingPrototypeScore item)
    {
        DatabaseReference target=root.Child("player_scores").Child(item.recordId);
        DataSnapshot old=await target.GetValueAsync();
        if(old.Exists)
        {
            PlayerScoreRecord existing=JsonUtility.FromJson<PlayerScoreRecord>(old.GetRawJsonValue());
            if(existing!=null && existing.player_name==item.record.player_name && existing.score==item.record.score && existing.created_at==item.record.created_at && existing.dish_name==item.record.dish_name && existing.level==item.record.level && existing.remarks==item.record.remarks && existing.owner_uid==item.record.owner_uid)return;
            throw new InvalidOperationException("Record identity conflict");
        }
        await target.SetRawJsonValueAsync(JsonUtility.ToJson(item.record));
    }
    public void RetryPending(){foreach(PendingPrototypeScore i in box.items)if(!i.synced)i.lastError="";Persist();retryAt=0;Initialize();Status="Retry requested. Pending scores retain their original IDs.";Notify();}
    public void LoadLeaderboard()
    {
        if(Reading)return;if(!ready){Leaderboard=string.IsNullOrEmpty(box.leaderboardCache)?"Online leaderboard unavailable. Configure the database URL first.":"Cached ("+box.cacheSavedUtc+")\n"+box.leaderboardCache;Initialize();Notify();return;}
        Reading=true;readStarted=Time.realtimeSinceStartup;int generation=++readGeneration;Leaderboard="Loading shared scores...";Notify();
        root.Child("player_scores").OrderByChild("score").LimitToLast(10).GetValueAsync().ContinueWithOnMainThread(task=>
        {
            if(!this || generation!=readGeneration)return;Reading=false;
            if(task.IsFaulted||task.IsCanceled){Leaderboard="Unable to load scores. Check connection and read permissions."+(string.IsNullOrEmpty(box.leaderboardCache)?"":"\nCached:\n"+box.leaderboardCache);Notify();return;}
            List<KeyValuePair<string,PlayerScoreRecord>> rows=new List<KeyValuePair<string,PlayerScoreRecord>>();
            foreach(DataSnapshot child in task.Result.Children)
            {
                try{if(!(child.Child("score").Value is long) && !(child.Child("score").Value is double))continue;PlayerScoreRecord r=JsonUtility.FromJson<PlayerScoreRecord>(child.GetRawJsonValue());if(r!=null&&r.score>=0&&!string.IsNullOrEmpty(r.player_name))rows.Add(new KeyValuePair<string,PlayerScoreRecord>(child.Key,r));}
                catch(Exception){/* malformed legacy record is skipped, never interpreted as a zero score */}
            }
            rows=rows.OrderByDescending(r=>r.Value.score).ThenBy(r=>r.Key,StringComparer.Ordinal).ToList();
            Leaderboard=rows.Count==0?"No valid records found.":string.Join("\n",rows.Select((r,i)=>(i+1)+". "+r.Value.player_name+" - "+r.Value.score+" pts | "+r.Value.dish_name+"\nID: "+r.Key+" | "+r.Value.created_at));
            box.leaderboardCache=Leaderboard;box.cacheSavedUtc=DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'");Persist();Notify();
        });
    }
    private void Notify(){Changed?.Invoke();}
    private void OnDestroy(){if(Instance==this)Instance=null;}
}
