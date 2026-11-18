using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// Activity 9 negative security tests. Local and isolated: no Firebase project is contacted.
public static class SecurityTests
{
    private sealed class Identity : IChefIdentity
    {
        public string Current="uid-owner-7f3a";
        public bool Reject;
        public string Uid=>Current;
        public Task<string> SignInAsync()=>Reject?Task.FromException<string>(new InvalidOperationException("Sign-in returned no UID. Enable Anonymous Authentication and retry.")):Task.FromResult(Current);
    }
    private sealed class Repository : IAttemptRepository
    {
        public int Writes;
        public Exception WriteError;
        public bool MissingRecord;
        public AttemptRecord Stored;
        public Task WriteAsync(PendingChefAttempt item){Writes++;if(WriteError!=null)return Task.FromException(WriteError);Stored=item.record;return Task.CompletedTask;}
        public Task<AttemptRecord> ReadAsync(string uid,string id)=>Task.FromResult(MissingRecord?null:Stored);
    }
    private static int passed;
    private static void Check(bool value,string name){if(!value)throw new Exception("SECURITY TEST FAILED: "+name);passed++;Console.WriteLine("PASS security "+passed+" "+name);}
    private static AttemptRecord Record(int score=950)=>new AttemptRecord{dish_id=AttemptContract.DishId,score=score,is_correct=true,remarks="batangas_prep_v3 | 3 stars",created_at="2026-11-18T10:05:00Z"};
    private static string Id()=>"attempt_"+Guid.NewGuid().ToString("N");
    private static ChefAttemptOutbox Store()=>new ChefAttemptOutbox(Path.Combine(Path.GetTempPath(),"sp-sec-"+Guid.NewGuid().ToString("N")+".json"));
    private static async Task<AttemptSaveCoordinator> Save(Identity identity,Repository repo,AttemptRecord record,string id=null)
    {
        var save=new AttemptSaveCoordinator(identity,repo,Store(),200);save.SetResult(true);
        await save.SaveAsync(id??Id(),"ChefMaria",record,true);return save;
    }
    public static async Task Run()
    {
        // ST-1 Input validation: invalid scores are rejected before any write.
        Check(!AttemptContract.Valid(Record(-500))&&!AttemptContract.Valid(Record(9376))&&!AttemptContract.ValidScoreValue(1.5)&&!AttemptContract.ValidScoreValue("950"),"score -500, 9376, 1.5 and \"950\" are rejected by the contract");
        var repo=new Repository();var save=await Save(new Identity(),repo,Record(-500));
        Check(save.State==AttemptSaveState.Error&&!save.CanContinue&&repo.Writes==0,"save with score -500 is refused, nothing is written, Continue stays locked");

        // ST-2 Input validation: nickname boundary.
        Check(!AttemptContract.ValidName(new string('a',25))&&!AttemptContract.ValidName("<script>")&&!AttemptContract.ValidName("Chef\nMaria")&&!AttemptContract.ValidName("   ")&&AttemptContract.ValidName("Chef Maria"),"nickname over 24 chars, markup, newline and blank are rejected");

        // ST-3 Authorization: a pending attempt cannot be re-bound to another UID.
        var outbox=Store();string shared=Id();outbox.Prepare("uid-owner-7f3a",shared,"ChefMaria",Record(),true);
        bool denied=false;try{outbox.Prepare("uid-intruder-0001",shared,"ChefMaria",Record(),true);}catch(InvalidOperationException){denied=true;}
        Check(denied,"another UID cannot take over a pending attempt ID");

        // ST-4 Authorization: Continue is revoked if the signed-in UID changes after a confirmed save.
        var identity=new Identity();repo=new Repository();save=await Save(identity,repo,Record());
        Check(save.CanContinue,"owner save confirmed (control case)");
        identity.Current="uid-other-9c21";
        Check(!save.CanContinue&&!save.SaveSucceeded,"after a UID change the confirmed save no longer unlocks Continue");

        // ST-5 Authentication failure: no write and no access.
        identity=new Identity{Reject=true};repo=new Repository();save=await Save(identity,repo,Record());
        Check(save.State==AttemptSaveState.Error&&repo.Writes==0&&!save.CanContinue,"failed sign-in writes nothing and keeps Continue locked");

        // ST-6 Network failure during save: handled, readable message, no crash.
        repo=new Repository{WriteError=new Exception("Network error: Unable to resolve host firebasedatabase.app")};save=await Save(new Identity(),repo,Record());
        Check(save.State==AttemptSaveState.Error&&save.Message.Contains("Check your connection")&&!save.Message.Contains("firebasedatabase.app")&&!save.CanContinue,"network failure shows a connection message without the server host name");

        // ST-7 Timeout: external service does not respond.
        var slow=new SlowRepository();var timed=new AttemptSaveCoordinator(new Identity(),slow,Store(),100);timed.SetResult(true);await timed.SaveAsync(Id(),"ChefMaria",Record(),true);
        Check(timed.State==AttemptSaveState.Error&&timed.Message.Contains("did not answer in time")&&!timed.CanContinue,"unresponsive server times out safely with a readable message");

        // ST-8 Missing record: read-back finds nothing.
        repo=new Repository{MissingRecord=true};save=await Save(new Identity(),repo,Record());
        Check(save.State==AttemptSaveState.Error&&!save.CanContinue,"missing read-back record is handled without a crash and Continue stays locked");

        // ST-9 Error exposure: raw SDK detail goes to the developer log, not to the player.
        UnityEngine.Debug.Lines.Clear();
        string raw="Firebase.Database.DatabaseException: Internal error at /users/uid-owner-7f3a/attempts https://savorpinoy.example/.json?auth=REDACTED";
        string shown=AttemptSaveCoordinator.Friendly(new Exception(raw));
        Check(shown=="Something went wrong. Please try again."&&!shown.Contains("/users/")&&!shown.Contains("http"),"unknown technical error shows a generic message only");
        Check(UnityEngine.Debug.Lines.Any(l=>l.Contains("DatabaseException")),"the technical detail is kept in the developer log");
        Check(AttemptSaveCoordinator.FriendlyFor(new Exception("Permission denied"))=="The save was not allowed. Check sign-in and the deployed database rules.","permission denied maps to a fixed message");

        // ST-10 Identity exposure on screen.
        string masked=AttemptContract.MaskUid("Xy7pQ2rTt9LmNa0bC3dE4fG5h6");
        Check(masked=="…G5h6"&&!masked.Contains("Xy7p"),"results screen shows only the last 4 UID characters");

        // ST-11 Backend remarks rule (same pattern as database.rules.json): only game-generated remarks are stored.
        // Firebase rules use JavaScript-style regex, where $ is the true end of the string; \z is the .NET equivalent.
        var rule=new Regex(@"^[a-z0-9_]{1,40} [|] [0-3] stars\z");
        Check(new[]{"batangas_prep_v3 | 3 stars","batangas_lomi_v1 | 0 stars","quezon_habhab_v2 | 2 stars","quezon_hardinera_v1 | 1 stars"}.All(rule.IsMatch),"all four dishes' real remarks match the rule");
        Check(!new[]{"Master Searing","<script>alert(1)</script>","batangas_prep_v3 | 9 stars","batangas_prep_v3 | 3 stars\n","",new string('a',41)+" | 3 stars"}.Any(rule.IsMatch),"free text, markup, impossible stars, newline, empty and oversized tags are rejected");
        Console.WriteLine("SECURITY COMPLETE "+passed);
    }
    private sealed class SlowRepository : IAttemptRepository
    {
        public Task WriteAsync(PendingChefAttempt item)=>new TaskCompletionSource<bool>().Task;
        public Task<AttemptRecord> ReadAsync(string uid,string id)=>Task.FromResult<AttemptRecord>(null);
    }
}
