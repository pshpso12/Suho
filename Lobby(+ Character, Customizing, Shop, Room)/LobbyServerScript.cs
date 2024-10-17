using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.Networking;
using Mirror;
using System;

public class LobbyServerScript : MonoBehaviour
{
    private string serverURL = "http://localhost:xxxx";
    private Lobby_Server loadingserver;

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
    }
    public SteamLobby_Server.CharactersResponse CharacterData { get; private set; }
    public SteamLobby_Server.OutfitsResponse OutfitData { get; private set; }
    public SteamLobby_Server.AccessoriesResponse AccessoriesData { get; private set; }
    [System.Serializable]
    public class UpdateCharacterIDData
    {
        public string userID;
        public int newMainCharacterID;
    }
    [System.Serializable]
    public class CheckData
    {
        public bool success;
    }
    [System.Serializable]
    public class UpdateClothData
    {
        public string userID;
        public int characterNum;
        public string type;
        public int outfitID;
    }
    [System.Serializable]
    public class CheckItemData
    {
        public string userID;
        public int characterNum;
        public string type;
        public string description;
        public string priceType;
        public int price;
        public bool worn;
    }
    [System.Serializable]
    public class CheckDataItem
    {
        public bool alreadyOwned;
        public bool sufficientFunds;
        public bool wornDone;
        public int cashpoint;
        public int basepoint;
    }

    [System.Serializable]
    public class PurchaseResponse
    {
        public ResponseData response;
        [System.Serializable]
        public class ResponseData
        {
            public string result;      // 성공 또는 실패 여부 ("OK"가 성공)
            public int errorcode;      // 에러 코드 (1이 성공)
            public string errordesc;   // 에러 설명
            public ParamsData @params; // 거래 관련 데이터 (필요에 따라 사용)

            [System.Serializable]
            public class ParamsData
            {
                public string orderid;
                public string transid;
            }
        }
    }
    [System.Serializable]
    public class TransactionReportResponse
    {
        public ResponseData response;
        [System.Serializable]
        public class ResponseData
        {
            public string result;
            public ParamsData @params;

            [System.Serializable]
            public class ParamsData
            {
                public string orderid;
                public string transid;
                public string steamid;
                public string status;
                public string currency;
                public string time;
                public string country;
                public string timecreated;
                public ItemData[] items;

                [System.Serializable]
                public class ItemData
                {
                    public int itemid;
                    public int qty;
                    public int amount;
                    public int vat;
                    public string itemstatus;
                }
            }
        }
    }

    [System.Serializable]
    public class SteamIDItemData
    {
        public string steamID;
        public int itemPrice;
    }

    public IEnumerator UpdateMaincha(int newIndex, NetworkIdentity ClientIdentity, string UId)
    {
        UpdateCharacterIDData dataToSend = new UpdateCharacterIDData { userID = UId, newMainCharacterID = newIndex };
        string json = JsonUtility.ToJson(dataToSend);

        UnityWebRequest www = new UnityWebRequest(serverURL + "/update-main-character", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        bool success = false;

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + www.error);
        }
        else
        {
            CheckData response = JsonUtility.FromJson<CheckData>(www.downloadHandler.text);
            success = response.success;
            if(ClientIdentity.connectionToClient != null)
            {
               mainLobby.MainChaDataSendToClient(ClientIdentity.connectionToClient, success, newIndex);
            }
        }
        www.uploadHandler.Dispose();
    }

    public IEnumerator UpdateChaCloth(NetworkIdentity ClientIdentity, int chaNum, string type, int OutfitID, string UId)
    {
        UpdateClothData dataToSend = new UpdateClothData
        {
            userID = UId,
            characterNum = chaNum,
            type = type,
            outfitID = OutfitID
        };

        string json = JsonUtility.ToJson(dataToSend);

        UnityWebRequest www = new UnityWebRequest(serverURL + "/update-character-clothes", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();
        bool success = false;
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + www.error);
        }
        else
        {
            CheckData response = JsonUtility.FromJson<CheckData>(www.downloadHandler.text);
            success = response.success;
            if(ClientIdentity.connectionToClient != null)
            {
               mainLobby.ChaClothsDataSendToClient(ClientIdentity.connectionToClient, success, chaNum, type, OutfitID);
            }
        }
        www.uploadHandler.Dispose();
    }

    public IEnumerator Buy_Items(NetworkIdentity ClientIdentity, int ChaNum, string Type, string Description, string Price_Type, string Price, bool isWorn, string UId)
    {
        CheckItemData dataToSend = new CheckItemData
        {
            userID = UId,
            characterNum = ChaNum,
            type = Type,
            description = Description,
            priceType = Price_Type,
            price = int.Parse(Price),
            worn = isWorn
        };

        string json = JsonUtility.ToJson(dataToSend);

        UnityWebRequest www = UnityWebRequest.Post(serverURL + "/check-item-existence", json);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            CheckDataItem response = JsonUtility.FromJson<CheckDataItem>(www.downloadHandler.text);
            if(response.alreadyOwned)
            {
                yield return StartCoroutine(GetDataFromTable_Main_Shop("/get-outfit", UId, ClientIdentity));
                mainLobby.ItemDataSendToClient(ClientIdentity.connectionToClient, true, false, isWorn, response.cashpoint, response.basepoint);
            }
            else if (!response.sufficientFunds)
            {
                yield return StartCoroutine(GetDataFromTable_Main_Shop("/get-outfit", UId, ClientIdentity));
                mainLobby.ItemDataSendToClient(ClientIdentity.connectionToClient, false, false, isWorn, response.cashpoint, response.basepoint);
            }
            else if(response.sufficientFunds && response.wornDone)
            {
                yield return StartCoroutine(GetDataFromTable_Main_Shop("/get-outfit", UId, ClientIdentity));
                yield return StartCoroutine(GetDataFromTable_Main_Shop("/get-characters", UId, ClientIdentity));
                mainLobby.ItemDataSendToClient(ClientIdentity.connectionToClient, false, true, isWorn, response.cashpoint, response.basepoint);
            }
            
        }
        else
        {
            Debug.LogError("Error: " + www.error);
        }
        www.uploadHandler.Dispose();
    }

    public IEnumerator GetDataFromTable_Main_Shop(string endpoint, string userID, NetworkIdentity ClientIdentity)
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
                SteamLobby_Server.CharactersResponse charactersResponse = JsonUtility.FromJson<SteamLobby_Server.CharactersResponse>(www.downloadHandler.text);
                if(ClientIdentity.connectionToClient != null)
                {
                    mainLobby.CharacterDataSendToClient(ClientIdentity.connectionToClient, charactersResponse);
                }
            }
            else if(endpoint == "/get-outfit")
            {
                SteamLobby_Server.OutfitsResponse outfitsResponse = JsonUtility.FromJson<SteamLobby_Server.OutfitsResponse>(www.downloadHandler.text);
                if(ClientIdentity.connectionToClient != null)
                {
                    mainLobby.OutfitDataSendToClient(ClientIdentity.connectionToClient, outfitsResponse);
                }
            }
            else if(endpoint == "/get-accessory")
            {
                SteamLobby_Server.AccessoriesResponse accessoriesResponse = JsonUtility.FromJson<SteamLobby_Server.AccessoriesResponse>(www.downloadHandler.text);
                if(ClientIdentity.connectionToClient != null)
                {
                    mainLobby.AccDataSendToClient(ClientIdentity.connectionToClient, accessoriesResponse);
                }
            }
        }
        www.uploadHandler.Dispose();
    }

    public IEnumerator Purchase_Things(NetworkIdentity ClientIdentity, string steamID, int purchaseList)
    {
        string url = "https://partner.steam-api.com/ISteamMicroTxn/InitTxn/v3/";
        string apiKey = ClientDataManager.Instance.SteamWebApi;
        string orderid = GenerateUInt64FromGuid().ToString();

        WWWForm form = new WWWForm();
        form.AddField("key", apiKey);
        form.AddField("orderid", orderid);  // Using ticks for unique orderid
        form.AddField("steamid", steamID);
        form.AddField("appid", 3236600);  // Replace with your actual app id
        form.AddField("itemcount", 1);  // Number of items in the purchase
        form.AddField("language", "ko");
        form.AddField("currency", "KRW");
        
        form.AddField("itemid[0]", purchaseList);  // Item ID
        form.AddField("qty[0]", 1);  // Quantity
        form.AddField("amount[0]", purchaseList * 100);  // Price in cents
        form.AddField("description[0]", $"{purchaseList.ToString("N0")} CP");

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        www.downloadHandler = new DownloadHandlerBuffer();

        /*
        string jsonData = $@"
        {{
          ""key"": ""{apiKey}"",
          ""orderid"": ""1"",
          ""steamid"": ""{steamID}"",
          ""appid"": ""{3236600}"",
          ""itemcount"": 1,
          ""language"": ""kr"",
          ""currency"": ""KRW"",
          ""items"": [
            {{
              ""itemid"": {purchaseList},
              ""qty"": 1,
              ""amount"": {purchaseList},
              ""description"": ""{purchaseList.ToString("N0")} CP""
            }}
          ]
        }}";

        UnityWebRequest www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        */
        
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(www.error);
            Debug.Log(www.downloadHandler.text.Replace("\"", " "));
            if(ClientIdentity.connectionToClient != null)
            {
                mainLobby.RpcPurchaseReturn(ClientIdentity.connectionToClient, false);
            }
        }
        else
        {
            PurchaseResponse response = JsonUtility.FromJson<PurchaseResponse>(www.downloadHandler.text);
            Debug.Log("Response: " + www.downloadHandler.text.Replace("\"", " "));
            if (response.response.result == "OK")
            {
                if (ClientIdentity.connectionToClient != null)
                {
                    mainLobby.RpcPurchaseReturn(ClientIdentity.connectionToClient, true);
                }
                //StartCoroutine(GetTransactionReport(ClientIdentity, orderid));
            }
            else
            {
                Debug.LogError($"Purchase failed with error: {response.response.errordesc}");
                if (ClientIdentity.connectionToClient != null)
                {
                    mainLobby.RpcPurchaseReturn(ClientIdentity.connectionToClient, false);
                }
            }
        }
        www.uploadHandler.Dispose();
    }

    public IEnumerator GetTransactionReport(NetworkIdentity ClientIdentity, ulong orderid)
    {
        string reportUrl = "https://partner.steam-api.com/ISteamMicroTxn/QueryTxn/v3/";
        string apiKey = ClientDataManager.Instance.SteamWebApi;

        string fullUrl = $"{reportUrl}?key={apiKey}&appid={3236600}&orderid={orderid.ToString()}";

        UnityWebRequest reportRequest = UnityWebRequest.Get(fullUrl);
        reportRequest.downloadHandler = new DownloadHandlerBuffer();

        yield return reportRequest.SendWebRequest();

        if (reportRequest.result == UnityWebRequest.Result.ConnectionError || reportRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(reportRequest.error);
            Debug.Log(reportRequest.downloadHandler.text.Replace("\"", " "));
            if(ClientIdentity.connectionToClient != null)
            {
                mainLobby.RpcFinalPurchaseReturn(ClientIdentity.connectionToClient, false);
            }
        }
        else
        {
            Debug.Log("Transaction report: " + reportRequest.downloadHandler.text.Replace("\"", " "));
            
            TransactionReportResponse report = JsonUtility.FromJson<TransactionReportResponse>(reportRequest.downloadHandler.text);
            if (report.response.result == "OK" && report.response.@params.status == "Approved")
            {
                string steamid = report.response.@params.steamid;
                int itemid = report.response.@params.items[0].itemid;

                Debug.Log($"SteamID: {steamid}, ItemID: {itemid}");

                StartCoroutine(FinalizeTransaction(ClientIdentity, orderid, steamid, itemid));
            }
            else
            {
                Debug.LogError("Transaction was not approved.");
                if (ClientIdentity.connectionToClient != null)
                {
                    mainLobby.RpcFinalPurchaseReturn(ClientIdentity.connectionToClient, false);
                }
            }
        }
    }

    private IEnumerator FinalizeTransaction(NetworkIdentity ClientIdentity, ulong orderid, string stemid, int itemid)
    {
        string finalizeUrl = "https://partner.steam-api.com/ISteamMicroTxn/FinalizeTxn/v2/";
        string apiKey = ClientDataManager.Instance.SteamWebApi;

        WWWForm form = new WWWForm();
        form.AddField("key", apiKey);
        form.AddField("orderid", orderid.ToString());
        form.AddField("appid", 3236600);

        UnityWebRequest finalizeRequest = UnityWebRequest.Post(finalizeUrl, form);
        finalizeRequest.downloadHandler = new DownloadHandlerBuffer();

        yield return finalizeRequest.SendWebRequest();

        if (finalizeRequest.result == UnityWebRequest.Result.ConnectionError || finalizeRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(finalizeRequest.error);
            Debug.Log(finalizeRequest.downloadHandler.text.Replace("\"", " "));
            if(ClientIdentity.connectionToClient != null)
            {
                mainLobby.RpcFinalPurchaseReturn(ClientIdentity.connectionToClient, false);
            }
        }
        else
        {
            Debug.Log("Finalize transaction response: " + finalizeRequest.downloadHandler.text.Replace("\"", " "));

            var jsonResponse = JsonUtility.FromJson<TransactionReportResponse>(finalizeRequest.downloadHandler.text);
            if (jsonResponse.response.result == "OK")
            {
                Debug.Log("Transaction finalized successfully! Order ID: " + jsonResponse.response.@params.orderid);
                Debug.Log("Transaction ID: " + jsonResponse.response.@params.transid);
                
                if (itemid > 0)
                {
                    StartCoroutine(PurchaseItem(ClientIdentity, stemid, itemid));
                }
                else
                {
                    if (ClientIdentity.connectionToClient != null)
                    {
                        mainLobby.RpcFinalPurchaseReturn(ClientIdentity.connectionToClient, false);
                    }
                }
                //StartCoroutine(PurchaseItem(ClientIdentity, stemid, itemid));
            }
            else
            {
                Debug.LogError("Finalize was not approved.");
                if (ClientIdentity.connectionToClient != null)
                {
                    mainLobby.RpcFinalPurchaseReturn(ClientIdentity.connectionToClient, false);
                }
            }
        }
    }

    private IEnumerator PurchaseItem(NetworkIdentity ClientIdentity, string steamID_, int itemPrice_)
    {
        SteamIDItemData dataToSend = new SteamIDItemData
        { 
            steamID = steamID_,
            itemPrice = itemPrice_
        };
        string json = JsonUtility.ToJson(dataToSend);

        UnityWebRequest www = new UnityWebRequest(serverURL + "/purchase-item", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + www.error);
            if(ClientIdentity.connectionToClient != null)
            {
                mainLobby.RpcFinalPurchaseReturn(ClientIdentity.connectionToClient, false);
            }
        }
        else
        {
            Debug.Log("Purchase response: " + www.downloadHandler.text.Replace("\"", " "));
            ServerResponse response = JsonUtility.FromJson<ServerResponse>(www.downloadHandler.text);
            
            if (response.exists)
            {
                Debug.Log("Updated Cashpoint: " + response.cashpoint);
                if(ClientIdentity.connectionToClient != null)
                {
                    mainLobby.RpcFinalPurchaseReturn(ClientIdentity.connectionToClient, true);
                    mainLobby.UpdateCPCost(ClientIdentity.connectionToClient, response.cashpoint);
                }
            }
            else
            {
                Debug.LogError("Purchase failed: " + response.message);
                if(ClientIdentity.connectionToClient != null)
                {
                    mainLobby.RpcFinalPurchaseReturn(ClientIdentity.connectionToClient, false);
                }
            }
        }
        www.uploadHandler.Dispose();
    }

    public static ulong GenerateUInt64FromGuid()
    {
        Guid guid = Guid.NewGuid();
        byte[] bytes = guid.ToByteArray();
        ulong uint64Value = BitConverter.ToUInt64(bytes, 0);
        
        return uint64Value;
    }
}
