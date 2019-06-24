using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WordsGame : MonoBehaviour, IHasChanged {
    [SerializeField] Transform slots;
    [SerializeField] Text words;
    [SerializeField] GameObject prefabWord;
    [SerializeField] AudioSource slotAudio;
    [SerializeField] private GameObject defaultTile, recallBtn, shuffleBtn;
    [SerializeField] private Text scoreText;

    //singleton
    private static WordsGame _instance;

    //tempat untuk dict words
    private HashSet<string> dicWords = new HashSet<string>();
    private TextAsset dictText;
    private HashSet<string> wordInPoint = new HashSet<string>();
    //tempat untuk semua data word dan point dari scritable object
    private Word[] wordList;
    
    
    //Kebutuhan untuk grid dan input player
    public int[][] wordSet = new int[6][];
    public MyControll[][] grid = new MyControll[15][];

    //Score pemain
    private int score = 0;
    

    public static WordsGame Instance { get { return _instance; } }

    void Awake()
    {
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
        for(int i2 = 0; i2 < wordSet.Length; i2++) wordSet[i2] = new int[2];
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

    //END KUMPULAN FUNGSI PENGECEKAN

    public void submitWords()
    {
        foreach(int[] word in wordSet)
        {
            score += polaKanan(word[0], word[1]);
            score += polaKiri(word[0], word[1]);
            score += polaAtas(word[0], word[1]);
            score += polaBawah(word[0], word[1]);
            score += polaKananBawah(word[0], word[1]);
        }
        scoreText.text = "Score : " + score.ToString();
        clearControl();
        ResetWordSet();
        RandomWord();
        RecallOrShuffle();
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
