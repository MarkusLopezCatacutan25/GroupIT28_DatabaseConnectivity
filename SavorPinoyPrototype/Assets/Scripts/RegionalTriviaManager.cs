using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RegionalTriviaManager : MonoBehaviour
{
    [SerializeField] private TMP_Text regionTitleText, questionText, timerText, feedbackBanner;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private Button proceedToKitchenBtn;
    [SerializeField] private Sprite mapGraphic;
    private readonly string[][] questions = {
        new[]{"barako","Which coffee is strongly associated with Batangas?","Kapeng Barako","Matcha","Cacao tea","Salabat","0","Kapeng Barako is associated with Batangas coffee heritage."},
        new[]{"palayok_heat","Why does a warmed clay pot need early flame adjustments in this game?","It instantly cools","It retains heat after the flame changes","It never warms","It removes vinegar","1","The palayok retains heat; reduce the flame before the pot overheats."},
        new[]{"turmeric","What gives Batangas Adobo sa Dilaw its golden color?","Fresh turmeric (luyang dilaw)","Soy sauce","Yellow food coloring","Bell pepper","0","Luyang dilaw is the defining ingredient of this selected Batangas adobo variant."},
        new[]{"palayok","What is the Filipino name for the traditional clay cooking pot?","Kawali","Palayok","Wok","Sieve","1","A palayok is an earthenware pot. In this simulation, retained heat means you must adjust the flame early."},
        new[]{"vinegar","Which sour ingredient is used in this Adobo sa Dilaw recipe?","Coconut milk","Sugar syrup","Native vinegar","Soy sauce","2","This recipe uses native vinegar, garlic, and turmeric. Follow the recipe's initial unstirred simmer step."}
    };
    private string[] question;
    private float remaining=15;
    private bool resolved, loading, paused, started;
    private UnityAction[] handlers;
    private RectTransform safeRoot, modal, designRoot;
    private Rect lastSafe;
    private Vector2Int lastSize;
    private Material readableText;
    private void Start()
    {
        if (!regionTitleText || !questionText || !timerText || !feedbackBanner || !proceedToKitchenBtn || optionButtons==null || optionButtons.Length!=4)
        { Debug.LogError("Trivia Hub references must be assigned to its existing custom UI."); enabled=false; return; }
        for (int i=0;i<4;i++) if (!optionButtons[i] || !optionButtons[i].GetComponentInChildren<TMP_Text>(true)) { Debug.LogError("Trivia option is missing its TMP label."); enabled=false; return; }
        GameSessionState.ResetBuffs(); question=questions[0];
        regionTitleText.text="Region IV-A (CALABARZON) - Batangas";
        questionText.text=question[1]; feedbackBanner.text="Adobo sa Dilaw\nCorrect answer: +15s preparation time and 1.25x final score.";
        handlers=new UnityAction[4];
        for (int i=0;i<4;i++) { int choice=i; handlers[i]=()=>Answer(choice); optionButtons[i].onClick.AddListener(handlers[i]); optionButtons[i].GetComponentInChildren<TMP_Text>(true).text=question[i+2]; optionButtons[i].interactable=true; }
        proceedToKitchenBtn.gameObject.SetActive(false); proceedToKitchenBtn.onClick.AddListener(Proceed);
        SetupLayout(); var zoom=GetComponent<RegionalMapZoomManager>(); if(!zoom)zoom=gameObject.AddComponent<RegionalMapZoomManager>(); zoom.Initialize(this,designRoot,modal,mapGraphic); ApplySafeArea();
        modal.GetComponent<Image>().color=Color.white;
        readableText=new Material(questionText.fontSharedMaterial);readableText.SetFloat("_OutlineWidth",0);readableText.SetFloat("_FaceDilate",0);
        readableText.DisableKeyword("UNDERLAY_ON");readableText.DisableKeyword("UNDERLAY_INNER");
        foreach(TMP_Text text in designRoot.GetComponentsInChildren<TMP_Text>(true)){text.fontSharedMaterial=readableText;text.fontStyle=FontStyles.Bold;}
        regionTitleText.color=new Color(1,.95f,.79f);
    }
    public TMP_FontAsset Font => questionText.font;
    public RectTransform Modal => modal;
    public void CancelTrivia()
    {
        started=false;resolved=false;loading=false;remaining=15;GameSessionState.ResetBuffs();
        if(modal)modal.gameObject.SetActive(false);
        if(timerText)timerText.text="";
        if(proceedToKitchenBtn)proceedToKitchenBtn.gameObject.SetActive(false);
    }
    public bool LoadTriviaForRegion(string region)
    {
        if(region!="CALABARZON" || started || !modal || !modal.gameObject.activeInHierarchy)return false;
        GameSessionState.BeginRun();question=questions[Random.Range(0,questions.Length)];
        questionText.text=question[1];remaining=15;resolved=false;loading=false;started=true;
        feedbackBanner.text="Correct answer: +15s preparation time and 1.25x final score.";
        for(int i=0;i<4;i++){optionButtons[i].interactable=true;optionButtons[i].GetComponentInChildren<TMP_Text>(true).text=question[i+2];}
        proceedToKitchenBtn.interactable=true;proceedToKitchenBtn.gameObject.SetActive(false);timerText.text="Time: 15s";return true;
    }
    public void PlaceSplitModal(RectTransform panel)
    {
        Place(panel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(860,880));
        Place(questionText.rectTransform,new Vector2(.5f,1),new Vector2(0,-275),new Vector2(620,105));questionText.fontSize=26;
        timerText.transform.SetParent(designRoot,false);timerText.gameObject.name="TimerBadge";
        Place(timerText.rectTransform,new Vector2(.5f,.5f),new Vector2(680,465),new Vector2(240,65));
        RectTransform options=optionButtons[0].transform.parent as RectTransform;
        Place(options,new Vector2(.5f,.5f),new Vector2(0,-10),new Vector2(620,260));
        foreach(Button b in optionButtons){TMP_Text label=b.GetComponentInChildren<TMP_Text>(true);label.fontSize=24;LayoutElement le=b.GetComponent<LayoutElement>();if(le){le.minHeight=55;le.preferredHeight=55;}}
        Place(feedbackBanner.rectTransform,new Vector2(.5f,.5f),new Vector2(0,-185),new Vector2(620,85));feedbackBanner.fontSize=18;feedbackBanner.enableAutoSizing=true;feedbackBanner.fontSizeMin=14;feedbackBanner.fontSizeMax=18;
        Place(proceedToKitchenBtn.transform as RectTransform,new Vector2(.5f,0),new Vector2(0,35),new Vector2(650,55));
        proceedToKitchenBtn.GetComponentInChildren<TMP_Text>(true).text="Proceed to Kitchen Prep";
    }
    private void SetupLayout()
    {
        Canvas canvas=GetComponent<Canvas>();
        if (!canvas) canvas=GetComponentInParent<Canvas>();
        if (!canvas) return;
        CanvasScaler scaler=canvas.GetComponent<CanvasScaler>();
        if(scaler){scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;}
        safeRoot=new GameObject("SafeAreaRoot",typeof(RectTransform)).GetComponent<RectTransform>();safeRoot.SetParent(canvas.transform,false);
        // Reuse every authored graphic and button; only constrain readable content bounds.
        designRoot=PrototypeUI.Rect("Scene2Design",safeRoot,0,0,1840,1000);
        regionTitleText.transform.SetParent(designRoot,false);
        modal=questionText.transform.parent as RectTransform;modal.SetParent(designRoot,false);
        Place(regionTitleText.rectTransform,new Vector2(.5f,.5f),new Vector2(0,465),new Vector2(900,70));
        regionTitleText.fontSize=28;regionTitleText.alignment=TextAlignmentOptions.Center;
        Place(modal,new Vector2(.5f,.5f),new Vector2(0,-40),new Vector2(1586,900));
        Place(questionText.rectTransform,new Vector2(.5f,1),new Vector2(0,-235),new Vector2(1060,110));questionText.fontSize=38;
        Place(timerText.rectTransform,new Vector2(1,1),new Vector2(-195,-145),new Vector2(240,60));timerText.fontSize=30;
        RectTransform options=optionButtons[0].transform.parent as RectTransform;Place(options,new Vector2(.5f,.5f),new Vector2(0,-35),new Vector2(900,330));
        foreach(Button b in optionButtons){TMP_Text label=b.GetComponentInChildren<TMP_Text>(true);label.fontSize=32;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.18f,.10f,.05f);LayoutElement le=b.GetComponent<LayoutElement>();if(le){le.minHeight=70;le.preferredHeight=70;}}
        feedbackBanner.transform.SetParent(modal,false);Place(feedbackBanner.rectTransform,new Vector2(.5f,0),new Vector2(0,165),new Vector2(1050,130));feedbackBanner.fontSize=26;feedbackBanner.alignment=TextAlignmentOptions.Center;
        proceedToKitchenBtn.transform.SetParent(modal,false);Place(proceedToKitchenBtn.transform as RectTransform,new Vector2(.5f,0),new Vector2(0,55),new Vector2(600,65));
        TMP_Text proceedLabel=proceedToKitchenBtn.GetComponentInChildren<TMP_Text>(true);if(proceedLabel){proceedLabel.text="Proceed to Kitchen";proceedLabel.fontSize=30;}
    }
    private static void Place(RectTransform r,Vector2 anchor,Vector2 pos,Vector2 size){r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;}
    private void ApplySafeArea()
    {
        if(!safeRoot || Screen.width==0 || Screen.height==0)return;
        Rect area=Screen.safeArea;lastSafe=area;lastSize=new Vector2Int(Screen.width,Screen.height);
        safeRoot.anchorMin=new Vector2(area.x/Screen.width,area.y/Screen.height);safeRoot.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
        Canvas.ForceUpdateCanvases();float factor=Mathf.Min((safeRoot.rect.width-30)/1840,(safeRoot.rect.height-30)/1000);designRoot.localScale=Vector3.one*Mathf.Max(.2f,factor);
    }
    private void Update()
    {
        if(safeRoot && (lastSafe!=Screen.safeArea || lastSize!=new Vector2Int(Screen.width,Screen.height))) ApplySafeArea();
        if(!started || paused || resolved)return;remaining=Mathf.Max(0,remaining-Time.deltaTime);timerText.text="Time: "+Mathf.CeilToInt(remaining)+"s";if(remaining<=0)Resolve("TimedOut");
    }
    private void Answer(int choice){if(!started||resolved||paused)return;if(remaining<=0){Resolve("TimedOut");return;}Resolve(choice==int.Parse(question[6])?"Correct":"Wrong");}
    private void Resolve(string outcome)
    {
        if(resolved || !GameSessionState.ResolveTrivia(question[0],outcome))return;resolved=true;
        foreach(Button b in optionButtons)b.interactable=false;
        feedbackBanner.text=(outcome=="Correct"?"Correct! +15s and 1.25x reward.":outcome=="TimedOut"?"Time's up. No bonus.":"Incorrect. No bonus.")+"\nAnswer: "+question[int.Parse(question[6])+2]+". "+question[7];
        proceedToKitchenBtn.gameObject.SetActive(true);
    }
    private void Proceed(){if(!resolved||loading)return;if(!Application.CanStreamedLevelBeLoaded("CookingStage")){feedbackBanner.text="CookingStage is missing from the active Build Profile.";return;}loading=true;proceedToKitchenBtn.interactable=false;SceneManager.LoadScene("CookingStage");}
    private void OnApplicationPause(bool value){paused=value;}
    private void OnDestroy(){if(readableText)Destroy(readableText);if(handlers!=null)for(int i=0;i<4;i++)if(optionButtons[i])optionButtons[i].onClick.RemoveListener(handlers[i]);if(proceedToKitchenBtn)proceedToKitchenBtn.onClick.RemoveListener(Proceed);}
}
