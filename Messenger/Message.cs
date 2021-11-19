using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
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
            Task task;
            if (args.Length is >= 1 and < 4)
            {
                switch (args[0].ToLower())
                {
                    case "sendmsg":
                        task = Messenger.sendMsg(args[1], args[2]);
                        task.Wait();
                        break;
                    case "sendkey":
                        task = Messenger.sendKey(args[1]);
                        task.Wait();
                        break;
                    case "keygen":
                        Messenger.keyGen();
                        break;
                    case "getkey":
                        task = Messenger.getKey(args[1]);
                        task.Wait();
                        break;
                    case "getmsg":
                        task = Messenger.getMsg(args[1]);
                        task.Wait();
                        break;
                    default:
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    class Messenger
    {
        private static readonly HttpClient Client = new HttpClient();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="n"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        public static void keyGen()
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

            var e_and_E = e.Concat(EBytes).ToArray();
            var d_and_D = d.Concat(DBytes).ToArray();
            var n_and_N = n.Concat(NBytes).ToArray();
            var publicByte = e_and_E.Concat(n_and_N).ToArray();
            var privateByte = d_and_D.Concat(n_and_N).ToArray();

            var publicKey = new PublicKey();
            publicKey.key = Convert.ToBase64String(publicByte);
            var privateKey = new PrivateKey();
            privateKey.key = Convert.ToBase64String(privateByte);

            using var sw = File.CreateText(publicPath);
            sw.WriteLineAsync(JsonConvert.SerializeObject(publicKey));
            using var swWriter = File.CreateText(privatePath);
            swWriter.WriteLineAsync(JsonConvert.SerializeObject(privateKey));
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="num"></param>
        /// <param name="littleEndian"></param>
        /// <returns></returns>
        private static byte[] genBytes(BigInteger num, bool littleEndian)
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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="msg"></param>
        public static async Task sendMsg(String email, String msg)
        {
            var curDirPath = Directory.GetCurrentDirectory();
            var emailPath = curDirPath + '\\' + email + ".key";
            if (File.Exists(emailPath))
            {
                String json = File.ReadAllText(emailPath);
                var publicKey = JsonConvert.DeserializeObject<PublicKey>(json);
                var encodedKey = publicKey.key;
                var msgByte = Encoding.ASCII.GetBytes(msg);
                var m = new BigInteger(msgByte);
                var encryptM = changeMessage(encodedKey, m);
                var encryptByte = genBytes(encryptM, true);
                var encryptMsg = Convert.ToBase64String(encryptByte);
                var newMsg = new Message();
                newMsg.email = email;
                newMsg.content = encryptMsg;
                var generic = JsonConvert.SerializeObject(newMsg);
                var jsonObject = JsonConvert.DeserializeObject(generic);
                var content = new StringContent(jsonObject.ToString(), Encoding.UTF8, "application/json");
                try
                {
                    var response = await Client.PutAsync("http://kayrun.cs.rit.edu:5000/Message/"
                                                         + email, content);
                    response.EnsureSuccessStatusCode();
                }
                catch
                {
                    Console.WriteLine("Attempt to put message on server was unsuccessful.");
                }
            }
            else
            {
                Console.WriteLine("You do not have that email's key! Please download it.");
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        public static async Task getMsg(String email)
        {
            string curDirPath = Directory.GetCurrentDirectory();
            var privatePath = curDirPath + "\\private.key";
            var jsonPrivate = File.ReadAllText(privatePath);
            var privateKey = JsonConvert.DeserializeObject<PrivateKey>(jsonPrivate);
            if (privateKey.emails.Contains(email))
            {
                try
                {
                    var response = await Client.GetAsync("http://kayrun.cs.rit.edu:5000/Message/" + email);
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadFromJsonAsync<Message>();
                    var msgByte = Convert.FromBase64String(responseBody.content);
                    var msgInt = new BigInteger(msgByte);
                    var decodedInt = changeMessage(privateKey.key, msgInt);
                    var decodedByte = decodedInt.ToByteArray();
                    var msg = Encoding.ASCII.GetString(decodedByte);
                    Console.WriteLine(msg);
                }
                catch
                {
                    Console.WriteLine("Email not on Server!");
                }       
            }
            else
            {
                Console.WriteLine("You don't have the private key for this message.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        public static async Task sendKey(String email)
        {
            var curDirPath = Directory.GetCurrentDirectory();
            var publicPath = curDirPath + "\\public.key";
            var privatePath = curDirPath + "\\private.key";
            if (File.Exists(publicPath))
            {
                var jsonPublic = File.ReadAllText(publicPath);
                var publicKey = JsonConvert.DeserializeObject<PublicKey>(jsonPublic);
                publicKey.email = email;
                var generic = JsonConvert.SerializeObject(publicKey);
                var jsonObject = JsonConvert.DeserializeObject(generic);
                var content = new StringContent(jsonObject.ToString(), Encoding.UTF8, "application/json");
                var jsonPrivate = File.ReadAllText(privatePath);
                var privateKey = JsonConvert.DeserializeObject<PrivateKey>(jsonPrivate);
                List<String> tempList;
                if (privateKey.emails == null)
                {
                    tempList = new List<string>();
                }
                else
                {
                    tempList = privateKey.emails.ToList();
                }
                tempList.Add(email);
                privateKey.emails = tempList.ToArray();
                await using var sw = File.CreateText(privatePath);
                await sw.WriteLineAsync(JsonConvert.SerializeObject(privateKey));
                try
                {
                    var response = await Client.PutAsync("http://kayrun.cs.rit.edu:5000/Key/"
                                                         + email, content);
                    response.EnsureSuccessStatusCode();
                }
                catch
                {
                    Console.WriteLine("Attempt to put key on server was unsuccessful.");
                }
            }
            else
            {
                Console.WriteLine("You do not have a public key! Please generate one.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static BigInteger changeMessage(String key, BigInteger message)
        {
            byte[] arr = Convert.FromBase64String(key);
            var size = new byte[4];
            Array.Copy(arr, size, 4);
            size = size.Reverse().ToArray();
            var e_Or_d = BitConverter.ToInt32(size);
            var tempArr = new byte[e_Or_d];
            Array.Copy(arr, 4, tempArr, 0, e_Or_d);
            var eOrD = new BigInteger(tempArr);
            Array.Copy(arr, 4 + e_Or_d, size, 0, 4);
            size = size.Reverse().ToArray();
            var n = BitConverter.ToInt32(size);
            tempArr = new byte[n];
            Array.Copy(arr, 4 + e_Or_d + 4, tempArr, 0, n);
            var N = new BigInteger(tempArr);
            var changedMsg = BigInteger.ModPow(message, eOrD, N);
            return changedMsg;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
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