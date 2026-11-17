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
    private StoveFlameGraphic flameGraphic;
    private RectTransform knobGroup,knob,heatPlates;
    private Image stove;
    private VesselContents contents;
    private bool laidOut,heatLayout;
    private Image pour,splash;
    public RectTransform DropTarget=>drop;
    public bool Busy=>dragging||transferring;
    public void Initialize(RectTransform design,KitchenArt art,Action<string> action,Action<float> flame)
    {
        this.art=art;this.action=action;
        overlay=PrototypeUI.Panel("CookingFocus",design,0,0,1800,1000,new Color(.16f,.08f,.035f,.98f)).rectTransform;
        stove=art.Draw("FocusedStove",overlay,art.Slice("burner"),0,-80,1400,700);
        // Flame is drawn before the vessel so it licks up around the base instead of covering it.
        flameGraphic=PrototypeUI.Rect("FocusedFlame",overlay,0,-205,430,70).gameObject.AddComponent<StoveFlameGraphic>();flameGraphic.Arc=true;flameGraphic.raycastTarget=false;
        container=art.Draw("FocusedContainer",overlay,art.OpenPot,0,20,850,570);
        food=art.Draw("VisibleFood",overlay,null,0,90,450,240);food.gameObject.SetActive(false);
        contents=new VesselContents(overlay,"FocusedContents",art,0,90,450,240);
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
        heatPlates=PrototypeUI.Rect("HeatPlates",overlay,0,0,1800,1000);
        PrototypeUI.Panel("HeatGuidePlate",heatPlates,-565,-205,560,250,new Color(.16f,.08f,.035f,.92f)).raycastTarget=false;
        PrototypeUI.Panel("HeatReadoutPlate",heatPlates,575,-235,520,150,new Color(.16f,.08f,.035f,.92f)).raycastTarget=false;
        heatPlates.SetSiblingIndex(heading.transform.GetSiblingIndex());
        // The knob sits on the stove's own front panel: OFF at the top, HIGH when turned all the way down.
        knobGroup=PrototypeUI.Rect("StoveKnobGroup",overlay,8,-240,260,260);
        StoveKnobGraphic dial=knobGroup.gameObject.AddComponent<StoveKnobGraphic>();dial.Plate=true;dial.raycastTarget=true;
        knob=PrototypeUI.Rect("StoveKnob",knobGroup,0,0,150,150);knob.gameObject.AddComponent<StoveKnobGraphic>().raycastTarget=false;
        string[] marks={"OFF","LOW","MED","HIGH"};
        for(int i=0;i<4;i++){float angle=i*60*Mathf.Deg2Rad;TMP_Text mark=PrototypeUI.Text("KnobMark"+i,knobGroup,marks[i],Mathf.Sin(angle)*128,Mathf.Cos(angle)*114,110,34,22);mark.color=new Color(.16f,.08f,.035f);mark.fontStyle=FontStyles.Bold;}
        StoveKnobControl turn=knobGroup.gameObject.AddComponent<StoveKnobControl>();turn.CanTurn=()=>interactive&&heatMode&&!Busy;turn.Changed=v=>{heat.SetValueWithoutNotify(v);flame(v);};
        heading.fontStyle=progress.fontStyle=hint.fontStyle=FontStyles.Bold;
        ghostRoot=PrototypeUI.Rect("IngredientGhostRoot",design,0,0,1800,1000);ghost=art.Draw("DraggedIngredient",ghostRoot,null,0,0,260,210);ghostRoot.gameObject.SetActive(false);overlay.gameObject.SetActive(false);
        pour=PrototypeUI.Panel("PourStream",ghostRoot,0,0,16,10,Color.white);pour.raycastTarget=false;pour.rectTransform.pivot=new Vector2(.5f,1);pour.transform.SetAsFirstSibling();pour.gameObject.SetActive(false);
        pour.sprite=art.Loose("PourStream");if(pour.sprite){pour.type=Image.Type.Filled;pour.fillMethod=Image.FillMethod.Vertical;pour.fillOrigin=(int)Image.OriginVertical.Top;}
        splash=art.Draw("PourSplash",ghostRoot,art.Loose("PourSplash"),0,0,330,215);splash.transform.SetAsFirstSibling();splash.gameObject.SetActive(false);
    }
    public void Render(CookingAttempt attempt,bool canInteract)
    {
        model=attempt;interactive=canInteract;
        if(!interactive){Cancel();return;}
        heat.SetValueWithoutNotify((float)model.Flame);heat.interactable=interactive;
        container.sprite=art.Vessel(model.Recipe);
container.color=CookingVesselPresentation.VesselTint(model);
        steam.gameObject.SetActive(CookingVesselPresentation.Steaming(model));bubbles.gameObject.SetActive(CookingVesselPresentation.Bubbling(model));
        Layout(heatMode);FitToVessel();contents.Render(model);
        flameGraphic.Level=(float)model.Flame;knobGroup.gameObject.SetActive(heatMode);knob.localRotation=Quaternion.Euler(0,0,-180*(float)model.Flame);
        heading.text=heatMode?"TURN THE STOVE KNOB":"ADD TO THE "+model.Recipe.Container;
        progress.text=heatMode?"HEAT "+Mathf.RoundToInt((float)model.Heat)+" / 110\nKeep it between 60 and 85":"HEAT INDEX "+Mathf.RoundToInt((float)model.Heat)+" / 110   |   Target 60-85";
        hint.text=heatMode?"Hold the knob and turn it to the right.\nOFF is at the top. HIGH is at the bottom.\nWatch the flame grow under the pot.":"Release the ingredient inside the highlighted opening. Drop outside to return it.";
    }
    // Heat view zooms in on the stove front so its knob and burner flame are both in view.
    private void Layout(bool heatView)
    {
        if(laidOut&&heatLayout==heatView)return;laidOut=true;heatLayout=heatView;
        if(heatView)
        {
            Place(stove.rectTransform,0,30,1300,778);Place(container.rectTransform,9,285,520,349);
            Place(bubbles.rectTransform,9,337,257,92);Place(steam.rectTransform,9,407,150,95);flameGraphic.Size=.85f;
            Place(heading.rectTransform,-565,-120,520,60);heading.fontSize=30;Place(hint.rectTransform,-565,-235,520,170);hint.fontSize=23;
            Place(progress.rectTransform,575,-235,500,120);progress.fontSize=30;
        }
        else
        {
            Place(stove.rectTransform,0,-80,1400,700);Place(container.rectTransform,0,20,850,570);
            Place(bubbles.rectTransform,0,105,420,150);Place(steam.rectTransform,0,220,240,150);flameGraphic.Size=1;
            Place(heading.rectTransform,0,330,1300,100);heading.fontSize=37;Place(hint.rectTransform,0,-385,1420,105);hint.fontSize=27;
            Place(progress.rectTransform,0,-300,1300,60);progress.fontSize=29;
        }
        heatPlates.gameObject.SetActive(heatView);
    }
    // Flame sits just under the vessel's visible base; the contents sit in its opening. Both follow the vessel art.
    private void FitToVessel()
    {
        RectTransform v=container.rectTransform;float baseY=art.VesselBase(container),width=art.VesselWidth(container);
        float height=container.sprite?Mathf.Min(v.sizeDelta.y,v.sizeDelta.x*container.sprite.rect.height/container.sprite.rect.width):v.sizeDelta.y;
        Place(flameGraphic.rectTransform,v.anchoredPosition.x,baseY+4,width*.62f,heatLayout?60:70);
        float interior=model!=null&&model.Recipe.UsesWok?.60f:.70f;
        contents.Place(v.anchoredPosition.x,baseY+height*interior,width*.56f,height*.34f);
    }
    private static void Place(RectTransform target,float x,float y,float w,float h){target.anchoredPosition=new Vector2(x,y);target.sizeDelta=new Vector2(w,h);}
    public void OpenHeat(){if(!interactive||Busy)return;heatMode=true;overlay.gameObject.SetActive(true);heat.gameObject.SetActive(false);drop.gameObject.SetActive(false);lid.gameObject.SetActive(false);Render(model,interactive);}
    public RectTransform BeginIngredient(string id)
    {
        if(!interactive||Busy)return null;
        dragging=true;heatMode=false;overlay.gameObject.SetActive(true);heat.gameObject.SetActive(false);drop.gameObject.SetActive(true);lid.gameObject.SetActive(!model.Recipe.UsesWok);lid.anchoredPosition=new Vector2(0,120);lid.localRotation=Quaternion.identity;
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
        bool liquid=id=="vinegar"||id=="broth"||id=="soy";
        if(liquid)
        {
            // Liquids are tipped over the container and poured instead of dropped in.
            // A bottle tips to the right of its neck; the broth pan tips away from its handle.
            bool pan=id=="broth";bool streamArt=pour.sprite,splashArt=splash.sprite;
            Vector2 above=to+(pan?new Vector2(95,290):new Vector2(-115,265)),mouth=pan?new Vector2(-93,-81):new Vector2(100,-28);
            float tilt=pan?55:-105,length=pan?215:235;
            Color tone=id=="vinegar"?new Color(.98f,.93f,.66f):pan?new Color(.86f,.58f,.25f):new Color(.34f,.17f,.09f);
            for(float t=0;t<1;t+=Time.unscaledDeltaTime*3.5f){ghost.rectTransform.anchoredPosition=Vector2.Lerp(from,above,Mathf.SmoothStep(0,1,t));yield return null;}
            ghost.rectTransform.anchoredPosition=above;
            pour.color=streamArt?tone:new Color(tone.r,tone.g,tone.b,.88f);
            pour.rectTransform.anchoredPosition=above+mouth;pour.rectTransform.sizeDelta=streamArt?new Vector2(56,length):new Vector2(16,0);
            if(streamArt){pour.fillOrigin=(int)Image.OriginVertical.Top;pour.fillAmount=0;}
            pour.gameObject.SetActive(true);
            if(splashArt){splash.rectTransform.anchoredPosition=to+new Vector2(0,-15);splash.color=new Color(tone.r,tone.g,tone.b,0);splash.gameObject.SetActive(true);}
            for(float t=0;t<1;t+=Time.unscaledDeltaTime*1.4f)
            {
                ghost.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(0,tilt,Mathf.Clamp01(t*5)));
                float flow=t<.8f?Mathf.Clamp01((t-.15f)*5):Mathf.Clamp01((1-t)*5);
                if(streamArt){pour.fillOrigin=(int)(t<.8f?Image.OriginVertical.Top:Image.OriginVertical.Bottom);pour.fillAmount=flow;}
                else pour.rectTransform.sizeDelta=new Vector2(12+4*Mathf.Sin(t*40),length*flow);
                if(splashArt){splash.rectTransform.localScale=Vector3.one*Mathf.Lerp(.35f,1,Mathf.Clamp01((t-.3f)*2.2f));splash.color=new Color(tone.r,tone.g,tone.b,Mathf.Clamp01((t-.3f)*4)*Mathf.Clamp01((1.05f-t)*4));}
                yield return null;
            }
            pour.gameObject.SetActive(false);splash.gameObject.SetActive(false);ghost.rectTransform.localRotation=Quaternion.identity;
        }
        else
        {
            for(float t=0;t<1;t+=Time.unscaledDeltaTime*2.5f){ghost.rectTransform.anchoredPosition=Vector2.Lerp(from,to,t)+new Vector2(0,Mathf.Sin(t*Mathf.PI)*80);ghost.rectTransform.localScale=Vector3.one*Mathf.Lerp(1,.45f,t);yield return null;}
        }
        ghost.rectTransform.localScale=Vector3.one;ghostRoot.gameObject.SetActive(false);drop.gameObject.SetActive(false);
        if(interactive)action("add:"+id);
        // Keep the container in view briefly so the added ingredient is seen inside it.
        for(float t=0;t<1;t+=Time.unscaledDeltaTime*1.6f){if(model!=null)contents.Render(model);contents.Root.localScale=Vector3.one*Mathf.Lerp(.75f,1,Mathf.SmoothStep(0,1,Mathf.Clamp01(t*3)));yield return null;}
        contents.Root.localScale=Vector3.one;transferring=false;animation=null;Close();
    }
    public void Close(){if(Busy){Cancel();return;}overlay.gameObject.SetActive(false);ghostRoot.gameObject.SetActive(false);}
    public void Cancel(){if(animation!=null)StopCoroutine(animation);animation=null;dragging=transferring=false;if(overlay)overlay.gameObject.SetActive(false);if(ghostRoot)ghostRoot.gameObject.SetActive(false);if(ghost){ghost.rectTransform.localScale=Vector3.one;ghost.rectTransform.localRotation=Quaternion.identity;}if(pour)pour.gameObject.SetActive(false);if(splash)splash.gameObject.SetActive(false);if(contents!=null)contents.Root.localScale=Vector3.one;}
    private void OnDisable(){Cancel();}
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PotLidGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper m){m.Clear();Ellipse(m,Vector2.zero,340,80,new Color(.35f,.13f,.04f));Ellipse(m,new Vector2(0,10),310,70,new Color(.69f,.32f,.14f));Ellipse(m,new Vector2(0,48),62,28,new Color(.35f,.13f,.04f));Ellipse(m,new Vector2(0,55),48,24,new Color(.75f,.39f,.18f));}
    private void Ellipse(VertexHelper m,Vector2 c,float rx,float ry,Color color){for(int i=0;i<48;i++){int k=m.currentVertCount;float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;m.AddVert(c,color,Vector2.zero);m.AddVert(c+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),color,Vector2.zero);m.AddVert(c+new Vector2(Mathf.Cos(b)*rx,Mathf.Sin(b)*ry),color,Vector2.zero);m.AddTriangle(k,k+1,k+2);}}
}

