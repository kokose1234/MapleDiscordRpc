// From https://github.com/diamondo25/MapleShark

using System;
using System.Globalization;
using System.Security.Cryptography;

namespace MapleDiscordRpc.Net;

public sealed class MapleAes : IDisposable
{
    private static readonly string[] AesKeys =
    {
        "DA296D3E62E0961234BF39A63F895EF16D0EE36C28A11E201DCBC2033F410784",
        "0F1405651B2861C9C5E72C8E463608DCF3A88DFEBEF2EB71FFA0D03B75068C7E",
        "8778734DD0BE82BEDBC246412B8CFA307F70F0A754863295AA5B68130BE6FCF5",
        "CABE7D9F898A411BFDB84F68F6727B1499CDD30DF0443AB4A66653330BCBA110",
        "5E4CEC034C73E605B4310EAAADCFD5B0CA27FFD89D144DF4792759427C9CC1F8",
        "CD8C87202364B8A687954CB05A8D4E2D99E73DB160DEB180AD0841E96741A5D5",
        "9FE4189F15420026FE4CD12104932FB38F735340438AAF7ECA6FD5CFD3A195CE",
        "2923BE84E16CD6AE529049F1F1BBE9EBB3A6DB3C870C3E99245E0D1C06B747DE",
        "B3124DC843BB8BA61F035A7D0938251F5DD4CBFC96F5453B130D890A1CDBAE32",
        "888138616B681262F954D0E7711748780D92291D86299972DB741CFA4F37B8B5",
        "209A50EE407836FD124932F69E7D49DCAD4F14F2444066D06BC430B7323BA122",
        "F622919DE18B1FDAB0CA9902B9729D492C807EC599D5E980B2EAC9CC53BF67D6",
        "BF14D67E2DDC8E6683EF574961FF698F61CDD11E9D9C167272E61DF0844F4A77",
        "02D7E8392C53CBC9121E33749E0CF4D5D49FD4A4597E35CF3222F4CCCFD3902D",
        "48D38F75E6D91D2AE5C0F72B788187440E5F5000D4618DBE7B0515073B33821F",
        "187092DA6454CEB1853E6915F8466A0496730ED9162F6768D4F74A4AD0576876",
        "5B628A8A8F275CF7E5874A3B329B614084C6C3B1A7304A10EE756F032F9E6AEF",
        "762DD0C2C9CD68D4496A792508614014B13B6AA51128C18CD6A90B87978C2FF1",
        "10509BC8814329288AF6E99E47A18148316CCDA49EDE81A38C9810FF9A43CDCF",
        "5E4EE1309CFED9719FE2A5E20C9BB44765382A4689A982797A7678C263B126DF"
    };

