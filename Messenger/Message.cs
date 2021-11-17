using System;
using System.Buffers.Text;
using System.IO;
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
            Messenger.convertKey("jsb@cs.rit.edu");
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
            //BigInteger E = 6
        }

        public static void convertKey(String email)
        {
            var curDirPath = Directory.GetCurrentDirectory();
            var emailPath = curDirPath + '\\' + email + ".key";
            String json = File.ReadAllText(emailPath);
            var publicKey = JsonConvert.DeserializeObject<PublicKey>(json);
            var encoded = publicKey.key;
            byte[] arr = Convert.FromBase64String(encoded);
            
        }
        
        public static async Task getKey(string email)
        {
            string curDirPath = Directory.GetCurrentDirectory();
            string emailPath = curDirPath + '\\' + email + ".key";
            await using var sw = File.CreateText(emailPath);
            try
            {
                var response = await Client.GetAsync("http://kayrun.cs.rit.edu:5000/Key/" + email);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadFromJsonAsync<PublicKey>();
                await sw.WriteLineAsync(JsonConvert.SerializeObject(responseBody, Formatting.Indented));
            }
            catch
            {
                Console.WriteLine("No Success");
            }
        }
    }
}