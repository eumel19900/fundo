using System.IO;

namespace fundo.core
{
    public static class FileAttributeHelper
    {
        private const FileAttribute KnownAttributesMask =
            FileAttribute.Readonly |
            FileAttribute.Hidden |
            FileAttribute.System |
            FileAttribute.Archive |
            FileAttribute.Temporary |
            FileAttribute.Compress |
            FileAttribute.Encrypted;

        public static byte ToByte(FileAttribute attributes)
        {
            return (byte)attributes;
        }

        public static FileAttribute FromByte(byte value)
        {
            return (FileAttribute)(value & (byte)KnownAttributesMask);
        }

        public static bool HasAttribute(FileAttribute attributes, FileAttribute attribute)
        {
            return (attributes & attribute) == attribute;
        }

        public static FileAttribute AddAttribute(FileAttribute attributes, FileAttribute attribute)
        {
            return attributes | attribute;
        }

        public static FileAttribute RemoveAttribute(FileAttribute attributes, FileAttribute attribute)
        {
            return attributes & ~attribute;
        }

        public static FileAttribute FromSystemFileAttributes(System.IO.FileAttributes attributes)
        {
            FileAttribute result = FileAttribute.None;

            if (attributes.HasFlag(System.IO.FileAttributes.ReadOnly))
            {
                result |= FileAttribute.Readonly;
            }

            if (attributes.HasFlag(System.IO.FileAttributes.Hidden))
            {
                result |= FileAttribute.Hidden;
            }

            if (attributes.HasFlag(System.IO.FileAttributes.System))
            {
                result |= FileAttribute.System;
            }

            if (attributes.HasFlag(System.IO.FileAttributes.Archive))
            {
                result |= FileAttribute.Archive;
            }

            if (attributes.HasFlag(System.IO.FileAttributes.Temporary))
            {
                result |= FileAttribute.Temporary;
            }

            if (attributes.HasFlag(System.IO.FileAttributes.Compressed))
            {
                result |= FileAttribute.Compress;
            }

            if (attributes.HasFlag(System.IO.FileAttributes.Encrypted))
            {
                result |= FileAttribute.Encrypted;
            }

            return result;
        }

        public static System.IO.FileAttributes ToSystemFileAttributes(FileAttribute attributes)
        {
            System.IO.FileAttributes result = 0;

            if (HasAttribute(attributes, FileAttribute.Readonly))
            {
                result |= System.IO.FileAttributes.ReadOnly;
            }

            if (HasAttribute(attributes, FileAttribute.Hidden))
            {
                result |= System.IO.FileAttributes.Hidden;
            }

            if (HasAttribute(attributes, FileAttribute.System))
            {
                result |= System.IO.FileAttributes.System;
            }

            if (HasAttribute(attributes, FileAttribute.Archive))
            {
                result |= System.IO.FileAttributes.Archive;
            }

            if (HasAttribute(attributes, FileAttribute.Temporary))
            {
                result |= System.IO.FileAttributes.Temporary;
            }

            if (HasAttribute(attributes, FileAttribute.Compress))
            {
                result |= System.IO.FileAttributes.Compressed;
            }

            if (HasAttribute(attributes, FileAttribute.Encrypted))
            {
                result |= System.IO.FileAttributes.Encrypted;
            }

            return result;
        }
    }
}
