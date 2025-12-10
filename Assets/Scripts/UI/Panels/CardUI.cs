using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class CardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Composants UI")]
    public Image imageCarte;
    public TMP_Text nomText; 
    public TMP_Text attaqueText;
    public TMP_Text defenseText;
    public TMP_Text nameCapacity;
    public TMP_Text descriptionCapacity;

    [Header("Icônes d'état")]
    public GameObject atk1Icon; // Icône première attaque
    public GameObject atk2Icon; // Icône deuxième attaque
    public GameObject passedIcon; // Icône "passé"
    public GameObject freezeIcon; // Icône "freeze"

    public RectTransform rectTransform;
    private Vector3 startPosition;
    public Vector3 offsetClick;

    public GameObject buttonAtk;
    public GameObject buttonPass;

    private LayoutElement layoutElement;
    public LayoutGroup layoutGroup;

    public bool isCardPlayer = false;
    public bool isCardOpponent = false;
    public bool isSelect = false;

    public string nameCard = "";

    [Header("Identification")]
    public string instanceId; // ID unique de la carte
    public string idCard;
    public int indexCarte; // Index dans la collection

    private void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        rectTransform = GetComponent<RectTransform>();
        layoutGroup = transform.parent?.GetComponent<LayoutGroup>();
    }

    public void setAttributesInitCard(CarteData data)
    {
        imageCarte.sprite = data.image;
        nameCard = data.nom;
        nomText?.SetText(data.nom);
        attaqueText?.SetText(data.attaque.ToString());
        defenseText?.SetText(data.defense.ToString());
        nameCapacity?.SetText(data.nameCapacity);
        descriptionCapacity?.SetText(data.descriptionCapacity);
        
        instanceId = data.instanceId;
        idCard = data.idCard.ToString();
        gameObject.name = $"CarteUI_{data.nom}_id{data.idCard}_inst{data.instanceId}";
        
        HideAllIcons();
    }

    public void HideAllIcons()
    {
        atk1Icon.SetActive(false);
        atk2Icon.SetActive(false);
        passedIcon.SetActive(false);
        freezeIcon.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayerActionManager.Instance.ClickOnBoardCard(this);
    }

    public void selectCard()
    {   
        if(isSelect)
        {
            Deselect(); 
            HideActionButtons();          
        }
        else
        {
            Select();
            ShowActionButtons();
        }
    }

    public void Select()
    {
        isSelect = true;
        GameManager.SetMode("hasCardSelectedToAction");

       // HorizontalLayoutGroup layoutGroup = transform.parent?.GetComponent<HorizontalLayoutGroup>();
       // layoutGroup.enabled = false;

        startPosition = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = startPosition + offsetClick;

       // if (layoutElement)
        //    layoutElement.ignoreLayout = true;
    }

    public void Deselect()
    {
        isSelect = false;
        GameManager.SetMode("selectCardToPlayAction");

       // HorizontalLayoutGroup layoutGroup = transform.parent?.GetComponent<HorizontalLayoutGroup>();
       // layoutGroup.enabled = false;

        startPosition = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = startPosition - offsetClick;

       // if (layoutElement)
        //    layoutElement.ignoreLayout = true;
    }

    private void ShowActionButtons()
    {
        buttonAtk?.SetActive(true);
        buttonPass?.SetActive(true);
    }
    
    private void HideActionButtons()
    {
        buttonAtk?.SetActive(false);
        buttonPass?.SetActive(false);
    }
}
