using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class CardUI : MonoBehaviour, IPointerClickHandler, ICard
{
    public static List<CardName> CardName = new List<CardName>();

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
    public GameObject atk1Icon;
    public GameObject passedIcon;
    public GameObject freezeIcon;

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
    public OffensiveState stateOffensif;
    public DefensiveState stateDefensif;
    public string target;
    public int targetID;
    public string lastTarget;

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
        stateDefensif = DefensiveState.NOT_CIBLED;
        stateOffensif = OffensiveState.WAIT;
    }
    private void Start()
    {
        startPosition = rectTransform.anchoredPosition;
        positionWithOffset = rectTransform.anchoredPosition + offsetClick;
        indexHierarchieOriginal = transform.GetSiblingIndex();
        indexCarte = transform.GetSiblingIndex();
    }
    private void Update()
    {
        if (isHiddenSlot) return;

        if(stateOffensif == OffensiveState.PASSED){
            passedIcon.SetActive(true);
            RectTransform passedRect = passedIcon.GetComponent<RectTransform>();
            StartCoroutine(CardsAnimation.SwingSablier(passedRect));
        }
        else{
            passedIcon.SetActive(false);
        }

        if(stateDefensif == DefensiveState.CIBLED){
            atk1Icon.SetActive(true);
        }else{
            atk1Icon.SetActive(false);
        }

        if (freezeIcon.activeSelf != isFrozen)
            freezeIcon.SetActive(isFrozen);

        if(isYellow){
            imageCarte.color = new Color(1f, 0.95f, 0.4f, 1f);
        }
    }

    void OnDisable() => BoardManager.cardsOnBoardUI.Remove(this);
    private void OnDestroy() => BoardManager.cardsOnBoardUI.Remove(this);

    public void ResetCardEndTurn(){
        if (isHiddenSlot && isYellow) return;

        actionChoiceDo = false;
        stateOffensif = OffensiveState.WAIT;
        stateDefensif = DefensiveState.NOT_CIBLED;

        if(freezeAtTurn != GameManager.Instance.round)
        {
            isFrozen = false;
        }
        
        freezeAtTurn = 0;
        lastTarget = target;
        target = "";
        targetID = 0;
        imageCarte.color = Color.white;
        rectTransform.anchoredPosition = startPosition;
        layoutGroup.enabled = true;
        
        HideAllIcons();
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

        if (!BoardManager.cardsOnBoardUI.Contains(this))
        {
            BoardManager.cardsOnBoardUI.Add(this);
        }
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

        if(GameManager.Instance.currentPlayerAction == PlayerActionState.UI){
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
        GameManager.Instance.mode = GameMode.HAS_CARD_SELECTED_TO_ACTION;
        stateOffensif = OffensiveState.WAIT_ORDER;
        rectTransform.anchoredPosition = positionWithOffset;

        if (layoutGroup?.enabled == true)
            layoutGroup.enabled = false;
        
        if (layoutElement)
            layoutElement.ignoreLayout = true;
    }
    public void Deselect()
    {
        isSelect = false;
        GameManager.Instance.mode = GameMode.SELECT_CARD_TO_PLAY_ACTION;
        stateOffensif = OffensiveState.WAIT;
        rectTransform.anchoredPosition = startPosition;

        if (layoutGroup?.enabled == true)
            layoutGroup.enabled = false;

        if (layoutElement)
            layoutElement.ignoreLayout = true;
    }

    public void ShowActionButtons()
    {
        buttonAtk?.SetActive(GameManager.Instance.numberOfAttacksUsedPlayer < 2);
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

        stateOffensif = OffensiveState.PASSED;
        rectTransform.anchoredPosition = startPosition - offsetClick;
        actionChoiceDo = true;
        BoardManager.Instance.CheckPlayerCardsDone();
        isSelect = false;
        imageCarte.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        if (layoutElement) layoutElement.ignoreLayout = true;
    }

    public void OnAttack()
    {
        if (isHiddenSlot) return;
        HideActionButtons();
        atk?.SetActive(true);

        stateOffensif = OffensiveState.SELECT_TARGET;
        GameManager.Instance.numberOfAttacksUsedPlayer++;
        
        // si Tyroine -> choisir aléatoire qui n'est pas déjà ciblée
        if(nameCard == CardName.TYROINE){
            List<CardAI> targetsAI = BoardManager.cardsOnBoardAI.Where(c => c.stateDefensif != DefensiveState.CIBLED && c.isHiddenSlot).ToList();
            if(targetsAI.Count > 0){
                int randomTarget = Random.Range(0, targetsAI.Count);
                CardAI randomTargetCard = targetsAI[randomTarget];
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
        stateOffensif = OffensiveState.ATK;
        //Debug.Log($"[CARD-UI] {nameCard} cible {cardAI.nameCard} (ATK:{attaqueValue} vs DEF:{cardAI.defenseValue})");
        int damage = Mathf.Max(1, attaqueValue - cardAI.defenseValue);
    }

    public bool HasCapacity(Capacity cap)
    {
        return nameCapacity.text.Contains(cap.ToString());
    }
    public bool IsAdjacentTo(CardUI other)
    {
        return Mathf.Abs(indexCarte - other.indexCarte) == 1;
    }
    public void HideAsEmptySlot()
    {
        isHiddenSlot = true;
        actionChoiceDo = false;
        stateOffensif = OffensiveState.HIDDEN;
        stateDefensif = DefensiveState.HIDDEN;
        isSelect = false;
        isFrozen = false;
        isYellow = false;
        atk?.SetActive(false);
        HideAllIcons();

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);
    }
    public (ICard left, ICard right) GetAdjacentCards(ICard card)
    {
        var list = BoardManager.cardsOnBoardUI
            .Where(c => !c.isHiddenSlot)
            .ToList();

        int index = list.FindIndex(c => c.idCard == card.idCard);
        Debug.Log($"[ADJ FIX] {card.nameCard} id={card.idCard} index={index}");

        if (index == -1)
        {
            Debug.LogWarning($"[ADJ ERROR] Card not found in list: {card.nameCard}");
            return (null, null);
        }

        ICard left = (index > 0) ? list[index - 1] : null;
        ICard right = (index < list.Count - 1) ? list[index + 1] : null;
        return (left, right);
    }

    public string INameCard => nameCard;
    string ICard.nameCard => nameCard;

    int ICard.idCard
    {
        get => idCard;
        set => idCard = value;
    }

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

    OffensiveState ICard.stateOffensif => stateOffensif;
    DefensiveState ICard.stateDefensif => stateDefensif;
    bool ICard.isHiddenSlot => isHiddenSlot;

    bool ICard.isCardPlayer
    {
        get => isCardPlayer;
        set => isCardPlayer = value;
    }
}
