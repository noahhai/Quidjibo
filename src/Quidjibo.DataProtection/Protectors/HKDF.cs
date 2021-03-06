using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Quidjibo.DataProtection.Protectors
{
    public class Hkdf<T> : IDisposable
        where T : KeyedHashAlgorithm, new()
    {
        private readonly KeyedHashAlgorithm _hmac;

        public Hkdf()
        {
            _hmac = new T();
        }

        public void Dispose()
        {
            _hmac.Dispose();
        }

        /// <summary>
        /// </summary>
        /// <param name="salt"></param>
        /// <param name="inputKeyMaterial"></param>
        /// <returns>A pseudorandom key.</returns>
        public byte[] Extract(byte[] salt, byte[] inputKeyMaterial)
        {
            _hmac.Key = salt ?? new byte[0];
            return _hmac.ComputeHash(inputKeyMaterial);
        }

        /// <summary>
        /// </summary>
        /// <param name="pseudoRandomKey"></param>
        /// <param name="info"></param>
        /// <param name="length"></param>
        /// <returns>Output keying material</returns>
        public byte[] Expand(byte[] pseudoRandomKey, byte[] info, int length)
        {
            if(length > _hmac.HashSize * 255 / 8)
            {
                throw new Exception("Invalid length. Must be less than or equal to 255 * Hash Length.");
            }

            if (info == null)
            {
                info = new byte[0];
            }

            _hmac.Key = pseudoRandomKey;

            var n = Convert.ToInt32(Math.Ceiling(length / (_hmac.HashSize / 8.0)));

            var lastT = new byte[0];

            var t = new List<byte>();
            var ikm = new List<byte>((_hmac.HashSize / 8) + info.Length + 1);
            for (var i = 1; i <= n; i++)
            {
                // T(N) = HMAC-Hash(PRK, T(N-1) | info | 0x0N)
                ikm.AddRange(lastT);
                ikm.AddRange(info);
                ikm.Add((byte)i);

                var tn = _hmac.ComputeHash(ikm.ToArray());
                t.AddRange(tn);

                lastT = tn;
                ikm.Clear();
            }

            return t.Take(length).ToArray();
        }
    }
}
