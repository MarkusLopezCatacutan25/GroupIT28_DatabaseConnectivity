using System;
using System.Collections.Generic;

[Serializable]
public sealed class AttemptRecord
{
    public string dish_id;
    public int score;
    public bool is_correct;
    public string remarks;
    public string created_at;

    public Dictionary<string, object> Payload() => new Dictionary<string, object>
    {
        {"dish_id",dish_id},{"score",score},{"is_correct",is_correct},{"remarks",remarks},{"created_at",created_at}
    };
    public bool Matches(AttemptRecord other) => other!=null && dish_id==other.dish_id && score==other.score && is_correct==other.is_correct && remarks==other.remarks && created_at==other.created_at;
}
