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
    private bool hasLoggedWaitingPlayer;
    private bool aiTurnLaunched;
    private bool roundResolutionInProgress;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    void Update()
    {
        if (GameManager.Instance.mode == "deck") return;

        if(GameManager.Instance.currentPlayerAction != "NONE")
        {
            if (GameManager.Instance.currentPlayerAction == "AI" && !aiTurnLaunched && !GameManager.Instance.isEndturnAI)
            {
                StartCoroutine(StartAITurnWithDelay(1.5f));
                aiTurnLaunched = true; // S'assurer que l'IA ne démarre qu'une fois
                GameManager.Instance.isEndturnAI = false; // Le tour IA n'est terminé qu'à la fin de ExecuteAITurn()
                hasLoggedWaitingPlayer = false;
            }else
            {
                if (GameManager.Instance.currentPlayerAction == "UI" && !GameManager.Instance.isEndturnPlayer && !hasLoggedWaitingPlayer)
                {
                    Debug.Log($"[BOARD] En attente du joueur (isEndturnPlayer: {GameManager.Instance.isEndturnPlayer})");
                    hasLoggedWaitingPlayer = true;
                }

                // Vérifie si toutes les cartes joueur ont fait leur choix
                var activePlayerCards = cardsOnBoardUI.Where(c => c != null && !c.isHiddenSlot).ToList();

                if (activePlayerCards.All(card => card.actionChoiceDo))
                {
                    GameManager.Instance.isEndturnPlayer = true;
                    hasLoggedWaitingPlayer = false;

                    Debug.Log($"[BOARD] ===== FIN DU TOUR JOUEUR =====");

                    int visiblePlayerCards = activePlayerCards.Count;
                    int attacksCount = activePlayerCards.Count(c => c.stateOffensif == "atk");
                    int passesCount = activePlayerCards.Count(c => c.stateOffensif == "passed");

                    Debug.Log($"[BOARD] Toutes les cartes joueur ont fait leur choix ({visiblePlayerCards} cartes)");
                    Debug.Log($"[BOARD] Résumé - Attaques: {attacksCount}, Passes: {passesCount}");

                    // Si le joueur termine en premier, on passe immédiatement la main à l'IA.
                    if (!GameManager.Instance.isEndturnAI)
                    {
                        GameManager.Instance.currentPlayerAction = "AI";
                        PanelManager.Instance?.ShowTurnBanner("AI");
                        Debug.Log("[GAME] Transition de tour: JOUEUR -> IA");
                    }
                }
            }
        }

        if(GameManager.Instance.isEndturnPlayer && GameManager.Instance.isEndturnAI && !roundResolutionInProgress){
            Debug.Log($"[BOARD] Les deux joueurs ont terminé - Application des choix");
            StartCoroutine(ResolveRoundCoroutine());
        }
    }

    public void SetupBoardCards(List<CarteData> cardsOpponent, List<CarteData> cardsPlayer)
    {
        Debug.Log($"[BOARD] ===== SETUP DU PLATEAU =====");
        
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

            var attackerCard = cardsOnBoardUI.FirstOrDefault(c => c.idCard == idAttacker);
            if (attackerCard != null)
            {
                Debug.Log($"[BOARD] Cible assignée: {attackerCard.nameCard} → {cardAI.nameCard}");
                attackerCard.SetDataTarget(cardAI);
                GameManager.Instance.mode = "selectCardToPlayAction";
                Debug.Log($"[BOARD] Mode changé: {GameManager.Instance.mode}");
            }
        }
    }

    // déselectionner toutes les autres cartes
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

    private IEnumerator ResolveRoundCoroutine()
    {
        roundResolutionInProgress = true;
        GameManager.Instance.currentPlayerAction = "NONE";

        yield return StartCoroutine(IA.Instance.ApplyAllBonus());
        yield return StartCoroutine(IA.Instance.ApplyAllAttacksCoroutine());

        yield return StartCoroutine(IA.Instance.ApplyAllMalusEndTurn());

        aiTurnLaunched = false;
        hasLoggedWaitingPlayer = false;
        roundResolutionInProgress = false;
    }

    public void ResetBoardForNextTurn(){
        GameManager.Instance.isEndturnPlayer = false;
        GameManager.Instance.isEndturnAI = false;
        aiTurnLaunched = false;
        roundResolutionInProgress = false;
        hasLoggedWaitingPlayer = false;

        GameManager.Instance.mode = "selectCardToPlayAction";

        foreach (CardUI card in cardsOnBoardUI)
        {
            card.ResetCardEndTurn();
        }
        foreach (CardAI card in cardsOnBoardAI)
        {
            card.ResetCardEndTurn();
        }

        yield return StartCoroutine(BoardManager.Instance.HandleNextTurnTransition());

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
                // Plus de remplaçante : on masque définitivement le slot
                card.HideAsEmptySlot();
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
                // Plus de remplaçante : on masque définitivement le slot
                card.HideAsEmptySlot();
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

    IEnumerator StartAITurnWithDelay(float delay)
    {
        Debug.Log($"[AI] Réflexion en cours... (delay: {delay}s, timeScale: {Time.timeScale})");
        yield return new WaitForSecondsRealtime(delay);
        Debug.Log($"[AI] Fin réflexion, tentative de lancement du tour IA (timeScale: {Time.timeScale})");

        IA ai = IA.Instance;
        Debug.Log($"[AI] IA trouvée: {ai.name} (activeInHierarchy: {ai.gameObject.activeInHierarchy})");
        ai.StartAITurn();
    }

    public IEnumerator HandleNextTurnTransition()
    {
        Debug.Log("[BOARD] Transition fin de tour - début fade");

        // 1) Fade des cartes de 1 à 0 (disparition)
        yield return StartCoroutine(FadeYellowCards(1f, 0f, 0.5f));
        yield return new WaitForSeconds(0.2f);

        Debug.Log("[BOARD] Remplacement des cartes jaunes");
    }

    private IEnumerator FadeYellowCards(float fromAlpha, float toAlpha, float duration)
    {
        var yellowCardsUI = BoardManager.cardsOnBoardUI
            .Where(c => c != null && c.yellowCard)
            .ToList();

        var yellowCardsAI = BoardManager.cardsOnBoardAI
            .Where(c => c != null && c.yellowCard)
            .ToList();

        foreach (var card in yellowCardsUI)
        {
            var anim = card.GetComponent<CardAnimations>();
            var img = card.GetComponentInChildren<Image>();

            if (anim == null || img == null)
                continue;

            anim.targetImage = img;

            yield return StartCoroutine(
                anim.Fade(card, fromAlpha, toAlpha, duration)
            );
        }

        foreach (var card in yellowCardsAI)
        {
            var anim = card.GetComponent<CardAnimations>();
            var img = card.GetComponentInChildren<Image>();

            if (anim == null || img == null)
                continue;

            anim.targetImage = img;

            yield return StartCoroutine(
                anim.Fade(card, fromAlpha, toAlpha, duration)
            );
        }
    }

    public (ICard left, ICard right) GetAdjacentCards(ICard card)
    {
        if (card is CardUI ui)
        {
            var (l, r) = GetAdjacentCards(ui);
            return (l, r);
        }

        if (card is CardAI ai)
        {
            var (l, r) = GetAdjacentCards(ai);
            return (l, r);
        }

        return (null, null);
    }

    // ==========================================
    // SYNC
    // ==========================================

    /// Retire une carte de mainPlayerB (carte masquée sans remplaçant).
    void SyncRemoveFromMainPlayerB(string instanceId)
    {
        var gm = GameManager.Instance;
        if (gm.mainPlayerB == null) return;
        var list = gm.mainPlayerB.Where(c => c.instanceId != instanceId).ToList();
        gm.mainPlayerB = new Queue<CarteData>(list);
    }
    /// Remplace une carte dans mainPlayerB (carte remplacée depuis la pioche)
    void SyncReplaceInMainPlayerB(string oldInstanceId, CarteData newCard)
    {
        var gm = GameManager.Instance;
        if (gm.mainPlayerB == null) return;
        var list = gm.mainPlayerB.Where(c => c.instanceId != oldInstanceId).ToList();
        list.Add(newCard);
        gm.mainPlayerB = new Queue<CarteData>(list);
    }

    /// Retire une carte de mainPlayerA (carte masquée sans remplaçant)
    void SyncRemoveFromMainPlayerA(string instanceId)
    {
        var gm = GameManager.Instance;
        if (gm.mainPlayerA == null) return;
        var list = gm.mainPlayerA.Where(c => c.instanceId != instanceId).ToList();
        gm.mainPlayerA = new Queue<CarteData>(list);
    }
    /// Remplace une carte dans mainPlayerA (carte remplacée depuis la pioche)
    void SyncReplaceInMainPlayerA(string oldInstanceId, CarteData newCard)
    {
        var gm = GameManager.Instance;
        if (gm.mainPlayerA == null) return;
        var list = gm.mainPlayerA.Where(c => c.instanceId != oldInstanceId).ToList();
        list.Add(newCard);
        gm.mainPlayerA = new Queue<CarteData>(list);
    }
} 
