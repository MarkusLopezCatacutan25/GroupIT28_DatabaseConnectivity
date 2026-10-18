using System;

[Serializable]
public class PlayerScoreRecord
{
    public string player_name;
    public int score;
    public string dish_name;
    public string level;
    public string remarks;
    public string created_at;
    // Authenticated creator; score_id remains the parent Firebase node key.
    public string owner_uid;

    public PlayerScoreRecord() { }

    public PlayerScoreRecord(string playerName, int score, string dishName, string level, string remarks, string createdAt)
    {
        this.player_name = playerName;
        this.score = score;
        this.dish_name = dishName;
        this.level = level;
        this.remarks = remarks;
        this.created_at = createdAt;
    }
}
