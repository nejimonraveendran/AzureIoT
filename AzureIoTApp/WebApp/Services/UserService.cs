using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using WebApp.Models;

namespace WebApp.Services
{
    public class UserService
    {
        private readonly IConfiguration _configuration;

        public UserService(IConfiguration configuration)
        {
            _configuration = configuration;

        }


        public User? FindUserByUserNameAndPassword(string username, string password)
        {
            var userStore = _configuration.GetSection("UserStore:Users").Get<List<User>>();
            var user = userStore.SingleOrDefault(x => x.UserName == username);

            if(user == null) 
                return null;    

            string passwordHash = GetHash(password, user.Salt);

            if(user.PasswordHash!= passwordHash)
                return null;

            return user;
            
        }

        public string GetHash(string password, string salt)
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt); 

            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password!,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 1000,
                numBytesRequested: 32));
        }
    }
}
