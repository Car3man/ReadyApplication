using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ReadyApplication.Core
{
    public static class RepositoryHelper
    {
        private static readonly HashAlgorithm HashAlgorithm = SHA256.Create();
        
        public static TimeSpan GetDefaultLongTtl()
            => TimeSpan.FromSeconds(60 * 5);
        
        public static TimeSpan GetDefaultShortTtl()
            => TimeSpan.FromSeconds(30);
        
        public static string GetQueryHash(string name, params object[] parameters)
        {
            var keyBuilder = new StringBuilder(256);
            keyBuilder.Append(name);
            foreach (object parameter in parameters)
            {
                if (parameter is IEnumerable<string> enumerableParam)
                {
                    foreach (string item in enumerableParam)
                    {
                        keyBuilder.Append(':').Append(item);
                    }
                }
                else
                {
                    keyBuilder.Append('_').Append(parameter);
                }
            }
            byte[] hashBytes;
            lock (HashAlgorithm)
            {
                hashBytes = HashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(keyBuilder.ToString()));
            }
            return Convert.ToBase64String(hashBytes);
        }
    }
}