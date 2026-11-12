using System;
using UnityEngine;

public static class GameSessionState
{
    public static string SelectedRegion = "Region IV-A (CALABARZON)";
    public static string SelectedProvince = "Batangas";
    public static string TargetDish = "Adobo sa Dilaw";
    public static string RunId { get; private set; }
    public static bool TriviaCompleted { get; private set; }
    public static bool TriviaAnsweredCorrectly { get; private set; }
    public static string TriviaOutcome { get; private set; }
    public static string QuestionId { get; private set; }
    public static float BonusPrepTimer => TriviaAnsweredCorrectly ? 15f : 0f;
    public static float ScoreBonusMultiplier => TriviaAnsweredCorrectly ? 1.25f : 1f;
    private static bool consumed;
    public static string NextProvince { get; private set; }
    public static void QueueNextProvince(){NextProvince=SelectedProvince=="Batangas"?"Quezon":"Batangas";}
    public static void QueueRetryProvince(){NextProvince=SelectedProvince;}
    public static string ChooseProvince(){string p=NextProvince;NextProvince=null;return p??(UnityEngine.Random.Range(0,2)==0?"Batangas":"Quezon");}

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() { RunId=null;NextProvince=null;SelectedProvince="Batangas";TargetDish="Adobo sa Dilaw"; ResetBuffs(); }
    public static void ResetBuffs() { TriviaCompleted=false; TriviaAnsweredCorrectly=false; TriviaOutcome="Unresolved"; QuestionId=null; consumed=false; }
    public static void BeginRun(string province="Batangas")
    {
        ResetBuffs(); RunId=Guid.NewGuid().ToString("N");
        SelectedRegion="Region IV-A (CALABARZON)"; SelectedProvince=province; TargetDish=RecipePlan.For(province).Dish;
    }
    public static bool ResolveTrivia(string questionId,string outcome)
    {
        if (TriviaCompleted || string.IsNullOrEmpty(RunId)) return false;
        if (outcome!="Correct" && outcome!="Wrong" && outcome!="TimedOut") return false;
        QuestionId=questionId; TriviaOutcome=outcome; TriviaCompleted=true; TriviaAnsweredCorrectly=outcome=="Correct"; return true;
    }
    public static bool TryConsumeKitchen(out string runId,out bool correct)
    {
        runId=RunId; correct=TriviaAnsweredCorrectly;
        if (!TriviaCompleted || !TriviaAnsweredCorrectly || consumed || string.IsNullOrEmpty(RunId)) return false;
        consumed=true; return true;
    }
}
