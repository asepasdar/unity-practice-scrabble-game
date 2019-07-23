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
    /*
    [SerializeField] Transform slots;
    [SerializeField] GameObject prefabWord, cover, scoreBoard, endGame;
    [SerializeField] private GameObject defaultTile, recallBtn, shuffleBtn;
    [SerializeField] AudioSource slotAudio;
    [SerializeField] private Text scoreText, timer, whosturn;
    [SerializeField] int jumlahRound = 2;
    */
    [SerializeField] public GameData Gdata;
    [SerializeField] private ApiControl MyApi;
    [SerializeField] private WordGameDict Dict;

    //singleton
    private static WordsGame _instance;
    private int turn = 1, enemyTurn = 1;

    //tempat untuk semua data word dan point dari scritable object
    private Word[] wordList;

    //Kebutuhan untuk grid dan input player
    public int[][] wordSet = new int[6][];
    public MyControll[][] grid = new MyControll[15][];

    //Score pemain
    private int myScore = 0, enemyScore;
    public JSONNode roomData, user_rmData, user_guestData, enemyData;

    //variable untuk cek apakah point enemy sudah di set
    private int checkTime = 0; // sudah berapa detik / kali di check batas 5
    public static WordsGame Instance { get { return _instance; } }
    void Awake()
    {
        Gdata.Cover.SetActive(true);
        Gdata.ScoreText.text = PlayerPrefs.GetInt("room_id").ToString() + " " + System.DateTime.Now;
        MyApi.DoGetRequest("/api/values/" + PlayerPrefs.GetInt("room_id"), returnValue => {
            roomData = returnValue;

            //Get detail user rm
            MyApi.DoGetRequest("/api/user/" + roomData["user_rm"], rval => {
                user_rmData = rval;
            });

            //Get detail user guest
            MyApi.DoGetRequest("/api/user/" + roomData["user_guest"], rguest => {
                user_guestData = rguest;
                
            });

            JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
            j.AddField("id", roomData["id"].ToString());
            j.AddField("user_rm", roomData["user_rm"].ToString());
            j.AddField("user_guest", roomData["user_guest"].ToString());
            j.AddField("status", 2);
            MyApi.DoPostRequest("/api/start/", j, myCallback => {});

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

            MyApi.DoPostRequest("/api/start/0", k, rReady => {
                InvokeRepeating("startPlay", 0f, 1f);
            });
        });

        

        
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
            MyApi.DoGetRequest("/api/values/" + PlayerPrefs.GetInt("room_id"), returnValue =>
            {
                roomData = returnValue;
                checkPlay();
            });
        }
    }
    public bool checkPlay()
    {
        if (roomData["ready_p1"] == 1 && roomData["ready_p2"] == 1)
        {
            Gdata.Cover.SetActive(false);
            if (PlayerPrefs.GetInt("user_id") == user_guestData["id"])
            {
                Gdata.MyName.text = user_guestData["name"];
                Gdata.EnemyName.text = user_rmData["name"];
                enemyData = user_rmData;
                ChangeControl(false);
                StartCoroutine(waitForEnemy());
            }
            else
            {
                Gdata.MyName.text = user_guestData["name"];
                Gdata.EnemyName.text = user_rmData["name"];
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
        Gdata.ScoreBoard.transform.GetChild(enemyOrPlayer).GetChild(abcd - 1).GetChild(0).GetComponent<Text>().text = score.ToString();
    }
    void setEnemyWord (JSONNode data)
    {
        foreach(JSONNode list in data)
        {
            Word apiData = convert(list["data"]);
            var lokasi = Gdata.Slots[list["row"]].GetChild(list["col"]);
            var row = (int)list["row"];
            var col = (int)list["col"];
            

            GameObject wordGenerate = Instantiate(Gdata.PrefabWord, lokasi);
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
            MyApi.DoPostRequest("/api/game", j, data => {
                setScoreBoard(1, enemyTurn, data["point"]);
                setEnemyWord(data["list"]);
                enemyTurn++;
                enemyScore += data["point"];
                StartCoroutine(autoSubmit());
                checkForWinner();
            });
            CancelInvoke();
        }
        else
        {
            Debug.Log("check" + checkTime);
            MyApi.DoPostRequest("/api/game/" + enemyTurn, j, data => {
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
            });
        }
        
    }
    IEnumerator startCountDown(int angka)
    {
        float normalizedTime = angka;
        while (normalizedTime >= 0)
        {
            normalizedTime -= Time.deltaTime;
            var fix = (int)normalizedTime;
            Gdata.Timer.text = fix.ToString();
            yield return null;
        }
    }
    IEnumerator autoSubmit()
    {
        Gdata.WhosTurn.text = "<color=white>Your turn</color>";
        ChangeControl(true);
        StartCoroutine(startCountDown(14));
        yield return new WaitForSeconds(14);
        submitWords();
        
    }
    IEnumerator waitForEnemy()
    {
        Gdata.WhosTurn.text = "<color=red>Enemy's turn</color>";
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
        if (turn == Gdata.JumlahRound && turn == enemyTurn)
        {
            if (myScore == enemyScore)
                Gdata.TextResult.text = "DRAW";
            else
            {
                string winOrLose = myScore > enemyScore ? "WIN" : "LOSE";
                Gdata.TextResult.text = "You - " + winOrLose;
            }
            Gdata.EndGame.SetActive(true);
            Invoke("Quit", 5f);
        }
    }
    private void checkEnemyPoint()
    {
        JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
        
        j.AddField("turn", enemyTurn);
        j.AddField("user_id", enemyData["id"].ToString());
        j.AddField("room_id", PlayerPrefs.GetInt("room_id"));
        MyApi.DoPostRequest("/api/game/" + enemyTurn, j, data => {
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
            
        });
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
        if (Dict.CheckWord(hasil, 1))
        {
            if (Dict.GetWordInPoint().Contains(hasil))
                return 0;
            else
            {
                Dict.GetWordInPoint().Add(hasil);
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
        Gdata.SlotAudio.clip = Gdata.ClipDragWord;
        Gdata.SlotAudio.Play();
    }
    public void playPointAudio()
    {
        Gdata.SlotAudio.clip = Gdata.ClipGetPoint;
        Gdata.SlotAudio.Play();
    }
    void Quit()
    {
        JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
        j.AddField("id", roomData["id"].ToString());
        j.AddField("user_rm", roomData["user_rm"].ToString());
        j.AddField("user_guest", roomData["user_guest"].ToString());
        j.AddField("status", 3);
        MyApi.DoPostRequest("/api/start/", j, myCallback => {
        });
        
        PlayerPrefs.DeleteKey("room_id");
        SceneManager.LoadScene(0);
    }

    //=========================== INI KUMPULAN FUNGSI PENGECEKAN WORD SESUAI POLA MASING2 =====================

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

    //=========================== END FUNGSI PENGECEKAN ==========================================================


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
                if (!Dict.GetWordInSend().Contains(word[0].ToString() + word[1].ToString()) && grid[word[0]][word[1]] != null)
                {
                    Dict.GetWordInSend().Add(word[0].ToString() + word[1].ToString());
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
        if (point > 0) playPointAudio();
        //Kebutuhan untuk di local game
        Gdata.ScoreText.text = "Score : " + myScore.ToString();
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


        MyApi.DoPostRequest("/api/game/", j, data => {
            setScoreBoard(0, turn, data["point"]);
            myScore += data["point"];
            turn++;
            StartCoroutine(waitForEnemy());
            checkForWinner();
        });

        Dict.GetWordInSend().Clear();
    }
    public void RandomWord()
    {
        int urutan = 0;
        for (int i=0; i < Gdata.DefaultTile.transform.childCount; i++)
        {
            GameObject slot = Gdata.DefaultTile.transform.GetChild(i).gameObject;
            //Jika default tile memiliki komponen di dalamnya, maka destroy
            if (slot.transform.childCount > 0)
                Destroy(slot.transform.GetChild(0).gameObject);

            Word data = wordList[Random.Range(0, wordList.Length)];
            GameObject wordGenerate = Instantiate(Gdata.PrefabWord, slot.transform);
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
            if (word != null && Gdata.Slots[word[0]].GetChild(word[1]).childCount > 0)
                return true;
        }
        return false;
    }

    //Kebutuhan fungsi Control pemain
    public void RecallOrShuffle()
    {
        if (isWordInGrid())
        {
            Gdata.RecallBtn.SetActive(true);
            Gdata.ShuffleBtn.SetActive(false);
        }
        else
        {
            Gdata.RecallBtn.SetActive(false);
            Gdata.ShuffleBtn.SetActive(true);
        }
    }
    private void ChangeControl(bool param)
    {
        for (int i = 0; i < Gdata.DefaultTile.transform.childCount; i++)
        {
            MyControll word = Gdata.DefaultTile.transform.GetChild(i).GetChild(0).GetComponent<MyControll>();
            word.canDrag = param;
        }
    }
    public void Recall()
    {
        foreach(int[] word in wordSet)
        {
            if (word != null && Gdata.Slots[word[0]].GetChild(word[1]).childCount > 0)
                Gdata.Slots[word[0]].GetChild(word[1]).GetChild(0).GetComponent<MyControll>().setToDefault();
        }
        ResetWordSet();
        RecallOrShuffle();
    }
    public void Shuffle()
    {
        foreach(int[] word in wordSet)
        {
            if (word != null && Gdata.Slots[word[0]].GetChild(word[1]).childCount > 0)
                Destroy(Gdata.Slots[word[0]].GetChild(word[1]).GetChild(0).gameObject);
        }
        ResetWordSet();
        RandomWord();
    }
    // End kebutuhan fungsi control pemain

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
