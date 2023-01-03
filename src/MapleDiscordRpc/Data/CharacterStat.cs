using System;

namespace MapleDiscordRpc.Data;

[Flags]
public enum CharacterStat
{
    None = 0,
    Level = 1 << 4,
    Job = 1 << 5,
}