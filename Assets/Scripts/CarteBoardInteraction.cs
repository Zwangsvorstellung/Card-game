using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class CarteBoardInteraction : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public List<IAAction.Capacity> capacites; // la liste des capacités de la carte

    [SerializeField] public bool isCardPlayer = false;
    [SerializeField] public bool isCardOpponent = false;
    [SerializeField] public bool isSelected = false;
    [SerializeField] public int isCibledCount = 0; 
    [SerializeField] public string stateOffensif = "";
    [SerializeField] public string stateDefensif = "";  
    [SerializeField] public string lastTarget = "";
    [SerializeField] public string currentTarget = "";  
    [SerializeField] public int bonusAtk;  
    [SerializeField] public int bonusDfs;  
    [SerializeField] public int malusAtk;  
    [SerializeField] public int malusDfs; 

    [SerializeField] public bool isFreeze; 
    [SerializeField] public int freezeNumberLoop = 0;   
    [SerializeField] public bool resetBonusAtk = true;  
    [SerializeField] public string nameCard;  

    [SerializeField] public int defenseValueMax; 
    [SerializeField] public int attackValueMax;
    [SerializeField] public int defenseValueCurrent; 
    [SerializeField] public int attackValueCurrent;

    private Coroutine currentMoveCoroutine;
    public static readonly List<CarteBoardInteraction> AllCardsInteractions = new();

    public bool choiceDo = false;
    public CardUI cardUI;
    private LayoutElement layoutElement;
    public LayoutGroup layoutGroup;
    private CardAnimations cardAnimations;
    private Image img;
    public GameObject freezeIcon; // Icône "freeze"


    public Vector3 startPosition; 
    public Vector3 newPosition;
    public RectTransform rectTransform;
    private bool ignorePointer  = false;

    public bool yellowCard = false; 
    private static CarteBoardInteraction attackingCard = null; 
    public static int numberOfAttacksMax = 2;
  
    private Vector3 targetHoverOffset = new Vector3(0, -50, 0);
    private TMP_FontAsset poppinsRegular;
    private TMP_FontAsset poppinsBold;
    public GameObject buttonAtk;
    public GameObject buttonPass;
    private static Color colorAtk1 = new Color(0.8f, 0.8f, 1f, 1f);
    private static Color colorAtk2 = new Color(1f, 0.8f, 0.8f, 1f);
    private static List<CarteBoardInteraction> coloredCards = new List<CarteBoardInteraction>();
    private static List<CarteBoardInteraction> targetCards = new List<CarteBoardInteraction>();
    private struct AttaqueInfo
    {
        public CarteBoardInteraction attacker;
        public CarteBoardInteraction target;
        public int damage;
        public AttaqueInfo(CarteBoardInteraction attacker, CarteBoardInteraction target, int damage)
        {
            this.attacker = attacker;
            this.target = target;
            this.damage = damage;
        }
    }
    private static List<AttaqueInfo> attaquesDuTour = new List<AttaqueInfo>();
  
    public static bool isAITurn = false;

    public CarteBoardInteraction CurrentTarget;

    public bool WillAttackThisTurn = false;

    private void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        rectTransform = GetComponent<RectTransform>();
        layoutGroup = transform.parent?.GetComponent<LayoutGroup>();
        poppinsBold = Resources.Load<TMP_FontAsset>("Fonts/Poppins-Bold SDF");
        poppinsRegular = Resources.Load<TMP_FontAsset>("Fonts/Poppins-Regular SDF");

        cardAnimations = GetComponent<CardAnimations>();

        bonusAtk = 0;
        bonusDfs = 0;
        malusAtk = 0;
        malusDfs = 0;

        isFreeze = false;
    }
    
    void Start()
    {
        GameManager.currentRound = 1;
        cardUI = GetComponent<CardUI>();

        defenseValueMax = int.Parse(cardUI.defenseText.text);
        attackValueMax = int.Parse(cardUI.attaqueText.text);
        defenseValueCurrent = int.Parse(cardUI.defenseText.text);
        attackValueCurrent = int.Parse(cardUI.attaqueText.text);

        img = cardUI.GetComponent<Image>();
        nameCard = cardUI?.nomText?.text ?? "NULL";
        GameManager.numberOfAttacksUsed = 0;

        isAITurn = false;
        stateOffensif = "waitOrder";
        stateDefensif = "notCibled";
    }

    void Update()
    {
        Transform freezeTransform = cardUI.transform.Find("freezeIcon");
        freezeIcon = freezeTransform.gameObject;

        if(isFreeze){
            freezeIcon.SetActive(true);
        }else{
            freezeIcon.SetActive(false);
        }

        if(GameManager.isEndturnPlayer){
            //Invoke(nameof(CallMarkEndOfTurn), 0.5f);
            GameManager.isEndturnPlayer = false;
        }
        
        if((malusDfs > 0 && bonusDfs == 0)){
            UpdateMalusDefenseColor(this);
        }else if(bonusDfs > 0 && malusDfs == 0){
            UpdateBonusDefenseColor(this);
        }
        else{
            resetColorDefense(this);
        }

        if((malusAtk > 0 && bonusAtk == 0)){
            UpdateMalusAtqColor(this);
        }else if(bonusAtk > 0 && malusAtk == 0){
            UpdateBonusAtqColor(this);
        }
        else{
            resetColorAtk(this);
        }
    }

    void OnEnable() => AllCardsInteractions.Add(this);
    void OnDisable() => AllCardsInteractions.Remove(this);

    private void CallMarkEndOfTurn()
    {
       // BoardManager.Instance.MarkEndOfTurn();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        //StartCoroutine(cardAnimations.Rotate360());
        //StartCoroutine(cardAnimations.Wobble());
        //StartCoroutine(cardAnimations.Flip());
        //StartCoroutine(cardAnimations.Rotate());
        //StartCoroutine(cardAnimations.PopScale());
        //StartCoroutine(cardAnimations.Bounce(0.5f, 30f));

        if (isAITurn) return;
        if (GameManager.mode == "deck") return;
        if (isCardOpponent && GameManager.mode != "atk") return;
        if (isCardPlayer && GameManager.mode == "atk") return;
        if (!isCardPlayer && GameManager.mode == "select" && GameManager.mode == "selectCard") return;

        cardAnimations.targetImage = cardUI.GetComponentInChildren<Image>();
        //StartCoroutine(cardAnimations.Glow(cardUI));
       // StartCoroutine(cardAnimations.Fade(cardUI, 1f, 0f, 0.5f));

        if(!choiceDo && GameManager.mode != "atk"){
            if (isSelected)
            {
                DeselectCard();            
                HideActionButtons();
            }
            else
            {
                DeselectAllOtherCards();
                SelectCard();
                ShowActionButtons();
            }
        }

        if (GameManager.mode == "atk" && this.isCardOpponent && this.stateDefensif != "isAttacked"){
            SelectTarget();
            StartCoroutine(cardAnimations.ColorFlash());
            StartCoroutine(cardAnimations.Shake());
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ignorePointer || choiceDo) return;

        if (GameManager.mode == "select" && isFreeze){
            return;
        }

        if ((GameManager.mode == "select" && isCardPlayer) || (GameManager.mode == "atk" && isCardOpponent && stateDefensif != "isAttacked"))
        {
            rectTransform.anchoredPosition = (Vector2)newPosition;
            ignorePointer = true;
            StartCoroutine(ReenablePointerAfterDelay(0.1f));
        }      
    }

    private IEnumerator ReenablePointerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ignorePointer = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(this.choiceDo) return;

        if ((GameManager.mode == "select" && isCardPlayer) || (GameManager.mode == "atk" && isCardOpponent))
        {
            rectTransform.anchoredPosition = startPosition;
        }
    }

    // action sélection/désélection
    private void SelectCard()
    {           
        isSelected = true;
        rectTransform.anchoredPosition = newPosition;

        GameManager.SetMode("selectCard");
        
        if (layoutGroup?.enabled == true)
            layoutGroup.enabled = false;
        
        if (layoutElement)
            layoutElement.ignoreLayout = true;
    }
    public void DeselectCard()
    {
        GameManager.SetMode("select");

        isSelected = false;
        rectTransform.anchoredPosition = startPosition;
        
        if (layoutElement != null)
            layoutElement.ignoreLayout = false;
    }
    private void DeselectAllOtherCards()
    {
        foreach (CarteBoardInteraction card in AllCardsInteractions)
        {
            if (card.isSelected && !choiceDo)
            {
                card.DeselectCard();
                card.HideActionButtons();
            }
        }
    }
    
    private void ShowActionButtons()
    {
        if (!isCardPlayer) return;      
            
        bool canAttack = GameManager.numberOfAttacksUsed < GameManager.numberOfAttacksMax && !isFreeze;

        buttonAtk?.SetActive(canAttack);
        buttonPass?.SetActive(true);
    }

    public void HideActionButtons()
    {
        buttonAtk?.SetActive(false);
        buttonPass?.SetActive(false);
    }

    public void DestroyButton()
    {
        if (buttonAtk) Destroy(buttonAtk);
        if (buttonPass) Destroy(buttonPass);
    }
    private void OnAttaque()
    {
        /*
        GameManager.SetMode("atk");
        buttonPass?.SetActive(false);    

        if(buttonAtk)
        {
            if (buttonAtk.TryGetComponent(out RectTransform rect))
            {
                rect.anchoredPosition = new Vector2(0, rect.anchoredPosition.y);
                rect.sizeDelta = new Vector2(140, 36);
            }
    
            if (buttonAtk.TryGetComponent(out TMP_Text text))
            {
                text.color = Color.red;
                text.fontStyle = FontStyles.Bold;
                text.fontSize = 22;
            }

            if (buttonAtk.TryGetComponent(out Button button))
            {
                button.interactable = false;
            }
        }

        if (layoutElement) layoutElement.ignoreLayout = true;               
        
        attackingCard = this; 

        //PanelManager.instance.AddLog($"{nameCard} : ATTAQUE sélectionnée ({GameManager.numberOfAttacksUsed}/{GameManager.numberOfAttacksMax})");
        
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
                //PanelManager.instance.AddLog($"   → -1 en dfs pour : {chosenTarget.nameCard} (sera appliqué en fin de tour)");
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
        else
        {
            //PanelManager.instance.AddLog("   → Sélectionnez une cible adverse");
        }
        
        CheckEndOfTurn();
        */
    }
    private void OnPasser()
    {
       /* cardUI.AfficherIconePassed();
                
        if (!coloredCards.Contains(this))
            coloredCards.Add(this);

        Image imgCard = GetComponent<Image>() ?? GetComponentInChildren<Image>();
        StartCoroutine(cardAnimations.ChangeColorSmoothly(imgCard, new Color(0.4f, 0.4f, 0.4f, 1f), 0.5f));
        
        if (layoutElement)
            layoutElement.ignoreLayout = true;
        
        rectTransform.anchoredPosition = startPosition;
        
        buttonAtk.SetActive(false);

        RectTransform rectPasser = buttonPass.GetComponent<RectTransform>();
        rectPasser.anchoredPosition = new Vector2(0, rectPasser.anchoredPosition.y);
        rectPasser.sizeDelta = new Vector2(140, 36);
        
        TMP_Text txtPass = buttonPass.GetComponentInChildren<TMP_Text>();
        if(txtPass)
        {
            txtPass.fontStyle = FontStyles.Bold;
            txtPass.color = Color.black;
            txtPass.fontSize = 26;
        }
        
        // Désactiver le bouton Passer
        Button buttonPassComponent = buttonPass.GetComponent<Button>();
        buttonPassComponent.interactable = false;

        if(nameCard == "Clorel")
        {
            int currentDef = GetDefenseValue(this);
            bonusDfs++;
            int newDef = currentDef + bonusDfs;
            cardUI?.defenseText?.SetText(newDef.ToString());
            SetDefenseValue(newDef);
            //PanelManager.instance?.AddLog($"{nameCard} : PASSER sélectionné (+1 défense)");
        }
        else if(nameCard == "Cassandre"){
            int index = cardUI.indexHierarchieOriginal;
            string team = isCardOpponent ? "opponent": "player";

            var (leftCard, rightCard) = BoardManager.Instance.GetAdjacentCards(index, team);

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
        else
        {
            //PanelManager.instance?.AddLog($"{nameCard} : passe son tour");
        }
        
        choiceDo = true;
        stateOffensif = "passed";
        isSelected = false;
        
        GameManager.SetMode("select");
                
        CheckEndOfTurn();
        */
    }
    
    private void ColorCard(CarteBoardInteraction card, Color color)
    {
        Image image = card.GetComponent<Image>() ?? card.GetComponentInChildren<Image>();
        if (image)
            image.color = color;
    }
    
    public void ResetIcon(CarteBoardInteraction card)
    {   
        CardUI cardUIIcon = card.GetComponent<CardUI>();
        Transform cardTransform = cardUIIcon.transform;

        GameObject atk1Icon = cardTransform.Find("atk1")?.gameObject;
        GameObject atk2Icon = cardTransform.Find("atk2")?.gameObject;
        GameObject passedIcon = cardTransform.Find("passed")?.gameObject;

        if (atk1Icon) atk1Icon.SetActive(false);
        if (atk2Icon) atk2Icon.SetActive(false);
        if (passedIcon) passedIcon.SetActive(false);
    }

    public void RestoreCardColor(CarteBoardInteraction card)
    {   
        Image image = card.GetComponent<Image>() ?? card.GetComponentInChildren<Image>();
        if (image)
            image.color = Color.white;
    }
    
    // BoardManager
    public void ApplyAllAttacks()
    {
        /*
        int indexBelindraOpponent = -1;
        int indexBelindraPlayer = -1;
        CarteBoardInteraction belindraOpponent = null;
        CarteBoardInteraction belindraPlayer = null;
        CarteBoardInteraction zarlaCard = null;

        // Préparer dictionnaires par camp
        var playerCards = AllCardsInteractions.Where(c => c.isCardPlayer).ToList();
        var opponentCards = AllCardsInteractions.Where(c => c.isCardOpponent).ToList();

        // Identifier Zarla et Belindra
        foreach (var card in AllCardsInteractions)
        {
            switch (card.nameCard)
            {
                case "Zarla":
                    zarlaCard ??= card;
                    break;
                case "Belindra" when card.stateOffensif == "passed":
                    var index = card.GetComponent<CardUI>().indexHierarchieOriginal;
                    if (card.isCardPlayer)
                    {
                        belindraPlayer = card;
                        indexBelindraPlayer = index;
                    }
                    else
                    {
                        belindraOpponent = card;
                        indexBelindraOpponent = index;
                    }
                    break;
            }
        }

        foreach (var attaque in attaquesDuTour)
        {
            if (attaque.target == null) continue;

            var target = attaque.target;
            var attacker = attaque.attacker;
            var attackerName = attacker?.nameCard ?? "NULL";
            var targetName = target.nameCard ?? "NULL";

            // --- Minoson ---
            var minoson = (target.isCardPlayer ? playerCards : opponentCards)
                .FirstOrDefault(c => c.nameCard == "Minoson");
            if (minoson != null && targetName != "Minoson" && UnityEngine.Random.value < 0.5f)
            {
                target = minoson;
                targetName = minoson.nameCard;
                Debug.Log($"[ApplyAllAttacks] {minoson.nameCard} intercepte l'attaque destinée à {attaque.target.nameCard}.");
            }

            // --- Belindra ---
            CarteBoardInteraction leftCard = null, rightCard = null;
            if ((belindraOpponent != null && target.isCardOpponent) || (belindraPlayer != null && target.isCardPlayer))
            {
                var indexBelindra = target.isCardPlayer ? indexBelindraPlayer : indexBelindraOpponent;
                (leftCard, rightCard) = BoardManager.Instance.GetAdjacentCards(indexBelindra, target.isCardPlayer ? "player" : "opponent");


                
                if (leftCard != null) ApplyAttackMalus(leftCard, leftCard.nameCard);
                if (rightCard != null) ApplyAttackMalus(rightCard, rightCard.nameCard);
                //PanelManager.instance.AddLog("Présence de Belindra");
            }

            // --- Zarla ---
            if (zarlaCard != null && target.nameCard == "Zarla") ApplyAttackBonus(target, "Zarla");

            // --- Jaycota ---
            if (targetName == "Jaycota") 
            { 
                target.malusDfs++; 
                Debug.Log($"[ApplyAllAttacks] {targetName} malus défense appliqué."); 
            }

            // --- Tyroine ---
            if (attackerName == "Tyroine")
            {
                target.malusDfs++;
                int currentDef = target.GetDefenseValue(target);
                int newDef = Mathf.Max(0, currentDef - 1);
                target.cardUI?.defenseText?.SetText(newDef.ToString());
                target.SetDefenseValue(newDef);
                Debug.Log($"[ApplyAllAttacks] {targetName} défense réduite par Tyroine (-1 DF).");
            }

            // --- Neo ---
            if (attackerName == "Neo" && targetName != attacker.lastTarget && !string.IsNullOrEmpty(attacker.lastTarget))
            {
                ApplyAttackBonus(attacker, targetName);
                attacker.resetBonusAtk = false;
                Debug.Log($"[ApplyAllAttacks] {targetName} nouvelle cible bonus attaque pour {attackerName}.");
            }

            // --- Hiver ---
            //if (attackerName == "Hiver" && freezeIcon != null && !freezeIcon.activeSelf)
            if (attackerName == "Hiver")
            { 
                target.isFreeze = true; 
                //PanelManager.instance?.AddLog($"{target.nameCard} est gelée et ne pourra pas attaquer au tour prochain");
                Debug.Log($"[ApplyAllAttacks] {target.nameCard} is frozen by Hiver."); 
                target.freezeNumberLoop = GameManager.currentRound;
                Debug.Log($"[currentRound] {target.freezeNumberLoop}"); 
            }

            // --- Anaxagore ---
            if (attackerName == "Anaxagore") 
            { 
                target.malusDfs++;
                int currentDef = target.GetDefenseValue(target);
                int newDef = Mathf.Max(0, currentDef - 1);
                target.cardUI?.defenseText?.SetText(newDef.ToString());
                target.SetDefenseValue(newDef);
                Debug.Log($"[ApplyAllAttacks] {targetName} défense réduite par Anaxagore (-1 DF)."); 
            }

            // --- Ambroise (effet différé) ---
            if (GameManager.ambroiseEffectPending)
            {
                var passedOpponents = AllCardsInteractions
                    .Where(c => c.isCardOpponent && !attaquesDuTour.Any(a => a.attacker == c))
                    .ToList();
                    
                if(passedOpponents.Count > 0)
                {
                    var randomTarget = passedOpponents[Random.Range(0, passedOpponents.Count)];
                    randomTarget.malusDfs++;
                    int currentDef = randomTarget.GetDefenseValue(randomTarget);
                    int newDef = Mathf.Max(0, currentDef - 1);
                    randomTarget.cardUI?.defenseText?.SetText(newDef.ToString());
                    randomTarget.SetDefenseValue(newDef);
                    
                    randomTarget.UpdateMalusDefenseColor(randomTarget);
                    //PanelManager.instance?.AddLog($"   → Onde de Choc Passive d'Ambroise : -1 DF à {randomTarget.nameCard}");
                }
                
                GameManager.ambroiseEffectPending = false;
            }

            // --- Trahison (effet différé) ---
            if (GameManager.trahisonEffectPending)
            {
                var passedOpponents = AllCardsInteractions
                    .Where(c => c.isCardOpponent && !attaquesDuTour.Any(a => a.attacker == c))
                    .ToList();
                    
                if(passedOpponents.Count > 0)
                {
                    foreach (var passiveOpponent in passedOpponents)
                    {
                        passiveOpponent.malusDfs++;
                        int currentDef = passiveOpponent.GetDefenseValue(passiveOpponent);
                        int newDef = Mathf.Max(0, currentDef - 1);
                        passiveOpponent.cardUI?.defenseText?.SetText(newDef.ToString());
                        passiveOpponent.SetDefenseValue(newDef);
                        
                        passiveOpponent.UpdateMalusDefenseColor(passiveOpponent);
                        //PanelManager.instance?.AddLog($"   → Terreur Sélective de Trahison : -1 DF à {passiveOpponent.nameCard}");
                    }
                    
                    //PanelManager.instance?.AddLog($"   → Terreur Sélective de Trahison inflige -1 DF à {passedOpponents.Count} adversaire(s) passif(s)");
                }
                
                GameManager.trahisonEffectPending = false;
            }

            // --- Vilaine ---
            if (attackerName == "Vilaine") 
            { 
                // Malus d'attaque : inflige -1 ATK à sa cible sur le tour courant
                target.malusAtk++;
                int currentAtkValue = target.GetAttackValue(target);
                int newAtkValue = Mathf.Max(0, currentAtkValue - 1);
                target.SetAttaqueValue(newAtkValue);
                
                target.UpdateMalusAtqColor(target);
                //PanelManager.instance?.AddLog($"{attackerName} : Malus d'attaque inflige -1 ATK à {target.nameCard}");
            }

            if (targetName == "Solicia") 
            { 
                // Réflexion partielle : inflige 1 dégât à l'attaquant
                attacker.ApplyDamageToTarget(1, targetName);
                //PanelManager.instance?.AddLog($"{targetName} : Réflexion partielle inflige 1 dégât à {attackerName}");
            }

            // --- Zao : intouchable si a passé son tour ---
            if (targetName == "Zao" && target.stateOffensif == "passed")
            {
                Debug.Log($"[ApplyAllAttacks] {target.nameCard} esquive l'attaque de {attackerName} (Zao - mode passé).");
                //PanelManager.instance?.AddLog($"{targetName} : Zao est intouchable (mode passé)");
                continue;
            }

            // --- Esquive : une carte en mode attaque esquive les attaques --- (sauf ZAO)
            if (target.stateOffensif == "atk" && targetName != "Zao")
            {
                Debug.Log($"[ApplyAllAttacks] {target.nameCard} esquive l'attaque de {attackerName} (mode attaque).");
                //PanelManager.instance?.AddLog($"{targetName} : Esquive (mode attaque)");
                continue;
            }

            // --- Ruby : inflige 1 dégât aux ennemis adjacents si elle inflige des dégâts ---
            if (attackerName == "Ruby" && attaque.damage > 0)
            {
                int targetIndex = target.GetComponent<CardUI>().indexHierarchieOriginal;

                (leftCard, rightCard) = BoardManager.Instance.GetAdjacentCards(
                    targetIndex, 
                    target.isCardPlayer ? "player" : "opponent"
                );

                // Crée une liste des cartes adjacentes disponibles
                var adjacentEnemies = new List<CarteBoardInteraction>(2);
                if (leftCard != null) adjacentEnemies.Add(leftCard);
                if (rightCard != null) adjacentEnemies.Add(rightCard);

                if (adjacentEnemies.Count > 0)
                {
                    var chosenTarget = adjacentEnemies[UnityEngine.Random.Range(0, adjacentEnemies.Count)];
                    chosenTarget.ApplyDamageToTarget(1, attackerName);
                    //PanelManager.instance.AddLog($"{attackerName} inflige 1 dégât supplémentaire à {chosenTarget.nameCard} !");
                }
            }

            // --- Dégâts ---
            target.ApplyDamageToTarget(attaque.damage, attackerName);
        }

        attaquesDuTour.Clear();
        */
    }

    public void ResetPosition()
    {
        rectTransform.anchoredPosition = startPosition;
    }
    
    public void AutoPass()
    {
        /*stateOffensif = "passed";

        CardUI cardUIComponent = GetComponent<CardUI>();
        cardUIComponent.AfficherIconePassed();
                
        if (!coloredCards.Contains(this))
            coloredCards.Add(this);

        Image imageCard = GetComponent<Image>() ?? GetComponentInChildren<Image>();
        imageCard.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        
        // Désactiver le LayoutElement pour que la carte ne soit plus affectée par le GridLayout
        if (layoutElement)
            layoutElement.ignoreLayout = true;
        
        // Redescendre la carte à sa position initiale
        rectTransform.anchoredPosition = startPosition;
        
        // Désactiver les effets de hover du Button si présent
        Button buttonCard = GetComponent<Button>();
        if (buttonCard)
        {
            ColorBlock colors = buttonCard.colors;
            colors.normalColor = Color.white;
            colors.colorMultiplier = 1;
            colors.fadeDuration = 0;
            buttonCard.colors = colors;
        }
        choiceDo = true;
        */
    }
    
    private void ComputeAndStoreDamage()
    {
        if (attackingCard == null) return;
        
        int damage = GetAttackValue(attackingCard);
        int defenseTarget = GetDefenseValue(this);
        
        string nameAttacker = attackingCard.nameCard ?? "Attaquant";
        string nameTarget = nameCard ?? "Cible";
                
        //PanelManager.instance?.AddLog($"{nameAttacker} : ATK : {damage}");
       // PanelManager.instance?.AddLog($"{nameTarget} : DEF : {defenseTarget}");
        
        BoardManager.Instance.roundDamage.Add($"{nameAttacker} → {nameTarget} (DEF:{defenseTarget}) = {damage} dégâts");
        attaquesDuTour.Add(new AttaqueInfo(attackingCard, this, damage));
    }

    public void ComputeAndStoreDamageIA(CarteBoardInteraction attackingCard, CarteBoardInteraction target, string nameAttacker, string nameTarget)
    {        
        int damage = GetAttackValue(attackingCard);
        int defenseTarget = GetDefenseValue(target);

        if(nameTarget == "Zao" && target.stateOffensif == "passed")
        {
           //PanelManager.instance?.AddLog($"[ATTAQUEAI] {nameAttacker} : ATK = {damage} Echec de l'attaque, Zao est intouchable");
           return;
        }
         
       // PanelManager.instance?.AddLog($"[ATTAQUEAI] {nameAttacker} : ATK = {damage}");
       // PanelManager.instance?.AddLog($"[DEFENSEAI] {nameTarget} : DEF = {defenseTarget}");
        
        BoardManager.Instance.roundDamage.Add($"{nameAttacker} (ATK:{damage}) → {nameTarget} (DEF:{defenseTarget}) = {attackingCard} dégâts");
        attaquesDuTour.Add(new AttaqueInfo(attackingCard, target, damage));
    }

    private void ApplyDamageToTarget(int damage, string attackerName)
    {
        int dfsValue = GetDefenseValue(this);
        dfsValue = CalculateEffectiveDefense(dfsValue, attackerName);
        int newDfs = Mathf.Max(0, dfsValue - damage);
    
        int atqValue = GetAttackValue(this);
        
        cardUI?.defenseText?.SetText(newDfs.ToString());
        cardUI?.attaqueText?.SetText(atqValue.ToString());
        SetDefenseValue(newDfs);
        
        if (newDfs <= 0 && !yellowCard)
        {
            yellowCard = true;
            if (cardUI?.imageCarte)
                cardUI.imageCarte.color = Color.yellow;
            
            // Déduire les points de vie uniquement si c'est une carte du joueur qui est détruite
            if (isCardPlayer)
            {
                GameManager.playerScore = Mathf.Max(0, GameManager.playerScore - 1);
                //PanelManager.instance?.AddLog($"{cardUI?.nomText?.text ?? "Carte"} : DÉFENSE À 0 - Score Joueur: {GameManager.playerScore}");
            }
            else if (isCardOpponent)
            {
                GameManager.scoreOpponent = Mathf.Max(0, GameManager.scoreOpponent - 1);
                //PanelManager.instance?.AddLog($"{cardUI?.nomText?.text ?? "Carte"} : DÉFENSE À 0 - Score IA: {GameManager.scoreOpponent}");
            }
        }
    }
    
    private void CheckEndOfTurn()
    {
        /*if (GameManager.numberOfAttacksUsed == GameManager.numberOfAttacksMax)
            BoardManager.Instance.AutoPassLastCards();

        var cardsPlayer = AllCardsInteractions.Where(c => c.isCardPlayer).ToList();

        // Le tour se termine si toutes les cartes actives ont fait leur choix OU s'il n'y a plus de cartes actives
        if (cardsPlayer.All(c => c.choiceDo))
        {
            GameManager.isEndturnPlayer = true;
            if(GameManager.iaActive)
                isAITurn = false;
        }*/
    }
    
    public void SelectTarget()
    {
        /*
        if (attackingCard == null)
        {
            Debug.LogError("[SelectTarget] attackingCard est null ! Le mode 'atk' n’a pas été préparé.");
            return;
        }
        string nameTarget = nameCard ?? "Nom inconnu";
        string nameAttacker = attackingCard?.nameCard ?? "Nom inconnu";

        Debug.Log($"[SelectTarget] Attaquant {nameAttacker} Cible {nameTarget}");
        
        GameManager.numberOfAttacksUsed++;

        Color colorAtk = GameManager.numberOfAttacksUsed == 1 ? colorAtk1 : colorAtk2;        
        isCibledCount++;
        
        CardUI cardUIComponent = GetComponent<CardUI>();
        
        // Afficher atk1 pour le premier ciblage, atk2 pour le deuxième
        cardUIComponent.ShowAttackIcon(isCibledCount);
        // Appliquer la couleur de l'attaquant sur l'icône d'attaque
        if (attackingCard != null && attackingCard.cardUI != null)
        {
            CarteScriptableObject[] cartesAssets = Resources.LoadAll<CarteScriptableObject>("CartesGenerees");
            var so = System.Array.Find(cartesAssets, c => c.nom == nameAttacker);
  
            if (so != null && !string.IsNullOrEmpty(so.color))
            {
                if (isCibledCount == 1)
                {
                    cardUIComponent.SetAtk1IconColor(so.color);
                    cardUIComponent.SetAtk1IconTooltip(so.nom, so.atk);
                }
                else if (isCibledCount == 2)
                {
                    cardUIComponent.SetAtk2IconColor(so.color);
                    cardUIComponent.SetAtk2IconTooltip(so.nom, so.atk);
                }
            }
        }

        Debug.Log($"[SelectTarget] Cible {attackingCard} ");
        
        ColorCard(attackingCard, colorAtk);
        if (!coloredCards.Contains(attackingCard))
            coloredCards.Add(attackingCard);
        
        ColorCard(this, colorAtk);
        if (!coloredCards.Contains(this))
            coloredCards.Add(this);
                    
        ComputeAndStoreDamage();
        
        attackingCard.choiceDo = true;
        attackingCard.stateOffensif = "atk";
        attackingCard.isSelected = false;
        stateDefensif = "isAttacked";

        attackingCard.currentTarget = nameTarget;
        
        //PanelManager.instance?.AddLog($"{nameAttacker} attaque {nameTarget} !");
        
        CheckEndOfTurn();
        
        GameManager.SetMode("select");
        attackingCard = null;
        nameTarget = null;
        
        //BoardManager.Instance.ResetAllCardsPositions();
        */
    }

    // Board
    /*public void ReplaceOpponentYellowCards()
    {
        var yellowOpponent = AllCardsInteractions.Where(c => c.yellowCard && c.isCardOpponent).ToList();
        var yellowPlayer = AllCardsInteractions.Where(c => c.yellowCard && c.isCardPlayer).ToList();
        
        if (yellowOpponent.Count == 0 && yellowPlayer.Count == 0) 
            return;
        
        var deckOpponent = GameManager.Instance.piochePlayerB;
        var deckPlayer = GameManager.Instance.piochePlayerA;

        var cartesIntoBoardOpponent = AllCardsInteractions.Where(c => c.isCardOpponent && c.cardUI != null)
                                           .Select(c => c.cardUI.carteID).ToHashSet();

        var cartesIntoBoardPlayer = AllCardsInteractions.Where(c => c.isCardPlayer && c.cardUI != null)
                                           .Select(c => c.cardUI.carteID).ToHashSet();
        var availableCardsOpponent = deckOpponent.Where(c => !cartesIntoBoardOpponent.Contains(c.idCard.ToString())).ToList();
        var availableCardsPlayer = deckPlayer.Where(c => !cartesIntoBoardPlayer.Contains(c.idCard.ToString())).ToList();
        
        foreach (CarteBoardInteraction card in yellowOpponent)
        {
            if (availableCardsOpponent.Count == 0)
            {
                // Plus de remplaçante : rendre invisibles tous les enfants de la carte
                foreach (Transform child in card.transform)
                {
                    child.gameObject.SetActive(false);
                }
                continue;
            }
            int idx = Random.Range(0, availableCardsOpponent.Count);
            var newCard = availableCardsOpponent[idx];
            availableCardsOpponent.RemoveAt(idx);
        
            var tempList = deckOpponent.ToList();
            tempList.Remove(newCard);
            deckOpponent.Clear();

            foreach (var c in tempList) deckOpponent.Enqueue(c);

            Transform parent = card.transform.parent;
            int siblingIndex = card.transform.GetSiblingIndex();

            Vector3 oldInitialPosition = card.startPosition;

            GameObject.DestroyImmediate(card.gameObject);

            GameObject carteGO = GameObject.Instantiate(BoardManager.Instance.cartePrefab, parent);
            carteGO.transform.SetSiblingIndex(siblingIndex);

            // Réappliquer la position exacte
            RectTransform rtNewCard = carteGO.GetComponent<RectTransform>();
            rtNewCard.anchoredPosition = oldInitialPosition;

            CardUI cardUI = carteGO.GetComponent<CardUI>();
            cardUI.setAttributesInitCard(newCard);
            cardUI.isCardOpponent = true;
            BoardManager.Instance.InitializeCardOnBoard(cardUI);
        }
        
        foreach (var card in yellowPlayer)
        {
            if (availableCardsPlayer.Count == 0)
            {
                // Plus de remplaçante : rendre invisibles tous les enfants de la carte
                foreach (Transform child in card.transform)
                {
                    child.gameObject.SetActive(false);
                }
                continue;
            }
            int idx = Random.Range(0, availableCardsPlayer.Count);
            var newCard = availableCardsPlayer[idx];
            availableCardsPlayer.RemoveAt(idx);
        
            var tempList = deckPlayer.ToList();
            tempList.Remove(newCard);
            deckPlayer.Clear();

            foreach (var c in tempList) deckPlayer.Enqueue(c);

            Transform parent = card.transform.parent;
            int siblingIndex = card.transform.GetSiblingIndex();

            Vector3 oldPositionInitial = card.startPosition;

            GameObject.DestroyImmediate(card.gameObject);

            GameObject carteGO = GameObject.Instantiate(BoardManager.Instance.cartePrefab, parent);
            carteGO.transform.SetSiblingIndex(siblingIndex);

            // Réappliquer la position exacte
            RectTransform rtNewCard = carteGO.GetComponent<RectTransform>();
            rtNewCard.anchoredPosition = oldPositionInitial;

            CardUI cardUI = carteGO.GetComponent<CardUI>();
            cardUI.setAttributesInitCard(newCard);
            cardUI.isCardPlayer = true;
            BoardManager.Instance.InitializeCardOnBoard(cardUI);
        }
        GameManager.Instance.CheckGameOver();
    }
    */

    public bool HasCapacity(IAAction.Capacity cap)
    {
        return capacites != null && capacites.Contains(cap);
    }

    public static bool IsAdjacentTo(CarteBoardInteraction a, CarteBoardInteraction b)
    {
        CardUI cardUIA = a.GetComponent<CardUI>();
        CardUI cardUIB = b.GetComponent<CardUI>();
        if (cardUIA == null || cardUIB == null) return false;
        return Mathf.Abs(cardUIA.indexCarte - cardUIB.indexCarte) == 1;
    }


    // calcul des atq/dfs
    private int CalculateEffectiveDefense(int baseDfs, string attackerName)
    {
        if(malusDfs > 0){
            ApplyDfsMalus(this, attackerName);
        }

        if (attackerName == "Tyroine" || attackerName == "Xiang"  || attackerName == "Anaxagore"){
            //PanelManager.instance?.AddLog($"{nameCard ?? "Carte"} : Pertededéfense par {attackerName}");
            return Mathf.Max(0, baseDfs - 1);
        }

        return baseDfs;
    }
    private int CalculateEffectiveAttaque(int baseAtq, string attackerName)
    {
        if (attackerName == "Triomphe"){
            //PanelManager.instance?.AddLog($"{nameCard ?? "Carte"} : GainAttaque par {attackerName}");
            return Mathf.Max(0, baseAtq + 1);
        }

        return baseAtq;
    }

    // récupération/set des valeurs
    public int GetAttackValue(CarteBoardInteraction card)
    {
        if (card?.cardUI?.attaqueText)
        {
            if (int.TryParse(card.cardUI.attaqueText.text, out int atk))
                return atk;
        }
        return 0;
    }
    public int GetDefenseValue(CarteBoardInteraction card)
    {
        if (card?.cardUI?.defenseText)
        {
            if (int.TryParse(card.cardUI.defenseText.text, out int dfs))
                return dfs;
        }
        return 0;
    }
    private void SetDefenseValue(int newDfsValue)
    {
        if (cardUI?.defenseText != null)
            cardUI.defenseText.SetText(newDfsValue.ToString());
    }
    private void SetAttaqueValue(int newAtkValue)
    {
        if (cardUI?.attaqueText != null)
            cardUI.attaqueText.SetText(newAtkValue.ToString());
    }

    // application des bonus/malus
    private void ApplyAttackBonus(CarteBoardInteraction card, string nameCard)
    {
        card.bonusAtk++;
        int currentAttaqueValue = GetAttackValue(card);
        int newAtkValue = currentAttaqueValue + 1;
        card.SetAttaqueValue(newAtkValue);
        Debug.Log($"{nameCard} : +1 atk");
        //PanelManager.instance.AddLog($"{nameCard} : Bonus +1 atk");

    }
    private void UnsetAttackBonus(CarteBoardInteraction card, string nameCard)
    {
        int currentAttaqueValue = GetAttackValue(card);
        int newAtkValue = currentAttaqueValue -bonusAtk;
        bonusAtk = 0;
        card.SetAttaqueValue(newAtkValue);
        Debug.Log($"{nameCard} : unset atk");
        //PanelManager.instance.AddLog($"{nameCard} : Bonus unset atk");
    }
    private void ApplyDfsBonus(CarteBoardInteraction card, string nameCard)
    {
        card.bonusDfs++;
        int currentDfsValue = GetDefenseValue(card);
        int newDfsValue = currentDfsValue + 1;
        card.SetDefenseValue(newDfsValue);
        Debug.Log($"{nameCard} : +1 dfs");
        //PanelManager.instance.AddLog($"{nameCard} : Bonus +1 dfs");
    }
    private void ApplyAttackMalus(CarteBoardInteraction card, string nameCard)
    {
        card.malusAtk++;
        int currentAttaqueValue = GetAttackValue(card);
        int newAtkValue = currentAttaqueValue - 1;
        card.SetAttaqueValue(newAtkValue);
        Debug.Log($"{nameCard} : -1 atk");
        //PanelManager.instance.AddLog($"{nameCard} : Malus -1 atk");
    }
    private void ApplyDfsMalus(CarteBoardInteraction card, string nameCard)
    {
        card.malusDfs++;
        int currentDfsValue = GetDefenseValue(card);
        int newDfsValue = currentDfsValue - 1;
        card.SetDefenseValue(newDfsValue);
        Debug.Log($"{nameCard} : -1 dfs");
        //PanelManager.instance.AddLog($"{nameCard} : Malus -1 dfs");
    }

    public void ResetAllBonusMalus(CarteBoardInteraction card)
    {
        if (card == null || card.cardUI == null) 
            return;

        int atk = 0;
        int dfs = 0;

        if (card.cardUI.attaqueText != null)
            int.TryParse(card.cardUI.attaqueText.text, out atk);

        if (card.cardUI.defenseText != null)
            int.TryParse(card.cardUI.defenseText.text, out dfs);

        // Retirer bonus/malus

        if(card.resetBonusAtk)
            atk -= card.bonusAtk;

        atk -= card.malusAtk;
        dfs += card.malusDfs;
        dfs -= card.bonusDfs;

        if(card.resetBonusAtk)
            card.bonusAtk = 0;

        // Reset des états
        card.malusAtk = 0;
        card.malusDfs = 0;
        card.bonusDfs = 0;
        if(card.freezeNumberLoop != GameManager.currentRound)
            card.isFreeze = false;

        // Réappliquer les valeurs recalculées
        if (card.cardUI.attaqueText != null)
        {
            card.cardUI.attaqueText.text = atk.ToString();
            card.cardUI.attaqueText.color = Color.black;
        }

        if (card.cardUI.defenseText != null)
        {
            card.cardUI.defenseText.text = dfs.ToString();
            card.cardUI.defenseText.color = Color.black;
        }
    }



    // color
    public void UpdateBonusAtqColor(CarteBoardInteraction card)
    {
        if (card?.cardUI?.attaqueText)
        {
            card.cardUI.attaqueText.color = Color.green;
        }
    }
    public void UpdateBonusDefenseColor(CarteBoardInteraction card)
    {
        if (card?.cardUI?.defenseText)
        {   
            card.cardUI.defenseText.color = Color.green;
        }
    }
    public void UpdateMalusDefenseColor(CarteBoardInteraction card)
    {
        if (card?.cardUI?.defenseText)
        {
            card.cardUI.defenseText.color = Color.red;
        }
    }
    public void UpdateMalusAtqColor(CarteBoardInteraction card)
    {
        if (card?.cardUI?.attaqueText)
        {
            card.cardUI.attaqueText.color = Color.red;
        }
    }
    public void resetColorAtk(CarteBoardInteraction card)
    {
        card.cardUI.attaqueText.color = Color.black;
    }
    public void resetColorDefense(CarteBoardInteraction card)
    {
        card.cardUI.defenseText.color = Color.black;
    }
} 


