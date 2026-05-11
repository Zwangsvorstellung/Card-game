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
    private bool resolvingRound = false;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    void Update()
    {
        if (GameManager.Instance.mode == GameMode.DECK) return;

        if (GameManager.Instance.currentPlayerAction != PlayerActionState.NONE)
        {
            if (GameManager.Instance.currentPlayerAction == PlayerActionState.AI && !aiTurnLaunched && !GameManager.Instance.isEndturnAI)
            {
                StartCoroutine(StartAITurnWithDelay(1.5f));
                aiTurnLaunched = true; // S'assurer que l'IA ne démarre qu'une fois
                GameManager.Instance.isEndturnAI = false; // Le tour IA n'est terminé qu'à la fin de ExecuteAITurn()
                hasLoggedWaitingPlayer = false;
            }else
            {
                if (GameManager.Instance.currentPlayerAction == PlayerActionState.UI && !GameManager.Instance.isEndturnPlayer && !hasLoggedWaitingPlayer)
                {
                    Debug.Log($"[BOARD] En attente du joueur (isEndturnPlayer: {GameManager.Instance.isEndturnPlayer})");
                    hasLoggedWaitingPlayer = true;
                }
            }
        }

        if (GameManager.Instance.isEndturnPlayer &&
            GameManager.Instance.isEndturnAI &&
            !resolvingRound)
        {
            PanelManager.Instance?.OffTurnBanner();
            resolvingRound = true;
            StartCoroutine(ResolveRoundCoroutine());
        }
    }

    public void CheckPlayerCardsDone()
    {
        if (GameManager.Instance.currentPlayerAction != PlayerActionState.UI)
            return;

        var activeCards = cardsOnBoardUI
            .Where(c => c != null && !c.isHiddenSlot)
            .ToList();

        Debug.Log($"[CHECK] COUNT={activeCards.Count}");

        if (activeCards.Count == 0)
            return;

        bool allDone = true;

        foreach (var c in activeCards)
        {
            if (!c.actionChoiceDo)
            {
                allDone = false;
                break;
            }
        }

        Debug.Log($"[CHECK] ALL DONE = {allDone}");

        if (!allDone) return;

        // 🔒 protection anti double trigger
        if (GameManager.Instance.isEndturnPlayer)
            return;

        Debug.Log("✅ PLAYER TURN COMPLETE");

        GameManager.Instance.isEndturnPlayer = true;

        if (!GameManager.Instance.isEndturnAI)
        {
            GameManager.Instance.currentPlayerAction = PlayerActionState.AI;
            PanelManager.Instance?.ShowTurnBanner(PlayerActionState.AI);
        }
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
        if(GameManager.Instance.mode != GameMode.SELECT_CARD_OPPONENT_TO_ATTACK){
            cardUI.selectCard();
        }
    }

    // selection manuelle de la cible à attaquer (JOUEUR)
    public void selectCardOpponentOnBoard(CardAI cardAI)
    {
        if(GameManager.Instance.mode == GameMode.SELECT_CARD_OPPONENT_TO_ATTACK){
            int idAttacker = cardAI.isSelectCard();
            var attackerCard = cardsOnBoardUI.FirstOrDefault(c => c.idCard == idAttacker);
            attackerCard.SetDataTarget(cardAI);
            GameManager.Instance.mode = GameMode.SELECT_CARD_TO_PLAY_ACTION;
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
        return cardsOnBoardUI.FirstOrDefault(card => card.stateOffensif == OffensiveState.SELECT_TARGET);
    }

    private IEnumerator ResolveRoundCoroutine()
    {
        roundResolutionInProgress = true;
        GameManager.Instance.currentPlayerAction = PlayerActionState.NONE;

        yield return StartCoroutine(IA.Instance.ApplyAllBonus());
        yield return StartCoroutine(IA.Instance.ApplyAllAttacksCoroutine());
        //yield return StartCoroutine(IA.Instance.ApplyAllMalusEndTurn());

        aiTurnLaunched = false;
        hasLoggedWaitingPlayer = false;
        roundResolutionInProgress = false;
        resolvingRound = false;
        GameManager.Instance.isEndturnPlayer = false;
        GameManager.Instance.isEndturnAI = false;
    }

    public IEnumerator ResetBoardForNextTurn()
    {
        GameManager.Instance.isEndturnPlayer = false;
        GameManager.Instance.isEndturnAI = false;
        aiTurnLaunched = false;
        roundResolutionInProgress = false;
        hasLoggedWaitingPlayer = false;
        GameManager.Instance.mode = GameMode.SELECT_CARD_TO_PLAY_ACTION;

        yield return BoardManager.Instance.HandleNextTurnTransition();
        ReplaceOpponentYellowCards();

        foreach (CardUI card in cardsOnBoardUI)
        {
            card.ResetCardEndTurn();
        }
        foreach (CardAI card in cardsOnBoardAI)
        {
            card.ResetCardEndTurn();
        }
    }

    public void ReplaceOpponentYellowCards()
    {
        var yellowAI = cardsOnBoardAI.Where(c => c.isYellow).ToList();
        var yellowUI = cardsOnBoardUI.Where(c => c.isYellow).ToList();

        if (yellowAI.Count == 0 && yellowUI.Count == 0) 
            return;

        var deckUI = GameManager.Instance.piochePlayerUI;
        var deckAI = GameManager.Instance.piochePlayerAI;

        var cardsIntoBoardAI = cardsOnBoardAI.Select(c => c.idCard).ToHashSet();
        var cardsIntoBoardUI = cardsOnBoardUI.Select(c => c.idCard).ToHashSet();

        var availableCardsAI = deckAI.Where(c => !cardsIntoBoardAI.Contains(c.idCard)).ToList();
        var availableCardsUI = deckUI.Where(c => !cardsIntoBoardUI.Contains(c.idCard)).ToList();

        foreach (CardAI card in yellowAI)
        {
            if (availableCardsAI.Count == 0)
            {
                // Plus de remplaçante : on masque définitivement le slot
                card.HideAsEmptySlot();
                // Synchroniser mainPlayerAI : retirer la carte éliminée
                SyncRemoveFromMainPlayerAI(card.instanceId);
                continue;
            }
            int idx = Random.Range(0, availableCardsAI.Count);
            var newCard = availableCardsAI[idx];
            availableCardsAI.RemoveAt(idx);
        
            var tempList = deckAI.ToList();
            tempList.Remove(newCard);
            deckAI.Clear();

            foreach (var c in tempList) deckAI.Enqueue(c);

            Transform parent = card.transform.parent;
            int siblingIndex = card.transform.GetSiblingIndex();
            Vector3 oldInitialPosition = card.startPosition;
            string oldInstanceId = card.instanceId;

            GameObject.DestroyImmediate(card.gameObject);

            GameObject carteGO = GameObject.Instantiate(BoardManager.Instance.cartePrefabAI, parent);
            carteGO.transform.SetSiblingIndex(siblingIndex);

            RectTransform rtNewCard = carteGO.GetComponent<RectTransform>();
            rtNewCard.anchoredPosition = oldInitialPosition;

            CardAI cardAI = carteGO.GetComponent<CardAI>();
            cardAI.setAttributesInitCardAI(newCard);
            // Synchroniser mainPlayerAI : retirer l'ancienne carte, ajouter la nouvelle
            SyncReplaceInMainPlayerAI(oldInstanceId, newCard);
        }

        foreach (CardUI card in yellowUI)
        {
            if (availableCardsUI.Count == 0)
            {
                // Plus de remplaçante : on masque définitivement le slot
                card.HideAsEmptySlot();
                // Synchroniser mainPlayerUI : retirer la carte éliminée
                SyncRemoveFromMainPlayerUI(card.instanceId);
                continue;
            }
            int idx = Random.Range(0, availableCardsUI.Count);
            var newCard = availableCardsUI[idx];
            availableCardsUI.RemoveAt(idx);
        
            var tempList = deckUI.ToList();
            tempList.Remove(newCard);
            deckUI.Clear();

            foreach (var c in tempList) deckUI.Enqueue(c);

            Transform parent = card.transform.parent;
            int siblingIndex = card.transform.GetSiblingIndex();
            Vector3 oldInitialPosition = card.startPosition;
            string oldInstanceId = card.instanceId;

            GameObject.DestroyImmediate(card.gameObject);

            GameObject carteGO = GameObject.Instantiate(BoardManager.Instance.cartePrefab, parent);
            carteGO.transform.SetSiblingIndex(siblingIndex);

            RectTransform rtNewCard = carteGO.GetComponent<RectTransform>();
            rtNewCard.anchoredPosition = oldInitialPosition;

            CardUI cardUI = carteGO.GetComponent<CardUI>();
            cardUI.isCardPlayer = true;
            cardUI.startPosition = oldInitialPosition;
            cardUI.GetComponent<RectTransform>().anchoredPosition = oldInitialPosition;
            cardUI.setAttributesInitCardPlayer(newCard);
            // Synchroniser mainPlayerUI : retirer l'ancienne carte, ajouter la nouvelle
            SyncReplaceInMainPlayerUI(oldInstanceId, newCard);
        }
    }

    IEnumerator StartAITurnWithDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        IA ai = IA.Instance;
        ai.StartAITurn();
    }

    public IEnumerator HandleNextTurnTransition()
    {
        Debug.Log("[BOARD] Transition fin de tour - début fade");
        yield return StartCoroutine(FadeYellowCards(1f, 0f, 0.5f));
        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator FadeYellowCards(float fromAlpha, float toAlpha, float duration)
    {
        var yellowCardsUI = BoardManager.cardsOnBoardUI
            .Where(c => c.isYellow).ToList();

        var yellowCardsAI = BoardManager.cardsOnBoardAI
            .Where(c => c.isYellow).ToList();

        foreach (var card in yellowCardsUI)
        {
            var anim = card.GetComponent<CardsAnimation>();
            var img = card.GetComponentInChildren<Image>();
            anim.targetImage = img;
            yield return StartCoroutine(anim.Fade(card.gameObject, fromAlpha, toAlpha, duration));
        }

        foreach (var card in yellowCardsAI)
        {
            var anim = card.GetComponent<CardsAnimation>();
            var img = card.GetComponentInChildren<Image>();
            anim.targetImage = img;
            yield return StartCoroutine(anim.Fade(card.gameObject, fromAlpha, toAlpha, duration));
        }
    }

    public (CardUI left, CardUI right) GetAdjacentCards(CardUI card)
    {
        for (int i = 0; i < BoardManager.cardsOnBoardUI.Count; i++)
        {
            var c = BoardManager.cardsOnBoardUI[i];
            Debug.Log($"[BOARD ORDER] i={i} name={c.nameCard} indexCarte={c.indexCarte}");
        }

        CardUI left = null;
        CardUI right = null;
        var list = BoardManager.cardsOnBoardUI;

        if (card.indexCarte > 0)
            left = list[card.indexCarte - 1];

        if (card.indexCarte < list.Count - 1)
            right = list[card.indexCarte + 1];

        Debug.Log($"[RESULT] left={(left ? left.nameCard : "null")} right={(right ? right.nameCard : "null")}");
        return (left, right);
    }
    // ==========================================
    // SYNC
    // ==========================================
    /// Retire une carte de mainPlayerAI (carte masquée sans remplaçant).
    void SyncRemoveFromMainPlayerAI(string instanceId)
    {
        var gm = GameManager.Instance;
        if (gm.mainPlayerAI == null) return;
        var list = gm.mainPlayerAI.Where(c => c.instanceId != instanceId).ToList();
        gm.mainPlayerAI = new Queue<CarteData>(list);
    }
    /// Remplace une carte dans mainPlayerAI (carte remplacée depuis la pioche)
    void SyncReplaceInMainPlayerAI(string oldInstanceId, CarteData newCard)
    {
        var gm = GameManager.Instance;
        if (gm.mainPlayerAI == null) return;
        var list = gm.mainPlayerAI.Where(c => c.instanceId != oldInstanceId).ToList();
        list.Add(newCard);
        gm.mainPlayerAI = new Queue<CarteData>(list);
    }
    /// Retire une carte de mainPlayerUI (carte masquée sans remplaçant)
    void SyncRemoveFromMainPlayerUI(string instanceId)
    {
        var gm = GameManager.Instance;
        if (gm.mainPlayerUI == null) return;
        var list = gm.mainPlayerUI.Where(c => c.instanceId != instanceId).ToList();
        gm.mainPlayerUI = new Queue<CarteData>(list);
    }
    /// Remplace une carte dans mainPlayerUI (carte remplacée depuis la pioche)
    void SyncReplaceInMainPlayerUI(string oldInstanceId, CarteData newCard)
    {
        var gm = GameManager.Instance;
        if (gm.mainPlayerUI == null) return;
        var list = gm.mainPlayerUI.Where(c => c.instanceId != oldInstanceId).ToList();
        list.Add(newCard);
        gm.mainPlayerUI = new Queue<CarteData>(list);
    }
}