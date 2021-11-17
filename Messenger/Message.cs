using System;
using System.Buffers.Text;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Messenger
{
    class Program
    {
        static void Main(string[] args)
        {
            Task task = Messenger.getKey("jsb@cs.rit.edu");
            task.Wait();
            Messenger.encryptMessage("jsb@cs.rit.edu");
        }
    }

    class Messenger
    {
        private static readonly HttpClient Client = new HttpClient();
        static BigInteger modInverse(BigInteger a, BigInteger n)
        {
            BigInteger i = n, v = 0, d = 1;
            while (a > 0)
            {
                BigInteger t = i / a, x = a;
                a = BigInteger.ModPow(i, 1, x);
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }
            
            v = BigInteger.ModPow(v, 1, n);
            if (v < 0) v = BigInteger.ModPow((v + n), 1, n);
            return v;
        }

        public void keyGen()
        {
            var curDirPath = Directory.GetCurrentDirectory();
            var publicPath = curDirPath + "\\public.key";
            var privatePath = curDirPath + "\\private.key";
            BigInteger E = 65537;
            
            
        }

        public static void encryptMessage(String email)
        {
            var curDirPath = Directory.GetCurrentDirectory();
            var emailPath = curDirPath + '\\' + email + ".key";
            if (File.Exists(emailPath))
            {
                String json = File.ReadAllText(emailPath);
                var publicKey = JsonConvert.DeserializeObject<PublicKey>(json);
                var encoded = publicKey.key;
                byte[] arr = Convert.FromBase64String(encoded);
                var size = new byte[4];
                Array.Copy(arr, size, 4);
                size = size.Reverse().ToArray();
                var e = BitConverter.ToInt32(size);
                var tempArr = new byte[e];
                Array.Copy(arr, 4, tempArr, 0, e);
                var E = new BigInteger(tempArr);
                Array.Copy(arr, 4 + e, size, 0, 4);
                size = size.Reverse().ToArray();
                var n = BitConverter.ToInt32(size);
                tempArr = new byte[n];
                Array.Copy(arr, 4 + e + 4, tempArr, 0, n);
                var N = new BigInteger(tempArr);
            }
            else
            {
                
            }
        }
        
        public static async Task getKey(string email)
        {
            string curDirPath = Directory.GetCurrentDirectory();
            string emailPath = curDirPath + '\\' + email + ".key";
            try
            {
                var response = await Client.GetAsync("http://kayrun.cs.rit.edu:5000/Key/" + email);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadFromJsonAsync<PublicKey>();
                await using var sw = File.CreateText(emailPath);
                await sw.WriteLineAsync(JsonConvert.SerializeObject(responseBody));
            }
            catch
            {
                Console.WriteLine("Email not on Server!");
            }
        }
    }
}