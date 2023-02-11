// From https://github.com/diamondo25/MapleShark

using System;
using System.Text;

namespace MapleDiscordRpc.Net;

public sealed class MaplePacket
{
    private static readonly Encoding StringEncoding = Encoding.GetEncoding(949);

    public DateTime Timestamp { get; }
    public ushort Build { get; }
    public ushort Locale { get; }
    public ushort Opcode { get; }
    public byte[] Buffer { get; }
    public int Cursor { get; private set; }
    public int Length => Buffer.Length;
    public int Remaining => Length - Cursor;
    public uint PreDecodeIv { get; }
    public uint PostDecodeIv { get; }

    internal MaplePacket(DateTime pTimestamp, ushort pBuild, ushort pLocale, ushort pOpcode, byte[] pBuffer, uint pPreDecodeIv, uint pPostDecodeIv)
    {
        Timestamp = pTimestamp;
        Build = pBuild;
        Opcode = pOpcode;
        Buffer = pBuffer;
        Locale = pLocale;
        PreDecodeIv = pPreDecodeIv;
        PostDecodeIv = pPostDecodeIv;
    }

    public void Rewind()
    {
        Cursor = 0;
    }

    public void Skip(int amount) => Cursor += amount;

    public byte ReadByte()
    {
        var val = Buffer[Cursor++];
        return val;
    }

    public short ReadShort()
    {
        var val = BitConverter.ToInt16(new ReadOnlySpan<byte>(Buffer, Cursor, 2));
        Cursor += 2;
        return val;
    }


    public int ReadInt()
    {
        var val = BitConverter.ToInt32(new ReadOnlySpan<byte>(Buffer, Cursor, 4));
        Cursor += 4;
        return val;
    }

    public long ReadLong()
    {
        var val = BitConverter.ToInt64(new ReadOnlySpan<byte>(Buffer, Cursor, 8));
        Cursor += 8;
        return val;
    }

    public bool ReadLong(out long pValue)
    {
        pValue = 0;
        if (Cursor + 8 > Length) return false;
        pValue = Buffer[Cursor++] |
                 Buffer[Cursor++] << 8 |
                 Buffer[Cursor++] << 16 |
                 Buffer[Cursor++] << 24 |
                 Buffer[Cursor++] << 32 |
                 Buffer[Cursor++] << 40 |
                 Buffer[Cursor++] << 48 |
                 Buffer[Cursor++] << 56;
        return true;
    }

    public bool ReadFlippedLong(out long pValue)
    {
        pValue = 0;
        if (Cursor + 8 > Length) return false;
        pValue = Buffer[Cursor++] << 32 |
                 Buffer[Cursor++] << 40 |
                 Buffer[Cursor++] << 48 |
                 Buffer[Cursor++] << 56 |
                 Buffer[Cursor++] |
                 Buffer[Cursor++] << 8 |
                 Buffer[Cursor++] << 16 |
                 Buffer[Cursor++] << 24;
        return true;
    }

    public bool ReadDouble(out double pValue)
    {
        pValue = 0;
        if (Cursor + 8 > Length) return false;
        pValue = BitConverter.ToDouble(Buffer, Cursor);
        Cursor += 8;
        return true;
    }

    public bool ReadString(out string text)
    {
        var len = ReadShort();
        if (len > 0)
        {
            Cursor += len;
            text = StringEncoding.GetString(new ReadOnlySpan<byte>(Buffer, Cursor, len));
            return true;
        }

        text = "";
        return false;
    }

    public string ReadString(int count)
    {
        var text = StringEncoding.GetString(new ReadOnlySpan<byte>(Buffer, Cursor, count));
        Cursor += count;
        return text;
    }
}