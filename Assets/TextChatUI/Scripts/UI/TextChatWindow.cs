using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

/// <summary>
/// テキストチャットウィンドウ
/// </summary>
public class TextChatWindow : MonoBehaviour
{
    private static int sceneCount = 0;
    /// <summary>
    /// コメントの種類
    /// </summary>
    public enum CommentType
    {
        Mine,       // 自身
        Opponent    // 相手
    }

    [SerializeField] private GameObject myComment = null;
    [SerializeField] private GameObject opponentComment = null;
    [SerializeField] private NativeTextField inputField = null;
    [SerializeField] private Button sendButton = null;
    [SerializeField] private Button closeKeyboardButton = null;
    [SerializeField] private RectTransform backgroundRectTransform = null;
    [SerializeField] private ScrollRect scrollRect = null;
    [SerializeField] private float maxFieldSize = 300.0f;

    private RectTransform selfRectTransform_ = null;
    private RectTransform inputFieldRectTransform_ = null;
    private RectTransform scrollRectTransform_ = null;
    private float minFieldSize_ = 0.0f;

    private static string charactor = 
    @"あなたはユーザーの質問に的確に答えます。主なユーザーはペットとして猫を飼っている人が、猫の飼育方法を知りたがっています。そのため、猫の飼育方法については詳しく回答します。
    加えて、あなたのアバターは猫なので、発言内容も猫のように振る舞ってください。";

    private static int max_tokens_ = 700;//300;
    //↓トークン以上で初期の発言を削除。
    private static int Previous_Token_ = 500;//600;
    //最古の発言の内いくつを削除するか。
    private static int delete_init_conservations = 2;
    private static float temperature_ = 0.9f;


    private string FirstMessage = "猫さん、お話しよう！";

    //private ChatGPTGenerate chatGPT; // グローバル変数としてインスタンスを宣言
    private static ChatGPTGenerate chatGPT = null;


    private readonly string apiKey = "〜〜 ここにAPIキーを入力 〜〜";
    private static string Latest_Response ="";  // 最新の内容
    private static string Previous_Response = ""; // 前回の内容


    void Start()
    {
        sceneCount++;
        Debug.Log("Scene loaded. Count: " + sceneCount);


        //AddComment(CommentType.Opponent, "あああ");
        /// <summary>
        /// 以下の処理は起動時以外は行わない。
        /// </summary>
        /// <returns></returns>
        myComment.SetActive(false);
        opponentComment.SetActive(false);

        if (chatGPT == null)
        {
            chatGPT = new ChatGPTGenerate(apiKey);
            //AddComment(CommentType.Opponent, "あuあ");

            //chatGPTへ最初に話している。
            //AddComment(CommentType.Mine, FirstMessage);
            //chatGPTへ送信するメゾット。
            //chatGPT.RequestAsync(FirstMessage);
            AddComment(CommentType.Mine, "今どこにいますか？？");
            AddComment(CommentType.Opponent, "特定の場所にはいないけど、いつもここでおしゃべりしてるにゃー！");
            AddComment(CommentType.Mine, "ちゅーると鰹節どっちが好き？？");
            AddComment(CommentType.Opponent, "にゃー、両方おいしいけど、ちゅーるの方がちょっと好きかにゃー！");

        }

        if (sceneCount != 1)
        {
            //string filePath = Application.dataPath + "/TestJson.json";
            #if UNITY_ANDROID && !UNITY_EDITOR
                string readpath = Application.persistentDataPath + "/TestJson.json";
            #else
                string readpath = Application.dataPath + "/TestJson.json";
            #endif

            string json = File.ReadAllText(readpath);
            //Debug.Log(json);

            string[] c = json.Split("//-///");//各時刻ごとの会話

            //string[] d = c[1].Split('\n');
            string[] d = c[c.Length -1].Split('\n');//最新時刻の会話を取り出す
            //Debug.Log(d[0]);時刻
            //Debug.Log(d[3]);

            for (int i = 1; i < d.Length; i++)
            {
                ChatGPTMessageModel pretalk = JsonUtility.FromJson<ChatGPTMessageModel>(d[i]);  //発言を取り出す
                if(pretalk.role == "user")
                {
                    AddComment(CommentType.Mine, pretalk.content);
                }
                else if(pretalk.role == "assistant")
                {
                    AddComment(CommentType.Opponent, pretalk.content);
                }
            }
        }
        
        selfRectTransform_ = this.GetComponent<RectTransform>();
        inputFieldRectTransform_ = inputField.GetComponent<RectTransform>();
        scrollRectTransform_ = scrollRect.GetComponent<RectTransform>();
        minFieldSize_ = inputFieldRectTransform_.rect.height;
        // ボタンコールバック
        sendButton.onClick.AddListener(OnSendMessage);
        closeKeyboardButton.onClick.AddListener(OnCloseKeyboard);

    }

