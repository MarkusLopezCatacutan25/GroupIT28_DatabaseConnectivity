using System.Threading.Tasks;

public interface IAttemptRepository
{
    Task WriteAsync(PendingChefAttempt item);
    Task<AttemptRecord> ReadAsync(string uid,string attemptId);
}
