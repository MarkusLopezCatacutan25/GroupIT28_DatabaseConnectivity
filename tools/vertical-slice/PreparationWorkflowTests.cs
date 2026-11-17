using System;

public static class PreparationWorkflowTests
{
    private static int checks;
    private static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Console.WriteLine("PREPARATION PASS "+label);}
    private static void Tick(CookingAttempt a,double seconds){for(int i=0;i<(int)(seconds/.02);i++)a.Advance(.02);}
    public static void Run()
    {
        foreach(var plan in RecipePlan.All)
        {
            var a=new CookingAttempt(true,false,120,plan);a.ChooseKnife();Check(a.Phase==CookingPhase.Gather,"missing ingredients block preparation: "+plan.Dish);
            if(plan.Distractor!=null){a.Gather(plan.Distractor);Check(a.Mistakes==1&&!a.HasIngredient(plan.Distractor),"distractor rejected: "+plan.Dish);}foreach(string id in plan.Pantry)if(id!=plan.Distractor)a.Gather(id);a.ChooseKnife();
            a.Cut(1);Check(a.Cuts==0,"knife cannot skip hand hygiene: "+plan.Dish);
            a.Prepare("peel_garlic");Check(a.Preparation.Index==0,"out-of-order gesture cannot skip hygiene: "+plan.Dish);
            int guard=0;while(a.Phase==CookingPhase.Chop&&guard++<100){var step=a.Preparation.Current;int cuts=a.Cuts;if(step.IsCut){a.Cut(double.NaN);Check(a.Cuts==cuts,"NaN cut rejected: "+step.Id);a.Cut(1);}else a.Prepare(step.Id);}
            Check(a.Phase==CookingPhase.HeatPot&&a.Preparation.Complete&&a.Cuts==a.Preparation.RequiredCuts&&a.Cuts>0,"complete ordered preparation required: "+plan.Dish);
            a.SetFlame(.6);Tick(a,8);int count=a.AdditionIndex;a.AddToPot("unknown");Check(a.AdditionIndex==count,"wrong addition never progresses: "+plan.Dish);
            int iterations=0;while(a.Phase<CookingPhase.Plate&&!a.Terminal&&iterations++<30){if(a.Phase==CookingPhase.Sear)Tick(a,4.2);else if(a.Phase==CookingPhase.Simmer)Tick(a,13);else a.AddToPot(a.ExpectedIngredient);}
            Check(a.Phase==CookingPhase.Plate,"recipe-specific sequence reaches serving: "+plan.Dish);a.Plate();Check(a.Phase==CookingPhase.Plate,"flame must be off before serving: "+plan.Dish);a.SetFlame(0);a.Plate();
            Check(a.Phase==CookingPhase.Success,"full recipe succeeds: "+plan.Dish);Check(a.AdditionIndex==plan.Additions.Length,"every addition used: "+plan.Dish);Check(AttemptContract.From(a).dish_id==plan.Id,"selected dish is serialized: "+plan.Dish);
        }
        Check(!RecipePlan.Supported("invented_dish"),"unknown dishes remain rejected");
        Check(RecipePlan.InProvince("Batangas").Length==2&&RecipePlan.InProvince("Quezon").Length==2,"two dishes per province");
        foreach(var plan in RecipePlan.All){var b=new CookingAttempt(true,false,120,plan);Check(Math.Abs(b.CookingRemaining-plan.CookSeconds)<1e-9,"cook time scales with additions: "+plan.Dish);}
        Console.WriteLine("COMPLETE "+checks+" preparation checks passed; pure domain only.");
    }
}
