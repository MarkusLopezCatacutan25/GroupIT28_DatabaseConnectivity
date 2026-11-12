using System;
using System.Threading.Tasks;

// Binds result UI intent to save/query workflows. Does not evaluate cooking or call Firebase APIs.
public sealed class CookingResultPresenter : IDisposable
{
    private readonly CookingResultView view;
    private readonly AttemptSaveCoordinator save;
    private readonly AttemptQueryController query;
    private readonly AttemptQueryService queries;
    private readonly IChefIdentity identity;
    private AttemptRecord record;
    private string id,name;
    private bool ranked,completed;
    public event Action RetryRequested,ExitRequested;
    public CookingResultPresenter(CookingResultView view,AttemptSaveCoordinator save,AttemptQueryController query,AttemptQueryService queries,IChefIdentity identity)
    {
        this.view=view;this.save=save;this.query=query;this.queries=queries;this.identity=identity;
        view.SaveRequested+=Save;view.ContinueRequested+=Continue;view.HistoryRequested+=History;view.LeaderboardRequested+=Leaderboard;
        view.NextProvinceRequested+=NextProvince;
        view.RetryRequested+=Retry;view.ExitRequested+=Exit;save.Changed+=Refresh;query.Changed+=Refresh;
    }
    public void Show(CookingAttempt attempt,string runId,string displayName)
    {
        id="attempt_"+runId;name=displayName;ranked=attempt.TimedChallenge;
        record=AttemptContract.From(attempt);save.SetResult(record.is_correct);view.Show(attempt,name);Refresh();
    }
    public void Refresh(){view.RenderSave(save,identity.Uid);view.RenderQuery(query);}
    public void IdentityChanged(){query.IdentityChanged();Refresh();}
    private async void Save(){if(record!=null)await save.SaveAsync(id,name,record,ranked);}
    private void Continue(){if(!save.CanContinue)return;if(!completed){completed=true;GameSessionState.QueueNextProvince();}view.Complete();}
    private void NextProvince(){if(!save.CanContinue)return;GameSessionState.QueueNextProvince();RetryRequested?.Invoke();}
    private async void History(){await query.LoadAsync(queries.HistoryAsync);}
    private async void Leaderboard(){await query.LoadAsync(queries.LeaderboardAsync);}
    private void Retry(){GameSessionState.QueueRetryProvince();RetryRequested?.Invoke();}
    private void Exit(){ExitRequested?.Invoke();}
    public void Dispose()
    {
        view.SaveRequested-=Save;view.ContinueRequested-=Continue;view.HistoryRequested-=History;view.LeaderboardRequested-=Leaderboard;view.RetryRequested-=Retry;view.ExitRequested-=Exit;
        view.NextProvinceRequested-=NextProvince;
        save.Changed-=Refresh;query.Changed-=Refresh;save.Dispose();query.Dispose();
    }
}
