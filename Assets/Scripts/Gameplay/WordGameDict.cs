using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class WordGameDict : MonoBehaviour{
    // In C# using a HashSet is an O(1) operation. It's a dictionary without the keys!
    
    private HashSet<string> DicWords = new HashSet<string>();
    private HashSet<string> WordInPoint = new HashSet<string>();
    private HashSet<string> WordInSend = new HashSet<string>();
    private TextAsset DictText;
    void Awake()
    {
        InitializeDictionary("ospd"); //load dict word
    }


    public void checkInput()
    {
        if (CheckWord(WordsGame.Instance.Gdata.TextField.text, 0)) WordsGame.Instance.Gdata.SearchResult.text = "<color=#2AFF21>" + WordsGame.Instance.Gdata.TextField.text.ToUpper() + "</color> is a valid word";
        else WordsGame.Instance.Gdata.SearchResult.text = "<color=red>" + WordsGame.Instance.Gdata.TextField.text.ToUpper() + "</color> is not a valid word";

        WordsGame.Instance.Gdata.TextField.text = "";
    }
    protected void InitializeDictionary(string filename)
    {
        DictText = (TextAsset)Resources.Load(filename, typeof(TextAsset));
        var text = DictText.text;

        foreach (string s in text.Split('\n'))
        {
            DicWords.Add(s);
        }
    }
    public bool CheckWord(string word, int minLength)
    {
        if (word.Length < minLength)
        {
            return false;
        }

        return (DicWords.Contains(word.ToUpper()));
    }
    public HashSet<string> GetWordInPoint()
    {
        return WordInPoint;
    }
    public HashSet<string> GetWordInSend()
    {
        return WordInSend;
    }
}
