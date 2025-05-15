namespace FileStore.Services
{
    public interface IUserService
    {
        Task<bool> RegisterUser(string email, string password, string firstName, string lastName);
    }
}
