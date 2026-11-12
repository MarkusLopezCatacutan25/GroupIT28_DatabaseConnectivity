using System;
using System.Threading.Tasks;

// Bounds UI waiting without pretending that a queued SDK write was cancelled on the server.
public static class FirebaseTaskTimeout
{
    public static async Task Within(Task operation,int milliseconds=15000)
    {
        if(await Task.WhenAny(operation,Task.Delay(milliseconds))!=operation)
        {
            _=operation.ContinueWith(t=>{var observed=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
            throw new TimeoutException("No server confirmation yet. The same-ID write may still finish later.");
        }
        await operation;
    }
    public static async Task<T> Within<T>(Task<T> operation,int milliseconds=15000){await Within((Task)operation,milliseconds);return await operation;}
}
