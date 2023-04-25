using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Core.Extensions
{
    public static class StringExtensions
    {
        public static string CalculateSHA256(this string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                StringBuilder hashStringBuilder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    hashStringBuilder.Append(hashBytes[i].ToString("x2"));
                }

                return hashStringBuilder.ToString();
            }
        }
    }
}
