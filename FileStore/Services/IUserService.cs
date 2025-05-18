using FileStore.Models;


namespace FileStore.Services
{
    public interface IUserService
    {
        Task<bool> RegisterUser(string email, string firstName, string lastName, string password);

        Task<User> ValidateUserCredentials(string email, string password);

        IEnumerable<User> GetAllUsers();
    }
}
