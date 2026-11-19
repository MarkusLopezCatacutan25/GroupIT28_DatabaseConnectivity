using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Wooden sandok (ladle) drawn as a mesh, lying horizontally: bowl on the left, handle to the right.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SandokGraphic : MaskableGraphic
{
    private static readonly Color Dark=new Color(.33f,.17f,.07f), Wood=new Color(.78f,.53f,.28f), Light=new Color(.90f,.68f,.40f);
    protected override void OnPopulateMesh(VertexHelper m)
    {
        m.Clear();Rect r=rectTransform.rect;float h=r.height,w=r.width,cx=r.xMin+h*.55f;
        Capsule(m,new Vector2(cx+h*.3f,0),new Vector2(r.xMax-h*.18f,h*.06f),h*.14f,Dark);Capsule(m,new Vector2(cx+h*.3f,0),new Vector2(r.xMax-h*.18f,h*.06f),h*.09f,Wood);
        Ellipse(m,new Vector2(cx,0),h*.52f,h*.42f,Dark);Ellipse(m,new Vector2(cx,0),h*.46f,h*.36f,Wood);Ellipse(m,new Vector2(cx-h*.05f,h*.05f),h*.30f,h*.20f,Light);
    }
    internal static void Ellipse(VertexHelper m,Vector2 c,float rx,float ry,Color t)
    {int k=m.currentVertCount;m.AddVert(c,t,Vector2.zero);for(int i=0;i<=32;i++){float a=i*Mathf.PI*2/32;m.AddVert(c+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),t,Vector2.zero);}for(int i=0;i<32;i++)m.AddTriangle(k,k+1+i,k+2+i);}
    private static void Capsule(VertexHelper m,Vector2 a,Vector2 b,float r,Color t)
    {
        Ellipse(m,a,r,r,t);Ellipse(m,b,r,r,t);Vector2 n=(b-a).normalized;n=new Vector2(-n.y,n.x)*r;int k=m.currentVertCount;
        m.AddVert(a+n,t,Vector2.zero);m.AddVert(b+n,t,Vector2.zero);m.AddVert(b-n,t,Vector2.zero);m.AddVert(a-n,t,Vector2.zero);m.AddTriangle(k,k+1,k+2);m.AddTriangle(k,k+2,k+3);
    }
}
// Serving plate seen from the front. Presentation only.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PlateGraphic : MaskableGraphic
{
    public bool Glow;
    protected override void OnPopulateMesh(VertexHelper m)
    {
        m.Clear();Rect r=rectTransform.rect;float rx=r.width*.5f,ry=r.height*.5f;
        if(Glow)SandokGraphic.Ellipse(m,Vector2.zero,rx,ry,new Color(1,.85f,.35f,.9f));
        SandokGraphic.Ellipse(m,new Vector2(0,-ry*.08f),rx*.94f,ry*.86f,new Color(.55f,.52f,.48f));
        SandokGraphic.Ellipse(m,Vector2.zero,rx*.92f,ry*.82f,new Color(.98f,.97f,.93f));
        SandokGraphic.Ellipse(m,new Vector2(0,ry*.05f),rx*.62f,ry*.50f,new Color(.90f,.88f,.83f));
    }
}

// Sandok drag: moving it over the cooking container stirs; releasing it on the plate scoops and serves.
public sealed class SandokTool : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Func<bool> CanUse;
    public Func<bool> Serving;
    public RectTransform Pot, Plate, GhostParent;
    public Action Stirred;
    public Action<bool> Released;
    public static event Action Grabbed;
    private RectTransform ghost;private int pointer=int.MinValue;private float lastStir;private Vector2 lastPoint;private float travel;
    public bool Dragging=>pointer!=int.MinValue;
    public void OnPointerDown(PointerEventData d){if(CanUse!=null&&CanUse())transform.localScale=Vector3.one*1.1f;}
    public void OnPointerUp(PointerEventData d){transform.localScale=Vector3.one;}
    public void OnBeginDrag(PointerEventData d)
    {
        if(pointer!=int.MinValue||CanUse==null||!CanUse())return;pointer=d.pointerId;travel=0;
        ghost=Instantiate((RectTransform)transform,GhostParent);ghost.name="SandokGhost";ghost.localScale=Vector3.one*1.15f;
        foreach(var g in ghost.GetComponentsInChildren<Graphic>())g.raycastTarget=false;foreach(var b in ghost.GetComponentsInChildren<MonoBehaviour>())if(b is SandokTool)Destroy(b);
        ghost.SetAsLastSibling();GetComponent<CanvasGroup>().alpha=.35f;Move(d);Grabbed?.Invoke();
    }
    public void OnDrag(PointerEventData d)
    {
        if(pointer!=d.pointerId)return;Move(d);
        bool overPot=!Serving()&&IngredientDrag.Inside(Pot,d.position,d.pressEventCamera,.15f);
        travel+=(d.position-lastPoint).magnitude;lastPoint=d.position;
        // Stirring needs real movement over the container, a little at a time.
        if(overPot&&travel>40&&Time.unscaledTime-lastStir>.35f){travel=0;lastStir=Time.unscaledTime;Stirred?.Invoke();}
        if(ghost)ghost.localRotation=Quaternion.Euler(0,0,overPot?Mathf.Sin(Time.unscaledTime*12)*18-30:-15);
    }
    private void Move(PointerEventData d){if(ghost&&RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ghost.parent,d.position,d.pressEventCamera,out Vector2 p))ghost.anchoredPosition=p+new Vector2(-40,40);if(pointer==int.MinValue)lastPoint=d.position;}
    public void OnEndDrag(PointerEventData d)
    {
        if(pointer!=d.pointerId)return;bool onPlate=Serving()&&IngredientDrag.Inside(Plate,d.position,d.pressEventCamera,.35f);Finish();Released?.Invoke(onPlate);
    }
    private void Finish(){pointer=int.MinValue;transform.localScale=Vector3.one;if(ghost)Destroy(ghost.gameObject);ghost=null;var g=GetComponent<CanvasGroup>();if(g)g.alpha=1;}
    public void Cancel(){if(pointer!=int.MinValue)Finish();}
    private void OnDisable(){Cancel();}
}

