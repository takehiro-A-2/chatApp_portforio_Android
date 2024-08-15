using System.Collections;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;

public class ScreenShot : MonoBehaviour
{

    public Canvas Canvas_UI;
    public Canvas Canvas0;
    //public Toggle Toggletext;
    [DllImport("__Internal")]
    private static extern void SaveToAlbum(string path);

    public void CameraMode()
    {
        Canvas_UI.gameObject.SetActive (false);
        Canvas0.gameObject.SetActive (true);
    }

    IEnumerator SaveToCameraroll(string path)
    {
        // ファイルが生成されるまで待つ
        while(true)
        {
            if(File.Exists(path))
                break;            
            yield return null;
        }
        
        SaveToAlbum(path);
    }

    //void Update()
    public void ScreenShotButton()
    {
        //if(Input.GetMouseButtonDown(0))
        //{
#if UNITY_EDITOR
#else
        string filename = "test.png";
        string path = Application.persistentDataPath + "/" + filename;

        // 以前のスクリーンショットを削除する
        File.Delete(path);

        // スクリーンショットを撮影する
        ScreenCapture.CaptureScreenshot(filename);
        
        // カメラロールに保存する
        StartCoroutine(SaveToCameraroll(path));
#endif
    //}
}
}