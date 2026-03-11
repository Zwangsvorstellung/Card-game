using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using TMPro;

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


    bool aiTurnStarted = false;


    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Update()
    {
        if (GameManager.Instance.mode == "deck") return;


        // Vérifie si toutes les cartes joueur ont fait leur choix
        if(cardsOnBoardUI.All(card => card.actionChoiceDo) && !GameManager.Instance.isEndturnPlayer && GameManager.Instance.currentPlayerAction == "UI"){
            GameManager.Instance.isEndturnPlayer = true;
            Debug.Log($"[BOARD] ===== FIN DU TOUR JOUEUR =====");
            Debug.Log($"[BOARD] Toutes les cartes joueur ont fait leur choix ({cardsOnBoardUI.Count} cartes)");
            int attacksCount = cardsOnBoardUI.Count(c => c.stateOffensif == "atk");
            int passesCount = cardsOnBoardUI.Count(c => c.stateOffensif == "passed");
            Debug.Log($"[BOARD] Résumé - Attaques: {attacksCount}, Passes: {passesCount}");
            GameManager.Instance.currentPlayerAction = "AI";

            Debug.Log($"[BOARD] aiTurnStarted={aiTurnStarted} avant lancement IA");

            if(!aiTurnStarted)
            {
                aiTurnStarted = true;
                StartCoroutine(StartAITurnWithDelay(1.5f));
            }
        }
        
        // Cas où l'IA doit commencer le round (playerStarts = false)
        if (GameManager.Instance.currentPlayerAction == "AI" && !aiTurnStarted && !GameManager.Instance.isEndturnAI)
        {
            aiTurnStarted = true;
            StartCoroutine(StartAITurnWithDelay(1.5f));
        }
        
        // Vérifie si toutes les cartes IA ont fait leur choix
        if(cardsOnBoardAI.All(card => card.actionChoiceDo) && !GameManager.Instance.isEndturnAI && GameManager.Instance.currentPlayerAction == "AI"){
            GameManager.Instance.isEndturnAI = true;
            aiTurnStarted = false;

            Debug.Log($"[BOARD] ===== FIN DU TOUR IA =====");
            Debug.Log($"[BOARD] Toutes les cartes IA ont fait leur choix ({cardsOnBoardAI.Count} cartes)");
            int attacksCount = cardsOnBoardAI.Count(c => c.stateOffensif == "atk");
            int passesCount = cardsOnBoardAI.Count(c => c.stateOffensif == "passed");
            Debug.Log($"[BOARD] Résumé - Attaques: {attacksCount}, Passes: {passesCount}");
            GameManager.Instance.currentPlayerAction = "UI";
        }
        
        // Si les deux ont fini, les attaques seront appliquées
        if(GameManager.Instance.isEndturnPlayer && GameManager.Instance.isEndturnAI){
            // Debug.Log($"[BOARD] Les deux joueurs ont terminé - Application des attaques en cours...");
            GameManager.Instance.currentPlayerAction = "NONE";
        }
        
    }

    public void SetupBoardCards(List<CarteData> cardsOpponent, List<CarteData> cardsPlayer)
    {
        Debug.Log($"[BOARD] ===== SETUP DU PLATEAU =====");
        Debug.Log($"[BOARD] Cartes adversaire: {cardsOpponent.Count}, Cartes joueur: {cardsPlayer.Count}");
        
        // Instancier les cartes de l'adversaire (4 premières)
        foreach (var card in cardsOpponent)
        {
            GameObject carteGO = Instantiate(cartePrefabAI, handOpponentTransform);
            CardAI cardAI = carteGO.GetComponent<CardAI>();
            cardAI.isCardOpponent = true;
            cardAI.setAttributesInitCardAI(card);
            instantiatedCards.Add(carteGO);
            Debug.Log($"[BOARD] Carte IA créée: {card.nom} (ATK:{card.attaque}, DEF:{card.defense})");
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
            Debug.Log($"[BOARD] Carte joueur créée: {card.nom} (ATK:{card.attaque}, DEF:{card.defense})");
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(handPlayerTransform as RectTransform);
        
        Debug.Log($"[BOARD] Plateau configuré - Cartes IA: {cardsOnBoardAI.Count}, Cartes joueur: {cardsOnBoardUI.Count}");
    }

    // selection de la carte qui va jouer
    public void selectCardOnBoard(CardUI cardUI)
    {
        if(GameManager.Instance.mode != "selectCardOpponentToAttack"){
            Debug.Log($"[BOARD] Sélection de la carte joueur: {cardUI.nameCard}");
            cardUI.selectCard();
        }
    }

    // selection de la cible à attaquer 
    public void selectCardOpponentOnBoard(CardAI cardAI)
    {
        if(GameManager.Instance.mode == "selectCardOpponentToAttack"){
            Debug.Log($"[BOARD] Sélection de la cible IA: {cardAI.nameCard}");
            int idAttacker = cardAI.isSelectCard();

            var card = cardsOnBoardUI.FirstOrDefault(c => c.idCard == idAttacker);
            if (card != null)
            {
                Debug.Log($"[BOARD] Cible assignée: {card.nameCard} → {cardAI.nameCard}");
                card.SetDataTarget(cardAI);
                GameManager.Instance.mode = "selectCardToPlayAction";
                Debug.Log($"[BOARD] Mode changé: {GameManager.Instance.mode}");
            }

        }
    }

    public void DeselectAllOtherCards()
    {
        foreach (var card in cardsOnBoardUI.Where(c => c.isSelect && !c.actionChoiceDo))
        {
            card.Deselect();
            card.HideActionButtons();
        }
    }

    public CardUI GetDataAttacker()
    {
        return cardsOnBoardUI.FirstOrDefault(card => card.stateOffensif == "selectTarget");
    }

    public void ResetBoardForNextTurn(){
        GameManager.Instance.isEndturnPlayer = false;
        GameManager.Instance.isEndturnAI = false;
        aiTurnStarted = false;
        GameManager.Instance.mode = "selectCardToPlayAction";

        foreach (CardUI card in cardsOnBoardUI)
        {
            card.ResetCardEndTurn();
        }
        foreach (CardAI card in cardsOnBoardAI)
        {
            card.ResetCardEndTurn();
        }

        ReplaceOpponentYellowCards();
    }

    public void ReplaceOpponentYellowCards()
    {
        var yellowOpponent = cardsOnBoardAI.Where(c => c.isYellow).ToList();
        var yellowPlayer = cardsOnBoardUI.Where(c => c.isYellow).ToList();

        if (yellowOpponent.Count == 0 && yellowPlayer.Count == 0) 
            return;

        var deckPlayer = GameManager.Instance.piochePlayerA;
        var deckOpponent = GameManager.Instance.piochePlayerB;

        Debug.Log($"Pioche deckPlayer contient {GameManager.Instance.piochePlayerA.Count} cartes :");
        Debug.Log($"Pioche deckOpponent contient {GameManager.Instance.piochePlayerB.Count} cartes :");


        var cartesIntoBoardOpponent = cardsOnBoardAI
                                        .Select(c => c.idCard)
                                        .ToHashSet();

        var cartesIntoBoardPlayer = cardsOnBoardUI
                                        .Select(c => c.idCard)
                                        .ToHashSet();

        var availableCardsOpponent = deckOpponent
                                    .Where(c => !cartesIntoBoardOpponent.Contains(c.idCard))
                                    .ToList();

        var availableCardsPlayer = deckPlayer
                                    .Where(c => !cartesIntoBoardPlayer.Contains(c.idCard))
                                    .ToList();

        foreach (CardAI card in yellowOpponent)
        {
            if (availableCardsOpponent.Count == 0)
            {
                // Plus de remplaçante : rendre invisibles tous les enfants de la carte
                foreach (Transform child in card.transform)
                {
                    child.gameObject.SetActive(false);
                }
                // Synchroniser mainPlayerB : retirer la carte éliminée
                SyncRemoveFromMainPlayerB(card.instanceId);
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
            string oldInstanceId = card.instanceId;

            GameObject.DestroyImmediate(card.gameObject);

            GameObject carteGO = GameObject.Instantiate(BoardManager.Instance.cartePrefabAI, parent);
            carteGO.transform.SetSiblingIndex(siblingIndex);

            // Réappliquer la position exacte
            RectTransform rtNewCard = carteGO.GetComponent<RectTransform>();
            rtNewCard.anchoredPosition = oldInitialPosition;

            CardAI cardAI = carteGO.GetComponent<CardAI>();
            cardAI.setAttributesInitCardAI(newCard);
            // Synchroniser mainPlayerB : retirer l'ancienne carte, ajouter la nouvelle
            SyncReplaceInMainPlayerB(oldInstanceId, newCard);
        }

        foreach (CardUI card in yellowPlayer)
        {
            if (availableCardsPlayer.Count == 0)
            {
                // Plus de remplaçante : rendre invisibles tous les enfants de la carte
                foreach (Transform child in card.transform)
                {
                    child.gameObject.SetActive(false);
                }
                // Synchroniser mainPlayerA : retirer la carte éliminée
                SyncRemoveFromMainPlayerA(card.instanceId);
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
            Vector3 oldInitialPosition = card.startPosition;
            string oldInstanceId = card.instanceId;

            GameObject.DestroyImmediate(card.gameObject);

            GameObject carteGO = GameObject.Instantiate(BoardManager.Instance.cartePrefab, parent);
            carteGO.transform.SetSiblingIndex(siblingIndex);

            // Réappliquer la position exacte
            RectTransform rtNewCard = carteGO.GetComponent<RectTransform>();
            rtNewCard.anchoredPosition = oldInitialPosition;

            CardUI cardUI = carteGO.GetComponent<CardUI>();
            cardUI.setAttributesInitCardPlayer(newCard);
            // Synchroniser mainPlayerA : retirer l'ancienne carte, ajouter la nouvelle
            SyncReplaceInMainPlayerA(oldInstanceId, newCard);
        }
    }

    /// <summary>Retire une carte de mainPlayerB (carte masquée sans remplaçant).</summary>
    void SyncRemoveFromMainPlayerB(string instanceId)
    {
        var gm = GameManager.Instance;
        if (gm.mainPlayerB == null) return;
        var list = gm.mainPlayerB.Where(c => c.instanceId != instanceId).ToList();
        gm.mainPlayerB = new Queue<CarteData>(list);
    }

    /// <summary>Remplace une carte dans mainPlayerB (carte remplacée depuis la pioche).</summary>
    void SyncReplaceInMainPlayerB(string oldInstanceId, CarteData newCard)
    {
        var gm = GameManager.Instance;
        if (gm.mainPlayerB == null) return;
        var list = gm.mainPlayerB.Where(c => c.instanceId != oldInstanceId).ToList();
        list.Add(newCard);
        gm.mainPlayerB = new Queue<CarteData>(list);
    }

    /// <summary>Retire une carte de mainPlayerA (carte masquée sans remplaçant).</summary>
    void SyncRemoveFromMainPlayerA(string instanceId)
    {
        var gm = GameManager.Instance;
        if (gm.mainPlayerA == null) return;
        var list = gm.mainPlayerA.Where(c => c.instanceId != instanceId).ToList();
        gm.mainPlayerA = new Queue<CarteData>(list);
    }

    /// <summary>Remplace une carte dans mainPlayerA (carte remplacée depuis la pioche).</summary>
    void SyncReplaceInMainPlayerA(string oldInstanceId, CarteData newCard)
    {
        var gm = GameManager.Instance;
        if (gm.mainPlayerA == null) return;
        var list = gm.mainPlayerA.Where(c => c.instanceId != oldInstanceId).ToList();
        list.Add(newCard);
        gm.mainPlayerA = new Queue<CarteData>(list);
    }

    /// <summary>
    /// Lance le tour de l'IA si ce n'est pas déjà fait. Appelé par GameManager quand l'IA doit commencer le round.
    /// </summary>
    public void StartAITurnIfNeeded()
    {
        if (!aiTurnStarted)
        {
            aiTurnStarted = true;
            StartCoroutine(StartAITurnWithDelay(1.5f));
        }
    }

    IEnumerator StartAITurnWithDelay(float delay)
    {
        Debug.Log("[AI] Réflexion en cours...");
        yield return new WaitForSeconds(delay);
        IA.Instance.StartAITurn();
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
