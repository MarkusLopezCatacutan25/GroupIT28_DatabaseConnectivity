using System.Threading.Tasks;

public interface IChefIdentity
{
    string Uid { get; }
    Task<string> SignInAsync();
}
