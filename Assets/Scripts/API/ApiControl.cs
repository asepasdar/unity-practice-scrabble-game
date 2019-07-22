using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

public class ApiControl : MonoBehaviour {
    private const string ipadd = "172.16.8.52:45456";
    public void DoDeleteRequets(string method, int id, System.Action<JSONNode> callback)
    {
        StartCoroutine(DeleteRequest("https://" + ipadd + method, id, returnValue => {
            callback(JSON.Parse(returnValue));
        }));
    }
    public void DoGetRequest(string method, System.Action<JSONNode> callback)
    {
        StartCoroutine(GetRequest("https://" + ipadd + method, returnValue => {
            callback(JSON.Parse(returnValue));
        }));
    }
    public void DoPostRequest(string method, JSONObject param, System.Action<JSONNode> callback)
    {
        StartCoroutine(PostRequest("https://" + ipadd + method, param.Print(), returnValue => {
            callback(JSON.Parse(returnValue));
        }));
    }
    //============================== END POST REQUEST ======================================/

    IEnumerator GetRequest(string uri, System.Action<string> callback = null)
    {
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => false;
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            //Accept all certificates
            webRequest.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
            webRequest.SetRequestHeader("token", "123123");
            webRequest.SetRequestHeader("game_version", Application.version);
            webRequest.SetRequestHeader("api-version", "0.1");
            webRequest.SetRequestHeader("device_id", SystemInfo.deviceUniqueIdentifier);
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
    IEnumerator PostRequest(string url, string json, System.Action<string> callback = null)
    {
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        var uwr = new UnityWebRequest(url, "POST")
        {
            chunkedTransfer = false,
            uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend)
        };
        uwr.uploadHandler.contentType = "application/json";
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Accept", "application/json");
        uwr.SetRequestHeader("token", "123123");
        uwr.SetRequestHeader("api-version", "0.1");
        uwr.SetRequestHeader("game_version", Application.version);
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
    IEnumerator DeleteRequest(string uri, int id, System.Action<string> callback = null)
    {
        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => false;
        using (UnityWebRequest webRequest = UnityWebRequest.Delete(uri + "/" + id))
        {
            //Accept all certificates
            webRequest.certificateHandler = new AcceptAllCertificatesSignedWithASpecificKeyPublicKey();
            webRequest.SetRequestHeader("token", "123123");
            webRequest.SetRequestHeader("api-version", "0.1");
            webRequest.SetRequestHeader("game_version", Application.version);
            webRequest.SetRequestHeader("device_id", SystemInfo.deviceUniqueIdentifier);
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
            string args = "";
            callback(args);
        }
    }
}
