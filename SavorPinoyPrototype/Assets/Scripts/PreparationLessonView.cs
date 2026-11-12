using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Preparation demonstrations and non-cut gestures. Ordered validation stays in the model.
public sealed class PreparationLessonView
{
    private readonly RectTransform group,ghostRoot;
    private readonly Image source,ghost,example;
    private readonly TMP_Text instruction,stepCount,sourceLabel,targetLabel;
    private readonly IngredientDrag gesture;
    private readonly KitchenArt art;
    private CookingAttempt model;
    private bool enabled;
    private string stepId;
    public PreparationLessonView(RectTransform closeup,RectTransform design,KitchenArt art,Action<string> action)
    {
        this.art=art;group=PrototypeUI.Rect("PreparationGesture",closeup,0,-15,1050,390);
        var target=PrototypeUI.Panel("PreparationTarget",group,300,0,290,230,new Color(.31f,.18f,.08f,.80f));
        targetLabel=PrototypeUI.Text("PreparationTargetLabel",target.transform,"",0,0,260,160,29);targetLabel.color=new Color(1,.94f,.78f);
        source=art.Draw("PreparationSource",group,null,-280,0,290,220);source.raycastTarget=true;
        sourceLabel=PrototypeUI.Text("PreparationSourceLabel",group,"",-280,-150,370,70,25);
        sourceLabel.color=new Color(.22f,.1f,.03f);
        PrototypeUI.Text("PreparationArrow",group,"> > >",0,0,180,60,44);
        example=art.Draw("PreparationExample",group,null,0,125,200,120);
        instruction=PrototypeUI.Text("PreparationInstruction",closeup,"",0,180,1450,80,28);instruction.color=new Color(.20f,.09f,.025f);
        stepCount=PrototypeUI.Text("PreparationStepCount",closeup,"",0,333,1380,28,23);stepCount.color=new Color(1,.94f,.78f);
        ghostRoot=PrototypeUI.Rect("PreparationGhostRoot",design,0,0,1800,1000);ghost=art.Draw("PreparationGhost",ghostRoot,null,0,0,220,180);ghostRoot.gameObject.SetActive(false);
        gesture=source.gameObject.AddComponent<IngredientDrag>();gesture.Target=target.rectTransform;
        gesture.CanDrag=()=>enabled&&model?.Preparation.Current!=null&&!model.Preparation.Current.IsCut;
        gesture.Begin=()=>{stepId=model.Preparation.Current.Id;ghost.sprite=source.sprite;ghostRoot.gameObject.SetActive(true);ghostRoot.SetAsLastSibling();return ghost.rectTransform;};
        gesture.End=(ok,point)=>{ghostRoot.gameObject.SetActive(false);if(ok&&enabled&&model.Preparation.Current?.Id==stepId)action("prepare:"+stepId);};
    }
    public void Render(CookingAttempt attempt,bool interactive)
    {
        model=attempt;enabled=interactive;var step=attempt.Preparation.Current;
        bool preparing=attempt.Phase==CookingPhase.Chop&&step!=null;group.gameObject.SetActive(preparing&&!step.IsCut);instruction.gameObject.SetActive(preparing);stepCount.gameObject.SetActive(preparing);
        if(!interactive)gesture.Cancel();if(!preparing)return;
        instruction.text=step.Instruction;stepCount.text="Preparation "+(attempt.Preparation.Index+1)+" / "+attempt.Preparation.Count+"   |   "+(attempt.Preparation.Repetition+1)+" / "+step.Repetitions;
        source.sprite=art.IngredientSprite(step.Ingredient,false);
        if(step.Id=="separate")source.sprite=art.Slice("garlic");
        if(step.Id=="peel_garlic")source.sprite=art.Atlas(0);
        sourceLabel.text=step.Ingredient=="hands"?"CLEANING KIT / DRAG":RecipePlan.Name(step.Ingredient)+" / DRAG";
        if(step.Ingredient=="hands"||step.Id=="meat_board")source.sprite=art.Slice("board");
        targetLabel.text=step.Id.Contains("wash")?"RUNNING WATER":step.Id=="hands"?"WASH HANDS":step.Id=="meat_board"?"RAW-MEAT STATION":step.Id=="clean_after_meat"?"CLEAN STATION":step.Id=="separate"?"SEPARATE CLOVES":"REMOVE SKIN";
        example.sprite=step.Id=="separate"?art.Atlas(0):step.Id=="peel_garlic"?art.Atlas(1):null;
        example.gameObject.SetActive(example.sprite);
        source.color=Color.Lerp(Color.white,new Color(1,.9f,.67f),attempt.Preparation.Repetition/(float)step.Repetitions);
    }
    public void Cancel(){gesture.Cancel();ghostRoot.gameObject.SetActive(false);}
}
