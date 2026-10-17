using UnityEngine;

// Applies only to the Main Menu's centered content. Its background stays full bleed.
public sealed class PrototypeSafeArea : MonoBehaviour
{
    private RectTransform content;
    private Rect area;
    private Vector2Int dimensions;
    private void Start()
    {
        content=new GameObject("SafeMenuContent",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(transform,false);
        foreach(string name in new[]{"Logo","PlayButton","OptionsButton","ExitButton"}){Transform child=transform.Find(name);if(child)child.SetParent(content,false);}
        Apply();
    }
    private void Update(){if(area!=Screen.safeArea||dimensions!=new Vector2Int(Screen.width,Screen.height))Apply();}
    private void Apply()
    {
        if(!content||Screen.width<=0||Screen.height<=0)return;area=Screen.safeArea;dimensions=new Vector2Int(Screen.width,Screen.height);
        content.anchorMin=new Vector2(area.x/Screen.width,area.y/Screen.height);content.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);content.offsetMin=content.offsetMax=Vector2.zero;
        Canvas.ForceUpdateCanvases();float scale=Mathf.Min(1,(content.rect.width-80)/1120,(content.rect.height-60)/940);
        foreach(RectTransform child in content)
        {
            child.localScale=Vector3.one*Mathf.Max(.1f,scale);
            float y=child.name=="Logo"?235:child.name=="PlayButton"?-65:child.name=="OptionsButton"?-205:-345;
            child.anchoredPosition=new Vector2(0,y*scale);
        }
    }
}
