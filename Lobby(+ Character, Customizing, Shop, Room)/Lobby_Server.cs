using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System;
using UnityEngine.EventSystems;
using System.Linq;
using Newtonsoft.Json;

/*동작을 쉽게 보기위해 포트폴리오에서는 서버동작과 클라이언트동작을 합쳐두었습니다.*/
/*실제로는 [TargetRpc]가 Loading_Client에서 동작합니다.*/
public class Lobby_Server : NetworkBehaviour
{
    [SerializeField] private LobbyServerScript lobbyserverscript;
    [SerializeField] private GameObject myNetworkRoomPrefab;

    /*캐시 결제 요청 금액을 받아 스팀서버에서 해당 요청이 있는지 확인*/
    [Command]
    void CmdPurcahseRe(NetworkIdentity ClientIdentity, string steamID, int Sumpurchase)
    {
        lobbyserverscript.StartCoroutine(lobbyserverscript.Purchase_Things(ClientIdentity, steamID, Sumpurchase));
    }
    /*스팀서버에서 결제 요청이 있으면 클라이언트는 결제를 확정 팝업 생성, 없다면 실패 팝업 생성*/
    [TargetRpc]
    public void RpcPurchaseReturn(NetworkConnectionToClient target, bool sucfail)
    {
        if(sucfail == true)
        {
            if(purchasethings.PurchaseCheckReal_Panel != null)
            {
                purchasethings.PurchaseCheckReal_Panel.SetActive(true);
            }
        }
        else
        {
            if(purchasethings.PurchaseCheckRealFail_Panel != null)
            {
                purchasethings.PurchaseCheckRealFail_Panel.SetActive(true);
                if(UisoundManager != null)
                    UisoundManager.PlayWarringSound();
            }
        }
    }
    /*최종 결제가 완료되면 클라이언트로 해당 orderID를 받아 DB에 재화 추가*/
    [Command]
    void SendTransactionToServer(NetworkIdentity ClientIdentity, ulong orderID)
    {
        lobbyserverscript.StartCoroutine(lobbyserverscript.GetTransactionReport(ClientIdentity, orderID));
    }
    /*재화가 정상적으로 추가 되었으면 성공 팝업, 아니라면 고객센터 문의 팝업 생성*/
    [TargetRpc]
    public void RpcFinalPurchaseReturn(NetworkConnectionToClient target, bool sucfail)
    {
        if(sucfail == true)
        {
            if(purchasethings.Purchase_Log_Success != null)
            {
                purchasethings.Purchase_Log_Success.SetActive(true);
            }
        }
        else
        {
            if(purchasethings.Purchase_Log_Fail2 != null)
            {
                purchasethings.Purchase_Log_Fail2.SetActive(true);
                if(UisoundManager != null)
                    UisoundManager.PlayWarringSound();
            }
        }
    }
    /*최종 결제가 완료되면 클라이언트의 캐시 새로고침*/
    [TargetRpc]
    public void UpdateCPCost(NetworkConnectionToClient target, int CP)
    {
        ClientDataManager.Instance.CostUpdate(CP, ClientDataManager.Instance.UserDetails.basepoint);
        /*이건 어느 씬이든 동일하기 때문에 적용 가능*/
        TMP_Text ReloadCpText = GameObject.Find("Canvas/Ui_Overone/Cp_text").GetComponent<TMP_Text>();
        if(ReloadCpText)
        {
            ReloadCpText.text = $"{ClientDataManager.Instance.UserDetails.cashpoint.ToString("N0")}";
        }
        if(UisoundManager != null)
            UisoundManager.PlayBuySound();
    }
    /*캐릭터 정보창에서 메인캐릭터 변경 시 DB에서 USER의 메인캐릭터 변경*/
    [Command]
    private void Change_MainCha(int index, NetworkIdentity ClientIdentity, string UId)
    {
        lobbyserverscript.StartCoroutine(lobbyserverscript.UpdateMaincha(index + 1, ClientIdentity, UId));
    }
    /*메인캐릭터가 정상적으로 변경되면 클라이언트에 데이터 저장, 팝업 띄움*/
    [TargetRpc]
    public void MainChaDataSendToClient(NetworkConnectionToClient target, bool sucfail, int newindex)
    {
        if(sucfail == true)
        {
            ClientDataManager.Instance.UpdateUserDetails_MainCha(newindex.ToString());
            charthings.Log_panel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(charthings.Log_panel_btn.gameObject);
        }
        else
        {
            charthings.Log_panel_fail.SetActive(true);
            EventSystem.current.SetSelectedGameObject(charthings.Log_panel_fail_btn.gameObject);
            if(UisoundManager != null)
                UisoundManager.PlayWarringSound();
        }
    }
    /*새로운 방 생성 요청이 오면 방 생성*/
    [Command]
    private void CmdRoomCre(NetworkIdentity ClientIdentity, string roomName, string roomPass, string RoomPNum)
    {
        int capacity = int.Parse(RoomPNum);
        /*RoomManager를 통해 Additive로 씬이 로드 되면, 해당 씬에 방에서의 기능을 담당할 Prefeb을 생성*/
        RoomManager.Instance.CreateRoom(roomName, capacity, roomPass, (createdRoom, loadedScene) => {
            GameObject RoomGameobject = Instantiate(myNetworkRoomPrefab);
            RoomData roomData = RoomGameobject.GetComponent<RoomData>();
            if (roomData != null)
            {
                /*방의 정보를 입력*/
                roomData.RoomId = createdRoom.Id;
                roomData.PlayerIndex = 0;
                roomData.CurrnetRoomNumber = createdRoom.RoomNumber;
                roomData.CurrnetRoomName = createdRoom.RoomName;
                roomData.CurrnetMaxRoomPNumber = createdRoom.MaxRoomPNumber;
                roomData.CurrnetCurrentRoomPNumber = createdRoom.CurrentRoomPNumber;
                roomData.CurrnetPassword = createdRoom.Password;
                 /*방의 유저 정보를 초기화*/
                for(int i = 0; i < 4; i++)
                {
                    Item newItem = new Item
                    {
                        PlayerExist = false,
                        FakeHost = true,
                        PlayerNickname = null,
                        PlayerLevel = 0,
                        PlayerTexture = null,
                        PlayerMainCharacterID = null,
                        PlayerTopOutfitID = null,
                        PlayerBottomOutfitID = null,
                        PlayerShoesOutfitID = null,
                        PlayerAllInOneOutfitID = null,
                        Ready = false
                    };
                    roomData.inventory.Add(newItem);
                }
            }
            /*방의 기능이 있는 오브젝트를 클라이언트에도 생성*/
            NetworkServer.Spawn(RoomGameobject, ClientIdentity.connectionToClient);
            roomData.PlayerObj = ClientIdentity.gameObject;
            /*기존의 Lobby_Client를 포함한 오브젝트를 이동시켜 채팅을 나눌 수 있음*/
            SceneManager.MoveGameObjectToScene(ClientIdentity.gameObject, loadedScene);
            SceneManager.MoveGameObjectToScene(RoomGameobject, loadedScene);
            /*클라이언트들에게 Room씬으로 이동을 명령*/
            TargetChangeScene(ClientIdentity.connectionToClient, "Ingame");
            /*방이 생성되면 모든 클라이언트들에게 생성된 방이 포함된 모든 방 정보를 보내려고 했으나 이용하지 않음*/
            string roomDataJson = JsonConvert.SerializeObject(KihanaRoomManager.Instance.GetRooms());
            TargetRoomCreationComplete(ClientIdentity.connectionToClient, roomData    Json);
        });
    }
    /*클라이언트의 씬 전환*/
    [TargetRpc]
    private void TargetChangeScene(NetworkConnection target, string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    /*방생성중 팝업을 닫음*/
    [TargetRpc]
    private void TargetRoomCreationComplete(NetworkConnection target, string roomDataJson)
    {
        lobbythings.Room_Cre_Panl.SetActive(false);
    }
    /*클라이언트가 방 새로고침 버튼 클릭 시 서버는 현재의 방 정보를 보내줌*/
    [Command]
    private void CmdReLoadRooms(NetworkIdentity ClientIdentity)
    {
        string roomDataJson = JsonConvert.SerializeObject(KihanaRoomManager.Instance.GetRooms());
        RpcReLoadRooms(ClientIdentity.connectionToClient, roomDataJson);
    }
    /*클라이언트는 방 정보를 받고 방 목록을 새로고침*/
    [TargetRpc]
    private void RpcReLoadRooms(NetworkConnection target, string roomDataJson)
    {
        List<Room> rooms = JsonConvert.DeserializeObject<List<Room>>(roomDataJson);
        RoomManager.Instance.rooms = rooms;
        lobbybase.UpdateRoomList();
    }
}
