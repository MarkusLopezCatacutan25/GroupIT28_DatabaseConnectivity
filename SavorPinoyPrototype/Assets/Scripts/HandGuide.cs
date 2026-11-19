using System;
using UnityEngine;
using UnityEngine.UI;

// Cartoon hand drawn as a mesh, so the guide needs no extra art. The rect's pivot is the index fingertip.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HandGraphic : MaskableGraphic
{
    public bool Pressed, Open;
    private static readonly Color Outline=new Color(.30f,.16f,.07f), Skin=new Color(.98f,.80f,.60f), Shade=new Color(.90f,.66f,.46f), Cuff=new Color(.16f,.45f,.70f);
    protected override void OnPopulateMesh(VertexHelper m)
    {
        m.Clear();
        for(int pass=0;pass<2;pass++)
        {
            float o=pass==0?4:0;Color c=pass==0?Outline:Skin,cs=pass==0?Outline:Shade;
            if(Open)
            {
                // Open palm (used for washing): four fingers up and the thumb out to the side.
                for(int f=0;f<4;f++){float x=-27+f*18;Capsule(m,new Vector2(x,-6-Mathf.Abs(f-1.5f)*6),new Vector2(x*.9f,-62),9+o,c);}
                Capsule(m,new Vector2(-46,-62),new Vector2(-30,-88),10+o,c);
            }
            else
            {
                Capsule(m,new Vector2(0,-10),new Vector2(0,-72),13+o,c);
                Disc(m,new Vector2(23,-82),12+o,cs);Disc(m,new Vector2(36,-91),12+o,cs);Disc(m,new Vector2(45,-104),11+o,cs);
                Capsule(m,new Vector2(-28,-100),new Vector2(-12,-80),11+o,c);
            }
            Ellipse(m,new Vector2(Open?-2:10,-104),37+o,33+o,c);
            Quad(m,(Open?-34:-20)-o,-152-o,(Open?30:40)+o,-128+o,pass==0?Outline:Cuff);
        }
        if(Pressed){Ring(m,Vector2.zero,24,5,new Color(1,.95f,.55f,.95f));Ring(m,Vector2.zero,38,3,new Color(1,.95f,.55f,.6f));}
    }
    private static void Disc(VertexHelper m,Vector2 c,float r,Color t)=>Ellipse(m,c,r,r,t);
    private static void Ellipse(VertexHelper m,Vector2 c,float rx,float ry,Color t)
    {int k=m.currentVertCount;m.AddVert(c,t,Vector2.zero);for(int i=0;i<=28;i++){float a=i*Mathf.PI*2/28;m.AddVert(c+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),t,Vector2.zero);}for(int i=0;i<28;i++)m.AddTriangle(k,k+1+i,k+2+i);}
    private static void Capsule(VertexHelper m,Vector2 a,Vector2 b,float r,Color t)
    {
        Disc(m,a,r,t);Disc(m,b,r,t);Vector2 n=(b-a).normalized;n=new Vector2(-n.y,n.x)*r;int k=m.currentVertCount;
        m.AddVert(a+n,t,Vector2.zero);m.AddVert(b+n,t,Vector2.zero);m.AddVert(b-n,t,Vector2.zero);m.AddVert(a-n,t,Vector2.zero);m.AddTriangle(k,k+1,k+2);m.AddTriangle(k,k+2,k+3);
    }
    private static void Quad(VertexHelper m,float x0,float y0,float x1,float y1,Color t)
    {int k=m.currentVertCount;m.AddVert(new Vector2(x0,y0),t,Vector2.zero);m.AddVert(new Vector2(x1,y0),t,Vector2.zero);m.AddVert(new Vector2(x1,y1),t,Vector2.zero);m.AddVert(new Vector2(x0,y1),t,Vector2.zero);m.AddTriangle(k,k+1,k+2);m.AddTriangle(k,k+2,k+3);}
    private static void Ring(VertexHelper m,Vector2 c,float r,float w,Color t)
    {
        for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32,b=(i+1)*Mathf.PI*2/32;int k=m.currentVertCount;
        m.AddVert(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,t,Vector2.zero);m.AddVert(c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r,t,Vector2.zero);
        m.AddVert(c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(r-w),t,Vector2.zero);m.AddVert(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(r-w),t,Vector2.zero);m.AddTriangle(k,k+1,k+2);m.AddTriangle(k,k+2,k+3);}
    }
}

