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
using Insight;

/*동작을 쉽게 보기위해 포트폴리오에서는 서버동작과 클라이언트동작을 합쳐두었습니다.*/
public class Loading_Server : NetworkBehaviour
{
    /*클라이언트로 부터 버전 정보를 받고 동일한지 확인합니다.*/
    [Command]
    private void CmdSendGameVersionString(string GVersion)
    {
        if(GameVersionString == GVersion)
        {
            RpcVersionMatch(1);
        }
        else
        {
            RpcVersionMatch(2);
        }
    }
    /*클라이언트의 변수가 변경되고 CheckGameVersion()에서 이를 확인합니다.*/
    [TargetRpc]
    public void RpcVersionMatch(int isVersionMatch)
    {
        GameVersionCheck = isVersionMatch;
    }
    /*SteamID를 클라이언트로 부터 받아, 해당 SteamID가 DB에 존재하는지 확인합니다.*/
    [Command]
    private void CmdSendSteamData(string steamID, NetworkIdentity ClientIdentity)
    {
        LodingServerScript.StartCoroutine(LodingServerScript.CheckSteamID(steamID, ClientIdentity));
    }
    /*LoadingServerScript를 통해 벤 유저인 경우 클라이언트는 프로그램을 종료합니다.*/
    [TargetRpc]
    public void isBanUser(NetworkConnectionToClient target)
    {
        Application.Quit();
    }
    /*클라이언트는 정보를 respone 정보를 저장하고, isUserDataUpdated_1 변수를 true로 설정합니다.
    클라이언트는 Update()에서 다음을 확인하고 HandleLoadingProcess()를 진행합니다.*/
    [TargetRpc]
    public void DataSendToClient(NetworkConnectionToClient target, SteamLobby_Server.ServerResponse response)
    {
        ClientDataManager.Instance.UpdateUserDetails(response);
        isUserDataUpdated_1 = true;
    }
    /*클라이언트는 캐릭터 상태를 저장합니다.*/
    [TargetRpc]
    public void CharacterDataSendToClient(NetworkConnectionToClient target, SteamLobby_Server.CharactersResponse response)
    {
        ClientDataManager.Instance.UpdateCharacterData(response);
    }
    /*클라이언트는 보유 의상 정보를 저장합니다.*/
    [TargetRpc]
    public void OutfitDataSendToClient(NetworkConnectionToClient target, SteamLobby_Server.OutfitsResponse response)
    {
        ClientDataManager.Instance.UpdateOutfitData(response);
    }
    /*클라이언트는 보유 악세사리 정보를 저장합니다.*/
    [TargetRpc]
    public void AccDataSendToClient(NetworkConnectionToClient target, SteamLobby_Server.AccessoriesResponse response)
    {
        ClientDataManager.Instance.UpdateAccessoriesData(response);
    }
    /*서버는 닉네임과 SteamID를 받아 유저 생성을 진행합니다.*/
    [Command]
    private void CmdCreateNewUser(string nickname, string steamID, NetworkIdentity ClientIdentity)
    {
        LodingServerScript.StartCoroutine(LodingServerScript.CreateNewUser(nickname, steamID, ClientIdentity));
    }
    /*유저 생성이 정상적으로 완료되었으면, 비동기로 로드한 씬을 완료하여 Lobby로 이동합니다.*/
    [TargetRpc]
    public void CreateDone(NetworkConnectionToClient target)
    {
        sceneAsync.allowSceneActivation = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        
    }
}
