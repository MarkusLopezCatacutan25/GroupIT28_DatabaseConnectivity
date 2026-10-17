using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// One scene, one guarded transition owner. Existing trivia art is supplied by its manager.
public sealed class RegionalMapZoomManager : MonoBehaviour
{
    public enum ViewState { Overview, Zooming, Preview, Splitting, Trivia }
    [SerializeField] private Sprite archipelagoMap, calabarzonMap, recipePreviewCard, regionPin;
    [SerializeField, Range(.2f,2f)] private float transitionSeconds=.65f;
    [SerializeField] private Vector2 calabarzonNormalizedPoint=new Vector2(.43f,.72f);
    public ViewState State { get; private set; }
    private RegionalTriviaManager trivia;
    private RectTransform root, left, right, map, recipe;
    private Image mapImage;
    private Button calabarzonPinBtn, selectRecipeBtn, backToOverviewBtn;
    private CanvasGroup recipeGroup, rightGroup;
    private Coroutine transition;
    private Vector2 focusPosition;
    private Sprite overviewSprite;
    private bool initialized, suspended;
    private Sprite croppedRecipe;

    public void Initialize(RegionalTriviaManager owner,RectTransform safeRoot,RectTransform authoredModal,Sprite fallback)
    {
        if(initialized)return;initialized=true;trivia=owner;root=safeRoot;
        PrototypeUI.Font=owner.Font;
        RectTransform split=PrototypeUI.Rect("SplitContainer",root,0,-35,1760,880);
        // Manual anchors own the animated split; a LayoutGroup would override the tween.
        left=PrototypeUI.Panel("LeftMapPanel",split,0,0,1760,880,new Color(.37f,.73f,.76f)).rectTransform;
        left.gameObject.AddComponent<RectMask2D>();
        map=PrototypeUI.Rect("MapContent",left,0,0,1760,880);
        mapImage=map.gameObject.AddComponent<Image>();overviewSprite=archipelagoMap?archipelagoMap:fallback;
        mapImage.sprite=overviewSprite;mapImage.preserveAspect=true;mapImage.raycastTarget=false;
        Vector2 point=MapPoint();
        calabarzonPinBtn=PrototypeUI.Button("CalabarzonPin",map,"IV-A",point.x,point.y,150,70,FocusRegion);
        calabarzonPinBtn.image.color=Color.clear;
        ((RectTransform)calabarzonPinBtn.transform).sizeDelta=new Vector2(230,300);
        if(regionPin){Image pin=PrototypeUI.Panel("PinArtwork",calabarzonPinBtn.transform,0,65,190,150,Color.white);pin.sprite=regionPin;pin.preserveAspect=true;pin.raycastTarget=false;calabarzonPinBtn.targetGraphic=pin;}
        var pinLabel=calabarzonPinBtn.GetComponentInChildren<TMPro.TMP_Text>();pinLabel.text="CALABARZON";pinLabel.fontSize=21;pinLabel.rectTransform.anchoredPosition=new Vector2(0,-35);pinLabel.rectTransform.sizeDelta=new Vector2(230,40);
        right=PrototypeUI.Rect("RightTriviaPanel",split,440,0,880,880);
        authoredModal.SetParent(right,false);owner.PlaceSplitModal(authoredModal);
        rightGroup=right.gameObject.AddComponent<CanvasGroup>();
        recipe=PrototypeUI.Panel("RecipePreviewCard",root,525,-35,540,720,Color.white).rectTransform;
        // Keep the supplied PNG intact; a sprite rectangle excludes its white outer margin.
        if(recipePreviewCard){recipe.GetComponent<Image>().sprite=recipePreviewCard;recipe.GetComponent<Image>().preserveAspect=true;}
        recipeGroup=recipe.gameObject.AddComponent<CanvasGroup>();
        if(!recipePreviewCard)PrototypeUI.Text("RecipeDescription",recipe,"Prepare luyang dilaw and follow the recipe.",0,100,410,200,28);
        PrototypeUI.Text("RecipeTitle",recipe,"Adobo sa Dilaw",0,-100,420,65,32);
        PrototypeUI.Text("RecipeRegion",recipe,"Batangas / Region IV-A",0,-170,410,55,25);
        selectRecipeBtn=PrototypeUI.Button("ChooseBatangas",recipe,"Select Recipe",0,-255,380,70,SelectRecipe);
        backToOverviewBtn=PrototypeUI.Button("BackToOverview",root,"Back to Overview",-670,465,350,65,ResetMapZoom);
        ResetMapZoom();
    }
    private Vector2 MapPoint()
    {
        // Convert normalized sprite coordinates to the aspect-fitted image rectangle.
        Vector2 size=map.rect.size;
        if(overviewSprite){float ratio=overviewSprite.rect.width/overviewSprite.rect.height;if(size.x/size.y>ratio)size.x=size.y*ratio;else size.y=size.x/ratio;}
        Vector2 normalized=archipelagoMap?calabarzonNormalizedPoint:new Vector2(.42f,.46f);
        return Vector2.Scale(normalized-Vector2.one*.5f,size);
    }
    public void FocusRegion()
    {
        if(!initialized||State!=ViewState.Overview||suspended)return;
        State=ViewState.Zooming;calabarzonPinBtn.interactable=false;
        focusPosition=-MapPoint()*2.2f;transition=StartCoroutine(Zoom());
    }
    private IEnumerator Zoom()
    {
        backToOverviewBtn.gameObject.SetActive(true);
        yield return Tween(()=>{ },t=>{map.localScale=Vector3.one*Mathf.Lerp(1,2.2f,t);map.anchoredPosition=Vector2.Lerp(Vector2.zero,focusPosition,t);});
        if(calabarzonMap){mapImage.sprite=calabarzonMap;map.localScale=Vector3.one;map.anchoredPosition=Vector2.zero;focusPosition=Vector2.zero;}
        calabarzonPinBtn.gameObject.SetActive(false);recipe.gameObject.SetActive(true);
        yield return Tween(()=>{},t=>{recipeGroup.alpha=t;recipe.anchoredPosition=new Vector2(Mathf.Lerp(650,525,t),-35);});
        State=ViewState.Preview;selectRecipeBtn.interactable=true;transition=null;
    }
    private void SelectRecipe()
    {
        if(State!=ViewState.Preview||suspended)return;State=ViewState.Splitting;selectRecipeBtn.interactable=false;transition=StartCoroutine(Split());
    }
    private IEnumerator Split()
    {
        yield return Tween(()=>{},t=>{recipeGroup.alpha=1-t;recipe.anchoredPosition=new Vector2(Mathf.Lerp(525,650,t),-35);});
        recipe.gameObject.SetActive(false);right.gameObject.SetActive(true);trivia.Modal.gameObject.SetActive(true);
        yield return Tween(()=>{},t=>{left.sizeDelta=new Vector2(Mathf.Lerp(1760,880,t),880);map.sizeDelta=new Vector2(Mathf.Lerp(1760,880,t),880);left.anchoredPosition=new Vector2(Mathf.Lerp(0,-440,t),0);rightGroup.alpha=t;});
        State=ViewState.Trivia;rightGroup.interactable=rightGroup.blocksRaycasts=true;
        trivia.LoadTriviaForRegion("CALABARZON");transition=null;
    }
    private IEnumerator Tween(System.Action begin,System.Action<float> apply)
    {
        begin();float elapsed=0;apply(0);
        while(elapsed<transitionSeconds){yield return null;if(suspended)continue;elapsed+=Time.unscaledDeltaTime;apply(Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/transitionSeconds)));}
        apply(1);
    }
    public void ResetMapZoom()
    {
        if(!initialized)return;if(transition!=null)StopCoroutine(transition);transition=null;
        trivia.CancelTrivia();State=ViewState.Overview;
        mapImage.sprite=overviewSprite;map.anchoredPosition=Vector2.zero;map.localScale=Vector3.one;
        left.sizeDelta=new Vector2(1760,880);left.anchoredPosition=Vector2.zero;
        map.sizeDelta=new Vector2(1760,880);
        recipe.gameObject.SetActive(false);recipeGroup.alpha=0;selectRecipeBtn.interactable=false;
        right.gameObject.SetActive(false);rightGroup.alpha=0;rightGroup.interactable=rightGroup.blocksRaycasts=false;
        calabarzonPinBtn.gameObject.SetActive(true);calabarzonPinBtn.interactable=true;backToOverviewBtn.gameObject.SetActive(false);
    }
    private void OnApplicationPause(bool value){suspended=value;}
    private void OnDisable(){if(transition!=null)StopCoroutine(transition);transition=null;}
    private void OnDestroy()
    {
        if(croppedRecipe)Destroy(croppedRecipe);
        if(calabarzonPinBtn)calabarzonPinBtn.onClick.RemoveListener(FocusRegion);
        if(selectRecipeBtn)selectRecipeBtn.onClick.RemoveListener(SelectRecipe);
        if(backToOverviewBtn)backToOverviewBtn.onClick.RemoveListener(ResetMapZoom);
    }
}
