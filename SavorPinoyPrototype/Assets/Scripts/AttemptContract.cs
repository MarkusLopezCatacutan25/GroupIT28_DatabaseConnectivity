using System;
using System.Globalization;
using System.Text.RegularExpressions;

// Domain-to-storage mapping and validation; no Firebase or scene dependencies.
public static class AttemptContract
{
    public const string DishId="Batangas_AdoboSaDilaw";
    public const int MaximumScore=9375;
    public const string TimestampFormat="yyyy-MM-dd'T'HH:mm:ss'Z'";
    public static string UtcNow()=>DateTime.UtcNow.ToString(TimestampFormat,CultureInfo.InvariantCulture);
    public static bool ValidName(string value)=>value!=null && value.Length<=24 && Regex.IsMatch(value,@"\A[a-zA-Z0-9 _ñÑáéíóúÁÉÍÓÚ.\-]+\z") && Regex.IsMatch(value,@"[a-zA-Z0-9ñÑáéíóúÁÉÍÓÚ]");
    public static bool ValidId(string value)=>value!=null && Regex.IsMatch(value,@"\Aattempt_[a-f0-9]{32}\z");
    public static bool ValidTimestamp(string value)=>DateTime.TryParseExact(value,TimestampFormat,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal,out _);
    public static bool ValidScoreValue(object value)=>value is long number&&number>=0&&number<=MaximumScore || value is double fraction&&!double.IsNaN(fraction)&&fraction>=0&&fraction<=MaximumScore&&Math.Truncate(fraction)==fraction;
    public static bool Valid(AttemptRecord record)=>record!=null && RecipePlan.Supported(record.dish_id) && record.score>=0 && record.score<=MaximumScore && !string.IsNullOrWhiteSpace(record.remarks) && record.remarks.Length<=120 && ValidTimestamp(record.created_at);
    public static AttemptRecord From(CookingAttempt attempt)
    {
        if(attempt==null || !attempt.Terminal)throw new InvalidOperationException("Finish the cooking attempt first.");
        return new AttemptRecord{dish_id=attempt.Recipe.Id,score=attempt.FinalScore,is_correct=attempt.Phase==CookingPhase.Success,remarks=(attempt.Recipe.IsHabhab?"quezon_habhab_v1":"batangas_prep_v2")+" | "+attempt.Stars+" stars",created_at=UtcNow()};
    }
}
