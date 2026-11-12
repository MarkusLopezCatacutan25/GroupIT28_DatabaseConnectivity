using System;

// Recipe data only. The same preparation and cooking presenters consume both plans.
public sealed class RecipePlan
{
    public readonly string Id, Province, Dish, Container, Heritage;
    public readonly string[] Pantry, Additions;
    public readonly PreparationStep[] Preparation;
    public bool IsHabhab=>Id=="Quezon_PancitHabhab";
    private RecipePlan(string id,string province,string dish,string container,string heritage,string[] pantry,string[] additions,PreparationStep[] preparation)
    {Id=id;Province=province;Dish=dish;Container=container;Heritage=heritage;Pantry=pantry;Additions=additions;Preparation=preparation;}
    public static RecipePlan For(string province)=>province=="Quezon"?Quezon:Batangas;
    public static bool Supported(string id)=>id==Batangas.Id||id==Quezon.Id;
    public static string Name(string id)
    {
        switch(id){case "garlic":return "Garlic";case "turmeric":return "Luyang dilaw";case "chicken":return "Boneless chicken";case "soy":return "Soy sauce";case "vinegar":return "Native vinegar";case "onion":return "Onion";case "carrot_planks":return "Carrot planks";case "vegetables":return "Carrot & cabbage";case "noodles":return "Miki noodles";case "broth":return "Seasoned vegetable broth";default:return id;}
    }
    private static PreparationStep Gesture(string id,string ingredient,string title,string instruction,int repeats=1)=>new PreparationStep(id,ingredient,title,instruction,false,repeats);
    private static PreparationStep Cut(string id,string ingredient,string title,string instruction)=>new PreparationStep(id,ingredient,title,instruction,true,3);
    private static readonly PreparationStep Hands=Gesture("hands","hands","CLEAN HANDS FIRST","Drag the soap to the hand marker. In a real kitchen, wash with soap and water for at least 20 seconds.");
    private static readonly PreparationStep Separate=Gesture("separate","garlic","SEPARATE THE GARLIC","Drag the bulb toward the cloves marker to separate it into individual cloves.");
    private static readonly PreparationStep PeelGarlic=Gesture("peel_garlic","garlic","PEEL EACH GARLIC CLOVE","Swipe from the clove toward the skin tray. Remove the papery skin; repeat for each highlighted clove.",3);
    private static readonly PreparationStep GarlicCut=Cut("slice_garlic","garlic","SLICE THE PEELED GARLIC","Place a peeled clove flat-side down. Curl supporting fingertips back; follow the downward guide for even slices.");
    public static readonly RecipePlan Batangas=new RecipePlan("Batangas_AdoboSaDilaw","Batangas","Adobo sa Dilaw","PALAYOK",
        "Turmeric gives this selected Batangas adobo variant its golden color. Recipes vary between households. This lesson uses boneless chicken for beginner portioning. Game heat and seconds are abstract; real chicken requires a food thermometer reading of 74 C / 165 F.",
        new[]{"garlic","turmeric","chicken","soy"},new[]{"garlic","turmeric","chicken","vinegar"},
        new[]{Hands,Separate,PeelGarlic,GarlicCut,
        Gesture("wash_turmeric","turmeric","CLEAN THE TURMERIC","Drag turmeric to the water marker. In a real kitchen, scrub it under running water before peeling."),
        Gesture("peel_turmeric","turmeric","PEEL THE TURMERIC","Use a spoon edge to scrape the thin skin away. Swipe toward the peel tray.",3),
        Cut("slice_turmeric","turmeric","SLICE THE TURMERIC","Place on a stable flat side. Keep fingertips curled back and slice thinly along the guide."),
        Gesture("meat_board","chicken","USE A SEPARATE MEAT BOARD","Drag the raw-meat board to the station. Do not wash raw chicken; keep it away from prepared vegetables."),
        Cut("portion_chicken","chicken","PORTION BONELESS CHICKEN","Use a separate board. Cut boneless chicken into similar bite-size portions, keeping fingers away from the blade. This lesson does not teach cutting through bones."),
        Gesture("clean_after_meat","hands","CLEAN AFTER RAW CHICKEN","Drag the cleaning kit to the station. In a real kitchen wash hands, knife and board thoroughly before touching cooked food.")});
    public static readonly RecipePlan Quezon=new RecipePlan("Quezon_PancitHabhab","Quezon","Pancit Habhab","WOK",
        "Pancit Habhab is associated with Lucban, Quezon and traditionally served on banana leaves. This prototype demonstrates a simplified vegetable adaptation, not the complete traditional meat recipe. Prepare with instructor supervision and use the noodle package's cooking directions in a real kitchen.",
        new[]{"garlic","onion","vegetables","noodles"},new[]{"garlic","onion","vegetables","broth","noodles"},
        new[]{Hands,Separate,PeelGarlic,GarlicCut,
        Gesture("peel_onion","onion","PEEL THE ONION","Swipe the dry outer skin toward the peel tray. Work with a stable halved onion on its flat side.",2),
        Cut("slice_onion","onion","SLICE THE ONION","Keep the halved onion flat-side down, tuck fingertips back and slice evenly along the guide."),
        Gesture("wash_vegetables","vegetables","CLEAN THE VEGETABLES","Drag the carrot and cabbage to the water marker. Rinse produce under running water before cutting."),
        Gesture("peel_carrot","carrot","PEEL THE CARROT","Swipe the peeler away from your supporting hand toward the peel tray.",2),
        Cut("plank_carrot","carrot","SLICE CARROT INTO PLANKS","Use a stable flat carrot section, with supporting fingertips curled back. Make thin, even planks along the guide."),
        Cut("slice_carrot","carrot_planks","CUT CARROT PLANKS INTO STRIPS","Lay the flat planks securely on the board. Slice into narrow strips, keeping fingertips behind the blade."),
        Cut("shred_cabbage","cabbage","SHRED THE CABBAGE","Use a stable cabbage wedge on its flat side. Curl fingertips back and make thin slices along the guide.")});
}
