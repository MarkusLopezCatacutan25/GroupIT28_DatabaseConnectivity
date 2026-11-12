using UnityEngine;
using UnityEngine.UI;

// Code-native animated travel overlay, independent of the painted map texture.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class TravelCloudGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();Vector2 size=rectTransform.rect.size;
        for(int row=0;row<4;row++)for(int col=0;col<9;col++)
        {
            Vector2 center=new Vector2((col-4)*size.x/7f+(row%2)*70,(row-1.5f)*size.y/3f);
            float rx=size.x/6f,ry=size.y/3f;
            for(int j=0;j<40;j++){
                float a=j*Mathf.PI*2/40,b=(j+1)*Mathf.PI*2/40;int k=mesh.currentVertCount;
                Color tint=Color.Lerp(new Color(.83f,.83f,.74f),new Color(1,.97f,.85f),(float)row/3);
                mesh.AddVert(center,tint,Vector2.zero);mesh.AddVert(center+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),tint,Vector2.zero);mesh.AddVert(center+new Vector2(Mathf.Cos(b)*rx,Mathf.Sin(b)*ry),tint,Vector2.zero);mesh.AddTriangle(k,k+1,k+2);
            }
        }
    }
}

// Native parchment-and-gold card, following the existing wood/paper UI palette.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RecipeCardFrameGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();Vector2 size=rectTransform.rect.size;
        Rounded(mesh,size,38,new Color(.30f,.13f,.045f));
        Rounded(mesh,size-new Vector2(12,12),34,new Color(.95f,.68f,.30f));
        Rounded(mesh,size-new Vector2(34,34),26,new Color(.64f,.32f,.10f));
        Rounded(mesh,size-new Vector2(48,48),22,new Color(.98f,.92f,.76f));
    }
    private static void Rounded(VertexHelper m,Vector2 size,float radius,Color tint)
    {
        int start=m.currentVertCount;m.AddVert(Vector2.zero,tint,Vector2.zero);
        for(int corner=0;corner<4;corner++)for(int step=0;step<=10;step++){
            float a=(corner*90+step*9)*Mathf.Deg2Rad;
            Vector2 c=new Vector2(corner==0||corner==3?size.x*.5f-radius:-size.x*.5f+radius,corner<2?size.y*.5f-radius:-size.y*.5f+radius);
            m.AddVert(c+new Vector2(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius),tint,Vector2.zero);
        }
        for(int i=0;i<44;i++)m.AddTriangle(start,start+1+i,start+1+(i+1)%44);
    }
}
