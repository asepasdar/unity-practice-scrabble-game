using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Scrabble.Reference
{
    public class GameData : MonoBehaviour
    {
        [Header("Grid Settings")]
        public Transform[] Slots;

        [Header("Word Settings")]
        public GameObject PrefabWord;

        [Header("Player Tools")]
        public GameObject DefaultTile;
        public GameObject RecallBtn;
        public GameObject ShuffleBtn;

        [Header("Control Panel Settings")]
        public GameObject Cover;
        public GameObject ScoreBoard;

        [Header("Game Result Settings")]
        public Text TextResult;
        public GameObject EndGame;

        [Header("Game Control Settings")]
        public Text ScoreText;
        public Text Timer;
        public Text WhosTurn;
        public Text MyName;
        public Text EnemyName;
        public int JumlahRound = 2;

        [Header("Audio Settings")]
        public AudioSource SlotAudio;
        public AudioClip ClipGetPoint;
        public AudioClip ClipDragWord;

        [Header("Search Words Settings")]
        public InputField TextField;
        public Text SearchResult;
    }
}
