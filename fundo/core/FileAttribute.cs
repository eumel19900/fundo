using System;

namespace fundo.core
{
    [Flags]
    public enum FileAttribute : byte
    {
        None = 0,
        Readonly = 1 << 0,
        Hidden = 1 << 1,
        System = 1 << 2,
        Archive = 1 << 3,
        Temporary = 1 << 4,
        Compress = 1 << 5,
        Encrypted = 1 << 6
    }
}
