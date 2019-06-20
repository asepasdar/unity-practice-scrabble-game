using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WordsGame : MonoBehaviour, IHasChanged {
    [SerializeField] Transform slots;
    [SerializeField] Text words;
    [SerializeField] GameObject prefabWord;

    private static WordsGame _instance;
    private Word[] wordList;
    private GameObject[] defaultTile;


    public string[][] grid = new string[15][];
    

    public static WordsGame Instance { get { return _instance; } }

    void Awake()
    {
        wordList = Resources.LoadAll<Word>("Words"); //Data word scriptable object
        defaultTile = GameObject.FindGameObjectsWithTag("defaultPos"); //slot default bawah
        RandomWord(); //acak kata yang akan di tampilkan

        //instance class WordsGame
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else _instance = this;

        //init array untuk grid
        for (int i = 0; i < 15; i++)
        {
            grid[i] = new string[15];
        }
    }


    public void RandomWord()
    {
        foreach(GameObject slot in defaultTile)
        {
            if(slot.transform.childCount > 0)
            {
                Destroy(slot.transform.GetChild(0).gameObject);
            }
            Word data = wordList[Random.Range(0, wordList.Length)];
            GameObject wordGenerate = Instantiate(prefabWord, slot.transform);
            wordGenerate.GetComponent<Image>().sprite = data.sprite;
            wordGenerate.GetComponent<MyControll>().huruf = data.huruf;
            wordGenerate.transform.GetChild(0).GetComponent<Text>().text = data.point.ToString();
        }
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
