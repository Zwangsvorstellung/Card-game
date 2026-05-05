using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class CardUI : MonoBehaviour, IPointerClickHandler, ICard
{
    [Header("Composants UI")]
    public Image imageCarte;
    public TMP_Text nomText; 
    public TMP_Text attaqueText;
    public TMP_Text defenseText;
    public TMP_Text nameCapacity;
    public TMP_Text descriptionCapacity;

    public int attaqueValue;
    public int defenseValue;

    [Header("Icônes d'état")]
    public GameObject atk1Icon; // Icône première attaque
    public GameObject passedIcon; // Icône "passé"
    public GameObject freezeIcon; // Icône "freeze"

    [Header("Position")]
    public RectTransform rectTransform;
    public Vector2 startPosition;
    public Vector2 positionWithOffset;
    public Vector2 offsetClick;

    public GameObject atk;
    public GameObject passed;
    public GameObject buttonAtk;
    public GameObject buttonPass;

    private LayoutElement layoutElement;
    public LayoutGroup layoutGroup;

    [Header("Caractéristiques")]
    public bool isCardPlayer = true;
    public bool isSelect = false;
    public bool isFrozen = false;
    public int freezeAtTurn = 0;
    public bool isYellow = false;
    public bool isHiddenSlot = false;
    public bool actionChoiceDo = false;
    public string stateOffensif;
    public string stateDefensif;
    public string target;
    public int targetID;
    public string lastTarget;
    public int lastTargetID;

    [Header("Identification")]
    public string instanceId; // ID unique de la carte
    public int idCard;
    public string nameCard;
    public int indexCarte; // Index dans la collection
    public int indexHierarchieOriginal;

    private void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        rectTransform = GetComponent<RectTransform>();
        layoutGroup = transform.parent?.GetComponent<LayoutGroup>();
        stateDefensif = "notCibled";
        stateOffensif = "wait";
    }
    private void Start()
    {
        startPosition = rectTransform.anchoredPosition;
        positionWithOffset = rectTransform.anchoredPosition + offsetClick;
        indexHierarchieOriginal = transform.GetSiblingIndex();
    }
    private void Update()
    {
        if (isHiddenSlot) return;

        if(stateOffensif == "passed"){
            passedIcon.SetActive(true);
            RectTransform passedRect = passedIcon.GetComponent<RectTransform>();
            StartCoroutine(CardsAnimation.SwingSablier(passedRect));
        }
        else{
            passedIcon.SetActive(false);
        }

        if(stateDefensif == "cibled"){
            atk1Icon.SetActive(true);
        }else{
            atk1Icon.SetActive(false);
        }

        if(isFrozen){
            freezeIcon.SetActive(true);
        }else{
           freezeIcon.SetActive(false);
        }

        if(isYellow){
            imageCarte.color = new Color(1f, 0.95f, 0.4f, 1f);
        }
    }

    void OnEnable() => BoardManager.cardsOnBoardUI.Add(this);
    void OnDisable() => BoardManager.cardsOnBoardUI.Remove(this);

    public void ResetCardEndTurn(){
        if (isHiddenSlot) return;

        actionChoiceDo = false;
        stateOffensif = "wait";
        stateDefensif = "notCibled";

        if(freezeAtTurn != GameManager.Instance.round)
        {
            isFrozen = false;
        }
        
        freezeAtTurn = 0;
        lastTarget = target;
        target = "";
        targetID = 0;

        imageCarte.color = Color.white;
        
        HideAllIcons();
        
        rectTransform.anchoredPosition = startPosition;
        layoutGroup.enabled = true;

        atk?.SetActive(false);
    }


    public void setAttributesInitCardPlayer(CarteData data)
    {
        imageCarte.sprite = data.image;
        nameCard = data.nom;
        nomText?.SetText(data.nom);
        attaqueText?.SetText(data.attaque.ToString());
        defenseText?.SetText(data.defense.ToString());
        nameCapacity?.SetText(data.nameCapacity);
        descriptionCapacity?.SetText(data.descriptionCapacity);

        attaqueValue = data.attaque;
        defenseValue = data.defense;
        
        instanceId = data.instanceId;
        idCard = data.idCard;
        gameObject.name = $"CardUI_{data.nom}_id{data.idCard}_inst{data.instanceId}";
        
        HideAllIcons();
    }

    public void HideAllIcons()
    {
        atk1Icon.SetActive(false);
        passedIcon.SetActive(false);
        freezeIcon.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(isHiddenSlot) return;
        if(isFrozen) return;

        if(GameManager.Instance.currentPlayerAction == "UI"){
            if(!actionChoiceDo)
                PlayerActionManager.Instance.ClickOnBoardCard(this);
        }
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
            BoardManager.Instance.DeselectAllOtherCards();
            Select();
            ShowActionButtons();
        }
    }
    public void Select()
    {
        isSelect = true;
        GameManager.Instance.mode = "hasCardSelectedToAction";
        stateOffensif = "waitOrder";
        rectTransform.anchoredPosition = positionWithOffset;

        if (layoutGroup?.enabled == true)
            layoutGroup.enabled = false;
        
        if (layoutElement)
            layoutElement.ignoreLayout = true;
    }
    public void Deselect()
    {
        isSelect = false;
        GameManager.Instance.mode = "selectCardToPlayAction";
        stateOffensif = "wait";

        if (layoutGroup?.enabled == true)
            layoutGroup.enabled = false;

        rectTransform.anchoredPosition = startPosition;

        if (layoutElement)
            layoutElement.ignoreLayout = true;
    }

    public void ShowActionButtons()
    {
        if(GameManager.Instance.numberOfAttacksUsedPlayer < 2)
            buttonAtk?.SetActive(true);

        buttonPass?.SetActive(true);
    }
    public void HideActionButtons()
    {
        buttonAtk?.SetActive(false);
        buttonPass?.SetActive(false);
    }

    public void OnPassed()
    {
        if (isHiddenSlot) return;
        HideActionButtons();

        stateOffensif = "passed";
        rectTransform.anchoredPosition = startPosition - offsetClick;
        actionChoiceDo = true;
        isSelect = false;
        imageCarte.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        //Debug.Log($"[CARD-UI] {nameCard} passe son tour (ATK:{attaqueValue}, DEF:{defenseValue})");
        //if (layoutElement) layoutElement.ignoreLayout = true;
    }

    public void OnAttack()
    {
        if (isHiddenSlot) return;
        HideActionButtons();
        atk?.SetActive(true);

        stateOffensif = "selectTarget";
        GameManager.Instance.numberOfAttacksUsedPlayer++;
        
        //Debug.Log($"[CARD-UI] {nameCard} passe en mode ATTAQUE (ATK:{attaqueValue}, DEF:{defenseValue})");
        Debug.Log($"[CARD-UI] Attaques utilisées joueur: {GameManager.Instance.numberOfAttacksUsedPlayer}/{GameManager.MAX_NUMBER_ATK_ROUND}");

        // si Tyroine -> choisir aléatoire qui n'est pas déjà ciblée
        if(nameCard == "Tyroine"){
            List<CardAI> targetsAI = BoardManager.cardsOnBoardAI.Where(c => c.stateDefensif != "cibled").ToList();
            if(targetsAI.Count > 0){
                int randomTarget = Random.Range(0, targets.Count);
                CardAI randomTargetCard = targets[randomTarget];
                PlayerActionManager.Instance.ClickSelectTargetOnBoard(randomTargetCard);
            }
            // TO DO - cas ou pas de carte possible
        }
        // if (layoutElement) layoutElement.ignoreLayout = true;               
    }

    public void SetDataTarget(CardAI cardAI)
    {   
        target = cardAI.nameCard;
        targetID = cardAI.idCard;
        actionChoiceDo = true;
        isSelect = false;    
        stateOffensif = "atk";
        
        //Debug.Log($"[CARD-UI] {nameCard} cible {cardAI.nameCard} (ATK:{attaqueValue} vs DEF:{cardAI.defenseValue})");
        int damage = attaqueValue - cardAI.defenseValue;
        if (damage < 0) damage = 1;
        //Debug.Log($"[CARD-UI] Dégâts potentiels: {damage} (sera appliqué à la fin du tour)");
    }

    public bool HasCapacity(IAAction.Capacity cap)
    {
        if (nameCapacity == null) return false;
        return nameCapacity.text.Contains(cap.ToString());
    }
    public bool IsAdjacentTo(CardUI other)
    {
        if (other == null) return false;
        return Mathf.Abs(indexCarte - other.indexCarte) == 1;
    }
    public void HideAsEmptySlot()
    {
        isHiddenSlot = true;
        actionChoiceDo = true;
        stateOffensif = "hidden";
        stateDefensif = "hidden";
        isSelect = false;
        isFrozen = false;
        isYellow = false;
        atk?.SetActive(false);
        HideAllIcons();

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);
    }
    public (CardUI left, CardUI right) GetAdjacentCards(CardUI card)
    {
        CardUI left = null;
        CardUI right = null;

        int index = card.indexCarte;
        var list = BoardManager.cardsOnBoardUI;

        if (index > 0)
            left = list[index - 1];

        if (index < list.Count - 1)
            right = list[index + 1];

        return (left, right);
    }

    public string INameCard => nameCard;

    string ICard.nameCard => nameCard;

    int ICard.defenseValue
    {
        get => defenseValue;
        set => defenseValue = value;
    }
    int ICard.attaqueValue
    {
        get => attaqueValue;
        set => attaqueValue = value;
    }

    TMP_Text ICard.defenseText => defenseText;
    TMP_Text ICard.attaqueText => attaqueText;

    string ICard.stateOffensif => stateOffensif;
    string ICard.stateDefensif => stateDefensif;
    bool ICard.isCardPlayer
    {
        get => isCardPlayer;
        set => isCardPlayer = value;
    }
}
