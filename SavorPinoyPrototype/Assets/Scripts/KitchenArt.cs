using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class KitchenArt : MonoBehaviour
{
    [SerializeField] private Texture2D kitchenSheet, ingredientSheet;
    [SerializeField] private Sprite turmeric;
    [SerializeField] private Sprite environment, closeup, knife, openPot, garlicClean;
    public Sprite OpenPot => openPot;
    public Sprite Environment => environment;
    public Sprite Closeup => closeup;
    public Sprite Knife => knife;
    private readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
    private readonly List<Sprite> slices=new List<Sprite>();
    public Sprite Turmeric => turmeric;
    public Sprite Slice(string key)
    {
        if(key=="garlic"&&garlicClean)return garlicClean;
        if(cache.TryGetValue(key,out Sprite cached))return cached;
        Texture2D sheet=ingredientSheet;Rect r;
        // Source rectangles use top-left coordinates; Unity Sprite.Create uses bottom-left.
        switch(key)
        {
            case "room":sheet=kitchenSheet;r=new Rect(12,0,817,421);break;
            case "garlic":r=new Rect(427,128,257,262);break;
            case "soy":r=new Rect(971,80,156,293);break;
            case "chicken":r=new Rect(587,417,383,267);break;
            case "board":r=new Rect(54,419,507,248);break;
            case "pot":r=new Rect(971,367,504,343);break;
            case "burner":r=new Rect(47,696,536,321);break;
            case "dish":r=new Rect(633,733,451,292);break;
            case "card":r=new Rect(1178,704,292,318);break;
            default:return null;
        }
        if(!sheet)return null;r.y=sheet.height-r.y-r.height;
        Sprite sprite=Sprite.Create(sheet,r,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);        slices.Add(sprite);cache[key]=sprite;return sprite;
    }
    public Image Draw(string name,Transform parent,Sprite sprite,float x,float y,float w,float h,bool aspect=true)
    {
        Image image=PrototypeUI.Panel(name,parent,x,y,w,h,Color.white);image.sprite=sprite;image.preserveAspect=aspect;image.useSpriteMesh=true;image.raycastTarget=false;return image;
    }
    private void OnDestroy(){foreach(Sprite sprite in slices)if(sprite)Destroy(sprite);}
}
