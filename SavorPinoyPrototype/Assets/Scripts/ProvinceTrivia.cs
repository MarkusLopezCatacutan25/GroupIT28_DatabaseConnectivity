public sealed class ProvinceQuestion
{
    public readonly string Id,Prompt,Explanation;
    public readonly string[] Options;
    public readonly int Correct;
    public ProvinceQuestion(string id,string prompt,string[] options,int correct,string explanation)
    {Id=id;Prompt=prompt;Options=options;Correct=correct;Explanation=explanation;}
}
// Batangas entries reused from the existing question bank. Quezon sources recorded in docs.
public static class ProvinceTrivia
{
    private static readonly ProvinceQuestion[] batangas={
        new ProvinceQuestion("barako","Which coffee is strongly associated with Batangas?",new[]{"Kapeng Barako","Matcha","Cacao tea","Salabat"},0,"Kapeng Barako is associated with Batangas coffee heritage."),
        new ProvinceQuestion("turmeric","What gives Batangas Adobo sa Dilaw its golden color?",new[]{"Fresh turmeric (luyang dilaw)","Soy sauce","Yellow food coloring","Bell pepper"},0,"Luyang dilaw gives this selected adobo variant its golden color.")};
    private static readonly ProvinceQuestion[] quezon={
        new ProvinceQuestion("habhab_origin","Which Quezon town is associated with Pancit Habhab?",new[]{"Lucban","Taal","Pila","Silang"},0,"Pancit Habhab is a specialty associated with Lucban, Quezon."),
        new ProvinceQuestion("habhab_leaf","What is Pancit Habhab traditionally served on?",new[]{"A bread bowl","A banana leaf","A coconut shell","A clay lid"},1,"The banana leaf is part of the traditional serving style of Pancit Habhab.")};
    public static ProvinceQuestion[] For(string province)=>(ProvinceQuestion[])(province=="Quezon"?quezon:batangas).Clone();
}
