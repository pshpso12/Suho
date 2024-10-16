using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;
using TMPro; // For TMP_InputField Remove later
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System;
using UnityEngine.EventSystems;
using System.Linq;
using Newtonsoft.Json;

public class MainLobby : NetworkBehaviour
{
    private AttachMain ForRoomCon_AtMain;
    private LobbyThings lobbythings;
    private CharThings charthings;
    private CusThings custhings;
    private ShopThings shopthings;
    private PurchasesThings purchaseshings;
    private List<string> profanitiesList = new List<string>();
    private List<string> messageBuffer = new List<string>();
    private const int maxMessageCount = 30;
    
    private static event Action<string> OnMessage;
    private string lastWhisperID = "";
    private List<string> messageHistory = new List<string>();
    private int currentHistoryIndex = -1;
    
    private Callback<MicroTxnAuthorizationResponse_t> microTxnCallback;

    public BackgroundManager audioManager;
    public UISoundManager UisoundManager;
    [SerializeField] private Texture2D cursorTexture;

    private float lastMessageTime = 0f;
    private int messageCount = 0;
    private float restrictionTime = 30f;
    private int maxMessagesPerSecond = 3;
    private bool messageenabled = true;
    
    void Start()
    {
        /*event와 Callback 등록*/
        OnMessage += HandleNewMessage;
        microTxnCallback = Callback<MicroTxnAuthorizationResponse_t>.Create(OnMicroTxnAuthorizationResponse);
    }
    
    public void Initialize()
    {
        if(isClient && isLocalPlayer)
        {
            /*GameServer에서 Lobby로 이동 시 저장된 데이터 삭제*/
            Clone_Datas CloneDatas_22 = FindObjectOfType<Clone_Datas>();
            if(CloneDatas_22 != null)
                Destroy(CloneDatas_22.gameObject);

            /*비속어 불러오기*/
            LoadProfanities();

            
            chatInputField.onEndEdit.AddListener(Send);
            /*"/r"로 다시 보내는 기능*/
            chatInputField.onValueChanged.AddListener(delegate { HandleInputFieldChange(); });

            RoomCreateButton.onClick.AddListener(OnButtonSendRoom);

            /*결제 기능 적용*/
            PurChaseLoad();

            /*채팅 기능이 없는 커스터마이징, 상점에서 다시 로비로 올 때 그 사이에 온 메시지 입력 (최신 순으로 최대 30개)*/
            if (chatText != null && messageBuffer.Count > 0)
            {
                chatText.text = string.Concat(messageBuffer);
                messageBuffer.Clear();
            }
            /*방 목록 찾기와 새로고침 적용*/
            GameObject ForRoomCon = GameObject.Find("Scenmanager");
            if(ForRoomCon != null)
            {
                ForRoomCon_AtMain = ForRoomCon.GetComponent<AttachMain>();
                if(ForRoomCon_AtMain != null)
                {
                    ForRoomCon_AtMain.onReLoadRoom.AddListener(ReLoadRooms);
                    ForRoomCon_AtMain.onSendRoom.AddListener(SendRoom);
                }
            }
            /*초기 로비 이동 시 방 목록 새로고침*/
            ReLoadRooms();

            if (cursorTexture != null)
            {
                Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
            }

            if(audioManager != null)
            {
                audioManager.StopIngameMusic();
                if(!audioManager.IsMusicBackPlaying())
                {
                    audioManager.PlayBackMusic();
                }
                if(!audioManager.IsMusicPlaying())
                {
                    audioManager.PlayMusic_A();
                }
            }
            else
            {
                audioManager = GameObject.Find("SoundObject").GetComponent<BackgroundManager>();
                audioManager.ToLobbyMusic();
                if(!audioManager.IsMusicBackPlaying())
                {
                    audioManager.PlayBackMusic();
                }
            }
            
            UisoundManager = GameObject.Find("UI_SoundObject").GetComponent<UISoundManager>();
            Button Schange_chbtn1 = GameObject.Find("Ui_Underone/Btn_im").GetComponent<Schange_Script>().ch_btn1;
            Button Schange_chbtn2 = GameObject.Find("Ui_Underone/Btn_im").GetComponent<Schange_Script>().ch_btn2;
            Button Schange_chbtn3 = GameObject.Find("Ui_Underone/Btn_im").GetComponent<Schange_Script>().ch_btn3;
            Button SQuitBtn = GameObject.Find("Ui_Overone/Over_Btn/Quit_btn").GetComponent<Button>();
            Button SSetBtn = GameObject.Find("Ui_Overone/Over_Btn/Set_btn").GetComponent<Button>();
            Button UiCreBtn = GameObject.Find("Ui_cre/Cre_btn").GetComponent<Button>();
            AddButtonListeners(Schange_chbtn1, true, false, 0);
            AddButtonListeners(Schange_chbtn2, true, false, 0);
            AddButtonListeners(Schange_chbtn3, true, false, 0);
            AddButtonListeners(ForRoomCon_AtMain.RoomSearchButton, true, true, 0);
            AddButtonListeners(ForRoomCon_AtMain.RoomReloadButton, true, true, 0);
            AddButtonListeners(ForRoomCon_AtMain.RoomleftButton, true, true, 0);
            AddButtonListeners(ForRoomCon_AtMain.RoomrightButton, true, true, 0);
            AddButtonListeners(UiCreBtn, true, true, 1);
            AddButtonListeners(SQuitBtn, true, true, 1);
            AddButtonListeners(SSetBtn, true, true, 1);

            AddButtonListeners(RoomCreateButton, false, true, 3);
            AddButtonListeners(RoomSetUI.transform.Find("Panel/cre_btn_2").GetComponent<Button>(), false, true, 2);
            AddButtonListeners(Room_Exit_Forced.transform.Find("Panel/Buy_success_btn").GetComponent<Button>(), false, true, 3);
            AddButtonListeners(Room_Enter_Error_Passwrong.transform.Find("Panel/Buy_success_btn").GetComponent<Button>(), false, true, 3);
            AddButtonListeners(Room_Enter_Withpass_Panl.transform.Find("Panel/enter_btn_1").GetComponent<Button>(), false, true, 3);
            AddButtonListeners(Room_Enter_Withpass_Panl.transform.Find("Panel/enter_btn_2").GetComponent<Button>(), false, true, 2);
            AddButtonListeners(Room_Enter_Error_Panl.transform.Find("Panel/Buy_success_btn").GetComponent<Button>(), false, true, 3);

            AddButtonListeners(GameObject.Find("Canvas/Quit_set/Panel/cre_btn_1").GetComponent<Button>(), false, true, 3);
            AddButtonListeners(GameObject.Find("Canvas/Quit_set/Panel/cre_btn_2").GetComponent<Button>(), false, true, 2);

            AddButtonListeners(GameObject.Find("Canvas/Set_set/Panel/cre_btn_1").GetComponent<Button>(), false, true, 3);
            AddButtonListeners(GameObject.Find("Canvas/Set_set/Panel/cre_btn_2").GetComponent<Button>(), false, true, 2);
        }
    }

    public void Initialize_Char()
    {
        if(isClient && isLocalPlayer)
        {
            objectActivator = Ui_List.GetComponent<ObjectActivatorM>();
            Change_Btn.onClick.AddListener(OnButtonSendIndex);

            PurChaseLoad();

            Button SQuitBtn = GameObject.Find("Ui_Overone/Over_Btn/Quit_btn").GetComponent<Button>();
            foreach (Button btn in objectActivator.cha_buttons)
            {
                AddButtonListeners(btn, true, true, 0);
            }
            AddButtonListeners(Change_Btn, true, true, 3);
            AddButtonListeners(Log_panel_btn, false, true, 3);
            AddButtonListeners(Log_panel_fail_btn, false, true, 3);
            AddButtonListeners(SQuitBtn, true, true, 0);

            if(audioManager != null)
            {
                audioManager.StopIngameMusic();
                if(audioManager.IsMusicBackPlaying())
                {
                    audioManager.StopBackMusic();
                }
                if(!audioManager.IsMusicPlaying())
                {
                    audioManager.PlayMusic_A();
                }
            }
        }
    }

