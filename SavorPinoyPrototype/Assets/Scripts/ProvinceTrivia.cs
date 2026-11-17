public sealed class ProvinceQuestion
{
    public readonly string Id,Prompt,Explanation;
    public readonly string[] Options;
    public readonly int Correct;
    public ProvinceQuestion(string id,string prompt,string[] options,int correct,string explanation)
    {Id=id;Prompt=prompt;Options=options;Correct=correct;Explanation=explanation;}
}
// Two questions per dish; one is drawn per attempt. Sources recorded in the project docs.
public static class ProvinceTrivia
{
    private static readonly ProvinceQuestion[] adobo={
        new ProvinceQuestion("barako","Which coffee is strongly associated with Batangas?",new[]{"Kapeng Barako","Matcha","Cacao tea","Salabat"},0,"Kapeng Barako is associated with Batangas coffee heritage."),
        new ProvinceQuestion("turmeric","What gives Batangas Adobo sa Dilaw its golden color?",new[]{"Fresh turmeric (luyang dilaw)","Soy sauce","Yellow food coloring","Bell pepper"},0,"Luyang dilaw gives this selected adobo variant its golden color.")};
    private static readonly ProvinceQuestion[] lomi={
        new ProvinceQuestion("lomi_city","Which Batangas city is famous as the home of lomi?",new[]{"Lipa","Lucban","Taal","Tagaytay"},0,"Lomi became a signature dish of Lipa, Batangas, where a family panciteria began serving it in 1968."),
        new ProvinceQuestion("lomi_egg","What is stirred into Batangas lomi just before serving?",new[]{"Beaten egg","Coconut milk","Calamansi juice","Brown sugar"},0,"A beaten egg is stirred into the thick broth just before serving.")};
    private static readonly ProvinceQuestion[] habhab={
        new ProvinceQuestion("habhab_origin","Which Quezon town is associated with Pancit Habhab?",new[]{"Lucban","Taal","Pila","Silang"},0,"Pancit Habhab is a specialty associated with Lucban, Quezon."),
        new ProvinceQuestion("habhab_leaf","What is Pancit Habhab traditionally served on?",new[]{"A bread bowl","A banana leaf","A coconut shell","A clay lid"},1,"The banana leaf is part of the traditional serving style of Pancit Habhab.")};
    private static readonly ProvinceQuestion[] hardinera={
        new ProvinceQuestion("hardinera_mold","Hardinera is traditionally steamed in which oval tin mold?",new[]{"Llanera","Palayok","Bilao","Kawali"},0,"Hardinera is steamed in a llanera, the oval tin mold also used for leche flan."),
        new ProvinceQuestion("hardinera_town","Hardinera is a fiesta dish from which Quezon town?",new[]{"Lucban","Lipa","Nasugbu","Calamba"},0,"Hardinera is a fiesta dish from Lucban, Quezon.")};
    public static ProvinceQuestion[] For(string province)=>(ProvinceQuestion[])(province=="Quezon"?habhab:adobo).Clone();
    public static ProvinceQuestion[] ForDish(string dishId)
    {
        switch(dishId){case "Batangas_Lomi":return (ProvinceQuestion[])lomi.Clone();case "Quezon_PancitHabhab":return (ProvinceQuestion[])habhab.Clone();case "Quezon_Hardinera":return (ProvinceQuestion[])hardinera.Clone();default:return (ProvinceQuestion[])adobo.Clone();}
    }
}
