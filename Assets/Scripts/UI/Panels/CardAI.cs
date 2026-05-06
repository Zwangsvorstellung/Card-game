using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class CardAI : MonoBehaviour, IPointerClickHandler, ICard
{
    [Header("Composants UI")]
    public Image imageCarte;
    public TMP_Text nomText; 
    public TMP_Text attaqueText;
    public TMP_Text defenseText;
    public TMP_Text nameCapacity;
    public TMP_Text descriptionCapacity;

    public CardsAnimation cardsAnimation;

    public int attaqueValue;
    public int defenseValue;

    [Header("Icônes d'état")]
    public GameObject atk1Icon; // Icône première attaque
    public GameObject passedIcon; // Icône "passé"
    public GameObject freezeIcon; // Icône "freeze"

    public RectTransform rectTransform;
    public Vector3 startPosition;
    public Vector3 offsetClick;

    public GameObject atk;

    //private LayoutElement layoutElement;
    public LayoutGroup layoutGroup;

    public bool isCardOpponent = true;
    public bool isCardPlayer = false;
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

    public string nameAttacker;
    public int idAttacker;

    [Header("Identification")]
    public string nameCard;
    public string instanceId; // ID unique de la carte
    public int idCard;
    public int indexCarte; // Index dans la collection
    public int indexHierarchieOriginal;

    void Awake()
    {
        stateDefensif = "notCibled";
        stateOffensif = "wait";
        cardsAnimation = GetComponent<CardsAnimation>();
    }

    private void Start()
    {
        startPosition = rectTransform.anchoredPosition;
        indexHierarchieOriginal = transform.GetSiblingIndex();
        indexCarte = transform.GetSiblingIndex();
    }

    void Update()
    {
        if (isHiddenSlot) return;

        /// CHECK
        UpdatePassedState();
        UpdateFreezeState();
        UpdateYellowState();

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

    bool isPassedAnimating;
    void UpdatePassedState()
    {
        bool isPassed = stateOffensif == "passed";

        if (passedIcon.activeSelf != isPassed)
            passedIcon.SetActive(isPassed);

        if (isPassed && !isPassedAnimating)
        {
            isPassedAnimating = true;
            StartCoroutine(PassedAnimation());
        }
    }

    IEnumerator PassedAnimation()
    {
        RectTransform rect = passedIcon.GetComponent<RectTransform>();
        yield return CardsAnimation.SwingSablier(rect);
        isPassedAnimating = false;
    }

    void UpdateDefensiveState()
    {
        atk1Icon.SetActive(stateDefensif == "cibled");
    }
    void UpdateFreezeState()
    {
        freezeIcon.SetActive(isFrozen);
    }
    bool wasYellow;
    void UpdateYellowState()
    {
        if (isYellow && !wasYellow)
        {
            imageCarte.color = new Color(1f, 0.95f, 0.4f, 1f);
            wasYellow = true;
        }
        else if (!isYellow && wasYellow)
        {
            imageCarte.color = Color.white;
            wasYellow = false;
        }
    }


    void OnEnable() => BoardManager.cardsOnBoardAI.Add(this);
    void OnDisable() => BoardManager.cardsOnBoardAI.Remove(this);

    public void setAttributesInitCardAI(CarteData data)
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
        gameObject.name = $"CarteAI_{data.nom}_id{data.idCard}_inst{data.instanceId}";   
        
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
        PlayerActionManager.Instance.ClickSelectTargetOnBoard(this);
    }

    public int isSelectCard()
    {   
        if(stateDefensif != "cibled"){
            StartCoroutine(cardsAnimation.ColorFlash(this, Color.red, 0.5f));
            StartCoroutine(cardsAnimation.Shake(0.3f, 5f));

            imageCarte.color = new Color(0.5f, 0.7f, 1f, 1f);
            stateDefensif = "cibled";
            CardUI cardUI = BoardManager.Instance.GetDataAttacker();
            nameAttacker = cardUI.nameCard;
            idAttacker = cardUI.idCard;

            return idAttacker;
        }
        return 0;
    }

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

        atk?.SetActive(false);
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

    public bool HasCapacity(IAAction.Capacity cap)
    {
        if (nameCapacity == null) return false;
        return nameCapacity.text.Contains(cap.ToString());
    }

    public bool IsAdjacentTo(CardAI other)
    {
        return Mathf.Abs(indexCarte - other.indexCarte) == 1;
    }

    public (CardAI left, CardAI right) GetAdjacentCards(CardAI card)
    {
        CardAI left = null;
        CardAI right = null;

        int index = card.indexCarte;
        var list = BoardManager.cardsOnBoardAI;

        if (index > 0)
            left = list[index - 1];

        if (index < list.Count - 1)
            right = list[index + 1];

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

    string ICard.stateOffensif => stateOffensif;
    string ICard.stateDefensif => stateDefensif;
    bool ICard.isHiddenSlot => isHiddenSlot;

    bool ICard.isCardPlayer
    {
        get => isCardPlayer;
        set => isCardPlayer = value;
    }
}
