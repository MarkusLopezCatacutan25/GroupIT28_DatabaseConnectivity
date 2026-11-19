using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Beginner tutorial played like a short screen recording: each page acts out one part of the game
// (a demo hand taps, drags, slices, washes, cooks and plates) on a loop. Next / Back / Skip.
public sealed class DemoTutorial : MonoBehaviour
{
    public const string SeenKey="SavorPinoy.DemoSeen";
    public static bool Seen=>PlayerPrefs.GetInt(SeenKey,0)==1;
    private sealed class Page{public RectTransform Root;public string Title,Body;public float Loop;public Action<float> Animate;}
    private Page[] pages;private int index;private float time;
    private TMP_Text title,body,counter;private TMP_Text nextLabel;private Button back;
    private RectTransform stage;private KitchenArt art;private Action closed;
    private static readonly Color Parchment=new Color(1,.94f,.78f), Wood=new Color(.24f,.115f,.055f), Ink=new Color(.16f,.07f,.02f);

    public static DemoTutorial Show(Transform canvas,KitchenArt art,Action onClosed)
    {
        var root=PrototypeUI.Panel("DemoTutorial",canvas,0,0,1920,1080,new Color(.08f,.04f,.015f,.96f)).rectTransform;
        root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.sizeDelta=Vector2.zero;root.SetAsLastSibling();
        var demo=root.gameObject.AddComponent<DemoTutorial>();demo.art=art;demo.closed=onClosed;demo.Build(root);return demo;
    }
    private void Build(RectTransform root)
    {
        var frame=PrototypeUI.Rect("DemoFrame",root,0,0,1600,960);ScreenFit.Attach(frame,1600,960);
        PrototypeUI.Panel("DemoCardEdge",frame,0,0,1600,960,new Color(.93f,.74f,.38f)).raycastTarget=false;
        PrototypeUI.Panel("DemoCard",frame,0,0,1584,944,new Color(1,.95f,.80f)).raycastTarget=false;
        counter=PrototypeUI.Text("DemoCounter",frame,"",0,438,1400,44,26);counter.color=new Color(.45f,.26f,.10f);counter.fontStyle=FontStyles.Bold;
        PrototypeUI.Panel("DemoStageEdge",frame,0,110,1428,588,Wood).raycastTarget=false;
        stage=PrototypeUI.Panel("DemoStage",frame,0,110,1412,572,new Color(.36f,.20f,.10f)).rectTransform;stage.gameObject.AddComponent<RectMask2D>();stage.GetComponent<Image>().raycastTarget=false;
        title=PrototypeUI.Text("DemoTitle",frame,"",0,-220,1440,64,42);title.color=Ink;title.fontStyle=FontStyles.Bold;
        body=PrototypeUI.Text("DemoBody",frame,"",0,-298,1440,96,28);body.color=Ink;body.richText=true;
        Button skip=PrototypeUI.Button("DemoSkip",frame,"Skip tutorial",-560,-410,300,78,Close);
        back=PrototypeUI.Button("DemoBack",frame,"Back",230,-410,220,78,()=>Go(index-1));
        Button next=PrototypeUI.Button("DemoNext",frame,"Next",540,-410,300,78,()=>{if(index>=pages.Length-1)Close();else Go(index+1);});
        nextLabel=next.GetComponentInChildren<TMP_Text>();
        foreach(var b in new[]{skip,back,next})b.GetComponentInChildren<TMP_Text>().fontStyle=FontStyles.Bold;
        pages=new[]{Gather(),Wash(),Drag(),Slice(),Cook(),StirPlate(),Finish()};
        Go(0);
    }
    private void Go(int i)
    {
        index=Mathf.Clamp(i,0,pages.Length-1);time=0;
        for(int k=0;k<pages.Length;k++)pages[k].Root.gameObject.SetActive(k==index);
        title.text=pages[index].Title;body.text=pages[index].Body;counter.text="HOW TO PLAY   |   PART "+(index+1)+" OF "+pages.Length;
        back.interactable=index>0;nextLabel.text=index==pages.Length-1?"Let's cook!":"Next";
    }
    private void Close(){PlayerPrefs.SetInt(SeenKey,1);PlayerPrefs.Save();var c=closed;closed=null;Destroy(gameObject);c?.Invoke();}
    private void Update(){if(pages==null)return;time+=Time.unscaledDeltaTime;var p=pages[index];p.Animate(Mathf.Repeat(time,p.Loop));}

