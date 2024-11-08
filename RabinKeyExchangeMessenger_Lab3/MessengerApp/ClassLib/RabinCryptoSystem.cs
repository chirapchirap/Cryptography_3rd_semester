﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace ClassLib
{
    public class RabinCryptoSystem
    {
        private readonly BigInteger p;
        private readonly BigInteger q;

        public BigInteger N { get; }
        private readonly int blockSize;

        public RabinCryptoSystem()
        {
            p = GenerateLargePrime(512);
            q = GenerateLargePrime(512);
            N = p * q;

            blockSize = (N.GetByteCount() - 1);  
        }

        public byte[] Encrypt(string message)
        {
            var blocks = SplitMessageIntoBlocks(message);   
            var encryptedBlocks = new List<byte[]>();

            foreach (var block in encryptedBlocks)
            {
                BigInteger m = new BigInteger(block, isUnsigned: true, isBigEndian: true);

                // Шифрование: c = m^2 mod N
                BigInteger c = BigInteger.ModPow(m, 2, N);
                encryptedBlocks.Add(c.ToByteArray(isUnsigned: true, isBigEndian: true));
            }
            return CombineByteArrays(encryptedBlocks);
        }

        private BigInteger GenerateLargePrime(int bits)
        {
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[bits / 8];
                while (true)
                {
                    rng.GetBytes(bytes);
                    bytes[bytes.Length - 1] |= 0x80; // старшему биту присваеваем 1, чтобы получалось большое число
                    BigInteger p = new BigInteger(bytes);
                    if (p > 0 && IsProbablePrime(p, 10))
                    {
                        return p;
                    }
                }
            }
        }

        private bool IsProbablePrime(BigInteger source, int certainty)
        {
            if (source == 2 || source == 3)
            {
                return true;
            }
            if (source < 2 || source % 2 == 0)
            {
                return false;
            }

            BigInteger d = source - 1;
            int s = 0;
            while (d % 2 == 0)
            {
                d /= 2;
                s++;
            }

            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[source.ToByteArray().LongLength];
                for (int i = 0; i < certainty; i++)
                {
                    BigInteger a;
                    do
                    {
                        rng.GetBytes(bytes);
                        a = new BigInteger(bytes);
                    } while (a < 2 || a >= source - 2);

                    BigInteger x = BigInteger.ModPow(a, d, source);
                    if (x == 1 || x == source - 1)
                    {
                        continue;
                    }

                    for (int r = 1; r < s; r++)
                    {
                        x = BigInteger.ModPow(x, 2, source);
                        if (x == 1)
                        {
                            return false;
                        }
                        if (x == source - 1)
                        {
                            break;
                        }
                    }
                    if (x != source - 1)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
