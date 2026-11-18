using System;

// Recipe data only. The same preparation and cooking presenters consume every plan.
public sealed class RecipePlan
{
    public readonly string Id, Province, Dish, Container, Heritage;
    public readonly string[] Pantry, Additions;
    public readonly PreparationStep[] Preparation;
    // Distractor: a pantry item that is not part of this recipe (null when every pantry item is needed).
    public readonly string Distractor;
    // The sear/saute/stir-fry hold happens after this many additions; 0 means no hold.
    public readonly int SearAfter;
    public readonly bool StirAllowed, UsesWok;
    public readonly string SearLabel, SearHint, SimmerLabel, SimmerHint, NoStirMessage, RemarksTag, MainIngredient;
    public bool IsHabhab=>Id=="Quezon_PancitHabhab";
    public int Required=>Distractor==null?Pantry.Length:Pantry.Length-1;
    // Extra additions (oil and seasonings) get extra cooking time in Timed challenge.
    public double CookSeconds=>75+8*Math.Max(0,Additions.Length-4);
    private RecipePlan(string id,string province,string dish,string container,bool wok,string heritage,string[] pantry,string distractor,string[] additions,int searAfter,bool stirAllowed,
        string searLabel,string searHint,string simmerLabel,string simmerHint,string noStir,string remarks,string main,PreparationStep[] preparation)
    {
        Id=id;Province=province;Dish=dish;Container=container;UsesWok=wok;Heritage=heritage;Pantry=pantry;Distractor=distractor;Additions=additions;SearAfter=searAfter;StirAllowed=stirAllowed;
        SearLabel=searLabel;SearHint=searHint;SimmerLabel=simmerLabel;SimmerHint=simmerHint;NoStirMessage=noStir;RemarksTag=remarks;MainIngredient=main;Preparation=preparation;
    }
    public static RecipePlan For(string province)=>province=="Quezon"?Quezon:Batangas;
    public static RecipePlan ById(string id){foreach(var plan in All)if(plan.Id==id)return plan;return Batangas;}
    public static RecipePlan[] InProvince(string province)=>Array.FindAll(All,plan=>plan.Province==province);
    public static bool Supported(string id)=>Array.Exists(All,plan=>plan.Id==id);
    // Go, Grow and Glow food groups (DepEd Order No. 13, s. 2017) shown on each Heritage Card.
    public static string FoodGroups(string id)
    {
        switch(id)
        {
            case "Batangas_AdoboSaDilaw":return Groups("cooking oil","chicken","turmeric, garlic");
            case "Batangas_Lomi":return Groups("miki noodles","pork, egg","onion, garlic");
            case "Quezon_PancitHabhab":return Groups("miki noodles","pork broth","carrot, cabbage");
            case "Quezon_Hardinera":return Groups("cooking oil","pork, egg","onion, bell pepper");
            default:return "";
        }
    }
    private static string Groups(string go,string grow,string glow)=>"<color=#B8860B><b>GO</b></color> "+go+"\n<color=#B22222><b>GROW</b></color> "+grow+"\n<color=#2E7D32><b>GLOW</b></color> "+glow;
    public static string Name(string id)
    {
        switch(id)
        {
            case "garlic":return "Garlic";case "turmeric":return "Luyang dilaw";case "chicken":return "Boneless chicken";case "soy":return "Soy sauce";case "vinegar":return "Native vinegar";
            case "onion":return "Onion";case "carrot_planks":return "Carrot planks";case "vegetables":return "Carrot & cabbage";case "noodles":return "Miki noodles";case "broth":return "Broth";
            case "pork":return "Pork";case "egg":return "Egg";case "bellpepper":return "Red bell pepper";case "tomato_sauce":return "Tomato sauce";case "oil":return "Cooking oil";
            case "salt":return "Salt";case "pepper":return "Ground pepper";case "msg":return "Vetsin (MSG)";case "llanera":return "Llanera";
            default:return id;
        }
    }
    private static PreparationStep Gesture(string id,string ingredient,string title,string instruction,int repeats=1,string target=null)=>new PreparationStep(id,ingredient,title,instruction,false,repeats,target);
    private static PreparationStep Cut(string id,string ingredient,string title,string instruction)=>new PreparationStep(id,ingredient,title,instruction,true,3);
    private static readonly PreparationStep Hands=Gesture("hands","hands","CLEAN HANDS FIRST","Drag the soap to the sink. In a real kitchen, wash with soap and water for at least 20 seconds.",1,"SINK");
    private static readonly PreparationStep Separate=Gesture("separate","garlic","SEPARATE THE GARLIC","Drag the bulb toward the cloves marker to separate it into individual cloves.");
    private static readonly PreparationStep PeelGarlic=Gesture("peel_garlic","garlic","PEEL EACH GARLIC CLOVE","Swipe from the clove toward the skin tray. Remove the papery skin; repeat for each highlighted clove.",3);
    private static readonly PreparationStep GarlicCut=Cut("slice_garlic","garlic","SLICE THE PEELED GARLIC","Place a peeled clove flat-side down. Curl supporting fingertips back; follow the downward guide for even slices.");
    private static readonly PreparationStep PeelOnion=Gesture("peel_onion","onion","PEEL THE ONION","Swipe the dry outer skin toward the peel tray. Work with a stable halved onion on its flat side.",2);
    private static readonly PreparationStep SliceOnion=Cut("slice_onion","onion","SLICE THE ONION","Keep the halved onion flat-side down, tuck fingertips back and slice evenly along the guide.");
    private static readonly PreparationStep CleanAfterMeat=Gesture("clean_after_meat","hands","CLEAN AFTER RAW MEAT","Drag the cleaning kit to the sink. In a real kitchen wash hands, knife and board thoroughly before touching cooked food.",1,"SINK");
    private static readonly PreparationStep BeatEgg=Gesture("beat_egg","egg","BEAT THE EGG","Drag the egg to the mixing bowl. Crack it on a flat surface, then beat until the yolk and white are mixed.",1,"MIXING BOWL");