    private static readonly byte[] ShiftKey =
    {
        0xEC, 0x3F, 0x77, 0xA4, 0x45, 0xD0, 0x71, 0xBF, 0xB7, 0x98, 0x20, 0xFC, 0x4B, 0xE9, 0xB3, 0xE1,
        0x5C, 0x22, 0xF7, 0x0C, 0x44, 0x1B, 0x81, 0xBD, 0x63, 0x8D, 0xD4, 0xC3, 0xF2, 0x10, 0x19, 0xE0,
        0xFB, 0xA1, 0x6E, 0x66, 0xEA, 0xAE, 0xD6, 0xCE, 0x06, 0x18, 0x4E, 0xEB, 0x78, 0x95, 0xDB, 0xBA,
        0xB6, 0x42, 0x7A, 0x2A, 0x83, 0x0B, 0x54, 0x67, 0x6D, 0xE8, 0x65, 0xE7, 0x2F, 0x07, 0xF3, 0xAA,
        0x27, 0x7B, 0x85, 0xB0, 0x26, 0xFD, 0x8B, 0xA9, 0xFA, 0xBE, 0xA8, 0xD7, 0xCB, 0xCC, 0x92, 0xDA,
        0xF9, 0x93, 0x60, 0x2D, 0xDD, 0xD2, 0xA2, 0x9B, 0x39, 0x5F, 0x82, 0x21, 0x4C, 0x69, 0xF8, 0x31,
        0x87, 0xEE, 0x8E, 0xAD, 0x8C, 0x6A, 0xBC, 0xB5, 0x6B, 0x59, 0x13, 0xF1, 0x04, 0x00, 0xF6, 0x5A,
        0x35, 0x79, 0x48, 0x8F, 0x15, 0xCD, 0x97, 0x57, 0x12, 0x3E, 0x37, 0xFF, 0x9D, 0x4F, 0x51, 0xF5,
        0xA3, 0x70, 0xBB, 0x14, 0x75, 0xC2, 0xB8, 0x72, 0xC0, 0xED, 0x7D, 0x68, 0xC9, 0x2E, 0x0D, 0x62,
        0x46, 0x17, 0x11, 0x4D, 0x6C, 0xC4, 0x7E, 0x53, 0xC1, 0x25, 0xC7, 0x9A, 0x1C, 0x88, 0x58, 0x2C,
        0x89, 0xDC, 0x02, 0x64, 0x40, 0x01, 0x5D, 0x38, 0xA5, 0xE2, 0xAF, 0x55, 0xD5, 0xEF, 0x1A, 0x7C,
        0xA7, 0x5B, 0xA6, 0x6F, 0x86, 0x9F, 0x73, 0xE6, 0x0A, 0xDE, 0x2B, 0x99, 0x4A, 0x47, 0x9C, 0xDF,
        0x09, 0x76, 0x9E, 0x30, 0x0E, 0xE4, 0xB2, 0x94, 0xA0, 0x3B, 0x34, 0x1D, 0x28, 0x0F, 0x36, 0xE3,
        0x23, 0xB4, 0x03, 0xD8, 0x90, 0xC8, 0x3C, 0xFE, 0x5E, 0x32, 0x24, 0x50, 0x1F, 0x3A, 0x43, 0x8A,
        0x96, 0x41, 0x74, 0xAC, 0x52, 0x33, 0xF0, 0xD9, 0x29, 0x80, 0xB1, 0x16, 0xD3, 0xAB, 0x91, 0xB9,
        0x84, 0x7F, 0x61, 0x1E, 0xCF, 0xC5, 0xD1, 0x56, 0x3D, 0xCA, 0xF4, 0x05, 0xC6, 0xE5, 0x08, 0x49
    };

    private readonly ushort _build;
    private readonly RijndaelManaged _aes = new();
    private readonly ICryptoTransform _transformer;
    private bool _disposed;

    public byte[] IV { get; private set; }

    internal MapleAes(ushort build, byte locale, byte[] iv, byte subVersion)
    {
        _build = build;

        if ((short)build < 0) build = (ushort)(0xFFFF - build);

        Console.WriteLine(build);
        InitAesKey();
        _aes.Mode = CipherMode.ECB;
        _aes.Padding = PaddingMode.PKCS7;
        _transformer = _aes.CreateEncryptor();
        IV = iv;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _transformer.Dispose();
        _disposed = true;
    }


    public bool ConfirmHeader(byte[] pBuffer, int pStart)
    {
        var b = (pBuffer[pStart] ^ IV[2]) == (_build & 0xFF) &&
                (pBuffer[pStart + 1] ^ IV[3]) == (_build >> 8 & 0xFF);
        return b;
    }

    public static int GetHeaderLength(byte[] pBuffer, int pBytesAvailable)
    {
        var ivBytes = (ushort)(pBuffer[0] | pBuffer[1] << 8);
        var xorredSize = (ushort)(pBuffer[2] | pBuffer[3] << 8);
        var length = (ushort)(xorredSize ^ ivBytes);

        return length == 0xFF00 ? 8 : 4;
    }