    public void Initialize_Cus()
    {
        if(isClient && isLocalPlayer)
        {
            GameObject levelListGameObject = GameObject.Find("Charater_List/Scroll View/Viewport");
            if(levelListGameObject != null)
            {
                ScrollControllerM scrollController = levelListGameObject.GetComponent<ScrollControllerM>();
                if(scrollController != null)
                {
                    scrollController.onSendCloth.AddListener(HandleSendCloth);

                    AddButtonListeners(scrollController.Lpage_Btn, true, true, 0);
                    AddButtonListeners(scrollController.Rpage_Btn, true, true, 0);
                    foreach(Button cbtn in scrollController.buttons)
                    {
                        AddButtonListeners(cbtn, true, true, 0);
                    }
                }
            }

            PurChaseLoad();

            Button SQuitBtn = GameObject.Find("Ui_Overone/Over_Btn/Quit_btn").GetComponent<Button>();
            AttachCus ForCusCon = GameObject.Find("Scenmanager").GetComponent<AttachCus>();
            if(ForCusCon)
            {
                foreach (Button btn in ForCusCon.ChaButtons)
                {
                    AddButtonListeners(btn, true, true, 0);
                }
            }
            AddButtonListeners(Cus_Log_panel_fail_btn, false, true, 3);
            AddButtonListeners(SQuitBtn, true, true, 0);

            if(audioManager != null)
            {
                audioManager.StopIngameMusic();
                if(audioManager.IsMusicBackPlaying())
                {
                    audioManager.StopBackMusic();
                }
                if(!audioManager.IsMusicPlaying())
                {
                    //StartCoroutine(audioManager.PlayMusic());
                    audioManager.PlayMusic_A();
                }
            }
        }
    }

    public void Initialize_Shop()
    {
        if(isClient && isLocalPlayer)
        {
            GameObject levelListGameObject = GameObject.Find("Charater_List/Scroll View/Viewport");
            if(levelListGameObject != null)
            {
                scrollController_shop = levelListGameObject.GetComponent<ScrollController_ShopM>();
                if(scrollController_shop != null)
                {
                    scrollController_shop.onSendItem.AddListener(HandleSendItem);

                    foreach(Button cbtn in scrollController_shop.buttons)
                    {
                        AddButtonListeners(cbtn, true, true, 0);
                    }
                }
            }
            Shop_dropdown.onValueChanged.AddListener(delegate {
                DropdownValueChanged(Shop_dropdown);
            });
            Shop_BuySuccess.onClick.AddListener(() => 
            OnButtonItemBuy(Shop_characterNum, Shop_type, Shop_description, Shop_itemName, 
            Shop_price, Shop_priceType, Shop_isWorn));
            Shop_toggle.onValueChanged.AddListener(ToggleValueChanged);

            Button SQuitBtn = GameObject.Find("Ui_Overone/Over_Btn/Quit_btn").GetComponent<Button>();
            Button Shop_BuyNo1 = buy_Panel.transform.Find("Button").GetComponent<Button>();
            Button Shop_BuyNo2 = buy_Panel.transform.Find("Buy_success_btn_no").GetComponent<Button>();
            Button Shop_BuyYes = buy_Panel.transform.Find("Buy_success_btn_yes").GetComponent<Button>();
            AttachShop ForShopCon = GameObject.Find("Scenmanager").GetComponent<AttachShop>();

            PurChaseLoad();
            
            if(ForShopCon)
            {
                AddButtonListeners(ForShopCon.ReloadBtn, true, true, 0);
                foreach (Button btn in ForShopCon.ChaButtons)
                {
                    AddButtonListeners(btn, true, true, 0);
                }
            }
            AddButtonListeners(Shop_BuyYes, false, true, 3);
            AddButtonListeners(Shop_BuyNo1, false, true, 2);
            AddButtonListeners(Shop_BuyNo2, false, true, 2);
            AddButtonListeners(Shop_BuySuccess, false, true, 3);
            AddButtonListeners(buy_Panel.transform.Find("BuyCheck_Panel/Buy_success/Panel/LastBuy_success_btn_no").GetComponent<Button>(), false, true, 2);
            AddButtonListeners(Shop_Log_Success_btn, false, true, 3);
            AddButtonListeners(Shop_Log_Fail_btn, false, true, 3);
            AddButtonListeners(Shop_Log_Fail_btn2, false, true, 3);
            AddButtonListeners(SQuitBtn, true, true, 0);
            
            if(audioManager != null)
            {
                audioManager.StopIngameMusic();
                if(audioManager.IsMusicBackPlaying())
                {
                    audioManager.StopBackMusic();
                }
                if(!audioManager.IsMusicPlaying())
                {
                    //StartCoroutine(audioManager.PlayMusic());
                    audioManager.PlayMusic_A();
                }
            }
        }
    }

