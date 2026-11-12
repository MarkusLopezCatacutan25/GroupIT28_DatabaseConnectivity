using UnityEngine;

// Presentation derived from domain state; never changes cooking or grading.
public static class CookingVesselPresentation
{
    public static bool Burnt(CookingAttempt attempt)=>attempt.Phase==CookingPhase.Failure&&attempt.ScorchExposure>=3;
    public static bool Steaming(CookingAttempt attempt)=>!attempt.Terminal&&attempt.AdditionIndex>0&&attempt.Heat>=60;
    public static bool Bubbling(CookingAttempt attempt)=>Steaming(attempt)&&attempt.Phase==CookingPhase.Simmer;
    public static Color VesselTint(CookingAttempt attempt)=>Burnt(attempt)?new Color(.38f,.27f,.22f):Color.Lerp(Color.white,new Color(1,.72f,.52f),Mathf.InverseLerp(85,105,(float)attempt.Heat));
    public static Color FoodTint(CookingAttempt attempt)
    {
        if(Burnt(attempt))return new Color(.19f,.12f,.075f);
        if(!attempt.Recipe.IsHabhab&&attempt.AdditionIndex>=3)
            return Color.Lerp(new Color(.87f,.78f,.64f),new Color(.88f,.65f,.29f),Mathf.Clamp01((float)(attempt.SearProgress/4)));
        return Color.Lerp(Color.white,new Color(1,.88f,.62f),Mathf.Clamp01((float)(attempt.SimmerProgress/CookingAttempt.SimmerSeconds)));
    }
}