/*
est une carte joueur
est une carte adversaire
est selectionné
nombre de fois ciblée
état offensif
état defensif
derniere cible
cible actuelle
bonus atk
malus atk
bonus defense
malus defense
est freeze
freeze sur le tour numéro
reset bonus atk (?)
nom de la carte
defense max
attaque max
defense courante
attaque courante
a fait son action du tour
est une carte jaune
va attaquer ce tour
*/
/*
Cycle de vie des actions — joueur

Joueur sélectionne une carte → OnPointerClick() → OnAttaque()
Joueur sélectionne une cible → SelectTarget() (ligne 1060)
Incrémente numberOfAttacksUsed
Affiche les icônes d'attaque
Appelle ComputeAndStoreDamage() (ligne 1123)
Calcule les dégâts (ATK de l'attaquant)
Stocke l'attaque dans attaquesDuTour (ligne 989)
N'applique pas encore les dégâts
Vérifie la fin de tour → CheckEndOfTurn() (ligne 1134)
Si toutes les cartes ont fait leur choix → isEndturnPlayer = true
Update() détecte isEndturnPlayer → MarkEndOfTurn() (ligne 135-136)
Affiche le résumé des dégâts
Lance le tour de l'IA si active
N'appelle pas ApplyAllAttacks()
Les attaques restent dans attaquesDuTour et ne sont pas appliquées



Cycle de vie des actions — IA

Début du tour IA → StartAITurn() → ExecuteAITurn() (ligne 28)
IA décide les actions → IAAction.DecideAction() (ligne 66)
Évalue chaque carte IA
Choisit attaquer ou passer
IA exécute les attaques → ExecuteAttack() → SimulateAIAttack() → ApplyAttack() (ligne 161)
Appelle ComputeAndStoreDamageIA() (ligne 198)
Calcule les dégâts
Stocke dans attaquesDuTour (ligne 1007)
N'applique pas encore les dégâts
IA passe les autres cartes → ExecutePass() (ligne 201)
Fin du tour IA → ApplyAllAttacks() (ligne 111)
Parcourt attaquesDuTour
Applique les effets spéciaux (Minoson, Belindra, Zarla, etc.)
Vérifie l'esquive (ligne 815)
Condition : (playerCards.Contains(target) || opponentCards.Contains(target)) && targetName != "Zao"
Cette condition est toujours vraie (sauf pour Zao)
Toutes les attaques sont esquivées → continue (ligne 818)
Les dégâts ne sont jamais appliqués
Vide attaquesDuTour (ligne 844)
Constat
Joueur : les attaques sont stockées dans attaquesDuTour mais ApplyAllAttacks() n'est jamais appelé, donc les dégâts ne sont pas appliqués.
IA : ApplyAllAttacks() est appelé, mais la condition d'esquive (ligne 815) bloque toutes les attaques, donc les dégâts ne sont pas appliqués.
Dans les deux cas, les dégâts ne sont pas appliqués, mais pour des raisons différentes.

*/