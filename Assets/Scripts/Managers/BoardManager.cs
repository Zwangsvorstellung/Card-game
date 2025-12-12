using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    public GameObject cartePrefab;
    public GameObject cartePrefabAI;
    public  List<string> roundDamage = new List<string>(); 
    private List<GameObject> instantiatedCards = new List<GameObject>();
    public static readonly List<CardUI> cardsOnBoardUI = new List<CardUI>();
    public static readonly List<CardAI> cardsOnBoardAI = new List<CardAI>();
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
            GameObject carteGO = Instantiate(cartePrefabAI, handOpponentTransform);
            CardAI cardAI = carteGO.GetComponent<CardAI>();
            cardAI.isCardOpponent = true;
            cardAI.setAttributesInitCardAI(card);
            instantiatedCards.Add(carteGO);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(handOpponentTransform as RectTransform);

        // Instancier les cartes du joueur (4 dernières)
        foreach (var card in cardsPlayer)
        {
            GameObject carteGO = Instantiate(cartePrefab, handPlayerTransform);
            CardUI cardUI = carteGO.GetComponent<CardUI>();
            cardUI.isCardPlayer = true;
            cardUI.setAttributesInitCardPlayer(card);
            instantiatedCards.Add(carteGO);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(handPlayerTransform as RectTransform);
    }

    // selection de la carte qui va jouer
    public void selectCardOnBoard(CardUI cardUI)
    {
        if(GameManager2.Instance.mode != "selectCardOpponentToAttack"){
            cardUI.selectCard();
        }
    }

    // selection de la cible à attaquer 
    public void selectCardOpponentOnBoard(CardAI cardAI)
    {
        if(GameManager2.Instance.mode == "selectCardOpponentToAttack"){
            int idAttacker = cardAI.isSelectCard();

            foreach (CardUI card in cardsOnBoardUI)
            {
                if (card.idCard == idAttacker)
                {
                    card.SetDataTarget(cardAI);
                    GameManager2.Instance.mode = "selectCardToPlayAction";
                    break;
                }
            }
        }
    }

    public void DeselectAllOtherCards()
    {
        foreach (CardUI card in cardsOnBoardUI)
        {
            if (card.isSelect && !card.actionChoiceDo)
            {
                card.Deselect();
                card.HideActionButtons();
            }
        }
    }

    public CardUI getDataAttacker()
    {
        foreach (CardUI card in cardsOnBoardUI)
        {
            if (card.stateOffensif == "atk")
            {
                return card;
            }
        }
        return null;
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
