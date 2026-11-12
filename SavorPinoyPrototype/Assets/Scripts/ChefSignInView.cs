using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sign-in widgets and safe-area layout only; authentication is supplied by the coordinator.
public sealed class ChefSignInView
{
    private readonly RectTransform overlay,safe,card;
    private readonly TMP_InputField alias;
    private readonly TMP_Text status;
    private readonly Button submit;
    public event Action SubmitRequested,BackRequested;
    public string Alias=>alias.text;
    public bool Visible=>overlay.gameObject.activeSelf;
    public ChefSignInView(Canvas canvas)
    {
        PrototypeUI.Font=TMP_Settings.defaultFontAsset;
        overlay=PrototypeUI.Panel("ChefSignInOverlay",canvas.transform,0,0,1920,1080,new Color(.12f,.055f,.02f,.92f)).rectTransform;
        overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
        safe=PrototypeUI.Rect("SignInSafeArea",overlay,0,0,1920,1080);
        card=PrototypeUI.Panel("SignInWoodFrame",safe,0,0,1000,760,new Color(.33f,.17f,.07f)).rectTransform;
        PrototypeUI.Panel("SignInParchment",card,0,0,960,720,new Color(.98f,.92f,.77f)).raycastTarget=false;
        foreach(Image source in canvas.GetComponentsInChildren<Image>(true))
        {
            if(source.name!="Logo"||!source.sprite)continue;
            Image logo=PrototypeUI.Panel("SignInLogo",card,0,260,420,130,Color.white);logo.sprite=source.sprite;logo.preserveAspect=true;logo.raycastTarget=false;break;
        }
        var title=PrototypeUI.Text("SignInHeading",card,"CHEF SIGN-IN",0,155,860,70,40);title.fontStyle=FontStyles.Bold;
        PrototypeUI.Text("SignInIntro",card,"Sign in with a guest profile to save your cooking progress.",0,85,850,70,26);
        PrototypeUI.Text("SignInAliasLabel",card,"Chef name (display only / optional)",0,10,800,42,24);
        alias=PrototypeUI.Input("SignInChefAlias",card,0,-55,650,68);((TMP_Text)alias.placeholder).text="Chef";
        status=PrototypeUI.Text("SignInStatus",card,"",0,-140,850,85,24);
        submit=PrototypeUI.Button("GuestSignInButton",card,"Continue as guest",0,-235,650,78,()=>SubmitRequested?.Invoke());
        PrototypeUI.Button("BackFromSignIn",card,"Back",0,-325,240,50,()=>BackRequested?.Invoke());
        Hide();
    }
    public void Show(){alias.text=PlayerPrefs.GetString("SavorPinoy.ChefAlias","");overlay.SetAsLastSibling();overlay.gameObject.SetActive(true);Fit();}
    public void Hide(){overlay.gameObject.SetActive(false);}
    public void Render(ChefSignInCoordinator signIn)
    {
        status.text=signIn.Message;submit.interactable=alias.interactable=!signIn.Busy;
        submit.GetComponentInChildren<TMP_Text>().text=signIn.Busy?"Signing in...":signIn.State==ChefSignInState.Error?"Retry sign-in":"Continue as guest";
    }
    public void Fit()
    {
        if(Screen.width<=0||Screen.height<=0)return;Rect area=Screen.safeArea;
        safe.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);safe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
        float scale=Mathf.Min(1,(safe.rect.width-40)/1000,(safe.rect.height-40)/760);card.localScale=Vector3.one*Mathf.Max(.1f,scale);
    }
}
