using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FileInstance : MonoBehaviour
{
    public string fileName;
    public Archive.FileEntry fileEntry;

    public void Clicked()
    {
        ArchiveBrowser.previousSelectedFileIcon = ArchiveBrowser.currentSelectedFileIcon;
        ArchiveBrowser.currentSelectedFileIcon = gameObject;
        ArchiveBrowser.currentSelected = fileName;
        ArchiveBrowser.interacted = true;
    }
}
