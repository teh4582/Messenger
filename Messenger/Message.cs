using System;
using System.Numerics;

namespace Messenger
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            PrimeGen prime = new PrimeGen();
            prime.SetupGen(2048);
            Console.WriteLine(prime.GetIntFromList(0));
            prime.SetupGen(1024);
            Console.WriteLine(prime.GetIntFromList(1));
        }
    }
}