    void PurChaseLoad()
    {
        List<int> PurchaseList = new List<int> { 0, 0, 0, 0, 0};

        Purchases_Button = GameObject.Find("Ui_Overone/Purchases_Btn").GetComponent<Button>();
        Purchase_MainPanel = GameObject.Find("Canvas/Purchase_Panel");
        Purchase_Panel = GameObject.Find("Canvas/Purchase_Panel/Purchase_Main/Panel");
        Purchase_textCost = Purchase_Panel.transform.Find("Text_Cost").GetComponent<TMP_Text>();
        Purchase_BuySuccess = Purchase_Panel.transform.Find("BuyCheck_Panel/Buy_success/Panel/LastBuy_success_btn_yes").GetComponent<Button>();
        Purchase_BuyFail = Purchase_Panel.transform.Find("BuyCheck_Panel/Buy_fail/Panel/LastBuy_success_btn_yes").GetComponent<Button>();
        PurchaseCheckLast_Panel = Purchase_Panel.transform.Find("BuyCheck_Panel").gameObject;
        PurchaseCheckReal_Panel = PurchaseCheckLast_Panel.transform.Find("Buy_success").gameObject;
        PurchaseCheckRealFail_Panel = PurchaseCheckLast_Panel.transform.Find("Buy_fail").gameObject;


        Purchases_Button1000 = Purchase_Panel.transform.Find("Purchase1000").GetComponent<Button>();
        Purchases_Button5000 = Purchase_Panel.transform.Find("Purchase5000").GetComponent<Button>();
        Purchases_Button10000 = Purchase_Panel.transform.Find("Purchase10000").GetComponent<Button>();
        Purchases_Button50000 = Purchase_Panel.transform.Find("Purchase50000").GetComponent<Button>();
        Purchases_Button100000 = Purchase_Panel.transform.Find("Purchase100000").GetComponent<Button>();
        Purchases_ButtonReset = Purchase_Panel.transform.Find("PurchaseReset").GetComponent<Button>();

        int totalCost = 0;

        Purchases_Button.onClick.AddListener(() => {
            PurchaseList = new List<int> { 0, 0, 0, 0, 0};
            totalCost = 0;
            Purchase_textCost.text = totalCost.ToString("N0");
            Purchase_MainPanel.SetActive(true);
        });

        Purchases_Button1000.onClick.AddListener(() => {
            PurchaseList[0] += 1;
            totalCost += 1000;
            Purchase_textCost.text = totalCost.ToString("N0");
        });

        Purchases_Button5000.onClick.AddListener(() => {
            PurchaseList[1] += 1;
            totalCost += 5000;
            Purchase_textCost.text = totalCost.ToString("N0");
        });

        Purchases_Button10000.onClick.AddListener(() => {
            PurchaseList[2] += 1;
            totalCost += 10000;
            Purchase_textCost.text = totalCost.ToString("N0");
        });

        Purchases_Button50000.onClick.AddListener(() => {
            PurchaseList[3] += 1;
            totalCost += 50000;
            Purchase_textCost.text = totalCost.ToString("N0");
        });

        Purchases_Button100000.onClick.AddListener(() => {
            PurchaseList[4] += 1;
            totalCost += 100000;
            Purchase_textCost.text = totalCost.ToString("N0");
        });

        Purchases_ButtonReset.onClick.AddListener(() => {
            PurchaseList = new List<int> { 0, 0, 0, 0, 0 };
            totalCost = 0;
            Purchase_textCost.text = totalCost.ToString("N0");
        });
        
        Button PurChaseCheck = Purchase_Panel.transform.Find("Buy_success_btn_yes").GetComponent<Button>();
        PurChaseCheck.onClick.AddListener(() => {
            if(totalCost != 0)
            {
                PurchaseCheckLast_Panel.SetActive(true);
                NetworkIdentity opponentIdentity = GetComponent<NetworkIdentity>();
                CSteamID steamID = SteamUser.GetSteamID();
                int Sumpurchase = CalculateSumPurchase(PurchaseList);
                CmdPurcahseRe(opponentIdentity, steamID.ToString(), Sumpurchase);
            }
        });

        Purchase_BuySuccess.onClick.AddListener(() => {
            PurchaseCheckReal_Panel.SetActive(false);
            PurchaseCheckRealFail_Panel.SetActive(false);
            PurchaseCheckLast_Panel.SetActive(false);
            Purchase_MainPanel.SetActive(false);
        });
        Purchase_BuyFail.onClick.AddListener(() => {
            PurchaseCheckReal_Panel.SetActive(false);
            PurchaseCheckRealFail_Panel.SetActive(false);
            PurchaseCheckLast_Panel.SetActive(false);
            Purchase_MainPanel.SetActive(false);
        });

        Purchase_Log_Success = GameObject.Find("Purchase_success_1");
        Purchase_Log_Success_btn = Purchase_Log_Success.transform.Find("Panel/Buy_success_btn").GetComponent<Button>();
        
        Purchase_Log_Fail = GameObject.Find("Purchase_fail_1");
        Purchase_Log_Fail_btn = Purchase_Log_Fail.transform.Find("Panel/Buy_success_btn").GetComponent<Button>();

        Purchase_Log_Fail2 = GameObject.Find("Purchase_fail_2");
        Purchase_Log_Fail2_btn = Purchase_Log_Fail.transform.Find("Panel/Buy_success_btn").GetComponent<Button>();

        Purchase_Log_Success_btn.onClick.AddListener(() => {
            Purchase_Log_Success.SetActive(false);
            PurchaseCheckReal_Panel.SetActive(false);
            PurchaseCheckRealFail_Panel.SetActive(false);
            PurchaseCheckLast_Panel.SetActive(false);
            Purchase_MainPanel.SetActive(false);
        });

        Purchase_Log_Fail_btn.onClick.AddListener(() => {
            Purchase_Log_Fail.SetActive(false);
            PurchaseCheckReal_Panel.SetActive(false);
            PurchaseCheckRealFail_Panel.SetActive(false);
            PurchaseCheckLast_Panel.SetActive(false);
            Purchase_MainPanel.SetActive(false);
        });

        Purchase_Log_Fail2_btn.onClick.AddListener(() => {
            Purchase_Log_Fail2.SetActive(false);
            PurchaseCheckReal_Panel.SetActive(false);
            PurchaseCheckRealFail_Panel.SetActive(false);
            PurchaseCheckLast_Panel.SetActive(false);
            Purchase_MainPanel.SetActive(false);
        });

        AddButtonListeners(Purchases_Button, false, true, 1);
        AddButtonListeners(Purchases_Button1000, false, true, 0);
        AddButtonListeners(Purchases_Button5000, false, true, 0);
        AddButtonListeners(Purchases_Button10000, false, true, 0);
        AddButtonListeners(Purchases_Button50000, false, true, 0);
        AddButtonListeners(Purchases_Button100000, false, true, 0);
        AddButtonListeners(Purchases_ButtonReset, false, true, 0);

        AddButtonListeners(Purchase_Panel.transform.Find("Buy_success_btn_yes").GetComponent<Button>(), false, true, 3);
        AddButtonListeners(Purchase_Panel.transform.Find("Buy_success_btn_no").GetComponent<Button>(), false, true, 2);
        AddButtonListeners(Purchase_Panel.transform.Find("Button").GetComponent<Button>(), false, true, 2);
        AddButtonListeners(Purchase_BuySuccess, false, true, 3);
        AddButtonListeners(Purchase_Log_Success_btn, false, true, 3);
        AddButtonListeners(Purchase_Log_Fail_btn, false, true, 3);
        AddButtonListeners(Purchase_Log_Fail2_btn, false, true, 3);

        PurchaseCheckLast_Panel.SetActive(false);
        Purchase_MainPanel.SetActive(false);
        Purchase_Log_Success.SetActive(false);
        Purchase_Log_Fail.SetActive(false);
        Purchase_Log_Fail2.SetActive(false);
    }
    int CalculateSumPurchase(List<int> PurchaseList)
    {
        int[] purchaseAmounts = { 1000, 5000, 10000, 50000, 100000 };
        int Sumpurchase = 0;

        for (int i = 0; i < PurchaseList.Count; i++)
        {
            Sumpurchase += PurchaseList[i] * purchaseAmounts[i];
        }

        return Sumpurchase;
    }
    [Command]
    void CmdPurcahseRe(NetworkIdentity ClientIdentity, string steamID, int Sumpurchase)
    {
        mainLobbyServer.StartCoroutine(mainLobbyServer.Purchase_Things(ClientIdentity, steamID, Sumpurchase));
    }
    [TargetRpc]
    public void RpcPurchaseReturn(NetworkConnectionToClient target, bool sucfail)
    {
        if(sucfail == true)
        {
            if(PurchaseCheckReal_Panel != null)
            {
                PurchaseCheckReal_Panel.SetActive(true);
            }
        }
        else
        {
            if(PurchaseCheckRealFail_Panel != null)
            {
                PurchaseCheckRealFail_Panel.SetActive(true);
                if(UisoundManager != null)
                    UisoundManager.PlayWarringSound();
            }
        }
    }
    void OnMicroTxnAuthorizationResponse(MicroTxnAuthorizationResponse_t pCallback) {
		//Debug.Log("[" + MicroTxnAuthorizationResponse_t.k_iCallback + " - MicroTxnAuthorizationResponse] - " + pCallback.m_unAppID + " -- " + pCallback.m_ulOrderID + " -- " + pCallback.m_bAuthorized);
        if (pCallback.m_bAuthorized == 1)
        {
            NetworkIdentity opponentIdentity = GetComponent<NetworkIdentity>();
            SendTransactionToServer(opponentIdentity, pCallback.m_ulOrderID);
        }
        else
        {
            if(Purchase_Log_Fail != null)
            {
                Purchase_Log_Fail.SetActive(true);
            }
        }
    }
    [Command]
    void SendTransactionToServer(NetworkIdentity ClientIdentity, ulong orderID)
    {
        mainLobbyServer.StartCoroutine(mainLobbyServer.GetTransactionReport(ClientIdentity, orderID));
    }
    [TargetRpc]
    public void RpcFinalPurchaseReturn(NetworkConnectionToClient target, bool sucfail)
    {
        if(sucfail == true)
        {
            if(Purchase_Log_Success != null)
            {
                Purchase_Log_Success.SetActive(true);
            }
        }
        else
        {
            if(Purchase_Log_Fail2 != null)
            {
                Purchase_Log_Fail2.SetActive(true);
                if(UisoundManager != null)
                    UisoundManager.PlayWarringSound();
            }
        }
    }
    [TargetRpc]
    public void UpdateCPCost(NetworkConnectionToClient target, int CP)
    {
        ClientDataManager.Instance.CostUpdate(CP, ClientDataManager.Instance.UserDetails.basepoint);
        TMP_Text ReloadCpText = GameObject.Find("Canvas/Ui_Overone/Cp_text").GetComponent<TMP_Text>();
        if(ReloadCpText)
        {
            ReloadCpText.text = $"{ClientDataManager.Instance.UserDetails.cashpoint.ToString("N0")}";
        }
        if(UisoundManager != null)
            UisoundManager.PlayBuySound();
    }
    void AddButtonListeners(Button button, bool enableMouseOverSound, bool enableClickSound, int playNextSound = 0)
    {
        if (enableMouseOverSound)
        {
            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            entry.callback.AddListener((eventData) => {
                if (button.interactable)
                {
                    UisoundManager?.PlayMouseOverSound();
                }
            });
            trigger.triggers.Add(entry);
        }
        if (enableClickSound)
        {
            button.onClick.AddListener(() => {
                if (button.interactable)
                {
                    if (playNextSound == 0)
                    {
                        UisoundManager?.PlayClick_NextSound();
                    }
                    else if (playNextSound == 1)
                    {
                        UisoundManager?.PlayClick_NoneSound();
                    }
                    else if (playNextSound == 2)
                    {
                        UisoundManager?.PlayCancelSound();
                    }
                    else if (playNextSound == 3)
                    {
                        UisoundManager?.PlaySuccessBtnSound();
                    }
                }
            });
        }
    }
    
