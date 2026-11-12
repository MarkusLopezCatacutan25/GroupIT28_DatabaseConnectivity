using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Focused stove/container presentation and ingredient transfer animation only.
public sealed class CookingFocusView : MonoBehaviour
{
    private RectTransform overlay,drop,ghostRoot,lid;
    private Image ghost,container,food;
    private TMP_Text heading,progress,hint;
    private Slider heat;
    private CookingAttempt model;
    private bool interactive,dragging,heatMode,transferring;
    private Action<string> action;
    private KitchenArt art;
    private Coroutine animation;
    private KitchenCueGraphic steam,bubbles;
    public RectTransform DropTarget=>drop;
    public bool Busy=>dragging||transferring;
    public void Initialize(RectTransform design,KitchenArt art,Action<string> action,Action<float> flame)
    {
        this.art=art;this.action=action;
        overlay=PrototypeUI.Panel("CookingFocus",design,0,0,1800,1000,new Color(.16f,.08f,.035f,.98f)).rectTransform;
        art.Draw("FocusedStove",overlay,art.Slice("burner"),0,-80,1400,700);
        container=art.Draw("FocusedContainer",overlay,art.OpenPot,0,20,850,570);
        food=art.Draw("VisibleFood",overlay,null,0,90,450,240);
        bubbles=PrototypeUI.Rect("FocusedSimmerBubbles",overlay,0,105,420,150).gameObject.AddComponent<KitchenCueGraphic>();bubbles.Boiling=true;bubbles.raycastTarget=false;
        steam=PrototypeUI.Rect("FocusedSteam",overlay,0,220,240,150).gameObject.AddComponent<KitchenCueGraphic>();steam.Steam=true;steam.raycastTarget=false;
        drop=PrototypeUI.Rect("PotDropTarget",overlay,0,100,650,340);
        var ring=drop.gameObject.AddComponent<KitchenCueGraphic>();ring.color=new Color(1,.85f,.38f);ring.raycastTarget=false;
        lid=PrototypeUI.Rect("AnimatedLid",overlay,0,120,700,200);var lidGraphic=lid.gameObject.AddComponent<PotLidGraphic>();lidGraphic.raycastTarget=false;
        heading=PrototypeUI.Text("FocusHeading",overlay,"",0,330,1300,100,37);heading.color=new Color(1,.94f,.78f);
        progress=PrototypeUI.Text("CookingProgress",overlay,"",0,-230,1300,75,29);progress.color=new Color(1,.94f,.78f);
        hint=PrototypeUI.Text("FocusHint",overlay,"",0,-385,1420,105,27);hint.color=new Color(1,.94f,.78f);
        heat=PrototypeUI.HeatSlider(overlay,0,-300,840);heat.onValueChanged.AddListener(v=>flame(v));
        PrototypeUI.Button("CloseCookingFocus",overlay,"Back to kitchen",-650,300,350,70,Close);
        ghostRoot=PrototypeUI.Rect("IngredientGhostRoot",design,0,0,1800,1000);ghost=art.Draw("DraggedIngredient",ghostRoot,null,0,0,260,210);ghostRoot.gameObject.SetActive(false);overlay.gameObject.SetActive(false);
    }
    public void Render(CookingAttempt attempt,bool canInteract)
    {
        model=attempt;interactive=canInteract;
        if(!interactive){Cancel();return;}
        heat.SetValueWithoutNotify((float)model.Flame);heat.interactable=interactive;
        container.sprite=model.Recipe.IsHabhab?art.Atlas(10):art.EmptyPalayok;
        string visibleFood=model.Recipe.IsHabhab?(model.AdditionIndex>=5?"noodles":model.AdditionIndex>=3?"vegetables":model.AdditionIndex>=2?"onion":"garlic"):(model.AdditionIndex>=3?"chicken":model.AdditionIndex>=2?"turmeric":"garlic");
        food.sprite=model.AdditionIndex>0?art.IngredientSprite(visibleFood,true):null;
        food.gameObject.SetActive(food.sprite);
        food.color=CookingVesselPresentation.FoodTint(model);container.color=CookingVesselPresentation.VesselTint(model);
        steam.gameObject.SetActive(CookingVesselPresentation.Steaming(model));bubbles.gameObject.SetActive(CookingVesselPresentation.Bubbling(model));
        heading.text=heatMode?"ADJUST THE STOVE HEAT":"ADD TO THE "+model.Recipe.Container;
        progress.text="HEAT INDEX "+Mathf.RoundToInt((float)model.Heat)+" / 110   |   Target 60-85";
        hint.text=heatMode?"OFF       LOW       MEDIUM       HIGH\nGame heat is an abstract index, not degrees Celsius.":"Release the ingredient inside the highlighted opening. Drop outside to return it.";
    }
    public void OpenHeat(){if(!interactive||Busy)return;heatMode=true;overlay.gameObject.SetActive(true);heat.gameObject.SetActive(true);drop.gameObject.SetActive(false);lid.gameObject.SetActive(false);Render(model,interactive);}
    public RectTransform BeginIngredient(string id)
    {
        if(!interactive||Busy)return null;
        dragging=true;heatMode=false;overlay.gameObject.SetActive(true);heat.gameObject.SetActive(false);drop.gameObject.SetActive(true);lid.gameObject.SetActive(!model.Recipe.IsHabhab);lid.anchoredPosition=new Vector2(0,120);lid.localRotation=Quaternion.identity;
        ghost.sprite=art.IngredientSprite(id,true);ghostRoot.gameObject.SetActive(true);ghostRoot.SetAsLastSibling();
        if(animation!=null)StopCoroutine(animation);animation=StartCoroutine(LiftLid());return ghost.rectTransform;
    }
    private IEnumerator LiftLid(){for(float t=0;t<1;t+=Time.deltaTime*3){float eased=Mathf.SmoothStep(0,1,t);lid.anchoredPosition=Vector2.Lerp(new Vector2(0,120),new Vector2(-360,260),eased);lid.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(0,25,eased));yield return null;}lid.gameObject.SetActive(false);animation=null;}
    public void EndIngredient(string id,bool accepted,Vector2 from)
    {
        if(!dragging)return;dragging=false;
        if(!accepted||!interactive){Close();return;}
        if(animation!=null)StopCoroutine(animation);animation=StartCoroutine(Transfer(id,from));
    }
    private IEnumerator Transfer(string id,Vector2 from)
    {
        transferring=true;lid.gameObject.SetActive(false);
        Vector2 to=ghostRoot.InverseTransformPoint(drop.position);
        for(float t=0;t<1;t+=Time.unscaledDeltaTime*2.5f){ghost.rectTransform.anchoredPosition=Vector2.Lerp(from,to,t)+new Vector2(0,Mathf.Sin(t*Mathf.PI)*80);ghost.rectTransform.localScale=Vector3.one*Mathf.Lerp(1,.25f,t);yield return null;}
        ghost.rectTransform.localScale=Vector3.one;ghostRoot.gameObject.SetActive(false);transferring=false;animation=null;
        if(interactive)action("add:"+id);Close();
    }
    public void Close(){if(Busy){Cancel();return;}overlay.gameObject.SetActive(false);ghostRoot.gameObject.SetActive(false);}
    public void Cancel(){if(animation!=null)StopCoroutine(animation);animation=null;dragging=transferring=false;if(overlay)overlay.gameObject.SetActive(false);if(ghostRoot)ghostRoot.gameObject.SetActive(false);if(ghost)ghost.rectTransform.localScale=Vector3.one;}
    private void OnDisable(){Cancel();}
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PotLidGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper m){m.Clear();Ellipse(m,Vector2.zero,340,80,new Color(.35f,.13f,.04f));Ellipse(m,new Vector2(0,10),310,70,new Color(.69f,.32f,.14f));Ellipse(m,new Vector2(0,48),62,28,new Color(.35f,.13f,.04f));Ellipse(m,new Vector2(0,55),48,24,new Color(.75f,.39f,.18f));}
    private void Ellipse(VertexHelper m,Vector2 c,float rx,float ry,Color color){for(int i=0;i<48;i++){int k=m.currentVertCount;float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;m.AddVert(c,color,Vector2.zero);m.AddVert(c+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),color,Vector2.zero);m.AddVert(c+new Vector2(Mathf.Cos(b)*rx,Mathf.Sin(b)*ry),color,Vector2.zero);m.AddTriangle(k,k+1,k+2);}}
}
