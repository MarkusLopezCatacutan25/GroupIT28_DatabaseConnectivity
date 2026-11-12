using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public enum AttemptQueryState { Idle,Loading,Empty,Success,Error }

// Query UX state only. Null/malformed data never silently becomes an empty successful query.
public sealed class AttemptQueryController
{
    public AttemptQueryState State { get; private set; }
    public string Text { get; private set; }="Load your chef attempts or shared challenge scores.";
    public bool Busy=>State==AttemptQueryState.Loading;
    public event Action Changed;
    private int generation;
    private bool disposed;
    private readonly int timeoutMilliseconds;
    public AttemptQueryController(int timeoutMilliseconds=15000){this.timeoutMilliseconds=timeoutMilliseconds;}
    public void IdentityChanged(){generation++;State=AttemptQueryState.Idle;Text="Load attempts for the current signed-in chef.";Changed?.Invoke();}
    public async Task LoadAsync(Func<Task<List<string>>> query)
    {
        if(disposed||Busy)return;
        int token=++generation;State=AttemptQueryState.Loading;Text="Loading chef attempts...";Changed?.Invoke();
        try
        {
            List<string> rows=await FirebaseTaskTimeout.Within(query(),timeoutMilliseconds);
            if(disposed||token!=generation)return;
            if(rows==null)throw new InvalidOperationException("The database returned an invalid response.");
            State=rows.Count==0?AttemptQueryState.Empty:AttemptQueryState.Success;
            Text=rows.Count==0?"No chef attempts recorded yet":string.Join("\n\n",rows);
        }
        catch(Exception error){if(disposed||token!=generation)return;State=AttemptQueryState.Error;Text="Could not load chef attempts. "+AttemptSaveCoordinator.Friendly(error)+" Choose a load button to retry.";}
        Changed?.Invoke();
    }
    public void Dispose(){disposed=true;generation++;Changed=null;}
}
