using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Scrabble.Reference
{
    public class MenuData : MonoBehaviour
    {
        [Header("All Panel Object")]
        public GameObject WaitLobby;
        public GameObject LoginPanel;
        public GameObject WaitPanel;


        [Header("In Lobby Object")]
        public Text RoomMaster;
        public Text RoomGuest;
        public GameObject RoomPanelGuest;

        [Header("Data Lobby")]
        public GameObject ListLobby;
        public GameObject ListDataLobby;

        [Header("Error Panel")]
        public GameObject ErrorPanel;
        public Text ErrorMessage;

        [Header("Menu UI")]
        public Image Profile;
        public Text Name;

        [Header("Sprite Data")]
        public Sprite Male;
        public Sprite Female;

        [Header("Prefab Data")]
        public GameObject PrefabRoom;
    }
}
