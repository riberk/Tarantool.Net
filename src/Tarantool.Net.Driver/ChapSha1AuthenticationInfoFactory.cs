using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Tarantool.Net.Driver
{
    public class ChapSha1AuthenticationInfoFactory : IAuthenticationInfoFactory
    {
        public Task<AuthenticationInfo> Create(string userName, string password, ArraySegment<byte> salt)
        {
            var buffer = new byte[40];
            byte[] step1;
            using (var sha1 = SHA1.Create())
            {
                step1 = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
                var step2 = sha1.ComputeHash(step1);
                Array.Copy(salt.Array, salt.Offset, buffer, 0, 20);
                Array.Copy(step2, 0, buffer, 20, 20);
                var step3 = sha1.ComputeHash(buffer);

                for (var i = 0; i < step1.Length; i++)
                {
                    step1[i] ^= step3[i];
                }
            }
            return Task.FromResult(new AuthenticationInfo(userName, "chap-sha1", step1));
        }
    }
}