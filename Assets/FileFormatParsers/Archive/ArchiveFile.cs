using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class ArchiveFile
{
    private Archive.Header header;
    private int AARCBlockNumber = -1;
    private Archive.AARC aarc;

    public static Dictionary<string, Archive.AARCEntry> aarcEntries = new Dictionary<string, Archive.AARCEntry>();
    public static List<Archive.PackBlockHeader> packBlockHeaders = new List<Archive.PackBlockHeader>();

    public void ReadArchiveFile(string dataLocation, string fileName)
    {
        string filePath = dataLocation + fileName;
        if (File.Exists(filePath))
        {
            using (FileStream fs = File.OpenRead(filePath))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    ReadHeader(br);
                    ReadGlobalBlockInfo(br);
                    ReadAARCBlock(br);
                    ReadAARCEntries(br);

                    //Console.Log("aarcEntries : " + aarcEntries.Count + " " + "numAarcEntries : " + aarc.numAarcEntries);
                }
            }
        }
        else
        {
            Console.Log("Missing file : " + fileName, Console.LogType.Error);
        }
    }

    private void ReadHeader(BinaryReader br)
    {
        header = new Archive.Header();
        header.signature = br.ReadUInt32(); // == 'PACK'
        header.version = br.ReadUInt32();
        br.ReadBytes(512); // skip empty
        header.indexFileSize = br.ReadUInt64();
        br.ReadBytes(8); // skip empty
        header.ofsBlockTable = br.ReadUInt64();
        header.numBlocks = br.ReadUInt32();
        br.ReadBytes(28); // skip unknown
    }

    private void ReadGlobalBlockInfo(BinaryReader br)
    {
        packBlockHeaders = new List<Archive.PackBlockHeader>();
        br.BaseStream.Seek((long)header.ofsBlockTable, SeekOrigin.Begin);
        for (int b = 0; b < header.numBlocks; b++)
        {
            Archive.PackBlockHeader pbh = new Archive.PackBlockHeader();
            pbh.blockOffset = br.ReadUInt64();
            pbh.blockSize = br.ReadUInt64();
            // find AARC block by size //
            if (pbh.blockSize == 16)
            {
                AARCBlockNumber = b;
            }
            packBlockHeaders.Add(pbh);
        }
    }

    private void ReadAARCBlock(BinaryReader br)
    {
        if (AARCBlockNumber != -1)
        {
            br.BaseStream.Seek((long)packBlockHeaders[AARCBlockNumber].blockOffset, SeekOrigin.Begin);
            aarc = new Archive.AARC();
            aarc.magic = br.ReadUInt32();
            aarc.version = br.ReadUInt32();
            aarc.numAarcEntries = br.ReadUInt32();
            aarc.ofsAarcEntries = br.ReadUInt32();
        }
        else
        {
            Console.Log("Missing AARC Block.", Console.LogType.Error);
        }
    }

    private void ReadAARCEntries(BinaryReader br)
    {
        br.BaseStream.Seek((long)packBlockHeaders[(int)aarc.ofsAarcEntries].blockOffset, SeekOrigin.Begin);

        aarcEntries = new Dictionary<string, Archive.AARCEntry>();
        //br.BaseStream.Seek(aarc.ofsAarcEntries, SeekOrigin.Begin);
        for (int a = 0; a < aarc.numAarcEntries; a++)
        {
            Archive.AARCEntry aarcEntry = new Archive.AARCEntry();
            aarcEntry.blockIndex = br.ReadUInt32();
            aarcEntry.shaHash = br.ReadBytes(20);
            aarcEntry.uncompressedSize = br.ReadUInt64();

            string hash = DataManager.ToHex(aarcEntry.shaHash);
            if (!aarcEntries.ContainsKey(hash))
            {
                aarcEntries.Add(hash, aarcEntry);
            }
        }
    }
    /*
    public string ToHex(byte[] bytes, bool upperCase = false)
    {
        StringBuilder result = new StringBuilder(bytes.Length * 2);

        for (int i = 0; i < bytes.Length; i++)
            result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

        return result.ToString();
    }
    */
}
