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
            //Task task = Messenger.getKey("jsb@cs.rit.edu");
            //task.Wait();
            
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
            PrimeGen prime = new PrimeGen();
            BigInteger E = 65537;
            BigInteger p = prime.SetupGen(512);
            BigInteger q = prime.SetupGen(512);
            BigInteger N = p * q;
            BigInteger r = (p - 1) * (q - 1);
            BigInteger D = modInverse(E, r);
            var DBytes = genBytes(D, true);
            var EBytes = genBytes(E, true);
            var NBytes = genBytes(N, true);
            var d = BitConverter.GetBytes(DBytes.Length);
            var e = BitConverter.GetBytes(EBytes.Length);
            var n = BitConverter.GetBytes(NBytes.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(d);
                Array.Reverse(e);
                Array.Reverse(n);
            }
            Console.WriteLine(d.Length);
        }

        private byte[] genBytes(BigInteger num, bool littleEndian)
        {
            var big = num.ToByteArray();
            
            if (BitConverter.IsLittleEndian)
            {
                if (!littleEndian)
                {
                    big = big.Reverse().ToArray();
                }
            }
            else
            {
                if (littleEndian)
                {
                    big = big.Reverse().ToArray();
                }
            }
            
            return big;
        }

        public static async Task sendMsg(String email, String Msg)
        {
            var curDirPath = Directory.GetCurrentDirectory();
            var emailPath = curDirPath + '\\' + email + ".key";
            if (File.Exists(emailPath))
            {
                String json = File.ReadAllText(emailPath);
                var publicKey = JsonConvert.DeserializeObject<PublicKey>(json);
                var encodedKey = publicKey.key;
                
            }
            else
            {
                
            }
        }

        private BigInteger changeMessage(String key, BigInteger message)
        {
            byte[] arr = Convert.FromBase64String(key);
            var size = new byte[4];
            Array.Copy(arr, size, 4);
            size = size.Reverse().ToArray();
            var eOrd = BitConverter.ToInt32(size);
            var tempArr = new byte[eOrd];
            Array.Copy(arr, 4, tempArr, 0, eOrd);
            var EOrD = new BigInteger(tempArr);
            Array.Copy(arr, 4 + eOrd, size, 0, 4);
            size = size.Reverse().ToArray();
            var n = BitConverter.ToInt32(size);
            tempArr = new byte[n];
            Array.Copy(arr, 4 + eOrd + 4, tempArr, 0, n);
            var N = new BigInteger(tempArr);
            var changedMsg = BigInteger.ModPow(message, EOrD, N);
            return changedMsg;
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