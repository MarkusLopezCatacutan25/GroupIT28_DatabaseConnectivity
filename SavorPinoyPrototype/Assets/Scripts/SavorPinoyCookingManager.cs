using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SavorPinoyCookingManager : MonoBehaviour
{
    [SerializeField] private string databaseUrl="";
    [SerializeField] private Canvas legacyCanvas;
    [SerializeField] private TMP_FontAsset fontAsset;
    private CookingAttempt attempt;
    private string runId;
    private RectTransform safe, design, resultPanel, pausePanel, board, heritagePanel;
    private TMP_Text phaseText,timerText,objectiveText,feedbackText,heatText,potText,networkText,resultText,leaderboardText,ingredientText;
    private TMP_InputField aliasInput, startingAlias;
    private Button knifeButton,spoonButton,plateButton,stirButton,saveButton;
    private Button[] pantryButtons,potButtons;
    private Slider flame;
    private Image guide, flameVisual;
    private Image potBody, platedDish, boardIngredient;
    private KitchenArt art;
    private TMP_Text traceHint;
    private Material readableText;
    private RectTransform preparedTray, kitchenScene, closeupScene, modePanel, feedbackGroup, heatGroup, stoveControls;
    private Image knifeBlade;
    private KitchenCueGraphic slicedPieces;
    private KitchenCueGraphic steam;
    private Button loadScores,retrySync;
    private TMP_Text resultBadge,boardTitle,boardNote;
    private TMP_Text closeupTitle, clockLabel, modeNote;
    private Button startCooking;
    private readonly KitchenCueGraphic[] ingredientHalos=new KitchenCueGraphic[4];
    private readonly TMP_Text[] ingredientLabels=new TMP_Text[4];
    private readonly string[] pantryIds={"garlic","turmeric","chicken","soy"};
    private bool choosingMode=true, triviaCorrect;
    private int lastAccepted=-1;
    private float feedbackUntil;
    private string lastMessage, practiceSaveStatus;
    private ChoppingSurface chopping;
    private PrototypeScoreService scores;
    private bool paused, saved, resultShown, leaving;
    private Rect lastSafe;
    private Vector2Int lastSize;

    private CookingPhase displayedPhase=(CookingPhase)(-1);
    private void Start()
    {
        if(!GameSessionState.TryConsumeKitchen(out runId,out bool correct))
        {
            Debug.LogWarning("Cooking requires a fresh resolved trivia attempt. Returning to hub.");
            SceneManager.LoadScene("RegionalTriviaHub");return;
        }
        if(legacyCanvas)legacyCanvas.gameObject.SetActive(false);
        PrototypeUI.Font=fontAsset;
        triviaCorrect=correct;attempt=new CookingAttempt(correct,false);BuildView();Fit();
        scores=PrototypeScoreService.Ensure(databaseUrl);scores.Changed+=NetworkChanged;NetworkChanged();Refresh();
    }
    private static readonly Color Parchment=new Color(1,.94f,.78f);
    private static readonly Color Wood=new Color(.24f,.115f,.055f);
    private TMP_Text Label(string name,Transform parent,string value,float x,float y,float w,float h,int size=28)
    {
        PrototypeUI.Panel(name+"Shadow",parent,x,y-3,w+8,h+6,new Color(.12f,.065f,.03f,.8f)).raycastTarget=false;
        PrototypeUI.Panel(name+"Wood",parent,x,y,w,h,Wood).raycastTarget=false;
        TMP_Text text=PrototypeUI.Text(name,parent,value,x,y,w-16,h-4,size);text.color=Parchment;text.fontStyle=FontStyles.Bold;return text;
    }
    private Button ActionButton(string name,Transform parent,string value,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
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
        art=GetComponent<KitchenArt>();
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
        art.Draw("RecipeGoal",kitchenScene,art.Slice("card"),735,145,255,325);
        Label("RecipeRegion",kitchenScene,"Batangas / CALABARZON",735,-45,280,45,20);
        pantryButtons=new Button[4];float[] px={-590,-370,-155,-170},py={75,75,-160,90};
        string[] labels={"Garlic","Luyang dilaw","Chicken","Soy sauce"};
        for(int i=0;i<4;i++)
        {
            int index=i;string id=pantryIds[i];float w=i==2?205:155,h=i==3?130:120;
            pantryButtons[i]=PrototypeUI.Button("Gather_"+id,kitchenScene,"",px[i],py[i],w+40,h+55,()=>{attempt.Gather(id);Refresh();});
            pantryButtons[i].image.color=Color.clear;pantryButtons[i].transition=Selectable.Transition.None;
            ingredientHalos[i]=Halo("Halo_"+id,pantryButtons[i].transform,0,0,w+25,h+28);
            art.Draw("Ingredient_"+id,pantryButtons[i].transform,id=="turmeric"?art.Turmeric:art.Slice(id),0,5,w,h);
            ingredientLabels[i]=Label("Label_"+id,pantryButtons[i].transform,labels[i],0,-h*.5f-25,w+45,42,21);
        }
        var overviewBoard=art.Draw("OverviewBoard",kitchenScene,art.Slice("board"),-515,-140,420,210);
        preparedTray=PrototypeUI.Rect("PreparedTray",overviewBoard.transform,0,0,350,100);
        art.Draw("PreparedGarlic",preparedTray,art.Slice("garlic"),-100,0,100,95);art.Draw("PreparedTurmeric",preparedTray,art.Turmeric,20,0,100,95);
        knifeButton=PrototypeUI.Button("Knife",kitchenScene,"",-515,-130,400,175,()=>{attempt.ChooseKnife();Refresh();});knifeButton.image.color=Color.clear;knifeButton.transition=Selectable.Transition.None;
        art.Draw("KnifeOnBoard",knifeButton.transform,art.Knife,0,0,300,150);
        Label("KnifeLabel",knifeButton.transform,"KNIFE / Tap to prepare",0,-115,350,48,23);
        var knifeDrag=knifeButton.gameObject.AddComponent<KnifeTool>();knifeDrag.Board=overviewBoard.rectTransform;knifeDrag.CanSelect=()=>!paused&&!choosingMode&&attempt.Phase==CookingPhase.Gather;knifeDrag.Selected=()=>{attempt.ChooseKnife();Refresh();};
        spoonButton=ActionButton("Spoon",kitchenScene,"Spoon",-750,-330,130,60,()=>{attempt.ChooseWrongTool();Refresh();});
        potBody=art.Draw("PotBody",kitchenScene,art.Slice("pot"),330,10,370,285);
        steam=PrototypeUI.Rect("Steam",kitchenScene,330,125,120,100).gameObject.AddComponent<KitchenCueGraphic>();steam.Steam=true;steam.raycastTarget=false;
        flameVisual=PrototypeUI.Panel("Flame",kitchenScene,330,-90,135,12,new Color(1,.48f,.12f));flameVisual.raycastTarget=false;flameVisual.transform.SetSiblingIndex(potBody.transform.GetSiblingIndex());
        heatGroup=PrototypeUI.Rect("HeatGroup",kitchenScene,330,210,380,75);heatText=Label("Heat",heatGroup,"",0,0,380,75,25);
        potText=Label("PotState",kitchenScene,"",330,-165,405,70,25);
        stoveControls=PrototypeUI.Rect("StoveControls",kitchenScene,0,0,1800,1000);
        flame=PrototypeUI.HeatSlider(stoveControls,350,-225,360);
        flame.handleRect.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(324,38);flame.handleRect.sizeDelta=new Vector2(40,0);
        flame.handleRect.GetComponent<Image>().enabled=false;var dial=PrototypeUI.Rect("HeatDial",flame.handleRect,0,0,40,38).gameObject.AddComponent<KitchenCueGraphic>();dial.Dial=true;flame.targetGraphic=dial;flame.onValueChanged.AddListener(value=>{attempt.SetFlame(value);Refresh();});
        Label("FlameLegend",stoveControls,"OFF    LOW    MEDIUM    HIGH",350,-270,440,30,18);
        potButtons=new Button[4];string[] cookingIds={"garlic","turmeric","chicken","vinegar"},cookingLabels={"Add garlic","Add turmeric","Add chicken","Add vinegar"};
        for(int i=0;i<4;i++){string id=cookingIds[i];potButtons[i]=ActionButton("Add_"+id,kitchenScene,cookingLabels[i],-650+(i%2)*285,-90-(i/2)*110,255,80,()=>{attempt.AddToPot(id);Refresh();});}
        stirButton=ActionButton("Stir",kitchenScene,"Stir",205,-315,185,65,()=>{attempt.Stir();Refresh();});
        plateButton=ActionButton("Plate",kitchenScene,"Plate dish",480,-315,250,65,()=>{attempt.Plate();Refresh();});
        startCooking=ActionButton("StartCooking",kitchenScene,"Start cooking",-415,-240,420,75,()=>{attempt.SetFlame(.6);flame.SetValueWithoutNotify(.6f);Refresh();});
        closeupScene=PrototypeUI.Rect("ChoppingCloseup",design,0,0,1800,1000);
        art.Draw("ChoppingBackground",closeupScene,art.Closeup,0,0,1800,1000,false);
        closeupTitle=Label("CloseupTitle",closeupScene,"",0,290,940,75,36);
        board=PrototypeUI.Panel("CuttingBoard",closeupScene,0,-35,950,420,Color.clear).rectTransform;
        chopping=board.gameObject.AddComponent<ChoppingSurface>();chopping.CanCut=()=>!paused&&!choosingMode&&attempt.Phase==CookingPhase.Chop;chopping.AcceptedCuts=()=>attempt.Cuts;chopping.Completed=accuracy=>{attempt.Cut(accuracy);Refresh();};
        boardIngredient=art.Draw("BoardIngredient",board,art.Slice("garlic"),0,0,380,260);
        var piecesRect=PrototypeUI.Rect("ChoppedPieces",board,-255,-120,200,85);slicedPieces=piecesRect.gameObject.AddComponent<KitchenCueGraphic>();slicedPieces.Ring=false;slicedPieces.raycastTarget=false;
        guide=PrototypeUI.Panel("CutGuide",board,0,0,5,252,new Color(.38f,.95f,.56f));guide.raycastTarget=false;
        Halo("StartMarker",guide.transform,0,126,42,42);Halo("FinishMarker",guide.transform,0,-126,42,42);
        Label("GuideStart",guide.transform,"START",-100,126,130,42,23);Label("GuideEnd",guide.transform,"FINISH",-100,-126,130,42,23);
        knifeBlade=art.Draw("ActiveKnife",board,art.Knife,0,0,430,250);
        chopping.StrokeMoved=p=>{knifeBlade.rectTransform.anchoredPosition=p+new Vector2(115,35);};
        chopping.StrokeActive=active=>{if(!active&&attempt!=null)ResetKnife();};
        ingredientText=Label("CutProgress",closeupScene,"",0,-325,740,60,30);
        traceHint=Label("TraceHint",closeupScene,"",0,-385,1220,50,26);
        Label("KitchenBrand",design,"SAVORPINOY",-650,438,450,78,39);
        phaseText=Label("Phase",design,"",-85,438,530,78,30);
        clockLabel=Label("ClockLabel",design,"",520,469,300,42,19);
        timerText=Label("Timer",design,"",520,421,300,62,39);
        ActionButton("PauseButton",design,"Pause",785,438,155,75,()=>Pause(true));
        objectiveText=Label("Objective",design,"",0,-468,1740,74,29);
        feedbackGroup=PrototypeUI.Rect("FeedbackToast",design,0,280,1090,70);feedbackText=Label("Feedback",feedbackGroup,"",0,0,1090,70,26);
        BuildResult();BuildPause();BuildMode();
        readableText=new Material(fontAsset.material);readableText.SetFloat("_OutlineWidth",0);readableText.SetFloat("_FaceDilate",0);readableText.DisableKeyword("UNDERLAY_ON");
        foreach(TMP_Text text in design.GetComponentsInChildren<TMP_Text>(true))text.fontSharedMaterial=readableText;
    }
    private void BuildMode()
    {
        modePanel=PrototypeUI.Panel("ChooseLearningMode",design,0,0,1800,1000,new Color(.12f,.06f,.025f,.90f)).rectTransform;
        Label("Welcome",modePanel,"LET'S MAKE ADOBO SA DILAW",0,330,1200,100,43);
        TMP_Text intro=PrototypeUI.Text("ModeIntro",modePanel,"Choose how you want to learn. Follow one step at a time.",0,235,1380,70,30);intro.color=Parchment;
        startingAlias=PrototypeUI.Input("StartingChefAlias",modePanel,0,135,650,70);startingAlias.text=PlayerPrefs.GetString("SavorPinoy.ChefAlias","");
        ActionButton("GuidedPractice",modePanel,"Guided practice / start here",-345,-5,600,105,()=>ChooseMode(false));
        ActionButton("TimedChallenge",modePanel,"Timed challenge",345,-5,600,105,()=>ChooseMode(true));
        var practice=PrototypeUI.Text("PracticeInfo",modePanel,"BEGINNER FRIENDLY\nElapsed-time clock. No expiry.\nPractice cuts, order, and heat at your pace.",-345,-160,590,200,29);practice.color=Parchment;
        var challenge=PrototypeUI.Text("ChallengeInfo",modePanel,"READY FOR A CHALLENGE?\n2-minute prep + trivia time bonus.\n75-second cooking countdown. Ranked score.",345,-160,590,200,29);challenge.color=Parchment;
        modeNote=PrototypeUI.Text("ModeNote",modePanel,"Your trivia reward is kept. Practice results stay separate from ranked scores.",0,-345,1450,80,25);modeNote.color=Parchment;
    }
    private void ChooseMode(bool timed)
    {
        if(!choosingMode)return;
        string alias=(startingAlias.text??"").Trim();
        if(!PrototypeScoreService.IsValidAlias(alias)){modeNote.text="Enter 1-24 letters/numbers; spaces, period, underscore and hyphen are allowed.";return;}
        aliasInput.text=alias;attempt=new CookingAttempt(triviaCorrect,timed,timed?120:60);choosingMode=false;modePanel.gameObject.SetActive(false);Refresh();
    }
    private void ResetKnife()
    {
        if(!knifeBlade||!board)return;float x=(.3f+.2f*(attempt.Cuts%3)-.5f)*board.rect.width;
        knifeBlade.rectTransform.anchoredPosition=new Vector2(x+115,board.rect.height*.3f+35);
    }
    private void Panel(string name,float x,float y,float w,float h){PrototypeUI.Panel(name,design,x,y,w,h,new Color(.97f,.91f,.77f)).raycastTarget=false;}
    private void BuildResult()
    {
        resultPanel=PrototypeUI.Panel("ResultOverlay",design,0,0,1740,940,new Color(.13f,.065f,.03f,.96f)).rectTransform;
        art.Draw("ResultKitchenBackdrop",resultPanel,art.Environment,0,0,1740,940,false).color=new Color(.45f,.32f,.23f,1);
        PrototypeUI.Panel("SkillsParchment",resultPanel,-325,145,1080,630,new Color(.98f,.92f,.78f)).raycastTarget=false;
        resultText=PrototypeUI.Text("ResultBreakdown",resultPanel,"",-325,165,1020,570,27);
        aliasInput=PrototypeUI.Input("ChefAlias",resultPanel,-415,-185,580,70);aliasInput.text=PlayerPrefs.GetString("SavorPinoy.ChefAlias","");
        saveButton=ActionButton("SaveScore",resultPanel,"Save score",-400,-270,300,75,SaveScore);
        retrySync=ActionButton("RetrySync",resultPanel,"Retry sync",-60,-270,270,75,()=>scores.RetryPending());
        ActionButton("RetryStage",resultPanel,"Retry via Trivia",-600,-375,320,80,()=>Leave("RegionalTriviaHub"));
        ActionButton("ReturnMenu",resultPanel,"Main Menu",-240,-375,300,80,()=>Leave("MainMenu"));
        ActionButton("HeritageCard",resultPanel,"Heritage Card",520,-375,360,80,()=>heritagePanel.gameObject.SetActive(true));
        platedDish=art.Draw("PlatedAdobo",resultPanel,art.Slice("dish"),540,235,480,270);
        resultBadge=Label("ResultBadge",resultPanel,"",540,420,540,80,38);
        boardTitle=Label("LeaderboardTitle",resultPanel,"",540,65,540,60,27);
        RectTransform viewport=PrototypeUI.Panel("LeaderboardViewport",resultPanel,535,-75,565,195,new Color(0,0,0,.25f)).rectTransform;
        viewport.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        leaderboardText=PrototypeUI.Text("LeaderboardRows",viewport,"",0,0,545,195,22);leaderboardText.color=Parchment;leaderboardText.alignment=TextAlignmentOptions.TopLeft;
        RectTransform content=leaderboardText.rectTransform;content.anchorMin=content.anchorMax=new Vector2(.5f,1);content.pivot=new Vector2(.5f,1);scroll.content=content;
        loadScores=ActionButton("LoadLeaderboard",resultPanel,"Refresh scores",520,-230,360,70,()=>scores.LoadLeaderboard());
        boardNote=PrototypeUI.Text("LeaderboardNote",resultPanel,"",520,-305,540,80,23);boardNote.color=Parchment;
        networkText=PrototypeUI.Text("NetworkStatus",resultPanel,"",0,-445,1630,65,23);networkText.color=Parchment;
        resultPanel.gameObject.SetActive(false);
        heritagePanel=PrototypeUI.Panel("HeritageOverlay",design,0,0,1740,940,new Color(.97f,.91f,.76f)).rectTransform;
        PrototypeUI.Text("HeritageHeading",heritagePanel,"Batangas Heritage Card",0,340,1500,100,48);
        PrototypeUI.Text("HeritageBody",heritagePanel,"Adobo sa Dilaw\n\nLuyang dilaw (turmeric) gives this selected Batangas adobo variant its golden color.\n\nThis prototype pairs turmeric with garlic, chicken, and native vinegar. It uses a palayok as its traditional cookware reference.\n\nThe simulation teaches preparation, ingredient order, an initial unstirred simmer, and adjusting heat early because the clay pot retains heat.\n\nRecipe variants exist. This game presents one simplified learning procedure.",0,35,1420,530,34);
        ActionButton("CloseHeritage",heritagePanel,"Back to Results",0,-335,470,85,()=>heritagePanel.gameObject.SetActive(false));heritagePanel.gameObject.SetActive(false);
    }
    private void BuildPause()
    {
        pausePanel=PrototypeUI.Panel("PauseOverlay",design,0,0,1760,960,new Color(.12f,.06f,.025f,.94f)).rectTransform;
        Label("PausedTitle",pausePanel,"Paused",0,160,1000,100,56);
        Label("PauseInfo",pausePanel,"Your current attempt is paused. Returning to the menu abandons this unfinished attempt.",0,30,1200,120,34);
        ActionButton("Resume",pausePanel,"Resume",0,-130,360,85,()=>Pause(false));
        ActionButton("LeaveStage",pausePanel,"Leave to Main Menu",0,-245,520,85,()=>Leave("MainMenu"));pausePanel.gameObject.SetActive(false);
    }
    private void Fit()
    {
        if(!safe || Screen.width<=0 || Screen.height<=0)return;Rect area=Screen.safeArea;lastSafe=area;lastSize=new Vector2Int(Screen.width,Screen.height);
        safe.anchorMin=new Vector2(area.x/Screen.width,area.y/Screen.height);safe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
        Canvas.ForceUpdateCanvases();design.localScale=Vector3.one*Mathf.Max(.1f,Mathf.Min((safe.rect.width-24)/1800,(safe.rect.height-24)/1000));
    }
    private void Update()
    {
        if(attempt==null)return;
        if(lastSafe!=Screen.safeArea||lastSize!=new Vector2Int(Screen.width,Screen.height))Fit();
        if(!paused && !choosingMode && !attempt.Terminal){attempt.Advance(Time.unscaledDeltaTime);Refresh();}
    }
    private static string Clock(double value){int seconds=Mathf.Max(0,(int)Math.Ceiling(value));return (seconds/60).ToString("00")+":"+(seconds%60).ToString("00");}
    private static string Stopwatch(double value){int ticks=(int)Math.Floor(value*10);return (ticks/600).ToString("00")+":"+((ticks/10)%60).ToString("00")+"."+(ticks%10);}
    private void Refresh()
    {
        if(attempt==null)return;
        bool preparing=attempt.Phase<=CookingPhase.Chop, cutting=attempt.Phase==CookingPhase.Chop;
        clockLabel.text=attempt.TimedChallenge?(preparing?"PREP TIME LEFT":"COOK TIME LEFT"):"ACTIVE TIME / PRACTICE";
        timerText.text=attempt.TimedChallenge?Clock(preparing?attempt.Remaining:attempt.CookingRemaining):Stopwatch(attempt.Elapsed);
        phaseText.text=attempt.Terminal?"REVIEW YOUR DISH":preparing?(cutting?"2 / PREPARE":"1 / GATHER"):"3 / COOK";
        objectiveText.text=Objective(attempt.Phase);
        if(lastMessage!=attempt.Message){lastMessage=attempt.Message;feedbackUntil=Time.unscaledTime+3;feedbackText.text=attempt.Message;}
        bool showFeedback=Time.unscaledTime<feedbackUntil && (attempt.Mistakes>0 || cutting&&attempt.Cuts>0);
        feedbackGroup.gameObject.SetActive(showFeedback&&!cutting);
        kitchenScene.gameObject.SetActive(!cutting);closeupScene.gameObject.SetActive(cutting);
        for(int i=0;i<4;i++)
        {
            bool collected=attempt.HasIngredient(pantryIds[i]);pantryButtons[i].gameObject.SetActive(preparing);
            pantryButtons[i].interactable=!paused&&!choosingMode&&attempt.Phase==CookingPhase.Gather&&!collected;
            ingredientHalos[i].color=collected?new Color(.45f,1,.55f):new Color(1,.83f,.3f);ingredientLabels[i].text=(collected?"READY / ":"")+new[]{"Garlic","Luyang dilaw","Chicken","Soy sauce"}[i];
            ingredientLabels[i].fontSize=collected?18:21;
        }
        knifeButton.gameObject.SetActive(preparing);knifeButton.interactable=!paused&&!choosingMode&&attempt.Phase==CookingPhase.Gather;
        spoonButton.gameObject.SetActive(preparing);spoonButton.interactable=!paused&&!choosingMode;
        bool canCook=!paused&&!choosingMode&&!attempt.Terminal&&!preparing;
        foreach(Button b in potButtons){b.gameObject.SetActive(!preparing&&attempt.Phase!=CookingPhase.HeatPot);b.interactable=canCook&&attempt.Phase<CookingPhase.Plate;}
        startCooking.gameObject.SetActive(attempt.Phase==CookingPhase.HeatPot);startCooking.interactable=canCook;
        flame.gameObject.SetActive(!preparing);flame.interactable=canCook;
        stirButton.gameObject.SetActive(!preparing);stirButton.interactable=canCook&&attempt.Phase==CookingPhase.Simmer;
        plateButton.gameObject.SetActive(!preparing);plateButton.interactable=canCook&&attempt.Phase==CookingPhase.Plate;
        heatGroup.gameObject.SetActive(!preparing);stoveControls.gameObject.SetActive(!preparing);potText.text=preparing?"PALAYOK / CLAY POT":attempt.Phase==CookingPhase.Simmer?"SIMMER / "+Mathf.FloorToInt((float)attempt.SimmerProgress)+" of 12s":attempt.Phase==CookingPhase.Sear?"SEAR / "+Mathf.FloorToInt((float)attempt.SearProgress)+" of 4s":"FOLLOW THE RECIPE";
        heatText.text="HEAT "+Mathf.RoundToInt((float)attempt.Heat)+" / 110\nKeep in the 60-85 band";
        flameVisual.gameObject.SetActive(!preparing&&attempt.Flame>.01);flameVisual.color=Color.Lerp(new Color(.8f,.27f,.04f),new Color(1,.68f,.1f),(float)attempt.Flame);
        steam.gameObject.SetActive(!preparing&&!attempt.Terminal&&attempt.Heat>=60&&attempt.Phase>=CookingPhase.Sear);
        potBody.sprite=attempt.Phase>=CookingPhase.Vinegar&&art.OpenPot?art.OpenPot:art.Slice("pot");
        potBody.color=Color.Lerp(Color.white,new Color(1,.65f,.45f),Mathf.InverseLerp(85,105,(float)attempt.Heat));
        preparedTray.gameObject.SetActive(!preparing);
        if(lastAccepted!=attempt.Cuts||displayedPhase!=attempt.Phase)
        {
            lastAccepted=attempt.Cuts;displayedPhase=attempt.Phase;
            bool turmeric=attempt.Cuts>=3;boardIngredient.sprite=turmeric?art.Turmeric:art.Slice("garlic");
            closeupTitle.text=turmeric?"PREPARE LUYANG DILAW / TURMERIC":"CHOP THE GARLIC";
            boardIngredient.rectTransform.sizeDelta=new Vector2(380-(attempt.Cuts%3)*45,260-(attempt.Cuts%3)*25);
            slicedPieces.Turmeric=turmeric;slicedPieces.Pieces=(attempt.Cuts%3)*4;slicedPieces.SetVerticesDirty();
            ingredientText.text="Cut "+(attempt.Cuts%3+1)+" of 3  /  "+Mathf.Min(attempt.Cuts,6)+" of 6 completed";
            guide.rectTransform.anchoredPosition=new Vector2((.3f+.2f*(attempt.Cuts%3)-.5f)*board.rect.width,0);boardIngredient.rectTransform.anchoredPosition=guide.rectTransform.anchoredPosition;ResetKnife();
        }
        traceHint.text=showFeedback?attempt.Message:"Touch START. Drag the knife down to FINISH. Lift to complete the cut.";
        if(attempt.Terminal&&!resultShown)ShowResult();
    }
    private static string Objective(CookingPhase phase)
    {
        switch(phase)
        {
            case CookingPhase.Gather:return "Tap garlic, luyang dilaw, and chicken. Then tap the knife on the board.";
            case CookingPhase.Chop:return "Follow the glowing guide. Accurate cuts matter more than speed.";
            case CookingPhase.HeatPot:return "Tap Start cooking. Warm the palayok, then add garlic.";
            case CookingPhase.Garlic:return "Step 1: Add prepared garlic to the warm pot.";
            case CookingPhase.Turmeric:return "Step 2: Add prepared luyang dilaw (turmeric).";
            case CookingPhase.Meat:return "Step 3: Add chicken for the prototype's simplified searing step.";
            case CookingPhase.Sear:return "Sear at heat index 60-85 for 4 simulated seconds before vinegar.";
            case CookingPhase.Vinegar:return "Step 4: Add native vinegar.";
            case CookingPhase.Simmer:return "Hold 60-85 heat for 12 simulated seconds. Do not stir yet.";
            case CookingPhase.Plate:return "Turn heat off and plate the completed dish.";
            default:return "Review your attempt.";
        }
    }
    private void ShowResult()
    {
        resultShown=true;resultBadge.text=attempt.Stars+" / 3 STARS";boardTitle.text=attempt.TimedChallenge?"TIMED CHALLENGE SCORES":"YOUR PRACTICE TIME";boardNote.text=attempt.TimedChallenge?"Practice results use a separate local history.":"Take your time. Focus on accurate cuts, ingredient order, and heat.";loadScores.gameObject.SetActive(attempt.TimedChallenge);retrySync.gameObject.SetActive(attempt.TimedChallenge);resultPanel.gameObject.SetActive(true);pausePanel.gameObject.SetActive(false);paused=false;flame.SetValueWithoutNotify(0);
        platedDish.gameObject.SetActive(attempt.Phase==CookingPhase.Success);
        string title=attempt.Phase==CookingPhase.Success?"Adobo sa Dilaw Completed":"Retry and Improve";
        resultText.text=title+"\n"+attempt.Stars+" / 3 stars\n\nYour Cooking Skills\nCutting: "+attempt.CuttingScore+" / 2500 ("+Mathf.RoundToInt((float)attempt.CuttingAccuracy*100)+"%)\nSequence: "+attempt.SequenceScore+" / 2500\nHeat control: "+attempt.ThermalScore+" / 2500\nPenalties: -"+attempt.Penalties+"\nRaw cooking score: "+attempt.RawScore+"\nTrivia multiplier: "+attempt.Multiplier+"x\nFinal score: "+attempt.FinalScore+"\nRecord ID: slice_"+runId+"\n\nActive time: "+attempt.Elapsed.ToString("0.00")+"s (prep "+attempt.PrepElapsed.ToString("0.00")+"s, cooking "+attempt.CookElapsed.ToString("0.00")+"s)\n"+(attempt.TimedChallenge?"Timed challenge":"Guided practice / not ranked")+"\n"+attempt.Message;
        saveButton.interactable=attempt.Phase==CookingPhase.Success;saveButton.GetComponentInChildren<TMP_Text>().text=attempt.TimedChallenge?"Save score":"Save practice";aliasInput.interactable=false;NetworkChanged();
    }
    private void SaveScore()
    {
        if(saved || scores==null)return;
        if(!attempt.TimedChallenge){practiceSaveStatus=PracticeAttemptStore.Save(runId,aliasInput.text,attempt);if(practiceSaveStatus.StartsWith("Practice saved")||practiceSaveStatus.StartsWith("This practice")){saved=true;saveButton.interactable=false;aliasInput.interactable=false;}NetworkChanged();return;}
        if(scores.Queue(runId,aliasInput.text,attempt)){saved=true;saveButton.interactable=false;aliasInput.interactable=false;PlayerPrefs.SetString("SavorPinoy.ChefAlias",aliasInput.text.Trim());PlayerPrefs.Save();}
        NetworkChanged();
    }
    private void NetworkChanged(){if(scores==null)return;if(networkText)networkText.text=attempt!=null&&!attempt.TimedChallenge?(practiceSaveStatus??"Guided practice / save your timing locally. Not submitted to the ranked board."):scores.Status;if(leaderboardText)leaderboardText.text=attempt!=null&&!attempt.TimedChallenge?"Preparation: "+attempt.PrepElapsed.ToString("0.00")+" s\nCooking: "+attempt.CookElapsed.ToString("0.00")+" s\nTotal active time: "+attempt.Elapsed.ToString("0.00")+" s":scores.Leaderboard;if(leaderboardText){leaderboardText.rectTransform.sizeDelta=new Vector2(545,Mathf.Max(195,leaderboardText.preferredHeight));}}
    private void Pause(bool value){if(attempt==null||attempt.Terminal)return;paused=value;chopping.CancelStroke();pausePanel.gameObject.SetActive(value);Refresh();}
    private void OnApplicationPause(bool value){if(value)Pause(true);}
    private void OnApplicationFocus(bool value){if(!value)Pause(true);}
    private void Leave(string scene){if(leaving)return;leaving=true;SceneManager.LoadScene(scene);}
    private void OnDestroy(){if(readableText)Destroy(readableText);if(scores)scores.Changed-=NetworkChanged;}
}
