using UnityEngine;

public class FolderInstance : MonoBehaviour {

    public string folderName;

    public void Clicked()
    {
        ArchiveBrowser.currentPath += "\\" + folderName;
        ArchiveBrowser.interacted = true;
        //Console.Log(ArchiveBrowser.currentPath);
    }

}
