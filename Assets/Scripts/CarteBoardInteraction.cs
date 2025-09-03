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
    [SerializeField] public int targetCount = 0; 
    [SerializeField] public string stateOffensif = "";
    [SerializeField] public string stateDefensif = "";  
    [SerializeField] public string lastTarget = "";
    [SerializeField] public string currentTargetString = "";  
    [SerializeField] public int bonusAtk;  
    [SerializeField] public int bonusDfs;  
    [SerializeField] public int malusAtk;  
    [SerializeField] public int malusDfs;  
    [SerializeField] public bool freeze;  
    [SerializeField] public bool resetBonusAtk = true;  
    [SerializeField] public string nameCard;  

    private Coroutine currentMoveCoroutine;
    public static readonly List<CarteBoardInteraction> AllCardsInteractions = new();

    public bool choiceDo = false;
    public CarteUI carteUI;
    private LayoutElement layoutElement;
    public LayoutGroup layoutGroup;
    private CardAnimations cardAnimations;
    private Image img;
    public GameObject freezeIcon; // Icône "freeze"


    public Vector3 startPosition; 
    public Vector3 newPosition;
    private RectTransform rectTransform;
    private bool ignorePointer  = false;

    public bool yellowCard = false; 
    private static CarteBoardInteraction attackingCard = null; 
    public static int numberOfAttacksMax = 2;
  
    private Vector3 targetHoverOffset = new Vector3(0, -50, 0);
    private TMP_FontAsset poppinsRegular;
    private TMP_FontAsset poppinsBold;
    private GameObject buttonAtk;
    private GameObject buttonPass;
    private static Color colorAtk1 = new Color(0.8f, 0.8f, 1f, 1f);
    private static Color colorAtk2 = new Color(1f, 0.8f, 0.8f, 1f);
    private static List<CarteBoardInteraction> coloredCards = new List<CarteBoardInteraction>();
    private static List<CarteBoardInteraction> targetCards = new List<CarteBoardInteraction>();
    private static List<string> roundDamage = new List<string>(); 
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
        freeze = false;
    }
    
    void Start()
    {
        GameManager.currentRound = 1;
        carteUI = GetComponent<CarteUI>();

        img = carteUI.GetComponent<Image>();
        nameCard = carteUI?.nomText?.text ?? "NULL";
        GameManager.numberOfAttacksUsed = 0;

        isAITurn = false;
        stateOffensif = "waitOrder";
        stateDefensif = "notCibled";
    }

    void Update()
    {
        if(freeze){
            Transform freezeTransform = carteUI.transform.Find("freezeIcon");
            freezeIcon = freezeTransform.gameObject;
            freezeIcon.SetActive(true);
        }
        if(GameManager.isEndturnPlayer){
            this.Invoke("MarkEndOfTurn", 0.5f);
            GameManager.isEndturnPlayer = false;
        }
        if(malusDfs > 0){
            UpdateMalusDefenseColor(this);
        }else{
            UpdateResetMalusDefenseColor(this);
        }

        if(bonusDfs > 0){
            UpdateBonusDefenseColor(this);
        }else{
            UpdateResetBonusDefenseColor(this);
        }

        if(malusAtk > 0){
            UpdateMalusAtqColor(this);
        }else{
            UpdateResetMalusAtqColor(this);
        }
        if(bonusAtk > 0){
            UpdateBonusAtqColor(this);
        }else{
            UpdateResetBonusAtqColor(this);
        }
    }

    void OnEnable() => AllCardsInteractions.Add(this);
    void OnDisable() => AllCardsInteractions.Remove(this);
    
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

        cardAnimations.targetImage = carteUI.GetComponentInChildren<Image>();
        //StartCoroutine(cardAnimations.Glow(carteUI));
       // StartCoroutine(cardAnimations.Fade(carteUI, 1f, 0f, 0.5f));

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
            StartCoroutine(Shake());
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ignorePointer || choiceDo) return;

        if (GameManager.mode == "select" && freeze){
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
    
    public IEnumerator Shake(float duration = 0.3f, float magnitude = 5f)
    {
        Vector3 originalPos = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

            rectTransform.anchoredPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = originalPos;
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
    
    // Button
    private void ShowActionButtons()
    {
        if (!isCardPlayer) return;
                
        if (buttonAtk == null || buttonPass == null)
            CreateButtonsUnderCard();
            
        bool canAttack = GameManager.numberOfAttacksUsed < GameManager.numberOfAttacksMax;

        buttonAtk?.SetActive(canAttack);
        buttonPass?.SetActive(true);
    }
    private void CreateButtonsUnderCard()
    {        
        float offsetY = - (GetComponent<RectTransform>().sizeDelta.y + 100);
    
        Vector2 atkPosition = new Vector2(-50, offsetY);
        Vector2 passedPosition = new Vector2(50, offsetY);
    
        buttonAtk = CreateButton("Attaque", OnAttaque, atkPosition);
        buttonPass = CreateButton("Passer", OnPasser, passedPosition);
    
        buttonAtk.SetActive(false);
        buttonPass.SetActive(false);
    }
    private GameObject CreateButton(string text, UnityEngine.Events.UnityAction action, Vector2 position)
    {
        // Création de l'objet bouton
        GameObject buttonGO = new GameObject($"Bouton{text}");
        buttonGO.transform.SetParent(transform, false);

        // Setup du RectTransform
        RectTransform rectBouton = buttonGO.AddComponent<RectTransform>();
        rectBouton.sizeDelta = new Vector2(120, 30);
        rectBouton.anchoredPosition = position;

        // Ajout du composant Button
        Button button = buttonGO.AddComponent<Button>();
        button.onClick.AddListener(action);

        // Rendre le bouton invisible par défaut (fond transparent)
        var colors = button.colors;
        colors.normalColor = new Color(1, 1, 1, 0);
        button.colors = colors;     

        GameObject txtGO = new GameObject("Texte");
        txtGO.transform.SetParent(buttonGO.transform, false);
        
        TMP_Text txtButton = txtGO.AddComponent<TextMeshProUGUI>();
        txtButton.text = text.ToUpperInvariant();
        txtButton.color = Color.white;
        txtButton.font = poppinsRegular;
        txtButton.fontSize = 22;
        txtButton.alignment = TextAlignmentOptions.Center;
                
        RectTransform rectTexte = txtGO.GetComponent<RectTransform>();
        rectTexte.anchorMin = Vector2.zero;
        rectTexte.anchorMax = Vector2.one;
        rectTexte.offsetMin = Vector2.zero;
        rectTexte.offsetMax = Vector2.zero;

        return buttonGO;
    }
    public void DestroyButton()
    {
        if (buttonAtk) Destroy(buttonAtk);
        if (buttonPass) Destroy(buttonPass);
    }
    private void OnAttaque()
    {
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

        PanelManager.instance.AddLog($"{nameCard} : ATTAQUE sélectionnée ({GameManager.numberOfAttacksUsed}/{GameManager.numberOfAttacksMax})");
        
        var availableTargets = CarteBoardInteraction.AllCardsInteractions
            .Where(c => c.isCardOpponent && c.stateDefensif != "isAttacked")
            .ToList();

        if(nameCard == "Tyroine")
        {
            PanelManager.instance.AddLog("   → Sélection aléatoire");

            if(availableTargets.Count > 0)
            {
                int randomIndex = Random.Range(0, availableTargets.Count);
                CarteBoardInteraction chosenTarget = availableTargets[randomIndex];
                chosenTarget.SelectTarget();

                PanelManager.instance.AddLog($"   → Cible aléatoire sélectionnée Par Tyroine : {chosenTarget.nameCard}");
                PanelManager.instance.AddLog($"   → -1 en dfs pour : {chosenTarget.nameCard}");
                    
                int currentDef = GetDefenseValue(chosenTarget);
                chosenTarget.malusDfs = 1;
                int newDef = currentDef - chosenTarget.malusDfs;
                chosenTarget.carteUI?.defenseText?.SetText(newDef.ToString());
                SetDefenseValue(newDef);
            }
            else
            {
                PanelManager.instance.AddLog("   → Aucune cible adverse disponible");
            }
        }
        else if(nameCard == "Ondine"){

            PanelManager.instance.AddLog("   → Sélection aléatoire des cibles");

            if(availableTargets.Count > 0)
            {
                // Déterminer combien de cibles on va prendre : 1 à 3, mais pas plus que le nombre disponible
                int numberOfTargets = Mathf.Min(Random.Range(1, 4), availableTargets.Count);

                // Mélanger la liste et prendre les 'numberOfTargets' premières
                var shuffledTargets = availableTargets.OrderBy(x => Random.value).Take(numberOfTargets).ToList();

                PanelManager.instance.AddLog($"   → Nombre de cibles sélectionnées : {numberOfTargets}");

                // Répartir les dégâts
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

                    target.SelectTarget();
                    PanelManager.instance.AddLog($"   → {target.nameCard} prend {dmg} de dégâts");

                    int currentDef = GetDefenseValue(target);
                    int newDef = Mathf.Max(0, currentDef - dmg);
                    target.carteUI?.defenseText?.SetText(newDef.ToString());
                    SetDefenseValue(newDef);
                }
            }
        }
        else
        {
            PanelManager.instance.AddLog("   → Sélectionnez une cible adverse");
        }
        
        CheckEndOfTurn();
    }
    private void OnPasser()
    {
        carteUI.AfficherIconePassed();
                
        if (!coloredCards.Contains(this))
            coloredCards.Add(this);

        Image imgCard = GetComponent<Image>() ?? GetComponentInChildren<Image>();
        StartCoroutine(ChangeColorSmoothly(imgCard, new Color(0.4f, 0.4f, 0.4f, 1f), 0.5f));
        
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
            carteUI?.defenseText?.SetText(newDef.ToString());
            SetDefenseValue(newDef);
            PanelManager.instance?.AddLog($"{nameCard} : PASSER sélectionné (+1 défense)");
        }
        else if(nameCard == "Cassandre"){
            int index = carteUI.indexHierarchieOriginal;
            string team = isCardOpponent ? "opponent": "player";

            var (leftCard, rightCard) = GetAdjacentCards(index, AllCardsInteractions,team);
            PanelManager.instance.AddLog($"Cassandre passe son tour");

            if(leftCard != null)
                ApplyAttackBonus(leftCard, leftCard.nameCard);
            if(rightCard != null)
                ApplyAttackBonus(rightCard, rightCard.nameCard);
        }
        else if(nameCard == "Désir"){

            PanelManager.instance.AddLog("   → Sélection aléatoire Désir");

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
                chosenTarget.freeze = true;

                PanelManager.instance.AddLog($"   → Cible aléatoire opponent sélectionnée : {chosenTarget.nameCard}");
            }
            else if(availableTargetsPlayer.Count > 0){

                int randomIndex = Random.Range(0, availableTargetsPlayer.Count);
                CarteBoardInteraction chosenTarget  = availableTargetsPlayer[randomIndex];
                chosenTarget.freeze = true;

                PanelManager.instance.AddLog($"   → Cible aléatoire player sélectionnée : {chosenTarget.nameCard}");
            }
            else
            {
                PanelManager.instance.AddLog("   → Aucune cible adverse disponible");
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
            PanelManager.instance?.AddLog($"{nameCard} : Onde de Choc Passive en attente...");
        }
        else if(nameCard == "Trahison")
        {
            // Marquer que Trahison veut appliquer son effet plus tard
            GameManager.trahisonEffectPending = true;
            PanelManager.instance?.AddLog($"{nameCard} : Terreur Sélective en attente...");
        }
        else
        {
            PanelManager.instance?.AddLog($"{nameCard} : PASSER sélectionné");
        }
        
        choiceDo = true;
        stateOffensif = "passed";
        isSelected = false;
        
        GameManager.SetMode("select");
                
        CheckEndOfTurn();
    }
        
    public void HideActionButtons()
    {
        buttonAtk?.SetActive(false);
        buttonPass?.SetActive(false);
    }
    
    public static void ShowScore()
    {
        PanelManager.instance?.AddLog($"SCORE: {GameManager.playerScore} points");
    }
    
    private void ColorCard(CarteBoardInteraction card, Color color)
    {
        //if (card || !card.gameObject.activeInHierarchy)
        //    return;

        Image image = card.GetComponent<Image>() ?? card.GetComponentInChildren<Image>();
        if (image)
            image.color = color;
    }
    
    public void ResetIcon(CarteBoardInteraction card)
    {   
        CarteUI cardUIIcon = card.GetComponent<CarteUI>();

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
        ColorCard(card, Color.white);
    }
    
    public void ApplyAllAttacks()
    {
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
                    var index = card.GetComponent<CarteUI>().indexHierarchieOriginal;
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
                (leftCard, rightCard) = GetAdjacentCards(indexBelindra, AllCardsInteractions, target.isCardPlayer ? "player" : "opponent");
                if (leftCard != null) ApplyAttackMalus(leftCard, leftCard.nameCard);
                if (rightCard != null) ApplyAttackMalus(rightCard, rightCard.nameCard);
                PanelManager.instance.AddLog("Présence de Belindra");
            }

            // --- Zarla ---
            if (zarlaCard != null && target.nameCard == "Zarla") ApplyAttackBonus(target, "Zarla");

            // --- Jaycota ---
            if (targetName == "Jaycota") 
            { 
                target.malusDfs++; 
                Debug.Log($"[ApplyAllAttacks] {targetName} malus défense appliqué."); 
            }

            // --- Neo ---
            if (attackerName == "Neo" && targetName != attacker.lastTarget && !string.IsNullOrEmpty(attacker.lastTarget))
            {
                ApplyAttackBonus(attacker, targetName);
                attacker.resetBonusAtk = false;
                Debug.Log($"[ApplyAllAttacks] {targetName} nouvelle cible bonus attaque pour {attackerName}.");
            }

            // --- Hiver ---
            if (attackerName == "Hiver" && freezeIcon != null && !freezeIcon.activeSelf)
            { 
                target.freeze = true; 
                PanelManager.instance?.AddLog($"{target.nameCard} est gelée et ne pourra pas attaquer au tour prochain");
                Debug.Log($"[ApplyAllAttacks] {target.nameCard} is frozen by Hiver."); 
            }

            // --- Anaxagore ---
            if (attackerName == "Anaxagore") 
            { 
                target.malusDfs = Mathf.Max(0, target.malusDfs - 1); 
                Debug.Log($"[ApplyAllAttacks] {targetName} défense réduite par Anaxagore."); 
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
                    randomTarget.carteUI?.defenseText?.SetText(newDef.ToString());
                    randomTarget.SetDefenseValue(newDef);
                    
                    randomTarget.UpdateMalusDefenseColor(randomTarget);
                    PanelManager.instance?.AddLog($"   → Onde de Choc Passive d'Ambroise : -1 DF à {randomTarget.nameCard}");
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
                        passiveOpponent.carteUI?.defenseText?.SetText(newDef.ToString());
                        passiveOpponent.SetDefenseValue(newDef);
                        
                        passiveOpponent.UpdateMalusDefenseColor(passiveOpponent);
                        PanelManager.instance?.AddLog($"   → Terreur Sélective de Trahison : -1 DF à {passiveOpponent.nameCard}");
                    }
                    
                    PanelManager.instance?.AddLog($"   → Terreur Sélective de Trahison inflige -1 DF à {passedOpponents.Count} adversaire(s) passif(s)");
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
                PanelManager.instance?.AddLog($"{attackerName} : Malus d'attaque inflige -1 ATK à {target.nameCard}");
            }

            if (targetName == "Solicia") 
            { 
                // Réflexion partielle : inflige 1 dégât à l'attaquant
                attacker.ApplyDamageToTarget(1, targetName);
                PanelManager.instance?.AddLog($"{targetName} : Réflexion partielle inflige 1 dégât à {attackerName}");
            }

            // --- Esquive ---
            if ((playerCards.Contains(target) || opponentCards.Contains(target)) && targetName != "Zao")
            {
                Debug.Log($"[ApplyAllAttacks] {target.nameCard} esquive l'attaque de {attackerName}.");
                continue;
            }

            // --- Ruby : inflige 1 dégât aux ennemis adjacents si elle inflige des dégâts ---
            if (attackerName == "Ruby" && attaque.damage > 0)
            {
                int targetIndex = target.GetComponent<CarteUI>().indexHierarchieOriginal;
                (leftCard, rightCard) = GetAdjacentCards(targetIndex, AllCardsInteractions, target.isCardPlayer ? "opponent" : "player");

                // Crée une liste des cartes adjacentes disponibles
                var adjacentEnemies = new List<CarteBoardInteraction>(2);
                if (leftCard != null) adjacentEnemies.Add(leftCard);
                if (rightCard != null) adjacentEnemies.Add(rightCard);

                if (adjacentEnemies.Count > 0)
                {
                    var chosenTarget = adjacentEnemies[UnityEngine.Random.Range(0, adjacentEnemies.Count)];
                    chosenTarget.ApplyDamageToTarget(1, attackerName);
                    PanelManager.instance.AddLog($"{attackerName} inflige 1 dégât supplémentaire à {chosenTarget.nameCard} !");
                }
            }

            // --- Dégâts ---
            target.ApplyDamageToTarget(attaque.damage, attackerName);
        }

        attaquesDuTour.Clear();
    }

    public (CarteBoardInteraction leftCard, CarteBoardInteraction rightCard) GetAdjacentCards(
        int index, 
        List<CarteBoardInteraction> allCardsInteractions, 
        string team)
    {
        CarteBoardInteraction leftCard = allCardsInteractions.Find(c =>
        {
            var carteUI = c.GetComponent<CarteUI>();
            if (carteUI == null) return false;

            bool isTeamMatch = (team == "opponent" && c.isCardOpponent) ||
                            (team == "player" && c.isCardPlayer);

            return isTeamMatch && carteUI.indexHierarchieOriginal == index - 1;
        });

        CarteBoardInteraction rightCard = allCardsInteractions.Find(c =>
        {
            var carteUI = c.GetComponent<CarteUI>();
            if (carteUI == null) return false;

            bool isTeamMatch = (team == "opponent" && c.isCardOpponent) ||
                            (team == "player" && c.isCardPlayer);

            return isTeamMatch && carteUI.indexHierarchieOriginal == index + 1;
        });

        return (leftCard, rightCard);
    }

    public void ResetPosition()
    {
        rectTransform.anchoredPosition = startPosition;
    }
    
    private void AutoPassLastCards()
    {        
        foreach (CarteBoardInteraction card in AllCardsInteractions)
        {
            if (card.isCardPlayer && !card.choiceDo)
                card.AutoPass();
        }
    }
    
    private void MarkEndOfTurn()
    {
        // Si l'IA est active, simuler les attaques de l'IA
        if (GameManager.iaActive)
        {            
            PanelManager.instance?.AddLog("[IA] Lancement du tour IA");
        
            Invoke("StartAI", 0.2f);
            
            if (roundDamage.Count > 0)
            {
                PanelManager.instance.AddLog("--- RÉSUMÉ DES DÉGÂTS ---");
                foreach (string calcul in roundDamage)
                    PanelManager.instance.AddLog(calcul);
            }
            roundDamage.Clear();
            
            PanelManager.instance.AddLog($"--- SCORE ACTUEL: {GameManager.playerScore} points ---");
        }
        else
        {            
            BoardManager.Instance.ShowButtonNextStep(true);
        }
        GameManager.currentRound++;
    }
        
    private void StartAI()
    {
        IA.Instance.StartCoroutine(IA.Instance.StartAITurnCoroutine());
    }
    
    public static void EndAITurn()
    {
        Debug.Log("[IA] Tour IA terminé, passage au tour suivant");
        BoardManager.Instance.ShowButtonNextStep(true);
    }

    private IEnumerator ChangeColorSmoothly(Image image, Color targetColor, float duration)
    {
        Color startColor = image.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            image.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            yield return null;
        }
        image.color = targetColor;
    }
    
    public void AutoPass()
    {
        stateOffensif = "passed";

        CarteUI carteUIComponent = GetComponent<CarteUI>();
        carteUIComponent.AfficherIconePassed();
                
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
    }
    
    private void ComputeAndStoreDamage()
    {
        if (attackingCard == null) return;
        
        int damage = GetAttackValue(attackingCard);
        int defenseTarget = GetDefenseValue(this);
        
        string nameAttacker = attackingCard.nameCard ?? "Attaquant";
        string nameTarget = nameCard ?? "Cible";
                
        PanelManager.instance?.AddLog($"[ATTAQUE] {nameAttacker} : ATK = {damage}");
        PanelManager.instance?.AddLog($"[DEFENSE] {nameTarget} : DEF = {defenseTarget}");
        
        roundDamage.Add($"{nameAttacker} → {nameTarget} (DEF:{defenseTarget}) = {damage} dégâts");
        attaquesDuTour.Add(new AttaqueInfo(attackingCard, this, damage));
    }

    public void ComputeAndStoreDamageIA(CarteBoardInteraction attackingCard, CarteBoardInteraction target, string nameAttacker, string nameTarget)
    {        
        int damage = GetAttackValue(attackingCard);
        int defenseTarget = GetDefenseValue(target);
         
        PanelManager.instance?.AddLog($"[ATTAQUEAI] {nameAttacker} : ATK = {damage}");
        PanelManager.instance?.AddLog($"[DEFENSEAI] {nameTarget} : DEF = {defenseTarget}");
        
        roundDamage.Add($"{nameAttacker} (ATK:{damage}) → {nameTarget} (DEF:{defenseTarget}) = {attackingCard} dégâts");
        attaquesDuTour.Add(new AttaqueInfo(attackingCard, target, damage));
    }

    private void ApplyDamageToTarget(int damage, string attackerName)
    {
        int dfsValue = GetDefenseValue(this);
        dfsValue = CalculateEffectiveDefense(dfsValue, attackerName);
        int newDfs = Mathf.Max(0, dfsValue - damage);
    
        int atqValue = GetAttackValue(this);
        
        carteUI?.defenseText?.SetText(newDfs.ToString());
        carteUI?.attaqueText?.SetText(atqValue.ToString());
        
        if (newDfs <= 0 && !yellowCard)
        {
            yellowCard = true;
            if (carteUI?.imageCarte)
                carteUI.imageCarte.color = Color.yellow;
            
            GameManager.playerScore = Mathf.Max(0, GameManager.playerScore - 1);
            ShowScore();
            
            PanelManager.instance?.AddLog($"{carteUI?.nomText?.text ?? "Carte"} : DÉFENSE À 0 - Score: {GameManager.playerScore}");
        }
    }
    
    private void CheckEndOfTurn()
    {
        if (GameManager.numberOfAttacksUsed == GameManager.numberOfAttacksMax)
            AutoPassLastCards();

        var cardsPlayer = AllCardsInteractions.Where(c => c.isCardPlayer).ToList();

        // Le tour se termine si toutes les cartes actives ont fait leur choix OU s'il n'y a plus de cartes actives
        if (cardsPlayer.All(c => c.choiceDo))
        {
            GameManager.isEndturnPlayer = true;
            if(GameManager.iaActive)
                isAITurn = false;
        }
    }
    
    public void SelectTarget()
    {
        string nameTarget = nameCard ?? "Nom inconnu";
        string nameAttacker = attackingCard?.nameCard ?? "Nom inconnu";
        
        GameManager.numberOfAttacksUsed++;

        Color colorAtk = GameManager.numberOfAttacksUsed == 1 ? colorAtk1 : colorAtk2;        
        targetCount++;
        
        CarteUI carteUIComponent = GetComponent<CarteUI>();
        
        // Afficher atk1 pour le premier ciblage, atk2 pour le deuxième
        carteUIComponent.ShowAttackIcon(targetCount);
        // Appliquer la couleur de l'attaquant sur l'icône d'attaque
        if (attackingCard != null && attackingCard.carteUI != null)
        {
            CarteScriptableObject[] cartesAssets = Resources.LoadAll<CarteScriptableObject>("CartesGenerees");
            var so = System.Array.Find(cartesAssets, c => c.nom == nameAttacker);
  
            if (so != null && !string.IsNullOrEmpty(so.color))
            {
                if (targetCount == 1)
                {
                    carteUIComponent.SetAtk1IconColor(so.color);
                    carteUIComponent.SetAtk1IconTooltip(so.nom, so.atk);
                }
                else if (targetCount == 2)
                {
                    carteUIComponent.SetAtk2IconColor(so.color);
                    carteUIComponent.SetAtk2IconTooltip(so.nom, so.atk);
                }
            }
        }
        
        ColorCard(attackingCard, colorAtk);
        if (!coloredCards.Contains(attackingCard))
            coloredCards.Add(attackingCard);
        
        ColorCard(this, colorAtk);
        if (!coloredCards.Contains(this))
            coloredCards.Add(this);

        // --- Appliquer malus Anaxagore ---
        if (attackingCard != null && attackingCard.nameCard == "Anaxagore")
        {
            int currentDef = GetDefenseValue(this);
            this.malusDfs = 1;
            int newDef = currentDef - this.malusDfs;
            this.carteUI?.defenseText?.SetText(newDef.ToString());
            SetDefenseValue(newDef);
            Debug.Log($"[SelectTarget] Malus défense appliqué par {attackingCard.nameCard} par  {this.nameCard}");
        }
                    
        ComputeAndStoreDamage();
        
        attackingCard.choiceDo = true;
        attackingCard.stateOffensif = "atk";
        attackingCard.isSelected = false;
        stateDefensif = "isAttacked";

        attackingCard.currentTargetString = nameTarget;
        
        PanelManager.instance?.AddLog($"{nameAttacker} attaque {nameTarget} !");
        
        CheckEndOfTurn();
        
        GameManager.SetMode("select");
        attackingCard = null;
        nameTarget = null;
        
        ResetAllCardsPositions();
    }
    
    public void ResetAllCardsPositions()
    {
        foreach (CarteBoardInteraction interaction in AllCardsInteractions)
        {
            interaction.rectTransform.anchoredPosition = interaction.startPosition;
        }
    }

    public void ReplaceOpponentYellowCards()
    {
        var yellowOpponent = AllCardsInteractions.Where(c => c.yellowCard && c.isCardOpponent).ToList();
        var yellowPlayer = AllCardsInteractions.Where(c => c.yellowCard && c.isCardPlayer).ToList();
        
        if (yellowOpponent.Count == 0 && yellowPlayer.Count == 0) 
            return;
        
        var deckOpponent = GameManager.Instance.piochePlayerB;
        var deckPlayer = GameManager.Instance.piochePlayerA;

        var cartesIntoBoardOpponent = AllCardsInteractions.Where(c => c.isCardOpponent && c.carteUI != null)
                                           .Select(c => c.carteUI.carteID).ToHashSet();

        var cartesIntoBoardPlayer = AllCardsInteractions.Where(c => c.isCardPlayer && c.carteUI != null)
                                           .Select(c => c.carteUI.carteID).ToHashSet();
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

            CarteUI carteUI = carteGO.GetComponent<CarteUI>();
            carteUI.setAttributesInitCard(newCard);
            carteUI.isCardOpponent = true;
            BoardManager.Instance.SetCardPropertiesForGame(carteUI);
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

            CarteUI carteUI = carteGO.GetComponent<CarteUI>();
            carteUI.setAttributesInitCard(newCard);
            carteUI.isCardPlayer = true;
            BoardManager.Instance.SetCardPropertiesForGame(carteUI);
        }
        CheckGameOver();
    }
    
    public static void CheckGameOver()
    {
        if (BoardManager.Instance != null && BoardManager.Instance.handOpponentTransform != null)
        {
            var cardsOpponent = BoardManager.Instance.handOpponentTransform.GetComponentsInChildren<CarteUI>(true)
                .Where(c => c.gameObject.activeInHierarchy && c.transform.Cast<Transform>().Any(child => child.gameObject.activeSelf))
                .ToArray();
            if (cardsOpponent.Length == 0)
                TriggerVictory();
        }
    }
    
    private static void TriggerVictory()
    {
        Debug.Log("VICTOIRE ! L'adversaire n'a plus de cartes.");
        
        GameManager.playerScore++;
        PanelManager.instance.ShowVictory(GameManager.playerScore);
    }

    public bool HasCapacity(IAAction.Capacity cap)
    {
        return capacites != null && capacites.Contains(cap);
    }

    public static bool IsAdjacentTo(CarteBoardInteraction a, CarteBoardInteraction b)
    {
        CarteUI carteUIA = a.GetComponent<CarteUI>();
        CarteUI carteUIB = b.GetComponent<CarteUI>();
        if (carteUIA == null || carteUIB == null) return false;
        return Mathf.Abs(carteUIA.indexCarte - carteUIB.indexCarte) == 1;
    }


    // calcul des atq/dfs
    private int CalculateEffectiveDefense(int baseDfs, string attackerName)
    {
        if(malusDfs > 0){
            ApplyDfsMalus(this, attackerName);
        }

        if (attackerName == "Tyroine" || attackerName == "Xiang"  || attackerName == "Anaxagore"){
            PanelManager.instance?.AddLog($"{nameCard ?? "Carte"} : Pertededéfense par {attackerName}");
            return Mathf.Max(0, baseDfs - 1);
        }

        return baseDfs;
    }
    private int CalculateEffectiveAttaque(int baseAtq, string attackerName)
    {
        if (attackerName == "Triomphe"){
            PanelManager.instance?.AddLog($"{nameCard ?? "Carte"} : GainAttaque par {attackerName}");
            return Mathf.Max(0, baseAtq + 1);
        }

        return baseAtq;
    }

    // récupération/set des valeurs
    public int GetAttackValue(CarteBoardInteraction card)
    {
        if (card?.carteUI?.attaqueText)
        {
            if (int.TryParse(card.carteUI.attaqueText.text, out int atk))
                return atk;
        }
        return 0;
    }
    public int GetDefenseValue(CarteBoardInteraction card)
    {
        if (card?.carteUI?.defenseText)
        {
            if (int.TryParse(card.carteUI.defenseText.text, out int dfs))
                return dfs;
        }
        return 0;
    }
    private void SetDefenseValue(int newDfsValue)
    {
        if (carteUI?.defenseText != null)
            carteUI.defenseText.SetText(newDfsValue.ToString());
    }
    private void SetAttaqueValue(int newAtkValue)
    {
        if (carteUI?.attaqueText != null)
            carteUI.attaqueText.SetText(newAtkValue.ToString());
    }

    // application des bonus/malus
    private void ApplyAttackBonus(CarteBoardInteraction card, string nameCard)
    {
        card.bonusAtk++;
        int currentAttaqueValue = GetAttackValue(card);
        int newAtkValue = currentAttaqueValue + 1;
        card.SetAttaqueValue(newAtkValue);
        Debug.Log($"{nameCard} : +1 atk");
        PanelManager.instance.AddLog($"{nameCard} : Bonus +1 atk");

    }
    private void UnsetAttackBonus(CarteBoardInteraction card, string nameCard)
    {
        int currentAttaqueValue = GetAttackValue(card);
        int newAtkValue = currentAttaqueValue -bonusAtk;
        bonusAtk = 0;
        card.SetAttaqueValue(newAtkValue);
        Debug.Log($"{nameCard} : unset atk");
        PanelManager.instance.AddLog($"{nameCard} : Bonus unset atk");
    }
    private void ApplyDfsBonus(CarteBoardInteraction card, string nameCard)
    {
        card.bonusDfs++;
        int currentDfsValue = GetDefenseValue(card);
        int newDfsValue = currentDfsValue + 1;
        card.SetDefenseValue(newDfsValue);
        Debug.Log($"{nameCard} : +1 dfs");
        PanelManager.instance.AddLog($"{nameCard} : Bonus +1 dfs");
    }
    private void ApplyAttackMalus(CarteBoardInteraction card, string nameCard)
    {
        card.malusAtk++;
        int currentAttaqueValue = GetAttackValue(card);
        int newAtkValue = currentAttaqueValue - 1;
        card.SetAttaqueValue(newAtkValue);
        Debug.Log($"{nameCard} : -1 atk");
        PanelManager.instance.AddLog($"{nameCard} : Malus -1 atk");
    }
    private void ApplyDfsMalus(CarteBoardInteraction card, string nameCard)
    {
        card.malusDfs++;
        int currentDfsValue = GetDefenseValue(card);
        int newDfsValue = currentDfsValue - 1;
        card.SetDefenseValue(newDfsValue);
        Debug.Log($"{nameCard} : -1 dfs");
        PanelManager.instance.AddLog($"{nameCard} : Malus -1 dfs");
    }

    public void ResetAllBonusMalus(CarteBoardInteraction card)
    {
        if (card == null || card.carteUI == null) 
            return;

        int atk = 0;
        int dfs = 0;

        if (card.carteUI.attaqueText != null)
            int.TryParse(card.carteUI.attaqueText.text, out atk);

        if (card.carteUI.defenseText != null)
            int.TryParse(card.carteUI.defenseText.text, out dfs);

        // Retirer bonus/malus

        if(card.resetBonusAtk)
            atk -= card.bonusAtk;

        atk -= card.malusAtk;
        dfs += card.malusDfs;
        dfs += card.bonusDfs;

        if(card.resetBonusAtk)
            card.bonusAtk = 0;

        // Reset des états
        card.malusAtk = 0;
        card.malusDfs = 0;
        card.bonusDfs = 0;
        card.freeze = false;

        // Réappliquer les valeurs recalculées
        if (card.carteUI.attaqueText != null)
        {
            card.carteUI.attaqueText.text = atk.ToString();
            card.carteUI.attaqueText.color = Color.black;
        }

        if (card.carteUI.defenseText != null)
        {
            card.carteUI.defenseText.text = dfs.ToString();
            card.carteUI.defenseText.color = Color.black;
        }
    }



    // color
    public void UpdateBonusDefenseColor(CarteBoardInteraction card)
    {
        if (card?.carteUI?.defenseText)
        {   
            card.carteUI.defenseText.color = Color.green;
        }
    }
    public void UpdateMalusDefenseColor(CarteBoardInteraction card)
    {
        if (card?.carteUI?.defenseText)
        {
            card.carteUI.defenseText.color = Color.red;
        }
    }
    public void UpdateBonusAtqColor(CarteBoardInteraction card)
    {
        if (card?.carteUI?.attaqueText)
        {
            card.carteUI.attaqueText.color = Color.green;
        }
    }
    public void UpdateMalusAtqColor(CarteBoardInteraction card)
    {
        if (card?.carteUI?.attaqueText)
        {
            card.carteUI.attaqueText.color = Color.red;
        }
    }

    public void UpdateResetBonusDefenseColor(CarteBoardInteraction card)
    {
        if (card?.carteUI?.defenseText)
        {   
            card.carteUI.defenseText.color = Color.black;
        }
    }
    public void UpdateResetMalusDefenseColor(CarteBoardInteraction card)
    {
        if (card?.carteUI?.defenseText)
        {
            card.carteUI.defenseText.color = Color.black;
        }
    }
    public void UpdateResetBonusAtqColor(CarteBoardInteraction card)
    {
        if (card?.carteUI?.attaqueText)
        {
            card.carteUI.attaqueText.color = Color.black;
        }
    }
    public void UpdateResetMalusAtqColor(CarteBoardInteraction card)
    {
        if (card?.carteUI?.attaqueText)
        {
            card.carteUI.attaqueText.color = Color.black;
        }
    }
} 
