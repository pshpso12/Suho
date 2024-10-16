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
    public SteamLobby steamLobbyScript;
    [SerializeField] private GameObject chatUI;
    [SerializeField] private TMP_Text chatText;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TMP_Text InputFieldText;

    [SerializeField] private GameObject RoomSetUI;
    [SerializeField] private TMP_InputField RoomnameText;
    [SerializeField] private TMP_InputField RoompassText;
    [SerializeField] private TMP_Dropdown RoompnumText;
    [SerializeField] private Button RoomCreateButton;
    [SerializeField] private GameObject Room_Cre_Panl;
    [SerializeField] public GameObject myNetworkRoomPrefab;
    [SerializeField] private GameObject Room_Enter_Panl;
    [SerializeField] private GameObject Room_Enter_Error_Panl;
    [SerializeField] private GameObject Room_Enter_Withpass_Panl;
    [SerializeField] private TMP_InputField Room_Enter_Withpass_InputField;
    [SerializeField] private Button Room_Enter_Withpass_Button;
    [SerializeField] private GameObject Room_Enter_Error_Passwrong;
    [SerializeField] private GameObject Room_Exit_Forced;
    private AttachMain ForRoomCon_AtMain;

    private List<string> profanitiesList = new List<string>();
    private List<string> messageBuffer = new List<string>();
    private const int maxMessageCount = 30;
    
    private static event Action<string> OnMessage;
    public string lastWhisperID = "";
    private List<string> messageHistory = new List<string>();
    private int currentHistoryIndex = -1;

    [SerializeField] private GameObject Ui_List;
    [SerializeField] private ObjectActivatorM objectActivator;
    [SerializeField] private Button Change_Btn;
    [SerializeField] private GameObject Log_panel;
    [SerializeField] private Button Log_panel_btn;
    [SerializeField] private GameObject Log_panel_fail;
    [SerializeField] private Button Log_panel_fail_btn;
    public MainLobby_Server mainLobbyServer;
    
    [SerializeField] private GameObject Cus_Log_panel_fail;
    [SerializeField] private Button Cus_Log_panel_fail_btn;

    private ScrollController_ShopM scrollController_shop;
    [SerializeField] private GameObject buy_MainPanel;
    [SerializeField] private TMP_Text Shop_textCloth;
    [SerializeField] private Image Shop_outfitImage;
    [SerializeField] private TMP_Dropdown Shop_dropdown;
    [SerializeField] private TMP_Text Shop_textCost;
    [SerializeField] private Image Shop_CostImage;
    [SerializeField] private Toggle Shop_toggle;
    [SerializeField] private Button Shop_BuySuccess;
    [SerializeField] private GameObject buyCheckLast_Panel;
    public Sprite Cpimage;
    public Sprite Bpimage;
    public OutfitDataList outfitDataList;
    [SerializeField] private GameObject Shop_Log_Success;
    [SerializeField] private Button Shop_Log_Success_btn;
    [SerializeField] private GameObject Shop_Log_Fail;
    [SerializeField] private Button Shop_Log_Fail_btn;
    [SerializeField] private GameObject Shop_Log_Fail2;
    [SerializeField] private Button Shop_Log_Fail_btn2;

    private int Shop_characterNum;
    private string Shop_type;
    private string Shop_description;
    private string Shop_itemName;
    private string Shop_price;
    private string Shop_priceType;
    private bool Shop_isWorn;

    private Button Purchases_Button;
    private GameObject Purchase_MainPanel;
    private GameObject Purchase_Panel;
    private TMP_Text Purchase_textCost;
    private GameObject PurchaseCheckLast_Panel;
    private GameObject PurchaseCheckReal_Panel;
    private GameObject PurchaseCheckRealFail_Panel;
    private Button Purchase_BuySuccess;
    private Button Purchase_BuyFail;
    private GameObject Purchase_Log_Success;
    private Button Purchase_Log_Success_btn;
    private GameObject Purchase_Log_Fail;
    private Button Purchase_Log_Fail_btn;
    private GameObject Purchase_Log_Fail2;
    private Button Purchase_Log_Fail2_btn;
    private Button Purchases_Button1000;
    private Button Purchases_Button5000;
    private Button Purchases_Button10000;
    private Button Purchases_Button50000;
    private Button Purchases_Button100000;
    private Button Purchases_ButtonReset;
    
    private Callback<MicroTxnAuthorizationResponse_t> microTxnCallback;

    private static event Action<string> OnRoomMessage;
    [SerializeField] private GameObject RoomchatUI;
    [SerializeField] private TMP_Text RoomchatText;
    [SerializeField] private TMP_InputField RoomchatInputField;
    [SerializeField] private TMP_Text RoomInputFieldText;

    public BackgroundManager audioManager;
    public UISoundManager UisoundManager;
    public Texture2D cursorTexture;

    private float lastMessageTime = 0f;
    private int messageCount = 0;
    private float restrictionTime = 30f;
    private int maxMessagesPerSecond = 3;
    private bool messageenabled = true;
    
    
    void Start()
    {
        //Initialize();
        OnMessage += HandleNewMessage;
        //OnRoomMessage += HandleNewRoomMessage;
        microTxnCallback = Callback<MicroTxnAuthorizationResponse_t>.Create(OnMicroTxnAuthorizationResponse);
    }
    
    public void Initialize()
    {
        if(isClient && isLocalPlayer)
        {
            Clone_Datas CloneDatas_22 = FindObjectOfType<Clone_Datas>();
            if(CloneDatas_22 != null)
                Destroy(CloneDatas_22.gameObject);
            steamLobbyScript.enabled = false;
            chatUI = GameObject.Find("Chat");
            chatInputField = chatUI.transform.Find("Panel/InputField (TMP)").GetComponent<TMP_InputField>();
            chatText = chatUI.transform.Find("Panel/Scroll View/Viewport/Content/Text (TMP)").GetComponent<TMP_Text>();
            InputFieldText = chatUI.transform.Find("Panel/InputField (TMP)/Text Area/Text").GetComponent<TMP_Text>();
            LoadProfanities();
            chatInputField.onEndEdit.AddListener(Send);

            chatInputField.onValueChanged.AddListener(delegate { HandleInputFieldChange(); });

            RoomSetUI = GameObject.Find("Room_set");
            RoomnameText = RoomSetUI.transform.Find("Panel/InputField (room_name)").GetComponent<TMP_InputField>();
            RoompassText = RoomSetUI.transform.Find("Panel/InputField (room_pass)").GetComponent<TMP_InputField>();
            RoompnumText = RoomSetUI.transform.Find("Panel/Dropdown(room_num)").GetComponent<TMP_Dropdown>();
            RoomCreateButton = RoomSetUI.transform.Find("Panel/cre_btn_1").GetComponent<Button>();
            RoomCreateButton.onClick.AddListener(OnButtonSendRoom);

            Room_Cre_Panl = GameObject.Find("Room_Cre_Log");
            Room_Enter_Panl = GameObject.Find("Room_Enter_Log");
            Room_Enter_Error_Panl = GameObject.Find("Room_Enter_Error");
            Room_Enter_Withpass_Panl = GameObject.Find("Room_Enter_pass");
            Room_Enter_Withpass_Button = Room_Enter_Withpass_Panl.transform.Find("Panel/enter_btn_1").GetComponent<Button>();
            Room_Enter_Withpass_InputField = Room_Enter_Withpass_Panl.transform.Find("Panel/InputField (room_pass)").GetComponent<TMP_InputField>();
            Room_Enter_Error_Passwrong = GameObject.Find("Room_Enter_PassWrong");
            Room_Exit_Forced = GameObject.Find("Room_Exit_Forced");
            RoomSetUI.SetActive(false);
            Room_Cre_Panl.SetActive(false);
            Room_Enter_Panl.SetActive(false);
            Room_Enter_Error_Panl.SetActive(false);
            Room_Enter_Withpass_Panl.SetActive(false);
            Room_Enter_Error_Passwrong.SetActive(false);
            Room_Exit_Forced.SetActive(false);

            PurChaseLoad();

            if (chatText != null && messageBuffer.Count > 0)
            {
                chatText.text = string.Concat(messageBuffer);
                messageBuffer.Clear();
            }
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
                    //StartCoroutine(audioManager.PlayMusic());
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

            AddButtonListeners(GameObject.Find("Quit_set/Panel/cre_btn_1").GetComponent<Button>(), false, true, 3);
            AddButtonListeners(GameObject.Find("Quit_set/Panel/cre_btn_2").GetComponent<Button>(), false, true, 2);
            GameObject.Find("Quit_set").SetActive(false);

            AddButtonListeners(GameObject.Find("Set_set/Panel/cre_btn_1").GetComponent<Button>(), false, true, 3);
            AddButtonListeners(GameObject.Find("Set_set/Panel/cre_btn_2").GetComponent<Button>(), false, true, 2);
            GameObject.Find("Set_set").SetActive(false);
        }
    }
