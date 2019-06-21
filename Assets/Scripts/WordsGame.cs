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

    //singleton
    private static WordsGame _instance;

    //tempat untuk dict words
    private HashSet<string> dicWords = new HashSet<string>();
    private TextAsset dictText;

    //tempat untuk semua data word dan point dari scritable object
    private Word[] wordList;
    
    
    //Kebutuhan untuk grid dan input player
    public int[][] wordSet = new int[6][];
    public MyControll[][] grid = new MyControll[15][];
    

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
    public void playSlotAudio()
    {
        slotAudio.Play();
    }
    public void polaKanan(int row, int col)
    {
        string hasil = "";
        for (int i = col; i < 15; i++)
        {
            if (grid[row][i] != null)
                Debug.Log("Baris ke "+row + " kolom ke " + i);
            else
                break;
        }
        
    }

    public void submitWords()
    {
        Debug.Log(grid[0][1]);
        foreach(int[] word in wordSet)
        {
            polaKanan(word[0], word[1]);
        }
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

    public void checkWordsPoint()
    {

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

    #region IHasChanged implementation
    public void hasChanged()
    {
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
