namespace Application.ApplicationServiceContract.CrossCutting;
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellation=default);
}