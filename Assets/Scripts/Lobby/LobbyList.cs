using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Net;
using SimpleJSON;
using System;
using UnityEngine.SceneManagement;
using Facebook.Unity;
using Facebook.MiniJSON;

public class LobbyList : MonoBehaviour {

    [SerializeField] MenuData menu;
    [SerializeField] ApiControl api;

    private JSONNode rvData; //User current room data;
    void Start()
    {
        //Start init facebook SDK
        if (!FB.IsInitialized) FB.Init(InitCallback, OnHideUnity);
        else FB.ActivateApp();
        //End Init Facebook SDK

        //Check status server dan game version
        api.DoGetRequest("/api/start/", data => {
            if(data["status"] != "ok")
            {
                menu.ErrorPanel.SetActive(true);
                menu.ErrorMessage.text = data["message"];
            }
        });

        //PlayerPrefs.DeleteAll();
        if (!isAlreadyLogin()) menu.LoginPanel.SetActive(true);
        else setProfile();

        refreshLobby();
    }

    

    //Fungsi switch dan pengecekan data user
    private void setProfile()
    {
        menu.Name.text = PlayerPrefs.GetString("name");
        if (PlayerPrefs.GetString("gender") == "male")
            menu.Profile.sprite = menu.Male;
        else if (PlayerPrefs.GetString("gender") == "female")
            menu.Profile.sprite = menu.Female;

    }
    private bool isAlreadyLogin()
    {
        if (PlayerPrefs.HasKey("user_id"))
        {
            return true;
        }

        return false;
    }
    private void showLobbyToUser(JSONNode data)
    {
        clearLobby();
        foreach (JSONNode list in data)
        {
            var myClone = Instantiate(menu.PrefabRoom, menu.ListDataLobby.transform);
            myClone.GetComponent<EnterLobby>().roomId = list["id"];
            myClone.transform.GetChild(0).GetComponent<Text>().text = "Room - 0" + list["id"];
            if (list["user_guest"] == 1)
                myClone.transform.GetChild(1).GetComponent<Text>().text = "1/2";
            else
            {
                myClone.transform.GetChild(1).GetComponent<Text>().text = "2/2";
                myClone.GetComponent<Button>().interactable = false;
            }
        }
    }
    private void switchToWait()
    {
        menu.ListLobby.SetActive(false);
        menu.WaitLobby.SetActive(true);
        menu.RoomMaster.text = PlayerPrefs.GetString("name");
        menu.RoomPanelGuest.SetActive(false);
        InvokeRepeating("checkGuest", 0f, 1f);
    }
    private void switchToJoin(JSONNode data)
    {
        menu.ListLobby.SetActive(false);
        menu.WaitLobby.SetActive(true);
        menu.RoomMaster.text = data["name"];
        menu.RoomPanelGuest.SetActive(true);
        menu.RoomGuest.text = PlayerPrefs.GetString("name");
        readyToGo();
    }
    private void switchToLobby()
    {
        menu.ListLobby.SetActive(true);
        menu.WaitLobby.SetActive(false);
        PlayerPrefs.DeleteKey("room_id");
    }
    //End fungsi


    //Fungsi sebelum masuk kedalam lobby
    private void clearLobby()
    {
        for(int i =0; i < menu.ListDataLobby.transform.childCount; i++)
        {
            Destroy(menu.ListDataLobby.transform.GetChild(i).gameObject);
        }
    }
    public void refreshLobby()
    {
        api.DoGetRequest("/api/values/", data => {
            showLobbyToUser(data);
        });
    }
    public void CreateLobby()
    {
        menu.WaitPanel.SetActive(true);
        DateTime time = DateTime.Now;
        if (time.Second == 0) Invoke("postToAPI", 5 - time.Second);
        else if (time.Second < 5) Invoke("postToAPI", 5 - time.Second);
        else
        {
            var helper = ((Mathf.Ceil(time.Second / 5)) * 5) - time.Second;
            if (helper < 0)
                Invoke("postToAPI", helper + 5);
            else
                Invoke("postToAPI", helper);
        }
    }
    public void JoinLobby(int roomId)
    {
        JSONObject roomData = new JSONObject(JSONObject.Type.OBJECT);
        roomData.AddField("id", roomId);
        roomData.AddField("user_rm", 0);
        roomData.AddField("user_guest", PlayerPrefs.GetInt("user_id"));
        roomData.AddField("status", 1);
        roomData.AddField("time_created", DateTime.Now.ToString());

        api.DoPostRequest("/api/values/" + roomId, roomData, rData => {
            api.DoGetRequest("/api/user/" + rData["user_rm"], data => {
                api.DoGetRequest("/api/values/" + roomId, rGet => {
                    rvData = rGet;
                    PlayerPrefs.SetInt("room_id", roomId);
                    switchToJoin(data);
                });
            });
        });
    }
    private void postToAPI()
    {
        JSONObject roomData = new JSONObject(JSONObject.Type.OBJECT);
        roomData.AddField("id", 0);
        roomData.AddField("user_rm", PlayerPrefs.GetInt("user_id"));
        roomData.AddField("user_guest", 1);
        roomData.AddField("status", 1);
        api.DoPostRequest("/api/values/", roomData, data =>
        {
            if (data["id"] != 0)
            {
                menu.WaitPanel.SetActive(false);
                PlayerPrefs.SetInt("room_id", data["id"]);
                switchToWait();
            }
        });
    }
    //End fungsi


