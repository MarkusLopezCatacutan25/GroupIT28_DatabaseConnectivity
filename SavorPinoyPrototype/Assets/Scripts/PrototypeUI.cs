using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class PrototypeUI
{
    public static TMP_FontAsset Font;
    public static readonly Color Ink=new Color(.18f,.10f,.06f);
    public static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {
        RectTransform r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;
    }
    public static Image Panel(string name,Transform parent,float x,float y,float w,float h,Color color)
    {RectTransform r=Rect(name,parent,x,y,w,h);Image image=r.gameObject.AddComponent<Image>();image.color=color;return image;}
    public static TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,int size=30)
    {
        RectTransform r=Rect(name,parent,x,y,w,h);TextMeshProUGUI t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=Font ? Font : TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=size;t.color=Ink;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.richText=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
    }
    public static Button Button(string name,Transform parent,string label,float x,float y,float w,float h,UnityAction onClick)
    {
        Image image=Panel(name,parent,x,y,w,h,new Color(.88f,.65f,.32f));Button b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;
        ColorBlock colors=b.colors;colors.highlightedColor=new Color(1,.92f,.75f);colors.pressedColor=new Color(.7f,.53f,.3f);b.colors=colors;
        Text(name+"Label",b.transform,label,0,0,w-12,h-6,28);b.onClick.AddListener(onClick);return b;
    }
    public static TMP_InputField Input(string name,Transform parent,float x,float y,float w,float h)
    {
        Image image=Panel(name,parent,x,y,w,h,new Color(.98f,.95f,.86f));TMP_InputField input=image.gameObject.AddComponent<TMP_InputField>();
        RectTransform area=Rect("TextArea",input.transform,0,0,w-24,h-10);
        TMP_Text value=Text("Text",area,"",0,0,w-24,h-10,28);value.alignment=TextAlignmentOptions.Left;value.richText=false;
        TMP_Text placeholder=Text("Placeholder",area,"Chef alias",0,0,w-24,h-10,28);placeholder.color=new Color(.5f,.4f,.3f);placeholder.alignment=TextAlignmentOptions.Left;
        input.textViewport=area;input.textComponent=(TextMeshProUGUI)value;input.placeholder=placeholder;input.characterLimit=24;input.lineType=TMP_InputField.LineType.SingleLine;return input;
    }
    public static Slider HeatSlider(Transform parent,float x,float y,float w)
    {
        RectTransform r=Rect("FlameSlider",parent,x,y,w,65);Slider slider=r.gameObject.AddComponent<Slider>();
        Image track=Panel("Track",r,0,0,w,24,new Color(.28f,.15f,.07f));track.raycastTarget=false;
        RectTransform handleArea=Rect("HandleArea",r,0,0,w-36,65);
        Image handle=Panel("FlameHandle",handleArea,0,0,52,60,new Color(.96f,.50f,.12f));slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;slider.value=0;return slider;
    }
}
