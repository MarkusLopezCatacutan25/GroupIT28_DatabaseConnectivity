using UnityEngine;
using UnityEngine.UI;

// Native mesh overlays keep highlights and chopped pieces independent of painted backgrounds.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class KitchenCueGraphic : MaskableGraphic
{
    public bool Ring=true, Turmeric, Dial, Steam;
    public int Pieces;
    private void Update(){if(Steam)SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if(Dial)
        {
            Ellipse(mesh,Vector2.zero,19,19,0,new Color(.18f,.08f,.025f));
            Ellipse(mesh,Vector2.zero,15,15,0,new Color(1,.71f,.24f));return;
        }
        if(Steam)
        {
            for(int i=0;i<6;i++)
            {
                float rise=(Time.unscaledTime*24+i*13)%80;
                Ellipse(mesh,new Vector2((i%3-1)*24+Mathf.Sin(rise*.05f+i)*8,rise-40),10+rise*.08f,14+rise*.05f,0,new Color(1,.97f,.88f,.38f*(1-rise/80)));
            }
            return;
        }
        if(Ring) Ellipse(mesh,Vector2.zero,rectTransform.rect.width*.48f,rectTransform.rect.height*.48f,6,color);
        else for(int i=0;i<Pieces;i++)
        {
            Vector2 center=new Vector2((i%4-1.5f)*35,(i/4-.5f)*29);
            Ellipse(mesh,center,Turmeric?15:18,Turmeric?12:8,0,Turmeric?new Color(1,.60f,.12f):new Color(1,.91f,.69f));
        }
    }
    private static void Ellipse(VertexHelper m,Vector2 c,float rx,float ry,float thickness,Color tint)
    {
        const int n=48;
        for(int i=0;i<n;i++)
        {
            float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;
            Vector2 p=c+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),q=c+new Vector2(Mathf.Cos(b)*rx,Mathf.Sin(b)*ry);
            Vector2 r=thickness>0?c+new Vector2(Mathf.Cos(b)*(rx-thickness),Mathf.Sin(b)*(ry-thickness)):c;
            Vector2 s=thickness>0?c+new Vector2(Mathf.Cos(a)*(rx-thickness),Mathf.Sin(a)*(ry-thickness)):c;
            int k=m.currentVertCount;m.AddVert(p,tint,Vector2.zero);m.AddVert(q,tint,Vector2.zero);m.AddVert(r,tint,Vector2.zero);m.AddVert(s,tint,Vector2.zero);
            m.AddTriangle(k,k+1,k+2);m.AddTriangle(k,k+2,k+3);
        }
    }
}
