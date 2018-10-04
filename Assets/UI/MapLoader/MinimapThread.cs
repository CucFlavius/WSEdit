using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class MinimapThread 
{
    public static bool ThreadAlive = false;
    public static bool ResetParentSize = false;
    public static string currentMapName = "";
    public static bool[,] minimapAvailability = new bool[128, 128];
    public static int minimapCount;
    public static Vector2 min;
    public static Vector2 max;
    public static Queue<MinimapBlockData> MinimapDataQueue = new Queue<MinimapBlockData>();

    public static void LoadThread()
    {
        ThreadAlive = true;
        CompileMapList(currentMapName);
        GetMinMax();
        ResetParentSize = true;
        RequestAvailableTEXs(currentMapName);
        ThreadAlive = false;
    }

    // Build an array of available minimaps //
    public static void CompileMapList(string mapName)
    {
        Dictionary<string, Archive.FileEntry> fileEntries = DataManager.GetFileList(@"AIDX\Map\" + mapName);
        List<string> Minimaps = new List<string>(fileEntries.Keys);
        minimapCount = 0;

        for (int x = 0; x < 128; x++)
        {
            for (int y = 0; y < 128; y++)
            {
                minimapAvailability[x, y] = false;
            }
        }

        foreach (string map in Minimaps)
        {
            string[] split = map.Split("."[0]);
            if (Path.GetExtension(map).ToLower() == ".tex")
            {
                string hexCoords = split[1];
                int x = int.Parse((new string(new char[] { hexCoords[0], hexCoords[1] }).ToUpper()), System.Globalization.NumberStyles.HexNumber);
                int y = int.Parse((new string(new char[] { hexCoords[2], hexCoords[3] }).ToUpper()), System.Globalization.NumberStyles.HexNumber);
                minimapAvailability[x, y] = true;
                minimapCount++;
            }
        }
        if (minimapCount == 0)
        {
            Console.Log("No Minimaps found.");
        }
        else
        {
            Console.Log("Minimaps found: " + minimapCount);
        }
    }

    private static void GetMinMax()
    {
        int firstXCoord = 128;
        int firstYCoord = 128;
        int lastXCoord = 0;
        int lastYCoord = 0;

        for (int x = 0; x < 128; x++)
        {
            for (int y = 0; y < 128; y++)
            {
                if (minimapAvailability[x, y])
                {
                    if (x < firstXCoord) firstXCoord = x;
                    if (y < firstYCoord) firstYCoord = y;
                }
            }
        }
        for (int x = 0; x < 128; x++)
        {
            for (int y = 0; y < 128; y++)
            {
                if (minimapAvailability[x, y])
                {
                    if (x > lastXCoord) lastXCoord = x;
                    if (y > lastYCoord) lastYCoord = y;
                }
            }
        }

        //Debug.Log(minimapAvailability[0, 0]);
        min = new Vector2(firstXCoord, firstYCoord);
        max = new Vector2(lastXCoord, lastYCoord);
        //Debug.Log("min:" + min.x + "-" + min.y + " max:" + max.x + "-" + max.y);
    }

    // Request TEX blocks //
    private static void RequestAvailableTEXs(string mapName)
    {
        int X = (int)(min.x + ((max.x - min.x) / 2));
        int Y = (int)(min.x + ((max.x - min.x) / 2));
        int x, y, dx, dy;
        x = y = dx = 0;
        dy = -1;
        int t = Mathf.Max((int)max.x + 1, (int)max.y) + 1;
        int maxI = t * t;
        for (int i = 0; i < maxI; i++)
        {
            //if (((x + X) > 0) && ((x + X) < 128) && ((y + Y) > 0) && ((y + Y) < 128))
            //if ((-X / 2 <= x) && (x <= X / 2) && (-Y / 2 <= y) && (y <= Y / 2))
            if (((x + X) > 0) && ((x + X) < maxI) && ((y + Y) > 0) && ((y + Y) < maxI))
            {
                if (minimapAvailability[x + X, y + Y])
                {
                    MinimapRequest minimapRequest = new MinimapRequest();
                    minimapRequest.mapName = mapName;
                    minimapRequest.coords = new Vector2(x + X, y + Y);
                    RequestBlock(minimapRequest);
                }
            }
            if ((x == y) || ((x < 0) && (x == -y)) || ((x > 0) && (x == 1 - y)))
            {
                t = dx;
                dx = -dy;
                dy = t;
            }
            x += dx;
            y += dy;
        }
    }

    // Request a minimap image from the parser //
    private static void RequestBlock(MinimapRequest minimapRequest)
    {
        string mapName = minimapRequest.mapName;
        int x = (int)minimapRequest.coords.x;
        int y = (int)minimapRequest.coords.y;
        string fileName = mapName + "." + x.ToString("X").ToLower() + y.ToString("X").ToLower() + ".tex";
        string path = @"AIDX\Map\" + mapName + @"\" + fileName;
        if (DataManager.fileList.ContainsKey(path))
        {
            TexFile texFile = new TexFile();
            byte[] data = texFile.Read(path);
            MinimapBlockData blockData = new MinimapBlockData();
            blockData.mapName = mapName;
            blockData.coords = minimapRequest.coords;
            blockData.textureInfo = texFile.header2;
            blockData.minimapByteData = data;
            MinimapDataQueue.Enqueue(blockData);
        }
    }

    public struct MinimapRequest
    {
        public string mapName;
        public Vector2 coords;
    }

    public struct MinimapBlockData
    {
        public string mapName;
        public Vector2 coords;
        public byte[] minimapByteData;
        public TexFile.HeaderV2 textureInfo;
    }
}