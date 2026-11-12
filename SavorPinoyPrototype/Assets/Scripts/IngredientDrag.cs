using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One touch/mouse drag, with no click-to-add fallback. The owner validates the drop.
public sealed class IngredientDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Func<bool> CanDrag;
    public Func<RectTransform> Begin;
    public Action<bool,Vector2> End;
    public RectTransform Target;
    private RectTransform ghost;
    private int pointer=int.MinValue;
    public void OnBeginDrag(PointerEventData data)
    {
        if(pointer!=int.MinValue||CanDrag==null||!CanDrag())return;
        ghost=Begin?.Invoke();if(!ghost)return;pointer=data.pointerId;Move(data);
    }
    public void OnDrag(PointerEventData data){if(pointer==data.pointerId)Move(data);}
    private void Move(PointerEventData data){if(ghost&&RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ghost.parent,data.position,data.pressEventCamera,out Vector2 point))ghost.anchoredPosition=point;}
    public void OnEndDrag(PointerEventData data)
    {
        if(pointer!=data.pointerId)return;
        bool accepted=CanDrag()!=false&&Target&&RectTransformUtility.RectangleContainsScreenPoint(Target,data.position,data.pressEventCamera);
        Vector2 point=ghost?ghost.anchoredPosition:Vector2.zero;pointer=int.MinValue;ghost=null;End?.Invoke(accepted,point);
    }
    public void Cancel(){if(pointer==int.MinValue)return;pointer=int.MinValue;ghost=null;End?.Invoke(false,Vector2.zero);}
    private void OnDisable(){Cancel();}
    private void OnApplicationPause(bool value){if(value)Cancel();}
}