    public void OnButtonSendIndex()
    {
        if(objectActivator != null)
        {
            int index = objectActivator.Index_Send;
            if (int.TryParse(ClientDataManager.Instance.UserDetails.MainCharacterID, out int MainCharacterID))
            {
                if(index + 1 == MainCharacterID)
                {
                    Log_panel.SetActive(true);
                    EventSystem.current.SetSelectedGameObject(Log_panel_btn.gameObject);
                }
                else
                {
                    NetworkIdentity opponentIdentity = GetComponent<NetworkIdentity>();
                    Change_MainCha(index, opponentIdentity, ClientDataManager.Instance.UserDetails.userID);
                }
            }
        }
    }
    [Command]
    private void Change_MainCha(int index, NetworkIdentity ClientIdentity, string UId)
    {
        //Debug.Log($"Received Index: {index + 1}");
        mainLobbyServer.StartCoroutine(mainLobbyServer.UpdateMaincha(index + 1, ClientIdentity, UId));
    }

    [TargetRpc]
    public void MainChaDataSendToClient(NetworkConnectionToClient target, bool sucfail, int newindex)
    {
        if(sucfail == true)
        {
            ClientDataManager.Instance.UpdateUserDetails_MainCha(newindex.ToString());
            Log_panel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(Log_panel_btn.gameObject);
        }
        else
        {
            Log_panel_fail.SetActive(true);
            EventSystem.current.SetSelectedGameObject(Log_panel_fail_btn.gameObject);
            if(UisoundManager != null)
                UisoundManager.PlayWarringSound();
        }
    }
    public void OnButtonSendRoom()
    {
        string roomName = RoomnameText.text;
        string roomPassword = RoompassText.text;
        int roomPlayerNumber = RoompnumText.value;
        string selectedPlayerNumberText = RoompnumText.options[roomPlayerNumber].text;

        NetworkIdentity opponentIdentity = GetComponent<NetworkIdentity>();
        CmdRoomCre(opponentIdentity, roomName, roomPassword, selectedPlayerNumberText);
        RoomSetUI.SetActive(false);
        Room_Cre_Panl.SetActive(true);
    }

