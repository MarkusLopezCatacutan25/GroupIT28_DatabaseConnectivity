using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Preparation demonstrations and non-cut gestures. Ordered validation stays in the model.
// Washing steps switch to a sink station: the item is dragged into the basin and rinsed under the tap.
public sealed class PreparationLessonView
{
    private readonly RectTransform group,ghostRoot,sinkStation,target;
    private readonly Image source,ghost,example,targetPanel,washItem,washStream,washSplash;
    private readonly TMP_Text instruction,stepCount,sourceLabel,targetLabel;
    private readonly GameObject arrow,sourcePlate;
    private readonly IngredientDrag gesture;
    private readonly KitchenArt art;
    private readonly Action<string> action;
    private CookingAttempt model;
    private bool enabled,washing;
    private string stepId;
    private Coroutine wash;
    private readonly BubbleGraphic idleBubbles,washBubbles;
    private readonly SparkleGraphic cleanGlints;
    private readonly TMP_Text cleanText;
    // Demonstration hands: two scrubbing hands at the sink and a guide hand for drag steps.
    private readonly RectTransform[] washHands=new RectTransform[2];
    private readonly HandGuide prepHand;
    // Sink station layout (closeup coordinates), measured from SinkStation.png (full-scene art, 16:9):
    // tap mouth at x 137 / y 122, basin interior centred at x 5 / y -216.
    private const float SinkX=137,ArtY=-60,MouthY=122,BasinX=5,BasinY=-216,BasinW=780,BasinH=250,ItemY=-190;
    private static readonly Color Water=new Color(.62f,.84f,1f,.85f);
    public PreparationLessonView(RectTransform closeup,RectTransform design,KitchenArt art,Action<string> action)
    {
        this.art=art;this.action=action;
        sinkStation=PrototypeUI.Rect("SinkStation",closeup,0,0,1800,1000);sinkStation.gameObject.AddComponent<RectMask2D>();
        art.Draw("SinkStationArt",sinkStation,art.Loose("SinkStation"),0,ArtY,1800,1006,false);
        // Small moving details keep the station lively: glints on the steel and soap bubbles drifting up.
        var glints=PrototypeUI.Rect("SinkGlints",sinkStation,0,0,1800,1000).gameObject.AddComponent<SparkleGraphic>();glints.raycastTarget=false;
        glints.Points=new[]{new Vector2(-12,91),new Vector2(-373,-84),new Vector2(401,-84),new Vector2(380,-345),new Vector2(295,39),new Vector2(-330,-330)};
        idleBubbles=PrototypeUI.Rect("SinkBubbles",sinkStation,300,40,160,260).gameObject.AddComponent<BubbleGraphic>();idleBubbles.raycastTarget=false;idleBubbles.Count=5;
        washBubbles=PrototypeUI.Rect("WashBubbles",sinkStation,SinkX,ItemY+60,340,300).gameObject.AddComponent<BubbleGraphic>();washBubbles.raycastTarget=false;washBubbles.Count=14;washBubbles.gameObject.SetActive(false);
        var sign=PrototypeUI.Panel("SinkStationSign",sinkStation,-640,60,300,64,new Color(.23f,.12f,.05f,.95f));sign.raycastTarget=false;
        var station=PrototypeUI.Text("SinkStationTitle",sign.transform,"SINK STATION",0,0,290,60,28);station.color=new Color(1,.94f,.78f);station.fontStyle=FontStyles.Bold;
        washStream=art.Draw("SinkWater",sinkStation,art.Loose("PourStream"),SinkX,MouthY,52,MouthY-ItemY-40,false);washStream.color=Water;
        washStream.rectTransform.pivot=new Vector2(.5f,1);washStream.rectTransform.anchoredPosition=new Vector2(SinkX,MouthY);
        if(washStream.sprite){washStream.type=Image.Type.Filled;washStream.fillMethod=Image.FillMethod.Vertical;washStream.fillOrigin=(int)Image.OriginVertical.Top;}
        washItem=art.Draw("SinkItem",sinkStation,null,SinkX,ItemY,240,180);
        washSplash=art.Draw("SinkSplash",sinkStation,art.Loose("PourSplash"),SinkX,ItemY-30,280,140);washSplash.color=Water;
        washStream.transform.SetSiblingIndex(washItem.transform.GetSiblingIndex()+1);
        for(int i=0;i<2;i++)
        {
            var h=PrototypeUI.Rect("WashHand"+i,sinkStation,SinkX,ItemY,130,160);h.pivot=new Vector2(.5f,1);
            var g=h.gameObject.AddComponent<HandGraphic>();g.Open=true;g.raycastTarget=false;if(i==1)h.localScale=new Vector3(-1,1,1);
            washHands[i]=h;h.gameObject.SetActive(false);
        }
        washBubbles.transform.SetAsLastSibling();
        // After rinsing: the item twinkles and a cheer pops up, so a finished wash feels rewarding.
        cleanGlints=PrototypeUI.Rect("CleanGlints",sinkStation,0,0,1800,1000).gameObject.AddComponent<SparkleGraphic>();cleanGlints.raycastTarget=false;
        cleanGlints.Points=new[]{new Vector2(SinkX-120,ItemY+70),new Vector2(SinkX+115,ItemY+80),new Vector2(SinkX+130,ItemY-50),new Vector2(SinkX-110,ItemY-55),new Vector2(SinkX,ItemY+105)};
        cleanText=PrototypeUI.Text("CleanCheer",sinkStation,"MALINIS NA!",SinkX,ItemY+150,440,70,44);cleanText.color=new Color(.08f,.45f,.78f);cleanText.fontStyle=FontStyles.Bold;
        cleanGlints.gameObject.SetActive(false);cleanText.gameObject.SetActive(false);
        washStream.gameObject.SetActive(false);washItem.gameObject.SetActive(false);washSplash.gameObject.SetActive(false);
        sinkStation.gameObject.SetActive(false);

        group=PrototypeUI.Rect("PreparationGesture",closeup,0,-15,1050,390);
        targetPanel=PrototypeUI.Panel("PreparationTarget",group,300,0,290,230,new Color(.23f,.12f,.05f,.92f));target=targetPanel.rectTransform;
        targetLabel=PrototypeUI.Text("PreparationTargetLabel",target,"",0,0,260,160,29);targetLabel.color=new Color(1,.94f,.78f);targetLabel.fontStyle=FontStyles.Bold;
        source=art.Draw("PreparationSource",group,null,-280,0,290,220);source.raycastTarget=true;
        sourcePlate=PrototypeUI.Panel("PreparationSourcePlate",group,-280,-150,390,58,new Color(.23f,.12f,.05f,.92f)).gameObject;sourcePlate.GetComponent<Image>().raycastTarget=false;
        sourceLabel=PrototypeUI.Text("PreparationSourceLabel",group,"",-280,-150,370,54,26);
        sourceLabel.color=new Color(1,.94f,.78f);sourceLabel.fontStyle=FontStyles.Bold;
        var arrowText=PrototypeUI.Text("PreparationArrow",group,"> > >",0,0,200,70,52);arrowText.fontStyle=FontStyles.Bold;arrow=arrowText.gameObject;
        example=art.Draw("PreparationExample",group,null,0,125,200,120);
        var lessonFrame=PrototypeUI.Panel("PreparationLessonFrame",closeup,0,256,1412,192,new Color(.93f,.74f,.38f));lessonFrame.raycastTarget=false;
        var instructionCard=PrototypeUI.Panel("PreparationInstructionCard",lessonFrame.transform,0,0,1400,180,new Color(1,.95f,.80f));instructionCard.raycastTarget=false;
        instruction=PrototypeUI.Text("PreparationInstruction",instructionCard.transform,"",0,-48,1340,80,27);instruction.color=new Color(.16f,.07f,.02f);instruction.fontStyle=FontStyles.Bold;
        stepCount=PrototypeUI.Text("PreparationStepCount",instructionCard.transform,"",0,68,1300,30,22);stepCount.color=new Color(.45f,.26f,.10f);stepCount.fontStyle=FontStyles.Bold;
        ghostRoot=PrototypeUI.Rect("PreparationGhostRoot",design,0,0,1800,1000);ghost=art.Draw("PreparationGhost",ghostRoot,null,0,0,220,180);ghostRoot.gameObject.SetActive(false);
        HoldingHand(ghost.rectTransform);
        gesture=source.gameObject.AddComponent<IngredientDrag>();gesture.Target=target;
        prepHand=HandGuide.Create("PrepHandGuide",group);
        gesture.CanDrag=()=>enabled&&!washing&&model?.Preparation.Current!=null&&!model.Preparation.Current.IsCut;
        gesture.Begin=()=>{stepId=model.Preparation.Current.Id;ghost.sprite=source.sprite;ghostRoot.gameObject.SetActive(true);ghostRoot.SetAsLastSibling();return ghost.rectTransform;};
        gesture.End=(ok,point)=>
        {
            ghostRoot.gameObject.SetActive(false);
            if(!ok||!enabled||model.Preparation.Current?.Id!=stepId)return;
            if(model.Preparation.Current.Target=="SINK"){if(wash!=null)art.StopCoroutine(wash);wash=art.StartCoroutine(Wash(stepId,source.sprite));}
            else action("prepare:"+stepId);
        };
    }
    // A hand under the dragged copy, so a grabbed item looks held between the fingers.
    public static void HoldingHand(RectTransform item)
    {
        var h=PrototypeUI.Rect("HoldingHand",item,18,-item.sizeDelta.y*.12f,130,160);h.pivot=new Vector2(.5f,1);h.SetAsFirstSibling();
        h.gameObject.AddComponent<HandGraphic>().raycastTarget=false;h.localRotation=Quaternion.Euler(0,0,-12);
    }
    // Rinse under the running tap: water falls, the item turns under it, then the step completes.
    private IEnumerator Wash(string id,Sprite item)
    {
        washing=true;source.enabled=false;SavorAudio.Play("SfxFaucet",.8f);
        washItem.sprite=item;washItem.gameObject.SetActive(true);washStream.gameObject.SetActive(true);washSplash.gameObject.SetActive(true);washBubbles.gameObject.SetActive(true);
        var itemRect=washItem.rectTransform;var splashRect=washSplash.rectTransform;
        for(float t=0;t<1;t+=Time.unscaledDeltaTime/1.6f)
        {
            float flow=Mathf.Clamp01(t*6)*Mathf.Clamp01((1-t)*6);
            if(washStream.sprite)washStream.fillAmount=flow;else washStream.rectTransform.sizeDelta=new Vector2(14,(MouthY-ItemY-40)*flow);
            itemRect.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI*6)*9);
            itemRect.anchoredPosition=new Vector2(SinkX+Mathf.Sin(t*Mathf.PI*4)*18,ItemY);
            splashRect.localScale=Vector3.one*(.6f+.4f*Mathf.Abs(Mathf.Sin(t*Mathf.PI*5)))*flow;
            // Two hands rub the item under the water.
            for(int i=0;i<2;i++){float side=i==0?-1:1,rub=Mathf.Sin(t*Mathf.PI*10+i*Mathf.PI)*22;washHands[i].gameObject.SetActive(true);washHands[i].SetAsLastSibling();
                washHands[i].anchoredPosition=new Vector2(SinkX+side*(70+rub),ItemY+95+Mathf.Cos(t*Mathf.PI*10)*8);washHands[i].localRotation=Quaternion.Euler(0,0,side*-(28+rub*.4f));}
            yield return null;
        }
        itemRect.localRotation=Quaternion.identity;washStream.gameObject.SetActive(false);washSplash.gameObject.SetActive(false);foreach(var h in washHands)h.gameObject.SetActive(false);
        cleanGlints.gameObject.SetActive(true);cleanText.gameObject.SetActive(true);
        for(float t=0;t<1;t+=Time.unscaledDeltaTime/.8f)
        {
            cleanText.rectTransform.localScale=Vector3.one*(1+.3f*Mathf.Sin(Mathf.Clamp01(t*2.5f)*Mathf.PI));
            itemRect.localScale=Vector3.one*(1+.07f*Mathf.Sin(t*Mathf.PI));yield return null;
        }
        itemRect.localScale=Vector3.one;washItem.gameObject.SetActive(false);washBubbles.gameObject.SetActive(false);cleanGlints.gameObject.SetActive(false);cleanText.gameObject.SetActive(false);
        washing=false;source.enabled=true;wash=null;
        if(enabled&&model.Preparation.Current?.Id==id)action("prepare:"+id);
    }
    public void Render(CookingAttempt attempt,bool interactive)
    {
        model=attempt;enabled=interactive;var step=attempt.Preparation.Current;
        bool preparing=attempt.Phase==CookingPhase.Chop&&step!=null;group.gameObject.SetActive(preparing&&!step.IsCut);instruction.transform.parent.parent.gameObject.SetActive(preparing);stepCount.gameObject.SetActive(preparing);
        bool atSink=preparing&&!step.IsCut&&step.Target=="SINK";sinkStation.gameObject.SetActive(atSink);
        if(!interactive)Cancel();if(!preparing)return;
        instruction.text=step.Instruction;stepCount.text="STEP "+(attempt.Preparation.Index+1)+" OF "+attempt.Preparation.Count+(step.Repetitions>1?"   |   Repeat "+(attempt.Preparation.Repetition+1)+" of "+step.Repetitions:"");
        source.sprite=art.IngredientSprite(step.Ingredient,false);
        if(step.Id=="separate")source.sprite=art.Slice("garlic");
        if(step.Id=="peel_garlic")source.sprite=art.Atlas(0);
        if(step.Id=="hands")source.sprite=Pick(art.Loose("Soap"),art.Slice("board"));
        else if(step.Ingredient=="hands"||step.Id.StartsWith("meat_board",StringComparison.Ordinal))source.sprite=art.Slice("board");
        if(step.Id=="peel_carrot")source.sprite=Pick(art.Loose(attempt.Preparation.Repetition==0?"CarrotWhole":"CarrotPeeled"),source.sprite);
        sourceLabel.text=step.Id=="hands"?"DRAG: SOAP":step.Id=="clean_after_meat"?"DRAG: KNIFE & BOARD":"DRAG: "+RecipePlan.Name(step.Ingredient).ToUpperInvariant();
        // Sink steps: item on the counter at the left, basin as the drop target. Other steps keep the board layout.
        Place(source.rectTransform,atSink?-640:-280,atSink?-150:0,atSink?230:290,atSink?180:220);
        Place((RectTransform)sourcePlate.transform,atSink?-640:-280,atSink?-280:-150,390,58);Place(sourceLabel.rectTransform,atSink?-640:-280,atSink?-280:-150,370,54);
        Place(target,atSink?BasinX:300,atSink?BasinY:0,atSink?BasinW:290,atSink?BasinH:230);
        targetPanel.color=atSink?new Color(1,1,1,0):new Color(.23f,.12f,.05f,.92f);targetLabel.gameObject.SetActive(!atSink);arrow.SetActive(!atSink);
        targetLabel.text=step.Target??(step.Id.StartsWith("meat_board",StringComparison.Ordinal)?"RAW-MEAT STATION":step.Id=="separate"?"SEPARATE CLOVES":"REMOVE SKIN");
        example.sprite=atSink?null:step.Id=="separate"?art.Atlas(0):step.Id=="peel_garlic"?art.Atlas(1):step.Id=="peel_carrot"?art.Loose("CarrotPeeled"):step.Id=="beat_egg"?art.Loose("EggBeaten"):null;
        example.gameObject.SetActive(example.sprite);
        source.color=Color.Lerp(Color.white,new Color(1,.9f,.67f),attempt.Preparation.Repetition/(float)step.Repetitions);
        if(interactive&&!washing&&!step.IsCut)prepHand.Show(HandGuide.Motion.Drag,source.rectTransform.anchoredPosition,target.anchoredPosition);else prepHand.Hide();
    }
    private static void Place(RectTransform r,float x,float y,float w,float h){r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    private static Sprite Pick(Sprite first,Sprite fallback)=>first?first:fallback;
    public void Cancel()
    {
        gesture.Cancel();ghostRoot.gameObject.SetActive(false);
        if(wash!=null){art.StopCoroutine(wash);wash=null;}
        foreach(var h in washHands)if(h)h.gameObject.SetActive(false);
        if(washing){washing=false;source.enabled=true;washItem.gameObject.SetActive(false);washStream.gameObject.SetActive(false);washSplash.gameObject.SetActive(false);washBubbles.gameObject.SetActive(false);cleanGlints.gameObject.SetActive(false);cleanText.gameObject.SetActive(false);washItem.rectTransform.localScale=Vector3.one;}
    }
}

