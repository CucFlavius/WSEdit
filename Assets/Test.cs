using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class Test : MonoBehaviour {

    public GameObject textureTestsPanel;
    public Terrain terrain;
    public UnityEngine.UI.RawImage textureTest;
    //private List<UnityEngine.UI.RawImage> images;


    // Use this for initialization
    void Start ()
    {
        DataManager dm = new DataManager();
        dm.InitializeData();
        
        //string testTexture = @"AIDX\Art\Creature\AgressorBot\AgressorBot_Color.tex";
        //string testTexture = @"AIDX\UI\Assets\TexPieces\UI_CRB_Anim_Notification_BracketsClose.tex";

        /*
        if (textureTestsPanel.activeSelf)
        {
            TextureFormat[] values = new TextureFormat[] {TextureFormat.Alpha8, TextureFormat.ARGB32, TextureFormat.ARGB4444, TextureFormat.ASTC_RGBA_10x10, TextureFormat.ASTC_RGBA_12x12,
            TextureFormat.ASTC_RGBA_4x4, TextureFormat.ASTC_RGBA_5x5, TextureFormat.ASTC_RGBA_6x6, TextureFormat.ASTC_RGBA_8x8, TextureFormat.ASTC_RGB_10x10, TextureFormat.ASTC_RGB_12x12,
            TextureFormat.ASTC_RGB_4x4, TextureFormat.ASTC_RGB_5x5, TextureFormat.ASTC_RGB_6x6, TextureFormat.ASTC_RGB_8x8, TextureFormat.BC4, TextureFormat.BC5, TextureFormat.BC6H,
            TextureFormat.BC7, TextureFormat.BGRA32, TextureFormat.DXT1, TextureFormat.DXT1Crunched, TextureFormat.DXT5, TextureFormat.DXT5Crunched, TextureFormat.EAC_R, TextureFormat.EAC_RG,
            TextureFormat.EAC_RG_SIGNED, TextureFormat.EAC_R_SIGNED, TextureFormat.ETC2_RGB, TextureFormat.ETC2_RGBA1, TextureFormat.ETC2_RGBA8, TextureFormat.ETC2_RGBA8Crunched, TextureFormat.ETC_RGB4,
            TextureFormat.ETC_RGB4Crunched, TextureFormat.ETC_RGB4_3DS, TextureFormat.ETC_RGBA8_3DS, TextureFormat.PVRTC_RGB2, TextureFormat.PVRTC_RGB4, TextureFormat.PVRTC_RGBA2, TextureFormat.PVRTC_RGBA4,
            TextureFormat.R16, TextureFormat.R8, TextureFormat.RFloat, TextureFormat.RG16, TextureFormat.RGB24, TextureFormat.RGB565, TextureFormat.RGB9e5Float, TextureFormat.RGBA32, TextureFormat.RGBA4444,
            TextureFormat.RGBAFloat, TextureFormat.RGBAHalf, TextureFormat.RGFloat, TextureFormat.RGHalf, TextureFormat.RHalf, TextureFormat.YUY2};

            int increment = 0;

            foreach (Transform child in textureTestsPanel.transform)
            {
                TexFile texFile = new TexFile();

                child.name = values[increment].ToString();
                child.GetComponent<UnityEngine.UI.RawImage>().texture = texFile.ReadToTexture2DAllDecoders(testTexture, values[increment]);
                increment++;
            }
        }
        */
        /*
        if(textureTest.gameObject.activeSelf)
        {
            TexFile texFile = new TexFile();
            textureTest.texture = texFile.ReadToTexture2DAllDecoders(testTexture, TextureFormat.BGRA32);
        }
        */

        /*
        TexFile texFile1 = new TexFile();
        byte[] data = texFile1.Read(testTexture);
		Console.Log("Export Size : " + data.Length);
        System.IO.File.WriteAllBytes(@"D:\export.data", data);
        */

        /*
        Area area = new Area();
        area.Load(@"AIDX\Map\AdventureAstrovoidPrison\AdventureAstrovoidPrison.3f3f.area");
        //area.Load(@"AIDX\Map\AdventureAstrovoidPrison\AdventureAstrovoidPrison.403f.area");

        terrain.Initialize();
        terrain.GenerateAreaTerrain();
        */

        /*
        TBL.DataTable data = TBL.Read(@"AIDX\DB\WorldLayer.tbl");

        foreach (List<object> dataRow in data.dataRows)
        {
            string s = " | ";
            for (int c = 0; c < data.ColumnTypes.Count; c++)
            {
                s += Convert.ChangeType(dataRow[c], data.ColumnTypes[c]).ToString() + " | ";
            }
            Console.Log(s);
        }
        */
    }
	
	// Update is called once per frame
	void Update ()
    {
		
	}
}
