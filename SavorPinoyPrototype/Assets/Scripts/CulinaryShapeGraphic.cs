using UnityEngine;
using UnityEngine.UI;

// Temporary code-drawn cookware/dish shapes until the team's kitchen sprites are supplied.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CulinaryShapeGraphic : MaskableGraphic
{
    public bool platedDish;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();float w=rectTransform.rect.width,h=rectTransform.rect.height;
        if(platedDish)
        {
            Ellipse(mesh,0,0,w*.48f,h*.46f,new Color(.94f,.93f,.83f));
            Ellipse(mesh,0,0,w*.39f,h*.35f,new Color(.36f,.20f,.08f));
            for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;Ellipse(mesh,Mathf.Cos(a)*w*.20f,Mathf.Sin(a)*h*.14f,w*.09f,h*.09f,new Color(.83f,.56f,.15f));}
            Ellipse(mesh,0,0,w*.07f,h*.15f,new Color(.35f,.48f,.18f));
        }
        else
        {
            Ellipse(mesh,-w*.40f,h*.13f,w*.10f,h*.14f,new Color(.38f,.18f,.08f));
            Ellipse(mesh,w*.40f,h*.13f,w*.10f,h*.14f,new Color(.38f,.18f,.08f));
            Ellipse(mesh,0,-h*.02f,w*.40f,h*.43f,color);
            Ellipse(mesh,0,h*.30f,w*.42f,h*.12f,new Color(.32f,.13f,.05f));
        }
    }
    private void Ellipse(VertexHelper mesh,float x,float y,float rx,float ry,Color tint)
    {
        int start=mesh.currentVertCount;UIVertex vertex=UIVertex.simpleVert;vertex.color=tint;vertex.position=new Vector3(x,y);mesh.AddVert(vertex);
        for(int i=0;i<=40;i++){float a=i*Mathf.PI*2/40;vertex.position=new Vector3(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry);mesh.AddVert(vertex);}
        for(int i=0;i<40;i++)mesh.AddTriangle(start,start+i+1,start+i+2);
    }
}
