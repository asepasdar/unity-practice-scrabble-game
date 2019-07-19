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

    [SerializeField] GameObject prefabRoom, waitLobby, listLobby, loginPanel, wait, error;
    [SerializeField] Image profile;
    [SerializeField] Text nama;
    [SerializeField] Sprite male, female;

    private const string ipadd = "172.16.8.162:45456";
    JSONNode rvData;
    // Use this for initialization
    void Start()
    {
        //Start init facebook SDK
        if (!FB.IsInitialized)
            FB.Init(InitCallback, OnHideUnity);
        else
            FB.ActivateApp();
        //End Init Facebook SDK

        //Check status server dan game version
        StartCoroutine(GetRequest("https://" + ipadd + "/api/start", returnValue => {
            var data = JSON.Parse(returnValue);
            if (data["status"] != "ok")
                error.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = data["message"];
            else
                error.SetActive(false);
        }));
        PlayerPrefs.DeleteAll();
        if (!isAlreadyLogin())
            loginPanel.SetActive(true);
        else
            setProfile();

        refreshLobby();
    }

    //Fungsi untuk geust
    public void tryGuestLogin()
    {
        JSONObject j = new JSONObject(JSONObject.Type.OBJECT);
        j.AddField("fb_id", "guest");
        j.AddField("device_id", SystemInfo.deviceUniqueIdentifier);
        j.AddField("name", "Guest"+ UnityEngine.Random.Range(0, 1000000).ToString());
        

        StartCoroutine(PostRequest("https://" + ipadd + "/api/facebook/0", j.Print(), returnValue => {
            JSONNode data = JSON.Parse(returnValue);
            if (data["id"] != 0)
            {
                PlayerPrefs.SetInt("user_id", data["id"]);
                PlayerPrefs.SetString("name", data["name"]);
                PlayerPrefs.SetString("token", data["token"]);
                PlayerPrefs.SetString("fb_id", data["fb_id"]);
                PlayerPrefs.SetString("gender", "");
                loginPanel.SetActive(false);
                nama.text = data["name"];
            }
        }));
    }
    //End fungsi

    //Fungsi switch dan pengecekan data user
    private void setProfile()
    {
        nama.text = PlayerPrefs.GetString("name");
        if (PlayerPrefs.GetString("gender") == "male")
            profile.sprite = male;
        else if (PlayerPrefs.GetString("gender") == "female")
            profile.sprite = female;

    }
    private bool isAlreadyLogin()
    {
        if (PlayerPrefs.HasKey("user_id"))
        {
            return true;
        }

        return false;
    }
    private void showLobbyToUser(string jsonString)
    {
        clearLobby();
        var data = JSON.Parse(jsonString);
        foreach (JSONNode list in data)
        {
            var myClone = Instantiate(prefabRoom, listLobby.transform.GetChild(0).transform);
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
        //Debug.Log(result.Count);
    }
    private void switchToWait()
    {
        listLobby.SetActive(false);
        waitLobby.SetActive(true);
        GameObject userMaster = waitLobby.transform.GetChild(0).GetChild(0).gameObject;
        GameObject userGuest = waitLobby.transform.GetChild(0).GetChild(1).gameObject;
        userMaster.transform.GetChild(0).GetComponent<Text>().text = PlayerPrefs.GetString("name");
        userGuest.SetActive(false);
        InvokeRepeating("checkGuest", 0f, 1f);
    }
    private void switchToJoin(JSONNode data)
    {
        listLobby.SetActive(false);
        waitLobby.SetActive(true);
        GameObject userMaster = waitLobby.transform.GetChild(0).GetChild(0).gameObject;
        GameObject userGuest = waitLobby.transform.GetChild(0).GetChild(1).gameObject;
        userMaster.transform.GetChild(0).GetComponent<Text>().text = data["name"];
        userGuest.SetActive(true);
        userGuest.transform.GetChild(0).GetComponent<Text>().text = PlayerPrefs.GetString("name");
        readyToGo();
    }
    private void switchToLobby()
    {
        listLobby.SetActive(true);
        waitLobby.SetActive(false);
        PlayerPrefs.DeleteKey("room_id");
    }
    //End fungsi


    //Fungsi sebelum masuk kedalam lobby
    private void clearLobby()
    {
        for(int i =0; i < listLobby.transform.GetChild(0).childCount; i++)
        {
            Destroy(listLobby.transform.GetChild(0).GetChild(i).gameObject);
        }
    }
    public void refreshLobby()
    {
        StartCoroutine(GetRequest("https://" + ipadd + "/api/values", returnValue => {
            showLobbyToUser(returnValue);
        }));
    }
    public void CreateLobby()
    {
        wait.SetActive(true);
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

        StartCoroutine(PostRequest("https://" + ipadd + "/api/values/" + roomId, roomData.Print(), returnValue => {
            var rData = JSON.Parse(returnValue);
            PlayerPrefs.SetInt("room_id", roomId);
            StartCoroutine(GetRequest("https://" + ipadd + "/api/user/" + rData["user_rm"], rval => {
                var data = JSON.Parse(rval);
                StartCoroutine(GetRequest("https://" + ipadd + "/api/values/" + PlayerPrefs.GetInt("room_id"), rGet => {
                    rvData = JSON.Parse(rGet);
                    switchToJoin(data);
                    Debug.Log(PlayerPrefs.GetInt("room_id"));
                }));
            }));
        }));
    }
    private void postToAPI()
    {
        JSONObject roomData = new JSONObject(JSONObject.Type.OBJECT);
        roomData.AddField("id", 0);
        roomData.AddField("user_rm", PlayerPrefs.GetInt("user_id"));
        roomData.AddField("user_guest", 1);
        roomData.AddField("status", 1);
        StartCoroutine(PostRequest("https://" + ipadd + "/api/values", roomData.Print(), returnValue => {
            wait.SetActive(false);
            var data = JSON.Parse(returnValue);
            if (data["id"] == 0)
                Debug.Log("Gagal membuat room");
            else
            {
                PlayerPrefs.SetInt("room_id", data["id"]);
                switchToWait();
            }
        }));
    }
    //End fungsi


    //Fungsi setelah didalam lobby
    private void checkGuest()
    {
        StartCoroutine(GetRequest("https://" + ipadd + "/api/values/" + PlayerPrefs.GetInt("room_id"), returnValue => {
            rvData = JSON.Parse(returnValue);
            
            if (rvData["user_guest"] != 1 && rvData["user_guest"] != 0)
            {
                StartCoroutine(GetRequest("https://" + ipadd + "/api/user/" + rvData["user_guest"], rval =>
                {
                    var data = JSON.Parse(rval);
                    GameObject userGuest = waitLobby.transform.GetChild(0).GetChild(1).gameObject;
                    userGuest.SetActive(true);
                    userGuest.transform.GetChild(0).GetComponent<Text>().text = data["name"];
                    readyToGo();
                }));
            }
        }));
    }
    public void DeleteLobby()
    {
        StartCoroutine(DeleteRequest("https://" + ipadd + "/api/values", PlayerPrefs.GetInt("room_id"), ret =>
        {
            StartCoroutine(GetRequest("https://" + ipadd + "/api/values/" + PlayerPrefs.GetInt("room_id"), returnValue =>
            {
                var data = JSON.Parse(returnValue);
                CancelInvoke();
                if (data["id"] == 0)
                    switchToLobby();
            }));
        }));
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

                StartCoroutine(PostRequest("https://" + ipadd + "/api/facebook", j.Print(), returnValue => {
                    JSONNode data = JSON.Parse(returnValue);
                    Debug.Log(data);
                    if (data["id"] != 0)
                    {
                        Debug.Log("Return berhasil");
                        PlayerPrefs.SetInt("user_id", data["id"]);
                        PlayerPrefs.SetString("name", data["name"]);
                        PlayerPrefs.SetString("token", data["token"]);
                        PlayerPrefs.SetString("fb_id", data["fb_id"]);
                        PlayerPrefs.SetString("gender", "male");
                        nama.text = dict["name"].ToString();
                        loginPanel.SetActive(false);
                        profile.sprite = male;
                    }
                    else
                    {
                        Debug.Log("Return gagal brooooo");
                    }
                }));
            });

        }
        else
        {
            Debug.Log("cancel");
        }
    }
    private void InitCallback()
    {
        if (FB.IsInitialized)
            FB.ActivateApp();
        else
        {
            Debug.Log("gagal init");
        }
            
    }
    private void OnHideUnity(bool isGameShown)
    {
        if (!isGameShown)
            Time.timeScale = 0;
        else
            Time.timeScale = 1;
    }
    //End Facebook SDK

    //fungsi request to API
    IEnumerator GetRequest(string uri, Action<String> callback = null)
    {
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => false;
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            //Accept all certificates
            webRequest.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
            webRequest.SetRequestHeader("token", "123123");
            webRequest.SetRequestHeader("game_version", Application.version);
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            if (webRequest.isNetworkError)
                Debug.Log(pages[page] + ": Error: " + webRequest.error);
            else
                callback(webRequest.downloadHandler.text);
        }
    }
    IEnumerator DeleteRequest(string uri, int id, Action<string> callback = null)
    {
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => false;
        using (UnityWebRequest webRequest = UnityWebRequest.Delete(uri+"/"+id))
        {
            //Accept all certificates
            webRequest.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
            webRequest.SetRequestHeader("token", "123123");
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
            string args = "";
            callback(args);
        }
    }
    IEnumerator PostRequest(string url, string json, Action<string> callback = null)
    {
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        var uwr = new UnityWebRequest(url, "POST");
        uwr.chunkedTransfer = false;
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.uploadHandler.contentType = "application/json";
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Accept", "application/json");
        uwr.SetRequestHeader("token", "123123");
        uwr.SetRequestHeader("game_version", "1.0");
        uwr.SetRequestHeader("device_id", SystemInfo.deviceUniqueIdentifier);
        uwr.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();

        //Send the request then wait here until it returns
        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
            Debug.Log("Error While Sending: " + uwr.error);
        else
            callback(uwr.downloadHandler.text);
    }
    //End Fungsi request to API
}
class AcceptAllCertificatesSignedWithASpecificKeyPublicKey : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}