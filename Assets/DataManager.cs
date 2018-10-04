using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.IO.Compression;

public class DataManager
{
    public static string dataSource = "ClientData";
    public IndexFile index;
    public ArchiveFile archive;
    public static Dictionary<string, Archive.FolderBlock> DirectoryTree = new Dictionary<string, Archive.FolderBlock>();
    public static Dictionary<string, string> FileNames = new Dictionary<string, string>();
    public static Dictionary<string, Archive.FileEntry> fileList = new Dictionary<string, Archive.FileEntry>();

    private static string installLocation;

    public void InitializeData()
    {
        installLocation = FindWSInstall();
        //installLocation = @"D:\Games\Wildstar\WildStar Beta\Wildstar.exe";
        if (installLocation != null)
        {
            string dataLocation = Path.GetDirectoryName(installLocation) + "\\Patch\\";
            index = new IndexFile();
            index.ReadIndexFile(dataLocation, dataSource + ".index");
            archive = new ArchiveFile();
            archive.ReadArchiveFile(dataLocation, dataSource + ".archive");
        }
    }

    public static byte[] ExtractFile(string path)
    {
        byte[] data = GetFileBytes(path);
        Directory.CreateDirectory(@"C:\" + Path.GetDirectoryName(path) + @"\");
        File.WriteAllBytes(@"C:\" + path, data);
        return data;
    }

    public static byte[] GetFileBytes (string path)
    {
        if (fileList.ContainsKey(path))
        {
            Archive.FileEntry fileEntry = fileList[path];
            uint compression = fileEntry.flags; // 3: zlib compressed, 5: lzma compressed
            byte[] data;
            byte[] byteHash = fileEntry.hash;
            string hash = ToHex(byteHash);
            if (ArchiveFile.aarcEntries.ContainsKey(hash))
            {
                Archive.AARCEntry aarcEntry = ArchiveFile.aarcEntries[hash];
                Archive.PackBlockHeader packBlockHeader = ArchiveFile.packBlockHeaders[(int)aarcEntry.blockIndex];
                string filePath = Path.GetDirectoryName(installLocation) + "\\Patch\\" + dataSource + ".archive";
                using (FileStream fs = File.OpenRead(filePath))
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        br.BaseStream.Seek((long)packBlockHeader.blockOffset, SeekOrigin.Begin);
                        data = br.ReadBytes((int)packBlockHeader.blockSize);
                    }
                }
                //Console.Log((int)fileEntry.uncompressedSize + " " + fileEntry.uncompressedSize);
                if (compression == 3)
                    return DecompressZlib(data, (int)fileEntry.uncompressedSize);
                else if (compression == 5)
                    return DecompressLzma(data, (int)fileEntry.uncompressedSize);
                else
                    return data;
            }
            else
            {
                Console.Log("Missing AARC Entry : " + hash, Console.LogType.Error);
                return null;
            }
        }
        else
        {
            Console.Log("Missing File : " + path, Console.LogType.Error);
            return null;
        }
    }

    public static byte[] DecompressZlib(byte[] data, int decompressedSize)
    {
        try
        {
            var unpackedData = new byte[decompressedSize];

            using (var inflate = new DeflateStream(new MemoryStream(data, 2, data.Length - 2), CompressionMode.Decompress))
            {
                var decompressed = new MemoryStream();
                inflate.CopyTo(decompressed);
                decompressed.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < decompressedSize; i++)
                    unpackedData[i] = (byte)decompressed.ReadByte();
            }
            return unpackedData;
        }
        catch
        {
            return null;
        }
    }

    public static byte[] DecompressLzma(byte[] data, int decompressedSize)
    {
        try
        {
            var unpackedData = new byte[decompressedSize];
            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
            var decompressed = new MemoryStream();
            using (var compressed = new MemoryStream(data))
            {
                var props = new byte[5];
                compressed.Read(props, 0, 5);
                decoder.SetDecoderProperties(props);
                decoder.Code(compressed, decompressed, data.Length, decompressedSize, null);
            }
            return decompressed.ToArray();
        }
        catch
        {
            return null;
        }
    }

    public static List<string> GetFolderList(string path)
    {
        List<string> folderList = new List<string>();
        if (DirectoryTree[path].subDirectories != null)
        {
            foreach (Archive.DirectoryEntry directoryEntry in DirectoryTree[path].subDirectories)
            {
                string word = "";
                int increment = 0;
                for (int t = 0; t < 200; t++)
                {
                    char c = DirectoryTree[path].names[directoryEntry.nameOffset + increment];
                    increment++;
                    if (c != '\0')
                    {
                        word += c;
                    }
                    else
                    {
                        break;
                    }
                }
                folderList.Add(word);
            }
        }
        return folderList;
    }

    public static Dictionary<string, Archive.FileEntry> GetFileList(string path)
    {
        Dictionary<string, Archive.FileEntry> fileList = new Dictionary<string, Archive.FileEntry>();
        if (DirectoryTree[path].files != null)
        {
            foreach (Archive.FileEntry fileEntry in DirectoryTree[path].files)
            {
                if (!fileList.ContainsKey(FileNames[ToHex(fileEntry.hash)]))
                    fileList.Add(FileNames[ToHex(fileEntry.hash)], fileEntry);
            }
        }
        return fileList;
    }

    public string FindWSInstall()
    {
        RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\WildStar");
        if (key != null)
        {
            Object o = key.GetValue("DisplayIcon");
            if (o != null)
            {
                string path = o as string;
                Console.Log("Found WildStar install location : " + path, Console.LogType.Error);
                return path;
            }
            else
            {
                Console.Log("Can't find registry value for WS install location.", Console.LogType.Error);
                return null;
            }
        }
        else
        {
            Console.Log("Can't find registry key for WS install location.", Console.LogType.Error);
            return null;
        }
    }

    public static string ToHex(byte[] bytes, bool upperCase = false)
    {
        StringBuilder result = new StringBuilder(bytes.Length * 2);

        for (int i = 0; i < bytes.Length; i++)
            result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

        return result.ToString();
    }
}