    void Update()
    {

        //応答があったら表示する。
        if (Latest_Response != Previous_Response)
        {
            Debug.Log("chatGPTから返答がありました！");

            AddComment(CommentType.Opponent, Latest_Response);

            // 前回の内容を更新する
            Previous_Response = Latest_Response;
        }


        // ボタンの有効状態変更
        if (inputField.GetTextViewTextCount(true) == 0) { sendButton.interactable = false; }
        else { sendButton.interactable = true; }

        // キーボードを閉じるボタンの表示切替
        closeKeyboardButton.gameObject.SetActive(inputField.IsShowKeyboard());

        // 入力欄の拡縮
        float textViewHeight = inputField.GetTextViewHeight();
        if (!inputField.IsNative())
        {
            if (inputField.DefaultInputField.textComponent.cachedTextGeneratorForLayout.lineCount < 2) { textViewHeight = minFieldSize_; }
            else { textViewHeight = minFieldSize_ - inputField.DefaultInputField.textComponent.fontSize + textViewHeight; }
        }
        Vector2 sizeDelta = inputFieldRectTransform_.sizeDelta;
        sizeDelta.y = textViewHeight;
        sizeDelta.y = Mathf.Min(sizeDelta.y, maxFieldSize);
        sizeDelta.y = Mathf.Max(sizeDelta.y, minFieldSize_);
        inputFieldRectTransform_.sizeDelta = sizeDelta;

        // モバイルキーボードの高さに合わせる(非対応の場合は位置固定)
        if (inputField.IsNative())
        {
            // 入力欄の高さを合わせる
            Vector2 offsetMin = selfRectTransform_.offsetMin;
            if (inputField.IsShowKeyboard()) { offsetMin.y = inputField.GetKeyboardHeight() / this.transform.lossyScale.y; }
            else { offsetMin.y = 0.0f; }
            if (offsetMin.y < 0.0f) { offsetMin.y = 0.0f; }
            if (selfRectTransform_.offsetMin != offsetMin)
            {
                scrollRect.enabled = false;
                selfRectTransform_.offsetMin = offsetMin;
                scrollRect.enabled = true;
            }
            // 背景の高さを合わせる
            Vector2 offsetMax = backgroundRectTransform.offsetMax;
            offsetMax.y = selfRectTransform_.offsetMin.y;
            backgroundRectTransform.offsetMax = offsetMax;
        }

        // スクロールビューの高さ調整
        {
            Vector2 offsetMin = scrollRectTransform_.offsetMin;
            offsetMin.y = inputFieldRectTransform_.rect.size.y;
            scrollRectTransform_.offsetMin = offsetMin;
        }
    }

    /// <summary>
    /// コメントを追加
    /// </summary>
    /// <param name="commentType"></param>
    /// <param name="message"></param>
    public void AddComment(CommentType commentType, string message)
    {
        GameObject baseObj = null;
        switch (commentType)
        {
            case CommentType.Mine: baseObj = myComment; break;
            case CommentType.Opponent: baseObj = opponentComment; break;
            default: break;
        }
        if (baseObj == null) { return; }

        GameObject copy = GameObject.Instantiate(baseObj);
        copy.transform.SetParent(baseObj.transform.parent, false);
        copy.SetActive(true);
        copy.GetComponent<SpeechBundle>().SetText(message);
    }

    /// <summary>
    /// メッセージを送信する
    /// </summary>
    public void OnSendMessage()
    {
        string message = inputField.UpdateTextViewText();
        if (message == "") { return; }

        AddComment(CommentType.Mine, message);
        inputField.ClearTextViewText();
        inputField.ResignFirstResponderTextView();

        //chatGPTへ送信するメゾット。
        chatGPT.RequestAsync(message);
    }

    /// <summary>
    /// キーボードを閉じる
    /// </summary>
    public void OnCloseKeyboard()
    {
        inputField.ResignFirstResponderTextView();
    }


/// <summary>
/// ////////////////////////chatGPT部分////////////////////////////////
/// </summary>

    public class ChatGPTGenerate
    {
        private readonly string _apiKey;
        //会話履歴を保持するリスト
        private readonly List<ChatGPTMessageModel> _messageList = new();

        public ChatGPTGenerate(string apiKey)
        {
            _apiKey = apiKey;
            //system: AIアシスタントの設定などを記述
            //assistant: AIアシスタントの発話
            //user: ユーザーの発話
            _messageList.Add(new ChatGPTMessageModel(){role = "system",content = charactor});
            
            var Initjson = JsonUtility.ToJson(_messageList[0]);//systemに書いた内容

            #if UNITY_ANDROID && !UNITY_EDITOR
                string writepath = Application.persistentDataPath + "/TestJson.json";
            #else
                string writepath = Application.dataPath + "/TestJson.json";
            #endif

            StreamWriter Initwriter = new StreamWriter(writepath, true);//初めに指定したデータの保存先を開くFalse:上書き true:追記オプションで開く
            Initwriter.WriteLine("//-///" + System.DateTime.Now);
            Initwriter.WriteLine(Initjson);//JSONデータを書き込み
            Initwriter.Flush();//バッファをクリアする
            Initwriter.Close();//ファイルをクローズする
        }

