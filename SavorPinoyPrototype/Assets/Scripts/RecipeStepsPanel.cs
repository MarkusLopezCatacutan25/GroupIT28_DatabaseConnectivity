using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Side panel with brief, ordered steps for each part of the lesson (Gather, Prepare, Cook, Plate).
// The current step is highlighted and finished steps are ticked, so players always know what comes next.
public sealed class RecipeStepsPanel
{
    public const float OpenWidth=380, ClosedWidth=96;
    private readonly RectTransform root;private readonly Image background;
    private readonly TMP_Text body,title,toggleLabel;private readonly RectTransform bodyRect;
    private readonly RecipePlan plan;private string shown;
    // Narrow screens (4:3 tablets, 16:10) start with the panel folded so the kitchen stays large.
    public bool Open { get; private set; }=Screen.height<=0||(float)Screen.width/Screen.height>=1.7f;
    public float Width=>root.gameObject.activeSelf?(Open?OpenWidth:ClosedWidth):0;
    public event Action Toggled;
    private struct Item{public int Section;public string Text;public bool Done;}
    private static readonly string[] Sections={"GATHER","PREPARE","COOK","PLATE"};
    public RecipeStepsPanel(RectTransform safe,RecipePlan plan)
    {
        this.plan=plan;
        root=PrototypeUI.Rect("RecipeStepsPanel",safe,0,0,OpenWidth,0);root.anchorMin=new Vector2(0,0);root.anchorMax=new Vector2(0,1);root.pivot=new Vector2(0,.5f);root.anchoredPosition=Vector2.zero;
        var edge=PrototypeUI.Panel("StepsEdge",root,0,0,0,0,new Color(.93f,.74f,.38f));Stretch(edge.rectTransform,0,0);edge.raycastTarget=false;
        background=PrototypeUI.Panel("StepsBackground",root,0,0,0,0,new Color(.18f,.09f,.035f,.97f));Stretch(background.rectTransform,6,6);
        title=PrototypeUI.Text("StepsTitle",root,"RECIPE STEPS\n"+plan.Dish.ToUpperInvariant(),0,0,OpenWidth-40,90,26);title.richText=true;
        var tr=title.rectTransform;tr.anchorMin=tr.anchorMax=new Vector2(.5f,1);tr.pivot=new Vector2(.5f,1);tr.anchoredPosition=new Vector2(0,-18);title.color=new Color(1,.94f,.78f);title.fontStyle=FontStyles.Bold;
        body=PrototypeUI.Text("StepsBody",root,"",0,0,OpenWidth-44,0,21);body.richText=true;body.alignment=TextAlignmentOptions.TopLeft;body.color=new Color(1,.94f,.78f);
        bodyRect=body.rectTransform;bodyRect.anchorMin=new Vector2(0,0);bodyRect.anchorMax=new Vector2(1,1);bodyRect.offsetMin=new Vector2(24,96);bodyRect.offsetMax=new Vector2(-20,-118);
        body.enableAutoSizing=true;body.fontSizeMin=15;body.fontSizeMax=22;
        Button toggle=PrototypeUI.Button("StepsToggle",root,"HIDE",0,0,OpenWidth-48,62,()=>{Open=!Open;Apply();Toggled?.Invoke();});
        var r=(RectTransform)toggle.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2(0,22);
        toggleLabel=toggle.GetComponentInChildren<TMP_Text>();toggleLabel.fontStyle=FontStyles.Bold;toggleLabel.fontSize=24;Apply();
    }
    private static void Stretch(RectTransform r,float x,float y){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(x,y);r.offsetMax=new Vector2(-x,-y);}
    private void Apply()
    {
        root.sizeDelta=new Vector2(Open?OpenWidth:ClosedWidth,0);body.gameObject.SetActive(Open);title.gameObject.SetActive(Open);
        var r=(RectTransform)toggleLabel.transform.parent;r.sizeDelta=new Vector2((Open?OpenWidth:ClosedWidth)-24,Open?62:180);
        toggleLabel.text=Open?"HIDE STEPS":"S\nT\nE\nP\nS";toggleLabel.fontSize=Open?24:22;
        if(!Open)r.anchoredPosition=new Vector2(0,400);else r.anchoredPosition=new Vector2(0,22);
        shown=null;
    }
    public void SetVisible(bool value){if(root.gameObject.activeSelf!=value){root.gameObject.SetActive(value);Toggled?.Invoke();}}
    public void Render(CookingAttempt a)
    {
        if(!Open||a==null)return;
        List<Item> items=Build(a);int current=items.FindIndex(i=>!i.Done);int section=current<0?3:items[current].Section;
        var sb=new StringBuilder();
        for(int s=0;s<4;s++)
        {
            int total=0,done=0;foreach(var i in items)if(i.Section==s){total++;if(i.Done)done++;}
            bool active=s==section,finished=done==total;
            string head=(s+1)+"  "+Sections[s]+"   "+done+"/"+total;
            sb.Append(active?"<size=125%><color=#FFD54F><b>"+head+"</b></color></size>\n":finished?"<color=#8FD18F><b>"+head+"  done</b></color>\n":"<color=#C9B48F><b>"+head+"</b></color>\n");
            if(!active)continue;
            int n=0;
            for(int k=0;k<items.Count;k++)
            {
                if(items[k].Section!=s)continue;n++;
                if(items[k].Done)sb.Append("<color=#8FD18F>  "+n+". <s>"+items[k].Text+"</s></color>\n");
                else if(k==current)sb.Append("<color=#FFFFFF><b>> "+n+". "+items[k].Text+"</b></color>\n");
                else sb.Append("<color=#E8D9B8>  "+n+". "+items[k].Text+"</color>\n");
            }
            sb.Append("\n");
        }
        string text=sb.ToString();if(text!=shown){shown=text;body.text=text;}
    }
    private List<Item> Build(CookingAttempt a)
    {
        var list=new List<Item>();var p=a.Phase;bool after(CookingPhase x)=>p>x||a.Terminal;
        foreach(string id in plan.Pantry)if(id!=plan.Distractor)list.Add(new Item{Section=0,Text="Tap the "+RecipePlan.Name(id).ToLowerInvariant(),Done=a.HasIngredient(id)||after(CookingPhase.Gather)});
        list.Add(new Item{Section=0,Text="Tap the knife",Done=after(CookingPhase.Gather)});
        for(int i=0;i<plan.Preparation.Length;i++)
        {
            var step=plan.Preparation[i];string t=Sentence(step.Title)+(step.Repetitions>1?" (x"+step.Repetitions+")":"");
            list.Add(new Item{Section=1,Text=t,Done=after(CookingPhase.Chop)||p==CookingPhase.Chop&&a.Preparation.Index>i});
        }
        list.Add(new Item{Section=2,Text="Tap Start cooking (heat 60-85)",Done=after(CookingPhase.HeatPot)});
        for(int i=0;i<plan.Additions.Length;i++)
        {
            list.Add(new Item{Section=2,Text="Add "+RecipePlan.Name(plan.Additions[i]).ToLowerInvariant(),Done=a.AdditionIndex>i});
            if(plan.SearAfter>0&&i==plan.SearAfter-1)
                list.Add(new Item{Section=2,Text=Sentence(plan.SearLabel)+" 4 sec"+(plan.StirAllowed?", stir with sandok":""),Done=a.AdditionIndex>plan.SearAfter||a.AdditionIndex==plan.SearAfter&&p!=CookingPhase.Sear});
        }
        list.Add(new Item{Section=2,Text=Sentence(plan.SimmerLabel)+" 12 sec"+(plan.StirAllowed?"":", do not stir"),Done=p>=CookingPhase.Plate});
        list.Add(new Item{Section=3,Text="Turn the flame OFF",Done=a.Terminal||p==CookingPhase.Plate&&a.Flame<=.01});
        list.Add(new Item{Section=3,Text="Scoop with the sandok onto the plate",Done=a.Terminal});
        return list;
    }
    private static string Sentence(string upper){if(string.IsNullOrEmpty(upper))return upper;string s=upper.ToLowerInvariant();return char.ToUpperInvariant(s[0])+s.Substring(1);}
}
