using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;

[System.Serializable]
public class CharInfoCharacterData
{
    public string family;
    public string name;
    public string explanation;
    public string attackPower;
    public string attackSpeed;
    public string attackRange;

    public Sprite skill1;
    public Sprite skill2;
    public Sprite skill3;
    public Sprite skill4;

    public string skill1name;
    public string skill1description;
    public string skill2name;
    public string skill2description;
    public string skill3name;
    public string skill3description;
    public string skill4name;
    public string skill4description;

    public AudioClip CharacterAudio;
}

public class CharBase : MonoBehaviour
{
    public GameObject[] cha_imgs;
    public Button[] cha_buttons;
    public CharInfoCharacterData[] characterDataArray;

    public TMP_Text ChaFam;
    public TMP_Text ChaName;
    public TMP_Text ChaExplan;
    public TMP_Text ChaAPower;
    public TMP_Text ChaASpeed;
    public TMP_Text ChaARange;

    public Image Skill_1;
    public Image Skill_2;
    public Image Skill_3;
    public Image Skill_4;

    public TMP_Text Main_cashpoint;
    public TMP_Text Main_basepoint;

    public int Index_Send;
    public GameObject tooltipPanel;
    public Image tooltipImage;
    public TextMeshProUGUI tooltipTitleText;
    public TextMeshProUGUI tooltipDescriptionText;
    public int? currentlySelectedSkillIndex = null;
    
    void Start()
    {
        int mainCharacterId;

        if (int.TryParse(ClientDataManager.Instance.UserDetails.MainCharacterID, out mainCharacterId))
        {
            int imageIndex = mainCharacterId-1;
            
            ActiveImags(imageIndex);
        }
        else
        {
            // Handle the case where the string cannot be converted to an integer
            Debug.LogError("MainCharacterID is not a valid integer");
        }
        Main_cashpoint.text = $"{ClientDataManager.Instance.UserDetails.cashpoint.ToString("N0")}";
        Main_basepoint.text = $"{ClientDataManager.Instance.UserDetails.basepoint.ToString("N0")}";

        Image[] skillImages = { Skill_1, Skill_2, Skill_3, Skill_4 };
        for (int i = 0; i < skillImages.Length; i++)
        {
            EventTrigger trigger = skillImages[i].gameObject.GetComponent<EventTrigger>() ?? skillImages[i].gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            int skillIndex = i;
            entryEnter.callback.AddListener((data) => { ShowSkillTooltip(skillIndex); });
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { HideSkillTooltip(); });
            trigger.triggers.Add(entryExit);
        }

        tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (tooltipPanel.transform.parent as RectTransform), // Assuming the tooltipPanel is placed within a Canvas
                Input.mousePosition, 
                null, // Assuming the Canvas is Screen Space - Overlay
                out localPoint
            );

            tooltipPanel.transform.localPosition = localPoint;
        }
        if (tooltipPanel.activeSelf && currentlySelectedSkillIndex.HasValue)
        {
            ShowSkillTooltip(currentlySelectedSkillIndex.Value);
        }
    }

    void ShowSkillTooltip(int itemC)
    {
        currentlySelectedSkillIndex = itemC;
        string skillName = "";
        Sprite skillSprite = null;
        string skillDescription = "";

        switch (itemC)
        {
            case 0:
                skillName = characterDataArray[Index_Send].skill1name;
                skillSprite = characterDataArray[Index_Send].skill1;
                skillDescription = characterDataArray[Index_Send].skill1description;
                break;
            case 1:
                skillName = characterDataArray[Index_Send].skill2name;
                skillSprite = characterDataArray[Index_Send].skill2;
                skillDescription = characterDataArray[Index_Send].skill2description;
                break;
            case 2:
                skillName = characterDataArray[Index_Send].skill3name;
                skillSprite = characterDataArray[Index_Send].skill3;
                skillDescription = characterDataArray[Index_Send].skill3description;
                break;
            case 3:
                skillName = characterDataArray[Index_Send].skill4name;
                skillSprite = characterDataArray[Index_Send].skill4;
                skillDescription = characterDataArray[Index_Send].skill4description;
                break;
            default:
                Debug.LogError("Invalid skill index");
                return;
        }

        tooltipTitleText.text = skillName;
        tooltipImage.sprite = skillSprite;
        tooltipDescriptionText.text = skillDescription;

        tooltipPanel.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel.GetComponent<RectTransform>());
    }
    void HideSkillTooltip()
    {
        tooltipPanel.SetActive(false);
        currentlySelectedSkillIndex = null;
    }

    public void DeactiveImage()
    {
        foreach(GameObject cha_obj in cha_imgs)
        {
            cha_obj.SetActive(false);
        }
        foreach (Button btn in cha_buttons)
        {
            Transform selection = btn.transform.Find("Selection");
            if (selection != null)
            {
                selection.gameObject.SetActive(false);
            }
        }
    }
    public void ActiveImags(int index)
    {
        DeactiveImage();
        if(index >= 0 && index < cha_imgs.Length)
        {
            cha_imgs[index].SetActive(true);
            Index_Send = index;

            Transform selection = cha_buttons[index].transform.Find("Selection");
            if (selection != null)
            {
                selection.gameObject.SetActive(true);
            }
            SetCharacterData(index);
        }
    }
    private void SetCharacterData(int index)
    {
        if (index >= 0 && index < characterDataArray.Length)
        {
            CharInfoCharacterData data = characterDataArray[index];
            ChaFam.text = data.family;
            if (data.family == "Dark")
            {
                ChaFam.color = new Color32(203, 120, 207, 255); // #CB78CF
            }
            else if (data.family == "Light")
            {
                ChaFam.color = new Color32(255, 240, 146, 255); // #FFF092
            }
            ChaName.text = data.name;
            ChaExplan.text = data.explanation;
            ChaAPower.text = data.attackPower;
            ChaASpeed.text = data.attackSpeed;
            ChaARange.text = data.attackRange;

            Skill_1.sprite = data.skill1;
            Skill_2.sprite = data.skill2;
            Skill_3.sprite = data.skill3;
            Skill_4.sprite = data.skill4;

            InGameSoundManager.Instance?.CharCharacterSound(data.CharacterAudio);
        }
        else
        {
            Debug.LogError("Invalid character index");
        }
    }

    private void SetButtonColor(Button btn, Color color)
    {
        TMP_Text buttonText = btn.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.color = color;
        }
    }
}
