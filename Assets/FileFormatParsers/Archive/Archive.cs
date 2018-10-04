using System;
using System.Collections.Generic;
using System.Linq;

public class Archive {

    public struct Header
    {
        public UInt32 signature;
        public UInt32 version;
        public UInt64 indexFileSize;
        public UInt64 ofsBlockTable;
        public UInt32 numBlocks;
    };

    public struct PackBlockHeader
    {
        public UInt64 blockOffset;
        public UInt64 blockSize;
    };

    public struct AIDX
    {
        public UInt32 magic; // 'AIDX'
        public UInt32 version;
        public UInt32 unk1;
        public UInt32 rootBlock;
    };

    public struct AARC
    {
        public UInt32 magic; // 'AARC'
        public UInt32 version;
        public UInt32 numAarcEntries;
        public UInt32 ofsAarcEntries; // absolute offset
    };

    public struct FolderBlock
    {
        public UInt32 numSubdirectories;
        public UInt32 numFiles;
        public DirectoryEntry[] subDirectories;
        public FileEntry[] files;
        public char[] names;
    };

    public struct DirectoryEntry
    {
        public UInt32 nameOffset;
        public UInt32 nextBlock;
    };

    public struct FileEntry
    {
        public UInt32 nameOffset;
        public UInt32 flags; // 3: zlib compressed, 5: lzma compressed
        public UInt64 writeTime; // uint64 // FILETIME
        public UInt64 uncompressedSize;
        public UInt64 compressedSize;
        public byte[] hash;
        public UInt32 unk2;
    };

    public struct AARCEntry
    {
        public UInt32 blockIndex;
        public byte[] shaHash;
        public UInt64 uncompressedSize;
    };

    public struct FILETIME // Contains a 64-bit value representing the number of 100-nanosecond intervals since January 1, 1601 (UTC).
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    };

}

/*
public class ByteArrayComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[] left, byte[] right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }
        return left.SequenceEqual(right);
    }
    public int GetHashCode(byte[] key)
    {
        if (key == null)
            throw new ArgumentNullException("key");
        return key.Sum(b => b);
    }
}
*/

public class ByteArrayComparer : EqualityComparer<byte[]>
{
    public override bool Equals(byte[] first, byte[] second)
    {
        if (first == null || second == null)
        {
            // null == null returns true.
            // non-null == null returns false.
            return first == second;
        }
        if (ReferenceEquals(first, second))
        {
            return true;
        }
        if (first.Length != second.Length)
        {
            return false;
        }
        // Linq extension method is based on IEnumerable, must evaluate every item.
        return first.SequenceEqual(second);
    }
    public override int GetHashCode(byte[] obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException("obj");
        }
        // quick and dirty, instantly identifies obviously different
        // arrays as being different
        return obj.Length;
    }
}
