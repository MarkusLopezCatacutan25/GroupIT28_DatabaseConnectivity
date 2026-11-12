using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable] public sealed class PendingChefAttempt
{
    public string uid,attemptId,displayName;
    public AttemptRecord record;
    public bool publishToLeaderboard,acknowledged;
}
[Serializable] public sealed class ChefAttemptQueue { public int version=2;public List<PendingChefAttempt> items=new List<PendingChefAttempt>(); }

// Local durability only. Legacy outboxes are left untouched, never silently assigned to a new UID.
public sealed class ChefAttemptOutbox
{
    private readonly string path;
    private ChefAttemptQueue queue;
    private string loadError;
    public ChefAttemptOutbox(string path)
    {
        this.path=path;
        try{Load();}catch(Exception error){loadError=error.Message;}
    }
    private void Load()
    {
        queue=File.Exists(path)?JsonUtility.FromJson<ChefAttemptQueue>(File.ReadAllText(path)):new ChefAttemptQueue();
        if(queue==null||queue.version!=2||queue.items==null||queue.items.Exists(i=>i==null||i.record==null||string.IsNullOrEmpty(i.uid)||!AttemptContract.ValidId(i.attemptId)||!AttemptContract.Valid(i.record)))throw new InvalidOperationException("Pending attempt storage needs recovery. The existing file was preserved.");
        loadError=null;
    }
    public PendingChefAttempt Prepare(string uid,string id,string name,AttemptRecord record,bool ranked)
    {
        if(loadError!=null)Load();
        if(string.IsNullOrEmpty(uid))throw new InvalidOperationException("Authenticate before preparing an owned record.");
        PendingChefAttempt existing=queue.items.Find(i=>i.attemptId==id);
        if(existing!=null)
        {
            if(existing.uid!=uid)throw new InvalidOperationException("This pending attempt belongs to another UID.");
            if(existing.displayName!=name||!existing.record.Matches(record)||existing.publishToLeaderboard!=ranked)throw new InvalidOperationException("This attempt ID already has a different immutable payload.");
            return existing;
        }
        var snapshot=new AttemptRecord{dish_id=record.dish_id,score=record.score,is_correct=record.is_correct,remarks=record.remarks,created_at=record.created_at};
        var item=new PendingChefAttempt{uid=uid,attemptId=id,displayName=name,record=snapshot,publishToLeaderboard=ranked};
        queue.items.Add(item);
        try{Persist();}catch{queue.items.Remove(item);throw;}
        return item;
    }
    public void Acknowledge(PendingChefAttempt item){item.acknowledged=true;Persist();}
    private void Persist()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path+".tmp",JsonUtility.ToJson(queue,true));
        if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);
    }
}
