using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger
{
    public class PrimeGen
    {
        /// <summary>
        /// Class creates an instance of PrimeNum and generates and prints a specified
        /// amount of prime numbers, the bit length of the numbers, and the time it
        /// took to generate the numbers in a parallel manner.
        /// </summary>
        public static object myLock = new Object();

        private int indexOfNum;
        private List<BigInteger> list;

        /// <summary>
        /// Instantiates a new instance of PrimeNum.
        /// </summary>
        public PrimeGen()
        {
            indexOfNum = 0;
            list = new List<BigInteger>();
        }

        /// <summary>
        /// Generates a random BigInteger and returns it if its even and not divisible by a
        /// prime number within 100 and returns a -1 if it doesn't meet that criteria. 
        /// </summary>
        /// <param name="bitLen">The bit length of the BigInteger to generate</param>
        /// <returns>Returns the generated number if its not even and not divisible by a
        ///  prime number with 100. It returns -1 if these criteria aren't meet.</returns>
        private BigInteger GenerateNum(int bitLen)
        {
            int[] checkers = new int[]
                {2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97};
            var rngCsp = new RNGCryptoServiceProvider();
            int numOfBytes = bitLen / 8;
            byte[] bytes = new byte[numOfBytes];
            rngCsp.GetBytes(bytes);
            var num = new BigInteger(bytes);
            num = BigInteger.Abs(num);
            foreach (var check in checkers)
            {
                if (num % check == 0)
                {
                    return -1;
                }
            }

            return num;
        }

        /// <summary>
        /// Generates and prints a specified number of prime numbers as it finds them and does so in a parallel method.
        /// Cancels the threads once the specified number of prime numbers have been printed.
        /// The ar multiple checks to see if teh thread should be killed to ensure high speeds.
        /// </summary>
        /// <param name="bitLen">The bit length of the BigInteger object(s) that will be generated</param>
        private void FindPrimeNum(int bitLen)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            // Use ParallelOptions instance to store the CancellationToken
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cts.Token;
            try
            {
                Parallel.For(0, Int32.MaxValue, po, (i, state) =>
                    {
                        if (po.CancellationToken.IsCancellationRequested)
                        {
                            po.CancellationToken.ThrowIfCancellationRequested();
                        }
                        else
                        {
                            var num = GenerateNum(bitLen);
                            if (po.CancellationToken.IsCancellationRequested)
                            {
                                po.CancellationToken.ThrowIfCancellationRequested();
                            }
                            if (num != -1)
                            {
                                if (po.CancellationToken.IsCancellationRequested)
                                {
                                    po.CancellationToken.ThrowIfCancellationRequested();
                                }
                                if (num.IsProbablyPrime())
                                {
                                    
                                    lock (myLock)
                                    {
                                        if (po.CancellationToken.IsCancellationRequested)
                                        {
                                            po.CancellationToken.ThrowIfCancellationRequested();
                                        }
                                        else
                                        {
                                            list.Add(num);
                                            cts.Cancel();
                                            po.CancellationToken.ThrowIfCancellationRequested();
                                        }

                                    }
                                }
                            }
                            if (po.CancellationToken.IsCancellationRequested)
                            {
                                po.CancellationToken.ThrowIfCancellationRequested();
                            }
                        }
                    });
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Generates and prints a specified number of prime numbers, prints the bit length of numbers, and
        /// time it took to generate the numbers.
        /// </summary>
        /// <param name="bitLen">The bit length of the BigInteger object(s) that will be generated</param>
        /// <param name="numOfPrime">The number of prime numbers that need to be generated, defaults to 1</param>
        public BigInteger SetupGen(int bitLen)
        {
            FindPrimeNum(bitLen);
            var num = GetIntFromList(indexOfNum);
            indexOfNum++;
            return num;
        }

        private BigInteger GetIntFromList(int i)
        {
            return list[i];
        }
    }
    /// <summary>
        /// Holds extension methods for other classes to use. IsProbablyPrime checks if
        /// the given value is prime using k number with the range of [2, value - 2] against value.
        /// </summary>
        public static class MyExtensions
        {
            /// <summary>
            /// Checks if the given value is prime using k number with the range of [2, value - 2] against value. 
            /// </summary>
            /// <param name="value"> The given BigInteger to check if it's prime</param>
            /// <param name="k">The number of checks done on the number to see if it's prime</param>
            /// <returns>Returns true if teh number is prime and returns false if number is not prime</returns>
            public static bool IsProbablyPrime(this BigInteger value, int k = 10)
            {
                var n = value;
                var d = n - 1;
                var r = 0;
                var bytes = new byte[value.ToByteArray().Length];
                while (d % 2 == 0)
                {
                    d = d / 2;
                    r++;
                }

                for (var i = 0; i < k; i++)
                {
                    BigInteger a = default;
                    do
                    {
                        var gen = new Random();
                        gen.NextBytes(bytes);
                        a = new BigInteger(bytes);
                    } while (a < 2 || a >= n - 2);

                    var x = BigInteger.ModPow(a, d, n);
                    if (x == 1 || x == n - 1)
                    {
                        continue;
                    }

                    for (var j = 0; j < r - 1; j++)
                    {
                        x = BigInteger.ModPow(x, 2, n);
                        if (x == 1)
                        {
                            return false;
                        }
                        else if (x == n - 1)
                        {
                            break;
                        }
                    }

                    if (x != n - 1)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
}
