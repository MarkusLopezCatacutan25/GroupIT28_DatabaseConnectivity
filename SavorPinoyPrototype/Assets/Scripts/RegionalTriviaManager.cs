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
    private ProvinceQuestion question;
    private TMP_Text answerHeading,rewardNote;
    private RectTransform optionsRoot;
    private int selectedChoice=-1;
    public string SelectedProvince { get; private set; }="Batangas";
    public void SelectProvince(string province){SelectedProvince=province;regionTitleText.text="CALABARZON / "+province;}
    private float remaining=30;
    private bool resolved, loading, paused, started;
    private UnityAction[] handlers;
    private RectTransform safeRoot, modal, designRoot;
    private Rect lastSafe;
    private Vector2Int lastSize;
    private Material readableText;
    private void Start()
    {
        if(!ChefAccessSession.HasAccess){SceneManager.LoadScene("MainMenu");return;}
        if (!regionTitleText || !questionText || !timerText || !feedbackBanner || !proceedToKitchenBtn || optionButtons==null || optionButtons.Length!=4)
        { Debug.LogError("Trivia Hub references must be assigned to its existing custom UI."); enabled=false; return; }
        for (int i=0;i<4;i++) if (!optionButtons[i] || !optionButtons[i].GetComponentInChildren<TMP_Text>(true)) { Debug.LogError("Trivia option is missing its TMP label."); enabled=false; return; }
        GameSessionState.ResetBuffs(); question=ProvinceTrivia.For("Batangas")[0];
        regionTitleText.text="CALABARZON / Explore a food heritage trail";
        questionText.text=question.Prompt; feedbackBanner.text="Adobo sa Dilaw\nCorrect answer: +15s preparation time and 1.25x final score.";
        handlers=new UnityAction[4];
        for (int i=0;i<4;i++) { int choice=i; handlers[i]=()=>Answer(choice); optionButtons[i].onClick.AddListener(handlers[i]); optionButtons[i].GetComponentInChildren<TMP_Text>(true).text=question.Options[i]; optionButtons[i].interactable=true; }
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
        started=false;resolved=false;loading=false;remaining=30;GameSessionState.ResetBuffs();
        if(modal)modal.gameObject.SetActive(false);
        if(timerText)timerText.text="";
        if(proceedToKitchenBtn)proceedToKitchenBtn.gameObject.SetActive(false);
    }
    public bool LoadTriviaForRegion(string region)
    {
        if(region!="CALABARZON" || started || !modal || !modal.gameObject.activeInHierarchy)return false;
        GameSessionState.BeginRun(SelectedProvince);var bank=ProvinceTrivia.For(SelectedProvince);question=bank[Random.Range(0,bank.Length)];
        questionText.text=question.Prompt;remaining=30;resolved=false;loading=false;started=true;selectedChoice=-1;
        feedbackBanner.text="Answer correctly to unlock "+RecipePlan.For(SelectedProvince).Dish+".";
        for(int i=0;i<4;i++){optionButtons[i].interactable=true;optionButtons[i].GetComponentInChildren<TMP_Text>(true).text=question.Options[i];}
        ShowQuestion();proceedToKitchenBtn.interactable=true;proceedToKitchenBtn.gameObject.SetActive(false);timerText.text="Time: 30s";return true;
    }
    public void PlaceSplitModal(RectTransform panel)
    {
        Place(panel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(860,880));
        Place(questionText.rectTransform,new Vector2(.5f,.5f),new Vector2(0,150),new Vector2(620,105));questionText.fontSize=30;
        timerText.transform.SetParent(designRoot,false);timerText.gameObject.name="TimerBadge";
        Place(timerText.rectTransform,new Vector2(.5f,.5f),new Vector2(680,465),new Vector2(240,65));
        optionsRoot=optionButtons[0].transform.parent as RectTransform;
        Place(optionsRoot,new Vector2(.5f,.5f),new Vector2(0,-25),new Vector2(620,255));
        foreach(Button b in optionButtons){b.transition=Selectable.Transition.None;TMP_Text label=b.GetComponentInChildren<TMP_Text>(true);label.fontSize=25;LayoutElement le=b.GetComponent<LayoutElement>();if(le){le.minHeight=57;le.preferredHeight=57;}}
        answerHeading=PrototypeUI.Text("TriviaAnswerHeading",panel,"",0,60,620,60,33);answerHeading.fontStyle=FontStyles.Bold;answerHeading.gameObject.SetActive(false);
        rewardNote=PrototypeUI.Text("TriviaRewardNote",panel,"",0,-190,620,45,22);rewardNote.fontStyle=FontStyles.Bold;rewardNote.gameObject.SetActive(false);
        Place(proceedToKitchenBtn.transform as RectTransform,new Vector2(.5f,0),new Vector2(0,25),new Vector2(700,75));
        proceedToKitchenBtn.GetComponentInChildren<TMP_Text>(true).fontSize=28;
        ShowQuestion();
    }
    private void ShowQuestion()
    {
        if(!optionsRoot)return;optionsRoot.gameObject.SetActive(true);answerHeading.gameObject.SetActive(false);rewardNote.gameObject.SetActive(false);
        Place(feedbackBanner.rectTransform,new Vector2(.5f,.5f),new Vector2(0,-185),new Vector2(620,45));feedbackBanner.enableAutoSizing=false;feedbackBanner.fontSize=22;
        feedbackBanner.text="Choose one answer.";feedbackBanner.color=new Color(.23f,.12f,.05f);
        for(int i=0;i<4;i++){optionButtons[i].image.color=Color.white;var label=optionButtons[i].GetComponentInChildren<TMP_Text>(true);label.color=new Color(.18f,.10f,.05f);label.text=((char)('A'+i))+". "+question.Options[i];}
    }
    private void ShowAnswer(string outcome)
    {
        optionsRoot.gameObject.SetActive(false);answerHeading.gameObject.SetActive(true);rewardNote.gameObject.SetActive(true);
        bool correct=outcome=="Correct";answerHeading.text=correct?"CORRECT! RECIPE UNLOCKED":outcome=="TimedOut"?"TIME'S UP / TRY AGAIN":"LET'S TRY AGAIN";
        answerHeading.color=correct?new Color(.10f,.33f,.16f):new Color(.55f,.19f,.08f);
        Place(feedbackBanner.rectTransform,new Vector2(.5f,.5f),new Vector2(0,-85),new Vector2(610,195));feedbackBanner.fontSize=27;
        feedbackBanner.text=(!correct&&selectedChoice>=0?"You chose: "+question.Options[selectedChoice]+"\n":"")+"Answer: "+question.Options[question.Correct]+"\n\n"+question.Explanation;
        rewardNote.text=correct?"+15s timed prep  /  1.25x score bonus":"Read the explanation, then try this question again.";rewardNote.color=answerHeading.color;
        timerText.text=correct?"Ready to cook":"Try again";timerText.color=answerHeading.color;
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
        PrototypeUI.Panel("MapHeadingWood",designRoot,0,465,960,70,new Color(.24f,.115f,.055f)).raycastTarget=false;
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
    private void Answer(int choice){if(!started||resolved||paused)return;if(remaining<=0){Resolve("TimedOut");return;}selectedChoice=choice;Resolve(choice==question.Correct?"Correct":"Wrong");}
    private void Resolve(string outcome)
    {
        if(resolved)return;resolved=true;
        if(outcome=="Correct"&&!GameSessionState.ResolveTrivia(question.Id,outcome))return;
        foreach(Button b in optionButtons)b.interactable=false;
        ShowAnswer(outcome);
        proceedToKitchenBtn.GetComponentInChildren<TMP_Text>(true).text=outcome=="Correct"?"Start "+RecipePlan.For(SelectedProvince).Dish:"Try this question again";
        proceedToKitchenBtn.gameObject.SetActive(true);
    }
    private void Proceed()
    {
        if(!resolved||loading)return;
        if(!GameSessionState.TriviaAnsweredCorrectly){resolved=false;remaining=30;selectedChoice=-1;foreach(var b in optionButtons)b.interactable=true;proceedToKitchenBtn.gameObject.SetActive(false);ShowQuestion();timerText.text="Time: 30s";return;}
        if(!Application.CanStreamedLevelBeLoaded("CookingStage")){feedbackBanner.text="CookingStage is missing from the active Build Profile.";return;}
        loading=true;proceedToKitchenBtn.interactable=false;SceneManager.LoadScene("CookingStage");
    }
    private void OnApplicationPause(bool value){paused=value;}
    private void OnDestroy(){if(readableText)Destroy(readableText);if(handlers!=null)for(int i=0;i<4;i++)if(optionButtons[i])optionButtons[i].onClick.RemoveListener(handlers[i]);if(proceedToKitchenBtn)proceedToKitchenBtn.onClick.RemoveListener(Proceed);}
}
