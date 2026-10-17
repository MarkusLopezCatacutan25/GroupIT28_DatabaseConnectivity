using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class KnifeGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();float w=rectTransform.rect.width,h=rectTransform.rect.height;
        mesh.AddUIVertexQuad(Quad(-w*.48f,-h*.15f,-w*.05f,h*.15f,new Color(.35f,.18f,.08f)));
        mesh.AddUIVertexQuad(Quad(-w*.04f,-h*.2f,w*.45f,h*.2f,new Color(.83f,.88f,.88f)));
    }
    private UIVertex[] Quad(float x1,float y1,float x2,float y2,Color tint)
    {
        var v=new UIVertex[4];Vector2[] p={new Vector2(x1,y1),new Vector2(x1,y2),new Vector2(x2,y2),new Vector2(x2,y1)};
        for(int i=0;i<4;i++){v[i]=UIVertex.simpleVert;v[i].position=p[i];v[i].color=tint;}return v;
    }
}
public sealed class KnifeTool : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform Board;public Action Selected;public Func<bool> CanSelect;
    private Vector2 home;private RectTransform rect;private int pointer=int.MinValue;
    public void OnBeginDrag(PointerEventData data){if(CanSelect==null||!CanSelect()||pointer!=int.MinValue)return;rect=(RectTransform)transform;home=rect.anchoredPosition;pointer=data.pointerId;}
    public void OnDrag(PointerEventData data){if(pointer!=data.pointerId)return;if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,data.position,data.pressEventCamera,out Vector2 p))rect.anchoredPosition=p;}
    public void OnEndDrag(PointerEventData data){if(pointer!=data.pointerId)return;if(CanSelect()!=false && RectTransformUtility.RectangleContainsScreenPoint(Board,data.position,data.pressEventCamera))Selected?.Invoke();Cancel();}
    private void Cancel(){if(pointer==int.MinValue)return;pointer=int.MinValue;if(rect)rect.anchoredPosition=home;}
    private void OnDisable(){Cancel();}
    private void OnApplicationPause(bool value){if(value)Cancel();}
}