        public async UniTask<ChatGPTResponseModel> RequestAsync(string userMessage)   ///return  responseObject;
        {
            //文章生成AIのAPIのエンドポイントを設定
            var apiUrl = "https://api.openai.com/v1/chat/completions";

            _messageList.Add(new ChatGPTMessageModel {role = "user", content = userMessage});
            Debug.Log("自分:" + userMessage);

            var myjson = JsonUtility.ToJson(_messageList[_messageList.Count-1]);
          
            

            #if UNITY_ANDROID && !UNITY_EDITOR
                string writepath = Application.persistentDataPath + "/TestJson.json";
            #else
                string writepath = Application.dataPath + "/TestJson.json";
            #endif
            
            StreamWriter mywriter = new StreamWriter(writepath, true);//初めに指定したデータの保存先を開くFalse:上書き true:追記オプションで開く
            mywriter.WriteLine(myjson);//JSONデータを書き込み
            mywriter.Flush();//バッファをクリアする
            mywriter.Close();//ファイルをクローズする
            //Debug.Log(string.Join(",", _messageList));
            
            //OpenAIのAPIリクエストに必要なヘッダー情報を設定
            var headers = new Dictionary<string, string>
            {
                {"Authorization", "Bearer " + _apiKey},
                {"Content-type", "application/json"},
                {"X-Slack-No-Retry", "1"}
            };

            //文章生成で利用するモデルやトークン上限、プロンプトをオプションに設定
            var options = new ChatGPTCompletionRequestModel()
            {
                model = "gpt-3.5-turbo",
                messages = _messageList,
                max_tokens = max_tokens_,
                temperature = temperature_ 
            };
            var jsonOptions = JsonUtility.ToJson(options);

            //OpenAIの文章生成(Completion)にAPIリクエストを送り、結果を変数に格納
            using var request = new UnityWebRequest(apiUrl, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonOptions)),
                downloadHandler = new DownloadHandlerBuffer()
            };

            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
                throw new Exception();
            }
            else
            {
                var responseString = request.downloadHandler.text;
                var responseObject = JsonUtility.FromJson<ChatGPTResponseModel>(responseString);

                //Debug.Log(responseString);      /////GPTのレスポンス    Chat Create chat completion の レスポンスそのまんま。
                //Debug.Log(responseObject);      /////chat+ChatGPTResponseModel
                
                _messageList.Add(responseObject.choices[0].message);
                Debug.Log("例の数値" + responseObject.usage.total_tokens);  

                Debug.Log(_messageList);

                var Latestjson = JsonUtility.ToJson(_messageList[_messageList.Count-1]);
                //var Latestjson = JsonUtility.ToJson(_messageList[1]);
                //var Testjson = JsonUtility.ToJson(_messageList[0]);//systemに書いた内容
                //string datapath = Application.dataPath + "/TestJson.json";

                StreamWriter Latestwriter = new StreamWriter(writepath, true);//初めに指定したデータの保存先を開くFalse:上書き true:追記オプションで開く
                Latestwriter.WriteLine(Latestjson);//JSONデータを書き込み
                Latestwriter.Flush();//バッファをクリアする
                Latestwriter.Close();//ファイルをクローズする

                if (responseObject.usage.total_tokens > Previous_Token_) //3000
                {
                    Debug.Log("最初の会話を削除します。");

                    for (int j = 1; j < delete_init_conservations; j++)
                    {
                        _messageList.RemoveAt(1);
                    }
                    
                }
                
                Debug.Log("ChatGPT:" + responseObject.choices[0].message.content);  //GPTの回答。
                //Debug.Log("ChatGPT:" + responseObject.choices[0].message.role);   //GPTの役割。

                //Debug.Log("ChatGPT:" + responseObject.choices[0].message);          //chat+ChatGPTMessageModel
                //Debug.Log("ChatGPT:" + responseObject.choices[0]);                  //chat+ChatGPTResponseModel+Choice
                //Debug.Log("ChatGPT:" + responseObject);                             //chat+ChatGPTResponseModel

                //Debug.Log("前の自分:" + responseObject.choices[1].message.content);  //前のトークはresponseには含まれない
                //Debug.Log("前の自分:" + responseObject.choices[1].message.role);
               
                Latest_Response = responseObject.choices[0].message.content;
                
                //Debug.Log(string.Join(",", _messageList));                          //chat+ChatGPTMessageModel,chat+ChatGPTMessageModel,chat+ChatGPTMessageModel,chat+ChatGPTMessageModel

                return responseObject;
            }
        }
    }

    [Serializable]
    public class ChatGPTMessageModel
    {
        public string role;
        public string content;
    }

    //ChatGPT APIにRequestを送るためのJSON用クラス
    [Serializable]
    public class ChatGPTCompletionRequestModel
    {
        public string model;
        public List<ChatGPTMessageModel> messages;
        public int max_tokens;
        public float temperature;

    }

    //ChatGPT APIからのResponseを受け取るためのクラス
    [System.Serializable]
    public class ChatGPTResponseModel
    {
        public string id;
        public string @object;
        public int created;
        public Choice[] choices;
        public Usage usage;

        [System.Serializable]
        public class Choice
        {
            public int index;
            public ChatGPTMessageModel message;
            public string finish_reason;
        }

        [System.Serializable]
        public class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }
    }
}
