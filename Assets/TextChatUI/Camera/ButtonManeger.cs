using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;

using UnityEngine.SceneManagement;

public class ButtonManeger : MonoBehaviour
{


    float longTapTime = 1.8f;               // ロングタップと判定する秒数
    float nowTapTime;                       // タップしてからの時間
  
    public AudioClip sound1;
    AudioSource audioSource;

    // Start is called before the first frame update
    void Start(){audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
 
    void Update()
    {
        if (Input.GetMouseButton(0))
            {
                nowTapTime += Time.deltaTime;   // 秒数をカウント

                // タップし続けた時間が規定値を超えたらロングタップとして扱う
                if (nowTapTime >= longTapTime )
                {
                    nowTapTime = 0;             // タイマーリセット
    
                    Debug.Log("戻ります。");
                    SceneManager.LoadScene("TextChatUI");
                }
                Debug.Log("Long Tap");

            }
            else if (Input.GetMouseButtonUp(0))
            {
                // クリックを終えたら初期化
                nowTapTime = 0;
                //Debug.Log("スクリーンショットを取ります。");
                //TestCapture.ScreenShot ();
                //Debug.Log("スクリーンショットを取りました。");
                //audioSource.PlayOneShot(sound1);

                
            }
        
    }


    


    
}
