using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Linq;

public class TexFile
{
    public HeaderV1 header;
    public HeaderV2 header2;

    public byte[] Read(string path)
    {
        byte[] inputData = DataManager.GetFileBytes(path);
        byte[] outputData = inputData;

        if (inputData == null)
            return null;

        using (MemoryStream ms = new MemoryStream(inputData))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                ReadHeaderV2(br);

                // Unknown format //
                if (header2.format == 0)
                {
                    // skip to mip0
                    /*
                    int distance = 0;
                    for (int d = 0; d < header2.mipCount - 1; d++)
                    {
                        distance += (int)header2.sizes[d];
                    }
                    br.BaseStream.Seek(distance, SeekOrigin.Current);
                    */
                    header2.textureFormat = TextureFormat.BGRA32;
                    int remainingSize = (int)(br.BaseStream.Length - br.BaseStream.Position);
                    outputData = br.ReadBytes(remainingSize);
                }
                // DXT 1 //
                else if (header2.format == 13)
                {
                    int[] DXTSizes = CalculateDXTSizes((int)header2.mipCount, (int)header2.width, (int)header2.height, 8);
                    // skip to mip0
                    int distance = 0;
                    for (int d = 0; d < header2.mipCount - 1; d++)
                    {
                        distance += DXTSizes[d];
                    }
                    br.BaseStream.Seek(distance, SeekOrigin.Current);
                    int remainingSize = (int)(br.BaseStream.Length - br.BaseStream.Position);
                    outputData = br.ReadBytes(remainingSize);
                    header2.textureFormat = TextureFormat.DXT1;
                }
                // DXT 3 //
                else if (header2.format == 14)
                {
                    // ignore..
                }
                // DXT 5 //
                else if (header2.format == 15)
                {
                    int[] DXTSizes = CalculateDXTSizes((int)header2.mipCount, (int)header2.width, (int)header2.height, 16);
                    // skip to mip0
                    int distance = 0;
                    for (int d = 0; d < header2.mipCount - 1; d++)
                    {
                        distance += DXTSizes[d];
                    }
                    br.BaseStream.Seek(distance, SeekOrigin.Current);
                    int remainingSize = (int)(br.BaseStream.Length - br.BaseStream.Position);
                    outputData = br.ReadBytes(remainingSize);
                    header2.textureFormat = TextureFormat.DXT5;
                }
            }
        }
		
        return outputData;
    }

    public int[] CalculateDXTSizes(int miplevels, int width, int height, int blockSize)
    {
        int[] DXTSizes = new int[miplevels];
        int increment = 0;
        for (int m = miplevels - 1; m >= 0; m--)
        {
            int w = (int)(width / Mathf.Pow(2, m));
            int h = (int)(height / Mathf.Pow(2, m));
            DXTSizes [increment] = (int)(((w + 3) / 4) * ((h + 3) / 4) * blockSize);
            increment++;
        }
        return DXTSizes;
    }

    public int BoolArrayToInt(bool[] bits)
    {
        uint r = 0;
        for (int i = 0; i < bits.Length; i++)
        {
            if (bits[i])
            {
                r |= (uint)(1 << (bits.Length - i));
            }
        }
        return (int)(r / 2);
    }

    public Texture2D ReadToTexture2DAllDecoders(string path, TextureFormat textureFormat)
    {
        byte[] data = Read(path);
        Texture2D tex = new Texture2D((int)header2.width, (int)header2.height, textureFormat, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.LoadRawTextureData(data);
        tex.Apply();
        return tex;
    }

    public Texture2D ReadToTexture2D (string path)
    {
        TextureFormat textureFormat = TextureFormat.BGRA32;
        byte[] data = Read(path);

        if (data == null)
            return null;

        if (header2.format == 0)
        {
            textureFormat = TextureFormat.BGRA32;
            return null;
        }
        else if (header2.format == 13)
        {
            textureFormat = TextureFormat.DXT1;
        }
        else if (header2.format == 14)
        {
            textureFormat = TextureFormat.DXT5;
        }
        else if (header2.format == 15)
        {
            textureFormat = TextureFormat.DXT5;
        }
        else
        {
            Console.Log("Unsupported Texture Format : " + header2.format, Console.LogType.Error);
            return null;
        }
        Texture2D tex = new Texture2D((int)header2.width, (int)header2.height, textureFormat, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.LoadRawTextureData(data);
        tex.Apply();
        return tex;
    }

    private byte[] TrimToMipmap0 (byte[] data)
    {
        return null;
    }

    private void ReadHeaderV1(BinaryReader br)
    {
        header = new HeaderV1();
        header.magic = new string(br.ReadChars(4));
        header.type = br.ReadUInt32();
        header.width = br.ReadUInt32();
        header.height = br.ReadUInt32();
        header.unk1 = br.ReadUInt32();
        header.unk2 = br.ReadUInt32();
        header.mips = br.ReadUInt32();
        header.format = br.ReadUInt32();
    }

    private void ReadHeaderV2(BinaryReader br)
    {
        header2 = new HeaderV2();
        header2.signature = new string(br.ReadChars(4));
        header2.version = br.ReadUInt32();
        header2.width = br.ReadUInt32();
        header2.height = br.ReadUInt32();
        header2.depth = br.ReadUInt32();
        header2.faces = br.ReadUInt32();
        header2.mipCount = br.ReadUInt32();
        header2.format = br.ReadUInt32();
        header2.containsSizes = br.ReadUInt32();
        header2.unk1 = br.ReadUInt32();
        header2.unk2 = new UInt32[]{ br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32() };
        header2.unk3 = br.ReadUInt32();
        UInt32[] sizes = new UInt32[14];
        for (int s = 0; s < sizes.Length; s++)
            sizes[s] = br.ReadUInt32();
        header2.sizes = sizes;
    }

    public struct HeaderV1
    {
        public string magic;       // "XFG\0" magic number (GFX)
        public UInt32 type;        // always type 0
        public UInt32 width;       // width in pixels, power of 2
        public UInt32 height;      // height in pixels, power of 2
        public UInt32 unk1;        // depth
        public UInt32 unk2;        // sides
        public UInt32 mips;        // count of stored mipmaps
        public UInt32 format;      // 0,1 = uncomp, 13 = dxt1, 14 = dxt3, 15 = dxt5
    }

    public struct HeaderV2
    {
        public string signature;
        public UInt32 version;
        public UInt32 width;
        public UInt32 height;
        public UInt32 depth;
        public UInt32 faces;
        public UInt32 mipCount;
        public UInt32 format;
        public UInt32 containsSizes;
        public UInt32 unk1;
        public UInt32[] unk2; // kind of 4 * uint24 usually 0x4B 0x4B 0x4B, heavily used in format == 0
        public UInt32 unk3; // usually the same as mipCount, also mainly used in format == 0
        public UInt32[] sizes; // if containsSizes != 0, else 0
        public TextureFormat textureFormat;
    }

    /*
     ((w+3)/4) * ((h+3)/4) * 8
    For dxt1
    *16 for dxt 3 or 5
    */



}