    public static readonly RecipePlan Batangas=new RecipePlan("Batangas_AdoboSaDilaw","Batangas","Adobo sa Dilaw","PALAYOK",false,
        "Turmeric gives this selected Batangas adobo variant its golden color. Recipes vary between households. This lesson uses boneless chicken for beginner portioning. Game heat and seconds are abstract; real chicken requires a food thermometer reading of 74 C / 165 F.",
        new[]{"garlic","turmeric","chicken","soy"},"soy",new[]{"oil","garlic","turmeric","chicken","vinegar","salt","pepper"},4,false,
        "SEAR","Sear the chicken at heat 60-85 for 4 simulated seconds, then add the native vinegar.",
        "SIMMER","Simulated vinegar simmer. Real chicken must reach 74 C / 165 F using a food thermometer.",
        "Premature stirring: leave this recipe's initial vinegar simmer unstirred. -500 points.","batangas_prep_v3","chicken",
        new[]{Hands,Separate,PeelGarlic,GarlicCut,
        Gesture("wash_turmeric","turmeric","CLEAN THE TURMERIC","Drag the turmeric to the sink. In a real kitchen, scrub it under running water before peeling.",1,"SINK"),
        Gesture("peel_turmeric","turmeric","PEEL THE TURMERIC","Use a spoon edge to scrape the thin skin away. Swipe toward the peel tray.",3),
        Cut("slice_turmeric","turmeric","SLICE THE TURMERIC","Place on a stable flat side. Keep fingertips curled back and slice thinly along the guide."),
        Gesture("meat_board","chicken","USE A SEPARATE MEAT BOARD","Drag the raw-meat board to the station. Do not wash raw chicken; keep it away from prepared vegetables."),
        Cut("portion_chicken","chicken","PORTION BONELESS CHICKEN","Use a separate board. Cut boneless chicken into similar bite-size portions, keeping fingers away from the blade. This lesson does not teach cutting through bones."),
        CleanAfterMeat});

    public static readonly RecipePlan Lomi=new RecipePlan("Batangas_Lomi","Batangas","Lomi Batangas","KAWALI",true,
        "Lomi became a signature dish of Lipa, Batangas, where a family panciteria began serving it in 1968. Batangas lomi uses thick miki egg noodles in a thick broth and, unlike many other versions, traditionally has no vegetables. A beaten egg is stirred in just before serving. Game heat and seconds are abstract; in a real kitchen cook pork until no pink remains.",
        new[]{"garlic","onion","pork","vegetables"},"vegetables",new[]{"oil","garlic","onion","pork","broth","salt","pepper","msg","noodles","egg"},4,true,
        "SAUTE","Saute the pork at heat 60-85 for 4 simulated seconds, then add the broth.",
        "SIMMER","Simmer so the noodles cook and the egg sets in the thick broth. Simulated progress.",
        "","batangas_lomi_v1","pork",
        new[]{Hands,Separate,PeelGarlic,GarlicCut,PeelOnion,SliceOnion,
        Gesture("meat_board_pork","pork","USE A SEPARATE MEAT BOARD","Drag the raw-meat board to the station. Keep raw pork away from prepared vegetables."),
        Cut("slice_pork","pork","SLICE THE PORK","Lay the pork flat on the meat board. Keep fingertips curled back and cut thin, even strips along the guide."),
        CleanAfterMeat,BeatEgg});

