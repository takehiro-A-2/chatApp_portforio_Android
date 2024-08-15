using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class json : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //var Initjson = JsonUtility.ToJson(_messageList[0]);//systemに書いた内容
            string filePath = Application.dataPath + "/TestJson.json";
            string json = File.ReadAllText(filePath);
            //Debug.Log(json);

            string[] c = json.Split("//-///");

            Debug.Log(c[0]);
            //Debug.Log(c[c.Length -1]);
            Debug.Log(c[1]);

            //string[] d = c[1].Split('\n');
            string[] d = c[c.Length -1].Split('\n');
            Debug.Log(d[0]);
            Debug.Log(d[3]);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
