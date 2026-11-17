using UnityEngine;

// Heritage Card collection, stored on this device only (PlayerPrefs). Not part of the Firebase contract.
public static class HeritageCards
{
    private const string Prefix="SavorPinoy.HeritageCard.";
    public static bool Unlocked(string dishId)=>PlayerPrefs.GetInt(Prefix+dishId,0)==1;
    public static int Count{get{int n=0;foreach(var plan in RecipePlan.All)if(Unlocked(plan.Id))n++;return n;}}
    // Returns true only the first time a card is earned.
    public static bool Unlock(string dishId)
    {
        if(!RecipePlan.Supported(dishId)||Unlocked(dishId))return false;
        PlayerPrefs.SetInt(Prefix+dishId,1);PlayerPrefs.Save();return true;
    }
}
