using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class KitchenArt : MonoBehaviour
{
    [SerializeField] private Texture2D kitchenSheet, ingredientSheet;
    [SerializeField] private Sprite turmeric;
    [SerializeField] private Sprite environment, closeup, knife, openPot, garlicClean;
    [SerializeField] private Sprite preparedGarlic, preparedTurmeric, platedAdobo;
    public Sprite OpenPot => openPot;
    public Sprite Environment => environment;
    public Sprite Closeup => closeup;
    public Sprite Knife => knife;
    public Sprite PreparedGarlic => preparedGarlic;
    public Sprite PreparedTurmeric => preparedTurmeric;
    public Sprite PlatedDish => platedAdobo ? platedAdobo : Slice("dish");
    private readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
    private readonly List<Sprite> slices=new List<Sprite>();
    public Sprite Turmeric => turmeric;
    public Sprite EmptyPalayok
    {
        get{
            if(cache.TryGetValue("empty_palayok",out Sprite hit))return hit;
            var texture=Resources.Load<Texture2D>("Art/EmptyPalayokV3");if(!texture)return OpenPot;
            var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);cache["empty_palayok"]=sprite;slices.Add(sprite);return sprite;
        }
    }
    public Sprite Detail(int cell)
    {
        string key="detail_"+cell;if(cache.TryGetValue(key,out Sprite hit))return hit;
        var texture=Resources.Load<Texture2D>("Art/PreparationDetailsV1");if(!texture)return null;
        float w=texture.width/2f,h=texture.height/2f;
        var sprite=Sprite.Create(texture,new Rect((cell%2)*w,texture.height-(cell/2+1)*h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);cache[key]=sprite;slices.Add(sprite);return sprite;
    }
    public Sprite Atlas(int cell)
    {
        string key="atlas_"+cell;if(cache.TryGetValue(key,out Sprite hit))return hit;
        var texture=Resources.Load<Texture2D>("Art/PreparationAtlasV1");if(!texture)return null;
        float w=texture.width/4f,h=texture.height/3f;
        var sprite=Sprite.Create(texture,new Rect((cell%4)*w,texture.height-(cell/4+1)*h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);cache[key]=sprite;slices.Add(sprite);return sprite;
    }
    public Sprite DishSprite(RecipePlan recipe)
    {
        if(!recipe.IsHabhab)return PlatedDish;
        if(cache.TryGetValue("habhab_display",out Sprite hit))return hit;
        var source=Atlas(11);if(!source)return null;
        // Exclude transparent atlas gutters through a native sprite rectangle, keeping PNG intact.
        Rect r=source.rect;r.y+=r.height*.36f;r.width*=.98f;r.height*=.61f;
        var sprite=Sprite.Create(source.texture,r,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);cache["habhab_display"]=sprite;slices.Add(sprite);return sprite;
    }
    public Sprite IngredientSprite(string id,bool prepared)
    {
        switch(id){case "garlic":return prepared?(PreparedGarlic?PreparedGarlic:Atlas(1)):Atlas(1);case "turmeric":return prepared?PreparedTurmeric:Turmeric;case "chicken":return Atlas(prepared?8:7);case "onion":return prepared?Detail(0):Atlas(2);case "vegetables":return Atlas(prepared?6:4);case "carrot":return Atlas(prepared?5:4);case "carrot_planks":return prepared?Atlas(5):Detail(1);case "cabbage":return prepared?Atlas(6):Detail(2);case "noodles":return Atlas(9);case "broth":case "vinegar":return Slice("soy");default:return Slice(id);}
    }
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
