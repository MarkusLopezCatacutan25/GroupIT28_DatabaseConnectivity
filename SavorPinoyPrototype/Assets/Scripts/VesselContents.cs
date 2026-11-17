using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Draws everything added to the vessel so far: a liquid pool, the solid ingredients together, and seasoning specks.
// Presentation only; the order and the grading stay in CookingAttempt.
public sealed class VesselContents
{
    private readonly RectTransform root;
    private readonly LiquidPoolGraphic pool;
    private readonly SpeckGraphic specks;
    private readonly List<Image> solids=new List<Image>();
    private readonly KitchenArt art;
    private int shown=-1;
    private static readonly Vector2[] Slots={new Vector2(-.22f,.02f),new Vector2(.20f,.05f),new Vector2(0,.12f),new Vector2(-.05f,-.10f),new Vector2(.28f,-.08f),new Vector2(-.30f,-.06f)};
    public RectTransform Root=>root;
    public VesselContents(Transform parent,string name,KitchenArt art,float x,float y,float w,float h)
    {
        this.art=art;root=PrototypeUI.Rect(name,parent,x,y,w,h);
        pool=PrototypeUI.Rect("LiquidPool",root,0,0,w,h).gameObject.AddComponent<LiquidPoolGraphic>();pool.raycastTarget=false;
        StretchToRoot(pool.rectTransform);
        specks=PrototypeUI.Rect("Seasoning",root,0,0,w,h).gameObject.AddComponent<SpeckGraphic>();specks.raycastTarget=false;StretchToRoot(specks.rectTransform);
    }
    private static void StretchToRoot(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    public void Place(float x,float y,float w,float h){root.anchoredPosition=new Vector2(x,y);root.sizeDelta=new Vector2(w,h);shown=-1;}
    public static bool IsLiquid(string id)=>id=="oil"||id=="broth"||id=="vinegar"||id=="tomato_sauce"||id=="soy";
    public static bool IsSeasoning(string id)=>id=="salt"||id=="pepper"||id=="msg"||id=="egg";
    public static Color LiquidColor(string id)
    {
        switch(id){case "oil":return new Color(.96f,.80f,.30f,.55f);case "broth":return new Color(.62f,.40f,.18f,.90f);case "vinegar":return new Color(.93f,.86f,.58f,.75f);
            case "tomato_sauce":return new Color(.78f,.20f,.12f,.95f);default:return new Color(.30f,.15f,.07f,.92f);}
    }
    public void Render(CookingAttempt attempt)
    {
        root.gameObject.SetActive(attempt.AdditionIndex>0);
        Color tint=CookingVesselPresentation.FoodTint(attempt);
        if(shown!=attempt.AdditionIndex)
        {
            shown=attempt.AdditionIndex;string liquid=null;var seasonings=new List<string>();var items=new List<string>();
            for(int i=0;i<attempt.AdditionIndex&&i<attempt.Recipe.Additions.Length;i++)
            {
                string id=attempt.Recipe.Additions[i];
                if(IsLiquid(id)){if(liquid==null||id!="oil")liquid=id;}else if(IsSeasoning(id))seasonings.Add(id);else items.Add(id);
            }
            pool.gameObject.SetActive(liquid!=null);if(liquid!=null){pool.color=LiquidColor(liquid);pool.SetVerticesDirty();}
            specks.Kinds=seasonings.ToArray();specks.SetVerticesDirty();
            float w=root.sizeDelta.x,h=root.sizeDelta.y;
            while(solids.Count<items.Count){var img=art.Draw("Solid"+solids.Count,root,null,0,0,10,10);img.raycastTarget=false;solids.Add(img);}
            for(int i=0;i<solids.Count;i++)
            {
                bool on=i<items.Count;solids[i].gameObject.SetActive(on);if(!on)continue;
                Vector2 slot=Slots[i%Slots.Length];solids[i].sprite=art.IngredientSprite(items[i],true);
                // Newer items sit on top and slightly larger, like food piled in a pot.
                float size=Mathf.Lerp(.42f,.55f,items.Count<=1?1:i/(float)(items.Count-1));
                solids[i].rectTransform.anchoredPosition=new Vector2(slot.x*w,slot.y*h);solids[i].rectTransform.sizeDelta=new Vector2(w*size,h*size*1.4f);
                solids[i].transform.SetSiblingIndex(1+i);
            }
            specks.transform.SetAsLastSibling();
        }
        foreach(var img in solids)img.color=tint;
    }
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class LiquidPoolGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper m)
    {
        m.Clear();Rect r=rectTransform.rect;Vector2 c=r.center;float rx=r.width*.46f,ry=r.height*.38f;int k=m.currentVertCount;
        Color edge=new Color(color.r*.8f,color.g*.8f,color.b*.8f,color.a);m.AddVert(c,color,Vector2.zero);
        for(int i=0;i<=48;i++){float a=i*Mathf.PI*2/48;m.AddVert(c+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),edge,Vector2.zero);}
        for(int i=0;i<48;i++)m.AddTriangle(k,k+1+i,k+2+i);
    }
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SpeckGraphic : MaskableGraphic
{
    public string[] Kinds=new string[0];
    protected override void OnPopulateMesh(VertexHelper m)
    {
        m.Clear();Rect r=rectTransform.rect;var rng=new System.Random(7);
        foreach(string kind in Kinds)
        {
            bool egg=kind=="egg";Color c=egg?new Color(1,.86f,.32f):kind=="pepper"?new Color(.12f,.09f,.07f):new Color(1,1,1,.95f);
            for(int i=0;i<(egg?16:34);i++)
            {
                float a=(float)rng.NextDouble()*Mathf.PI*2,d=Mathf.Sqrt((float)rng.NextDouble());
                Vector2 p=r.center+new Vector2(Mathf.Cos(a)*r.width*.40f*d,Mathf.Sin(a)*r.height*.32f*d);float s=egg?r.width*.025f:kind=="pepper"?2.6f:2.2f;int k=m.currentVertCount;
                m.AddVert(p+new Vector2(-s,-s),c,Vector2.zero);m.AddVert(p+new Vector2(s,-s),c,Vector2.zero);m.AddVert(p+new Vector2(s,s),c,Vector2.zero);m.AddVert(p+new Vector2(-s,s),c,Vector2.zero);
                m.AddTriangle(k,k+1,k+2);m.AddTriangle(k,k+2,k+3);
            }
        }
    }
}