    //Fungsi setelah didalam lobby
    private void checkGuest()
    {
        api.DoGetRequest("/api/values/" + PlayerPrefs.GetInt("room_id"), rvData => {
            if (rvData["user_guest"] != 1 && rvData["user_guest"] != 0)
            {
                api.DoGetRequest("/api/user/" + rvData["user_guest"], data =>
                {
                    menu.RoomPanelGuest.SetActive(true);
                    menu.RoomGuest.text = data["name"];
                    readyToGo();
                });
            }
        });
    }
    public void DeleteLobby()
    {
        api.DoDeleteRequets("/api/values/", PlayerPrefs.GetInt("room_id"), data => {
            api.DoGetRequest("/api/values/" + PlayerPrefs.GetInt("room_id"), returnValue => {
                CancelInvoke();
                if (data["id"] == 0) switchToLobby();
            });
        });
    }
    private void readyToGo()
    {
        CancelInvoke();
        DateTime time = DateTime.Now;
        if (time.Second == 0) goPlay();
        else if (time.Second < 5) Invoke("goPlay", 5 - time.Second);
        else
        {
            var helper = ((Mathf.Ceil(time.Second / 5)) * 5) - time.Second;
            if (helper < 0)
                Invoke("goPlay", helper + 5);
            else
                Invoke("goPlay", helper);
        }
    }
    private void goPlay()
    {
        SceneManager.LoadScene(1);
    }
    //End fungsi setelah didalam lobby

    private void CallbackLogin(JSONNode data)
    {
        if (data["id"] != 0)
        {
            PlayerPrefs.SetInt("user_id", data["id"]);
            PlayerPrefs.SetString("name", data["name"]);
            PlayerPrefs.SetString("token", data["token"]);
            PlayerPrefs.SetString("fb_id", data["fb_id"]);
            PlayerPrefs.SetString("gender", "male");
            menu.Name.text = data["name"];
            menu.LoginPanel.SetActive(false);
            if(data["fb_id"] != "guest") menu.Profile.sprite = menu.Male;
        }
    }

    //Fungsi untuk geust
    public void tryGuestLogin()
    {
        JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
        j.AddField("fb_id", "guest");
        j.AddField("device_id", SystemInfo.deviceUniqueIdentifier);
        j.AddField("name", "Guest" + UnityEngine.Random.Range(0, 1000000).ToString());
        api.DoPostRequest("/api/facebook/0/", j, CallbackLogin);
    }
    //End fungsi

    //Facbook SDK
    public void tryFacebookLogin()
    {
        if(FB.IsLoggedIn)
            FB.LogOut();

        var perms = new List<string>() { "public_profile", "email"};
        FB.LogInWithReadPermissions(perms, AuthCallback);
    }
    private void AuthCallback(ILoginResult result)
    {
        Debug.Log(result);
        if (FB.IsLoggedIn)
        {
            Debug.Log("Saya berhaisl login");
            var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
            FB.API("/me?fields=name", HttpMethod.GET, r => {
                IDictionary dict = Json.Deserialize(r.RawResult) as IDictionary;

                JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
                j.AddField("fb_id", aToken.UserId);
                j.AddField("device_id", SystemInfo.deviceUniqueIdentifier);
                j.AddField("name", dict["name"].ToString());
                api.DoPostRequest("/api/facebook/", j, CallbackLogin);
            });

        }
        else
            Debug.Log("cancel");
    }
    private void InitCallback()
    {
        if (FB.IsInitialized)
            FB.ActivateApp();
        else
            Debug.Log("gagal init");
    }
    private void OnHideUnity(bool isGameShown)
    {
        if (!isGameShown)
            Time.timeScale = 0;
        else
            Time.timeScale = 1;
    }
    //End Facebook SDK
}
class AcceptAllCertificatesSignedWithASpecificKeyPublicKey : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}