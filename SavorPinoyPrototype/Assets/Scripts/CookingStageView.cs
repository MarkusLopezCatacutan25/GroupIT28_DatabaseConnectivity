using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Kitchen presentation and pointer bindings only; rules live in CookingAttempt.
public sealed class CookingStageView : MonoBehaviour
{
    private RectTransform safe, design, pausePanel, board;
    private TMP_Text phaseText,timerText,objectiveText,feedbackText,heatText,potText,ingredientText;
    private TMP_InputField startingAlias;
    private Button knifeButton,spoonButton,plateButton,stirButton;
    private Button[] pantryButtons,potButtons;
    private IngredientDrag[] ingredientDrags;
    private PreparationLessonView preparationView;
    private CookingFocusView focusView;
    private Button adjustHeat,inspectPot;
    private RecipePlan recipePlan;
    private Slider flame;
    private Image guide, flameVisual;
    private Image potBody, boardIngredient,potContents;
    private Image garlicCutPieces, turmericCutPieces;
    private Image otherCutPieces,rawMeatBoard;
    private float cutFeedbackStarted=-100;
    private KitchenArt art;
    private TMP_Text traceHint;
    private Material readableText;
    private RectTransform preparedTray, kitchenScene, closeupScene, modePanel, feedbackGroup, heatGroup, stoveControls;
    private Image knifeBlade;
    private KitchenCueGraphic slicedPieces;
    private KitchenCueGraphic steam,bubbles;


    private TMP_Text closeupTitle, clockLabel, modeNote;
    private Button startCooking;
    private readonly KitchenCueGraphic[] ingredientHalos=new KitchenCueGraphic[4];
    private readonly TMP_Text[] ingredientLabels=new TMP_Text[4];
    private string[] pantryIds;

    private int lastAccepted=-1,lastPreparation=-1;
    private float feedbackUntil;
    private string lastMessage;
    private ChoppingSurface chopping;
    private Rect lastSafe;
    private Vector2Int lastSize;

