using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Pindah : MonoBehaviour {

	// Use this for initialization
	public void pindah(int index)
    {
        SceneManager.LoadScene(index);
    }
}
