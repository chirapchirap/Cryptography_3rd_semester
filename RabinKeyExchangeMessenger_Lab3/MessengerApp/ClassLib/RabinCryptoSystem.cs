using System;
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

        public BigInteger N {  get; }

        public RabinCryptoSystem()
        {
            p = GenerateLargePrime(512);
            q = GenerateLargePrime(512);
            N = p * q;
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



    }
}