// Stove flame that grows with the heat setting. Presentation only.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StoveFlameGraphic : MaskableGraphic
{
    public float Level,Size=1;
    public bool Arc;
    private float time;
    private void Update(){if(Level>.01f){time+=Time.unscaledDeltaTime;SetVerticesDirty();}}
    protected override void OnPopulateMesh(VertexHelper m)
    {
        m.Clear();if(Level<=.01f)return;float half=rectTransform.rect.width*.5f,depth=rectTransform.rect.height*.5f;
        for(int i=0;i<13;i++)
        {
            // Arc follows the front edge of a round burner; otherwise the flames sit on a straight line.
            float side=i/12f*2-1,floor=Arc?-Mathf.Sqrt(Mathf.Max(0,1-side*side))*depth:0;
            float flicker=.78f+.22f*Mathf.Sin(time*9+i*1.7f),height=(20+74*Level)*flicker*(1-.30f*Mathf.Abs(side))*Size;
            Tongue(m,side*half,floor,26*Size,height,new Color(1,.42f,.06f,.96f));Tongue(m,side*half,floor,13*Size,height*.58f,new Color(1,.86f,.36f,.98f));
        }
    }
    private static void Tongue(VertexHelper m,float x,float y,float halfWidth,float height,Color tint)
    {int k=m.currentVertCount;m.AddVert(new Vector2(x-halfWidth,y),tint,Vector2.zero);m.AddVert(new Vector2(x+halfWidth,y),tint,Vector2.zero);m.AddVert(new Vector2(x,y+height),tint,Vector2.zero);m.AddTriangle(k,k+1,k+2);}
}
// Stove knob. Plate draws the fixed dial with tick marks; otherwise the round knob with its grip bar,
// which the view rotates: pointer up is OFF, pointer down is the highest flame.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StoveKnobGraphic : MaskableGraphic
{
    public bool Plate;
    protected override void OnPopulateMesh(VertexHelper m)
    {
        m.Clear();float r=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
        if(Plate)
        {
            Disc(m,Vector2.zero,r*.74f,new Color(.10f,.09f,.09f));Disc(m,Vector2.zero,r*.69f,new Color(.76f,.77f,.79f));
            for(int i=0;i<=6;i++){float a=i*30*Mathf.Deg2Rad;Disc(m,new Vector2(Mathf.Sin(a),Mathf.Cos(a))*r*.64f,i%2==0?5:3,new Color(.16f,.08f,.035f));}
            return;
        }
        Disc(m,Vector2.zero,r,new Color(.05f,.05f,.05f));Disc(m,Vector2.zero,r*.93f,new Color(.19f,.19f,.20f));Disc(m,Vector2.zero,r*.80f,new Color(.11f,.11f,.12f));
        Quad(m,-r*.22f,-r*.93f,r*.22f,r*.93f,new Color(.05f,.05f,.05f));Quad(m,-r*.18f,-r*.90f,r*.18f,r*.90f,new Color(.27f,.27f,.29f));
        Quad(m,-r*.18f,-r*.90f,-r*.10f,r*.90f,new Color(.40f,.40f,.43f));Quad(m,-r*.05f,r*.42f,r*.05f,r*.86f,new Color(1,.93f,.72f));
    }
    private static void Quad(VertexHelper m,float x0,float y0,float x1,float y1,Color tint)
    {int k=m.currentVertCount;m.AddVert(new Vector2(x0,y0),tint,Vector2.zero);m.AddVert(new Vector2(x1,y0),tint,Vector2.zero);m.AddVert(new Vector2(x1,y1),tint,Vector2.zero);m.AddVert(new Vector2(x0,y1),tint,Vector2.zero);m.AddTriangle(k,k+1,k+2);m.AddTriangle(k,k+2,k+3);}
    private static void Disc(VertexHelper m,Vector2 c,float r,Color tint)
    {int k=m.currentVertCount;m.AddVert(c,tint,Vector2.zero);for(int i=0;i<=40;i++){float a=i*Mathf.PI*2/40;m.AddVert(c+new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r),tint,Vector2.zero);}for(int i=0;i<40;i++)m.AddTriangle(k,k+1+i,k+2+i);}
}
// Turns pointer or touch movement around the knob into a flame setting: top is 0, clockwise to the bottom is 1.
public sealed class StoveKnobControl : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IDragHandler
{
    public Action<float> Changed;
    public Func<bool> CanTurn;
    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData){Turn(eventData);}
    public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData){Turn(eventData);}
    private void Turn(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if(Changed==null||CanTurn!=null&&!CanTurn())return;
        Vector2 local;
        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,eventData.position,eventData.pressEventCamera,out local))return;
        if(local.sqrMagnitude<100)return;
        float angle=Mathf.Atan2(local.x,local.y)*Mathf.Rad2Deg;
        Changed(Mathf.Clamp01(angle>=0?angle/180f:angle<-90?1:0));
    }
}
