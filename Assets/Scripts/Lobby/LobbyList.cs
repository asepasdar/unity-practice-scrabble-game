using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Net;
using SimpleJSON;
using System;
using UnityEngine.SceneManagement;

public class LobbyList : MonoBehaviour {

    [SerializeField] GameObject prefabRoom, waitLobby, listLobby, loginPanel, wait;
    
    [SerializeField] InputField username, password;

    [SerializeField] Text informasiLogin, waktuPlay;


    void Awake()
    {
        //PlayerPrefs.DeleteAll();
        if (!isAlreadyLogin())
            loginPanel.SetActive(true);
    }
    // Use this for initialization
    void Start()
    {
        refreshLobby();
    }
    private bool isAlreadyLogin()
    {
        if (PlayerPrefs.HasKey("user_id"))
            return true;

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
        InvokeRepeating("checkGuest", 0f, 5f);
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
    private void checkGuest()
    {
        Debug.Log("fungsi check guest dijalankan");
        StartCoroutine(GetRequest("https://localhost:44330/api/values/" + PlayerPrefs.GetInt("room_id"), returnValue => {
            var rvData = JSON.Parse(returnValue);
            Debug.Log(rvData["user_guest"]);
            if (rvData["user_guest"] != 1)
            {
                StartCoroutine(GetRequest("https://localhost:44330/api/user/" + rvData["user_guest"], rval =>
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
    private void switchToLobby()
    {
        listLobby.SetActive(true);
        waitLobby.SetActive(false);
        PlayerPrefs.DeleteKey("room_id");
    }
    private void clearLobby()
    {
        for(int i =0; i < listLobby.transform.GetChild(0).childCount; i++)
        {
            Destroy(listLobby.transform.GetChild(0).GetChild(i).gameObject);
        }
    }
    public void refreshLobby()
    {
        StartCoroutine(GetRequest("https://localhost:44330/api/values", returnValue => {
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

        StartCoroutine(PostRequest("https://localhost:44330/api/values/" + roomId, roomData.Print(), returnValue => {
            var rvData = JSON.Parse(returnValue);
            StartCoroutine(GetRequest("https://localhost:44330/api/user/" + rvData["user_rm"], rval => {
                var data = JSON.Parse(rval);
                PlayerPrefs.SetInt("room_id", roomId);
                switchToJoin(data);
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
        roomData.AddField("time_created", DateTime.Now.ToString());
        StartCoroutine(PostRequest("https://localhost:44330/api/values", roomData.Print(), returnValue => {
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
    public void Login()
    {
        JSONObject userData = new JSONObject(JSONObject.Type.OBJECT);
        userData.AddField("id", 0);
        userData.AddField("name", username.text);
        userData.AddField("pass", password.text);
        userData.AddField("created_at", DateTime.Now.ToString());
        StartCoroutine(PostRequest("https://localhost:44330/api/user", userData.Print(), returnValue => {
            var data = JSON.Parse(returnValue);
            if(data["name"] == null)
                informasiLogin.text = "Kombinasi Username & Password salah";
            else
            {
                PlayerPrefs.SetInt("user_id", data["id"]);
                PlayerPrefs.SetString("name", data["name"]);
                loginPanel.SetActive(false);
            }
        }));
    }
    public void DeleteLobby()
    {
        StartCoroutine(DeleteRequest("https://localhost:44330/api/values", PlayerPrefs.GetInt("room_id"), ret =>
        {
            StartCoroutine(GetRequest("https://localhost:44330/api/values/" + PlayerPrefs.GetInt("room_id"), returnValue =>
            {
                var data = JSON.Parse(returnValue);
                Debug.Log(data);
                if (data["id"] == 0)
                    switchToLobby();
            }));
        }));
    }
    IEnumerator GetRequest(string uri, Action<String> callback = null)
    {
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => false;
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            //Accept all certificates
            webRequest.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
            webRequest.SetRequestHeader("token", "123123");
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
        uwr.SetRequestHeader("api-version", "0.1");
        uwr.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();

        //Send the request then wait here until it returns
        //Send the request then wait here until it returns
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
            Debug.Log("Error While Sending: " + uwr.error);
        else
            callback(uwr.downloadHandler.text);
    }
}
class AcceptAllCertificatesSignedWithASpecificKeyPublicKey : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}