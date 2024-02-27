using System;
using System.IO;

namespace CryptoSoft
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Unknow File.");
                System.Environment.Exit(0);
            } else
            {
                string filePath = args[0];
                Encrypter.CryptFile(filePath);
            }
        }
    }

    public class Encrypter
    {
        public static void CryptFile(string filePath)
        {
            byte[] key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            byte[] buffer = System.IO.File.ReadAllBytes(filePath);

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ key[i % key.Length]);
            }

            System.IO.File.WriteAllBytes(filePath, buffer);
            System.Environment.Exit(0);
        }
    }
}
