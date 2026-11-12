using System;
using System.Collections.Generic;

// Unity-independent rules so sequence, timing, and grading can be verified without a scene.
public enum CookingPhase { Gather, Chop, HeatPot, Garlic, Turmeric, Meat, Sear, Vinegar, Simmer, Plate, Success, Failure }

public sealed class CookingAttempt
{
    public RecipePlan Recipe { get; }
    public PreparationProgress Preparation { get; }
    public int AdditionIndex { get; private set; }
    public string ExpectedIngredient => AdditionIndex<Recipe.Additions.Length ? Recipe.Additions[AdditionIndex] : null;
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

    public CookingAttempt(bool correctTrivia, bool timedChallenge=true, double prepSeconds=120, RecipePlan recipe=null)
    {
        Recipe=recipe??RecipePlan.Batangas; Preparation=new PreparationProgress(Recipe);
        Message="Gather the highlighted ingredients, then choose the knife.";
        TimedChallenge=timedChallenge;
        Remaining = prepSeconds + (correctTrivia ? 15 : 0);
        Multiplier = correctTrivia ? 1.25f : 1f;
    }
    public bool HasIngredient(string id) => gathered.Contains(id);
    public void Gather(string id)
    {
        if (Terminal || Phase != CookingPhase.Gather) return;
        if (Array.IndexOf(Recipe.Pantry,id)<0 || id=="soy") { Error("That ingredient is not part of this selected recipe.",100,5); return; }
        if (!gathered.Add(id)) { Message = "That ingredient is already selected."; return; }
        Message = "Ingredients gathered: " + gathered.Count + "/"+(Recipe.IsHabhab?4:3)+". Choose the knife when ready.";
    }
    public void ChooseKnife()
    {
        if (Terminal || Phase != CookingPhase.Gather) return;
        KnifeSelected = true;
        if (gathered.Count != (Recipe.IsHabhab?4:3)) { Message = "Gather all required recipe ingredients before preparation."; return; }
        Phase = CookingPhase.Chop;
        Message = Preparation.Current.Instruction;
    }
    public void ChooseWrongTool()
    {
        if (!Terminal && (Phase == CookingPhase.Gather || Phase == CookingPhase.Chop)) Error("Use the knife for this preparation step, not the spoon.",100,5);
    }
    public void Prepare(string stepId)
    {
        if(Terminal||Phase!=CookingPhase.Chop||!Preparation.Gesture(stepId))return;
        FinishPreparationStep();
    }
    private void FinishPreparationStep()
    {
        if(Preparation.Complete){Phase=CookingPhase.HeatPot;Message="Preparation complete. Warm the "+Recipe.Container.ToLowerInvariant()+" to the marked cooking band.";}
        else Message=Preparation.Current.Instruction;
    }
    public void Cut(double accuracy)
    {
        if (Terminal || Phase != CookingPhase.Chop || !KnifeSelected || Preparation.Current==null || !Preparation.Current.IsCut) return;
        if (double.IsNaN(accuracy) || double.IsInfinity(accuracy)) return;
        accuracy = Math.Max(0, Math.Min(1, accuracy));
        if (accuracy < .75) { Error("Missed guide: start at the top marker and trace down the line.",100,0); return; }
        accuracySum += accuracy; Cuts++;Preparation.Cut();
        FinishPreparationStep();
    }
    public void SetFlame(double value) { if (!Terminal && !double.IsNaN(value) && !double.IsInfinity(value)) Flame = Math.Max(0,Math.Min(1,value)); }
    public void AddToPot(string id)
    {
        if (Terminal || Phase < CookingPhase.HeatPot || Phase >= CookingPhase.Plate) return;
        string expected=(Phase==CookingPhase.Garlic||Phase==CookingPhase.Turmeric||Phase==CookingPhase.Meat||Phase==CookingPhase.Vinegar)?ExpectedIngredient:null;
        if(id!=expected||expected==null){Error("Follow the recipe order. Drag the highlighted ingredient into the cooking container.",100,0);return;}
        if(Heat<55){Message="The container is too cool. Raise the flame first.";return;}
        AdditionIndex++;
        if(Recipe.IsHabhab)
        {
            if(AdditionIndex==1)Phase=CookingPhase.Turmeric;
            else if(AdditionIndex==2)Phase=CookingPhase.Meat;
            else if(AdditionIndex==3)Phase=CookingPhase.Sear;
            else if(AdditionIndex==4)Phase=CookingPhase.Vinegar;
            else Phase=CookingPhase.Simmer;
            Message=Phase==CookingPhase.Sear?"Stir-fry the vegetables in the marked heat band.":Phase==CookingPhase.Simmer?"Toss gently and cook until the simulated noodle progress is complete.":"Next: "+RecipePlan.Name(ExpectedIngredient)+".";
        }
        else switch(Phase)
        {
            case CookingPhase.Garlic:Phase=CookingPhase.Turmeric;Message="Add prepared turmeric next.";break;
            case CookingPhase.Turmeric:Phase=CookingPhase.Meat;Message="Add the portioned chicken.";break;
            case CookingPhase.Meat:Phase=CookingPhase.Sear;Message="Keep the marked heat band for the simulated searing step.";break;
            case CookingPhase.Vinegar:Phase=CookingPhase.Simmer;Message="Simmer the vinegar initially unstirred in this selected recipe.";break;
        }
    }

    public void Stir()
    {
        if (Phase == CookingPhase.Simmer && !Recipe.IsHabhab) Error("Premature stirring: leave this recipe's initial vinegar simmer unstirred. -500 points.",500,0);
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
        if (Phase == CookingPhase.HeatPot && Heat >= 60) { Phase = CookingPhase.Garlic; Message = "Container ready. Drag the garlic into the opening."; }
        if (Phase == CookingPhase.Sear && Heat >= 60 && Heat <= 85)
        {
            SearProgress = Math.Min(4,SearProgress+dt);
            if (SearProgress >= 4) { Phase = CookingPhase.Vinegar; Message = Recipe.IsHabhab?"Vegetables ready. Add seasoned vegetable broth, then miki noodles.":"Searing complete. Add native vinegar."; }
        }
        if (Phase == CookingPhase.Simmer && Heat >= 60 && Heat <= 85)
        {
            SimmerProgress = Math.Min(SimmerSeconds,SimmerProgress+dt);
            if (SimmerProgress >= SimmerSeconds) { Phase = CookingPhase.Plate; Message = "Simulated cooking complete. Turn the flame off and serve "+Recipe.Dish+"."; }
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
