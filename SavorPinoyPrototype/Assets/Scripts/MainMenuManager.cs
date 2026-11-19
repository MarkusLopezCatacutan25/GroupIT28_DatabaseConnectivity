using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuManager : MonoBehaviour
{
    private RectTransform options;
    [SerializeField] private FirebaseSettings firebaseSettings;
    private ChefSignInController signIn;
    private void Start()
    {
        AudioListener.volume=PlayerPrefs.GetFloat("SavorPinoy.Volume",1);ChefAccessSession.Clear();
        Canvas canvas=MenuCanvas();
        if(!canvas){Debug.LogError("Main Menu needs a Canvas for sign-in.");return;}
        ScreenBrightness.Apply(PlayerPrefs.GetFloat("SavorPinoy.Brightness",1));FitStartArt(canvas);
        PrototypeUI.Font=TMP_Settings.defaultFontAsset;var cardsButton=PrototypeUI.Button("HeritageCardsButton",canvas.transform,"Heritage Cards  "+HeritageCards.Count+"/"+RecipePlan.All.Length,700,-455,380,84,OpenHeritageCards);var cardsRect=(RectTransform)cardsButton.transform;cardsRect.anchorMin=cardsRect.anchorMax=cardsRect.pivot=new Vector2(1,0);cardsRect.anchoredPosition=new Vector2(-36,36);cardsButton.GetComponentInChildren<TMP_Text>().fontStyle=FontStyles.Bold;
        var howButton=PrototypeUI.Button("HowToPlayButton",canvas.transform,"How to Play",0,0,300,84,OpenHowToPlay);var howRect=(RectTransform)howButton.transform;howRect.anchorMin=howRect.anchorMax=howRect.pivot=Vector2.zero;howRect.anchoredPosition=new Vector2(36,36);howButton.GetComponentInChildren<TMP_Text>().fontStyle=FontStyles.Bold;
        // Instructor access: read-only dashboard behind an admin email/password account.
        var adminButton=PrototypeUI.Button("AdminButton",canvas.transform,"Admin",0,0,170,64,()=>{var c=MenuCanvas();if(c)AdminDashboard.Show(c.transform,firebaseSettings);});var adminRect=(RectTransform)adminButton.transform;adminRect.anchorMin=adminRect.anchorMax=adminRect.pivot=new Vector2(0,1);adminRect.anchoredPosition=new Vector2(28,-28);adminButton.GetComponentInChildren<TMP_Text>().fontSize=24;
        signIn=gameObject.AddComponent<ChefSignInController>();
        signIn.Initialize(canvas,new FirebaseAuthManager(new FirebaseBootstrap(firebaseSettings)));
    }
    public void PlayGame()
    {
        SavorAudio.Click();
        if(!Application.CanStreamedLevelBeLoaded("RegionalTriviaHub")){Debug.LogError("RegionalTriviaHub is not enabled in the active Build Profile.");return;}
        // First time on this device: play the demonstration tutorial before sign-in (it can be skipped).
        if(!DemoTutorial.Seen){OpenHowToPlay(()=>{if(signIn)signIn.Show();});return;}
        if(signIn)signIn.Show();
    }
    public void OpenOptions()
    {
        SavorAudio.Click();
        if(options){options.gameObject.SetActive(true);return;}
        Canvas canvas=MenuCanvas();if(!canvas)return;
        PrototypeUI.Font=TMP_Settings.defaultFontAsset;
        options=PrototypeUI.Panel("OptionsPanel",canvas.transform,0,0,820,680,new Color(.96f,.89f,.72f)).rectTransform;ScreenFit.Attach(options,820,680);
        PrototypeUI.Text("OptionsTitle",options,"Settings",0,255,700,90,44).fontStyle=FontStyles.Bold;
        PrototypeUI.Text("VolumeLabel",options,"Volume",0,160,700,65,32);
        Slider slider=PrototypeUI.HeatSlider(options,0,90,600);slider.value=AudioListener.volume;
        TMP_Text volumeValue=PrototypeUI.Text("VolumeValue",options,Mathf.RoundToInt(AudioListener.volume*100)+"%",330,160,130,65,30);
        slider.onValueChanged.AddListener(value=>{volumeValue.text=Mathf.RoundToInt(value*100)+"%";AudioListener.volume=value;PlayerPrefs.SetFloat("SavorPinoy.Volume",value);});
        PrototypeUI.Text("BrightnessLabel",options,"Brightness",0,-20,700,65,32);
        Slider bright=PrototypeUI.HeatSlider(options,0,-90,600);bright.gameObject.name="BrightnessSlider";bright.minValue=.4f;bright.maxValue=1;bright.value=PlayerPrefs.GetFloat("SavorPinoy.Brightness",1);
        TMP_Text brightValue=PrototypeUI.Text("BrightnessValue",options,Mathf.RoundToInt(bright.value*100)+"%",330,-20,130,65,30);
        bright.onValueChanged.AddListener(value=>{brightValue.text=Mathf.RoundToInt(value*100)+"%";ScreenBrightness.Apply(value);PlayerPrefs.SetFloat("SavorPinoy.Brightness",value);});
        PrototypeUI.Button("CloseOptions",options,"Back",0,-245,320,80,()=>{PlayerPrefs.Save();options.gameObject.SetActive(false);});
    }
    private Canvas menuCanvas;
    // The scene's own canvas; the brightness overlay adds a second canvas that must not be picked up here.
    private Canvas MenuCanvas()
    {
        if(menuCanvas)return menuCanvas;
        foreach(Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(c.gameObject.scene==gameObject.scene&&c.isRootCanvas){menuCanvas=c;break;}
        return menuCanvas;
    }
    // Beginner tutorial: animated demonstration pages with Next / Back / Skip (see DemoTutorial).
    public void OpenHowToPlay(){OpenHowToPlay(null);}
    private void OpenHowToPlay(System.Action after)
    {
        Canvas canvas=MenuCanvas();if(!canvas)return;PrototypeUI.Font=TMP_Settings.defaultFontAsset;
        KitchenArt art=GetComponent<KitchenArt>();if(!art)art=gameObject.AddComponent<KitchenArt>();
        TutorialCoach.ResetAll();DemoTutorial.Show(canvas.transform,art,after);
    }
    public void QuitGame(){PlayerPrefs.Save();Application.Quit();}
    private RectTransform cards;
    // Heritage Card collection: one card per dish, earned by a verified save of a completed recipe. Device only.
    public void OpenHeritageCards()
    {
        if(cards){Destroy(cards.gameObject);}
        Canvas canvas=MenuCanvas();if(!canvas)return;PrototypeUI.Font=TMP_Settings.defaultFontAsset;
        KitchenArt art=GetComponent<KitchenArt>();if(!art)art=gameObject.AddComponent<KitchenArt>();
        cards=PrototypeUI.Panel("HeritageCards",canvas.transform,0,0,1920,1080,new Color(.12f,.055f,.02f,.94f)).rectTransform;ScreenFit.Attach(cards,1920,1080,true);
        cards.anchorMin=Vector2.zero;cards.anchorMax=Vector2.one;cards.offsetMin=cards.offsetMax=Vector2.zero;
        var title=PrototypeUI.Text("CardsTitle",cards,"HERITAGE CARDS",0,430,1200,80,52);title.color=new Color(1,.94f,.78f);title.fontStyle=FontStyles.Bold;
        var sub=PrototypeUI.Text("CardsSubtitle",cards,"Complete a dish and save your result to earn its card.   "+HeritageCards.Count+" of "+RecipePlan.All.Length+" collected",0,368,1500,50,26);sub.color=new Color(1,.94f,.78f);
        string[] art_={"DishAdobo","DishLomi","DishHabhab","DishHardinera"};
        for(int i=0;i<RecipePlan.All.Length;i++)
        {
            RecipePlan dish=RecipePlan.All[i];bool owned=HeritageCards.Unlocked(dish.Id);float x=-660+i*440;
            var frame=PrototypeUI.Rect("Card_"+dish.Id,cards,x,-40,400,620);frame.gameObject.AddComponent<RecipeCardFrameGraphic>().raycastTarget=false;
            var picture=art.Draw("CardDish",frame,art.Loose(art_[i]),0,150,330,220);if(!owned)picture.color=new Color(0,0,0,.55f);
            var cardName=PrototypeUI.Text("CardName",frame,dish.Dish,0,10,360,56,32);cardName.fontStyle=FontStyles.Bold;
            PrototypeUI.Text("CardProvince",frame,dish.Province+" / CALABARZON",0,-34,360,36,22);
            var body=PrototypeUI.Text("CardText",frame,owned?dish.Heritage:"LOCKED\nCook and save "+dish.Dish+" to unlock this card.",0,-128,340,160,owned?17:22);
            var groups=PrototypeUI.Text("CardFoodGroups",frame,RecipePlan.FoodGroups(dish.Id),0,-255,340,84,19);groups.richText=true;groups.lineSpacing=-8;
            body.enableAutoSizing=owned;body.fontSizeMin=12;body.fontSizeMax=18;if(!owned)body.fontStyle=FontStyles.Bold;
        }
        PrototypeUI.Button("CloseCards",cards,"Back",0,-445,320,80,()=>{Destroy(cards.gameObject);cards=null;});
    }
    // The start art is a 16:9 picture that already contains the logo. Keep its shape on every screen
    // instead of stretching it, and fill any side bars with a dimmed copy.
    private static void FitStartArt(Canvas canvas)
    {
        Transform logo=canvas.transform.Find("Logo");if(logo)logo.gameObject.SetActive(false);
        Transform background=canvas.transform.Find("Background");if(!background)return;
        Image art=background.GetComponent<Image>();if(!art||!art.sprite)return;
        Image fill=PrototypeUI.Panel("BackgroundFill",canvas.transform,0,0,1920,1080,new Color(.30f,.22f,.16f));fill.sprite=art.sprite;fill.raycastTarget=false;
        RectTransform area=fill.rectTransform;area.anchorMin=Vector2.zero;area.anchorMax=Vector2.one;area.offsetMin=area.offsetMax=Vector2.zero;area.SetAsFirstSibling();
        AspectRatioFitter fitter=background.gameObject.AddComponent<AspectRatioFitter>();fitter.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fitter.aspectRatio=art.sprite.rect.width/art.sprite.rect.height;
    }
}

// Screen dimmer shared by every scene. It never blocks touches.
public static class ScreenBrightness
{
    private static Image shade;
    public static void Apply(float value)
    {
        if(!shade)
        {
            GameObject root=new GameObject("ScreenBrightness",typeof(Canvas));Object.DontDestroyOnLoad(root);
            Canvas canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32000;
            shade=PrototypeUI.Panel("Shade",root.transform,0,0,100,100,Color.black);shade.raycastTarget=false;
            RectTransform area=shade.rectTransform;area.anchorMin=Vector2.zero;area.anchorMax=Vector2.one;area.offsetMin=area.offsetMax=Vector2.zero;
        }
        shade.color=new Color(0,0,0,Mathf.Clamp01(1-value)*.75f);
    }
}
