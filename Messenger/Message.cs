/*
 * Author: Tristan Hoenninger
 * Program interacts with a server sending keys, getting keys, sending messages, and getting messages.
 * Can generates a keypair of size keysize buts and stores them locally. Can send the public key that was
 * generated in the key generation phase and to the server, the email is also stored in the private key for alter
 * validation. Can retrieve public key for particular user. Can take a plaintext message, encrypt it using the
 * public key of the user you are sending it to, and ehn base64 encode it before sending it to the server.
 * getMsg will retrieve a message for a particular user, and while its possible to download messages of any user
 * the method can only decode messages the this user has the private key for. If user gives invalid command line
 * arguments error messages or the help message will be printed.
*/ 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Messenger
{
    class Program
    {
        /// <summary>
        /// Checks for valid arguments from the command line and if they are invalid
        /// print help message. If arguments are valid program can generates keys,
        /// send keys, get keys, send messages, and get messages.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            try
            {
                Task task;
                if (args.Length is >= 1 and < 4)
                {
                    if (args[0].ToLower() == "sendmsg" && args.Length == 3)
                    {
                        task = Messenger.sendMsg(args[1], args[2]);
                        task.Wait();
                    }
                    else if (args[0].ToLower() == "sendkey" && args.Length == 2)
                    {
                        task = Messenger.sendKey(args[1]);
                        task.Wait();
                    }
                    else if (args[0].ToLower() == "keygen" && args.Length == 2)
                    {
                        task = Messenger.keyGen(Int32.Parse(args[1]));
                        task.Wait();
                    }
                    else if (args[0].ToLower() == "getkey" && args.Length == 2)
                    {
                        task = Messenger.getKey(args[1]);
                        task.Wait();
                    }
                    else if (args[0].ToLower() == "getmsg" && args.Length == 2)
                    {
                        task = Messenger.getMsg(args[1]);
                        task.Wait();
                    }
                    else
                    {
                        helpMsg();
                    }
                }
            }
            catch
            {
                helpMsg();
            }
        }

        /// <summary>
        /// Prints a help message describing the different methods that program can call and use.
        /// </summary>
        static void helpMsg()
        {
            Console.WriteLine("dotnet run <option> <other arguments>\n" +
                "- keyGen keysize - this will generate a keypair of size keysize bits (public and private\n".PadLeft(93) 
                + "keys) and store them locally on the disk (in files called public.key and private.key\n".PadLeft(91) +
                "respectively), in the current directory.\n".PadLeft(47) +
                "- sendKey email - this option sends the public key that was generated in the keyGen\n".PadLeft(88) +
                "phase and to the server. The server will then register this email address as a\n".PadLeft(85) +
                "valid receiver of messages. If the server already has a key for this user,\n".PadLeft(81) +
                "it will be overwritten.\n".PadLeft(30) +
                "- getKey email - this will retrieve public key for a particular user.\n".PadLeft(74) +
                "- sendMsg email plaintext - this will take a plaintext message, encrypt it using the\n".PadLeft(89) +
                "public key of the person you are sending it to, and then bse64 encode it before\n".PadLeft(86) +
                "sending it to the server.\n".PadLeft(32) +
                "- getMsg email - this will retrieve a message for a particular user, while it is possible\n".PadLeft(94)
                + "to download messages for any user, you will only be able to decode messages for\n".PadLeft(86) +
                "which you have the private key.\n".PadLeft(38));
        }
    }

    /// <summary>
    /// Interacts with the server at http://kayrun.cs.rit.edu:5000. Generates keys, gets keys, sends keys,
    /// gets messages, and sends messages. 
    /// </summary>
    class Messenger
    {
        private static readonly HttpClient Client = new HttpClient();

        /// <summary>
        /// Finds the modulus of the inverse of the given BigInteger a using the given BigInteger n.
        /// </summary>
        /// <param name="a">The number the user wants the modulus inverse of.</param>
        /// <param name="n">The number to mod the inverse by</param>
        /// <returns>The modulus of the inverse of a</returns>
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
        /// /// Generates a public key for encryption and generates a private key for
        /// decryption of given keysize bits and stores them in the local directory that the application can access. 
        /// </summary>
        /// <param name="keysize">The number of bits the key pairs will be.</param>
        public static  async Task keyGen(int keysize)
        {
            try
            {
                int remainder = keysize % 16;
                if (remainder != 0)
                {
                    keysize += (16 - remainder);
                }
                var curDirPath = Directory.GetCurrentDirectory();
                var publicPath = curDirPath + "\\public.key";
                var privatePath = curDirPath + "\\private.key";
                PrimeGen prime = new PrimeGen();
                BigInteger bigE = 65537;
                BigInteger p = prime.SetupGen(keysize / 2);
                BigInteger q = prime.SetupGen(keysize / 2);
                BigInteger bigN = p * q;
                BigInteger r = (p - 1) * (q - 1);
                BigInteger bigD = modInverse(bigE, r);
                var dBytes = genBytes(bigD, true);
                var eBytes = genBytes(bigE, true);
                var nBytes = genBytes(bigN, true);
                var d = BitConverter.GetBytes(dBytes.Length);
                var e = BitConverter.GetBytes(eBytes.Length);
                var n = BitConverter.GetBytes(nBytes.Length);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(d);
                    Array.Reverse(e);
                    Array.Reverse(n);
                }

                var eAndE = e.Concat(eBytes).ToArray();
                var dAndD = d.Concat(dBytes).ToArray();
                var nAndN = n.Concat(nBytes).ToArray();
                var publicByte = eAndE.Concat(nAndN).ToArray();
                var privateByte = dAndD.Concat(nAndN).ToArray();

                var publicKey = new PublicKey();
                publicKey.key = Convert.ToBase64String(publicByte);
                var privateKey = new PrivateKey();
                privateKey.key = Convert.ToBase64String(privateByte);
                await using var sw = File.CreateText(publicPath);
                await sw.WriteLineAsync(JsonConvert.SerializeObject(publicKey));
                await using var swWriter = File.CreateText(privatePath);
                await swWriter.WriteLineAsync(JsonConvert.SerializeObject(privateKey));
            }
            catch
            {
                Console.WriteLine("Attempt to write in current directory was unsuccessful!");
                Console.WriteLine("Generate key in different directory.");
            }
        }
        
        /// <summary>
        /// Turns a given BigInteger into a byte array and sets it to little endian if
        /// boolean littleEndian is true or set ot big endian if boolean littleEndian is false. 
        /// </summary>
        /// <param name="num">BigInteger to turn into a byte array</param>
        /// <param name="littleEndian">The wanted endian for the array.If true byte array will be
        /// little endian and if false byte array will be big endian.</param>
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
        /// Encrypts the given message using the public key of the given email to encrypt the message
        /// and sends the message to the given email. 
        /// </summary>
        /// <param name="email">The email the message is to be sent to.</param>
        /// <param name="msg">The message user wants to send.</param>
        public static async Task sendMsg(String email, String msg)
        {
            var curDirPath = Directory.GetCurrentDirectory();
            var emailPath = curDirPath + '\\' + email + ".key";
            if (File.Exists(emailPath))
            {
                try
                {
                    String json = File.ReadAllText(emailPath);
                    var publicKey = JsonConvert.DeserializeObject<PublicKey>(json);
                    if (publicKey != null)
                    {
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
                        var response = await Client.PutAsync("http://kayrun.cs.rit.edu:5000/Message/"
                                                             + email, content);
                        response.EnsureSuccessStatusCode();
                        Console.WriteLine("Message written");
                    }
                    else
                    {
                        Console.WriteLine(email+".key is empty. Please retrieve key again.");
                    }
                }
                catch 
                {
                    Console.WriteLine("Attempt to put message on server was unsuccessful!");
                }
            }
            else
            {
                Console.WriteLine("You do not have that "+email+"'s key! Please download it.");
            }
        }

        /// <summary>
        /// Gets message at a given email if user has the private key for that email.
        /// If user has the private key the message is printed otherwise a warning is printed and
        /// nothing else happens.
        /// </summary>
        /// <param name="email">Email to retrieve a message from.</param>
        public static async Task getMsg(String email)
        {
            string curDirPath = Directory.GetCurrentDirectory();
            var privatePath = curDirPath + "\\private.key";
            if (File.Exists(privatePath))
            {
                try
                {
                    var jsonPrivate = File.ReadAllText(privatePath);
                    var privateKey = JsonConvert.DeserializeObject<PrivateKey>(jsonPrivate);
                    if (privateKey != null)
                    {
                        if (privateKey.emails.Contains(email))
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
                        else
                        {
                            Console.WriteLine("You don't have the private key for " + email);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Private key is empty! Please generate a private key.");
                    }
                }
                catch
                {
                    Console.WriteLine("Attempt to get message from server was not successful!");
                }
            }
            else
            {
                Console.WriteLine("You don't have a private key! Please generate one.");
            }
        }

        /// <summary>
        /// Takes the given string representing a base64 key and extracts E and N or D and N from it.
        /// If E and N are extracted from the given key the given message in the form of a BigInteger
        /// has the encryption algorithm performed on it. If D and N are extracted from the given key then the
        /// given message has the decryption algorithm performed on it.
        /// </summary>
        /// <param name="key">Key string from a public or private key. Extracts E and N from a public key.
        /// Extracts D adn N from a private key.</param>
        /// <param name="message">The message in BigInteger to either encrypt or decrypt.</param>
        /// <returns></returns>
        private static BigInteger changeMessage(String key, BigInteger message)
        {
            byte[] arr = Convert.FromBase64String(key);
            var size = new byte[4];
            Array.Copy(arr, size, 4);
            size = size.Reverse().ToArray();
            var eOrDSize = BitConverter.ToInt32(size);
            var tempArr = new byte[eOrDSize];
            Array.Copy(arr, 4, tempArr, 0, eOrDSize);
            var eOrD = new BigInteger(tempArr);
            Array.Copy(arr, 4 + eOrDSize, size, 0, 4);
            size = size.Reverse().ToArray();
            var n = BitConverter.ToInt32(size);
            tempArr = new byte[n];
            Array.Copy(arr, 4 + eOrDSize + 4, tempArr, 0, n);
            var N = new BigInteger(tempArr);
            var changedMsg = BigInteger.ModPow(message, eOrD, N);
            return changedMsg;
        }

        /// <summary>
        /// Sends the locally stored public to the server at the given email. The given email is added to the
        /// locally stored private key to be used for decrypting messages send to the given email.
        /// </summary>
        /// <param name="email">Email the key is to be sent to on the server.</param>
        public static async Task sendKey(String email)
        {
            var curDirPath = Directory.GetCurrentDirectory();
            var publicPath = curDirPath + "\\public.key";
            var privatePath = curDirPath + "\\private.key";
            if (File.Exists(publicPath) && File.Exists(privatePath))
            {
                try
                {
                    var jsonPublic = File.ReadAllText(publicPath);
                    var publicKey = JsonConvert.DeserializeObject<PublicKey>(jsonPublic);
                    if (publicKey != null)
                    {
                        publicKey.email = email;
                        var generic = JsonConvert.SerializeObject(publicKey);
                        var jsonObject = JsonConvert.DeserializeObject(generic);
                        var content = new StringContent(jsonObject.ToString(), Encoding.UTF8, "application/json");
                        var jsonPrivate = File.ReadAllText(privatePath);
                        var privateKey = JsonConvert.DeserializeObject<PrivateKey>(jsonPrivate);
                        if (privateKey != null)
                        {
                            List<String> tempList;
                            if (privateKey.emails == null)
                            {
                                tempList = new List<string>();
                            }
                            else
                            {
                                tempList = privateKey.emails.ToList();
                            }

                            if (!tempList.Contains(email)) {
                                tempList.Add(email);
                            }
                            privateKey.emails = tempList.ToArray();
                            await using var sw = File.CreateText(privatePath);
                            await sw.WriteLineAsync(JsonConvert.SerializeObject(privateKey));

                            var response = await Client.PutAsync("http://kayrun.cs.rit.edu:5000/Key/"
                                                                 + email, content);
                            response.EnsureSuccessStatusCode();
                            Console.WriteLine("Key saved");
                        }
                        else
                        {
                            Console.WriteLine("Your private key is empty! Please generate a new private key.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Your public key is empty! Please generate a new public key.");
                    }
                }
                catch 
                {
                    Console.WriteLine("Attempt to put key on server was unsuccessful.");
                }
            }
            else
            {
                if (!File.Exists(publicPath))
                {
                    Console.WriteLine("You do not have a public key! Please generate one.");
                } else if (!File.Exists(privatePath))
                {
                    Console.WriteLine("You do not have a private key! Please generate one.");
                }
            }
        }

        /// <summary>
        /// Gets public key of given email from the server.
        /// </summary>
        /// <param name="email">The email the user wants to get a public key from.</param>
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
                Console.WriteLine("Attempt to retrieve key from "+email+" was unsuccessful!");
            }
        }
    }
}