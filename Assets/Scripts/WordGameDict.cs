using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class WordGameDict : MonoBehaviour{
    // In C# using a HashSet is an O(1) operation. It's a dictionary without the keys!
    private InputField field;
    [SerializeField] Text searchResult;

    void Awake()
    {
        field = GetComponent<InputField>();
    }


    public void checkInput()
    {
        if (WordsGame.Instance.CheckWord(field.text, 0)) searchResult.text = "<color=#2AFF21>" + field.text.ToUpper() + "</color> is a valid word";
        else searchResult.text = "<color=red>" + field.text.ToUpper() + "</color> is not a valid word";

        field.text = "";
    }
}
