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

    public int attaqueValue;
    public int defenseValue;

    [Header("Icônes d'état")]
    public GameObject atk1Icon; // Icône première attaque
    public GameObject atk2Icon; // Icône deuxième attaque
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
    public bool isYellow = false;
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
    }

    private void Update()
    {
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
        HideActionButtons();

        stateOffensif = "passed";

        rectTransform.anchoredPosition = startPosition - offsetClick;

        actionChoiceDo = true;
        isSelect = false;          

        imageCarte.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        
        Debug.Log($"[CARD-UI] {nameCard} passe son tour (ATK:{attaqueValue}, DEF:{defenseValue})");
   
        /* 
        if (!coloredCards.Contains(this))
            coloredCards.Add(this);
        StartCoroutine(cardAnimations.ChangeColorSmoothly(imgCard, new Color(0.4f, 0.4f, 0.4f, 1f), 0.5f));
        
        if (layoutElement) layoutElement.ignoreLayout = true;
                
        if(nameCard == "Clorel")
        {
            int currentDef = GetDefenseValue(this);
            bonusDfs++;
            int newDef = currentDef + bonusDfs;
            carteUI?.defenseText?.SetText(newDef.ToString());
            SetDefenseValue(newDef);
            //PanelManager.instance?.AddLog($"{nameCard} : PASSER sélectionné (+1 défense)");
        }
        else if(nameCard == "Cassandre"){
            int index = carteUI.indexHierarchieOriginal;

            var (leftCard, rightCard) = BoardManager.Instance.GetAdjacentCards(index, 'player');

            //PanelManager.instance.AddLog($"Cassandre passe son tour");

            if(leftCard != null)
                ApplyAttackBonus(leftCard, leftCard.nameCard);
            if(rightCard != null)
                ApplyAttackBonus(rightCard, rightCard.nameCard);
        }
        else if(nameCard == "Désir"){

            //PanelManager.instance.AddLog("   → Sélection aléatoire Désir");

            var availableTargetsOpponent = CarteBoardInteraction.AllCardsInteractions
                .Where(c => c.isCardOpponent)
                .ToList();

            var availableTargetsPlayer = CarteBoardInteraction.AllCardsInteractions
                .Where(c => c.isCardPlayer)
                .ToList();

            if(availableTargetsOpponent.Count > 0)
            {
                int randomIndex = Random.Range(0, availableTargetsOpponent.Count);
                CarteBoardInteraction chosenTarget  = availableTargetsOpponent[randomIndex];
                chosenTarget.isFreeze = true;
                chosenTarget.freezeNumberLoop = GameManager.currentRound+1;

                //PanelManager.instance.AddLog($"   → Cible aléatoire opponent sélectionnée : {chosenTarget.nameCard}");
            }
            else if(availableTargetsPlayer.Count > 0){

                int randomIndex = Random.Range(0, availableTargetsPlayer.Count);
                CarteBoardInteraction chosenTarget  = availableTargetsPlayer[randomIndex];
                chosenTarget.isFreeze = true;
                chosenTarget.freezeNumberLoop = GameManager.currentRound+1;

                //PanelManager.instance.AddLog($"   → Cible aléatoire player sélectionnée : {chosenTarget.nameCard}");
            }
            else
            {
                //PanelManager.instance.AddLog("   → Aucune cible adverse disponible");
            }
        }
        else if(nameCard == "Neo")
        {
            UnsetAttackBonus(this, nameCard);
            lastTarget = "";
        }
        else if(nameCard == "Ambroise")
        {
            // Marquer qu'Ambroise veut appliquer son effet plus tard
            GameManager.ambroiseEffectPending = true;
            //PanelManager.instance?.AddLog($"{nameCard} : Onde de Choc Passive en attente.");
        }
        else if(nameCard == "Trahison")
        {
            // Marquer que Trahison veut appliquer son effet plus tard
            GameManager.trahisonEffectPending = true;
            //PanelManager.instance?.AddLog($"{nameCard} : Terreur Sélective en attente.");
        }
        else if(nameCard == "Belindra")
        {
            //PanelManager.instance?.AddLog($"{nameCard} : Belindra active Bouclier collectif.");
        }
        else if(nameCard == "Zao")
        {
            //PanelManager.instance?.AddLog($"{nameCard} : Zao passe son tour. Elle est intouchable.");
        }
        
        CheckEndOfTurn();
        */
    }

    public void OnAttack()
    {
        HideActionButtons();
        atk?.SetActive(true);

        stateOffensif = "selectTarget";
        GameManager.Instance.numberOfAttacksUsedPlayer++;
        
        Debug.Log($"[CARD-UI] {nameCard} passe en mode ATTAQUE (ATK:{attaqueValue}, DEF:{defenseValue})");
        Debug.Log($"[CARD-UI] Attaques utilisées joueur: {GameManager.Instance.numberOfAttacksUsedPlayer}/{GameManager.MAX_NUMBER_ATK_ROUND}");

        /*
        if (layoutElement) layoutElement.ignoreLayout = true;               
        
        var availableTargets = CarteBoardInteraction.AllCardsInteractions
            .Where(c => c.isCardOpponent && c.stateDefensif != "isAttacked")
            .ToList();

        if(nameCard == "Tyroine")
        {
            //PanelManager.instance.AddLog("   → Sélection aléatoire");

            if(availableTargets.Count > 0)
            {
                int randomIndex = Random.Range(0, availableTargets.Count);
                CarteBoardInteraction chosenTarget = availableTargets[randomIndex];
                chosenTarget.SelectTarget();

                //PanelManager.instance.AddLog($"   → Cible aléatoire sélectionnée Par Tyroine : {chosenTarget.nameCard}");
            }
            else
            {
                //PanelManager.instance.AddLog("   → Aucune cible adverse disponible");
            }
        }
        else if(nameCard == "Ondine"){

            //PanelManager.instance.AddLog("   → Sélection aléatoire des cibles");

            if(availableTargets.Count > 0)
            {
                // Déterminer combien de cibles on va prendre : 1 à 2 mais pas plus que le nombre disponible
                int numberOfTargets = Mathf.Min(Random.Range(1, 3), availableTargets.Count);

                // Mélanger la liste et prendre les 'numberOfTargets' premières
                var shuffledTargets = availableTargets.OrderBy(x => Random.value).Take(numberOfTargets).ToList();

                //PanelManager.instance.AddLog($"   → Nombre de cibles sélectionnées : {numberOfTargets}");

                List<int> damages;
                switch(numberOfTargets)
                {
                    case 1:
                        damages = new List<int> { 3 };
                        break;
                    case 2:
                        damages = new List<int> { 1, 2 }.OrderBy(x => Random.value).ToList(); // aléatoire qui prend 1 et qui prend 2
                        break;
                    case 3:
                    default:
                        damages = new List<int> { 1, 1, 1 };
                        break;
                }

                for(int i = 0; i < shuffledTargets.Count; i++)
                {
                    var target = shuffledTargets[i];
                    int dmg = damages[i];

                    target.isCibledCount++;
                    target.cardUI?.ShowAttackIcon(target.isCibledCount);
                    target.stateDefensif = "isAttacked";
                    
                    string nameAttacker = this.nameCard ?? "Ondine";
                    string nameTarget = target.nameCard ?? "Cible";
                    
                    //PanelManager.instance?.AddLog($"{nameAttacker} : ATK : {dmg}");
                    //PanelManager.instance?.AddLog($"{nameTarget} : DEF : {target.GetDefenseValue(target)}");
                    //PanelManager.instance.AddLog($"   → {target.nameCard} prend {dmg} de dégâts (sera appliqué en fin de tour)");
                    
                    BoardManager.Instance.roundDamage.Add($"{nameAttacker} → {nameTarget} (DEF:{target.GetDefenseValue(target)}) = {dmg} dégâts");
                    attaquesDuTour.Add(new AttaqueInfo(this, target, dmg));
                    
                    // Marquer la carte comme ayant fait son choix
                    this.choiceDo = true;
                    this.stateOffensif = "atk";
                }
            }
        }
        CheckEndOfTurn();
        */
    }

    public void SetDataTarget(CardAI cardAI)
    {   
        target = cardAI.nameCard;
        targetID = cardAI.idCard;
        actionChoiceDo = true;
        isSelect = false;    
        stateOffensif = "atk";      
        
        Debug.Log($"[CARD-UI] {nameCard} cible {cardAI.nameCard} (ATK:{attaqueValue} vs DEF:{cardAI.defenseValue})");
        int damage = attaqueValue - cardAI.defenseValue;
        if (damage < 0) damage = 1;
        Debug.Log($"[CARD-UI] Dégâts potentiels: {damage} (sera appliqué à la fin du tour)");
    }

    public bool HasCapacity(IAAction.Capacity cap)
    {
        if (nameCapacity == null) return false;

        return nameCapacity.text.Contains(cap.ToString());
    }

    public bool IsAdjacentTo(CardUI a, CardUI b)
    {
        CardUI cardUIA = a.GetComponent<CardUI>();
        CardUI cardUIB = b.GetComponent<CardUI>();
        if (cardUIA == null || cardUIB == null) return false;
        return Mathf.Abs(cardUIA.indexCarte - cardUIB.indexCarte) == 1;
    }
}
