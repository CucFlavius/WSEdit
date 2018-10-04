using System.Collections.Generic;
using System.IO;
using System.Text;

public class IndexFile
{
    private Archive.Header header;
    private List<Archive.PackBlockHeader> packBlockHeaders = new List<Archive.PackBlockHeader>();
    private int AIDXBlockNumber = -1;
    private Archive.AIDX aidx;

    public void ReadIndexFile(string dataLocation, string fileName)
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
                    ReadAIDXBlock(br);
                    Archive.FolderBlock folderBlock = ReadBlock((int)aidx.rootBlock, "AIDX", br);
                    DataManager.DirectoryTree.Add("AIDX", folderBlock); // add root

                    //Console.Log("FileNames : " + DataManager.FileNames.Count + " " + "fileList : " + DataManager.fileList.Count);
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
            // find AIDX block by size //
            if (pbh.blockSize == 16)
            {
                AIDXBlockNumber = b;
            }
            packBlockHeaders.Add(pbh);
        }
    }

    private void ReadAIDXBlock(BinaryReader br)
    {
        if (AIDXBlockNumber != -1)
        {
            br.BaseStream.Seek((long)packBlockHeaders[AIDXBlockNumber].blockOffset, SeekOrigin.Begin);
            aidx = new Archive.AIDX();
            aidx.magic = br.ReadUInt32();
            aidx.version = br.ReadUInt32();
            aidx.unk1 = br.ReadUInt32();
            aidx.rootBlock = br.ReadUInt32();
        }
        else
        {
            Console.Log("Missing AIDX Block.", Console.LogType.Error);
        }
    }

    private Archive.FolderBlock ReadBlock(int blockNumber, string currentDir, BinaryReader br)
    {
        br.BaseStream.Seek((long)packBlockHeaders[blockNumber].blockOffset, SeekOrigin.Begin);
        Archive.FolderBlock folderBlock = new Archive.FolderBlock();
        folderBlock.numSubdirectories = br.ReadUInt32();
        folderBlock.numFiles = br.ReadUInt32();
        if (folderBlock.numSubdirectories > 0)
        {
            Archive.DirectoryEntry[] directoryEntries = new Archive.DirectoryEntry[folderBlock.numSubdirectories];
            for (int i = 0; i < folderBlock.numSubdirectories; i++)
            {
                directoryEntries[i] = ReadDirectoryEntry(br);
            }
            folderBlock.subDirectories = directoryEntries;
        }
        if (folderBlock.numFiles > 0)
        {
            Archive.FileEntry[] fileEntries = new Archive.FileEntry[folderBlock.numFiles];
            for (int i = 0; i < folderBlock.numFiles; i++)
            {
                fileEntries[i] = ReadFileEntry(br);
            }
            folderBlock.files = fileEntries;
        }
        long remainingSize = (long)packBlockHeaders[blockNumber].blockSize - (br.BaseStream.Position - (long)packBlockHeaders[blockNumber].blockOffset);
        char[] nameslist = new char[remainingSize];
        for (int i = 0; i < remainingSize; i++)
        {
            nameslist[i] = br.ReadChar();
        }
        folderBlock.names = nameslist;
        if (folderBlock.subDirectories != null)
        {
            foreach (Archive.DirectoryEntry directoryEntry in folderBlock.subDirectories)
            {
                string word = "";
                int increment = 0;
                for (int t = 0; t < 200; t++)
                {
                    char c = folderBlock.names[directoryEntry.nameOffset + increment];
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
                Archive.FolderBlock fB = ReadBlock((int)directoryEntry.nextBlock, currentDir + "\\" + word, br);
                DataManager.DirectoryTree.Add(currentDir + "\\" + word, fB);
            }
        }
        //long currentStreamPosition = br.BaseStream.Position;
        if (folderBlock.files != null)
        {
            foreach (Archive.FileEntry fileEntry in folderBlock.files)
            {
                //br.BaseStream.Seek(fileEntry.nameOffset, SeekOrigin.Begin);
                string word = "";
                int increment = 0;
                for (int t = 0; t < 200; t++)
                {
                    char c = folderBlock.names[fileEntry.nameOffset + increment]; //br.ReadChar();
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
                //DataManager.FileNames.Add(fileEntry.nameOffset, word);
                string hash = DataManager.ToHex(fileEntry.hash);
                if (!DataManager.FileNames.ContainsKey(hash))
                {
                    DataManager.FileNames.Add(hash, word);
                }
                //else
                    //Console.Log(DataManager.FileNames[fileEntry.nameOffset] + " " + word);
                DataManager.fileList.Add(currentDir + "\\" + word, fileEntry);
            }
        }
        //br.BaseStream.Seek(currentStreamPosition, SeekOrigin.Begin);
        return folderBlock;
    }

    private Archive.DirectoryEntry ReadDirectoryEntry(BinaryReader br)
    {
        Archive.DirectoryEntry directoryEntry = new Archive.DirectoryEntry();
        directoryEntry.nameOffset = br.ReadUInt32();
        directoryEntry.nextBlock = br.ReadUInt32();
        return directoryEntry;
    }

    private Archive.FileEntry ReadFileEntry(BinaryReader br)
    {
        Archive.FileEntry fileEntry = new Archive.FileEntry();
        fileEntry.nameOffset = br.ReadUInt32();
        fileEntry.flags = br.ReadUInt32();
        fileEntry.writeTime = br.ReadUInt64();
        fileEntry.uncompressedSize = br.ReadUInt64();
        fileEntry.compressedSize = br.ReadUInt64();
        fileEntry.hash = br.ReadBytes(20);
        fileEntry.unk2 = br.ReadUInt32();
        return fileEntry;
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
