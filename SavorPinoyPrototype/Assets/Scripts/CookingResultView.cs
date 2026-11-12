using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Learner-facing widgets only. Save, query and navigation belong to the presenter.
public sealed class CookingResultView
{
    private readonly CookingStageView theme;
    private readonly RecipePlan recipe;
    private RectTransform panel,heritage,queryPanel;
    private TMP_Text heading,score,mode,chef,status,statusHeading,steps,identity,rows,queryHeading,diagnostic;
    private Image platedDish,practiceImage,burntFood;
    private RatingStarsGraphic stars;
    private readonly TMP_Text[] skillValues=new TMP_Text[3];
    private readonly RectTransform[] skillFills=new RectTransform[3];
    private Button nextProvinceButton;
    private Button queryRetryHistory,queryRetryScores;
    private string chefName,improvement;
    private bool correctResult;
    private static readonly Color Ink=new Color(.22f,.105f,.035f),Green=new Color(.12f,.33f,.19f),Cream=new Color(1,.96f,.83f);
    public Button SaveButton { get; private set; }
    public Button ContinueButton { get; private set; }
    public Button HistoryButton { get; private set; }
    public Button LeaderboardButton { get; private set; }
    public string Alias=>chefName;
    public event Action SaveRequested,ContinueRequested,HistoryRequested,LeaderboardRequested,RetryRequested,ExitRequested,NextProvinceRequested;

