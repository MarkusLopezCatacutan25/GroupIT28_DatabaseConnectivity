using System;
using System.Collections.Generic;

[Serializable]
public sealed class LeaderboardRecord
{
    public string uid, player_name, dish_id, created_at;
    public int score;
    public Dictionary<string,object> Payload()=>new Dictionary<string,object>{{"uid",uid},{"player_name",player_name},{"dish_id",dish_id},{"score",score},{"created_at",created_at}};
}
