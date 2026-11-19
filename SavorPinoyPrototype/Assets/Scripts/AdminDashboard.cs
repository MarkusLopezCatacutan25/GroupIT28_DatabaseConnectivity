using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Read-only instructor view. The admin signs in with an email/password account created in the Firebase
// console; access is granted only when admins/{uid} is true (set in the console, never by the app).
// It runs on a separate Firebase app instance, so a player's guest profile on the same device is untouched.
public sealed class AdminDashboard : MonoBehaviour
{
    private const string AdminAppName="SavorPinoyAdmin";
    private FirebaseBootstrap bootstrap;
    private FirebaseAuth auth;private FirebaseDatabase database;
    private RectTransform root,login,board;
    private TMP_InputField email,password;
    private TMP_Text status,summary,dishes,players,pageLabel,who;
    private List<PlayerRow> rows=new List<PlayerRow>();private int page;private bool busy;
    private const int PageSize=10;
    private static readonly Color Parchment=new Color(1,.95f,.80f), Ink=new Color(.16f,.07f,.02f), Edge=new Color(.93f,.74f,.38f);

    private sealed class PlayerRow{public string Name,Uid,Last;public int Attempts,Completed,Best;public HashSet<string> Dishes=new HashSet<string>();}

    public static AdminDashboard Show(Transform canvas,FirebaseSettings settings)
    {
        var existing=canvas.GetComponentInChildren<AdminDashboard>(true);if(existing){existing.gameObject.SetActive(true);existing.transform.SetAsLastSibling();return existing;}
        var panel=PrototypeUI.Panel("AdminDashboard",canvas,0,0,1920,1080,new Color(.08f,.04f,.015f,.97f)).rectTransform;
        var dash=panel.gameObject.AddComponent<AdminDashboard>();dash.bootstrap=new FirebaseBootstrap(settings);dash.Build(panel);return dash;
    }
    private void Build(RectTransform panel)
    {
        root=panel;ScreenFit.Attach(root,1920,1080,true);
        PrototypeUI.Panel("AdminEdge",root,0,0,1780,980,Edge).raycastTarget=false;PrototypeUI.Panel("AdminCard",root,0,0,1764,964,Parchment).raycastTarget=false;
        var title=PrototypeUI.Text("AdminTitle",root,"ADMIN DASHBOARD  (read-only)",0,440,1500,64,40);title.fontStyle=FontStyles.Bold;title.color=Ink;
        status=PrototypeUI.Text("AdminStatus",root,"",0,-440,1500,44,24);status.color=new Color(.55f,.19f,.08f);status.fontStyle=FontStyles.Bold;
        PrototypeUI.Button("AdminClose",root,"Close",760,440,200,64,Close);

        login=PrototypeUI.Rect("AdminLogin",root,0,20,900,520);
        var info=PrototypeUI.Text("AdminInfo",login,"Instructor sign-in. Use the admin email and password created in the Firebase console.",0,200,860,90,26);info.color=Ink;
        Label(login,"Email",0,110);email=PrototypeUI.Input("AdminEmail",login,0,60,700,72);email.characterLimit=120;email.contentType=TMP_InputField.ContentType.EmailAddress;SetPlaceholder(email,"admin@example.com");
        Label(login,"Password",0,-20);password=PrototypeUI.Input("AdminPassword",login,0,-70,700,72);password.characterLimit=64;password.contentType=TMP_InputField.ContentType.Password;password.inputType=TMP_InputField.InputType.Password;SetPlaceholder(password,"Password");
        PrototypeUI.Button("AdminSignIn",login,"Sign in",0,-180,420,80,()=>_=SignIn());

        board=PrototypeUI.Rect("AdminBoard",root,0,0,1700,900);
        who=PrototypeUI.Text("AdminWho",board,"",-560,380,560,40,22);who.alignment=TextAlignmentOptions.Left;who.color=Ink;
        PrototypeUI.Button("AdminRefresh",board,"Refresh",420,380,200,56,()=>_=Load());
        PrototypeUI.Button("AdminSignOut",board,"Sign out",640,380,200,56,SignOut);
        summary=Box(board,"AdminSummary",-560,190,520,300,26);
        dishes=Box(board,"AdminDishes",-560,-180,520,380,22);
        players=Box(board,"AdminPlayers",290,-20,1060,700,22);players.alignment=TextAlignmentOptions.TopLeft;
        PrototypeUI.Button("AdminPrev",board,"< Prev",80,-405,200,56,()=>{page=Mathf.Max(0,page-1);RenderPlayers();});
        PrototypeUI.Button("AdminNext",board,"Next >",500,-405,200,56,()=>{page=Mathf.Min(PageCount-1,page+1);RenderPlayers();});
        pageLabel=PrototypeUI.Text("AdminPage",board,"",290,-405,200,56,22);pageLabel.color=Ink;
        board.gameObject.SetActive(false);
    }
    private static void Label(RectTransform parent,string text,float x,float y){var t=PrototypeUI.Text("Label"+text,parent,text,x,y,700,40,24);t.alignment=TextAlignmentOptions.Left;t.color=Ink;t.fontStyle=FontStyles.Bold;}
    private static void SetPlaceholder(TMP_InputField input,string text){if(input.placeholder is TMP_Text t)t.text=text;}
    private static TMP_Text Box(RectTransform parent,string name,float x,float y,float w,float h,int size)
    {
        PrototypeUI.Panel(name+"Back",parent,x,y,w,h,new Color(.24f,.115f,.055f,.95f)).raycastTarget=false;
        var t=PrototypeUI.Text(name,parent,"",x,y,w-36,h-28,size);t.richText=true;t.color=new Color(1,.94f,.78f);t.alignment=TextAlignmentOptions.TopLeft;return t;
    }
    private int PageCount=>Mathf.Max(1,(rows.Count+PageSize-1)/PageSize);

