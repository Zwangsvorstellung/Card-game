using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class CarteBoardInteraction : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public List<IAAction.Capacite> capacites; // la liste des capacités de la carte

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

    private Coroutine currentMoveCoroutine;
    public static readonly List<CarteBoardInteraction> AllCardsInteractions = new();

    public bool choiceDo = false;
    public CarteUI carteUI;
    private LayoutElement layoutElement;
    public LayoutGroup layoutGroup;
    private CardAnimations cardAnimations;
    private Image img;

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

    public bool HasAttackedThisTurn = false;
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
        GameManager.numberOfAttacksUsed = 0;

        isAITurn = false;
        stateOffensif = "waitOrder";
        stateDefensif = "notCibled";
    }

    void Update()
    {
        if(GameManager.isEndturnPlayer){
            this.Invoke("MarkEndOfTurn", 0.5f);
            GameManager.isEndturnPlayer = false;
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

    private void SelectCard()
    {           
        isSelected = true;
        rectTransform.anchoredPosition = newPosition;

        GameManager.mode = "selectCard";
        
        if (layoutGroup?.enabled == true)
            layoutGroup.enabled = false;
        
        if (layoutElement)
            layoutElement.ignoreLayout = true;
    }
    
    public void DeselectCard()
    {
        GameManager.mode = "select";

        isSelected = false;
        rectTransform.anchoredPosition = startPosition;
        
        if (layoutElement != null)
            layoutElement.ignoreLayout = false;
    }
    
    private void DeselectAllOtherCards()
    {
        foreach (var card in AllCardsInteractions)
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
                
        if (buttonAtk == null || buttonPass == null)
            CreateButtonsUnderCard();
            
        bool canAttack = GameManager.numberOfAttacksUsed < GameManager.numberOfAttacksMax;

        buttonAtk?.SetActive(canAttack);
        buttonPass?.SetActive(true);
    }
    
    private void CreateButtonsUnderCard()
    {        
        float offsetY = - (GetComponent<RectTransform>().sizeDelta.y + 100);
    
        Vector2 attaquePosition = new Vector2(-50, offsetY);
        Vector2 passerPosition = new Vector2(50, offsetY);
    
        buttonAtk = CreateButton("Attaque", OnAttaque, attaquePosition);
        buttonPass = CreateButton("Passer", OnPasser, passerPosition);
    
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
        GameManager.mode = "atk";
        string nameCard = carteUI?.nomText?.text ?? "Nom inconnu";
        
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
        
        if(nameCard == "Tyroine")
        {
            PanelManager.instance.AddLog("   → Sélection aléatoire");

            // Récupérer toutes les cartes adversaires valides
            var availableTargets = CarteBoardInteraction.AllCardsInteractions
                .Where(c => c.isCardOpponent && c.stateDefensif != "isAttacked")
                .ToList();

            if(availableTargets.Count > 0)
            {
                int randomIndex = Random.Range(0, availableTargets.Count);
                var chosenTarget  = availableTargets[randomIndex];
                chosenTarget.SelectTarget();

                PanelManager.instance.AddLog($"   → Cible aléatoire sélectionnée Par Tyroine : {chosenTarget.carteUI.nomText.text}");
                PanelManager.instance.AddLog($"   → -1 en dfs pour : {chosenTarget.carteUI.nomText.text}");
                    
                int currentDef = GetDefenseValue(chosenTarget);
                Debug.Log(currentDef);
                malusDfs = 1;
                int newDef = currentDef - malusDfs;
                chosenTarget.carteUI?.defenseText?.SetText(newDef.ToString());
                SetDefenseValue(newDef);
            }
            else
            {
                PanelManager.instance.AddLog("   → Aucune cible adverse disponible");
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
        string nameCard = carteUI?.nomText?.text ?? "Nom inconnu";

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

            var (leftCard, rightCard) = GetAdjacentCards(index, AllCardsInteractions);

            ApplyAttackBonus(leftCard, nameCard);
            ApplyAttackBonus(rightCard, nameCard);
        }
        else if(nameCard == "Désir"){

            PanelManager.instance.AddLog("   → Sélection aléatoire Désir");

            var availableTargets = CarteBoardInteraction.AllCardsInteractions
                .Where(c => c.isCardOpponent)
                .ToList();

            if(availableTargets.Count > 0)
            {
                int randomIndex = Random.Range(0, availableTargets.Count);
                var chosenTarget  = availableTargets[randomIndex];
                chosenTarget.freeze = true;

                PanelManager.instance.AddLog($"   → Cible aléatoire sélectionnée : {chosenTarget.carteUI.nomText.text}");
            }
            else
            {
                PanelManager.instance.AddLog("   → Aucune cible adverse disponible");
            }
        }
        else
        {
            PanelManager.instance?.AddLog($"{nameCard} : PASSER sélectionné");
        }
        
        choiceDo = true;
        stateOffensif = "passed";
        isSelected = false;
        
        GameManager.mode = "select";
                
        CheckEndOfTurn();
    }

    private void ApplyAttackBonus(CarteBoardInteraction card, string nameCard)
    {
        int currentAttaqueValue = GetAttackValue(card);
        int newAtkValue = currentAttaqueValue + 1;
        card.SetAttaqueValue(newAtkValue);
        Debug.Log($"{nameCard} : +1 atk");
    }

    private void ApplyDfsBonus(CarteBoardInteraction card, string nameCard)
    {
        int currentDfsValue = GetDefenseValue(card);
        int newDfsValue = currentDfsValue + 1;
        card.SetDefenseValue(newDfsValue);
        Debug.Log($"{nameCard} : +1 dfs");
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
        //Debug.Log("=== [ApplyAllAttacks] Début de l'application des attaques ===");
        //Debug.Log($"Nombre d'attaques à appliquer : {attaquesDuTour.Count}");

        HashSet<CarteBoardInteraction> cardsAttacking = new HashSet<CarteBoardInteraction>();
        HashSet<CarteBoardInteraction> cardsAttackingPlayer = new HashSet<CarteBoardInteraction>();
        HashSet<CarteBoardInteraction> cardsAttackingOpponent = new HashSet<CarteBoardInteraction>();

        HashSet<CarteBoardInteraction> cardsTarget = new HashSet<CarteBoardInteraction>();
        HashSet<CarteBoardInteraction> cardsTargetPlayer = new HashSet<CarteBoardInteraction>();
        HashSet<CarteBoardInteraction> cardsTargetOpponnent = new HashSet<CarteBoardInteraction>();

        bool soliciaInPlayerDeck = false;
        bool soliciaInOpponentDeck = false;

        bool zarlaPresentOnBoard = false;

        foreach (CarteBoardInteraction interaction in AllCardsInteractions)
        {
            CarteUI carteUI = interaction.GetComponent<CarteUI>();
            string cardName = carteUI?.nomText?.text ?? "NULL";
    
            if (cardName == "Solicia")
            {
                if (interaction.isCardPlayer)
                    soliciaInPlayerDeck = true;
                else if (interaction.isCardOpponent)
                    soliciaInOpponentDeck = true;
    
                if (soliciaInPlayerDeck && soliciaInOpponentDeck)
                    break;
            }   

            if (cardName == "Zarla")
            {
                zarlaPresentOnBoard = true;
            } 
        }

        foreach (AttaqueInfo attaque in attaquesDuTour)
        {
            if (attaque.attacker)
                cardsAttacking.Add(attaque.attacker);

            if (attaque.attacker && attaque.attacker.isCardPlayer)
                cardsAttackingPlayer.Add(attaque.attacker);

            if (attaque.attacker && attaque.attacker.isCardOpponent)
                cardsAttackingOpponent.Add(attaque.attacker);

            if (attaque.target)
                cardsTarget.Add(attaque.target);

            if (attaque.target && attaque.target.isCardPlayer)
                cardsTargetPlayer.Add(attaque.target);

            if (attaque.target && attaque.target.isCardOpponent)
                cardsTargetOpponnent.Add(attaque.target);
        }

        bool zarlaPresentTarget = cardsTarget.Any(card =>
        {
            CarteUI carteUI = card.GetComponent<CarteUI>();
            string cardName = carteUI?.nomText?.text ?? "NULL";
            return cardName == "Zarla";
        });

        if (zarlaPresentTarget)
        {
            var zarlaCard = cardsTarget.First(card =>
                    (card.GetComponent<CarteUI>()?.nomText?.text ?? "") == "Zarla");
                
            ApplyAttackBonus(zarlaCard, "Zarla");
        }
        else if(zarlaPresentOnBoard)
        {
            foreach (CarteBoardInteraction card in AllCardsInteractions)
            {
                CarteUI carteUI = card.GetComponent<CarteUI>();
                string cardName = carteUI?.nomText?.text ?? "NULL";
        
                if (cardName == "Zarla")
                {
                    ApplyDfsBonus(card, "Zarla");
                }   
            }
        }

        foreach (AttaqueInfo attaque in attaquesDuTour)
        {
            string attackerName = attaque.attacker?.carteUI?.nomText?.text ?? "NULL";
            string targetName = attaque.target?.carteUI?.nomText?.text ?? "NULL";
            //Debug.Log($"[ApplyAllAttacks] attacker : {attackerName}, target : {targetName}, Dégâts : {attaque.damage}");

            if (attaque.target)
            {
                bool targetIsAttacking = cardsAttacking.Contains(attaque.target);
                bool isZao = targetName == "Zao";

                bool shouldDodge = targetIsAttacking && !isZao;

                if (shouldDodge)
                {
                    Debug.Log($"[ApplyAllAttacks] {targetName} esquive l'attaque de {attackerName}.");
                    continue;
                }

                // Apply freeze effect if attacker is "Hiver"
                if (attackerName == "Hiver")
                {
                    attaque.target.freeze = true;
                    Debug.Log($"[ApplyAllAttacks] {targetName} is frozen by Hiver.");
                    PanelManager.instance.AddLog($"[ApplyAllAttacks] {targetName} is frozen by Hiver.");
                }
                
                // Sinon elle prend les dégâts
                attaque.target.ApplyDamageToTarget(attaque.damage, attackerName);
                targetCards.Add(attaque.target);
                CurrentTarget = attaque.target;
                currentTargetString = targetName;
            }
        }

        attaquesDuTour.Clear();
    }

    public (CarteBoardInteraction leftCard, CarteBoardInteraction rightCard) GetAdjacentCards(int index, List<CarteBoardInteraction> allCardsInteractions)
    {
        CarteBoardInteraction leftCard = allCardsInteractions.Find(c => 
        {
            var carteUI = c.GetComponent<CarteUI>();
            return carteUI != null && carteUI.indexHierarchieOriginal == index - 1;
        });

        CarteBoardInteraction rightCard = allCardsInteractions.Find(c =>
        {
            var carteUI = c.GetComponent<CarteUI>();
            return carteUI != null && carteUI.indexHierarchieOriginal == index + 1;
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
            var cartesIA = AllCardsInteractions.Where(c => c.isCardOpponent).ToList();
            var cartesJoueur = AllCardsInteractions.Where(c => c.isCardPlayer).ToList();

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
        
        string nameAttacker = attackingCard.carteUI?.nomText?.text ?? "Attaquant";
        string nameTarget = carteUI?.nomText?.text ?? "Cible";
                
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

    private int CalculateEffectiveDefense(int baseDfs, string attackerName)
    {
        if (attackerName == "Tyroine"){
            PanelManager.instance?.AddLog($"{carteUI?.nomText?.text ?? "Carte"} : Pertededéfense");
            return Mathf.Max(0, baseDfs - 1);
        }
        return baseDfs;
    }

    private void ApplyDamageToTarget(int damage, string attackerName)
    {
        int dfsValue = GetDefenseValue(this);
        dfsValue = CalculateEffectiveDefense(dfsValue, attackerName);

        int newDfs = Mathf.Max(0, dfsValue - damage);
        
        carteUI?.defenseText?.SetText(newDfs.ToString());
        carteUI?.attaqueText?.SetText(GetAttackValue(this).ToString());
        
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

        var cartesJoueur = AllCardsInteractions.Where(c => c.isCardPlayer).ToList();

        // Le tour se termine si toutes les cartes actives ont fait leur choix OU s'il n'y a plus de cartes actives
        if (cartesJoueur.All(c => c.choiceDo))
        {
            GameManager.isEndturnPlayer = true;
            if(GameManager.iaActive)
                isAITurn = false;
        }
    }
    
    public void SelectTarget()
    {
        string nameTarget = carteUI?.nomText?.text ?? "Nom inconnu";
        string nameAttacker = attackingCard?.carteUI?.nomText?.text ?? "Nom inconnu";
        
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
                    
        ComputeAndStoreDamage();
        
        attackingCard.choiceDo = true;
        attackingCard.stateOffensif = "atk";
        attackingCard.isSelected = false;
        stateDefensif = "isAttacked";

        attackingCard.lastTarget = attackingCard.currentTargetString;
        attackingCard.currentTargetString = nameTarget;
        
        PanelManager.instance?.AddLog($"{nameAttacker} attaque {nameTarget} !");
        
        CheckEndOfTurn();
        
        GameManager.mode = "select";
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
        var jaunesAdversaire = AllCardsInteractions.Where(c => c.yellowCard && c.isCardOpponent).ToList();
        var jaunesPlayer = AllCardsInteractions.Where(c => c.yellowCard && c.isCardPlayer).ToList();
        
        if (jaunesAdversaire.Count == 0 && jaunesPlayer.Count == 0) 
            return;
        
        var piocheAdversaire = GameManager.Instance.piochePlayerB;
        var piocheplayer = GameManager.Instance.piochePlayerA;

        var cartesSurBoardAdversaire = AllCardsInteractions.Where(c => c.isCardOpponent && c.carteUI != null)
                                           .Select(c => c.carteUI.carteID).ToHashSet();

        var cartesSurBoardPlayer = AllCardsInteractions.Where(c => c.isCardPlayer && c.carteUI != null)
                                           .Select(c => c.carteUI.carteID).ToHashSet();
        var disponiblesAdversaire = piocheAdversaire.Where(c => !cartesSurBoardAdversaire.Contains(c.idCard.ToString())).ToList();
        var disponiblesPlayer = piocheplayer.Where(c => !cartesSurBoardPlayer.Contains(c.idCard.ToString())).ToList();
        
        foreach (var carte in jaunesAdversaire)
        {
            if (disponiblesAdversaire.Count == 0)
            {
                // Plus de remplaçante : rendre invisibles tous les enfants de la carte
                foreach (Transform child in carte.transform)
                {
                    child.gameObject.SetActive(false);
                }
                continue;
            }
            int idx = Random.Range(0, disponiblesAdversaire.Count);
            var nouvelleCarte = disponiblesAdversaire[idx];
            disponiblesAdversaire.RemoveAt(idx);
        
            var tempList = piocheAdversaire.ToList();
            tempList.Remove(nouvelleCarte);
            piocheAdversaire.Clear();

            foreach (var c in tempList) piocheAdversaire.Enqueue(c);

            Transform parent = carte.transform.parent;
            int siblingIndex = carte.transform.GetSiblingIndex();

            Vector3 anciennepositionInitiale = carte.startPosition;

            GameObject.DestroyImmediate(carte.gameObject);

            GameObject carteGO = GameObject.Instantiate(BoardManager.Instance.cartePrefab, parent);
            carteGO.transform.SetSiblingIndex(siblingIndex);

            // Réappliquer la position exacte
            RectTransform rtNouvelleCarte = carteGO.GetComponent<RectTransform>();
            rtNouvelleCarte.anchoredPosition = anciennepositionInitiale;

            CarteUI carteUI = carteGO.GetComponent<CarteUI>();
            carteUI.setAttributesInitCard(nouvelleCarte);
            carteUI.isCarteAdversaire = true;
            BoardManager.Instance.SetCardPropertiesForGame(carteUI);
        }
        
        foreach (var carte in jaunesPlayer)
        {
            if (disponiblesPlayer.Count == 0)
            {
                // Plus de remplaçante : rendre invisibles tous les enfants de la carte
                foreach (Transform child in carte.transform)
                {
                    child.gameObject.SetActive(false);
                }
                continue;
            }
            int idx = Random.Range(0, disponiblesPlayer.Count);
            var nouvelleCarte = disponiblesPlayer[idx];
            disponiblesPlayer.RemoveAt(idx);
        
            var tempList = piocheplayer.ToList();
            tempList.Remove(nouvelleCarte);
            piocheplayer.Clear();

            foreach (var c in tempList) piocheplayer.Enqueue(c);

            Transform parent = carte.transform.parent;
            int siblingIndex = carte.transform.GetSiblingIndex();

            Vector3 anciennepositionInitiale = carte.startPosition;

            GameObject.DestroyImmediate(carte.gameObject);

            GameObject carteGO = GameObject.Instantiate(BoardManager.Instance.cartePrefab, parent);
            carteGO.transform.SetSiblingIndex(siblingIndex);

            // Réappliquer la position exacte
            RectTransform rtNouvelleCarte = carteGO.GetComponent<RectTransform>();
            rtNouvelleCarte.anchoredPosition = anciennepositionInitiale;

            CarteUI carteUI = carteGO.GetComponent<CarteUI>();
            carteUI.setAttributesInitCard(nouvelleCarte);
            carteUI.isCartePlayer = true;
            BoardManager.Instance.SetCardPropertiesForGame(carteUI);
        }
        CheckGameOver();
    }
    
    public static void CheckGameOver()
    {
        if (BoardManager.Instance != null && BoardManager.Instance.mainAdversaireTransform != null)
        {
            var cartesAdversaire = BoardManager.Instance.mainAdversaireTransform.GetComponentsInChildren<CarteUI>(true)
                .Where(c => c.gameObject.activeInHierarchy && c.transform.Cast<Transform>().Any(child => child.gameObject.activeSelf))
                .ToArray();
            if (cartesAdversaire.Length == 0)
                TriggerVictory();
        }
    }
    
    private static void TriggerVictory()
    {
        Debug.Log("VICTOIRE ! L'adversaire n'a plus de cartes.");
        
        GameManager.playerScore++;
        PanelManager.instance.ShowVictory(GameManager.playerScore);
    }

    public bool HasCapacite(IAAction.Capacite cap)
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
} 
