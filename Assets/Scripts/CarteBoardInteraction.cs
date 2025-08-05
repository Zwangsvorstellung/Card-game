using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class CarteBoardInteraction : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] public bool isCardPlayer = false;
    [SerializeField] public bool isCardAdversaire = false;
    [SerializeField] public bool isSelected = false;
    [SerializeField] public bool isHover = false;
    [SerializeField] public int nombreCiblages = 0;
    [SerializeField] public string stateOffensif = "";
    [SerializeField] public string stateDefensif = "";
    

    public static readonly List<CarteBoardInteraction> AllCardsInteractions = new();

    private bool isAttack = false;
    public bool choiceDo = false;
    private CarteUI carteUI;
    private LayoutElement layoutElement;
    private LayoutGroup layoutGroup;

    public Vector3 positionInitiale;
    public Vector3 nouvellePosition;
    private RectTransform rectTransform;
    public bool positionInitialeEnregistree = false;

    private bool carteAssombrie = false;
    private bool carteJaune = false;
    private static CarteBoardInteraction carteAttaquante = null;
    public static int nombreAttaquesMaximales = 2;
  
    private Vector3 targetHoverOffset = new Vector3(0, -50, 0);
    private Vector3 positionBeforeTargetHover;
    private TMP_FontAsset poppinsRegular;
    private TMP_FontAsset poppinsBold;
    private GameObject boutonAttaque;
    private GameObject boutonPasser;
    private static Color couleurAttaque1 = new Color(0.8f, 0.8f, 1f, 1f);
    private static Color couleurAttaque2 = new Color(1f, 0.8f, 0.8f, 1f);
    private static List<CarteBoardInteraction> cartesColorees = new List<CarteBoardInteraction>();
    private static List<CarteBoardInteraction> cartesCibled = new List<CarteBoardInteraction>();
    private static List<string> degatsDuTour = new List<string>();
    private struct AttaqueInfo
    {
        public CarteBoardInteraction attaquant;
        public CarteBoardInteraction cible;
        public int degats;
        public AttaqueInfo(CarteBoardInteraction attaquant, CarteBoardInteraction cible, int degats)
        {
            this.attaquant = attaquant;
            this.cible = cible;
            this.degats = degats;
        }
    }
    private static List<AttaqueInfo> attaquesDuTour = new List<AttaqueInfo>();
  
    // Indique si c'est le tour de l'IA
    public static bool isAITurn = false;

    private void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        rectTransform = GetComponent<RectTransform>();
        layoutGroup = transform.parent?.GetComponent<LayoutGroup>();
        poppinsBold = Resources.Load<TMP_FontAsset>("Fonts/Poppins-Bold SDF");
        poppinsRegular = Resources.Load<TMP_FontAsset>("Fonts/Poppins-Regular SDF");
    }
    
    void Start()
    {
        GameManager.numeroTour = 1;
        carteUI = GetComponent<CarteUI>();
        GameManager.nombreAttaquesUtilisees = 0;

        isAITurn = false;
        stateOffensif = "waitOrder";
        stateDefensif = "notCibled";
    }

    void OnEnable() => AllCardsInteractions.Add(this);
    void OnDisable() => AllCardsInteractions.Remove(this);
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isAITurn) return;
        if (GameManager.mode == "deck") return;
        if (isCardAdversaire && GameManager.mode != "atk") return;
        if (isCardPlayer && GameManager.mode == "atk") return;
        if (!isCardPlayer && GameManager.mode == "select" && GameManager.mode == "selectCard") return;

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

        if (GameManager.mode == "atk" && this.isCardAdversaire){
            SelectTarget();
        }
    }

    // Fonction appelée lorsque la souris entre sur la carte (hover)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(this.choiceDo) return;
        isHover = true;
        if (!positionInitialeEnregistree)
        {
            positionInitiale = rectTransform.anchoredPosition;
            positionInitialeEnregistree = true;
        }

        if (GameManager.mode == "select" && isCardPlayer && positionInitialeEnregistree){
            nouvellePosition = positionInitiale - targetHoverOffset;
            rectTransform.anchoredPosition = nouvellePosition;
        }
        
        if (GameManager.mode == "atk" && isCardAdversaire && positionInitialeEnregistree){
            nouvellePosition = positionInitiale + targetHoverOffset;
            rectTransform.anchoredPosition = nouvellePosition;
        }        
    }

    // Fonction appelée lorsque la souris quitte la carte (fin du hover)
    public void OnPointerExit(PointerEventData eventData)
    {
        if(this.choiceDo) return;
        isHover = false;
        if (GameManager.mode != "deck" && !isSelected)
            rectTransform.anchoredPosition = positionInitiale;
    }
    
    private void SelectCard()
    {           
        isSelected = true;
        isHover = true;
        rectTransform.anchoredPosition = nouvellePosition;

        GameManager.mode = "selectCard";
        
        if (layoutGroup?.enabled == true)
            layoutGroup.enabled = false;
        
        if (layoutElement != null)
            layoutElement.ignoreLayout = true;
    }
    
    public void DeselectCard()
    {
        GameManager.mode = "select";

        isSelected = false;
        isHover = false;
        rectTransform.anchoredPosition = positionInitiale;
        
        if (layoutElement != null)
            layoutElement.ignoreLayout = false;
    }
    
    private void DeselectAllOtherCards()
    {
        foreach (var interaction in AllCardsInteractions)
        {
            if (interaction.isSelected && !choiceDo)
            {
                interaction.DeselectCard();
                interaction.HideActionButtons();
            }
        }
    }
    
    private void ShowActionButtons()
    {
        if (!isCardPlayer) return;
                
        if (boutonAttaque == null || boutonPasser == null)
            CreateButtonsUnderCard();
        
        boutonAttaque?.SetActive(GameManager.nombreAttaquesUtilisees < GameManager.nombreAttaquesMaximales);
        boutonPasser?.SetActive(true);
    }
    
    private void CreateButtonsUnderCard()
    {        
        float positionY = -(GetComponent<RectTransform>().sizeDelta.y + 100);
        
        boutonAttaque = CreateButton("Attaque", OnAttaque, new Vector2(-50, positionY));
        boutonPasser = CreateButton("Passer", OnPasser, new Vector2(50, positionY));
        boutonAttaque.SetActive(false);
        boutonPasser.SetActive(false);
    }
    
    private GameObject CreateButton(string texte, UnityEngine.Events.UnityAction action, Vector2 position)
    {
        GameObject boutonGO = new GameObject($"Bouton{texte}");
        boutonGO.transform.SetParent(transform, false);
        
        RectTransform rectBouton = boutonGO.AddComponent<RectTransform>();
        rectBouton.sizeDelta = new Vector2(120, 30);
        rectBouton.anchoredPosition = position;
        
        Button bouton = boutonGO.AddComponent<Button>();
        bouton.onClick.AddListener(action);

        ColorBlock colors = bouton.colors;
        colors.normalColor = new Color(1, 1, 1, 0);
        bouton.colors = colors;        

        GameObject texteGO = new GameObject("Texte");
        texteGO.transform.SetParent(boutonGO.transform, false);
        
        TMP_Text texteBouton = texteGO.AddComponent<TextMeshProUGUI>();
        texteBouton.text = texte.ToUpper();
        texteBouton.color = Color.white;
        texteBouton.font = poppinsRegular;
        texteBouton.fontSize = 22;
        texteBouton.alignment = TextAlignmentOptions.Center;
                
        RectTransform rectTexte = texteGO.GetComponent<RectTransform>();
        rectTexte.anchorMin = Vector2.zero;
        rectTexte.anchorMax = Vector2.one;
        rectTexte.offsetMin = Vector2.zero;
        rectTexte.offsetMax = Vector2.zero;

        return boutonGO;
    }

    public void DestroyButton()
    {
        if (boutonAttaque != null)
            Destroy(boutonAttaque);
        
        if (boutonPasser != null)
            Destroy(boutonPasser);
    }
    
    private void OnAttaque()
    {
        GameManager.mode = "atk";
        string nomCarte = carteUI?.nomText?.text ?? "Nom inconnu";
        
        boutonPasser.SetActive(false);
     
        RectTransform rectAttaque = boutonAttaque.GetComponent<RectTransform>();
        rectAttaque.anchoredPosition = new Vector2(0, rectAttaque.anchoredPosition.y);
        rectAttaque.sizeDelta = new Vector2(140, 36);
        
        // Changer la couleur du texte en rouge et mettre en gras
        TMP_Text texteAttaque = boutonAttaque.GetComponentInChildren<TMP_Text>();
        texteAttaque.color = Color.red;
        texteAttaque.fontStyle = FontStyles.Bold;
        texteAttaque.fontSize = 22;
        
        // Désactiver le bouton Attaque
        Button boutonAttaqueComponent = boutonAttaque.GetComponent<Button>();
        boutonAttaqueComponent.interactable = false;
      
        if (layoutElement != null)
            layoutElement.ignoreLayout = true;               

        GameManager.nombreAttaquesUtilisees++;
        
        carteAttaquante = this;
        isHover = true;

        PanelManager.instance.AddLog($"{nomCarte} : ATTAQUE sélectionnée ({GameManager.nombreAttaquesUtilisees}/{GameManager.nombreAttaquesMaximales})");
        PanelManager.instance.AddLog("   → Sélectionnez une cible adverse");
        
        CheckEndOfTurn();
    }
    
    private void OnPasser()
    {
        string nomCarte = carteUI?.nomText?.text ?? "Nom inconnu";
        
        CarteUI carteUIComponent = GetComponent<CarteUI>();
        carteUIComponent.AfficherIconePassed();
        
        carteAssombrie = true;
        
        if (!cartesColorees.Contains(this))
            cartesColorees.Add(this);

        Image imageCarte = GetComponent<Image>() ?? GetComponentInChildren<Image>();
        imageCarte.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        
        if (layoutElement != null)
            layoutElement.ignoreLayout = true;
        
        rectTransform.anchoredPosition = positionInitiale;
        
        // Masquer le bouton Attaque
        boutonAttaque.SetActive(false);

        // Centrer le bouton
        RectTransform rectPasser = boutonPasser.GetComponent<RectTransform>();
        rectPasser.anchoredPosition = new Vector2(0, rectPasser.anchoredPosition.y);
        
        // Augmenter la taille
        rectPasser.sizeDelta = new Vector2(140, 36);
        
        // Changer la couleur du texte en rouge et mettre en gras
        TMP_Text textePasser = boutonPasser.GetComponentInChildren<TMP_Text>();
        if (textePasser != null)
        {
            textePasser.fontStyle = FontStyles.Bold;
            textePasser.fontSize = 22;
        }
        
        // Désactiver le bouton Passer
        Button boutonPasserComponent = boutonPasser.GetComponent<Button>();
        boutonPasserComponent.interactable = false;
        
        // Désactiver les effets de hover du Button si présent
        Button boutonCarte = GetComponent<Button>();
        if (boutonCarte != null)
        {
            // Désactiver les transitions de couleur
            ColorBlock colors = boutonCarte.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1;
            colors.fadeDuration = 0;
            boutonCarte.colors = colors;
            
            // Désactiver les transitions
            boutonCarte.transition = Selectable.Transition.None;
        }
        
        // Marquer que la carte a fait son choix
        choiceDo = true;
        stateOffensif = "passed";
        isSelected = false;
        isHover = false;
        
        GameManager.mode = "select";
        
        PanelManager.instance?.AddLog($"{nomCarte} : PASSER sélectionné");
        
        // Vérifier si toutes les cartes ont fait leur choix
        CheckEndOfTurn();
    }
    
    public void HideActionButtons()
    {
        boutonAttaque?.SetActive(false);
        boutonPasser?.SetActive(false);
    }
    
    public static int GetRemainingAttacks()
    {
        return GameManager.nombreAttaquesMaximales - GameManager.nombreAttaquesUtilisees;
    }
    
    public static void ShowScore()
    {
        PanelManager.instance?.AddLog($"SCORE: {GameManager.scoreJoueur} points");
    }
    
    private void ColorCard(CarteBoardInteraction carte, Color couleur)
    {
        Image image = carte.GetComponent<Image>() ?? carte.GetComponentInChildren<Image>();
        image.color = couleur;
    }
    
    private void ColorCardWithMix(CarteBoardInteraction carte, Color color)
    {
        Image image = carte.GetComponent<Image>() ?? carte.GetComponentInChildren<Image>();
        Color couleurActuelle = image?.color ?? Color.white;
        
        ColorCard(carte, couleurActuelle != Color.white ? new Color(0.7f, 0.3f, 1f, 1f) : color);
    }
    
    public void ResetIcon(CarteBoardInteraction carte)
    {   
        CarteUI carteUIIcon = carte.GetComponent<CarteUI>();

        Transform carteTransform = carteUIIcon.transform;

        GameObject atk1Icon = carteTransform.Find("atk1")?.gameObject;
        GameObject atk2Icon = carteTransform.Find("atk2")?.gameObject;
        GameObject passedIcon = carteTransform.Find("passed")?.gameObject;

        if (atk1Icon != null) atk1Icon.SetActive(false);
        if (atk2Icon != null) atk2Icon.SetActive(false);
        if (passedIcon != null) passedIcon.SetActive(false);
    }

    public void RestoreCardColor(CarteBoardInteraction carte)
    {   
        ColorCard(carte, Color.white);
    }
    
    private void ApplyAllAttacks()
    {        
        foreach (AttaqueInfo attaque in attaquesDuTour)
        {
            if (attaque.cible != null){
                attaque.cible.ApplyDamageToTarget(attaque.degats);
                cartesCibled.Add(attaque.cible);
            }
        }
        
        attaquesDuTour.Clear();
    }
    
    public void ResetIconCardTooltip()
    {
        cartesColorees.Where(carte => carte != null).ToList().ForEach(RestoreCardColor);
        cartesColorees.Clear();
    }

    public void ResetAttackColors()
    {
        cartesCibled.ToList().ForEach(ResetIcon);
        cartesCibled.Clear();
    }

    public void ResetPosition()
    {
        if(positionInitialeEnregistree)
            rectTransform.anchoredPosition = positionInitiale;
    }
    
    private void AutoPassLastCards()
    {        
        List<CarteBoardInteraction> cartesJoueur = new List<CarteBoardInteraction>();
        
        foreach (CarteBoardInteraction carte in AllCardsInteractions)
        {
            if (carte.isCardPlayer && !carte.carteAssombrie)
                cartesJoueur.Add(carte);
        }
        
        int cartesAPasser = Mathf.Min(2, cartesJoueur.Count);
        for (int i = cartesJoueur.Count - cartesAPasser; i < cartesJoueur.Count; i++)
        {
            CarteBoardInteraction carte = cartesJoueur[i];
            if (carte != null)
                carte.AutoPass();
        }
        
        if (cartesJoueur.Count > 0)
            cartesJoueur[0].Invoke("MarkEndOfTurn", 0.5f);
    }
    
    private void MarkEndOfTurn()
    {
        ApplyAllAttacks();
        
        if (degatsDuTour.Count > 0)
        {
            PanelManager.instance.AddLog("--- RÉSUMÉ DES DÉGÂTS ---");
            foreach (string calcul in degatsDuTour)
                PanelManager.instance.AddLog(calcul);
        }
        degatsDuTour.Clear();
        
        PanelManager.instance.AddLog($"--- SCORE ACTUEL: {GameManager.scoreJoueur} points ---");
        
        // Si l'IA est active, simuler les attaques de l'IA
        if (GameManager.iaActive)
        {
            PanelManager.instance?.AddLog("[IA] Simulation des attaques...");
            
            var cartesIA = AllCardsInteractions.Where(c => c.isCardPlayer).ToList();
            var cartesJoueur = AllCardsInteractions.Where(c => c.isCardPlayer).ToList();

            PanelManager.instance?.AddLog($"[DEBUG] Cartes IA: {cartesIA.Count}, Cartes joueur: {cartesJoueur.Count}");
            PanelManager.instance?.AddLog("[IA] Lancement du tour IA");
        }
                
        if (GameManager.iaActive)
        {
            Invoke("StartAI", 0.2f);
        }
        else
        {            
            BoardManager.Instance.ShowButtonNextStep(true);
        }
        GameManager.numeroTour++;
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
    
    
    public void AutoPass()
    {
        string nomCarte = carteUI?.nomText?.text ?? "Nom inconnu";
        
        carteAssombrie = true;
        
        // Ajouter du noir sur l'image pour assombrir la carte
        Image imageCarte = GetComponent<Image>();
        if (imageCarte != null)
            imageCarte.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        
        
        // Désactiver le LayoutElement pour que la carte ne soit plus affectée par le GridLayout
        if (layoutElement != null)
            layoutElement.ignoreLayout = true;
        
        // Redescendre la carte à sa position initiale
        if (positionInitialeEnregistree)
            rectTransform.anchoredPosition = positionInitiale;
        
        // Désactiver les effets de hover du Button si présent
        Button boutonCarte = GetComponent<Button>();
        if (boutonCarte != null)
        {
            // Désactiver les transitions de couleur
            ColorBlock colors = boutonCarte.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1;
            colors.fadeDuration = 0;
            boutonCarte.colors = colors;
            
            // Désactiver les transitions
            boutonCarte.transition = Selectable.Transition.None;
        }
        
        // Marquer que la carte a fait son choix
        choiceDo = true;
    }
    
    private void ComputeAndStoreDamage()
    {
        if (carteAttaquante == null) return;
        
        int attaqueAttaquant = GetAttackValue(carteAttaquante);
        int defenseCible = GetDefenseValue(this);
        
        string nomAttaquant = carteAttaquante.carteUI?.nomText?.text ?? "Attaquant";
        string nomCible = carteUI?.nomText?.text ?? "Cible";
                
        PanelManager.instance?.AddLog($"[ATTAQUE] {nomAttaquant} : ATK = {attaqueAttaquant}");
        PanelManager.instance?.AddLog($"[DEFENSE] {nomCible} : DEF = {defenseCible}");
        
        degatsDuTour.Add($"{nomAttaquant} (ATK:{attaqueAttaquant}) → {nomCible} (DEF:{defenseCible}) = {attaqueAttaquant} dégâts");
        attaquesDuTour.Add(new AttaqueInfo(carteAttaquante, this, attaqueAttaquant));
    }
    
    private int GetAttackValue(CarteBoardInteraction carte)
    {
        if (carte?.carteUI?.attaqueText != null)
        {
            if (int.TryParse(carte.carteUI.attaqueText.text, out int attaque))
                return attaque;
        }
        return 0;
    }
    
    private int GetDefenseValue(CarteBoardInteraction carte)
    {
        if (carte?.carteUI?.defenseText != null)
        {
            if (int.TryParse(carte.carteUI.defenseText.text, out int defense))
                return defense;
        }
        return 0;
    }
    
    private void ApplyDamageToTarget(int degats)
    {
        int nouvelleDefense = Mathf.Max(0, GetDefenseValue(this) - degats);
        
        carteUI?.defenseText?.SetText(nouvelleDefense.ToString());
        carteUI?.attaqueText?.SetText(GetAttackValue(this).ToString());
        
        if (nouvelleDefense <= 0 && !carteJaune)
        {
            carteJaune = true;
            if (carteUI?.imageCarte != null)
                carteUI.imageCarte.color = Color.yellow;
            
            GameManager.scoreJoueur = Mathf.Max(0, GameManager.scoreJoueur - 1);
            ShowScore();
            
            PanelManager.instance?.AddLog($"{carteUI?.nomText?.text ?? "Carte"} : DÉFENSE À 0 - Score: {GameManager.scoreJoueur}");
        }
    }
    
    private void CheckEndOfTurn()
    {
        var cartesJoueur = AllCardsInteractions.Where(c => c.isCardPlayer).ToList();

        // Le tour se termine si toutes les cartes actives ont fait leur choix OU s'il n'y a plus de cartes actives
        if (cartesJoueur.All(c => c.choiceDo))
        {
            if(GameManager.iaActive)
                isAITurn = false;
                
            AllCardsInteractions[0].Invoke("MarkEndOfTurn", 0.5f);
        }
    }
    
    public void SelectTarget()
    {
        string nomCible = carteUI?.nomText?.text ?? "Nom inconnu";
        string nomAttaquant = carteAttaquante?.carteUI?.nomText?.text ?? "Nom inconnu";
            
        Color couleurAttaque = GameManager.nombreAttaquesUtilisees == 1 ? couleurAttaque1 : couleurAttaque2;        
        nombreCiblages++;
        
        // Afficher l'icône d'attaque sur la carte cible (cette carte)
        CarteUI carteUIComponent = GetComponent<CarteUI>();
        
        // Afficher atk1 pour le premier ciblage, atk2 pour le deuxième
        carteUIComponent.ShowAttackIcon(nombreCiblages);
        // Appliquer la couleur de l'attaquant sur l'icône d'attaque
        if (carteAttaquante != null && carteAttaquante.carteUI != null)
        {
            string nomAttaquantSO = carteAttaquante.carteUI.nomText?.text;

            CarteScriptableObject[] cartesAssets = Resources.LoadAll<CarteScriptableObject>("CartesGenerees");
            var so = System.Array.Find(cartesAssets, c => c.nom == nomAttaquantSO);
            if (so != null && !string.IsNullOrEmpty(so.color))
            {
                if (nombreCiblages == 1)
                {
                    carteUIComponent.SetAtk1IconColor(so.color);
                    carteUIComponent.SetAtk1IconTooltip(so.nom, so.atk);
                }
                else if (nombreCiblages == 2)
                {
                    carteUIComponent.SetAtk2IconColor(so.color);
                    carteUIComponent.SetAtk2IconTooltip(so.nom, so.atk);
                }
            }
        }
        
        ColorCardWithMix(carteAttaquante, couleurAttaque);
        if (!cartesColorees.Contains(carteAttaquante))
            cartesColorees.Add(carteAttaquante);
        
        ColorCardWithMix(this, couleurAttaque);
        if (!cartesColorees.Contains(this))
            cartesColorees.Add(this);
            
        isAttack = true;
        
        ComputeAndStoreDamage();
        
        carteAttaquante.choiceDo = true;
        carteAttaquante.stateOffensif = "atk";
        carteAttaquante.isSelected = false;
        
        PanelManager.instance?.AddLog($"{nomAttaquant} attaque {nomCible} !");
        
        CheckEndOfTurn();
        
        GameManager.mode = "select";
        carteAttaquante = null;
        
        DesactivateHoverEffectOnOpponentCards();
        
        if (GameManager.nombreAttaquesUtilisees == GameManager.nombreAttaquesMaximales)
            AutoPassLastCards();
    }
    
    // Désactive le mode hover cible sur toutes les cartes adversaires (après la sélection ou à la fin du tour).
    public void DesactivateHoverEffectOnOpponentCards()
    {
        foreach (CarteBoardInteraction interaction in AllCardsInteractions)
        {
            if (!interaction.isCardPlayer && interaction != this && !interaction.isAttack){
                interaction.ResetCardPosition();
            }
        }
    }
    
    // Remet la carte à sa position d'origine (utilisé dans le reset global ou à la fin de la sélection).
    public void ResetCardPosition()
    {
        if (positionInitialeEnregistree)
        {
            rectTransform.anchoredPosition = positionInitiale;
            transform.localRotation = Quaternion.identity;
            if (carteUI.indexHierarchieOriginal >= 0)
            {
                transform.SetSiblingIndex(carteUI.indexHierarchieOriginal);
            }
        }
    }

    public void ReplaceOpponentYellowCards()
    {
        var jaunes = AllCardsInteractions.Where(c => !c.isCardPlayer && c.carteJaune).ToList();
        
        if (jaunes.Count == 0) 
            return;
        
        var pioche = GameManager.Instance.piochePlayerB;
        var cartesSurBoard = AllCardsInteractions.Where(c => !c.isCardPlayer && c.carteUI != null)
                                           .Select(c => c.carteUI.carteID).ToHashSet();
        var disponibles = pioche.Where(c => !cartesSurBoard.Contains(c.idCard.ToString())).ToList();
        
        foreach (var carte in jaunes)
        {
            if (disponibles.Count == 0)
            {
                // Plus de remplaçante : rendre invisibles tous les enfants de la carte
                foreach (Transform child in carte.transform)
                {
                    child.gameObject.SetActive(false);
                }
                continue;
            }
            int idx = Random.Range(0, disponibles.Count);
            var nouvelleCarte = disponibles[idx];
            disponibles.RemoveAt(idx);
            var tempList = pioche.ToList();
            tempList.Remove(nouvelleCarte);
            pioche.Clear();
            foreach (var c in tempList) pioche.Enqueue(c);
            Transform parent = carte.transform.parent;
            int siblingIndex = carte.transform.GetSiblingIndex();
            GameObject.DestroyImmediate(carte.gameObject);
            GameObject carteGO = GameObject.Instantiate(BoardManager.Instance.cartePrefab, parent);
            carteGO.transform.SetSiblingIndex(siblingIndex);
            CarteUI carteUI = carteGO.GetComponent<CarteUI>();
            carteUI.isCarteAdversaire = true;
            carteUI.ShowCard(nouvelleCarte);

            CarteBoardInteraction interaction = carteGO.GetComponent<CarteBoardInteraction>();
            interaction.isCardAdversaire = true;
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
        
        GameManager.scoreJoueur++;
        PanelManager.instance.ShowVictory(GameManager.scoreJoueur);
    }
} 