    private async Task SignIn()
    {
        if(busy)return;string mail=email.text.Trim(),pass=password.text;
        if(mail.Length<5||!mail.Contains("@")||pass.Length<6){status.text="Enter the admin email and password.";return;}
        busy=true;status.text="Signing in...";
        try
        {
            await bootstrap.InitializeAsync();
            FirebaseApp app=FirebaseApp.GetInstance(AdminAppName)??FirebaseApp.Create(bootstrap.App.Options,AdminAppName);
            auth=FirebaseAuth.GetAuth(app);
            if(auth.CurrentUser==null||auth.CurrentUser.IsAnonymous)await FirebaseTaskTimeout.Within(auth.SignInWithEmailAndPasswordAsync(mail,pass));
            database=FirebaseDatabase.GetInstance(app,bootstrap.DatabaseUrl);
            var flag=await FirebaseTaskTimeout.Within(database.RootReference.Child("admins").Child(auth.CurrentUser.UserId).GetValueAsync());
            if(!(flag.Value is bool ok&&ok)){auth.SignOut();status.text="This account is not an admin.";return;}
            password.text="";login.gameObject.SetActive(false);board.gameObject.SetActive(true);
            who.text="Signed in: "+auth.CurrentUser.Email;await Load();
        }
        catch(Exception error){status.text="Sign-in failed. Check the email, password and internet connection.";Debug.LogWarning("Admin sign-in failed: "+error.GetType().Name);}
        finally{busy=false;password.text="";}
    }
    private async Task Load()
    {
        if(database==null)return;status.text="Loading players...";
        try
        {
            DataSnapshot users=await FirebaseTaskTimeout.Within(database.RootReference.Child("users").GetValueAsync(),20000);
            rows=new List<PlayerRow>();
            var perDish=RecipePlan.All.ToDictionary(r=>r.Id,r=>new int[4]); // attempts, completed, score sum, stars sum
            var best=RecipePlan.All.ToDictionary(r=>r.Id,r=>0);
            int attempts=0,completed=0;long scoreSum=0;
            foreach(DataSnapshot user in users.Children)
            {
                var row=new PlayerRow{Uid=user.Key,Name=user.Child("display_name").Value as string??"(no name)",Last=""};
                foreach(DataSnapshot a in user.Child("attempts").Children)
                {
                    string dish=a.Child("dish_id").Value as string;if(dish==null||!perDish.ContainsKey(dish))continue;
                    int score=Convert.ToInt32(a.Child("score").Value??0);bool done=a.Child("is_correct").Value is bool b&&b;
                    string remarks=a.Child("remarks").Value as string??"";int stars=0;int bar=remarks.LastIndexOf('|');if(bar>=0)int.TryParse(remarks.Substring(bar+1).Trim().Split(' ')[0],out stars);
                    string when=a.Child("created_at").Value as string??"";
                    row.Attempts++;attempts++;scoreSum+=score;if(done){row.Completed++;completed++;row.Dishes.Add(dish);}
                    row.Best=Mathf.Max(row.Best,score);if(string.CompareOrdinal(when,row.Last)>0)row.Last=when;
                    var d=perDish[dish];d[0]++;if(done)d[1]++;d[2]+=score;d[3]+=stars;best[dish]=Mathf.Max(best[dish],score);
                }
                rows.Add(row);
            }
            rows=rows.OrderByDescending(r=>r.Last).ToList();page=0;
            int active=rows.Count(r=>r.Attempts>0);
            summary.text="<b><size=120%>OVERVIEW</size></b>\n\nPlayers: <b>"+rows.Count+"</b>   (played: "+active+")\nAttempts saved: <b>"+attempts+"</b>\nDishes completed: <b>"+completed+"</b>\nAverage score: <b>"+(attempts>0?Mathf.RoundToInt(scoreSum/(float)attempts):0)+"</b>";
            var sb=new System.Text.StringBuilder("<b><size=120%>BY DISH</size></b>\n\n");
            foreach(var plan in RecipePlan.All){var d=perDish[plan.Id];sb.Append("<b>"+plan.Dish+"</b>\n  "+d[0]+" attempts, "+d[1]+" completed\n  avg "+(d[0]>0?d[2]/d[0]:0)+" pts, "+(d[0]>0?(d[3]/(float)d[0]).ToString("0.0"):"0")+" stars, best "+best[plan.Id]+"\n");}
            dishes.text=sb.ToString();RenderPlayers();
            status.text="Updated "+DateTime.Now.ToString("h:mm tt")+". Read-only: the dashboard cannot change player data.";
        }
        catch(Exception error){status.text="Could not load players. Check that this account is in admins and the new rules are published.";Debug.LogWarning("Admin load failed: "+error.GetType().Name);}
    }
    private void RenderPlayers()
    {
        var sb=new System.Text.StringBuilder("<b><size=115%>PLAYERS</size></b>   (newest first)\n\n<b>Name                      Attempts   Completed   Dishes   Best    Last played</b>\n");
        foreach(var r in rows.Skip(page*PageSize).Take(PageSize))
        {
            string last=r.Last.Length>=10?r.Last.Substring(0,10):"-";
            sb.Append("<mspace=0.6em>"+Pad(r.Name,24)+Pad(r.Attempts.ToString(),10)+Pad(r.Completed.ToString(),11)+Pad(r.Dishes.Count+"/4",8)+Pad(r.Best.ToString(),7)+last+"</mspace>  <color=#C9B48F>"+AttemptContract.MaskUid(r.Uid)+"</color>\n");
        }
        if(rows.Count==0)sb.Append("No players yet.");
        players.text=sb.ToString();pageLabel.text="Page "+(page+1)+" / "+PageCount;
    }
    private static string Pad(string s,int n){s=s??"";if(s.Length>n-1)s=s.Substring(0,n-2)+"…";return s.PadRight(n);}
    private void SignOut(){auth?.SignOut();database=null;rows.Clear();board.gameObject.SetActive(false);login.gameObject.SetActive(true);status.text="Signed out.";}
    private void Close(){gameObject.SetActive(false);}
    private void OnDestroy(){auth?.SignOut();}
}
