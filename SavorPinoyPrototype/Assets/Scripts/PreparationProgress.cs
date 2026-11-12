using System;

public sealed class PreparationStep
{
    public readonly string Id,Ingredient,Title,Instruction;
    public readonly bool IsCut;
    public readonly int Repetitions;
    public PreparationStep(string id,string ingredient,string title,string instruction,bool cut,int repetitions)
    {Id=id;Ingredient=ingredient;Title=title;Instruction=instruction;IsCut=cut;Repetitions=repetitions;}
}
// Ordered preparation state; UI gestures cannot skip peeling, meat separation or cleanup.
public sealed class PreparationProgress
{
    private readonly PreparationStep[] steps;
    public int Index { get; private set; }
    public int Repetition { get; private set; }
    public bool Complete=>Index>=steps.Length;
    public PreparationStep Current=>Complete?null:steps[Index];
    public int Count=>steps.Length;
    public int RequiredCuts { get; }
    public PreparationProgress(RecipePlan plan){steps=plan.Preparation;foreach(var step in steps)if(step.IsCut)RequiredCuts+=step.Repetitions;}
    public bool Gesture(string id){if(Complete||Current.IsCut||Current.Id!=id)return false;Next();return true;}
    public bool Cut(){if(Complete||!Current.IsCut)return false;Next();return true;}
    private void Next(){if(++Repetition>=Current.Repetitions){Index++;Repetition=0;}}
}
