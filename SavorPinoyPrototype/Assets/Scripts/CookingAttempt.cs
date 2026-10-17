using System;
using System.Collections.Generic;

// Unity-independent rules so sequence, timing, and grading can be verified without a scene.
public enum CookingPhase { Gather, Chop, HeatPot, Garlic, Turmeric, Meat, Sear, Vinegar, Simmer, Plate, Success, Failure }

public sealed class CookingAttempt
{
    public const string Rubric = "batangas_slice_v1";
    public const int RequiredCuts = 6;
    public const double SimmerSeconds = 12;
    public CookingPhase Phase { get; private set; } = CookingPhase.Gather;
    public bool TimedChallenge { get; }
    public double PrepElapsed { get; private set; }
    public double CookElapsed { get; private set; }
    public double Elapsed => PrepElapsed + CookElapsed;
    public double Remaining { get; private set; }
    public double CookingRemaining => cookingRemaining;
    public double Heat { get; private set; } = 20;
    public double Flame { get; private set; }
    public double SimmerProgress { get; private set; }
    public double SearProgress { get; private set; }
    public double ScorchExposure { get; private set; }
    public int Cuts { get; private set; }
    public int Mistakes { get; private set; }
    public int Penalties { get; private set; }
    public int ScorchWarnings { get; private set; }
    public string Message { get; private set; } = "Gather garlic, turmeric, and chicken. Soy sauce is a distractor for this recipe.";
    public int FinalScore { get; private set; }
    public int Stars { get; private set; }
    public double CuttingAccuracy => Cuts == 0 ? 0 : accuracySum / Cuts;
    public int CuttingScore => (int)Math.Round(2500 * CuttingAccuracy, MidpointRounding.AwayFromZero);
    public int SequenceScore => Math.Max(0, 2500 - Mistakes * 250);
    public int ThermalScore => Math.Max(0, 2500 - (int)(ScorchExposure * 150));
    public int RawScore => Math.Max(0, CuttingScore + SequenceScore + ThermalScore - Penalties);
    public bool Terminal => Phase == CookingPhase.Success || Phase == CookingPhase.Failure;
    public bool KnifeSelected { get; private set; }
    public float Multiplier { get; }
    private readonly HashSet<string> gathered = new HashSet<string>();
    private double accuracySum;
    private double cookingRemaining = 75;
    private bool warningActive;
    private double frameAccumulator;