    public static readonly RecipePlan Quezon=new RecipePlan("Quezon_PancitHabhab","Quezon","Pancit Habhab","WOK",true,
        "Pancit Habhab is associated with Lucban, Quezon and traditionally served on banana leaves. This prototype demonstrates a simplified vegetable adaptation, not the complete traditional meat recipe. Prepare with instructor supervision and use the noodle package's cooking directions in a real kitchen.",
        new[]{"garlic","onion","vegetables","noodles"},null,new[]{"oil","garlic","onion","vegetables","broth","salt","pepper","msg","noodles"},4,true,
        "STIR-FRY","Stir-fry the vegetables at heat 60-85 for 4 simulated seconds, then add the broth.",
        "SIMMER","Cook noodles in broth. Simulated progress; follow real package directions outside the game.",
        "","quezon_habhab_v2","noodles",
        new[]{Hands,Separate,PeelGarlic,GarlicCut,PeelOnion,SliceOnion,
        Gesture("wash_vegetables","vegetables","WASH THE VEGETABLES","Drag the carrot and cabbage to the sink. Rinse produce under running water before cutting.",1,"SINK"),
        Gesture("peel_carrot","carrot","TRIM AND PEEL THE CARROT","First swipe removes the leafy top. Second swipe peels the skin. Peel away from your supporting hand.",2),
        Cut("plank_carrot","carrot","SLICE CARROT INTO PLANKS","Use a stable flat carrot section, with supporting fingertips curled back. Make thin, even planks along the guide."),
        Cut("slice_carrot","carrot_planks","CUT CARROT PLANKS INTO STRIPS","Lay the flat planks securely on the board. Slice into narrow strips, keeping fingertips behind the blade."),
        Cut("shred_cabbage","cabbage","SHRED THE CABBAGE","Use a stable cabbage wedge on its flat side. Curl fingertips back and make thin slices along the guide.")});

    public static readonly RecipePlan Hardinera=new RecipePlan("Quezon_Hardinera","Quezon","Hardinera","KAWALI",true,
        "Hardinera is a fiesta dish from Lucban, Quezon. Diced pork is cooked in tomato sauce, topped with bell pepper and egg, and steamed in a llanera, the oval tin mold also used for leche flan. This lesson simplifies the recipe; real hardinera is steamed for about an hour until firm.",
        new[]{"pork","onion","bellpepper","egg"},null,new[]{"oil","onion","pork","tomato_sauce","salt","pepper","bellpepper","egg"},3,false,
        "SAUTE","Saute the pork at heat 60-85 for 4 simulated seconds, then add the tomato sauce.",
        "STEAM","Steam the filled llanera until set. Simulated progress; real hardinera steams for about an hour.",
        "Leave the llanera undisturbed while it steams. -500 points.","quezon_hardinera_v1","pork",
        new[]{Hands,PeelOnion,
        Cut("dice_onion","onion","DICE THE ONION","Keep the halved onion flat-side down, tuck fingertips back and cut small, even pieces along the guide."),
        Gesture("wash_pepper","bellpepper","WASH THE BELL PEPPER","Drag the bell pepper to the sink. Rinse it under running water, then remove the seeds.",1,"SINK"),
        Cut("dice_pepper","bellpepper","DICE THE BELL PEPPER","Lay the pepper skin-side down. Keep fingertips curled back and cut small squares along the guide."),
        Gesture("meat_board_pork","pork","USE A SEPARATE MEAT BOARD","Drag the raw-meat board to the station. Keep raw pork away from prepared vegetables."),
        Cut("dice_pork","pork","DICE THE PORK","Cut the pork into small, even cubes on the meat board, keeping fingers away from the blade."),
        CleanAfterMeat,BeatEgg,
        Gesture("line_llanera","llanera","PREPARE THE LLANERA","Drag the llanera to the station. Brush it with a little oil and line it with banana leaf so the hardinera comes out cleanly.",1,"STATION")});

    public static readonly RecipePlan[] All={Batangas,Lomi,Quezon,Hardinera};
}
