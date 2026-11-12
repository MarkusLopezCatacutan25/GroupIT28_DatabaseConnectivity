using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public static class WorkflowTests
{
    private sealed class Identity : IChefIdentity
    {
        public string Current="uid-a";
        public bool Reject;
        public string Uid=>Current;
        public Task<string> SignInAsync()=>Reject?Task.FromException<string>(new Exception("Sign-in rejected")):Task.FromResult(Current);
    }
    private sealed class Repository : IAttemptRepository
    {
        public int Writes;
        public TaskCompletionSource<bool> Write=new TaskCompletionSource<bool>();
        public TaskCompletionSource<AttemptRecord> Read=new TaskCompletionSource<AttemptRecord>();
        public Task WriteAsync(PendingChefAttempt item){Writes++;return Write.Task;}
        public Task<AttemptRecord> ReadAsync(string uid,string id)=>Read.Task;
    }
    private static int checks;
    private static string directory;
    private static AttemptRecord Record(bool correct=true)=>new AttemptRecord{dish_id=AttemptContract.DishId,score=correct?950:0,is_correct=correct,remarks="Master Searing",created_at="2026-10-27T10:05:00Z"};
    private static ChefAttemptOutbox Store()=>new ChefAttemptOutbox(Path.Combine(directory,Guid.NewGuid().ToString("N")+".json"));
    private static string Id()=>"attempt_"+Guid.NewGuid().ToString("N");
    private static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    public static async Task Main(string[] args)
    {
        PreparationWorkflowTests.Run();
        directory=Path.GetFullPath(args[0]);Directory.CreateDirectory(directory);
        Check(AttemptContract.Valid(Record()),"schema example validates");
        Check(Record().Payload().Keys.OrderBy(x=>x).SequenceEqual(new[]{"created_at","dish_id","is_correct","remarks","score"}),"attempt payload has exactly the five specified fields");
        Check(new LeaderboardRecord().Payload().Count==5,"leaderboard payload has exactly five fields");
        Check(!AttemptContract.ValidName("  ")&&!AttemptContract.ValidName("Chef\nMaria")&&AttemptContract.ValidName("ChefMaria"),"display name is validated independently of UID");
        Check(!AttemptContract.ValidId("ChefMaria")&&AttemptContract.ValidId(Id()),"nickname cannot be an attempt identity");
        var identity=new Identity();var repo=new Repository();var store=Store();var save=new AttemptSaveCoordinator(identity,repo,store);save.SetResult(true);
        Check(!save.CanContinue,"correct result alone does not enable Continue");
        string key=Id();var record=Record();Task pending=save.SaveAsync(key,"ChefMaria",record,true);
        Check(save.State==AttemptSaveState.Saving&&!save.CanContinue,"Continue stays locked while write is pending");
        await save.SaveAsync(key,"ChefMaria",record,true);Check(repo.Writes==1,"duplicate save click cannot create a second request");
        repo.Write.SetResult(true);await Task.Delay(10);Check(save.State==AttemptSaveState.ReadingBack&&!save.CanContinue,"write acknowledgement alone waits for read-back");
        repo.Read.SetResult(record);await pending;Check(save.CanContinue&&save.LoadedRecord.Matches(record),"matching UID write/read round trip enables Continue");
        identity.Current="uid-b";Check(!save.CanContinue,"switching identity revokes the confirmed Continue gate");
        bool rebound=false;try{store.Prepare("uid-b",key,"ChefMaria",record,true);}catch(InvalidOperationException){rebound=true;}Check(rebound,"pending record cannot rebind to a different UID");
        identity.Current="uid-a";repo=new Repository();save=new AttemptSaveCoordinator(identity,repo,Store());save.SetResult(false);pending=save.SaveAsync(Id(),"ChefMaria",Record(false),false);repo.Write.SetResult(true);repo.Read.SetResult(Record(false));await pending;Check(save.SaveSucceeded&&!save.CanContinue,"confirmed incorrect attempt never enables Continue");
        repo=new Repository();save=new AttemptSaveCoordinator(identity,repo,Store());save.SetResult(true);pending=save.SaveAsync(Id(),"ChefMaria",record,true);repo.Write.SetException(new Exception("Permission denied"));await pending;Check(save.State==AttemptSaveState.Error&&!save.CanContinue&&save.Message.Contains("rules"),"permission denial shows retryable human-readable error");
        repo=new Repository();save=new AttemptSaveCoordinator(identity,repo,Store());save.SetResult(true);pending=save.SaveAsync(Id(),"ChefMaria",record,true);repo.Write.SetResult(true);var wrong=Record();wrong.score=951;repo.Read.SetResult(wrong);await pending;Check(!save.CanContinue&&save.State==AttemptSaveState.Error,"read-back mismatch cannot report success");
        repo=new Repository();save=new AttemptSaveCoordinator(identity,repo,Store(),20);save.SetResult(true);key=Id();pending=save.SaveAsync(key,"ChefMaria",record,true);await pending;Check(save.State==AttemptSaveState.Error&&!save.CanContinue,"write timeout cannot report success");repo.Write.SetResult(true);await Task.Delay(5);Check(!save.CanContinue,"late acknowledgement cannot unlock an expired UI operation");repo.Read.SetResult(record);await save.SaveAsync(key,"ChefMaria",record,true);Check(save.CanContinue&&repo.Writes==2,"retry retains stable attempt ID and confirms a fresh round trip");
        repo=new Repository();save=new AttemptSaveCoordinator(identity,repo,Store());save.SetResult(true);pending=save.SaveAsync(Id(),"ChefMaria",record,true);save.Dispose();repo.Write.SetResult(true);await pending;Check(!save.CanContinue,"disposed scene ignores late save completion");
        repo=new Repository();save=new AttemptSaveCoordinator(identity,repo,Store());save.SetResult(true);await save.SaveAsync(Id(),"ChefMaria",Record(false),false);Check(save.State==AttemptSaveState.Error&&repo.Writes==0,"correctness cannot disagree with the record");
        identity.Reject=true;repo=new Repository();save=new AttemptSaveCoordinator(identity,repo,Store());save.SetResult(true);await save.SaveAsync(Id(),"ChefMaria",record,true);Check(save.State==AttemptSaveState.Error&&repo.Writes==0,"failed authentication performs no database write");identity.Reject=false;
        var query=new AttemptQueryController();var completion=new TaskCompletionSource<List<string>>();int calls=0;
        pending=query.LoadAsync(()=>{calls++;return completion.Task;});Check(query.Busy&&query.State==AttemptQueryState.Loading,"query exposes Loading and disables duplicate loads");await query.LoadAsync(()=>{calls++;return completion.Task;});Check(calls==1,"query ignores duplicate clicks");completion.SetResult(new List<string>());await pending;Check(query.State==AttemptQueryState.Empty&&query.Text=="No chef attempts recorded yet","empty query is a valid explicit Empty state");
        await query.LoadAsync(()=>Task.FromException<List<string>>(new Exception("Permission denied")));Check(query.State==AttemptQueryState.Error&&!query.Busy,"query failure exposes Error with retry available");
        await query.LoadAsync(()=>Task.FromResult(new List<string>{"ChefMaria | 950"}));Check(query.State==AttemptQueryState.Success&&query.Text.Contains("950"),"query retry maps records to Success");
        await query.LoadAsync(()=>Task.FromResult<List<string>>(null));Check(query.State==AttemptQueryState.Error,"malformed response is not treated as Empty");
        query=new AttemptQueryController(20);completion=new TaskCompletionSource<List<string>>();pending=query.LoadAsync(()=>completion.Task);await pending;Check(query.State==AttemptQueryState.Error,"query timeout exposes retryable Error");completion.SetResult(new List<string>{"late"});await Task.Delay(5);Check(query.State==AttemptQueryState.Error,"late query cannot replace timeout feedback");
        string corrupt=Path.Combine(directory,"corrupt.json");File.WriteAllText(corrupt,"{broken");var broken=new ChefAttemptOutbox(corrupt);bool refused=false;try{broken.Prepare("uid-a",Id(),"ChefMaria",record,true);}catch{refused=true;}Check(refused&&File.ReadAllText(corrupt)=="{broken","corrupt pending file is preserved and cannot silently overwrite data");
        query=new AttemptQueryController();completion=new TaskCompletionSource<List<string>>();pending=query.LoadAsync(()=>completion.Task);query.IdentityChanged();completion.SetResult(new List<string>{"previous owner"});await pending;Check(query.State==AttemptQueryState.Idle&&!query.Text.Contains("previous owner"),"UID change clears private rows and invalidates in-flight queries");
        var original=Record();var stored=Store().Prepare("uid-a",Id(),"ChefMaria",original,true);original.score=123;Check(stored.record.score==950,"pending payload snapshots cannot change through the caller's record reference");
        bool relabeled=false;try{save.SetResult(false);}catch(InvalidOperationException){relabeled=true;}Check(relabeled,"a bound save workflow cannot relabel its correctness");
        Check(!AttemptContract.ValidTimestamp("2026-02-31T10:00:00Z")&&!AttemptContract.ValidTimestamp("2026-10-27T10:00:00+08:00")&&AttemptContract.ValidTimestamp("2026-10-27T10:00:00Z"),"timestamp model enforces exact valid UTC seconds");
        Check(!AttemptContract.ValidScoreValue(950.5d)&&!AttemptContract.ValidScoreValue("950")&&AttemptContract.ValidScoreValue(950L),"database score mapper rejects fractional and string scores");
        await SignInWorkflowTests.Run();
        Console.WriteLine("COMPLETE "+checks+" workflow checks passed; FAKE gateways only, not live backend evidence.");
    }
}
