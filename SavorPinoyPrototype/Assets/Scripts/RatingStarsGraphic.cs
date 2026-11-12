using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class RatingStarsGraphic : MaskableGraphic
{
    private int stars;
    public void SetStars(int value){stars=Mathf.Clamp(value,0,3);SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();for(int i=0;i<3;i++){Vector2 center=new Vector2((i-1)*100,0);Star(mesh,center,42,new Color(.36f,.20f,.06f));Star(mesh,center,35,i<stars?new Color(1,.71f,.18f):new Color(.79f,.70f,.51f));}
    }
    private static void Star(VertexHelper mesh,Vector2 center,float radius,Color tint)
    {
        int k=mesh.currentVertCount;mesh.AddVert(center,tint,Vector2.zero);
        for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad,r=i%2==0?radius:radius*.46f;mesh.AddVert(center+new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r),tint,Vector2.zero);}
        for(int i=0;i<10;i++)mesh.AddTriangle(k,k+1+i,k+1+(i+1)%10);
    }
}
