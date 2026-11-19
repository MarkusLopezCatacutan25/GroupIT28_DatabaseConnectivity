using UnityEngine;
using UnityEngine.UI;

// Keeps a fixed-size panel fully visible inside the device's safe area on any screen size or aspect ratio
// (16:9 laptops, 20:9 phones, 4:3 tablets, notches). It shrinks the panel; it never stretches it.
public sealed class ScreenFit : MonoBehaviour
{
    public Vector2 Design=new Vector2(1920,1080);
    public float Margin=16;
    private Rect lastSafe;private Vector2Int lastSize;
    public static ScreenFit Attach(RectTransform panel,float width,float height,bool backdrop=false)
    {
        var fit=panel.gameObject.AddComponent<ScreenFit>();fit.Design=new Vector2(width,height);
        if(backdrop)
        {
            // An oversized backdrop child covers the side bars left when the panel is shrunk.
            Image source=panel.GetComponent<Image>();var back=PrototypeUI.Panel("FitBackdrop",panel,0,0,width*6,height*6,source?source.color:new Color(0,0,0,.9f));back.transform.SetAsFirstSibling();
        }
        fit.Apply();return fit;
    }
    private void Update(){if(lastSafe!=Screen.safeArea||lastSize!=new Vector2Int(Screen.width,Screen.height))Apply();}
    private void Apply()
    {
        var rect=(RectTransform)transform;var canvas=rect.GetComponentInParent<Canvas>();if(!canvas||Screen.width<=0||Screen.height<=0)return;
        canvas=canvas.rootCanvas;var root=(RectTransform)canvas.transform;lastSafe=Screen.safeArea;lastSize=new Vector2Int(Screen.width,Screen.height);
        Canvas.ForceUpdateCanvases();
        float w=root.rect.width*lastSafe.width/Screen.width,h=root.rect.height*lastSafe.height/Screen.height;
        float scale=Mathf.Min(1,(w-Margin*2)/Design.x,(h-Margin*2)/Design.y);
        // Fill stretched rects keep their size; only a fixed-size panel is scaled.
        if(rect.anchorMin==rect.anchorMax){rect.sizeDelta=Design;rect.localScale=Vector3.one*Mathf.Max(.2f,scale);}
        Vector2 centre=new Vector2((lastSafe.center.x/Screen.width-.5f)*root.rect.width,(lastSafe.center.y/Screen.height-.5f)*root.rect.height);
        if(rect.anchorMin==rect.anchorMax&&rect.anchorMin==new Vector2(.5f,.5f))rect.anchoredPosition=centre;
    }
}