    // ---------- helpers ----------
    private static float Seg(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.Clamp01((t-a)/(b-a)));
    private Page NewPage(string name,string head,string text,float loop,out RectTransform r)
    {r=PrototypeUI.Rect(name,stage,0,0,1412,572);return new Page{Root=r,Title=head,Body=text,Loop=loop};}
    private Image Pic(RectTransform parent,Sprite s,float x,float y,float w,float h){var i=art.Draw("DemoPic",parent,s,x,y,w,h);if(!s)i.color=Color.clear;return i;}
    private static TMP_Text Tag(RectTransform parent,string text,float x,float y,float w,int size=24)
    {
        var bg=PrototypeUI.Panel("DemoTag",parent,x,y,w,44,new Color(.20f,.10f,.04f,.92f));bg.raycastTarget=false;
        var t=PrototypeUI.Text("DemoTagText",bg.transform,text,0,0,w-12,42,size);t.color=Parchment;t.fontStyle=FontStyles.Bold;return t;
    }
    private static HandGraphic Hand(RectTransform parent,bool open=false)
    {
        var r=PrototypeUI.Rect("DemoHand",parent,0,0,130,160);r.pivot=new Vector2(.5f,1);var h=r.gameObject.AddComponent<HandGraphic>();h.raycastTarget=false;h.Open=open;return h;
    }
    private static void Press(HandGraphic h,bool pressed){if(h.Pressed!=pressed){h.Pressed=pressed;h.SetVerticesDirty();}h.rectTransform.localScale=Vector3.one*(pressed?.92f:1);}
    private static KitchenCueGraphic Ring(RectTransform parent,float x,float y,float w,float h,Color c)
    {var g=PrototypeUI.Rect("DemoRing",parent,x,y,w,h).gameObject.AddComponent<KitchenCueGraphic>();g.color=c;g.raycastTarget=false;return g;}