    private CookingPhase displayedPhase=(CookingPhase)(-1);
    private bool paused,choosingMode;
    private CookingAttempt attempt;
    private TMP_FontAsset fontAsset;
    private TMP_Text identityText;
    public event Action<string> ActionRequested;
    public event Action<bool> ModeRequested;
    public event Action<float> FlameChanged;
    public event Action<double> CutRequested;
    public RectTransform Design => design;
    public string StartingAlias => startingAlias.text;
    public void SetModeFeedback(string message){modeNote.text=message;}
    public void SetIdentity(string uid){identityText.text=string.IsNullOrEmpty(uid)?"Your guest profile is connecting...":"Chef: "+(ChefAccessSession.DisplayName??"Chef")+" / guest profile ready";}
    public void SetFlame(float value){flame.SetValueWithoutNotify(value);}
    public void ShowMode(bool value){modePanel.gameObject.SetActive(value);}
    public void ShowPause(bool value){chopping.CancelStroke();preparationView.Cancel();focusView.Cancel();pausePanel.gameObject.SetActive(value);}
    public void ShowTerminal(){pausePanel.gameObject.SetActive(false);flame.SetValueWithoutNotify(0);}
    public void FitIfNeeded(){if(lastSafe!=Screen.safeArea||lastSize!=new Vector2Int(Screen.width,Screen.height))Fit();}
    public void Initialize(CookingAttempt model,TMP_FontAsset font){attempt=model;fontAsset=font;PrototypeUI.Font=font;BuildView();Fit();}
    public void ApplyTextStyle(Transform parent){foreach(TMP_Text text in parent.GetComponentsInChildren<TMP_Text>(true))text.fontSharedMaterial=readableText;}
    private static readonly Color Parchment=new Color(1,.94f,.78f);
    private static readonly Color Wood=new Color(.24f,.115f,.055f);
    public TMP_Text Label(string name,Transform parent,string value,float x,float y,float w,float h,int size=28)
    {
        PrototypeUI.Panel(name+"Shadow",parent,x,y-3,w+8,h+6,new Color(.12f,.065f,.03f,.8f)).raycastTarget=false;
        PrototypeUI.Panel(name+"Wood",parent,x,y,w,h,Wood).raycastTarget=false;
        TMP_Text text=PrototypeUI.Text(name,parent,value,x,y,w-16,h-4,size);text.color=Parchment;text.fontStyle=FontStyles.Bold;return text;
    }
    public Button ActionButton(string name,Transform parent,string value,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
    {
        Button b=PrototypeUI.Button(name,parent,value,x,y,w,h,action);b.image.sprite=art.Slice("board");b.image.type=Image.Type.Simple;b.image.color=Color.white;
        var text=b.GetComponentInChildren<TMP_Text>();text.fontStyle=FontStyles.Bold;text.color=Wood;text.fontSize=27;return b;
    }
    private KitchenCueGraphic Halo(string name,Transform parent,float x,float y,float w,float h)
    {
        var ring=PrototypeUI.Rect(name,parent,x,y,w,h).gameObject.AddComponent<KitchenCueGraphic>();ring.color=new Color(1,.83f,.30f);ring.raycastTarget=false;return ring;
    }
    private void BuildView()
    {
        art=GetComponent<KitchenArt>();recipePlan=RecipePlan.For(GameSessionState.SelectedProvince);pantryIds=recipePlan.Pantry;
        GameObject go=new GameObject("KitchenCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        Canvas canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        Image backdrop=PrototypeUI.Panel("KitchenBackdrop",canvas.transform,0,0,1920,1080,Wood);backdrop.rectTransform.anchorMin=Vector2.zero;backdrop.rectTransform.anchorMax=Vector2.one;backdrop.rectTransform.sizeDelta=Vector2.zero;backdrop.raycastTarget=false;
        safe=PrototypeUI.Rect("SafeAreaRoot",canvas.transform,0,0,0,0);design=PrototypeUI.Rect("KitchenDesign",safe,0,0,1800,1000);
        kitchenScene=PrototypeUI.Rect("KitchenStations",design,0,0,1800,1000);
        art.Draw("StoryboardKitchen",kitchenScene,art.Environment,0,0,1800,1000,false);
        Label("PrepStation",kitchenScene,"PREP STATION",-425,-330,470,65,32);
        Label("CookingStation",kitchenScene,"COOKING STATION",405,-390,470,60,28);
        Label("RecipeHeading",kitchenScene,"HERITAGE RECIPE",735,342,270,65,27);
        var recipeCard=PrototypeUI.Rect("RecipeCard",kitchenScene,735,145,255,325);
        recipeCard.gameObject.AddComponent<RecipeCardFrameGraphic>().raycastTarget=false;
        art.Draw("RecipeGoal",recipeCard,art.DishSprite(recipePlan),0,35,230,185);
        var dishName=PrototypeUI.Text("RecipeDishName",recipeCard,recipePlan.Dish,0,-100,225,85,24);dishName.color=Wood;dishName.fontStyle=FontStyles.Bold;
        Label("RecipeRegion",kitchenScene,recipePlan.Province+" / CALABARZON",735,-45,280,45,20);
        pantryButtons=new Button[4];float[] px={-590,-370,-155,-170},py={75,75,-160,90};
        string[] labels=Array.ConvertAll(pantryIds,RecipePlan.Name);
        for(int i=0;i<4;i++)
        {
            int index=i;string id=pantryIds[i];float w=i==2?205:155,h=i==3?130:120;
            pantryButtons[i]=PrototypeUI.Button("Gather_"+id,kitchenScene,"",px[i],py[i],w+40,h+55,()=>{ActionRequested?.Invoke("gather:"+id);});
            pantryButtons[i].image.color=Color.clear;pantryButtons[i].transition=Selectable.Transition.None;
            ingredientHalos[i]=Halo("Halo_"+id,pantryButtons[i].transform,0,0,w+25,h+28);
            art.Draw("Ingredient_"+id,pantryButtons[i].transform,art.IngredientSprite(id,false),0,5,w,h);
            ingredientLabels[i]=Label("Label_"+id,pantryButtons[i].transform,labels[i],0,-h*.5f-25,w+45,42,21);
        }
        var overviewBoard=art.Draw("OverviewBoard",kitchenScene,art.Slice("board"),-515,-140,420,210);
        preparedTray=PrototypeUI.Rect("PreparedTray",overviewBoard.transform,0,0,350,100);
        art.Draw("PreparedGarlic",preparedTray,art.PreparedGarlic?art.PreparedGarlic:art.Slice("garlic"),-100,0,150,100);art.Draw("PreparedTurmeric",preparedTray,recipePlan.IsHabhab?art.IngredientSprite("onion",true):art.PreparedTurmeric?art.PreparedTurmeric:art.Turmeric,30,0,150,100);
        knifeButton=PrototypeUI.Button("Knife",kitchenScene,"",-515,-130,400,175,()=>{ActionRequested?.Invoke("knife");});knifeButton.image.color=Color.clear;knifeButton.transition=Selectable.Transition.None;
        art.Draw("KnifeOnBoard",knifeButton.transform,art.Knife,0,0,300,150);
        Label("KnifeLabel",knifeButton.transform,"KNIFE / Tap to prepare",0,-115,350,48,23);
        var knifeDrag=knifeButton.gameObject.AddComponent<KnifeTool>();knifeDrag.Board=overviewBoard.rectTransform;knifeDrag.CanSelect=()=>!paused&&!choosingMode&&attempt.Phase==CookingPhase.Gather;knifeDrag.Selected=()=>{ActionRequested?.Invoke("knife");};
        spoonButton=ActionButton("Spoon",kitchenScene,"Spoon",-750,-330,130,60,()=>{ActionRequested?.Invoke("spoon");});
        potBody=art.Draw("PotBody",kitchenScene,art.Slice("pot"),330,10,370,285);
        potContents=art.Draw("OverviewPotContents",kitchenScene,null,330,60,190,90);
        steam=PrototypeUI.Rect("Steam",kitchenScene,330,125,120,100).gameObject.AddComponent<KitchenCueGraphic>();steam.Steam=true;steam.raycastTarget=false;
        bubbles=PrototypeUI.Rect("SimmerBubbles",kitchenScene,330,65,170,70).gameObject.AddComponent<KitchenCueGraphic>();bubbles.Boiling=true;bubbles.raycastTarget=false;
        flameVisual=PrototypeUI.Panel("Flame",kitchenScene,330,-90,135,12,new Color(1,.48f,.12f));flameVisual.raycastTarget=false;flameVisual.transform.SetSiblingIndex(potBody.transform.GetSiblingIndex());
        heatGroup=PrototypeUI.Rect("HeatGroup",kitchenScene,330,210,380,75);heatText=Label("Heat",heatGroup,"",0,0,380,75,25);
        potText=Label("PotState",kitchenScene,"",330,-165,405,70,25);
        stoveControls=PrototypeUI.Rect("StoveControls",kitchenScene,0,0,1800,1000);
        flame=PrototypeUI.HeatSlider(stoveControls,350,-225,360);
        flame.handleRect.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(324,38);flame.handleRect.sizeDelta=new Vector2(40,0);
        flame.handleRect.GetComponent<Image>().enabled=false;var dial=PrototypeUI.Rect("HeatDial",flame.handleRect,0,0,40,38).gameObject.AddComponent<KitchenCueGraphic>();dial.Dial=true;flame.targetGraphic=dial;flame.onValueChanged.AddListener(value=>{FlameChanged?.Invoke(value);});
        Label("FlameLegend",stoveControls,"OFF    LOW    MEDIUM    HIGH",350,-270,440,30,18);
        potButtons=new Button[0];
        focusView=gameObject.AddComponent<CookingFocusView>();focusView.Initialize(design,art,a=>ActionRequested?.Invoke(a),v=>{SetFlame(v);FlameChanged?.Invoke(v);});
        ingredientDrags=new IngredientDrag[recipePlan.Additions.Length];
        for(int i=0;i<recipePlan.Additions.Length;i++)
        {
            string id=recipePlan.Additions[i];int index=i;
            var item=art.Draw("Drag_"+id,kitchenScene,art.IngredientSprite(id,true),-685+(i%3)*195,-90-(i/3)*170,165,115);item.raycastTarget=true;
            Label("DragLabel_"+id,item.transform,RecipePlan.Name(id),0,-85,190,46,20);
            var drag=item.gameObject.AddComponent<IngredientDrag>();ingredientDrags[i]=drag;drag.Target=focusView.DropTarget;
            drag.CanDrag=()=>!paused&&!choosingMode&&!attempt.Terminal&&attempt.Phase>=CookingPhase.Garlic&&attempt.Phase<CookingPhase.Plate&&Array.IndexOf(recipePlan.Additions,id)>=attempt.AdditionIndex;
            // During a drag the owner is busy; release validation checks the session instead.
            drag.Begin=()=>focusView.BeginIngredient(id);drag.End=(ok,point)=>focusView.EndIngredient(id,ok,point);
        }
        adjustHeat=ActionButton("AdjustHeat",kitchenScene,"Adjust heat / zoom",350,-225,400,80,()=>focusView.OpenHeat());
        inspectPot=ActionButton("InspectContainer",kitchenScene,"Inspect cooking",330,10,365,235,()=>focusView.OpenHeat());inspectPot.image.color=Color.clear;inspectPot.GetComponentInChildren<TMP_Text>().text="";
        stirButton=ActionButton("Stir",kitchenScene,"Stir",205,-315,185,65,()=>{ActionRequested?.Invoke("stir");});
        plateButton=ActionButton("Plate",kitchenScene,"Plate dish",480,-315,250,65,()=>{ActionRequested?.Invoke("plate");});
        startCooking=ActionButton("StartCooking",kitchenScene,"Start cooking",-415,-240,420,75,()=>{ActionRequested?.Invoke("warm");});
        closeupScene=PrototypeUI.Rect("ChoppingCloseup",design,0,0,1800,1000);
        art.Draw("ChoppingBackground",closeupScene,art.Closeup,0,0,1800,1000,false);
        closeupTitle=Label("CloseupTitle",closeupScene,"",0,270,940,75,36);
        board=PrototypeUI.Panel("CuttingBoard",closeupScene,0,-35,950,420,Color.clear).rectTransform;
        rawMeatBoard=art.Draw("SeparateRawMeatBoard",board,art.Slice("board"),0,0,950,420);rawMeatBoard.color=new Color(1,.75f,.72f);rawMeatBoard.gameObject.SetActive(false);
        chopping=board.gameObject.AddComponent<ChoppingSurface>();chopping.CanCut=()=>!paused&&!choosingMode&&attempt.Phase==CookingPhase.Chop&&attempt.Preparation.Current!=null&&attempt.Preparation.Current.IsCut;chopping.AcceptedCuts=()=>attempt.Cuts;chopping.Completed=accuracy=>{CutRequested?.Invoke(accuracy);};
        boardIngredient=art.Draw("BoardIngredient",board,art.Slice("garlic"),0,0,380,260);
        var piecesRect=PrototypeUI.Rect("ChoppedPieces",board,-255,-120,200,85);slicedPieces=piecesRect.gameObject.AddComponent<KitchenCueGraphic>();slicedPieces.Ring=false;slicedPieces.raycastTarget=false;
        garlicCutPieces=art.Draw("GarlicCutPieces",board,art.PreparedGarlic,-280,-125,260,160);garlicCutPieces.gameObject.SetActive(false);
        turmericCutPieces=art.Draw("TurmericCutPieces",board,art.PreparedTurmeric,280,-125,260,160);turmericCutPieces.gameObject.SetActive(false);
        otherCutPieces=art.Draw("OtherCutPieces",board,null,-280,-125,260,160);otherCutPieces.gameObject.SetActive(false);
        guide=PrototypeUI.Panel("CutGuide",board,0,0,5,252,new Color(.38f,.95f,.56f));guide.raycastTarget=false;
        Halo("StartMarker",guide.transform,0,126,42,42);Halo("FinishMarker",guide.transform,0,-126,42,42);
        Label("GuideStart",guide.transform,"START",-100,126,130,42,23);Label("GuideEnd",guide.transform,"FINISH",-100,-126,130,42,23);
        knifeBlade=art.Draw("ActiveKnife",board,art.Knife,0,0,280,170);
        chopping.StrokeMoved=p=>{knifeBlade.rectTransform.anchoredPosition=p+new Vector2(80,-70);};
        chopping.StrokeActive=active=>{if(!active&&attempt!=null)ResetKnife();};
        preparationView=new PreparationLessonView(closeupScene,design,art,a=>ActionRequested?.Invoke(a));
        ingredientText=Label("CutProgress",closeupScene,"",0,-325,740,60,30);
        traceHint=Label("TraceHint",closeupScene,"",0,-385,1220,50,26);
        Label("KitchenBrand",design,"SAVORPINOY",-650,438,450,78,39);
        phaseText=Label("Phase",design,"",-85,438,530,78,30);
        clockLabel=Label("ClockLabel",design,"",520,469,300,42,19);
        timerText=Label("Timer",design,"",520,421,300,62,39);
        identityText=Label("AuthenticatedUid",design,"Your guest profile is connecting...",-160,370,1250,42,19);
        ActionButton("PauseButton",design,"Pause",785,438,155,75,()=>ActionRequested?.Invoke("pause"));
        objectiveText=Label("Objective",design,"",0,-468,1740,74,29);
        feedbackGroup=PrototypeUI.Rect("FeedbackToast",design,0,280,1090,70);feedbackText=Label("Feedback",feedbackGroup,"",0,0,1090,70,26);
        focusView.transform.SetAsLastSibling();BuildPause();BuildMode();
        readableText=new Material(fontAsset.material);readableText.SetFloat("_OutlineWidth",0);readableText.SetFloat("_FaceDilate",0);readableText.DisableKeyword("UNDERLAY_ON");
        foreach(TMP_Text text in design.GetComponentsInChildren<TMP_Text>(true))text.fontSharedMaterial=readableText;
    }
    private void BuildMode()
    {
        modePanel=PrototypeUI.Panel("ChooseLearningMode",design,0,0,1800,1000,new Color(.12f,.06f,.025f,.90f)).rectTransform;
        Label("Welcome",modePanel,"LET'S MAKE "+recipePlan.Dish.ToUpperInvariant(),0,330,1200,100,43);
        TMP_Text intro=PrototypeUI.Text("ModeIntro",modePanel,"Choose how you want to learn. Follow one step at a time.",0,235,1380,70,30);intro.color=Parchment;
        startingAlias=PrototypeUI.Input("StartingChefAlias",modePanel,0,135,650,70);startingAlias.text=ChefAccessSession.DisplayName??PlayerPrefs.GetString("SavorPinoy.ChefAlias","Chef");startingAlias.interactable=false;
        ActionButton("GuidedPractice",modePanel,"Guided practice / start here",-345,-5,600,105,()=>ModeRequested?.Invoke(false));
        ActionButton("TimedChallenge",modePanel,"Timed challenge",345,-5,600,105,()=>ModeRequested?.Invoke(true));
        var practice=PrototypeUI.Text("PracticeInfo",modePanel,"BEGINNER FRIENDLY\nElapsed-time clock. No expiry.\nPractice cuts, order, and heat at your pace.",-345,-160,590,200,29);practice.color=Parchment;
        var challenge=PrototypeUI.Text("ChallengeInfo",modePanel,"READY FOR A CHALLENGE?\n4-minute prep + trivia time bonus.\n75-second cooking countdown. Ranked score.",345,-160,590,200,29);challenge.color=Parchment;
        modeNote=PrototypeUI.Text("ModeNote",modePanel,"Measured oil and seasonings are supplied for this simplified lesson.\nYour trivia reward is kept. Practice results stay separate from ranked scores.",0,-345,1450,100,25);modeNote.color=Parchment;
    }
    private void ResetKnife()
    {
        if(!knifeBlade||!board)return;float x=(.3f+.2f*(attempt.Cuts%3)-.5f)*board.rect.width;
        knifeBlade.rectTransform.anchoredPosition=new Vector2(x+80,board.rect.height*.3f-70);
    }
    private void Panel(string name,float x,float y,float w,float h){PrototypeUI.Panel(name,design,x,y,w,h,new Color(.97f,.91f,.77f)).raycastTarget=false;}
    private void BuildPause()
    {
        pausePanel=PrototypeUI.Panel("PauseOverlay",design,0,0,1760,960,new Color(.12f,.06f,.025f,.94f)).rectTransform;
        Label("PausedTitle",pausePanel,"Paused",0,160,1000,100,56);
        Label("PauseInfo",pausePanel,"Your current attempt is paused. Returning to the menu abandons this unfinished attempt.",0,30,1200,120,34);
        ActionButton("Resume",pausePanel,"Resume",0,-130,360,85,()=>ActionRequested?.Invoke("resume"));
        ActionButton("LeaveStage",pausePanel,"Leave to Main Menu",0,-245,520,85,()=>ActionRequested?.Invoke("exit"));pausePanel.gameObject.SetActive(false);
    }
    private void Fit()
    {
        if(!safe || Screen.width<=0 || Screen.height<=0)return;Rect area=Screen.safeArea;lastSafe=area;lastSize=new Vector2Int(Screen.width,Screen.height);
        safe.anchorMin=new Vector2(area.x/Screen.width,area.y/Screen.height);safe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
        Canvas.ForceUpdateCanvases();design.localScale=Vector3.one*Mathf.Max(.1f,Mathf.Min((safe.rect.width-24)/1800,(safe.rect.height-24)/1000));
    }
    public void Render(CookingAttempt model,bool isPaused,bool selecting)
    {
        attempt=model;paused=isPaused;choosingMode=selecting;
        if(attempt==null)return;
        bool preparing=attempt.Phase<=CookingPhase.Chop, cutting=attempt.Phase==CookingPhase.Chop;
        clockLabel.text=attempt.TimedChallenge?(preparing?"PREP TIME LEFT":"COOK TIME LEFT"):"ACTIVE TIME / PRACTICE";
        timerText.text=attempt.TimedChallenge?Clock(preparing?attempt.Remaining:attempt.CookingRemaining):Stopwatch(attempt.Elapsed);
        phaseText.text=attempt.Terminal?"REVIEW YOUR DISH":preparing?(cutting?"2 / PREPARE":"1 / GATHER"):"3 / COOK";
        objectiveText.text=attempt.Phase==CookingPhase.Gather?"Gather the highlighted ingredients, then choose Knife.":attempt.Phase==CookingPhase.Chop?"Follow the preparation steps. Take your time in Guided practice.":attempt.Phase==CookingPhase.HeatPot?"Tap Start cooking to warm the cooking container.":attempt.Phase==CookingPhase.Simmer?(recipePlan.IsHabhab?"Cook noodles in broth. Simulated progress; follow real package directions outside the game.":"Simulated vinegar simmer. Real chicken must reach 74 C / 165 F using a food thermometer."):attempt.ExpectedIngredient!=null&&attempt.Phase!=CookingPhase.Sear?"Drag "+RecipePlan.Name(attempt.ExpectedIngredient)+" into the "+recipePlan.Container.ToLowerInvariant()+".":Objective(attempt.Phase);
        if(lastMessage!=attempt.Message){lastMessage=attempt.Message;feedbackUntil=Time.unscaledTime+3;feedbackText.text=attempt.Message;}
        bool showFeedback=Time.unscaledTime<feedbackUntil && (attempt.Mistakes>0 || cutting&&attempt.Cuts>0);
        feedbackGroup.gameObject.SetActive(showFeedback&&!cutting);
        kitchenScene.gameObject.SetActive(!cutting);closeupScene.gameObject.SetActive(cutting);
        for(int i=0;i<4;i++)
        {
            bool collected=attempt.HasIngredient(pantryIds[i]);pantryButtons[i].gameObject.SetActive(preparing);
            pantryButtons[i].interactable=!paused&&!choosingMode&&attempt.Phase==CookingPhase.Gather&&!collected;
            ingredientHalos[i].color=collected?new Color(.45f,1,.55f):new Color(1,.83f,.3f);ingredientLabels[i].text=(collected?"READY / ":"")+RecipePlan.Name(pantryIds[i]);
            ingredientLabels[i].fontSize=collected?18:21;
        }
        knifeButton.gameObject.SetActive(preparing);knifeButton.interactable=!paused&&!choosingMode&&attempt.Phase==CookingPhase.Gather;
        spoonButton.gameObject.SetActive(preparing);spoonButton.interactable=!paused&&!choosingMode;
        bool canCook=!paused&&!choosingMode&&!attempt.Terminal&&!preparing;
        for(int i=0;i<ingredientDrags.Length;i++){
            var drag=ingredientDrags[i];bool available=!preparing&&attempt.Phase!=CookingPhase.HeatPot&&i>=attempt.AdditionIndex&&attempt.Phase<CookingPhase.Plate;
            if(!canCook)drag.Cancel();drag.gameObject.SetActive(available);
            drag.GetComponent<Image>().color=recipePlan.Additions[i]==attempt.ExpectedIngredient?Color.white:new Color(.65f,.65f,.65f);
        }
        focusView.Render(attempt,canCook);adjustHeat.gameObject.SetActive(!preparing);adjustHeat.interactable=canCook;inspectPot.interactable=canCook;
        preparationView.Render(attempt,!paused&&!choosingMode&&cutting);
        startCooking.gameObject.SetActive(attempt.Phase==CookingPhase.HeatPot);startCooking.interactable=canCook;
        flame.gameObject.SetActive(false);flame.interactable=false;
        stirButton.gameObject.SetActive(!preparing);stirButton.interactable=canCook&&attempt.Phase==CookingPhase.Simmer;
        plateButton.gameObject.SetActive(!preparing);plateButton.interactable=canCook&&attempt.Phase==CookingPhase.Plate;
        heatGroup.gameObject.SetActive(!preparing);stoveControls.gameObject.SetActive(!preparing);potText.text=preparing?(recipePlan.IsHabhab?"KAWALI / COOKING PAN":"PALAYOK / CLAY POT"):CookingVesselPresentation.Burnt(attempt)?"BURNT / LOWER HEAT EARLIER":attempt.Phase==CookingPhase.Simmer?"SIMMER / "+Mathf.FloorToInt((float)attempt.SimmerProgress)+" of 12s":attempt.Phase==CookingPhase.Sear?(recipePlan.IsHabhab?"STIR-FRY / ":"SEAR / ")+Mathf.FloorToInt((float)attempt.SearProgress)+" of 4s":"FOLLOW THE RECIPE";
        heatText.text="HEAT "+Mathf.RoundToInt((float)attempt.Heat)+" / 110\nKeep in the 60-85 band";
        flameVisual.gameObject.SetActive(!preparing&&attempt.Flame>.01);flameVisual.color=Color.Lerp(new Color(.8f,.27f,.04f),new Color(1,.68f,.1f),(float)attempt.Flame);
        bool burnt=CookingVesselPresentation.Burnt(attempt);
        steam.Smoke=burnt;steam.gameObject.SetActive(!preparing&&(burnt||CookingVesselPresentation.Steaming(attempt)));
        steam.enabled=!paused&&!choosingMode;
        bubbles.gameObject.SetActive(!preparing&&CookingVesselPresentation.Bubbling(attempt));bubbles.enabled=!paused&&!choosingMode;
        potBody.sprite=recipePlan.IsHabhab?art.Atlas(10):art.EmptyPalayok;
        potBody.color=CookingVesselPresentation.VesselTint(attempt);
        preparedTray.gameObject.SetActive(attempt.Phase==CookingPhase.HeatPot);
        potContents.gameObject.SetActive(!preparing&&attempt.AdditionIndex>0);
        string visibleFood=recipePlan.IsHabhab?(attempt.AdditionIndex>=5?"noodles":attempt.AdditionIndex>=3?"vegetables":attempt.AdditionIndex>=2?"onion":"garlic"):(attempt.AdditionIndex>=3?"chicken":attempt.AdditionIndex>=2?"turmeric":"garlic");
        potContents.sprite=art.IngredientSprite(visibleFood,true);potContents.color=CookingVesselPresentation.FoodTint(attempt);
        if(lastAccepted!=attempt.Cuts||displayedPhase!=attempt.Phase||lastPreparation!=attempt.Preparation.Index)
        {
            if(lastAccepted>=0&&attempt.Cuts>lastAccepted)cutFeedbackStarted=Time.unscaledTime;
            lastAccepted=attempt.Cuts;displayedPhase=attempt.Phase;lastPreparation=attempt.Preparation.Index;
            bool turmeric=attempt.Preparation.Current?.Ingredient=="turmeric";boardIngredient.sprite=art.IngredientSprite(attempt.Preparation.Current?.Ingredient??"garlic",false);if(attempt.Preparation.Current?.IsCut==true){if(turmeric)boardIngredient.sprite=art.Detail(3);else if(attempt.Preparation.Current.Ingredient=="onion")boardIngredient.sprite=art.Atlas(3);}
            closeupTitle.text=attempt.Preparation.Current?.Title??"PREPARATION COMPLETE";
            boardIngredient.rectTransform.sizeDelta=new Vector2(380-(attempt.Cuts%3)*45,260-(attempt.Cuts%3)*25);
            slicedPieces.Turmeric=turmeric;slicedPieces.Pieces=(attempt.Cuts%3)*4;slicedPieces.SetVerticesDirty();
            slicedPieces.gameObject.SetActive(turmeric?!art.PreparedTurmeric:!art.PreparedGarlic);
            ingredientText.text="Accepted cuts: "+attempt.Cuts+" / "+attempt.Preparation.RequiredCuts;
            guide.rectTransform.anchoredPosition=new Vector2((.3f+.2f*(attempt.Cuts%3)-.5f)*board.rect.width,0);boardIngredient.rectTransform.anchoredPosition=guide.rectTransform.anchoredPosition;ResetKnife();
        }
        bool cutStep=attempt.Preparation.Current!=null&&attempt.Preparation.Current.IsCut;
        rawMeatBoard.gameObject.SetActive(cutStep&&attempt.Preparation.Current.Ingredient=="chicken");
        otherCutPieces.gameObject.SetActive(cutStep&&attempt.Preparation.Repetition>0&&attempt.Preparation.Current.Ingredient!="garlic"&&attempt.Preparation.Current.Ingredient!="turmeric");
        if(cutStep){otherCutPieces.sprite=attempt.Preparation.Current.Id=="plank_carrot"?art.Detail(1):art.IngredientSprite(attempt.Preparation.Current.Ingredient,true);otherCutPieces.rectTransform.localScale=Vector3.one*Mathf.Lerp(.6f,1,attempt.Preparation.Repetition/3f);}
        boardIngredient.gameObject.SetActive(cutStep);guide.gameObject.SetActive(cutStep);knifeBlade.gameObject.SetActive(cutStep);slicedPieces.gameObject.SetActive(false);
        // Prepared sprites appear only after accepted strokes; invalid drags cannot reveal progress.
        garlicCutPieces.gameObject.SetActive(cutStep&&attempt.Preparation.Current.Ingredient=="garlic"&&art.PreparedGarlic&&attempt.Cuts>0);
        turmericCutPieces.gameObject.SetActive(cutStep&&attempt.Preparation.Current.Ingredient=="turmeric"&&art.PreparedTurmeric&&attempt.Cuts>3);
        float pulse=.10f*Mathf.Sin(Mathf.Clamp01((Time.unscaledTime-cutFeedbackStarted)/.22f)*Mathf.PI);
        garlicCutPieces.rectTransform.localScale=Vector3.one*(Mathf.Lerp(.65f,1,Mathf.Clamp01(attempt.Cuts/3f))+(attempt.Cuts<=3?pulse:0));
        turmericCutPieces.rectTransform.localScale=Vector3.one*(Mathf.Lerp(.65f,1,Mathf.Clamp01((attempt.Cuts-3)/3f))+(attempt.Cuts>3?pulse:0));
        traceHint.text=cutStep?"Touch START. Drag down to FINISH. Keep the ingredient stable and fingertips curled back.":"Drag the highlighted item to its target. Follow each preparation step before cooking.";

    }
    private string Objective(CookingPhase phase)
    {
        switch(phase)
        {
            case CookingPhase.Gather:return "Tap garlic, luyang dilaw, and chicken. Then tap the knife on the board.";
            case CookingPhase.Chop:return "Follow the glowing guide. Accurate cuts matter more than speed.";
            case CookingPhase.HeatPot:return "Tap Start cooking to warm the prepared cooking container. Then drag in garlic.";
            case CookingPhase.Garlic:return "Step 1: Add prepared garlic to the warm pot.";
            case CookingPhase.Turmeric:return "Step 2: Add prepared luyang dilaw (turmeric).";
            case CookingPhase.Meat:return "Step 3: Add chicken for the prototype's simplified searing step.";
            case CookingPhase.Sear:return recipePlan.IsHabhab?"Stir-fry the vegetables at heat index 60-85 for 4 simulated seconds, then add broth.":"Sear at heat index 60-85 for 4 simulated seconds before vinegar.";
            case CookingPhase.Vinegar:return recipePlan.IsHabhab?"Add seasoned vegetable broth, then miki noodles.":"Step 4: Add native vinegar.";
            case CookingPhase.Simmer:return "Hold 60-85 heat for 12 simulated seconds. Do not stir yet.";
            case CookingPhase.Plate:return "Turn heat off and plate the completed dish.";
            default:return "Review your attempt.";
        }
    }
    private static string Clock(double value){int seconds=Mathf.Max(0,(int)Math.Ceiling(value));return (seconds/60).ToString("00")+":"+(seconds%60).ToString("00");}
    private static string Stopwatch(double value){int ticks=(int)Math.Floor(value*10);return (ticks/600).ToString("00")+":"+((ticks/10)%60).ToString("00")+"."+(ticks%10);}
    private void OnDestroy(){if(readableText)Destroy(readableText);}
}
