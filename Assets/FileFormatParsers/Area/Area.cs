using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public static class Area
{
    public static AreaDataType areaData;
    public static bool threadWorkingMesh = false;
    public static bool working = false;

    public struct AreaDataType
    {
        public List<SubChunkData> subChunks;
        public Dictionary<UInt32, PropInfo> propInfoList;
        public Dictionary<int, string> propFileList;
    }

    public struct SubChunkData
    {
        public SubChunkFlags flags;
        public VertexData vertexData;
        public List<UInt32> textureIDs;
        public byte[] colorMap;
        public byte[] blendMaps;
        public byte[] unknownData;
        public byte unknownByte;
        public List<UInt32> propEntries;
    }

    public struct VertexData
    {
        public Vector3[] position;
        public Vector3[] normal;
        public Vector4[] tangent;
    }

    public struct PropInfo
    {
        public Int64 uniqueId;      // used in PROP chunks as references for collision/etc.
        public Int32[] unk1;
        public Int32 nameOffset;    // into unicode name chunk
        public Int32 unkOffset;
        public float Scale;
        public Quaternion Rotation; // quaternion XYZW
        public Vector3 Position;
        public Int16[] unk2;
        public Int32[] unk3;
        public Int32 Color1;        // typically 7f7f7fff so just a multiplier shading color
        public Int32 unkColor2;
        public Int32[] unk4;
        public Int32 unkColor3;
        public Int32 unk5;
    }

    public struct SubChunkFlags
    {
        public bool hasHeightmap;
        public bool hasTextureIds;
        public bool hasBlendMapOld;
        public bool hasColorMapOld;
        public bool unk1;
        public bool unk2;
        public bool unk3;
        public bool unk4;
        public bool hasShadowMap;
        public bool unk5;
        public bool unk6;
        public bool unk7;
        public bool unk8;
        public bool hasColorMapDXT;
        public bool unk10;
        public bool unk11;
        public bool unk12;
        public bool hasBlendMapDXT;
        public bool unk13;
        public bool unk14;
        public bool unk15;
        public bool unk16;
        public bool unk17;
        public bool unk18;
        public bool unk19;
        public bool unk20;
        public bool unk21;
        public bool unk22;
        public bool unk23;
        public bool unk24;
        public bool unk25;
        public bool unk26;
    }

    public static void Load(string path)
    {
        Console.Log("Loading : " + path);
        areaData = new AreaDataType();
        byte[] inputData = DataManager.GetFileBytes(path);

        using (MemoryStream ms = new MemoryStream(inputData))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                ReadHeader(br);
                ReadCHNK(br);
                ReadPROp(br);
                //Console.Log(new string(br.ReadChars(4)));
            }
        }
        //Console.Log("QUEUED");
        AreaData.areas.Enqueue(areaData);
        threadWorkingMesh = false;
    }

    private static void ReadHeader(BinaryReader br)
    {
        string AREA = new string(br.ReadChars(4));
        uint version = br.ReadUInt32();
    }

    private static void ReadCHNK(BinaryReader br)
    {
        areaData.subChunks = new List<SubChunkData>();
        string CHNK = new string(br.ReadChars(4));
        uint CHNKsize = br.ReadUInt32();

        Console.Log("CHNKsize : " + CHNKsize);

        long currentPosition = br.BaseStream.Position;
        int sunbchunkNumber = 0;

        while (br.BaseStream.Position < currentPosition + CHNKsize)
        {
            SubChunkData subChunkData = new SubChunkData();

            uint subChunkSize = br.ReadUInt32();
            long subChunkStartPosition = br.BaseStream.Position;
            subChunkData.flags = ReadSubChunkFlags(br.ReadUInt32());

            /*
            Console.Log((subChunkData.flags.hasHeightmap) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.hasTextureIds) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.unk0) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.hasColorMap) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.unk1) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.unk2) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.unk3) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.unk4) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.hasShadowMap) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.unk5) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.unk6) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.unk7) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.unk8) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.hasBlendMaps) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.unk10) ? 1 : 0).ToString() + " " +
                (subChunkData.flags.unk11) ? 1 : 0).ToString() + " | " +

                (subChunkData.flags22.unk0) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk1) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk2) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk3) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk4) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk5) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk6) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk7) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk8) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk9) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk10) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk11) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk12) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk13) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk14) ? 1 : 0).ToString() + " " +
                (subChunkData.flags22.unk15) ? 1 : 0).ToString() + " - " + subChunkStartPosition + " " + subChunkSize);
            */
            if (subChunkData.flags.hasHeightmap)
            {
                UInt16[,] VertexHeights = new UInt16[19, 19];
                for (int i = 0; i < 19; i++)
                    for (int j = 0; j < 19; j++)
                        VertexHeights[i, j] = br.ReadUInt16();
                subChunkData.vertexData = SortVertexData(VertexHeights);
            }

            if (subChunkData.flags.hasTextureIds)
            {
                subChunkData.textureIDs = new List<UInt32>();
                subChunkData.textureIDs.Add(br.ReadUInt32());
                subChunkData.textureIDs.Add(br.ReadUInt32());
                subChunkData.textureIDs.Add(br.ReadUInt32());
                subChunkData.textureIDs.Add(br.ReadUInt32());
            }

            if (subChunkData.flags.hasBlendMapOld)
            {
                Console.Log("found unk0 (old blendmaps) at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.hasColorMapOld)
            {
                Console.Log("found old colormap at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk1)
            {
                Console.Log("found unk1 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk2)
            {
                Console.Log("found unk2 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk3 || subChunkData.flags.unk4)
            {
                br.BaseStream.Position += 80;
            }

            if (subChunkData.flags.hasShadowMap)
            {
                Console.Log("found old Shadowmap at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk5)
            {
                Console.Log("found unk5 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk6)
            {
                Console.Log("found unk6 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk7)
            {
                Console.Log("found unk7 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk8)
            {
                Console.Log("found unk8 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.hasColorMapDXT)
            {
                subChunkData.colorMap = br.ReadBytes(4624);
            }

            if (subChunkData.flags.unk10)
            {
                Console.Log("found unk10 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk11)
            {
                Console.Log("found unk11 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk12)
            {
                for (int x = 0; x < 64; x++)
                {
                    for (int y = 0; y < 64; y++)
                    {
                        br.ReadByte();
                    }
                }
            }

            if (subChunkData.flags.hasBlendMapDXT)
            {
                subChunkData.blendMaps = br.ReadBytes(2312);
            }
            
            if (subChunkData.flags.unk13)
            {
                subChunkData.unknownData = br.ReadBytes(2312);
            }

            if (subChunkData.flags.unk14)
            {
                Console.Log("found unk14 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk15)
            {
                Console.Log("found unk15 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk16)
            {
                subChunkData.unknownByte = br.ReadByte();
                //Console.Log(subChunkData.unknownByte);
                //Console.Log("found unk16 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk17)
            {
                Console.Log("found unk17 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk18)
            {
                Console.Log("found unk18 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk19)
            {
                Console.Log("found unk19 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk20)
            {
                Console.Log("found unk20 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk21)
            {
                Console.Log("found unk21 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk22)
            {
                for (int x = 0; x < 64; x++)
                {
                    for (int y = 0; y < 64; y++)
                    {
                        br.ReadByte();
                    }
                }
            }

            if (subChunkData.flags.unk23)
            {
                for (int x = 0; x < 4; x++)
                {
                    br.ReadUInt32();
                }
            }

            if (subChunkData.flags.unk24)
            {
                Console.Log("found unk24 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk25)
            {
                Console.Log("found unk25 at " + br.BaseStream.Position);
            }

            if (subChunkData.flags.unk26)
            {
                Console.Log("found unk26 at " + br.BaseStream.Position);
            }

            if (br.BaseStream.Position != subChunkSize + subChunkStartPosition)
            {

                try
                {
                    if (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        // Extra Subchunk chunks //
                        while (br.BaseStream.Position < subChunkSize + subChunkStartPosition)
                        {

                            string Magic = new string(br.ReadChars(4));

                            if (Magic == "PORP")  // PROP
                            {
                                subChunkData.propEntries = new List<uint>();
                                // Header
                                UInt32 chunkSize = br.ReadUInt32();

                                // Entries
                                for (int e = 0; e < chunkSize / 4; e++)
                                {
                                    subChunkData.propEntries.Add(br.ReadUInt32());
                                }
                            }
                            else if (Magic == "Druc")  // curD
                            {
                                // Header
                                UInt32 chunkSize = br.ReadUInt32();

                                br.BaseStream.Position += chunkSize;
                                //Console.Log("Found: curD subchunk data at " + br.BaseStream.Position);
                            }
                            else if (Magic == "Psbw")  // wpsP
                            {
                                // Header
                                UInt32 chunkSize = br.ReadUInt32();

                                br.BaseStream.Position += chunkSize;
                                //Console.Log("Found: wpsP subchunk data at " + br.BaseStream.Position);
                            }
                            else if (Magic == "GtAW")  // WAtG
                            {
                                // Header
                                UInt32 chunkSize = br.ReadUInt32();

                                br.BaseStream.Position += chunkSize;
                                //Console.Log("Found: WAtG subchunk data at " + br.BaseStream.Position);
                            }
                            else // Unknown magic
                            {
                                // Header
                                UInt32 chunkSize = br.ReadUInt32();

                                br.BaseStream.Position += chunkSize;
                                //Console.Log("Found: unknown subchunk magic " + Magic + " at " + br.BaseStream.Position);
                            }
                        }
                    }
                }
                catch
                {
                    Console.Log(subChunkSize + subChunkStartPosition + " " + br.BaseStream.Length);
                    //br.BaseStream.Position = subChunkSize + subChunkStartPosition;
                    Console.Log("Bug Here.");
                }
            }

            Vector2 subChunkCoords = new Vector2((sunbchunkNumber % 16), (sunbchunkNumber / 16));
            areaData.subChunks.Add(subChunkData);
            sunbchunkNumber++;
            if (subChunkSize + subChunkStartPosition < br.BaseStream.Length)
                br.BaseStream.Seek(subChunkSize + subChunkStartPosition, SeekOrigin.Begin);
        }
    }

    private static SubChunkFlags ReadSubChunkFlags(UInt32 data)
    {
        SubChunkFlags flags = new SubChunkFlags
        {
            hasHeightmap = (data & 0x1) != 0,
            hasTextureIds = (data & 0x2) != 0,
            hasBlendMapOld = (data & 0x4) != 0,
            hasColorMapOld = (data & 0x8) != 0,
            unk1 = (data & 0x10) != 0,
            unk2 = (data & 0x20) != 0,
            unk3 = (data & 0x40) != 0,
            unk4 = (data & 0x80) != 0,
            hasShadowMap = (data & 0x100) != 0,
            unk5 = (data & 0x200) != 0,
            unk6 = (data & 0x400) != 0,
            unk7 = (data & 0x800) != 0,
            unk8 = (data & 0x1000) != 0,
            hasColorMapDXT = (data & 0x2000) != 0,
            unk10 = (data & 0x4000) != 0,
            unk11 = (data & 0x8000) != 0,
            unk12 = (data & 0x10000) != 0,
            hasBlendMapDXT = (data & 0x20000) != 0,
            unk13 = (data & 0x40000) != 0,
            unk14 = (data & 0x80000) != 0,
            unk15 = (data & 0x100000) != 0,
            unk16 = (data & 0x200000) != 0,
            unk17 = (data & 0x400000) != 0,
            unk18 = (data & 0x800000) != 0,
            unk19 = (data & 0x1000000) != 0,
            unk20 = (data & 0x2000000) != 0,
            unk21 = (data & 0x4000000) != 0,
            unk22 = (data & 0x8000000) != 0,
            unk23 = (data & 0x10000000) != 0,
            unk24 = (data & 0x20000000) != 0,
            unk25 = (data & 0x40000000) != 0,
            unk26 = (data & 0x80000000) != 0
        };
        return flags;
    }

    // This chunk lists all instances of .m3 objects in the map tile along with various data for that instance.
    private static void ReadPROp(BinaryReader br)
    {
        if (br.BaseStream.Position >= br.BaseStream.Length)
            return;

        string PROp = new string(br.ReadChars(4));
        uint PROpSize = br.ReadUInt32();

        Int32 numPropEntries = br.ReadInt32();
        long currentPosition = br.BaseStream.Position;

        // Read PROp model informations //
        areaData.propInfoList = new Dictionary<UInt32, PropInfo>();
        for (int p = 0; p < numPropEntries; p++)
        {
            PropInfo propInfo = new PropInfo();
            propInfo.uniqueId = br.ReadInt64();    // used in PROP chunks as references for collision/etc.
            propInfo.unk1 = new Int32[3] { br.ReadInt32(), br.ReadInt32(), br.ReadInt32() };
            propInfo.nameOffset = br.ReadInt32();  // into unicode name chunk
            propInfo.unkOffset = br.ReadInt32();
            propInfo.Scale = br.ReadSingle();
            propInfo.Rotation = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()); // quaternion XYZW
            propInfo.Position = new Vector3(br.ReadSingle() + 512, br.ReadSingle(), br.ReadSingle() + 512);
            propInfo.unk2 = new Int16[4] { br.ReadInt16(), br.ReadInt16(), br.ReadInt16(), br.ReadInt16() };
            propInfo.unk3 = new Int32[3] { br.ReadInt32(), br.ReadInt32(), br.ReadInt32() };
            propInfo.Color1 = br.ReadInt32();      // typically 7f7f7fff so just a multiplier shading color
            propInfo.unkColor2 = br.ReadInt32();
            propInfo.unk4 = new Int32[2] { br.ReadInt32(), br.ReadInt32() };
            propInfo.unkColor3 = br.ReadInt32();
            propInfo.unk5 = br.ReadInt32();
            areaData.propInfoList.Add((UInt32)propInfo.uniqueId, propInfo);
        }

        // Read PROp file names //
        areaData.propFileList = new Dictionary<int,string>();
        long size = PROpSize - 104 * numPropEntries;
        byte[] nameData = br.ReadBytes((int)size);
        string name = "";
        foreach(KeyValuePair<UInt32, PropInfo> prop in areaData.propInfoList)
        {
            int arrayPosition = 0;
            for (int l = 0; l < 300; l++) // max file length ~300
            {
                if (prop.Value.nameOffset > 0)
                {
                    int charPosition = (prop.Value.nameOffset - (104 * numPropEntries)) + arrayPosition - 4;
                    char c = (char)nameData[charPosition];
                    if (c != '\0')
                    {
                        name = name + c;
                        arrayPosition = arrayPosition + 2;
                    }
                    else
                    {
                        if (!areaData.propFileList.ContainsKey(prop.Value.nameOffset))
                            areaData.propFileList.Add(prop.Value.nameOffset, name);
                        //Console.Log(name);
                        name = "";
                        arrayPosition = 0;
                        break;
                    }
                }
            }
        }
        br.BaseStream.Seek(currentPosition + PROpSize, SeekOrigin.Begin);
    }

    private static VertexData SortVertexData(UInt16[,] data)
    {
        VertexData vertexData = new VertexData
        {
            position = new Vector3[361],
            normal = new Vector3[361],
            tangent = new Vector4[361]
        };
        int vertCount = 0;
        for (int y = -1; y < 18; ++y)
        {
            for (int x = -1; x < 18; ++x)
            {
                int h = data[y + 1,x + 1] & 0x7FFF;
                // vertex positions //
                vertexData.position[vertCount].x = x * 2;
                vertexData.position[vertCount].z = y * 2;
                vertexData.position[vertCount].y = -2048.0f + h / 8.0f;
                // Normals //
                if (y > 0 && x > 0 && y <= 17 && y <= 17)
                {
                    Vector3 tl = vertexData.position[(y - 1) * 19 + x - 1];
                    Vector3 tr = vertexData.position[(y - 1) * 19 + x + 1];
                    Vector3 br = vertexData.position[(y + 1) * 19 + x + 1];
                    Vector3 bl = vertexData.position[(y + 1) * 19 + x - 1];
                    Vector3 v = vertexData.position[y * 19 + x];
                    Vector3 P1 = new Vector3(tl.x, tl.y, tl.z);
                    Vector3 P2 = new Vector3(tr.x, tr.y, tr.z);
                    Vector3 P3 = new Vector3(br.x, br.y, br.z);
                    Vector3 P4 = new Vector3(bl.x, bl.y, bl.z);
                    Vector3 vert = new Vector3(v.x, v.y, v.z);
                    Vector3 N1 = Vector3.Cross((P2 - vert),(P1 - vert));
                    Vector3 N2 = Vector3.Cross((P3 - vert),(P2 - vert));
                    Vector3 N3 = Vector3.Cross((P4 - vert),(P3 - vert));
                    Vector3 N4 = Vector3.Cross((P1 - vert),(P4 - vert));
                    Vector3 norm = Vector3.Normalize(N1 + N2 + N3 + N4);
                    vertexData.normal[y * 19 + x ] = norm;
                }
                vertCount++;
            }
        }
        return vertexData;
    }

    // normalize 0|15 to 0|255 //
    public static int NormalizeHalfResAlphaPixel(int value)
    {
        return value * 255 / 15;
    }

}

