﻿using ECExperiments.Bitcoin;
using ECExperiments.ECC;
using System.Numerics;
using System.Security.Cryptography;

namespace ECExperiments
{
    internal class Program
    {
        private const string PLAYGROUND = "playground";
        private const string WIF_PARSER = "wifparser";
        private const string MAKE_KEY = "makekey";
        private const string SIGNER = "signer";
        private const string VALIDATOR = "validator";

        private static readonly string[] EXPERIMENTS = new string[]
        {
            PLAYGROUND,
            WIF_PARSER,
            MAKE_KEY,
            SIGNER,
            VALIDATOR
        };

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string experiment = args[0]?.ToLowerInvariant()?.Trim();

                if (!EXPERIMENTS.Contains(experiment))
                {
                    Console.WriteLine("Experiment not recognized.");
                }

                args = args.Skip(1).ToArray();

                RunExperiment(experiment, args);

                return;
            }

            while (true)
            {
                try
                {
                    if (!ReadExperiment(out string experiment))
                    {
                        return;
                    }

                    RunExperiment(experiment, new string[] { });

                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static bool ReadExperiment(out string experiment)
        {
            Console.WriteLine("Select an experiment or enter 'exit' to leave?");
            Console.WriteLine("    " + PLAYGROUND);
            Console.WriteLine("    " + WIF_PARSER);
            Console.WriteLine("    " + MAKE_KEY);
            Console.WriteLine("    " + SIGNER);
            Console.WriteLine("    " + VALIDATOR);

            experiment = null;

            do
            {
                if (experiment != null)
                {
                    Console.WriteLine("Value not recognized. Please enter a valid experiment name.");
                }

                Console.Write("Selection: ");
                experiment = Console.ReadLine()?.ToLowerInvariant()?.Trim() ?? string.Empty;

                if (experiment == "exit")
                {
                    return false;
                }
            }
            while (!EXPERIMENTS.Contains(experiment));

            return true;
        }

        private static void RunExperiment(string experiment, string[] args)
        {
            switch (experiment)
            {
                case PLAYGROUND:
                    PointPlayground playground = new PointPlayground();
                    playground.Run();
                    break;
                case WIF_PARSER:
                    ParseAndPrintWIF(args);
                    break;
                case MAKE_KEY:
                    MakeKey();
                    break;
                case SIGNER:
                    MakeSignature(args);
                    break;
                case VALIDATOR:
                    ValidateSignature(args);
                    break;
            }
        }

        private static void ParseAndPrintWIF(string[] args)
        {
            string wif;
            if(args.Length > 0)
            {
                wif = args[0];
            }
            else
            {
                Console.WriteLine("Enter a Bitcoin private key in Wallet Import Format (WIF):");
                wif = Console.ReadLine().Trim();
            }

            bool publicKeyCompressed = wif.StartsWith("K") || wif.StartsWith("L") || wif.StartsWith("M");
            Console.WriteLine($"Public key is {(publicKeyCompressed ? "compressed" : "uncompressed")}");

            BigInteger privateKey = WIFUtils.WIFToPrivateKey(wif);
            Console.WriteLine("Private Key:");
            Console.WriteLine("    " + Convert.ToHexString(Utils.MakeUnsignedBigEndianArray(privateKey)));
            Console.WriteLine();

            ECEncryptor encryptor = new ECEncryptor(WeierstrasCurve.secp256k1);
            encryptor.SetPrivateKey(privateKey);
            BigPoint publicKey = encryptor.GetPublicKey();

            Console.WriteLine("Public Key as Point:");
            Console.WriteLine($"    X: {Convert.ToHexString(Utils.MakeUnsignedBigEndianArray(publicKey.X))}");
            Console.WriteLine($"    Y: {Convert.ToHexString(Utils.MakeUnsignedBigEndianArray(publicKey.Y))}");
            Console.WriteLine();

            byte[] publicKeyData;
            if (publicKeyCompressed)
            {
                publicKeyData = encryptor.ExportPublicKeyCompressed();
            }
            else
            {
                publicKeyData = encryptor.ExportPublicKey();
            }

            Console.WriteLine("Public Key as Byte Array:");
            Console.WriteLine("    " + Convert.ToHexString(publicKeyData));
            Console.WriteLine();

            string address = WIFUtils.CreateBitcoinAddressString(publicKeyData);

            Console.WriteLine("Wallet Address:");
            Console.WriteLine("    " + address);
        }

        private static void MakeKey()
        {
            ECEncryptor encryptor = new ECEncryptor(WeierstrasCurve.secp256k1);

            BigInteger privateKey = encryptor.GenerateRandomPrivateKey();
            encryptor.SetPrivateKey(privateKey);

            byte[] privateKeyData = encryptor.ExportPrivateKey();

            Console.WriteLine("Private Key:");
            Console.WriteLine("    " + Convert.ToHexString(privateKeyData));
            Console.WriteLine();

            BigPoint publicKey = encryptor.GetPublicKey();

            Console.WriteLine("Public Key as Point:");
            Console.WriteLine($"    X: {Convert.ToHexString(Utils.MakeUnsignedBigEndianArray(publicKey.X))}");
            Console.WriteLine($"    Y: {Convert.ToHexString(Utils.MakeUnsignedBigEndianArray(publicKey.Y))}");
            Console.WriteLine();

            byte[] publicKeyUncompressed = encryptor.ExportPublicKey();

            Console.WriteLine("Uncompressed Public Key:");
            Console.WriteLine("    " + Convert.ToHexString(publicKeyUncompressed));
            Console.WriteLine();

            byte[] publicKeyCompressed = encryptor.ExportPublicKeyCompressed();

            Console.WriteLine("Compressed Public Key:");
            Console.WriteLine("    " + Convert.ToHexString(publicKeyCompressed));
        }

        private static void MakeSignature(string[] args)
        {
            ECEncryptor encryptor = new ECEncryptor(WeierstrasCurve.secp256k1);

            byte[] privateKey;
            if(args.Length > 0)
            {
                privateKey = Convert.FromHexString(args[0]);
            }
            else
            {
                Console.WriteLine("Enter a private key as a hex string:");
                string hexString = Console.ReadLine().Trim();
                privateKey = Convert.FromHexString(hexString);
            }

            encryptor.ImportPrivateKey(privateKey);

            string filePath;
            if(args.Length > 1)
            {
                filePath = args[1];
            }
            else
            {
                Console.WriteLine("Enter the path to the file to generate a signature for:");
                filePath = Console.ReadLine().Trim();
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine("File does not exist.");
                return;
            }

            Console.WriteLine("Reading file...");
            byte[] fileData = File.ReadAllBytes(filePath);

            Console.WriteLine("Hashing file with SHA256...");
            byte[] hash = SHA256.HashData(fileData);

            Console.WriteLine("Generating signature...");
            byte[] signature = encryptor.SignHash(hash);

            Console.WriteLine("Signature:");
            Console.WriteLine("    " + Convert.ToHexString(signature));
        }

        private static void ValidateSignature(string[] args)
        {
            ECEncryptor encryptor = new ECEncryptor(WeierstrasCurve.secp256k1);

            byte[] publicKey;
            if (args.Length > 0)
            {
                publicKey = Convert.FromHexString(args[0]);
            }
            else
            {
                Console.WriteLine("Enter the public key as a hex string:");
                string hexString = Console.ReadLine().Trim();
                publicKey = Convert.FromHexString(hexString);
            }

            encryptor.ImportPublicKey(publicKey);

            byte[] signature;
            if (args.Length > 1)
            {
                signature = Convert.FromHexString(args[1]);
            }
            else
            {
                Console.WriteLine("Enter the signature as a hex string:");
                string hexString = Console.ReadLine().Trim();
                signature = Convert.FromHexString(hexString);
            }

            string filePath;
            if (args.Length > 2)
            {
                filePath = args[2];
            }
            else
            {
                Console.WriteLine("Enter the path to the file that was signed:");
                filePath = Console.ReadLine().Trim();
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine("File does not exist.");
                return;
            }

            Console.WriteLine("Reading file...");
            byte[] fileData = File.ReadAllBytes(filePath);

            Console.WriteLine("Hashing file with SHA256...");
            byte[] hash = SHA256.HashData(fileData);

            Console.WriteLine("Validating signature...");
            
            if(encryptor.VerifySignature(hash, signature))
            {
                Console.WriteLine("Signature is Valid");
            }
            else
            {
                Console.WriteLine("Signature is Invalid");
            }
        }
    }
}