// Twinkling four-point glints at fixed spots (steel faucet, basin rim). Presentation only.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SparkleGraphic : MaskableGraphic
{
    public Vector2[] Points=new Vector2[0];
    private float time;
    private void Update(){time+=Time.unscaledDeltaTime;SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper m)
    {
        m.Clear();
        for(int i=0;i<Points.Length;i++)
        {
            float phase=Mathf.Repeat(time*.45f+i*.37f,1),size=Mathf.Sin(phase*Mathf.PI);size=size*size*16;if(size<.5f)continue;
            Color c=new Color(1,1,.92f,.9f*Mathf.Sin(phase*Mathf.PI));Vector2 p=Points[i];int k=m.currentVertCount;
            m.AddVert(p,c,Vector2.zero);
            m.AddVert(p+new Vector2(0,size),c,Vector2.zero);m.AddVert(p+new Vector2(size*.22f,size*.22f),c,Vector2.zero);
            m.AddVert(p+new Vector2(size,0),c,Vector2.zero);m.AddVert(p+new Vector2(size*.22f,-size*.22f),c,Vector2.zero);
            m.AddVert(p+new Vector2(0,-size),c,Vector2.zero);m.AddVert(p+new Vector2(-size*.22f,-size*.22f),c,Vector2.zero);
            m.AddVert(p+new Vector2(-size,0),c,Vector2.zero);m.AddVert(p+new Vector2(-size*.22f,size*.22f),c,Vector2.zero);
            for(int j=1;j<=8;j++)m.AddTriangle(k,k+j,k+(j%8)+1);
        }
    }
}
// Soap bubbles drifting upward and fading, drawn as thin rings with a highlight. Presentation only.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class BubbleGraphic : MaskableGraphic
{
    public int Count=6;
    private float time;
    private void Update(){time+=Time.unscaledDeltaTime;SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper m)
    {
        m.Clear();Rect r=rectTransform.rect;
        for(int i=0;i<Count;i++)
        {
            float seed=i*1.618f,phase=Mathf.Repeat(time*.28f+seed,1);
            float x=r.xMin+r.width*(.15f+.7f*Mathf.Repeat(seed*.73f,1))+Mathf.Sin(time*1.7f+seed*3)*10,y=r.yMin+r.height*phase;
            float radius=7+6*Mathf.Repeat(seed*1.31f,1),alpha=Mathf.Sin(phase*Mathf.PI)*.85f;
            Ring(m,new Vector2(x,y),radius,2.2f,new Color(1,1,1,alpha));Ring(m,new Vector2(x-radius*.35f,y+radius*.35f),radius*.22f,radius*.22f,new Color(1,1,1,alpha));
        }
    }
    private static void Ring(VertexHelper m,Vector2 c,float radius,float width,Color tint)
    {
        const int n=20;
        for(int i=0;i<n;i++)
        {
            float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n,inner=Mathf.Max(0,radius-width);int k=m.currentVertCount;
            m.AddVert(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,tint,Vector2.zero);m.AddVert(c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,tint,Vector2.zero);
            m.AddVert(c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*inner,tint,Vector2.zero);m.AddVert(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*inner,tint,Vector2.zero);
            m.AddTriangle(k,k+1,k+2);m.AddTriangle(k,k+2,k+3);
        }
    }
}
