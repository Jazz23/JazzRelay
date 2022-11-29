using System;
using System.Collections.Generic;

namespace JazzRelay.DataTypes
{
    public class RC4
    {
        private const int StateLength = 256;
        private byte[] _engineState;
        private byte[] _workingKey;
        private int _x;
        private int _y;

        public RC4(byte[] key)
        {
            _workingKey = key;
            SetKey(_workingKey);
        }

        public void Cipher(byte[] data, int slice)
        {
            ProcessBytes(data, slice, data.Length - slice, data, slice);
        }

        public void Reset()
        {
            SetKey(_workingKey);
        }

        private void ProcessBytes(IReadOnlyList<byte> input, int inOff, int length, IList<byte> output, int outOff)
        {
            for (var i = 0; i < length; i++)
            {
                _x = (_x + 1) & 255;
                _y = (_engineState[_x] + _y) & 255;
                (_engineState[_x], _engineState[_y]) = (_engineState[_y], _engineState[_x]);
                output[i + outOff] = (byte)(input[i + inOff] ^
                                             _engineState[(_engineState[_x] + _engineState[_y]) & byte.MaxValue]);
            }
        }

        private void SetKey(byte[] keyBytes)
        {
            _workingKey = keyBytes;
            _x = _y = 0;
            var flag = _engineState == null;
            if (flag) _engineState = new byte[StateLength];
            for (var i = 0; i < StateLength; i++) _engineState[i] = (byte)i;
            var num = 0;
            var num2 = 0;
            for (var j = 0; j < StateLength; j++)
            {
                num2 = ((keyBytes[num] & byte.MaxValue) + _engineState[j] + num2) & 255;
                (_engineState[j], _engineState[num2]) = (_engineState[num2], _engineState[j]);
                num = (num + 1) % keyBytes.Length;
            }
        }

        public static byte[] HexStringToBytes(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            var arr = new byte[hex.Length >> 1];

            for (var i = 0; i < hex.Length >> 1; ++i)
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));

            return arr;
        }

        private static int GetHexVal(char hex)
        {
            var val = (int)hex;
            return val - (val < 58 ? 48 : val < 97 ? 55 : 87);
        }
    }
}