using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SimpleJSON;
using System.Net;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class WordsGame : MonoBehaviour, IHasChanged {
    [SerializeField] Transform slots;
    [SerializeField] GameObject prefabWord, cover, scoreBoard, endGame;
    [SerializeField] private GameObject defaultTile, recallBtn, shuffleBtn;
    [SerializeField] AudioSource slotAudio;
    [SerializeField] private Text scoreText, timer, whosturn;
    [SerializeField] int jumlahRound = 2;

    public ApiControl api;
    //singleton
    private static WordsGame _instance;
    private int turn = 1, enemyTurn = 1;

    //tempat untuk dict words
    private HashSet<string> dicWords = new HashSet<string>();
    private TextAsset dictText;
    private HashSet<string> wordInPoint = new HashSet<string>();
    private HashSet<string> wordInSend = new HashSet<string>();
    //tempat untuk semua data word dan point dari scritable object
    private Word[] wordList;


    //Kebutuhan untuk grid dan input player
    public int[][] wordSet = new int[6][];
    public MyControll[][] grid = new MyControll[15][];

    //Score pemain
    private int myScore = 0, enemyScore;
    public JSONNode roomData, user_rmData, user_guestData, enemyData;

    //variable untuk cek apakah point enemy sudah di set
    private bool isEnemyPointSet = false;
    private int checkTime = 0; // sudah berapa detik / kali di check batas 5
    public static WordsGame Instance { get { return _instance; } }
    private const string ipadd = "172.16.8.162:45456";
    void Awake()
    {
        scoreText.text = PlayerPrefs.GetInt("room_id").ToString() + " " + System.DateTime.Now;
        StartCoroutine(GetRequest("https://"+ipadd+"/api/values/" + PlayerPrefs.GetInt("room_id"), returnValue => {
            roomData = JSON.Parse(returnValue);

            //Get detail user rm
            StartCoroutine(GetRequest("https://" + ipadd + "/api/user/" + roomData["user_rm"], rval => {
                user_rmData = JSON.Parse(rval);
            }));

            //Get detail user guest
            StartCoroutine(GetRequest("https://" + ipadd + "/api/user/" + roomData["user_guest"], rguest => {
                user_guestData = JSON.Parse(rguest);
                
            }));

            JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
            j.AddField("id", roomData["id"].ToString());
            j.AddField("user_rm", roomData["user_rm"].ToString());
            j.AddField("user_guest", roomData["user_guest"].ToString());
            j.AddField("status", 2);
            StartCoroutine(PostRequest("https://" + ipadd + "/api/start/", j.Print(), myCallback => {
            }));

            JSONObject k = new JSONObject(JSONObject.Type.OBJECT);

            if (PlayerPrefs.GetInt("user_id") == roomData["user_rm"])
            {
                k.AddField("id", roomData["id"].ToString());
                k.AddField("user_rm", roomData["user_rm"].ToString());
                k.AddField("ready_p1", "1");
            }
            else
            {
                k.AddField("id", roomData["id"].ToString());
                k.AddField("user_guest", roomData["user_guest"].ToString());
                k.AddField("ready_p2", "1");
            }

            StartCoroutine(PostRequest("https://" + ipadd + "/api/start/0", k.Print(), rReady => {
                InvokeRepeating("startPlay", 0f, 1f);
            }));
        }));

        

        InitializeDictionary("ospd"); //load dict word
        wordList = Resources.LoadAll<Word>("Words"); //Data word scriptable object
        RandomWord(); //acak kata yang akan di tampilkan

        //instance class WordsGame
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else _instance = this;

        //init array untuk grid
        for (int i = 0; i < grid.Length; i++) grid[i] = new MyControll[15];

        //init array untuk satu set huruf yang sedang dipakai
        //array ini digunakan sebagai tracking huruf di grid keberapa
        for (int i2 = 0; i2 < wordSet.Length; i2++) wordSet[i2] = new int[2];

        
    }
    void startPlay()
    {
        if (!checkPlay())
        {
            StartCoroutine(GetRequest("https://" + ipadd + "/api/values/" + PlayerPrefs.GetInt("room_id"), returnValue =>
            {
                roomData = JSON.Parse(returnValue);
                checkPlay();
            }));
        }
    }
    public bool checkPlay()
    {
        if (roomData["ready_p1"] == 1 && roomData["ready_p2"] == 1)
        {
            cover.SetActive(false);
            if (PlayerPrefs.GetInt("user_id") == user_guestData["id"])
            {
                scoreBoard.transform.parent.GetChild(0).GetComponent<Text>().text = user_guestData["name"];
                scoreBoard.transform.parent.GetChild(1).GetComponent<Text>().text = user_rmData["name"];
                enemyData = user_rmData;
                ChangeControl(false);
                StartCoroutine(waitForEnemy());
            }
            else
            {
                scoreBoard.transform.parent.GetChild(1).GetComponent<Text>().text = user_guestData["name"];
                scoreBoard.transform.parent.GetChild(0).GetComponent<Text>().text = user_rmData["name"];
                enemyData = user_guestData;
                StartCoroutine(autoSubmit());
            }
            CancelInvoke();
            return true;
        }
        return false;
    }
    void setScoreBoard(int enemyOrPlayer, int abcd, int score)
    {
        scoreBoard.transform.GetChild(enemyOrPlayer).GetChild(abcd - 1).GetChild(0).GetComponent<Text>().text = score.ToString();
    }
    void setEnemyWord (JSONNode data)
    {
        foreach(JSONNode list in data)
        {
            Word apiData = convert(list["data"]);
            var lokasi = slots.GetChild(list["row"]).GetChild(list["col"]);
            var row = (int)list["row"];
            var col = (int)list["col"];
            

            GameObject wordGenerate = Instantiate(prefabWord, lokasi);
            wordGenerate.transform.localScale = new Vector3(0.48f, 0.48f, 0.48f);
            wordGenerate.GetComponent<Image>().sprite = apiData.sprite;
            wordGenerate.GetComponent<MyControll>().huruf = apiData.huruf;
            wordGenerate.GetComponent<MyControll>().point = apiData.point;
            wordGenerate.transform.GetChild(0).GetComponent<Text>().text = apiData.point.ToString();
            grid[row][col] = wordGenerate.GetComponent<MyControll>();
        }
    }
    void checkExtend()
    {
        Debug.Log("check extend");
        checkTime++;
        JSONObject j = new JSONObject(JSONObject.Type.OBJECT);

        j.AddField("turn", enemyTurn);
        j.AddField("user_id", enemyData["id"].ToString());
        j.AddField("room_id", PlayerPrefs.GetInt("room_id"));

        if(checkTime > 10)
        {
            j.AddField("point", 0);
            StartCoroutine(PostRequest("https://" + ipadd + "/api/game", j.Print(), returnValue => {
                var data = JSON.Parse(returnValue);
                setScoreBoard(1, enemyTurn, data["point"]);
                setEnemyWord(data["list"]);
                enemyTurn++;
                enemyScore += data["point"];
                StartCoroutine(autoSubmit());
                checkForWinner();
            }));
            CancelInvoke();
        }
        else
        {
            Debug.Log("check" + checkTime);
            StartCoroutine(PostRequest("https://" + ipadd + "/api/game/" + enemyTurn, j.Print(), returnValue => {
                var data = JSON.Parse(returnValue);
                if (data["id"] != 0)
                {
                    CancelInvoke();
                    setScoreBoard(1, enemyTurn, data["point"]);
                    setEnemyWord(data["list"]);
                    enemyTurn++;
                    enemyScore += data["point"];
                    StartCoroutine(autoSubmit());
                    checkForWinner();
                }
            }));
        }
        
    }
    IEnumerator startCountDown(int angka)
    {
        float normalizedTime = angka;
        while (normalizedTime >= 0)
        {
            normalizedTime -= Time.deltaTime;
            var fix = (int)normalizedTime;
            timer.text = fix.ToString();
            yield return null;
        }
    }
    IEnumerator autoSubmit()
    {
        whosturn.text = "<color=white>Your turn</color>";
        ChangeControl(true);
        StartCoroutine(startCountDown(14));
        yield return new WaitForSeconds(14);
        submitWords();
        
    }
    IEnumerator waitForEnemy()
    {
        whosturn.text = "<color=red>Enemy's turn</color>";
        ChangeControl(false);
        if (turn > 2)
        {
            StartCoroutine(startCountDown(16));
            yield return new WaitForSeconds(16);
        }
        else
        {
            StartCoroutine(startCountDown(14 + turn));
            yield return new WaitForSeconds(14 + turn);
        }

        checkEnemyPoint();
    }
    void checkForWinner()
    {
        Text resultText = endGame.transform.GetChild(0).GetChild(0).GetComponent<Text>();
        if (turn == jumlahRound && turn == enemyTurn)
        {
            if (myScore == enemyScore)
                resultText.text = "DRAW";
            else
            {
                string winOrLose = myScore > enemyScore ? "WIN" : "LOSE";
                resultText.text = "You - " + winOrLose;
            }
            endGame.SetActive(true);
            Invoke("Quit", 5f);
        }
    }
    private void checkEnemyPoint()
    {
        JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
        
        j.AddField("turn", enemyTurn);
        j.AddField("user_id", enemyData["id"].ToString());
        j.AddField("room_id", PlayerPrefs.GetInt("room_id"));
        StartCoroutine(PostRequest("https://" + ipadd + "/api/game/" + enemyTurn, j.Print(), returnValue => {
            var data = JSON.Parse(returnValue);
            if(data["id"] == 0)
            {
                checkTime = 0;
                InvokeRepeating("checkExtend", 0f, 1f);
            }
            else
            {
                setScoreBoard(1, enemyTurn, data["point"]);
                setEnemyWord(data["list"]);
                enemyTurn++;
                enemyScore += data["point"];
                StartCoroutine(autoSubmit());
                checkForWinner();
            }
            
        }));
    }
    protected void InitializeDictionary(string filename)
    {
        dictText = (TextAsset)Resources.Load(filename, typeof(TextAsset));
        var text = dictText.text;

        foreach (string s in text.Split('\n'))
        {
            dicWords.Add(s);
        }
    }
    public bool CheckWord(string word, int minLength)
    {
        if (word.Length < minLength)
        {
            return false;
        }

        return (dicWords.Contains(word.ToUpper()));
    }

    private void ResetWordSet()
    {
        foreach(int[] word in wordSet)
        {
            word[0] = 0;
            word[1] = 0;
        }

    }
    private int checkForPoint(string hasil, int point)
    {
        if (CheckWord(hasil, 1))
        {
            if (wordInPoint.Contains(hasil))
                return 0;
            else
            {
                wordInPoint.Add(hasil);
                return point;
            }
        }
        else
            return 0;
    }

    private void clearControl()
    {
        foreach(int[] word in wordSet)
        {
            if(grid[word[0]][word[1]] != null)
            {
                grid[word[0]][word[1]].canDrag = false;
            }
        }
    }
    public void playSlotAudio()
    {
        slotAudio.Play();
    }
    void Quit()
    {
        JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
        j.AddField("id", roomData["id"].ToString());
        j.AddField("user_rm", roomData["user_rm"].ToString());
        j.AddField("user_guest", roomData["user_guest"].ToString());
        j.AddField("status", 3);
        StartCoroutine(PostRequest("https://" + ipadd + "/api/start/", j.Print(), myCallback => {
        }));
        
        PlayerPrefs.DeleteKey("room_id");
        SceneManager.LoadScene(0);
    }

    // INI KUMPULAN FUNGSI PENGECEKAN WORD SESUAI POLA MASING2

    //Pola Pengecekan nilai dari kanan ke kiri
    private int polaKiri(int row, int col)
    {
        string hasil = "";
        int point = 0;

        if (col == 0)
            return 0;
        else if (grid[row][col - 1] == null || grid[row][col] == null)
            return 0;

        for(int i = col; i >= 0; i--)
        {
            if(grid[row][i] != null)
            {
                hasil = grid[row][i].GetComponent<MyControll>().huruf + hasil;
                point += grid[row][i].GetComponent<MyControll>().point;
            }
            else
                break;
        }

        return checkForPoint(hasil, point);
    }
    //Pola pengecekan nilai dari kiri ke kanan
    public int polaKanan(int row, int col)
    {
        string hasil = "";
        int point = 0;
        if (col == 14)
            return 0;
        else if (grid[row][col + 1] == null || grid[row][col] == null)
            return 0;

        for (int i = col; i < 15; i++)
        {
            if (grid[row][i] != null) { 
                hasil += grid[row][i].GetComponent<MyControll>().huruf;
                point += grid[row][i].GetComponent<MyControll>().point;
            }
            else
                break;
        }
        return checkForPoint(hasil, point);
        
    }
    //Pola pengecekan nilai dari atas ke bawah
    public int polaBawah(int row, int col)
    {
        string hasil = "";
        int point = 0;

        if (row == 14)
            return 0;
        else if (grid[row + 1][col] == null || grid[row][col] == null)
            return 0;

        for (int i = row; i < 15; i++)
        {
            if (grid[i][col] != null)
            {
                hasil += grid[i][col].GetComponent<MyControll>().huruf;
                point += grid[i][col].GetComponent<MyControll>().point;
            }
            else
                break;
        }

        return checkForPoint(hasil, point);
    }
    //Pola pengecekan nilai dari bawah ke atas
    public int polaAtas(int row, int col)
    {
        string hasil = "";
        int point = 0;

        if (row == 0)
            return 0;
        else if (grid[row - 1][col] == null || grid[row][col] == null)
            return 0;

        for(int i = row; i >= 0; i--)
        {
            if (grid[i][col] != null)
            {
                hasil = grid[i][col].GetComponent<MyControll>().huruf + hasil;
                point += grid[i][col].GetComponent<MyControll>().point;
            }
            else
                break;
        }

        return checkForPoint(hasil, point);
    }
    //Pola pengecekan nilai serong bawah kanan
    public int polaKananBawah(int row, int col)
    {
        string hasil = "";
        int point = 0;
        int endLoop = col > row ? 15 - col : 15 - row;

        if (row == 14 || col == 14)
            return 0;
        else if (grid[row + 1][col + 1] == null || grid[row][col] == null)
            return 0;
        
        for(int i =0; i < endLoop; i++)
        {
            if (grid[row][col] != null)
            {
                hasil += grid[row][col].GetComponent<MyControll>().huruf;
                point += grid[row][col].GetComponent<MyControll>().point;
                row++;
                col++;
            }
            else
                break;
        }

        return checkForPoint(hasil, point);
    }
    //Pola pengecekan nilai seorang atas kiri
    public int polaKiriAtas(int row, int col)
    {
        string hasil = "";
        int point = 0;
        int endLoop = col > row ? row : col;

        if (row == 0 || col == 0)
            return 0;
        else if (grid[row - 1][col - 1] == null || grid[row][col] == null)
            return 0;

        for (int i = 0; i < endLoop; i++)
        {
            if (grid[row][col] != null)
            {
                hasil = grid[row][col].GetComponent<MyControll>().huruf + hasil;
                point += grid[row][col].GetComponent<MyControll>().point;
                row--;
                col--;
            }
            else
                break;
        }

        return checkForPoint(hasil, point);
    }

    //END KUMPULAN FUNGSI PENGECEKAN


    public void submitWords()
    {
        JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
        JSONObject list = new JSONObject(JSONObject.Type.OBJECT);
        int point = 0;

        foreach (int[] word in wordSet)
        {
            if(grid[word[0]][word[1]] != null) { 
                JSONObject data = new JSONObject(JSONObject.Type.OBJECT);
                data.AddField("row", word[0]);
                data.AddField("col", word[1]);
                data.AddField("data", grid[word[0]][word[1]].GetComponent<MyControll>().huruf);
                if (!wordInSend.Contains(word[0].ToString() + word[1].ToString()) && grid[word[0]][word[1]] != null)
                {
                    wordInSend.Add(word[0].ToString() + word[1].ToString());
                    list.Add(data);

                }
            }
            point += polaKanan(word[0], word[1]);
            point += polaKiri(word[0], word[1]);
            point += polaAtas(word[0], word[1]);
            point += polaBawah(word[0], word[1]);
            point += polaKananBawah(word[0], word[1]);
            point += polaKiriAtas(word[0], word[1]);
        }
        //Kebutuhan untuk di local game
        scoreText.text = "Score : " + myScore.ToString();
        clearControl();
        ResetWordSet();
        RandomWord();
        RecallOrShuffle();

        //Kebutuhan untuk pengiriman data ke API
        j.AddField("turn", turn);
        j.AddField("user_id", PlayerPrefs.GetInt("user_id"));
        j.AddField("room_id", PlayerPrefs.GetInt("room_id"));
        j.AddField("point", point);
        j.AddField("list", list);


        StartCoroutine(PostRequest("https://" + ipadd + "/api/game", j.Print(), returnValue => {
            var data = JSON.Parse(returnValue);
            setScoreBoard(0, turn, data["point"]);
            myScore += data["point"];
            turn++;
            StartCoroutine(waitForEnemy());
            checkForWinner();
        }));

        wordInSend.Clear();
    }
    public void RandomWord()
    {
        int urutan = 0;
        for (int i=0; i < defaultTile.transform.childCount; i++)
        {
            GameObject slot = defaultTile.transform.GetChild(i).gameObject;
            //Jika default tile memiliki komponen di dalamnya, maka destroy
            if (slot.transform.childCount > 0)
                Destroy(slot.transform.GetChild(0).gameObject);

            Word data = wordList[Random.Range(0, wordList.Length)];
            GameObject wordGenerate = Instantiate(prefabWord, slot.transform);
            wordGenerate.GetComponent<Image>().sprite = data.sprite;
            wordGenerate.GetComponent<MyControll>().huruf = data.huruf;
            wordGenerate.GetComponent<MyControll>().urutan = urutan;
            wordGenerate.GetComponent<MyControll>().point = data.point;
            wordGenerate.GetComponent<MyControll>().canDrag = false;
            wordGenerate.transform.GetChild(0).GetComponent<Text>().text = data.point.ToString();
            urutan++;
        }
    }
    private bool isWordInGrid()
    {
        foreach (int[] word in wordSet)
        {
            if (word != null && slots.GetChild(word[0]).GetChild(word[1]).childCount > 0)
                return true;
        }
        return false;
    }

    //Kebutuhan fungsi Control pemain
    public void RecallOrShuffle()
    {
        if (isWordInGrid())
        {
            recallBtn.SetActive(true);
            shuffleBtn.SetActive(false);
        }
        else
        {
            recallBtn.SetActive(false);
            shuffleBtn.SetActive(true);
        }
    }
    private void ChangeControl(bool param)
    {
        for (int i = 0; i < defaultTile.transform.childCount; i++)
        {
            MyControll word = defaultTile.transform.GetChild(i).GetChild(0).GetComponent<MyControll>();
            word.canDrag = param;
        }
    }
    public void Recall()
    {
        foreach(int[] word in wordSet)
        {
            if (word != null && slots.GetChild(word[0]).GetChild(word[1]).childCount > 0)
                slots.GetChild(word[0]).GetChild(word[1]).GetChild(0).GetComponent<MyControll>().setToDefault();
        }
        ResetWordSet();
        RecallOrShuffle();
    }
    public void Shuffle()
    {
        foreach(int[] word in wordSet)
        {
            if (word != null && slots.GetChild(word[0]).GetChild(word[1]).childCount > 0)
                Destroy(slots.GetChild(word[0]).GetChild(word[1]).GetChild(0).gameObject);
        }
        ResetWordSet();
        RandomWord();
    }
    // End kebutuhan fungsi control pemain

    //Kebutuhan API
    IEnumerator GetRequest(string uri, System.Action<string> callback = null)
    {
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => false;
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            //Accept all certificates
            webRequest.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
            webRequest.SetRequestHeader("token", "123123");
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            if (webRequest.isNetworkError)
                Debug.Log(pages[page] + ": Error: " + webRequest.error);
            else
                callback(webRequest.downloadHandler.text);
        }
    }
    IEnumerator PostRequest(string url, string json, System.Action<string> callback = null)
    {
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        var uwr = new UnityWebRequest(url, "POST");
        uwr.chunkedTransfer = false;
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.uploadHandler.contentType = "application/json";
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Accept", "application/json");
        uwr.SetRequestHeader("token", "123123");
        uwr.SetRequestHeader("api-version", "0.1");
        uwr.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();

        //Send the request then wait here until it returns
        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
            Debug.Log("Error While Sending: " + uwr.error);
        else
            callback(uwr.downloadHandler.text);
    }
    //END Kebutuhan API
    private Word convert(string huruf)
    {
        int indexHuruf = 0;
        switch (huruf)
        {
            case "A":
                indexHuruf = 0;
                break;
            case "B":
                indexHuruf = 1;
                break;
            case "C":
                indexHuruf = 2;
                break;
            case "D":
                indexHuruf = 3;
                break;
            case "E":
                indexHuruf = 4;
                break;
            case "F":
                indexHuruf = 5;
                break;
            case "G":
                indexHuruf = 6;
                break;
            case "H":
                indexHuruf = 7;
                break;
            case "I":
                indexHuruf = 8;
                break;
            case "J":
                indexHuruf = 9;
                break;
            case "K":
                indexHuruf = 10;
                break;
            case "L":
                indexHuruf = 11;
                break;
            case "M":
                indexHuruf = 12;
                break;
            case "N":
                indexHuruf = 13;
                break;
            case "O":
                indexHuruf = 14;
                break;
            case "P":
                indexHuruf = 15;
                break;
            case "Q":
                indexHuruf = 16;
                break;
            case "R":
                indexHuruf = 17;
                break;
            case "S":
                indexHuruf = 18;
                break;
            case "T":
                indexHuruf = 19;
                break;
            case "U":
                indexHuruf = 20;
                break;
            case "V":
                indexHuruf = 21;
                break;
            case "W":
                indexHuruf = 22;
                break;
            case "X":
                indexHuruf = 23;
                break;
            case "Y":
                indexHuruf = 24;
                break;
            case "Z":
                indexHuruf = 25;
                break;
            default:
                indexHuruf = 0;
                break;
        }
        return wordList[indexHuruf];
    }
    #region IHasChanged implementation
    public void hasChanged()
    {
        /*
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(" - ");
        foreach (Transform row in slots)
        {
            foreach(Transform slot in row)
            {
                GameObject item = slot.transform.GetComponent<Slot>().item;
                if (item)
                {
                    builder.Append(item.GetComponent<MyControll>().huruf);
                    builder.Append(" - ");
                }
            }
            
        }
        words.text = builder.ToString();
        */
        //Check apakah ada kata yang sesuai
    }

    #endregion
}

namespace UnityEngine.EventSystems
{
    public interface IHasChanged : IEventSystemHandler
    {
        void hasChanged();
    }
}
