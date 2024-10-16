using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.Networking;
using Mirror;

public class LodingServerScript : MonoBehaviour
{
    private string serverURL = "http://localhost:XXXX"; // Replace with your server's URL if different.
    public Loading_Server loadingserver;
    [System.Serializable]
    public class SteamIDData
    {
        public string steamID;
    }
    [System.Serializable]
    public class UserIDData
    {
        public string userID;
    }
    [System.Serializable]
    public class ServerResponse
    {
        public bool exists;
        public string message;
        public string userID;
        public string Nickname;
        public int Level;
        public int ExperiencePoints;
        public string MainCharacterID;
        public int cashpoint;
        public int basepoint;
        public bool isban;
    }
    [System.Serializable]
    public class CharacterData
    {
        public int CharacterType;
        public string TopOutfitID;
        public string BottomOutfitID;
        public string ShoesOutfitID;
        public string AllInOneOutfitID;
        public string Accessory1ID;
        public string Accessory2ID;
        public string Accessory3ID;
        public string Accessory4ID;
        public string Accessory5ID;
        public string Accessory6ID;
        public string Accessory7ID;
        public string Accessory8ID;
    }
    [System.Serializable]
    public class OutfitData
    {
        public int OutfitID;
        public string Description;
        public string Type;
        public int Character_costume;
    }
    [System.Serializable]
    public class AccessoriesData
    {
        public string Description;
        public string Type;
        public int Character_costume;
    }

    [System.Serializable]
    public class CharactersResponse
    {
        public List<CharacterData> characters;
    }
    [System.Serializable]
    public class OutfitsResponse
    {
        public List<OutfitData> outfits;
    }
    [System.Serializable]
    public class AccessoriesResponse
    {
        public List<AccessoriesData> accessories;
    }

    [System.Serializable]
    public class NicknameData
    {
        public string nickname;
    }

    [System.Serializable]
    public class UserData
    {
        public string steamID;
        public string nickname;
    }
    public Fade_InOut fade_inout;

    /*DB에서 해당 steamID로 생성된 User가 있는지 확인*/
    public IEnumerator CheckSteamID(string steamID, NetworkIdentity ClientIdentity)
    {
        SteamIDData dataToSend = new SteamIDData { steamID = steamID };
        string json = JsonUtility.ToJson(dataToSend);

        UnityWebRequest www = new UnityWebRequest(serverURL + "/check-steamID", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            ServerResponse response = JsonUtility.FromJson<ServerResponse>(www.downloadHandler.text);

            /*존재한다면 Ban 유저인지를 확인하고 벤유저일 경우 클라이언트 종료 명령을 보냅니다.
             존재하고 Ban 유저가 아니라면 추가로 필요한 데이터를 가져옵니다.
             존재하지 않는다면 exits가 false인 respone을 전송해 유저 생성을 할 수 있도록 합니다.*/
            if(response.exists)
            {
                if(response.isban)
                {
                    if(ClientIdentity.connectionToClient != null)
                    {
                        loadingserver.isBanUser(ClientIdentity.connectionToClient);
                    }
                }
                else
                {
                    yield return StartCoroutine(GetUserDetails(steamID, ClientIdentity));
                    yield return StartCoroutine(GetDataFromTable("/get-outfit", response.userID, ClientIdentity));
                    yield return StartCoroutine(GetDataFromTable("/get-accessory", response.userID, ClientIdentity));
                    yield return StartCoroutine(GetDataFromTable("/get-characters", response.userID, ClientIdentity));
                }
            }
            else if(!response.exists)
            {
                if(ClientIdentity.connectionToClient != null)
                {
                    loadingserver.DataSendToClient(ClientIdentity.connectionToClient, response);
                }
            }
        }
        www.uploadHandler.Dispose();
    }