// First-time tips: one short card per new kind of action, with "Got it". Seen tips are remembered on the device.
// Guided practice always shows unseen tips; "Show tips again" in the pause menu or How to Play resets them.
public sealed class TutorialCoach
{
    private const string Prefix="SavorPinoy.Tip.";
    private static readonly string[] Keys={"gather","knife","wash","drag","cut","heat","add","stir","simmer","plate"};
    private readonly RectTransform card;private readonly TMP_Text title,body,count;private string showing;
    public bool Blocking=>card.gameObject.activeSelf;
    public static void ResetAll(){foreach(string k in Keys)PlayerPrefs.DeleteKey(Prefix+k);PlayerPrefs.Save();}
    public static bool Seen(string key)=>PlayerPrefs.GetInt(Prefix+key,0)==1;
    public TutorialCoach(RectTransform design,Func<string,Transform,string,float,float,float,float,UnityEngine.Events.UnityAction,Button> button)
    {
        card=PrototypeUI.Panel("TutorialShade",design,0,0,1800,1000,new Color(.08f,.04f,.015f,.55f)).rectTransform;
        PrototypeUI.Panel("TutorialCardEdge",card,0,40,1000,430,new Color(.93f,.74f,.38f)).raycastTarget=false;
        PrototypeUI.Panel("TutorialCard",card,0,40,984,414,new Color(1,.95f,.80f)).raycastTarget=false;
        count=PrototypeUI.Text("TutorialCount",card,"",0,215,900,40,22);count.color=new Color(.45f,.26f,.10f);count.fontStyle=FontStyles.Bold;
        title=PrototypeUI.Text("TutorialTitle",card,"",0,160,900,70,40);title.color=new Color(.16f,.07f,.02f);title.fontStyle=FontStyles.Bold;
        body=PrototypeUI.Text("TutorialBody",card,"",0,30,880,190,28);body.color=new Color(.16f,.07f,.02f);body.richText=true;
        button("TutorialGotIt",card,"GOT IT",0,-115,340,84,Dismiss);
        card.gameObject.SetActive(false);
    }
    public bool Offer(string key)
    {
        if(Blocking||Seen(key))return Blocking;int index=Array.IndexOf(Keys,key);if(index<0)return false;
        showing=key;count.text="BEGINNER TIP "+(index+1)+" OF "+Keys.Length;
        switch(key)
        {
            case "gather":title.text="1. GATHER";body.text="Look at the <b>Recipe steps</b> on the left.\nTap each ingredient the recipe needs. Watch out: some recipes include an item that does not belong!";break;
            case "knife":title.text="PICK UP THE KNIFE";body.text="When all ingredients are ready, tap the <b>knife</b> on the board to start preparing.";break;
            case "wash":title.text="WASH AT THE SINK";body.text="Drag the item into the <b>sink basin</b> and let go.\nWatch the hands: wash with soap and water for 20 seconds.";break;
            case "drag":title.text="DRAG AND DROP";body.text="Press and hold the item, slide it to the <b>dark target box</b>, then let go.\nFollow the moving hand if you get stuck.";break;
            case "cut":title.text="SLICE ALONG THE LINE";body.text="Put your finger on <b>START</b> and slide straight <b>down</b> the green line to <b>FINISH</b>.\nOne smooth stroke = one slice.";break;
            case "heat":title.text="3. COOK: TURN ON THE STOVE";body.text="Tap <b>Start cooking</b>. Keep the heat between <b>60 and 85</b>.\nTap <b>Adjust heat</b> and turn the knob if it gets too hot.";break;
            case "add":title.text="ADD IN ORDER";body.text="Drag the <b>bright, numbered</b> ingredient into the pot.\nThe Recipe steps panel shows what comes next.";break;
            case "stir":title.text="STIR WITH THE SANDOK";body.text="Hold the <b>sandok</b> and move it around over the pot to stir.\nThe saute timer only moves while you stir.";break;
            case "simmer":title.text="SIMMER";body.text="Wait while it cooks. Keep the heat in the 60-85 band.\nSome dishes must <b>not</b> be stirred now - check the steps.";break;
            case "plate":title.text="4. PLATE THE DISH";body.text="Turn the flame <b>OFF</b> with Adjust heat.\nThen drag the <b>sandok</b> onto the <b>plate</b> to serve.";break;
        }
        card.gameObject.SetActive(true);card.SetAsLastSibling();return true;
    }
    private void Dismiss(){if(showing!=null){PlayerPrefs.SetInt(Prefix+showing,1);PlayerPrefs.Save();}showing=null;card.gameObject.SetActive(false);}
    public void Hide(){card.gameObject.SetActive(false);showing=null;}
}
