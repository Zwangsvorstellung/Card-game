using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    public GameObject cartePrefab;
    public  List<string> roundDamage = new List<string>(); 
    private List<GameObject> instantiatedCards = new List<GameObject>();
    public Transform handPlayerTransform;
    public Transform handOpponentTransform;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void SetupBoardCards(List<CarteData> cardsOpponent, List<CarteData> cardsPlayer)
    {        
        // Instancier les cartes de l'adversaire (4 premières)
        foreach (var card in cardsOpponent)
        {
            GameObject carteGO = Instantiate(cartePrefab, handOpponentTransform);
            CardUI cardUI = carteGO.GetComponent<CardUI>();
            cardUI.isCardOpponent = true;
            cardUI.setAttributesInitCard(card);
            instantiatedCards.Add(carteGO);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(handOpponentTransform as RectTransform);

        // Instancier les cartes du joueur (4 dernières)
        foreach (var card in cardsPlayer)
        {
            GameObject carteGO = Instantiate(cartePrefab, handPlayerTransform);
            CardUI cardUI = carteGO.GetComponent<CardUI>();
            cardUI.isCardPlayer = true;
            cardUI.setAttributesInitCard(card);
            instantiatedCards.Add(carteGO);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(handPlayerTransform as RectTransform);
        
        foreach (var go in instantiatedCards)
        {
            CardUI cardUI = go.GetComponent<CardUI>();
            InitializeCardOnBoard(cardUI);
        }
    }
    
    public void InitializeCardOnBoard(CardUI cardUI)
    {
        CarteBoardInteraction interaction = cardUI.GetComponent<CarteBoardInteraction>();
        RectTransform rectTransform = cardUI.GetComponent<RectTransform>();

        interaction.isCardPlayer = cardUI.isCardPlayer;
        interaction.isCardOpponent = cardUI.isCardOpponent;
        interaction.startPosition = (Vector3)cardUI.GetComponent<RectTransform>().anchoredPosition;
        interaction.newPosition = interaction.startPosition + (interaction.isCardPlayer ? Vector3.up * 50f : Vector3.down * 50f);
    }


    public void OnPassed(GameObject buttonObject)
    {
        /*
        carteUI.AfficherIconePassed();
                
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
            carteUI?.defenseText?.SetText(newDef.ToString());
            SetDefenseValue(newDef);
            //PanelManager.instance?.AddLog($"{nameCard} : PASSER sélectionné (+1 défense)");
        }
        else if(nameCard == "Cassandre"){
            int index = carteUI.indexHierarchieOriginal;
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

    public void OnAttack(GameObject buttonObject)
    {
        /*GameManager.SetMode("atk");
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
                    target.carteUI?.ShowAttackIcon(target.isCibledCount);
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

    public void selectCardOnBoard(CardUI cardUI)
    {
        cardUI.selectCard();
    }









/*



    public IEnumerator HandleNextTurnTransition()
    {
        // 1) Fade des cartes de 1 à 0 (disparition)
        yield return StartCoroutine(FadeYellowCards(1f, 0f, 0.5f));
        yield return new WaitForSeconds(1f);

        // 2) Remplacement des cartes après le fade
        CarteBoardInteraction interactionBoard = FindFirstObjectByType<CarteBoardInteraction>();
        BoardManager.Instance.ReplaceOpponentYellowCards();

        ResetBoardForNextTurn();
    }
    
    public void ResetBoardForNextTurn()
    {    
        GameManager.numberOfAttacksUsed = 0;
        foreach(CarteBoardInteraction card in CarteBoardInteraction.AllCardsInteractions){

            card.ResetIcon(card);
            card.RestoreCardColor(card);
            card.ResetPosition();
            card.DestroyButton();
            card.isCibledCount = 0;
            card.stateDefensif = "notCibled";
            card.stateOffensif = "waitOrder";
            card.choiceDo = false;
            card.isSelected = false;
            card.lastTarget = card.currentTarget;
            card.currentTarget = "";
            card.layoutGroup.enabled = true;

            // reset des bonus/malus
            card.ResetAllBonusMalus(card);
        }
    }

    public void ResetAllCardsPositions()
    {
        foreach(CarteBoardInteraction card in CarteBoardInteraction.AllCardsInteractions)
        {
            card.rectTransform.anchoredPosition = card.startPosition;
        }
    }

    public void AutoPassLastCards()
    {        
        foreach (CarteBoardInteraction card in CarteBoardInteraction.AllCardsInteractions)
        {
            if (card.isCardPlayer && !card.choiceDo)
                card.AutoPass();
        }
    }
    

    public (CarteBoardInteraction leftCard, CarteBoardInteraction rightCard) GetAdjacentCards(int index, string team)
    {
        List<CarteBoardInteraction> allCards = CarteBoardInteraction.AllCardsInteractions;

        CarteBoardInteraction leftCard = allCards.Find(c =>
        {
            var carteUI = c.GetComponent<CarteUI>();
            if (carteUI == null) return false;

            bool isTeamMatch = (team == "opponent" && c.isCardOpponent) ||
                            (team == "player" && c.isCardPlayer);

            return isTeamMatch && carteUI.indexHierarchieOriginal == index - 1;
        });

        CarteBoardInteraction rightCard = allCards.Find(c =>
        {
            var carteUI = c.GetComponent<CarteUI>();
            if (carteUI == null) return false;

            bool isTeamMatch = (team == "opponent" && c.isCardOpponent) ||
                            (team == "player" && c.isCardPlayer);

            return isTeamMatch && carteUI.indexHierarchieOriginal == index + 1;
        });

        return (leftCard, rightCard);
        
    }


    public void MarkEndOfTurn()
    {
        // Si l'IA est active, simuler les attaques de l'IA
        // Les attaques du joueur sont stockées dans attaquesDuTour et seront appliquées
        // avec les attaques de l'IA à la fin du tour complet dans ExecuteAITurn()
        if (GameManager.iaActive)
        {            
            //PanelManager.instance?.AddLog("[IA] Lancement");
        
            Invoke("StartAI", 0.2f);
            
            if (roundDamage.Count > 0)
            {
                //PanelManager.instance.AddLog("------");
                //foreach (string calcul in roundDamage)
                    //PanelManager.instance.AddLog(calcul);
            }
            roundDamage.Clear();
            
            //PanelManager.instance.AddLog($"--- SCORE : {GameManager.playerScore} points ---");
        }
        else
        {            
            //BoardManager.Instance.ShowButtonNextStep(true);
        }
        GameManager.currentRound++;
    }

    private void StartAI()
    {
        IA.Instance.StartCoroutine(IA.Instance.StartAITurnCoroutine());
    }


    private IEnumerator FadeYellowCards(float fromAlpha, float toAlpha, float duration)
    {
        var yellowCards = CarteBoardInteraction.AllCardsInteractions
        .Where(c => c.yellowCard && (c.isCardPlayer || c.isCardOpponent))
        .ToList();

        foreach (var card in yellowCards)
        {
            var anim = card.GetComponent<CardAnimations>();
            var img = card.GetComponentInChildren<Image>();

            if (anim != null && img != null)
            {
                anim.targetImage = img;
                Debug.Log($"Fading card {card.name}, img={img}");

                yield return StartCoroutine(anim.Fade(card.GetComponent<CarteUI>(), fromAlpha, toAlpha, duration));
            }

        }
    }

    public void ReplaceOpponentYellowCards()
    {
        List<CarteBoardInteraction> allCards = CarteBoardInteraction.AllCardsInteractions;

        var yellowOpponent = allCards.Where(c => c.yellowCard && c.isCardOpponent).ToList();
        var yellowPlayer = allCards.Where(c => c.yellowCard && c.isCardPlayer).ToList();
        
        if (yellowOpponent.Count == 0 && yellowPlayer.Count == 0) 
            return;
        
        var deckOpponent = GameManager.Instance.piochePlayerB;
        var deckPlayer = GameManager.Instance.piochePlayerA;

        var cartesIntoBoardOpponent = allCards.Where(c => c.isCardOpponent && c.carteUI != null)
                                           .Select(c => c.carteUI.carteID).ToHashSet();

        var cartesIntoBoardPlayer = allCards.Where(c => c.isCardPlayer && c.carteUI != null)
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
            BoardManager.Instance.InitializeCardOnBoard(carteUI);
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
            BoardManager.Instance.InitializeCardOnBoard(carteUI);
        }
        GameManager.Instance.CheckGameOver();
    }

    */

} 