    public static int GetPacketLength(byte[] pBuffer, int pBytesAvailable)
    {
        if (pBytesAvailable < 4) return pBytesAvailable - 4;

        var ivBytes = (ushort)(pBuffer[0] | pBuffer[1] << 8);
        var xorredSize = (ushort)(pBuffer[2] | pBuffer[3] << 8);
        var length = (ushort)(xorredSize ^ ivBytes);

        if (length == 0xFF00)
        {
            if (pBytesAvailable < 8) return pBytesAvailable - 8;
            return BitConverter.ToInt32(pBuffer, 4) ^ ivBytes;
        }

        return length;
    }

    public void TransformKMS(byte[] pBuffer)
    {
        var oudeIV = new byte[4];
        Buffer.BlockCopy(IV, 0, oudeIV, 0, 4);
        for (var i = 0; i < pBuffer.Length; i++)
        {
            var v7 = (byte)(pBuffer[i] ^ ShiftKey[IV[0]]);
            var v8 = (byte)(v7 >> 1 & 0x55 | 2 * (v7 & 0xD5));
            pBuffer[i] = (byte)(0x10 * v8 | v8 >> 4);
            Morph(pBuffer[i], IV);
        }

        ShiftIV(oudeIV);
    }

    public void TransformAES(byte[] pData)
    {
        var freshIVBlock = new[]
        {
            IV[0], IV[1], IV[2], IV[3],
            IV[0], IV[1], IV[2], IV[3],
            IV[0], IV[1], IV[2], IV[3],
            IV[0], IV[1], IV[2], IV[3]
        };
        var currentIVBlock = new byte[16];
        var dataSize = pData.Length;
        var blockSize = 0;

        for (var start = 0; start < dataSize; start += blockSize)
        {
            blockSize = Math.Min(start == 0 ? 1456 : 1460, dataSize - start);
            Buffer.BlockCopy(freshIVBlock, 0, currentIVBlock, 0, 16);

            for (var i = 0; i < blockSize; i++)
            {
                if (i % 16 == 0)
                {
                    _transformer.TransformBlock(currentIVBlock, 0, 16, currentIVBlock, 0);
                }

                pData[start + i] ^= currentIVBlock[i % 16];
            }
        }
    }

    public void TransformNewKMS(byte[] pBuffer)
    {
        for (var i = 0; i < pBuffer.Length; i++)
        {
            pBuffer[i] -= IV[0];
        }
    }

    public void ShiftIV(byte[] pOldIV = null)
    {
        pOldIV ??= IV;
        byte[] newIV = { 0xF2, 0x53, 0x50, 0xC6 };

        for (var i = 0; i < 4; ++i)
            Morph(pOldIV[i], newIV);

        Buffer.BlockCopy(newIV, 0, IV, 0, IV.Length);
    }

    public static void Morph(byte pValue, byte[] pIV)
    {
        var tableInput = ShiftKey[pValue];

        pIV[0] += (byte)(ShiftKey[pIV[1]] - pValue);
        pIV[1] -= (byte)(pIV[2] ^ tableInput);
        pIV[2] ^= (byte)(ShiftKey[pIV[3]] + pValue);
        pIV[3] -= (byte)(pIV[0] - tableInput);

        var val = (uint)(pIV[0] | pIV[1] << 8 | pIV[2] << 16 | pIV[3] << 24);
        val = val >> 0x1D | val << 0x03;
        pIV[0] = (byte)(val & 0xFF);
        pIV[1] = (byte)(val >> 8 & 0xFF);
        pIV[2] = (byte)(val >> 16 & 0xFF);
        pIV[3] = (byte)(val >> 24 & 0xFF);
    }

    /// <summary>
    /// 현재 버전에 맞는 Aes키를 계산
    /// </summary>
    private void InitAesKey()
    {
        var keyString = AesKeys[_build % AesKeys.Length];
        var key = new byte[32];

        for (var i = 0; i < keyString.Length; i += 8)
        {
            key[i / 2] = byte.Parse($"{keyString[i]}{keyString[i + 1]}", NumberStyles.HexNumber);
        }

        _aes.Key = key;
    }
}