    public CookingAttempt(bool correctTrivia, bool timedChallenge=true, double prepSeconds=60)
    {
        TimedChallenge=timedChallenge;
        Remaining = prepSeconds + (correctTrivia ? 15 : 0);
        Multiplier = correctTrivia ? 1.25f : 1f;
    }
    public bool HasIngredient(string id) => gathered.Contains(id);
    public void Gather(string id)
    {
        if (Terminal || Phase != CookingPhase.Gather) return;
        if (id != "garlic" && id != "turmeric" && id != "chicken") { Error("Soy sauce is not used in this selected Adobo sa Dilaw recipe. Choose luyang dilaw.",100,5); return; }
        if (!gathered.Add(id)) { Message = "That ingredient is already selected."; return; }
        Message = "Ingredients gathered: " + gathered.Count + "/3. Choose the knife when ready.";
    }
    public void ChooseKnife()
    {
        if (Terminal || Phase != CookingPhase.Gather) return;
        KnifeSelected = true;
        if (gathered.Count != 3) { Message = "Gather all three ingredients before chopping."; return; }
        Phase = CookingPhase.Chop;
        Message = "Trace each marked line with the knife: three garlic cuts, then three turmeric preparation strokes.";
    }
    public void ChooseWrongTool()
    {
        if (!Terminal && (Phase == CookingPhase.Gather || Phase == CookingPhase.Chop)) Error("Use the knife for this preparation step, not the spoon.",100,5);
    }
    public void Cut(double accuracy)
    {
        if (Terminal || Phase != CookingPhase.Chop || !KnifeSelected) return;
        if (double.IsNaN(accuracy) || double.IsInfinity(accuracy)) return;
        accuracy = Math.Max(0, Math.Min(1, accuracy));
        if (accuracy < .75) { Error("Missed guide: start at the top marker and trace down the line.",100,0); return; }
        accuracySum += accuracy; Cuts++;
        if (Cuts == RequiredCuts) { Phase = CookingPhase.HeatPot; Message = "Preparation complete. Warm the palayok to the marked cooking band."; }
        else Message = "Accepted cut " + Cuts + "/6. Follow the next guide.";
    }
    public void SetFlame(double value) { if (!Terminal && !double.IsNaN(value) && !double.IsInfinity(value)) Flame = Math.Max(0,Math.Min(1,value)); }
    public void AddToPot(string id)
    {
        if (Terminal || Phase < CookingPhase.HeatPot || Phase >= CookingPhase.Plate) return;
        string expected = Phase == CookingPhase.Garlic ? "garlic" : Phase == CookingPhase.Turmeric ? "turmeric" : Phase == CookingPhase.Meat ? "chicken" : Phase == CookingPhase.Vinegar ? "vinegar" : null;
        if (id != expected || expected == null) { Error("Wrong step. Follow the current recipe objective; repeating ingredients cannot earn points.",100,0); return; }
        if (Phase != CookingPhase.Vinegar && Heat < 55) { Message = "The pot is too cool. Raise the flame before adding the next ingredient."; return; }
        switch (Phase)
        {
            case CookingPhase.Garlic: Phase = CookingPhase.Turmeric; Message = "Garlic added. Add prepared turmeric next."; break;
            case CookingPhase.Turmeric: Phase = CookingPhase.Meat; Message = "Turmeric added. Add chicken for the simplified searing step."; break;
            case CookingPhase.Meat: Phase = CookingPhase.Sear; Message = "Sear the chicken at heat index 60-85 for 4 simulated seconds before adding vinegar."; break;
            case CookingPhase.Vinegar: Phase = CookingPhase.Simmer; Message = "Keep heat within 60-85. Leave the vinegar simmer unstirred for 12 simulated seconds."; break;
        }
    }
    public void Stir()
    {
        if (Phase == CookingPhase.Simmer) Error("Premature stirring: leave this recipe's initial vinegar simmer unstirred. -500 points.",500,0);
    }
    private void Error(string message,int penalty,double seconds)
    {
        if (Terminal) return;
        Mistakes++; Penalties += penalty; Remaining = Math.Max(0,Remaining-(TimedChallenge ? seconds : 0)); Message = message;
        if (TimedChallenge && Remaining <= 0 && (Phase == CookingPhase.Gather || Phase == CookingPhase.Chop)) Fail("Preparation time expired. Retry and choose the correct ingredients and tools.");
    }
    public void Advance(double seconds)
    {
        if (Terminal || seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
        if (Phase <= CookingPhase.Chop) PrepElapsed += seconds; else CookElapsed += seconds;
        frameAccumulator += Math.Min(seconds, .5);
        while (frameAccumulator >= .02 && !Terminal) { Step(.02); frameAccumulator -= .02; }
    }
    private void Step(double dt)
    {
        if (Phase == CookingPhase.Gather || Phase == CookingPhase.Chop)
        {
            if (TimedChallenge) Remaining = Math.Max(0,Remaining-dt);
            if (TimedChallenge && Remaining <= 0) Fail("Preparation time expired. Retry for a fresh attempt.");
            return;
        }
        if (TimedChallenge) cookingRemaining -= dt;
        if (TimedChallenge && cookingRemaining <= 0) { Fail("Cooking time expired. Follow the recipe objectives and heat band."); return; }
        // Abstract heat index, not Celsius or a physical cooking-safety model.
        Heat += ((20 + Flame * 90) - Heat) * dt / 4;
        bool hot = Heat > 95;
        if (hot && !warningActive) { ScorchWarnings++; Message = "Scorch warning! Lower the flame early; the clay pot retains heat."; }
        warningActive = hot;
        if (hot) ScorchExposure += dt;
        if (ScorchExposure >= 3) { Fail("Burnt dish: the pot stayed too hot. Lower heat earlier to account for retained heat."); return; }
        if (Phase == CookingPhase.HeatPot && Heat >= 60) { Phase = CookingPhase.Garlic; Message = "Pot ready. Add garlic first."; }
        if (Phase == CookingPhase.Sear && Heat >= 60 && Heat <= 85)
        {
            SearProgress = Math.Min(4,SearProgress+dt);
            if (SearProgress >= 4) { Phase = CookingPhase.Vinegar; Message = "Searing complete. Add native vinegar."; }
        }
        if (Phase == CookingPhase.Simmer && Heat >= 60 && Heat <= 85)
        {
            SimmerProgress = Math.Min(SimmerSeconds,SimmerProgress+dt);
            if (SimmerProgress >= SimmerSeconds) { Phase = CookingPhase.Plate; Message = "Simmer complete. Turn the flame off and plate your Adobo sa Dilaw."; }
        }
    }
    public void Plate()
    {
        if (Phase != CookingPhase.Plate) return;
        if (Flame > .01) { Message = "Turn the flame off before plating."; return; }
        FinalScore = (int)Math.Round(RawScore * Multiplier,MidpointRounding.AwayFromZero);
        // Judge skill with raw performance, not trivia-inflated points.
        Stars = RawScore >= 7000 && Mistakes == 0 && CuttingAccuracy >= .85 && ScorchWarnings == 0 ? 3 : RawScore >= 5500 && Mistakes <= 1 && CuttingAccuracy >= .70 ? 2 : RawScore >= 4000 && Mistakes <= 2 ? 1 : 0;
        Phase = Stars > 0 ? CookingPhase.Success : CookingPhase.Failure;
        Flame = 0;
        Message = Stars > 0 ? "Completed! Review your cooking skills and trivia bonus separately." : "Procedure completed, but too many mistakes. Review the breakdown and retry.";
    }
    public void Fail(string reason) { if (Terminal) return; Phase=CookingPhase.Failure; FinalScore=0; Stars=0; Flame=0; Message=reason; }
}