// Animated demonstration hand: shows the gesture the player should make (tap, drag, slice, rub),
// waits while the player is touching the screen, and comes back after a short idle pause.
public sealed class HandGuide : MonoBehaviour
{
    public enum Motion { None, Tap, Drag, Rub, Circle }
    private RectTransform rect;private HandGraphic hand;private CanvasGroup fade;
    private Motion motion;private Vector2 from,to;private float time,idle;
    public float IdleDelay=1.2f;
    public bool Visible=>motion!=Motion.None&&fade.alpha>.01f;
    public static HandGuide Create(string name,Transform parent)
    {
        RectTransform r=PrototypeUI.Rect(name,parent,0,0,130,160);r.pivot=new Vector2(.5f,1);
        var guide=r.gameObject.AddComponent<HandGuide>();guide.rect=r;
        var g=PrototypeUI.Rect("Hand",r,0,0,130,160);g.pivot=new Vector2(.5f,1);g.anchorMin=g.anchorMax=new Vector2(.5f,1);g.anchoredPosition=Vector2.zero;
        guide.hand=g.gameObject.AddComponent<HandGraphic>();guide.hand.raycastTarget=false;
        guide.fade=r.gameObject.AddComponent<CanvasGroup>();guide.fade.blocksRaycasts=false;guide.fade.interactable=false;guide.fade.alpha=0;return guide;
    }
    public void Show(Motion m,Vector2 a,Vector2 b=default(Vector2))
    {
        if(m==motion&&a==from&&b==to)return;motion=m;from=a;to=m==Motion.Drag?b:a;time=0;hand.Open=m==Motion.Rub;hand.SetVerticesDirty();transform.SetAsLastSibling();
    }
    public void Hide(){motion=Motion.None;}
    private static bool Touching()
    {
        var p=UnityEngine.InputSystem.Pointer.current;return p!=null&&p.press.isPressed;
    }
    private void Update()
    {
        float dt=Time.unscaledDeltaTime;
        if(Touching())idle=0;else idle+=dt;
        bool show=motion!=Motion.None&&idle>=IdleDelay;
        fade.alpha=Mathf.MoveTowards(fade.alpha,show?1:0,dt*(show?3:6));
        if(!show){time=0;return;}
        time+=dt;bool pressed=false;Vector2 p=from;float angle=0;
        switch(motion)
        {
            case Motion.Tap:{float t=Mathf.Repeat(time,1.2f);pressed=t>.35f&&t<.6f;p=from+new Vector2(18,-18)*(pressed?0:Mathf.Clamp01(1-Mathf.Abs(t-.47f)*3));break;}
            case Motion.Drag:
            {
                // Press at the start, slide to the end, lift and fade back to the start.
                float t=Mathf.Repeat(time,2.4f);pressed=t>.35f&&t<1.75f;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.5f)/1.15f));p=Vector2.Lerp(from,to,k);
                fade.alpha=Mathf.Min(fade.alpha,t>2.1f?Mathf.Clamp01((2.4f-t)/.3f):t<.2f?t/.2f:1);break;
            }
            case Motion.Circle:{pressed=true;float a=time*5;p=from+new Vector2(Mathf.Cos(a)*70,Mathf.Sin(a)*28);break;}
            case Motion.Rub:{p=from+new Vector2(Mathf.Sin(time*9)*34,Mathf.Cos(time*18)*6);angle=Mathf.Sin(time*9)*12;break;}
        }
        rect.anchoredPosition=p;rect.localRotation=Quaternion.Euler(0,0,angle);
        if(hand.Pressed!=pressed){hand.Pressed=pressed;hand.SetVerticesDirty();}
        hand.rectTransform.localScale=Vector3.one*(pressed?.92f:1);
    }
}
