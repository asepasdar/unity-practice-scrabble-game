using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Net;
using SimpleJSON;
using System;

namespace Scrabble.Logic
{
    public class EnterLobby : MonoBehaviour
    {
        public int roomId;
        // Use this for initialization
        public void JoinLobby()
        {
            transform.parent.parent.parent.GetComponent<LobbyList>().JoinLobby(roomId);
        }
    }
}

