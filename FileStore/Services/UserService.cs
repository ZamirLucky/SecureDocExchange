
using FileStore.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
//using Microsoft.CodeAnalysis.Editing;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace FileStore.Services
{
    public class UserService : IUserService
    {
        // In memory store for users  
        private static readonly ConcurrentDictionary<string, User> _users =
            new ConcurrentDictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        public UserService()
        {
            // Pre-seed demo user: demo@example.com / Demo User / Pass123!  
            string demoPassword = "Pass123!";
            using var sha256Hash = SHA256.Create();

            var demo = new User
            {
                Email = "demo@example.com",
                FirstName = "Demo",
                LastName = "User",
                Password = GetHash(sha256Hash, demoPassword)
            };
            _users[demo.Email] = demo;
        }

        public Task<bool> RegisterUser(string email, string firstName, string lastName, string password)
        {

            using var sha256Hash = SHA256.Create();
            string passwordHash = GetHash(sha256Hash, password);

            var user = new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Password = passwordHash
            };

            // Add user to the in-memory store
            var added = _users.TryAdd(email, user);
            return Task.FromResult(added);
        }

        public Task<User> ValidateUserCredentials(string email, string password)
        {
            if (_users.TryGetValue(email, out var user))
            {
                using var sha256Hash = SHA256.Create();
                string passwordHash = GetHash(sha256Hash, password);
                if (string.Equals(passwordHash, user.Password, StringComparison.Ordinal))
                    return Task.FromResult(user);
            }
            return Task.FromResult<User>(null);
            //throw new InvalidOperationException("Invalid user credentials.");
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _users.Values;
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        
    }
       
}
