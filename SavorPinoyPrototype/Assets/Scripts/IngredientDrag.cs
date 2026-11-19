using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One touch/mouse drag, with no click-to-add fallback. The owner validates the drop.
// Pilot feedback: students lost the item under their finger and missed small targets, so the dragged
// copy floats above the fingertip, the item grows when grabbed, and drops count a little outside the target.
public sealed class IngredientDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Func<bool> CanDrag;
    public Func<RectTransform> Begin;
    public Action<bool,Vector2> End;
    public RectTransform Target;
    public float Margin=.35f;
    public Vector2 Lift=new Vector2(0,70);
    public static event Action<IngredientDrag> Grabbed;
    private RectTransform ghost;
    private int pointer=int.MinValue;
    public void OnPointerDown(PointerEventData data){if(CanDrag!=null&&CanDrag())transform.localScale=Vector3.one*1.1f;}
    public void OnPointerUp(PointerEventData data){transform.localScale=Vector3.one;}
    public void OnBeginDrag(PointerEventData data)
    {
        if(pointer!=int.MinValue||CanDrag==null||!CanDrag())return;
        ghost=Begin?.Invoke();if(!ghost)return;pointer=data.pointerId;Move(data);Grabbed?.Invoke(this);
    }
    public void OnDrag(PointerEventData data){if(pointer==data.pointerId)Move(data);}
    private void Move(PointerEventData data){if(ghost&&RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ghost.parent,data.position,data.pressEventCamera,out Vector2 point))ghost.anchoredPosition=point+Lift;}
    public void OnEndDrag(PointerEventData data)
    {
        if(pointer!=data.pointerId)return;transform.localScale=Vector3.one;
        bool accepted=CanDrag()!=false&&Inside(Target,data.position,data.pressEventCamera,Margin);
        Vector2 point=ghost?ghost.anchoredPosition:Vector2.zero;pointer=int.MinValue;ghost=null;End?.Invoke(accepted,point);
    }
    // Screen-space hit test on the target grown by Margin of its size on every side.
    public static bool Inside(RectTransform target,Vector2 screen,Camera camera,float margin)
    {
        if(!target)return false;Vector3[] c=new Vector3[4];target.GetWorldCorners(c);
        Vector2 a=RectTransformUtility.WorldToScreenPoint(camera,c[0]),b=RectTransformUtility.WorldToScreenPoint(camera,c[2]);
        Rect r=Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
        float mx=r.width*margin,my=r.height*margin;r.xMin-=mx;r.xMax+=mx;r.yMin-=my;r.yMax+=my;return r.Contains(screen);
    }
    public void Cancel(){transform.localScale=Vector3.one;if(pointer==int.MinValue)return;pointer=int.MinValue;ghost=null;End?.Invoke(false,Vector2.zero);}
    private void OnDisable(){Cancel();}
    private void OnApplicationPause(bool value){if(value)Cancel();}
}