    public CookingResultView(CookingStageView theme,KitchenArt art)
    {
        this.theme=theme;recipe=RecipePlan.For(GameSessionState.SelectedProvince);
        panel=PrototypeUI.Panel("ResultOverlay",theme.Design,0,0,1800,1000,Ink).rectTransform;
        art.Draw("ResultKitchenBackdrop",panel,art.Environment,0,0,1800,1000,false).color=new Color(.48f,.36f,.25f);
        heading=theme.Label("ResultHeading",panel,"",0,440,1660,85,42);
        Card("SkillsParchment",panel,-410,0,900,730);Card("NextStepCard",panel,510,0,680,730);
        Text("ResultRecipe",panel,recipe.Dish,-410,315,810,65,36,true);
        chef=Text("ResultChef",panel,"",-410,265,810,45,25);
        platedDish=art.Draw("PlatedAdobo",panel,art.DishSprite(recipe),-645,135,350,235);
        practiceImage=art.Draw("PracticeCookingIllustration",panel,recipe.IsHabhab?art.Atlas(10):art.EmptyPalayok,-645,135,330,220);
        burntFood=art.Draw("BurntFoodIllustration",panel,art.IngredientSprite(recipe.IsHabhab?"noodles":"chicken",true),-645,155,155,80);burntFood.raycastTarget=false;
        stars=PrototypeUI.Rect("ResultStars",panel,-210,210,310,85).gameObject.AddComponent<RatingStarsGraphic>();stars.raycastTarget=false;
        score=Text("ResultScore",panel,"",-210,125,370,95,60,true);
        mode=Text("ResultMode",panel,"",-210,40,370,60,23);
        string[] names={"Knife accuracy","Recipe steps","Heat control"};
        for(int i=0;i<3;i++){
            float y=-35-i*80;
            var label=Text("SkillLabel"+i,panel,names[i],-605,y,390,42,27,true);label.alignment=TextAlignmentOptions.Left;
            skillValues[i]=Text("SkillValue"+i,panel,"",-155,y,280,42,25);skillValues[i].alignment=TextAlignmentOptions.Right;
            var track=PrototypeUI.Panel("SkillTrack"+i,panel,-410,y-33,780,13,new Color(.81f,.69f,.48f));track.raycastTarget=false;
            var fill=PrototypeUI.Panel("SkillFill"+i,track.transform,0,0,780,13,Green);fill.raycastTarget=false;
            skillFills[i]=fill.rectTransform;skillFills[i].anchorMin=skillFills[i].anchorMax=skillFills[i].pivot=new Vector2(0,.5f);skillFills[i].anchoredPosition=Vector2.zero;
        }
        Text("SkillScoresCaption",panel,"Skill points: each bar is out of 2,500",-410,-255,800,38,22);
        Text("TimeSummary",panel,"",-410,-310,810,62,24);
        Text("NextStepHeading",panel,"WHAT HAPPENS NEXT?",510,310,590,55,29,true);
        statusHeading=Text("ResultStatusHeading",panel,"",510,235,580,90,34,true);
        status=Text("SaveStatus",panel,"",510,120,570,145,27);
        steps=Text("CompletionSteps",panel,"",510,-55,560,140,25);steps.alignment=TextAlignmentOptions.Left;
        SaveButton=Primary("SaveAttemptButton",panel,"Save result",510,-210,570,80,()=>SaveRequested?.Invoke());
        ContinueButton=Primary("ContinueButton",panel,"Save first to continue",510,-300,570,70,()=>ContinueRequested?.Invoke());ContinueButton.interactable=false;
        Primary("RetryStage",panel,"Try recipe again",-650,-425,340,80,()=>RetryRequested?.Invoke());
        theme.ActionButton("ReturnMenu",panel,"Main menu",-280,-425,330,80,()=>ExitRequested?.Invoke());
        HistoryButton=theme.ActionButton("LoadAttemptsButton",panel,"My attempts",300,-425,300,80,()=>{OpenQuery("MY RECENT ATTEMPTS");HistoryRequested?.Invoke();});
        LeaderboardButton=theme.ActionButton("LoadLeaderboard",panel,"Top scores",650,-425,300,80,()=>{OpenQuery("TIMED CHALLENGE SCORES");LeaderboardRequested?.Invoke();});
        BuildQuery();BuildHeritage(art);theme.ApplyTextStyle(panel);theme.ApplyTextStyle(heritage);
        panel.gameObject.SetActive(false);heritage.gameObject.SetActive(false);
    }
    private TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,int size,bool bold=false)
    {var text=PrototypeUI.Text(name,parent,value,x,y,w,h,size);text.color=Ink;if(bold)text.fontStyle=FontStyles.Bold;return text;}
    private static void Card(string name,Transform parent,float x,float y,float w,float h)
    {PrototypeUI.Rect(name,parent,x,y,w,h).gameObject.AddComponent<RecipeCardFrameGraphic>().raycastTarget=false;}
    private Button Primary(string name,Transform parent,string label,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
    {
        var button=PrototypeUI.Button(name,parent,label,x,y,w,h,action);button.image.color=Green;
        var colors=button.colors;colors.disabledColor=new Color(.68f,.68f,.68f);colors.highlightedColor=new Color(.85f,1,.85f);button.colors=colors;
        var text=button.GetComponentInChildren<TMP_Text>();text.color=Cream;text.fontStyle=FontStyles.Bold;text.fontSize=28;return button;
    }
    private void BuildQuery()
    {
        queryPanel=PrototypeUI.Panel("HistoryOverlay",panel,0,0,1800,1000,new Color(.12f,.065f,.03f,.97f)).rectTransform;
        Card("HistoryParchment",queryPanel,0,0,1480,850);
        queryHeading=Text("HistoryHeading",queryPanel,"",-40,335,1120,70,36,true);
        theme.ActionButton("CloseHistory",queryPanel,"Back",610,335,190,70,()=>queryPanel.gameObject.SetActive(false));
        var viewport=PrototypeUI.Panel("AttemptQueryViewport",queryPanel,0,5,1320,490,new Color(.99f,.95f,.84f)).rectTransform;
        viewport.gameObject.AddComponent<RectMask2D>();var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        rows=Text("AttemptQueryRows",viewport,"",0,0,1260,490,28);rows.alignment=TextAlignmentOptions.TopLeft;
        rows.rectTransform.anchorMin=rows.rectTransform.anchorMax=new Vector2(.5f,1);rows.rectTransform.pivot=new Vector2(.5f,1);scroll.content=rows.rectTransform;
        queryRetryHistory=Primary("ReloadAttempts",queryPanel,"Reload attempts",-290,-285,470,70,()=>{queryHeading.text="MY RECENT ATTEMPTS";HistoryRequested?.Invoke();});
        queryRetryScores=Primary("ReloadScores",queryPanel,"Reload scores",290,-285,470,70,()=>{queryHeading.text="TIMED CHALLENGE SCORES";LeaderboardRequested?.Invoke();});
        identity=Text("ResultUid",queryPanel,"",0,-345,1290,35,18);
        diagnostic=Text("ConnectionDetails",queryPanel,"",0,-385,1290,45,18);queryPanel.gameObject.SetActive(false);
    }
    private void OpenQuery(string title){queryHeading.text=title;queryPanel.gameObject.SetActive(true);queryPanel.SetAsLastSibling();}
    private void BuildHeritage(KitchenArt art)
    {
        heritage=PrototypeUI.Panel("HeritageOverlay",theme.Design,0,0,1800,1000,new Color(.16f,.08f,.035f,.98f)).rectTransform;
        Card("HeritageParchment",heritage,0,0,1640,860);
        Text("HeritageHeading",heritage,"STAGE COMPLETE!",0,335,1450,80,45,true);
        art.Draw("HeritageDish",heritage,art.DishSprite(recipe),-485,80,510,340);
        Text("HeritageRecipe",heritage,recipe.Dish+"\n"+recipe.Province+" / CALABARZON",-485,-165,520,110,30,true);
        var body=Text("HeritageBody",heritage,recipe.Heritage,280,55,870,410,29);body.alignment=TextAlignmentOptions.Left;
        nextProvinceButton=Primary("NextProvince",heritage,"Explore "+(recipe.IsHabhab?"Batangas":"Quezon"),230,-295,600,90,()=>NextProvinceRequested?.Invoke());
        theme.ActionButton("CloseHeritage",heritage,"Back to result",-440,-295,500,90,()=>heritage.gameObject.SetActive(false));
    }
    public void Show(CookingAttempt attempt,string name)
    {
        chefName=name;correctResult=attempt.Phase==CookingPhase.Success;panel.gameObject.SetActive(true);queryPanel.gameObject.SetActive(false);heritage.gameObject.SetActive(false);
        heading.text=correctResult?"RECIPE COMPLETED":"PRACTICE RESULT";chef.text=name+"  /  "+recipe.Province;
        platedDish.gameObject.SetActive(correctResult);practiceImage.gameObject.SetActive(!correctResult);
        bool burnt=CookingVesselPresentation.Burnt(attempt);practiceImage.color=burnt?CookingVesselPresentation.VesselTint(attempt):Color.white;
        burntFood.gameObject.SetActive(burnt);burntFood.color=CookingVesselPresentation.FoodTint(attempt);stars.SetStars(attempt.Stars);
        score.text=attempt.FinalScore.ToString("N0")+" pts";mode.text=attempt.Stars+" / 3 stars / "+(attempt.TimedChallenge?"timed challenge":"guided practice")+"\n"+(attempt.TimedChallenge?"Ranked":"Not ranked")+" / trivia bonus "+attempt.Multiplier.ToString("0.##")+"x";
        int[] values={attempt.CuttingScore,attempt.SequenceScore,attempt.ThermalScore};
        for(int i=0;i<3;i++){skillFills[i].sizeDelta=new Vector2(780*Mathf.Clamp01(values[i]/2500f),13);skillValues[i].text=i==0?Mathf.RoundToInt((float)attempt.CuttingAccuracy*100)+"% accuracy":i==1?attempt.Mistakes+" mistakes":attempt.ScorchWarnings+" heat warnings";}
        panel.Find("TimeSummary").GetComponent<TMP_Text>().text="Preparation "+Clock(attempt.PrepElapsed)+"   |   Cooking "+Clock(attempt.CookElapsed)+"\nPenalties: "+attempt.Penalties+" pts";
        improvement=attempt.Mistakes>2?"Follow each highlighted preparation and cooking step on retry. "+attempt.Mistakes+" mistakes prevented completion.":attempt.CuttingAccuracy<.7?"Practise starting and finishing each guided cut steadily.":attempt.ScorchWarnings>0?"Lower the flame earlier to avoid overheating.":attempt.Message;
        if(attempt.Message.IndexOf("time expired",StringComparison.OrdinalIgnoreCase)>=0)improvement="The challenge timer ended. Choose Guided practice to learn without a countdown, or retry the challenge.";
        else if(attempt.Message.StartsWith("Burnt dish",StringComparison.Ordinal))improvement="The container stayed too hot. Lower the flame earlier and keep the heat in the marked band.";
    }
    public void RenderSave(AttemptSaveCoordinator save,string uid)
    {
        identity.text=string.IsNullOrEmpty(uid)?"Guest profile is not connected.":"Guest profile / authenticated UID: "+uid;diagnostic.text=save.Message;
        bool mismatch=!string.IsNullOrEmpty(save.OwnerUid)&&save.OwnerUid!=uid,confirmed=save.SaveSucceeded;
        SaveButton.interactable=!save.Busy&&!confirmed;
        SaveButton.GetComponentInChildren<TMP_Text>().text=save.Busy?(save.State==AttemptSaveState.ReadingBack?"Checking saved result...":"Saving result..."):confirmed?"Result saved":save.State==AttemptSaveState.Error?"Retry save":correctResult?"Save result":"Save practice result";
        ContinueButton.interactable=save.CanContinue;nextProvinceButton.interactable=save.CanContinue;
        ContinueButton.image.color=save.CanContinue?Green:new Color(.79f,.70f,.52f);
        ContinueButton.GetComponentInChildren<TMP_Text>().text=!correctResult?"Complete recipe to unlock":confirmed?"Continue / next province":"Save first to continue";
        if(!correctResult){statusHeading.text="TRY THE RECIPE AGAIN";status.text=(confirmed?"Your practice result is saved.\n\n":"")+improvement;statusHeading.color=Ink;}
        else if(mismatch){statusHeading.text="PROFILE CHANGED";status.text="This result belongs to your previous guest profile. Sign back into that profile to verify its save.";statusHeading.color=new Color(.58f,.18f,.10f);}
        else if(save.Busy){statusHeading.text="SAVING YOUR RESULT";status.text=save.State==AttemptSaveState.ReadingBack?"Your result reached the database. Checking it before unlocking the next province...":"Keep this screen open while we save your result.";statusHeading.color=Green;}
        else if(confirmed){statusHeading.text="NEXT PROVINCE READY";status.text="Your result was saved and checked. Continue to discover the dish's heritage and explore another province.";statusHeading.color=Green;}
        else if(save.State==AttemptSaveState.Error){statusHeading.text="SAVE NEEDS ATTENTION";status.text=save.Message.IndexOf("rules",StringComparison.OrdinalIgnoreCase)>=0?"The game's save settings need updating. Ask your teacher or project facilitator for help, then choose Retry save. Your result is kept for retry.":"We couldn't confirm your save. Check your connection, then choose Retry save. Your result is kept for retry.";statusHeading.color=new Color(.58f,.18f,.10f);}
        else{statusHeading.text="SAVE TO UNLOCK";status.text="Well done! Save your completed recipe to unlock the next province.";statusHeading.color=Green;}
        steps.text="1. Cook recipe: "+(correctResult?"Complete":"Try again")+"\n2. Save result: "+(confirmed?"Verified":save.Busy?"In progress":save.State==AttemptSaveState.Error?"Retry needed":"Not saved yet")+"\n3. Next province: "+(save.CanContinue?"Ready":"Locked");
    }
    public void RenderQuery(AttemptQueryController query)
    {
        rows.text=query.State==AttemptQueryState.Error?"We couldn't load the records. Check your connection or ask your teacher for help. Choose Reload attempts or Reload scores below to try again.":query.Text;
        if(query.State==AttemptQueryState.Error)diagnostic.text=query.Text;
        rows.rectTransform.sizeDelta=new Vector2(1260,Mathf.Max(490,rows.preferredHeight+25));HistoryButton.interactable=LeaderboardButton.interactable=queryRetryHistory.interactable=queryRetryScores.interactable=!query.Busy;
    }
    public void Complete(){heritage.gameObject.SetActive(true);heritage.SetAsLastSibling();}
    private static string Clock(double seconds){int value=Math.Max(0,(int)Math.Round(seconds));return (value/60).ToString("00")+":"+(value%60).ToString("00");}
}
