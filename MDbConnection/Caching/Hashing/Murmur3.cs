using System.Runtime.CompilerServices;

namespace System.Data.Caching.Hashing
{

    /// <summary>
    ///     A lightweight hashing algorithm developed by Austin Appleby. For more information about Murmur3, please see:
    ///     https://en.wikipedia.org/wiki/MurmurHash
    /// </summary>
    /// <remarks>
    ///     This class was heavily modified from its origins. I don't know where, exactly, it originated from. Edward, my dear friend,
    /// took that version and smashed it with his unsafe knowledge. It's pretty freaking fast. He gets the credit for this.
    /// </remarks>
    public static class Murmur3
    {
        const uint C1 = 0x239b961b;
        const uint C2 = 0xab0e9789;
        const uint C3 = 0x38b34ae5;
        const uint C4 = 0xa1e38b93;

        /// <summary>
        ///     Computes the hash of a given array
        /// </summary>
        /// <param name="input">The input array to compute a hash from</param>
        /// <returns><see cref="T:byte[]"/></returns>
        public unsafe static byte[] ComputeHash32(byte[] input)
        {
            fixed (byte* data = input)
            {
                return ComputeHash32Impl(data, input.Length);
            }
        }

        /// <summary>
        ///     Computes the hash of a given string
        /// </summary>
        /// <param name="input">The string to compute a hash from</param>
        /// <returns><see cref="byte[]"/></returns>
        public unsafe static byte[] ComputeHash32(string input)
        {
            fixed (char* s = input)
            {
                var data = (byte*)s;
                return ComputeHash32Impl(data, input.Length * sizeof(char));
            }
        }

        /// <summary>
        ///     Magic
        /// </summary>
        /// <param name="input">An array of bytes</param>
        /// <param name="length">The length of the array of bytes</param>
        /// <returns><see cref="byte[]"/></returns>
        private static unsafe byte[] ComputeHash32Impl(byte* input, int length)
        {
            var data = input;
            {
                var nblocks = length / 16;

                uint h1 = 0;
                uint h2 = 0;
                uint h3 = 0;
                uint h4 = 0;
                uint k1;
                uint k2;
                uint k3;
                uint k4;


                // Mix the bytes into the hash pieces.

                var blocks = (uint*)(data + nblocks * 16);

                for (int i = -nblocks; i != 0; i++)
                {
                    k1 = blocks[i * 4 + 0];
                    k2 = blocks[i * 4 + 1];
                    k3 = blocks[i * 4 + 2];
                    k4 = blocks[i * 4 + 3];

                    k1 *= C1;
                    k1 = LeftRotate32(k1, 15);
                    k1 *= C2;
                    h1 ^= k1;

                    h1 = LeftRotate32(h1, 19);
                    h1 += h2;
                    h1 = h1 * 5 + 0x561ccd1b;

                    k2 *= C2;
                    k2 = LeftRotate32(k2, 16);
                    k2 *= C3;
                    h2 ^= k2;

                    h2 = LeftRotate32(h2, 17);
                    h2 += h3;
                    h2 = h2 * 5 + 0x0bcaa747;

                    k3 *= C3;
                    k3 = LeftRotate32(k3, 17);
                    k3 *= C4;
                    h3 ^= k3;

                    h3 = LeftRotate32(h3, 15);
                    h3 += h4;
                    h3 = h3 * 5 + 0x96cd1c35;

                    k4 *= C4;
                    k4 = LeftRotate32(k4, 18);
                    k4 *= C1;
                    h4 ^= k4;

                    h4 = LeftRotate32(h4, 13);
                    h4 += h1;
                    h4 = h4 * 5 + 0x32ac3b17;
                }


                //Optimization for mopping up the trailing bits. Still mixing byte data into the hash.

                var tail = (uint*)(data + nblocks * 16);

                k1 = 0;
                k2 = 0;
                k3 = 0;
                k4 = 0;

                switch (length & 15)
                {
                    case 15:
                        k4 ^= tail[14] << 16;
                        goto case 14;
                    case 14:
                        k4 ^= tail[13] << 8;
                        goto case 13;
                    case 13:
                        k4 ^= tail[12] << 0;
                        k4 *= C4;
                        k4 = LeftRotate32(k4, 18);
                        k4 *= C1;
                        h4 ^= k4;
                        goto case 12;
                    case 12:
                        k3 ^= tail[11] << 24;
                        goto case 11;
                    case 11:
                        k3 ^= tail[10] << 16;
                        goto case 10;
                    case 10:
                        k3 ^= tail[9] << 8;
                        goto case 9;
                    case 9:
                        k3 ^= tail[8] << 0;
                        k3 *= C3;
                        k3 = LeftRotate32(k3, 17);
                        k3 *= C4;
                        h3 ^= k3;
                        goto case 8;
                    case 8:
                        k2 ^= tail[7] << 24;
                        goto case 7;
                    case 7:
                        k2 ^= tail[6] << 16;
                        goto case 6;
                    case 6:
                        k2 ^= tail[5] << 8;
                        goto case 5;
                    case 5:
                        k2 ^= tail[4] << 0;
                        k2 *= C2;
                        k2 = LeftRotate32(k2, 16);
                        k2 *= C3;
                        h2 ^= k2;
                        goto case 4;
                    case 4:
                        k1 ^= tail[3] << 24;
                        goto case 3;
                    case 3:
                        k1 ^= tail[2] << 16;
                        goto case 2;
                    case 2:
                        k1 ^= tail[1] << 8;
                        goto case 1;
                    case 1:
                        k1 ^= tail[0] << 0;
                        k1 *= C1;
                        k1 = LeftRotate32(k1, 15);
                        k1 *= C2;
                        h1 ^= k1;
                        break;
                }

                //Finalizing.

                var ulength = (uint)length;
                h1 ^= ulength;
                h2 ^= ulength;
                h3 ^= ulength;
                h4 ^= ulength;

                //Mixing
                h1 += h2;
                h1 += h3;
                h1 += h4;
                h2 += h1;
                h3 += h1;
                h4 += h1;

                //Applying avalanche bits.
                h1 = FinalMix32(h1);
                h2 = FinalMix32(h2);
                h3 = FinalMix32(h3);
                h4 = FinalMix32(h4);

                //Mixing
                h1 += h2;
                h1 += h3;
                h1 += h4;
                h2 += h1;
                h3 += h1;
                h4 += h1;

                //Copying from our hash bits to the output byte array.
                var output = new byte[16];
                fixed (byte* outputBytes = output)
                {
                    var pointer = (uint*)outputBytes;
                    pointer[0] = h1;
                    pointer[1] = h2;
                    pointer[2] = h3;
                    pointer[3] = h4;
                }
                return output;
            }

        }

        /// <summary>
        ///     Applies avalanche effect to all bits.
        /// </summary>
        /// <param name="h"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint FinalMix32(uint h)
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;

            return h;
        }

        /// <summary>
        ///     Magic
        /// </summary>
        /// <param name="n"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint LeftRotate32(uint n, byte amount)
        {
            return (n << amount) | (n >> (32 - amount));
        }

    }

}