    // ---------- 1. Gather ----------
    private Page Gather()
    {
        var p=NewPage("PageGather","1. GATHER THE INGREDIENTS","Follow the <b>Recipe steps</b> panel. <b>Tap</b> each ingredient the dish needs, then tap the knife. One item does <b>not</b> belong!",5.2f,out var r);
        Sprite[] s={art.IngredientSprite("garlic",false),art.IngredientSprite("onion",false),art.IngredientSprite("pork",false),art.IngredientSprite("vegetables",false)};
        string[] n={"Garlic","Onion","Pork","Carrot & cabbage"};float[] x={-480,-160,160,480};
        var rings=new KitchenCueGraphic[4];var tags=new TMP_Text[4];
        for(int i=0;i<4;i++){rings[i]=Ring(r,x[i],30,250,200,new Color(1,.83f,.30f));Pic(r,s[i],x[i],30,220,165);tags[i]=Tag(r,n[i],x[i],-110,250,22);}
        var wrong=Tag(r,"NOT IN LOMI!",480,140,250,24);wrong.color=new Color(1,.55f,.45f);
        Tag(r,"LOMI BATANGAS",0,240,330,24);
        var hand=Hand(r);
        p.Animate=t=>
        {
            int tapped=t<1.1f?0:t<2.2f?1:t<3.3f?2:3;
            for(int i=0;i<3;i++){bool done=i<tapped;rings[i].color=done?new Color(.45f,1,.55f):new Color(1,.83f,.30f);tags[i].text=(done?"READY / ":"")+n[i];}
            rings[3].color=t>3.9f?new Color(1,.35f,.3f):new Color(1,.83f,.30f);wrong.transform.parent.gameObject.SetActive(t>3.9f);
            float slot=Mathf.Clamp(t/1.1f,0,3.99f);int k=Mathf.Min(3,(int)slot);float f=slot-k;
            Vector2 at=new Vector2(x[k],20);Vector2 from=k==0?new Vector2(-480,-230):new Vector2(x[k-1],20);
            hand.rectTransform.anchoredPosition=Vector2.Lerp(from,at,Seg(f,0,.55f))+new Vector2(20,-10);Press(hand,f>.6f&&f<.85f&&k<3||k==3&&f>.6f&&f<.85f);
        };
        return p;
    }
    // ---------- 2. Wash ----------
    private Page Wash()
    {
        var p=NewPage("PageWash","2. WASH AT THE SINK","<b>Drag</b> the item into the sink basin and let go. The hands scrub it under running water - in a real kitchen, wash for 20 seconds.",5.2f,out var r);
        const float k=.722f;float mouthX=137*k,mouthY=122*k,itemX=137*k,itemY=-190*k;
        Pic(r,art.Loose("SinkStation"),0,-60*k,1800*k,1006*k).preserveAspect=false;
        var stream=Pic(r,art.Loose("PourStream"),mouthX,mouthY,40,mouthY-itemY-30);stream.rectTransform.pivot=new Vector2(.5f,1);stream.rectTransform.anchoredPosition=new Vector2(mouthX,mouthY);stream.color=new Color(.62f,.84f,1f,.85f);
        if(stream.sprite){stream.type=Image.Type.Filled;stream.fillMethod=Image.FillMethod.Vertical;stream.fillOrigin=(int)Image.OriginVertical.Top;}
        var item=Pic(r,art.IngredientSprite("bellpepper",false),-470,-120,190,150);
        var hands=new[]{Hand(r,true),Hand(r,true)};hands[1].rectTransform.localScale=new Vector3(-1,1,1);
        var grab=Hand(r);
        var cheer=PrototypeUI.Text("Clean",r,"MALINIS NA!",itemX,itemY+150,440,70,44);cheer.color=new Color(.08f,.45f,.78f);cheer.fontStyle=FontStyles.Bold;
        Tag(r,"DRAG HERE",itemX,itemY-90,200,22);
        p.Animate=t=>
        {
            float move=Seg(t,.4f,1.6f);Vector2 pos=Vector2.Lerp(new Vector2(-470,-120),new Vector2(itemX,itemY),move);
            bool washing=t>1.7f&&t<4f;item.rectTransform.anchoredPosition=washing?pos+new Vector2(Mathf.Sin(t*14)*12,0):pos;item.rectTransform.localRotation=Quaternion.Euler(0,0,washing?Mathf.Sin(t*10)*8:0);
            grab.gameObject.SetActive(t<1.7f);grab.rectTransform.anchoredPosition=pos+new Vector2(15,-45);Press(grab,t>.3f&&t<1.65f);
            float flow=Mathf.Clamp01((t-1.7f)*5)*Mathf.Clamp01((4f-t)*5);if(stream.sprite)stream.fillAmount=flow;else stream.rectTransform.sizeDelta=new Vector2(14,(mouthY-itemY-30)*flow);
            for(int i=0;i<2;i++){float side=i==0?-1:1,rub=Mathf.Sin(t*Mathf.PI*5+i*Mathf.PI)*18;hands[i].gameObject.SetActive(washing);
                hands[i].rectTransform.anchoredPosition=new Vector2(itemX+side*(58+rub),itemY+75+Mathf.Cos(t*Mathf.PI*5)*6);hands[i].rectTransform.localRotation=Quaternion.Euler(0,0,side*-(28+rub*.4f));}
            cheer.gameObject.SetActive(t>4f);cheer.rectTransform.localScale=Vector3.one*(1+.25f*Mathf.Sin(Mathf.Clamp01((t-4f)*2.5f)*Mathf.PI));
        };
        return p;
    }
    // ---------- 3. Drag to the target ----------
    private Page Drag()
    {
        var p=NewPage("PageDrag","3. DRAG AND DROP","<b>Press and hold</b> the item, slide it onto the <b>dark target box</b>, then let go. A bright ring shows where it goes.",3.6f,out var r);
        Tag(r,"PEEL EACH GARLIC CLOVE",0,240,420,24);
        var box=PrototypeUI.Panel("Target",r,330,-10,300,240,new Color(.23f,.12f,.05f,.92f));box.raycastTarget=false;
        var label=PrototypeUI.Text("TargetText",box.transform,"REMOVE SKIN",0,88,280,50,26);label.color=Parchment;label.fontStyle=FontStyles.Bold;
        var ring=Ring(r,330,-10,330,270,new Color(1,.85f,.38f));
        var arrow=PrototypeUI.Text("Arrow",r,"> > >",0,-10,200,70,52);arrow.color=Parchment;arrow.fontStyle=FontStyles.Bold;
        var item=Pic(r,art.Atlas(0),-330,-10,260,200);var hand=Hand(r);
        p.Animate=t=>
        {
            float m=Seg(t,.5f,1.9f);Vector2 pos=Vector2.Lerp(new Vector2(-330,-10),new Vector2(330,-10),m);
            bool held=t>.35f&&t<2.05f;item.rectTransform.anchoredPosition=pos+(held?new Vector2(0,30):Vector2.zero);item.rectTransform.localScale=Vector3.one*(held?1.1f:1);
            item.sprite=t>2.1f?art.Atlas(1):art.Atlas(0);
            hand.rectTransform.anchoredPosition=pos+new Vector2(20,-40);Press(hand,held);hand.gameObject.SetActive(t<2.6f);
            ring.color=t>2.05f?new Color(.45f,1,.55f):new Color(1,.85f,.38f);label.text=t>2.05f?"DONE!":"REMOVE SKIN";
        };
        return p;
    }
    // ---------- 4. Slice ----------
    private Page Slice()
    {
        var p=NewPage("PageSlice","4. SLICE ALONG THE GREEN LINE","Put your finger on <b>START</b> and slide straight <b>down</b> to <b>FINISH</b> in one smooth stroke. Keep your other fingertips curled back, away from the blade.",3.4f,out var r);
        var board=PrototypeUI.Panel("Board",r,0,-20,860,380,new Color(.80f,.58f,.33f));board.raycastTarget=false;
        PrototypeUI.Panel("BoardEdge",board.transform,0,-176,860,28,new Color(.55f,.36f,.18f)).raycastTarget=false;
        var food=Pic(r,art.Atlas(3),60,-30,330,230);var pieces=Pic(r,art.IngredientSprite("onion",true),-300,-80,220,150);
        var line=PrototypeUI.Panel("Guide",r,0,-20,6,260,new Color(.38f,.95f,.56f));line.raycastTarget=false;
        Ring(r,0,110,44,44,new Color(1,.83f,.30f));Ring(r,0,-150,44,44,new Color(1,.83f,.30f));
        Tag(r,"START",-110,110,130,22);Tag(r,"FINISH",-110,-150,130,22);
        var knife=PrototypeUI.Rect("Knife",r,0,0,300,70).gameObject.AddComponent<KnifeGraphic>();knife.raycastTarget=false;
        var hand=Hand(r);var count=Tag(r,"",470,200,240,24);
        p.Animate=t=>
        {
            float m=Seg(t,.7f,1.9f);Vector2 tip=Vector2.Lerp(new Vector2(0,110),new Vector2(0,-150),m);
            bool cutting=t>.55f&&t<2f;hand.rectTransform.anchoredPosition=(t<.55f?Vector2.Lerp(new Vector2(250,-200),new Vector2(0,110),Seg(t,0,.5f)):tip)+new Vector2(0,0);Press(hand,cutting);
            knife.rectTransform.anchoredPosition=hand.rectTransform.anchoredPosition+new Vector2(-55,45);knife.rectTransform.localRotation=Quaternion.Euler(0,0,cutting?-8:0);
            bool done=t>=2f;pieces.gameObject.SetActive(done);pieces.rectTransform.localScale=Vector3.one*(done?1+.12f*Mathf.Sin(Mathf.Clamp01((t-2f)*4)*Mathf.PI):1);
            food.fillAmount=1;count.text=done?"CUT 1 OF 3":"CUT 0 OF 3";
        };
        return p;
    }
    // ---------- 5. Cook ----------
    private Page Cook()
    {
        var p=NewPage("PageCook","5. HEAT THE PAN AND ADD IN ORDER","Turn the <b>stove knob</b> until the heat is between <b>60 and 85</b>. Then drag the <b>numbered</b> ingredients into the pan, one by one: 1, 2, 3...",5.4f,out var r);
        var plan=RecipePlan.Lomi;var wok=Pic(r,art.Vessel(plan),-60,-30,560,330);
        var flame=PrototypeUI.Rect("Flame",r,-60,-150,330,60).gameObject.AddComponent<StoveFlameGraphic>();flame.Arc=true;flame.raycastTarget=false;flame.transform.SetSiblingIndex(wok.transform.GetSiblingIndex());
        var plate=PrototypeUI.Rect("KnobPlate",r,470,-130,220,220).gameObject.AddComponent<StoveKnobGraphic>();plate.Plate=true;plate.raycastTarget=false;
        var knob=PrototypeUI.Rect("Knob",r,470,-130,128,128);knob.gameObject.AddComponent<StoveKnobGraphic>().raycastTarget=false;
        var heat=Tag(r,"",470,30,300,24);
        string[] ids={"oil","garlic","onion"};var items=new Image[3];
        for(int i=0;i<3;i++){items[i]=Pic(r,art.IngredientSprite(ids[i],true),-560,170-i*150,130,100);Tag(items[i].rectTransform,(i+1)+". "+RecipePlan.Name(ids[i]),0,-62,190,20);}
        var hand=Hand(r);
        p.Animate=t=>
        {
            float turn=Seg(t,.4f,1.6f);knob.localRotation=Quaternion.Euler(0,0,-180*.6f*turn);flame.Level=.6f*turn;
            int value=Mathf.RoundToInt(Mathf.Lerp(20,72,Seg(t,.6f,2.2f)));heat.text="HEAT "+value+(value>=60?"  GOOD!":"");heat.color=value>=60?new Color(.6f,1,.6f):Parchment;
            float drag=Seg(t,2.6f,3.9f);Vector2 start=new Vector2(-560,170),inPan=new Vector2(-60,10);
            items[0].gameObject.SetActive(t<4.1f);items[0].rectTransform.anchoredPosition=Vector2.Lerp(start,inPan,drag)+(t>2.4f&&t<4f?new Vector2(0,30):Vector2.zero);
            items[0].rectTransform.localScale=Vector3.one*Mathf.Lerp(1,.6f,Seg(t,3.6f,4.1f));
            Vector2 hp=t<1.7f?new Vector2(470,-60)+new Vector2(Mathf.Sin(turn*Mathf.PI*.6f)*60,-turn*40):t<2.4f?Vector2.Lerp(new Vector2(470,-100),start,Seg(t,1.7f,2.4f)):Vector2.Lerp(start,inPan,drag);
            hand.rectTransform.anchoredPosition=hp+(t>=1.7f?new Vector2(15,-40):Vector2.zero);Press(hand,t>.35f&&t<1.65f||t>2.45f&&t<3.95f);hand.gameObject.SetActive(t<4.4f);
        };
        return p;
    }
    // ---------- 6. Stir and plate ----------
    private Page StirPlate()
    {
        var p=NewPage("PageStir","6. STIR WITH THE SANDOK, THEN PLATE","<b>Hold the sandok</b> and move it around over the pan to stir. When cooking is done, turn the flame <b>OFF</b> and drag the sandok onto the <b>plate</b>.",6f,out var r);
        var plan=RecipePlan.Lomi;var wok=Pic(r,art.Vessel(plan),-200,-30,560,330);var food=Pic(r,art.IngredientSprite("noodles",true),-200,10,300,130);
        var flame=PrototypeUI.Rect("Flame",r,-200,-150,330,60).gameObject.AddComponent<StoveFlameGraphic>();flame.Arc=true;flame.raycastTarget=false;flame.transform.SetSiblingIndex(wok.transform.GetSiblingIndex());
        var plate=PrototypeUI.Rect("Plate",r,420,-120,300,120).gameObject.AddComponent<PlateGraphic>();plate.raycastTarget=false;
        var served=Pic((RectTransform)plate.transform,art.DishSprite(plan),0,18,220,110);
        var sandok=PrototypeUI.Rect("Sandok",r,0,0,240,70).gameObject.AddComponent<SandokGraphic>();sandok.raycastTarget=false;
        var hand=Hand(r);var state=Tag(r,"",0,230,420,24);
        p.Animate=t=>
        {
            Vector2 sp;bool stirring=t<2.8f,serving=t>=3.4f;
            if(stirring){float a=t*5;sp=new Vector2(-200,40)+new Vector2(Mathf.Cos(a)*110,Mathf.Sin(a)*32);}
            else if(!serving)sp=new Vector2(-200,40);
            else sp=Vector2.Lerp(new Vector2(-200,40),new Vector2(420,-90),Seg(t,3.6f,4.8f));
            flame.Level=t<3.1f?.6f:Mathf.Lerp(.6f,0,Seg(t,3.1f,3.4f));
            sandok.rectTransform.anchoredPosition=sp+new Vector2(-40,40);sandok.rectTransform.localRotation=Quaternion.Euler(0,0,stirring?Mathf.Sin(t*12)*15-25:-15);
            hand.rectTransform.anchoredPosition=sp+new Vector2(20,-5);Press(hand,t<4.9f);
            food.gameObject.SetActive(t<4.8f);food.rectTransform.localRotation=Quaternion.Euler(0,0,stirring?Mathf.Sin(t*8)*3:0);
            served.gameObject.SetActive(t>=4.8f);plate.Glow=t>3.4f&&t<4.8f;plate.SetVerticesDirty();
            state.text=stirring?"STIRRING...":t<3.4f?"FLAME OFF":t<4.8f?"SCOOP TO THE PLATE":"SERVED!";
        };
        return p;
    }
    // ---------- 7. Finish ----------
    private Page Finish()
    {
        var p=NewPage("PageFinish","7. EARN STARS AND HERITAGE CARDS","Fewer mistakes, careful cuts and the right heat earn more stars. Finish a dish to unlock its <b>Heritage Card</b>. Stuck in the game? Wait a moment and a <b>moving hand</b> will show you.",3f,out var r);
        var card=PrototypeUI.Panel("Card",r,0,0,420,500,new Color(1,.95f,.80f));card.raycastTarget=false;
        Pic((RectTransform)card.transform,art.DishSprite(RecipePlan.Lomi),0,70,360,240);
        var cardName=PrototypeUI.Text("CardName",card.transform,"LOMI BATANGAS",0,-100,380,60,34);cardName.fontStyle=FontStyles.Bold;
        var stars=PrototypeUI.Text("Stars",card.transform,"",0,-170,380,60,34);stars.color=new Color(.75f,.45f,.0f);stars.fontStyle=FontStyles.Bold;
        var glints=PrototypeUI.Rect("Glints",r,0,0,1412,572).gameObject.AddComponent<SparkleGraphic>();glints.raycastTarget=false;
        glints.Points=new[]{new Vector2(-230,200),new Vector2(240,170),new Vector2(-250,-120),new Vector2(260,-180),new Vector2(0,270)};
        var unlocked=Tag(r,"HERITAGE CARD UNLOCKED!",0,-250,460,26);
        p.Animate=t=>{int n=Mathf.Clamp((int)(t/.5f),0,3);stars.text=n+" OF 3 STARS";card.rectTransform.localScale=Vector3.one*(1+.04f*Mathf.Sin(t*Mathf.PI*2/3));unlocked.transform.parent.gameObject.SetActive(t>1.6f);};
        return p;
    }
}
