using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable] public sealed class PracticeAttemptRecord
{
    public string runId, alias, savedUtc, dish="Adobo sa Dilaw", mode="guided_practice";
    public double prepSeconds, cookingSeconds, activeSeconds;
    public int rawScore, mistakes, stars;
    public double cuttingAccuracy;
}
[Serializable] public sealed class PracticeAttemptHistory { public List<PracticeAttemptRecord> attempts=new List<PracticeAttemptRecord>(); }
public static class PracticeAttemptStore
{
    public static string Save(string runId,string alias,CookingAttempt attempt)
    {
        if(attempt==null||attempt.TimedChallenge||!attempt.Terminal)return "Complete a practice attempt first.";
        string path=Path.Combine(Application.persistentDataPath,"savorpinoy-practice-history.json");
        try
        {
            PracticeAttemptHistory history=File.Exists(path)?JsonUtility.FromJson<PracticeAttemptHistory>(File.ReadAllText(path)):new PracticeAttemptHistory();
            if(history==null||history.attempts==null)return "Practice history needs recovery. Existing file preserved.";
            if(history.attempts.Exists(r=>r.runId==runId))return "This practice result is already saved on this device.";
            history.attempts.Add(new PracticeAttemptRecord{runId=runId,alias=(alias??"").Trim(),savedUtc=DateTime.UtcNow.ToString("o"),prepSeconds=attempt.PrepElapsed,cookingSeconds=attempt.CookElapsed,activeSeconds=attempt.Elapsed,rawScore=attempt.RawScore,mistakes=attempt.Mistakes,stars=attempt.Stars,cuttingAccuracy=attempt.CuttingAccuracy});
            File.WriteAllText(path+".tmp",JsonUtility.ToJson(history,true));
            if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);
            return "Practice saved on this device, including preparation and cooking times.";
        }
        catch(Exception e){Debug.LogWarning("Practice save: "+e.GetType().Name);return "Could not save practice. Try again; your result is still shown.";}
    }
}
