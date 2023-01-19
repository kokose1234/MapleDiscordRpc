// From https://github.com/diamondo25/MapleShark

using System;

namespace MapleDiscordRpc.Net;

[Flags]
public enum TransformMethod : int
{
    Aes = 1 << 1,
    MapleCrypto = 1 << 2,
    KmsCrypto = 1 << 4,
    NewKmsCrypto = 1 << 5,
    ShiftIv = 1 << 6,
}

public sealed class MapleStream
{
    private const int DEFAULT_SIZE = 300000;

    private readonly MapleAes _aes;
    private readonly TransformMethod _transformMethod;

    private byte[] _buffer = new byte[DEFAULT_SIZE];
    private int _cursor;
    private int _expectedDataSize = 4;

    public ushort Build { get; }


    public MapleStream(ushort build, byte locale, byte[] iv, byte subVersion, ushort port)
    {
        Build = build;
        _aes = new MapleAes((ushort) (0xFFFF - Build), locale, iv, subVersion);

        if (locale == 1 && Build >= 354)
        {
            if (port != 8484)
            {
                _transformMethod = TransformMethod.NewKmsCrypto | TransformMethod.ShiftIv;
            }
            else
            {
                //Login Server
                _transformMethod = TransformMethod.Aes | TransformMethod.ShiftIv;
            }
        }
    }

    public void Append(byte[] pBuffer) => Append(pBuffer, 0, pBuffer.Length);

    public void Append(byte[] pBuffer, int pStart, int pLength)
    {
        if (_buffer.Length - _cursor < pLength)
        {
            var newSize = _buffer.Length * 2;
            while (newSize < _cursor + pLength) newSize *= 2;
            Array.Resize(ref _buffer, newSize);
        }

        Buffer.BlockCopy(pBuffer, pStart, _buffer, _cursor, pLength);
        _cursor += pLength;
    }

    public MaplePacket Read(DateTime pTransmitted)
    {
        if (_cursor < _expectedDataSize) return null;
        if (!_aes.ConfirmHeader(_buffer, 0))
        {
            throw new Exception("Failed to confirm packet header");
        }

        var headerLength = MapleAes.GetHeaderLength(_buffer, _cursor);
        _expectedDataSize = headerLength;
        if (_cursor < headerLength)
        {
            return null;
        }

        var packetSize = MapleAes.GetPacketLength(_buffer, _cursor);
        _expectedDataSize = packetSize + headerLength;
        if (_cursor < (packetSize + headerLength))
        {
            return null;
        }

        var packetBuffer = new byte[packetSize];
        Buffer.BlockCopy(_buffer, headerLength, packetBuffer, 0, packetSize);
        var preDecodeIv = BitConverter.ToUInt32(_aes.IV, 0);
        Decrypt(packetBuffer, _transformMethod);
        var postDecodeIv = BitConverter.ToUInt32(_aes.IV, 0);

        _cursor -= _expectedDataSize;
        if (_cursor > 0) Buffer.BlockCopy(_buffer, _expectedDataSize, _buffer, 0, _cursor);

        var packetType = (ushort) (packetBuffer[0] | (packetBuffer[1] << 8));
        Buffer.BlockCopy(packetBuffer, 2, packetBuffer, 0, packetSize - 2);
        Array.Resize(ref packetBuffer, packetSize - 2);

        _expectedDataSize = 4;

        return new MaplePacket(pTransmitted, Build, 1, packetType, packetBuffer, preDecodeIv, postDecodeIv);
    }

    private void Decrypt(byte[] pBuffer, TransformMethod pTransformLocale)
    {
        if ((pTransformLocale & TransformMethod.Aes) != 0)
            _aes.TransformAES(pBuffer);

        if ((pTransformLocale & TransformMethod.MapleCrypto) != 0)
        {
            for (var index1 = 1; index1 <= 6; ++index1)
            {
                byte firstFeedback = 0;
                byte secondFeedback;
                var length = (byte) (pBuffer.Length & 0xFF);

                if ((index1 % 2) == 0)
                {
                    for (var index2 = 0; index2 < pBuffer.Length; ++index2)
                    {
                        var temp = pBuffer[index2];
                        temp -= 0x48;
                        temp = (byte) (~temp);
                        temp = RollLeft(temp, length & 0xFF);
                        secondFeedback = temp;
                        temp ^= firstFeedback;
                        firstFeedback = secondFeedback;
                        temp -= length;
                        temp = RollRight(temp, 3);
                        pBuffer[index2] = temp;
                        --length;
                    }
                }
                else
                {
                    for (var index2 = pBuffer.Length - 1; index2 >= 0; --index2)
                    {
                        var temp = pBuffer[index2];
                        temp = RollLeft(temp, 3);
                        temp ^= 0x13;
                        secondFeedback = temp;
                        temp ^= firstFeedback;
                        firstFeedback = secondFeedback;
                        temp -= length;
                        temp = RollRight(temp, 4);
                        pBuffer[index2] = temp;
                        --length;
                    }
                }
            }
        }

        if ((pTransformLocale & TransformMethod.KmsCrypto) != 0) _aes.TransformKMS(pBuffer);
        if ((pTransformLocale & TransformMethod.NewKmsCrypto) != 0) _aes.TransformNewKMS(pBuffer);
        if ((pTransformLocale & TransformMethod.ShiftIv) != 0) _aes.ShiftIV();
    }

    public static byte RollLeft(byte pThis, int pCount)
    {
        var overflow = (uint) pThis << (pCount % 8);
        return (byte) ((overflow & 0xFF) | (overflow >> 8));
    }

    public static byte RollRight(byte pThis, int pCount)
    {
        var overflow = ((uint) pThis << 8) >> (pCount % 8);
        return (byte) ((overflow & 0xFF) | (overflow >> 8));
    }
}