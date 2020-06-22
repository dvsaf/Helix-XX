using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Helix.Commons
{
    public sealed class Magma : HashAlgorithm
    {
        private const int InBlockSize = 8;

        private static readonly byte[] NodeTab =
        {
            0x4, 0x2, 0xF, 0x5, 0x9, 0x1, 0x0, 0x8, 0xE, 0x3, 0xB, 0xC, 0xD, 0x7, 0xA, 0x6,
            0xC, 0x9, 0xF, 0xE, 0x8, 0x1, 0x3, 0xA, 0x2, 0x7, 0x4, 0xD, 0x6, 0x0, 0xB, 0x5,
            0xD, 0x8, 0xE, 0xC, 0x7, 0x3, 0x9, 0xA, 0x1, 0x5, 0x2, 0x4, 0x6, 0xF, 0x0, 0xB,
            0xE, 0x9, 0xB, 0x2, 0x5, 0xF, 0x7, 0x1, 0x0, 0xD, 0xC, 0x6, 0xA, 0x4, 0x3, 0x8,
            0x3, 0xE, 0x5, 0x9, 0x6, 0x8, 0x0, 0xD, 0xA, 0xB, 0x7, 0xC, 0x2, 0x1, 0xF, 0x4,
            0x8, 0xF, 0x6, 0xB, 0x1, 0x9, 0xC, 0x5, 0xD, 0x3, 0x7, 0xA, 0x0, 0xE, 0x2, 0x4,
            0x9, 0xB, 0xC, 0x0, 0x3, 0x6, 0x7, 0x5, 0x4, 0x8, 0xE, 0xF, 0x1, 0xA, 0x2, 0xD,
            0xC, 0x6, 0x5, 0x2, 0xB, 0x0, 0x9, 0xD, 0x3, 0xE, 0x7, 0xA, 0xF, 0x4, 0x1, 0x8
        };

        private static readonly int[] IniKey = {0, 0, 0, 0, 0, 0, 0, 0};
        private static readonly int[] ExtTable;
        private static readonly int[] ExtKey;

        private static readonly HashAlgorithm HashAlgorithm = new Magma();

        private readonly int[] _buffer = new int[InBlockSize / sizeof(int)];
        private int _i0;
        private int _i1;
        private int _position;

        static Magma()
        {
            ExtTable = new int[1024];
            ExtKey = new int[16];

            for (var i = 0; i <= 15; i++)
                ExtKey[i] = IniKey[i & 7];

            var n = 0;
            for (var j = 16; j <= 31; j++)
            for (var k = 0; k <= 15; k++)
            {
                var i = (NodeTab[j] << 4) | NodeTab[k];
                ExtTable[n] = i << 11;
                n++;
            }

            for (var j = 48; j <= 63; j++)
            for (var k = 32; k <= 47; k++)
            {
                var i = (NodeTab[j] << 4) | NodeTab[k];
                ExtTable[n] = i << 19;
                n++;
            }

            for (var j = 80; j <= 95; j++)
            for (var k = 64; k <= 79; k++)
            {
                var i = (NodeTab[j] << 4) | NodeTab[k];
                ExtTable[n] = (i << 27) | (i >> 5);
                n++;
            }

            for (var j = 112; j <= 127; j++)
            for (var k = 96; k <= 111; k++)
            {
                var i = (NodeTab[j] << 4) | NodeTab[k];
                ExtTable[n] = i << 3;
                n++;
            }
        }

        private Magma()
        {
            Initialize();
        }

        public static ulong CalculateFileChecksum(string fileName)
        {
            HashAlgorithm.Initialize();

            using var stream = new FileStream(fileName, FileMode.Open);
            return BitConverter.ToUInt32(HashAlgorithm.ComputeHash(stream));
        }

        public override void Initialize()
        {
            Array.Clear(_buffer, 0, InBlockSize / sizeof(int));
            _position = 0;
            _i0 = 0;
            _i1 = 0;
        }

        private void CalcBlock(IReadOnlyList<int> buffer)
        {
            var n1 = _i0 ^ buffer[0];
            var n2 = _i1 ^ buffer[1];

            for (var j = 0; j <= 15; j++)
            {
                var cm1 = n1 + ExtKey[j];
                var r = ExtTable[cm1 & 0xff] | ExtTable[256 + ((cm1 >> 8) & 0xff)] |
                        ExtTable[512 + ((cm1 >> 16) & 0xff)] | ExtTable[768 + ((cm1 >> 24) & 0xff)];
                var cm2 = r ^ n2;

                n2 = n1;
                n1 = cm2;
            }

            _i0 = n1;
            _i1 = n2;
        }

        private void CalcSum(byte[] buffer, int count)
        {
            int totalCount;
            var offset = 0;

            if (_position > 0)
            {
                totalCount = _position + count;
                if (totalCount >= InBlockSize)
                {
                    Buffer.BlockCopy(buffer, 0, _buffer, _position, InBlockSize - _position);
                    CalcBlock(_buffer);
                    totalCount -= InBlockSize;
                    offset = InBlockSize - _position;
                }
                else
                {
                    Buffer.BlockCopy(buffer, 0, _buffer, _position, totalCount - _position);
                    _position = totalCount;
                    return;
                }
            }
            else
            {
                totalCount = count;
            }

            while (totalCount >= InBlockSize)
            {
                Buffer.BlockCopy(buffer, offset, _buffer, 0, InBlockSize);
                CalcBlock(_buffer);
                offset += InBlockSize;
                totalCount -= InBlockSize;
            }

            _position = totalCount;
            if (totalCount > 0) Buffer.BlockCopy(buffer, offset, _buffer, 0, totalCount);
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            var buffer = new byte[cbSize];
            Buffer.BlockCopy(array, ibStart, buffer, 0, cbSize);
            CalcSum(buffer, cbSize);
        }

        protected override byte[] HashFinal()
        {
            if (_position <= 0)
                return BitConverter.GetBytes(_i0);

            for (var i = _position; i < InBlockSize; i++)
                _buffer[i] = 0;

            CalcBlock(_buffer);

            return BitConverter.GetBytes(_i0);
        }
    }
}