    [Command]
    private void CmdRoomCre(NetworkIdentity ClientIdentity, string roomName, string roomPass, string RoomPNum)
    {
        int capacity = int.Parse(RoomPNum);
        KihanaRoomManager.Instance.CreateRoom(roomName, capacity, roomPass, (createdRoom, loadedScene) => {
            GameObject RoomGameobject = Instantiate(myNetworkRoomPrefab);
            RoomData roomData = RoomGameobject.GetComponent<RoomData>();
            if (roomData != null)
            {
                roomData.RoomId = createdRoom.Id;
                //roomData.FakeHost = true;
                roomData.PlayerIndex = 0;
                roomData.CurrnetRoomNumber = createdRoom.RoomNumber;
                roomData.CurrnetRoomName = createdRoom.RoomName;
                roomData.CurrnetMaxRoomPNumber = createdRoom.MaxRoomPNumber;
                roomData.CurrnetCurrentRoomPNumber = createdRoom.CurrentRoomPNumber;
                roomData.CurrnetPassword = createdRoom.Password;
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
            NetworkServer.Spawn(RoomGameobject, ClientIdentity.connectionToClient);
            roomData.PlayerObj = ClientIdentity.gameObject;
            SceneManager.MoveGameObjectToScene(ClientIdentity.gameObject, loadedScene);
            SceneManager.MoveGameObjectToScene(RoomGameobject, loadedScene);
            TargetChangeScene(ClientIdentity.connectionToClient, "Ingame");
            string roomDataJson = JsonConvert.SerializeObject(KihanaRoomManager.Instance.GetRooms());
            TargetRoomCreationComplete(ClientIdentity.connectionToClient, roomDataJson);
        });
    }
    
    [TargetRpc]
    private void TargetChangeScene(NetworkConnection target, string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    [TargetRpc]
    private void TargetRoomCreationComplete(NetworkConnection target, string roomDataJson)
    {
        Room_Cre_Panl.SetActive(false);
        /*
        if (isLocalPlayer)
        {
            Room_Cre_Panl.SetActive(false);
            //Debug.Log("Room):CreDone!");
        }
        else
        {
            List<Room> rooms = JsonConvert.DeserializeObject<List<Room>>(roomDataJson);
            KihanaRoomManager.Instance.rooms = rooms;
        }
        */
    }
    private void ReLoadRooms()
    {
        NetworkIdentity opponentIdentity = GetComponent<NetworkIdentity>();
        CmdReLoadRooms(opponentIdentity);
    }
    [Command]
    private void CmdReLoadRooms(NetworkIdentity ClientIdentity)
    {
        string roomDataJson = JsonConvert.SerializeObject(KihanaRoomManager.Instance.GetRooms());
        RpcReLoadRooms(ClientIdentity.connectionToClient, roomDataJson);
    }
    [TargetRpc]
    private void RpcReLoadRooms(NetworkConnection target, string roomDataJson)
    {
        List<Room> rooms = JsonConvert.DeserializeObject<List<Room>>(roomDataJson);
        KihanaRoomManager.Instance.rooms = rooms;
        ForRoomCon_AtMain.UpdateRoomList();
    }

    private void SendRoom(Room room)
    {
        Room_Enter_Panl.SetActive(true);
        NetworkIdentity opponentIdentity = GetComponent<NetworkIdentity>();
        CmdSendRoom(opponentIdentity, room);
    }
    [Command]
    private void CmdSendRoom(NetworkIdentity ClientIdentity, Room room)
    {
        Room existingRoom = KihanaRoomManager.Instance.rooms.FirstOrDefault(r => r.RoomNumber == room.RoomNumber);
        if (existingRoom != null && existingRoom.Id == room.Id && existingRoom.RoomStart == false)
        {
            int occupiedSlots = existingRoom.PlayerSlots.Count(slot => slot == true);
            bool isSlotAvailable = existingRoom.PlayerSlots.Any(slot => slot == false) && occupiedSlots < existingRoom.MaxRoomPNumber;
            if (isSlotAvailable && occupiedSlots != 0)
            {
                int playerIndex = existingRoom.PlayerSlots.FindIndex(slot => slot == false);
                if(existingRoom.Password == "" && playerIndex != -1)
                {
                    GameObject RoomGameobject = Instantiate(myNetworkRoomPrefab);
                    RoomData roomData = RoomGameobject.GetComponent<RoomData>();
                    if (roomData != null)
                    {
                        roomData.RoomId = existingRoom.Id;
                        //roomData.FakeHost = false;
                        roomData.PlayerIndex = playerIndex;
                        roomData.CurrnetRoomNumber = existingRoom.RoomNumber;
                        roomData.CurrnetRoomName = existingRoom.RoomName;
                        roomData.CurrnetMaxRoomPNumber = existingRoom.MaxRoomPNumber;
                        roomData.CurrnetCurrentRoomPNumber = existingRoom.CurrentRoomPNumber + 1;
                        roomData.CurrnetPassword = existingRoom.Password;
                        for(int i = 0; i < 4; i++)
                        {
                            Item newItem = new Item
                            {
                                PlayerExist = false,
                                FakeHost = false,
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
                    NetworkServer.Spawn(RoomGameobject, ClientIdentity.connectionToClient);
                    roomData.PlayerObj = ClientIdentity.gameObject;
                    SceneManager.MoveGameObjectToScene(ClientIdentity.gameObject, existingRoom.RoomScene);
                    SceneManager.MoveGameObjectToScene(RoomGameobject, existingRoom.RoomScene);
                    TargetChangeScene(ClientIdentity.connectionToClient, "Ingame");
                    existingRoom.CurrentRoomPNumber += 1;
                    existingRoom.PlayerSlots[playerIndex] = true;
                }
                else if(existingRoom.Password != "" && playerIndex != -1)
                {
                    Enter_Room_Pass(ClientIdentity.connectionToClient, existingRoom);
                }
            }
            else
            {
                string roomDataJson = JsonConvert.SerializeObject(KihanaRoomManager.Instance.GetRooms());
                Enter_Room_Fail(ClientIdentity.connectionToClient, roomDataJson);
            }
        }
        else
        {
            string roomDataJson = JsonConvert.SerializeObject(KihanaRoomManager.Instance.GetRooms());
            Enter_Room_Fail(ClientIdentity.connectionToClient, roomDataJson);
        }
    }
    [TargetRpc]
    private void Enter_Room_Pass(NetworkConnection target, Room room)
    {
        Room_Enter_Panl.SetActive(false);
        Room_Enter_Withpass_Panl.SetActive(true);
        Room_Enter_Withpass_Button.onClick.RemoveAllListeners();
        Room_Enter_Withpass_Button.onClick.AddListener(() => OnSubmitRoomPasswordClicked(room));
    }
    private void OnSubmitRoomPasswordClicked(Room room)
    {
        string password = Room_Enter_Withpass_InputField.text;
        NetworkIdentity opponentIdentity = GetComponent<NetworkIdentity>();
        CmdSubmitRoomPass(opponentIdentity, password, room);
        Room_Enter_Panl.SetActive(true);
    }
    [Command]
    private void CmdSubmitRoomPass(NetworkIdentity ClientIdentity, string password, Room room)
    {
        Room existingRoom = KihanaRoomManager.Instance.rooms.FirstOrDefault(r => r.RoomNumber == room.RoomNumber);
        if (existingRoom != null && existingRoom.Id == room.Id && existingRoom.RoomStart == false)
        {
            int occupiedSlots = existingRoom.PlayerSlots.Count(slot => slot == true);
            bool isSlotAvailable = existingRoom.PlayerSlots.Any(slot => slot == false) && occupiedSlots < existingRoom.MaxRoomPNumber;
            if (isSlotAvailable && occupiedSlots != 0)
            {
                int playerIndex = existingRoom.PlayerSlots.FindIndex(slot => slot == false);
                if(existingRoom.Password == password && playerIndex != -1)
                {
                    GameObject RoomGameobject = Instantiate(myNetworkRoomPrefab);
                    RoomData roomData = RoomGameobject.GetComponent<RoomData>();
                    if (roomData != null)
                    {
                        roomData.RoomId = existingRoom.Id;
                        //roomData.FakeHost = false;
                        roomData.PlayerIndex = playerIndex;
                        roomData.CurrnetRoomNumber = existingRoom.RoomNumber;
                        roomData.CurrnetRoomName = existingRoom.RoomName;
                        roomData.CurrnetMaxRoomPNumber = existingRoom.MaxRoomPNumber;
                        roomData.CurrnetCurrentRoomPNumber = existingRoom.CurrentRoomPNumber + 1;
                        roomData.CurrnetPassword = existingRoom.Password;
                        for(int i = 0; i < 4; i++)
                        {
                            Item newItem = new Item
                            {
                                PlayerExist = false,
                                FakeHost = false,
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
                    NetworkServer.Spawn(RoomGameobject, ClientIdentity.connectionToClient);
                    roomData.PlayerObj = ClientIdentity.gameObject;
                    SceneManager.MoveGameObjectToScene(ClientIdentity.gameObject, existingRoom.RoomScene);
                    SceneManager.MoveGameObjectToScene(RoomGameobject, existingRoom.RoomScene);
                    TargetChangeScene(ClientIdentity.connectionToClient, "Ingame");
                    existingRoom.CurrentRoomPNumber += 1;
                    existingRoom.PlayerSlots[playerIndex] = true;
                }
                else if(existingRoom.Password != password && playerIndex != -1)
                {
                    Enter_Room_Pass_Fail(ClientIdentity.connectionToClient);
                }
            }
            else
            {
                string roomDataJson = JsonConvert.SerializeObject(KihanaRoomManager.Instance.GetRooms());
                Enter_PassRoom_Fail(ClientIdentity.connectionToClient, roomDataJson);
            }
        }
        else
        {
            string roomDataJson = JsonConvert.SerializeObject(KihanaRoomManager.Instance.GetRooms());
            Enter_PassRoom_Fail(ClientIdentity.connectionToClient, roomDataJson);
        }
    }
    [TargetRpc]
    private void Enter_Room_Fail(NetworkConnection target, string roomDataJson)
    {
        Room_Enter_Panl.SetActive(false);
        Room_Enter_Error_Panl.SetActive(true);
        List<Room> rooms = JsonConvert.DeserializeObject<List<Room>>(roomDataJson);
        KihanaRoomManager.Instance.rooms = rooms;
        ForRoomCon_AtMain.UpdateRoomList();
        if(UisoundManager != null)
            UisoundManager.PlayWarringSound();
    }
    [TargetRpc]
    private void Enter_PassRoom_Fail(NetworkConnection target, string roomDataJson)
    {
        Room_Enter_Withpass_Panl.SetActive(false);
        Room_Enter_Panl.SetActive(false);
        Room_Enter_Error_Panl.SetActive(true);
        List<Room> rooms = JsonConvert.DeserializeObject<List<Room>>(roomDataJson);
        KihanaRoomManager.Instance.rooms = rooms;
        ForRoomCon_AtMain.UpdateRoomList();
        if(UisoundManager != null)
            UisoundManager.PlayWarringSound();
    }
    [TargetRpc]
    private void Enter_Room_Pass_Fail(NetworkConnection target)
    {
        Room_Enter_Panl.SetActive(false);
        Room_Enter_Error_Passwrong.SetActive(true);
        if(UisoundManager != null)
            UisoundManager.PlayWarringSound();
    }
    [TargetRpc]
    public void TargetActivateRoomExitForced(NetworkConnection target)
    {
        StartCoroutine(WaitForSceneAndActivate());
    }
    private IEnumerator WaitForSceneAndActivate()
    {
        while (SceneManager.GetActiveScene().name != "Ingame12")
        {
            yield return null; // Wait until the next frame
        }

        if (Room_Exit_Forced != null)
        {
            Room_Exit_Forced.SetActive(true);
            if(UisoundManager != null)
                UisoundManager.PlayWarringSound();
        }
    }
    private void HandleNewMessage(string message)
    {
        if (message.StartsWith("\n<color=#FFDA2F>"))
        {
            var receivedWhisperMatch = Regex.Match(message, @"\[\d{2}:\d{2}\] <b>(.*?)<\/b>이 당신에게: ");
            if (receivedWhisperMatch.Success)
            {
                lastWhisperID = receivedWhisperMatch.Groups[1].Value;
            }
        }
        if(chatText == null && RoomchatText != null)
            RoomchatText.text += message;
        if(chatText != null)
            chatText.text += message;
        else
        {
            messageBuffer.Add(message);
            if (messageBuffer.Count > maxMessageCount)
            {
                messageBuffer.RemoveAt(0);
            }
        }
    }

    private IEnumerator RestrictMessaging()
    {
        messageenabled = false; // 30초 동안 메시지 전송 비활성화
        while (restrictionTime > 0)
        {
            yield return new WaitForSeconds(1f);
            restrictionTime--;
        }
        restrictionTime = 30f;
        messageenabled = true; // 전송 가능하게 다시 활성화
    }

    [Client]
    public void Send(string message)
    {
        if(!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) { return; }
        
        if(string.IsNullOrWhiteSpace(message)) { return; }

        if(!messageenabled)
        {
            HandleNewMessage($"\n<color=#FF3B48>메시지 제한! {restrictionTime}초 후 전송 가능</color=#FF3B48>");
            chatInputField.text = string.Empty;
            chatInputField.ActivateInputField();
            return;
        }
        if(ClientDataManager.Instance.ChatBan)
        {
            HandleNewMessage($"\n<color=#FF3B48>                             ----- 메시지 제한! -----</color=#FF3B48>");
            chatInputField.text = string.Empty;
            chatInputField.ActivateInputField();
            return;
        }

        float currentTime = Time.time;
        if (currentTime - lastMessageTime < 1f)
        {
            messageCount++;
            if (messageCount > maxMessagesPerSecond)
            {
                HandleNewMessage($"\n<color=#FF3B48>메시지 전송이 너무 빈번합니다. (30초 제한)</color=#FF3B48>");
                StartCoroutine(RestrictMessaging());
                chatInputField.text = string.Empty;
                chatInputField.ActivateInputField();
                return;
            }
        }
        else
        {
            lastMessageTime = currentTime;
            messageCount = 1;
        }

        string trimmedMessage = message.Trim();
        bool isWhisper = trimmedMessage.StartsWith("/w ") || trimmedMessage.StartsWith("/Whisper ")
        || trimmedMessage.StartsWith("/귓 ") || trimmedMessage.StartsWith("/msg ");


        foreach (var profanity in profanitiesList)
        {
            if (message.Contains(profanity, StringComparison.OrdinalIgnoreCase))
            {
                string replacement = new string('#', profanity.Length);
                message = Regex.Replace(message, profanity, replacement, RegexOptions.IgnoreCase);
            }
        }
        
        if (isWhisper)
        {
            // Handle sending a whisper here
            string[] splitMessage = trimmedMessage.Split(new char[] { ' ' }, 3);
            if (splitMessage.Length >= 3)
            {
                string whisperTarget = splitMessage[1];
                string whisperMessage = splitMessage[2];
                CmdSendWhisper(whisperTarget, whisperMessage, ClientDataManager.Instance.UserDetails.Nickname);
            }
        }
        else
        {
            // Regular message send
            CmdSendMessage(message, ClientDataManager.Instance.UserDetails.Nickname);
        }
        if (messageHistory.Count >= 3)
        {
            messageHistory.RemoveAt(0); // Remove the oldest message
        }
        messageHistory.Add(message);
        currentHistoryIndex = messageHistory.Count;

        chatInputField.text = string.Empty;

        chatInputField.ActivateInputField();
    }
    [Client]
    private void RoomChatSend(string message)
    {
        if(!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) { return; }
        
        if(string.IsNullOrWhiteSpace(message)) { return; }

        if(!messageenabled)
        {
            HandleNewMessage($"\n<color=#FF3B48>메시지 제한! {restrictionTime}초 후 전송 가능</color=#FF3B48>");
            RoomchatInputField.text = string.Empty;
            RoomchatInputField.ActivateInputField();
            return;
        }
        if(ClientDataManager.Instance.ChatBan)
        {
            HandleNewMessage($"\n<color=#FF3B48>                             ----- 메시지 제한! -----</color=#FF3B48>");
            RoomchatInputField.text = string.Empty;
            RoomchatInputField.ActivateInputField();
            return;
        }

        float currentTime = Time.time;
        if (currentTime - lastMessageTime < 1f)
        {
            messageCount++;
            if (messageCount > maxMessagesPerSecond)
            {
                HandleNewMessage($"\n<color=#FF3B48>메시지 전송이 너무 빈번합니다. (30초 제한)</color=#FF3B48>");
                StartCoroutine(RestrictMessaging());
                RoomchatInputField.text = string.Empty;
                RoomchatInputField.ActivateInputField();
                return;
            }
        }
        else
        {
            lastMessageTime = currentTime;
            messageCount = 1;
        }

        string trimmedMessage = message.Trim();
        bool isWhisper = trimmedMessage.StartsWith("/w ") || trimmedMessage.StartsWith("/Whisper ")
        || trimmedMessage.StartsWith("/귓 ") || trimmedMessage.StartsWith("/msg ");


        foreach (var profanity in profanitiesList)
        {
            if (message.Contains(profanity, StringComparison.OrdinalIgnoreCase))
            {
                string replacement = new string('#', profanity.Length);
                message = Regex.Replace(message, profanity, replacement, RegexOptions.IgnoreCase);
            }
        }
        
        if (isWhisper)
        {
            // Handle sending a whisper here
            string[] splitMessage = trimmedMessage.Split(new char[] { ' ' }, 3);
            if (splitMessage.Length >= 3)
            {
                string whisperTarget = splitMessage[1];
                string whisperMessage = splitMessage[2];
                CmdSendWhisper(whisperTarget, whisperMessage, ClientDataManager.Instance.UserDetails.Nickname);
            }
        }
        else
        {
            // Regular message send
            CmdSendMessage(message, ClientDataManager.Instance.UserDetails.Nickname);
        }
        if (messageHistory.Count >= 3)
        {
            messageHistory.RemoveAt(0); // Remove the oldest message
        }
        messageHistory.Add(message);
        currentHistoryIndex = messageHistory.Count;

        RoomchatInputField.text = string.Empty;

        RoomchatInputField.ActivateInputField();
    }
    [Command]
    private void CmdSendMessage(string message, string nickname)
    {
        string currentTime = DateTime.Now.ToString("HH:mm");
        RpcHandleMessage($"[{currentTime}] {nickname}: {message}");
    }
    [Command]
    private void CmdSendWhisper(string target, string message, string nickname)
    {
        string currentTime = DateTime.Now.ToString("HH:mm");
        
        NetworkConnectionToClient targetConnection = FindPlayerConnection(target);
        if (targetConnection != null)
        {
            if(target != nickname)
            {
                TargetReceiveWhisper(connectionToClient, $"<color=#FFDA2F>[{currentTime}] 당신이 <b>{target}</b>에게: {message}</color>");
                foreach (var player in targetConnection.owned)
                {
                    MainLobby mainlobby = player.GetComponent<MainLobby>();
                    if (mainlobby != null)
                    {
                        player.GetComponent<MainLobby>()?.TargetReceiveWhisper(targetConnection, $"<color=#FFDA2F>[{currentTime}] <b>{nickname}</b>이 당신에게: {message}</color>");
                    }
                }
                
                //TargetReceiveWhisper(targetConnection, $"<color=#FFDA2F>[{currentTime}] <b>{nickname}</b>이 당신에게: {message}</color>");
            }
            else if(target == nickname)
                TargetReceiveWhisper(connectionToClient, $"<color=#FF3B48>[{currentTime}] 자신에게 귓속말을 보낼 수 없습니다.</color>");
        }
        else
        {
            if(target != nickname)
            {
                string steamid_Get = FindPlayerSteamID(target);
                //Debug.Log("ToGame");
                if(steamid_Get != null)
                {
                    var ChatRegi = FindObjectOfType<Insight.ChatServer>();
                    if (ChatRegi != null)
                    {
                        TargetReceiveWhisper(connectionToClient, $"<color=#FFDA2F>[{currentTime}] 당신이 <b>{target}</b>에게: {message}</color>");
                        ChatRegi.SendChatToGame(steamid_Get, $"<color=#FFDA2F>[{currentTime}] <b>{nickname}</b>이 당신에게: {message}</color>");
                    }
                    else
                        TargetReceiveWhisper(connectionToClient, $"<color=#FF3B48>[{currentTime}] 메시지 전송에 실패하였습니다.</color>");
                }
                else
                {
                    TargetReceiveWhisper(connectionToClient, $"<color=#FF3B48>[{currentTime}] <b>{target}</b>을 찾을 수 없습니다.</color>");
                }
            }
            else
            {
                TargetReceiveWhisper(connectionToClient, $"<color=#FF3B48>[{currentTime}] <b>{target}</b>을 찾을 수 없습니다.</color>");
            }
        }
        
    }
    private void LoadProfanities()
    {
        string[] profanities = Resources.Load<TextAsset>("profanities").text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        profanitiesList.AddRange(profanities);
    }
    [ClientRpc]
    private void RpcHandleMessage(string message)
    {
        OnMessage?.Invoke($"\n{message}");
    }
    [TargetRpc]
    public void TargetReceiveWhisper(NetworkConnection target, string message)
    {
        ReceiveWhisper(message);
    }
    private void ReceiveWhisper(string message)
    {
        string coloredMessage = $"{message}";
        OnMessage?.Invoke($"\n{coloredMessage}");
    }
    private NetworkConnectionToClient FindPlayerConnection(string nickname)
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn != null)
            {
                ServerDataManger dataManager = conn.identity.GetComponent<ServerDataManger>();
                if (dataManager != null && dataManager.playerName.Equals(nickname, StringComparison.OrdinalIgnoreCase))
                {
                    return conn;
                }
            }
        }
        return null;
    }
    private string FindPlayerSteamID(string nickname)
    {
        ServerDataManger[] allDataManagers = FindObjectsOfType<ServerDataManger>();
        foreach (ServerDataManger dataManager in allDataManagers)
        {
            if (dataManager.playerName.Equals(nickname, StringComparison.OrdinalIgnoreCase))
            {
                return dataManager.SteamID_; // Assuming steamid_ is of type ulong in your ServerDataManager
            }
        }
        return null; // Return 0 or an appropriate default value indicating not found
    }
    private void HandleInputFieldChange()
    {
        string text = chatInputField.text;
        if (text.StartsWith("/r ", StringComparison.OrdinalIgnoreCase))
        {
            string[] splitText = text.Split(' ', 2);
            if (splitText.Length > 0)
            {
                if (!string.IsNullOrEmpty(lastWhisperID))
                {
                    chatInputField.text = $"/msg {lastWhisperID} ";
                }
                else
                {
                    chatInputField.text = $"/msg ";
                }
                //chatInputField.text = $"/msg {lastWhisperID} ";
                chatInputField.Select();
                chatInputField.MoveTextEnd(false);
            }
        }
    }
    private void HandleRoomInputFieldChange()
    {
        string text = RoomchatInputField.text;
        if (text.StartsWith("/r ", StringComparison.OrdinalIgnoreCase))
        {
            string[] splitText = text.Split(' ', 2);
            if (splitText.Length > 0)
            {
                if (!string.IsNullOrEmpty(lastWhisperID))
                {
                    RoomchatInputField.text = $"/msg {lastWhisperID} ";
                }
                else
                {
                    RoomchatInputField.text = $"/msg ";
                }
                //chatInputField.text = $"/msg {lastWhisperID} ";
                RoomchatInputField.Select();
                RoomchatInputField.MoveTextEnd(false);
            }
        }
    }
    private void HandleSendCloth(string info)
    {
        NetworkIdentity opponentIdentity = GetComponent<NetworkIdentity>();
        string[] values = info.Split(';');
        int characterNum = int.Parse(values[0]);
        string type = values[1];
        int outfitID = int.Parse(values[2]);
        string description = values[3];
        Change_ChaCloths(opponentIdentity, characterNum, type, outfitID, ClientDataManager.Instance.UserDetails.userID);
    }
    [Command]
    private void Change_ChaCloths(NetworkIdentity ClientIdentity, int chaNum, string type, int OutfitID, string UId)
    {
        mainLobbyServer.StartCoroutine(mainLobbyServer.UpdateChaCloth(ClientIdentity, chaNum, type, OutfitID, UId));
    }
    [TargetRpc]
    public void ChaClothsDataSendToClient(NetworkConnectionToClient target, bool sucfail, int chaNum, string type, int OutfitID)
    {
        if(sucfail == true)
        {
            string outfitIDStr = OutfitID.ToString();
            ClientDataManager.Instance.UpdateCharacterOutfit(chaNum, type, outfitIDStr);
        }
        else
        {
            Cus_Log_panel_fail.SetActive(true);
            EventSystem.current.SetSelectedGameObject(Cus_Log_panel_fail_btn.gameObject);
            if(UisoundManager != null)
                UisoundManager.PlayWarringSound();
        }
    }

    private void HandleSendItem(string info)
    {
        string[] values = info.Split(';');
        int characterNum = int.Parse(values[0]);
        string type = values[1];
        string description = values[2];
        string ItemName = values[3];
        string[] priceEntries = values[4].Split('&');
        //Debug.Log($"Price Entries: {string.Join(", ", priceEntries)}");
        List<PriceData> prices = priceEntries.Select(entry => {
            string[] priceInfo = entry.Split(',');
            return new PriceData { price_Type = priceInfo[0], price = int.Parse(priceInfo[1]) };
        }).ToList();

        Shop_textCloth.text = ItemName;
        Sprite itemSprite_Item = Resources.Load<Sprite>($"Images/{description}");
        Shop_outfitImage.sprite = itemSprite_Item;

        Shop_dropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> dropdownOptions = new List<TMP_Dropdown.OptionData>();
        foreach (var priceData in prices)
        {
            string optionText = $"{priceData.price:N0}";
            Sprite optionImage = priceData.price_Type == "CP" ? Cpimage : Bpimage;

            TMP_Dropdown.OptionData optionData = new TMP_Dropdown.OptionData(optionText, optionImage);
            dropdownOptions.Add(optionData);
        }
        Shop_dropdown.AddOptions(dropdownOptions);
        Shop_textCost.text = Shop_dropdown.options[Shop_dropdown.value].text;
        Shop_CostImage.sprite = Shop_dropdown.options[Shop_dropdown.value].image;
        Shop_toggle.isOn = true;
        UpdateButtonBuyValues(characterNum, type, 
        description, ItemName, Shop_textCost.text, Shop_CostImage.sprite.name, Shop_toggle.isOn);
        buy_MainPanel.SetActive(true);
    }

    private void DropdownValueChanged(TMP_Dropdown dropdown)
    {
        string selectedOptionText = dropdown.options[dropdown.value].text;
        Shop_textCost.text = selectedOptionText;
        Shop_CostImage.sprite = dropdown.options[dropdown.value].image;
        UpdateButtonBuyValues(Shop_characterNum, Shop_type, 
        Shop_description, Shop_itemName, Shop_textCost.text, Shop_CostImage.sprite.name, Shop_isWorn);
    }
    private void ToggleValueChanged(bool isOn)
    {
        Shop_isWorn = isOn;
        UpdateButtonBuyValues(Shop_characterNum, Shop_type, Shop_description, Shop_itemName, Shop_price, Shop_priceType, Shop_isWorn);
    }
    private void UpdateButtonBuyValues(int chaNum, string t, string desc, string iName, string p, string pType, bool worn)
    {
        Shop_characterNum = chaNum;
        Shop_type = t;
        Shop_description = desc;
        Shop_itemName = iName;
        if (int.TryParse(p, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.Integer, null, out int shopPrice))
        {
            Shop_price = shopPrice.ToString();
        }
        Shop_priceType = pType;
        Shop_isWorn = worn;
    }
    void OnButtonItemBuy(int ChaNum, string Type, string Description, string ItemName, string Price, string Price_Type, bool isWorn)
    {
        NetworkIdentity opponentIdentity = GetComponent<NetworkIdentity>();
        buyCheckLast_Panel.SetActive(false);
        buy_MainPanel.SetActive(false);
        BuyButton_Check(opponentIdentity, ChaNum, Type, Description, ItemName, Price, Price_Type, isWorn, ClientDataManager.Instance.UserDetails.userID);
    }
    [Command]
    private void BuyButton_Check(NetworkIdentity ClientIdentity, int ChaNum, string Type, string Description, string ItemName, string Price, string Price_Type, bool isWorn, string UId)
    {
        var matchingOutfits = outfitDataList.outfits.Where(outfit => 
            outfit.outfitName == ItemName && 
            outfit.outfit_Type == Type && 
            outfit.CharaterNum == ChaNum && 
            outfit.image.name == Description).ToList();

        if (matchingOutfits.Count == 1)
        {
            var outfit = matchingOutfits[0];
            //Debug.Log($"Character Number: {ChaNum}, Type: {Type}, Description: {Description}, Item Name: {ItemName}, Price: {Price}, Price Type: {Price_Type}, Is Worn: {isWorn}");

            mainLobbyServer.StartCoroutine(mainLobbyServer.Buy_Items(ClientIdentity, ChaNum, Type, Description, Price_Type, Price, isWorn, UId));
        }
    }
    [TargetRpc]
    public void ItemDataSendToClient(NetworkConnectionToClient target, bool ItemExist, bool EnoughPoint, bool WornDone, int CP, int BP)
    {
        NetworkIdentity opponentIdentity = GetComponent<NetworkIdentity>();
        if(ItemExist && !EnoughPoint)
        {
            
            scrollController_shop.ActivateObject(scrollController_shop.currentIndexList);
            ClientDataManager.Instance.CostUpdate(CP, BP);
            scrollController_shop.ReLoadPoint();
            Shop_Log_Fail.SetActive(true);
            EventSystem.current.SetSelectedGameObject(Shop_Log_Fail_btn.gameObject);
            if(UisoundManager != null)
                UisoundManager.PlayWarringSound();
        }
        else if(!ItemExist && !EnoughPoint)
        {

            scrollController_shop.ActivateObject(scrollController_shop.currentIndexList);
            ClientDataManager.Instance.CostUpdate(CP, BP);
            scrollController_shop.ReLoadPoint();
            Shop_Log_Fail2.SetActive(true);
            EventSystem.current.SetSelectedGameObject(Shop_Log_Fail_btn2.gameObject);
            if(UisoundManager != null)
                UisoundManager.PlayWarringSound();
        }
        else if(!ItemExist && EnoughPoint)
        {
            scrollController_shop.ActivateObject(scrollController_shop.currentIndexList);
            ClientDataManager.Instance.CostUpdate(CP, BP);
            scrollController_shop.ReLoadPoint();

            if(WornDone)
            {
                scrollController_shop.ReLoadCha();
            }
            Shop_Log_Success.SetActive(true);
            EventSystem.current.SetSelectedGameObject(Shop_Log_Success_btn.gameObject);
            if(UisoundManager != null)
                UisoundManager.PlayBuySound();
        }
    }

    [TargetRpc]
    public void CharacterDataSendToClient(NetworkConnectionToClient target, SteamLobby_Server.CharactersResponse response)
    {
        ClientDataManager.Instance.UpdateCharacterData(response);
    }
    [TargetRpc]
    public void OutfitDataSendToClient(NetworkConnectionToClient target, SteamLobby_Server.OutfitsResponse response)
    {
        ClientDataManager.Instance.UpdateOutfitData(response);
    }
    [TargetRpc]
    public void AccDataSendToClient(NetworkConnectionToClient target, SteamLobby_Server.AccessoriesResponse response)
    {
        ClientDataManager.Instance.UpdateAccessoriesData(response);
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "Ingame12")
        {
            Initialize();
        }
        else if(scene.name == "Char_Scenes")
        {
            Initialize_Char();
        }
        else if(scene.name == "Cus_Scenes")
        {
            Initialize_Cus();
        }
        else if(scene.name == "Shop_Scenes")
        {
            Initialize_Shop();
        }
        
        else if(scene.name == "Ingame")
        {
            Initialize_Room();
        }
        
    }

    void Update()
    {
        if (!isClient) return;
        
        if(RoomchatInputField != null && RoomchatInputField.isFocused && Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (messageHistory.Count > 0)
            {
                currentHistoryIndex--;
                if (currentHistoryIndex < 0)
                {
                    currentHistoryIndex = messageHistory.Count - 1;
                }
                RoomchatInputField.text = messageHistory[currentHistoryIndex];
                RoomchatInputField.caretPosition = RoomchatInputField.text.Length; // Move the cursor to the end of the text
            }
        }
        else if(chatInputField != null && chatInputField.isFocused && Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (messageHistory.Count > 0)
            {
                currentHistoryIndex--;
                if (currentHistoryIndex < 0)
                {
                    currentHistoryIndex = messageHistory.Count - 1;
                }
                chatInputField.text = messageHistory[currentHistoryIndex];
                chatInputField.caretPosition = chatInputField.text.Length; // Move the cursor to the end of the text
            }
        }

    }
    
}
