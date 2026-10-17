using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ChoppingSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Func<bool> CanCut;
    public Func<int> AcceptedCuts;
    public Action<double> Completed;
    public Action<Vector2> StrokeMoved;
    public Action<bool> StrokeActive;
    private readonly List<CutPoint> points=new List<CutPoint>();
    private int pointer=int.MinValue;
    private RectTransform rect;
    private void Awake(){rect=transform as RectTransform;}
    public void OnPointerDown(PointerEventData data)
    {
        if(pointer!=int.MinValue || CanCut==null || !CanCut())return;
        pointer=data.pointerId;points.Clear();StrokeActive?.Invoke(true);AddPoint(data);
    }
    public void OnDrag(PointerEventData data){if(data.pointerId==pointer && CanCut!=null && CanCut())AddPoint(data);}
    public void OnPointerUp(PointerEventData data)
    {
        if(data.pointerId!=pointer)return;
        if(CanCut!=null && CanCut()){AddPoint(data);Completed?.Invoke(CutStrokeEvaluator.Evaluate(points,AcceptedCuts()));}
        CancelStroke();
    }
    private void AddPoint(PointerEventData data)
    {
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position,data.pressEventCamera,out Vector2 p))
        {
            StrokeMoved?.Invoke(p);
            CutPoint next=new CutPoint((p.x-rect.rect.xMin)/rect.rect.width,(p.y-rect.rect.yMin)/rect.rect.height);
            if(points.Count==0 || Math.Abs(next.X-points[points.Count-1].X)+Math.Abs(next.Y-points[points.Count-1].Y)>.003)points.Add(next);
        }
    }
    public void CancelStroke(){pointer=int.MinValue;points.Clear();StrokeActive?.Invoke(false);}
    private void OnDisable(){CancelStroke();}
    private void OnApplicationPause(bool value){if(value)CancelStroke();}
}
