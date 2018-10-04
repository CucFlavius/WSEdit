using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class ArchiveBrowser : MonoBehaviour
{
    public GameObject fileProperties;
    public GameObject specificProperties;
    public GameObject folderIconPrefab;
    public GameObject fileIconPrefab;
    public GameObject iconArea;
    public GameObject BreadCrumb;
    public GameObject texturePreview;
    public Sprite[] icons;

    public static GameObject previousSelectedFileIcon;
    public static GameObject currentSelectedFileIcon;

    private bool previouslyOpened = false;
    private string previousPath = "AIDX";
    private string previousSelected = "";

    public static string currentPath = "AIDX";
    public static string currentSelected = "";
    public static bool interacted = false;

    private Dictionary<string, Archive.FileEntry> fileList;

    private void Update()
    {
        if (interacted)
        {
            interacted = false;
            // clicked folder //
            if (currentPath != previousPath)
            {
                previousPath = currentPath;
                BreadCrumb.GetComponent<UnityEngine.UI.Text>().text = currentPath;
                OpenFolder();
            }
            // clicked file //
            if (currentSelected != previousSelected)
            {
                previousSelected = currentSelected;
                if (previousSelectedFileIcon != null)
                    previousSelectedFileIcon.GetComponent<UnityEngine.UI.Image>().color = Color.white;
                currentSelectedFileIcon.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 1, .2f);
                string infoText = "";
                infoText += currentPath + "\\" + currentSelected + "\n";
                //infoText += "Name Offset: " + fileList[currentSelected].nameOffset + "\n";
                string compression = "Uncompressed";
                if (fileList[currentSelected].flags == 3)
                    compression = "ZLib";
                else if (fileList[currentSelected].flags == 5)
                    compression = "LZMA";
                infoText += "Compression: " + fileList[currentSelected].flags + " " + compression + "\n";
                DateTime date = new DateTime((long)fileList[currentSelected].writeTime);
                infoText += "Write Time: " + date.Hour + ":" + date.Minute + " - " + date.Day + "\\" + date.Month + "\\" + date.Year + "\n";
                infoText += "Uncompressed Size: " + fileList[currentSelected].uncompressedSize + "\n";
                infoText += "Compressed Size: " + fileList[currentSelected].compressedSize + "\n";
                infoText += "Hash: " + DataManager.ToHex(fileList[currentSelected].hash, false) + "\n";
                infoText += "Unknown: " + fileList[currentSelected].unk2 + "\n";
                fileProperties.GetComponent<UnityEngine.UI.Text>().text = infoText;

                DataManager.ExtractFile(currentPath + "\\" + currentSelected);
                //DataManager.GetFileBytes(currentPath + "\\" + currentSelected);

                // Texture File //
                if (Path.GetExtension(currentSelected).ToLower() == ".tex")
                {
                    TexFile texFile = new TexFile();

                    texFile.Read(currentPath + @"\" + currentSelected);
                    Texture2D tex = texFile.ReadToTexture2D(currentPath + @"\" + currentSelected);
                    texturePreview.GetComponent<UnityEngine.UI.RawImage>().texture = tex;

                    string texPropertiesString = "";
                    texPropertiesString += "Type: " + texFile.header2.version + "\n";
                    texPropertiesString += "Dimensions: " + texFile.header2.width + "x" + +texFile.header2.height + "\n";
                    texPropertiesString += "Depth: " + texFile.header2.depth + "\n";
                    texPropertiesString += "Sides: " + texFile.header2.faces + "\n";
                    texPropertiesString += "MipmapCount: " + texFile.header2.mipCount + "\n";
                    string format = texFile.header2.format.ToString();
                    /*
                    if (texFile.header2.format == 13)
                        format = "DXT1";
                    else if (texFile.header2.format == 14)
                        format = "DXT3";
                    else if (texFile.header2.format == 15)
                        format = "DXT5";
                        */
                    texPropertiesString += "Format: " + format + "\n";
                    texPropertiesString += "Sizes: " + texFile.header2.sizes[0] + " " + texFile.header2.sizes[1] + " " + texFile.header2.sizes[2] + " " + texFile.header2.sizes[3] + " " + texFile.header2.sizes[4] + " " + texFile.header2.sizes[5] + " " +
                        texFile.header2.sizes[6] + " " + texFile.header2.sizes[7] + " " + texFile.header2.sizes[8] + " " + texFile.header2.sizes[9] + " " + texFile.header2.sizes[10] + " " + texFile.header2.sizes[11] + " " + texFile.header2.sizes[12] + " " +
                        texFile.header2.sizes[13] + " " + "\n";
                    texPropertiesString += "unk1: " + texFile.header2.unk1 + "\n";
                    texPropertiesString += "unk2: " + texFile.header2.unk2[0] + " " + texFile.header2.unk2[1] + " " + texFile.header2.unk2[2] + " " + "\n";
                    texPropertiesString += "unk3: " + texFile.header2.unk3 + "\n";
                    specificProperties.GetComponent<UnityEngine.UI.Text>().text = texPropertiesString;

                }
            }
        }
    }

    private void OnEnable()
    {
        // File Browser Panel was opened //
        if (!previouslyOpened)
        {
            previouslyOpened = true;
            List<string> folderList = DataManager.GetFolderList("AIDX");
            if (folderList.Count > 0)
            {
                foreach (string folder in folderList)
                {
                    CreateFolderIcon(folder);
                }
            }
        }
    }

    public void OpenFolder()
    {
        Clear();
        List<string> folderList = DataManager.GetFolderList(currentPath);
        if (folderList.Count > 0)
        {
            foreach (string folder in folderList)
            {
                CreateFolderIcon(folder);
            }
        }
        fileList = DataManager.GetFileList(currentPath);
        if (fileList.Count > 0)
        {
            foreach (KeyValuePair<string, Archive.FileEntry> file in fileList)
            {
                CreateFileIcon(file.Key, file.Value);
            }
        }

    }

    public void UpOneFolder()
    {
        if (currentPath != "AIDX")
        {
            currentPath = System.IO.Path.GetDirectoryName(currentPath);
            interacted = true;
        }
    }

    private void CreateFolderIcon(string Name)
    {
        GameObject folderIcon = Instantiate(folderIconPrefab);
        folderIcon.GetComponent<FolderInstance>().folderName = Name;
        folderIcon.transform.SetParent(iconArea.transform);
        folderIcon.transform.localScale = Vector3.one;
        folderIcon.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = Name;
    }

    private void CreateFileIcon(string Name, Archive.FileEntry FileInfo)
    {
        GameObject fileIcon = Instantiate(fileIconPrefab);
        fileIcon.GetComponent<FileInstance>().fileName = Name;
        fileIcon.GetComponent<FileInstance>().fileEntry = FileInfo;
        fileIcon.transform.SetParent(iconArea.transform);
        fileIcon.transform.localScale = Vector3.one;
        fileIcon.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = Name;

        if (Path.GetExtension(Name).ToLower() == ".tex")
        {
            fileIcon.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().sprite = icons[0];
        }
    }

    private void Clear()
    {
        foreach (Transform child in iconArea.transform)
        {
            Destroy(child.gameObject);
        }
    }

}