    /*DB에서 유니크키인 steamID를 이용해 User 테이블의 정보를 가져옴*/
    IEnumerator GetUserDetails(string steamID, NetworkIdentity ClientIdentity)
    {
        SteamIDData dataToSend = new SteamIDData { steamID = steamID };
        string json = JsonUtility.ToJson(dataToSend);

        UnityWebRequest www = new UnityWebRequest(serverURL + "/get-userdetails", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            ServerResponse response = JsonUtility.FromJson<ServerResponse>(www.downloadHandler.text);
            if(ClientIdentity.connectionToClient != null)
            {
                loadingserver.DataSendToClient(ClientIdentity.connectionToClient, response);
            }
        }
        www.uploadHandler.Dispose();
    }

    /**/
    IEnumerator GetDataFromTable(string endpoint, string userID, NetworkIdentity ClientIdentity)
    {
        UserIDData dataToSend = new UserIDData { userID = userID };
        string json = JsonUtility.ToJson(dataToSend);

        UnityWebRequest www = new UnityWebRequest(serverURL + endpoint, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            //Debug.Log(www.downloadHandler.text);
            if(endpoint == "/get-characters")
            {
                CharactersResponse charactersResponse = JsonUtility.FromJson<CharactersResponse>(www.downloadHandler.text);
                if(ClientIdentity.connectionToClient != null)
                {
                    steamClientScript.CharacterDataSendToClient(ClientIdentity.connectionToClient, charactersResponse);
                }
            }
            else if(endpoint == "/get-outfit")
            {
                OutfitsResponse outfitsResponse = JsonUtility.FromJson<OutfitsResponse>(www.downloadHandler.text);
                if(ClientIdentity.connectionToClient != null)
                {
                    steamClientScript.OutfitDataSendToClient(ClientIdentity.connectionToClient, outfitsResponse);
                }
            }
            else if(endpoint == "/get-accessory")
            {
                AccessoriesResponse accessoriesResponse = JsonUtility.FromJson<AccessoriesResponse>(www.downloadHandler.text);
                if(ClientIdentity.connectionToClient != null)
                {
                    steamClientScript.AccDataSendToClient(ClientIdentity.connectionToClient, accessoriesResponse);
                }
            }
        }
        www.uploadHandler.Dispose();
    }

    public IEnumerator CheckNickname(string check_nickname, NetworkIdentity ClientIdentity)
    {
        NicknameData dataToSend = new NicknameData { nickname = check_nickname };
        string json = JsonUtility.ToJson(dataToSend);

        // Adjust the endpoint for nickname checking
        UnityWebRequest www = new UnityWebRequest(serverURL + "/check-nickname", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            ServerResponse response = JsonUtility.FromJson<ServerResponse>(www.downloadHandler.text);
            //Debug.Log(www.downloadHandler.text); // Handle the response (JSON) appropriately.

            if(response.exists)
            {
                // Handle the case where the nickname exists in the database
                //Debug.Log("Nickname already exists");
                if(ClientIdentity.connectionToClient != null)
                {
                    steamClientScript.TargetNicknameSubmissionResult_fail(ClientIdentity.connectionToClient, response, check_nickname);
                }
            }
            else
            {
                // Handle the case where the nickname doesn't exist
                //Debug.Log("Nickname is available");
                if(ClientIdentity.connectionToClient != null)
                {
                    steamClientScript.TargetNicknameSubmissionResult(ClientIdentity.connectionToClient, response, check_nickname);
                }
            }
        }
        www.uploadHandler.Dispose();
    }

    public IEnumerator CreateNewUser(string nickname, string steamID, NetworkIdentity ClientIdentity)
    {
        UserData dataToSend = new UserData { steamID = steamID, nickname = nickname };
        string json = JsonUtility.ToJson(dataToSend);

        UnityWebRequest www = new UnityWebRequest(serverURL + "/add-user", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            //Debug.Log(www.downloadHandler.text); 
            yield return StartCoroutine(CheckSteamID(steamID, ClientIdentity));
            //yield return fade_inout.StartCoroutine(fade_inout.FadeinCanvas(10));
            steamClientScript.CreateDone(ClientIdentity.connectionToClient);
        }

        www.uploadHandler.Dispose();
    }
    
}
