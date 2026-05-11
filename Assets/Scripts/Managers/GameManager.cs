using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public const int MAX_CARTES_TAPIS_SELECT_DECK = 7;
    public const int MAX_CARTES_TAPIS = 4;
    public static int MAX_NUMBER_ATK_ROUND = 2;

    public List<CarteData> deckPlayerUI;
    public List<CarteData> deckPlayerAI;

    public int numberOfAttacksUsedPlayer;
    public int numberOfAttacksUsedIA;

    public MainUIManager mainUIManager;
    public BoardManager boardManager;
    public Queue<CarteData> mainPlayerUI;
    public Queue<CarteData> mainPlayerAI;
    public Queue<CarteData> piochePlayerUI;
    public Queue<CarteData> piochePlayerAI;

    public PlayerActionState currentPlayerAction;

    public bool isEndturnPlayer = false;
    public bool isEndturnAI = false;

    public GameMode mode;
    public bool aiStart;
    public int round;

    [Header("Debug")]
    [SerializeField] private bool limitCardsForDebug = true;
    [SerializeField, Min(5)] private int debugCardsPerDeck = 5;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }
    void Start()
    {
        round = 1;
        mode = GameMode.SELECT_DECK;
                
        deckPlayerUI = new List<CarteData>();
        deckPlayerAI = new List<CarteData>();

        // Charger toutes les cartes .asset dans Resources/CartesGenerees
        CarteScriptableObject[] cartesAssets = Resources.LoadAll<CarteScriptableObject>("CartesGenerees");
        
        // Mélanger toutes les cartes et les répartir entre les deux joueurs (deck partagé)
        int cardsAdded = 0;
        foreach (var asset in cartesAssets)
        {
            if (limitCardsForDebug && cardsAdded >= debugCardsPerDeck)
                break;

            // Une instance pour A
            CarteData dataA = new CarteData(
                asset.idCard,
                asset.nom,
                asset.nameCapacity,
                asset.descriptionCapacity,
                asset.atk,
                asset.def,
                asset.capacityId,
                asset.image           
            );
            // Une instance pour B
            CarteData dataB = new CarteData(
                asset.idCard,
                asset.nom,
                asset.nameCapacity,
                asset.descriptionCapacity,
                asset.atk,
                asset.def,
                asset.capacityId,
                asset.image
            );
            deckPlayerUI.Add(dataA);
            deckPlayerAI.Add(dataB);
            cardsAdded++;
        }
        Shuffle(deckPlayerUI);
        Shuffle(deckPlayerAI);

        // Convertir en Queue et distribuer 7 cartes pour la main de chaque joueur
        mainPlayerUI = new Queue<CarteData>();
        mainPlayerAI = new Queue<CarteData>();
        piochePlayerUI = new Queue<CarteData>();
        piochePlayerAI = new Queue<CarteData>();

        // Distribuer les cartes dans les mains
        int nombreCartesMain = Mathf.Min(MAX_CARTES_TAPIS_SELECT_DECK, deckPlayerUI.Count);
        int nombreCartesMainAdversaire = Mathf.Min(MAX_CARTES_TAPIS_SELECT_DECK, deckPlayerAI.Count);

        for (int i = 0; i < nombreCartesMain; i++)
        {
            mainPlayerUI.Enqueue(deckPlayerUI[i]);
        }
        for (int i = 0; i < nombreCartesMainAdversaire; i++)
        {
            mainPlayerAI.Enqueue(deckPlayerAI[i]);
        }

        // Le reste va dans la pioche
        for (int i = nombreCartesMain; i < deckPlayerUI.Count; i++)
        {
            piochePlayerUI.Enqueue(deckPlayerUI[i]);
        }
        for (int i = nombreCartesMainAdversaire; i < deckPlayerAI.Count; i++)
        {
            piochePlayerAI.Enqueue(deckPlayerAI[i]);
        }

        mainUIManager.ShowHand(mainPlayerUI.ToList());
    }

    public void StartRound()
    {
        if (CheckGameOver())
            return;

        numberOfAttacksUsedPlayer = 0;
        numberOfAttacksUsedIA = 0;

        aiStart = Random.Range(0, 2) == 0;

        if(aiStart)
        {
            currentPlayerAction = PlayerActionState.AI;
            Debug.Log($"[GAME] → L'IA commence ce tour (Round {round})");
        }
        else
        {
            currentPlayerAction = PlayerActionState.UI;
            Debug.Log($"[GAME] → Le JOUEUR commence ce tour (Round {round})");
        }

        PanelManager.Instance?.ShowTurnBanner(currentPlayerAction);
    }
    
    public void confirmEndRound(){
        PanelManager.Instance.ShowButtonNextStep();
    }

    public void initRound(){
        StartCoroutine(boardManager.ResetBoardForNextTurn());
    }

    public void EndTurn()
    {
        Debug.Log($"[GAME] ===== FIN DU TOUR {round} =====");
        round++;
        StartRound();
    }

    public bool CheckGameOver()
    {
        if (mode == GameMode.GAME_OVER) return true;

        int playerBoard = BoardManager.cardsOnBoardUI.Count(c => !c.isHiddenSlot && !c.isYellow);
        int aiBoard = BoardManager.cardsOnBoardAI.Count(c => !c.isHiddenSlot && !c.isYellow);
        int playerDeck = piochePlayerUI?.Count ?? 0;
        int aiDeck = piochePlayerAI?.Count ?? 0;

        bool playerHasNoCards = playerBoard == 0 && playerDeck == 0;
        bool aiHasNoCards = aiBoard == 0 && aiDeck == 0;

        if (!playerHasNoCards && !aiHasNoCards) return false;

        currentPlayerAction = PlayerActionState.NONE;
        mode = GameMode.GAME_OVER;
        isEndturnPlayer = true;
        isEndturnAI = true;

        string resultMessage;
        if (playerHasNoCards && aiHasNoCards)
            resultMessage = "Match nul !";
        else if (aiHasNoCards)
            resultMessage = "Victoire";
        else
            resultMessage = "Défaite";

        string details = $"Round: {round}\n" +
                         $"Joueur - Plateau:{playerBoard} Pioche:{playerDeck}\n" +
                         $"IA - Plateau:{aiBoard} Pioche:{aiDeck}";
        string popupMessage = $"{resultMessage}\n{details}";

        PanelManager.Instance.endGamePanel.SetActive(true);
        PanelManager.Instance.logResultEndGame.SetText(popupMessage);
        boardManager.gameObject.SetActive(false);

        return true;
    }
    
    public List<CarteData> GetSelectedCards()
    {
        MainUIManager mainUIManager = GameObject.Find("MainUIManager").GetComponent<MainUIManager>();
        List<CarteData> mainList = mainPlayerUI.ToList();
        
        return mainUIManager.transform.GetComponentsInChildren<CardMain>(true)
            .Where(card => card.isSelect)
            .OrderBy(card => card.transform.GetSiblingIndex())
            .Select(cardMain => mainList.FirstOrDefault(c => c.instanceId == cardMain.instanceId))
            .Where(carteData => carteData != null)
            .ToList();
    }

    private void Shuffle(List<CarteData> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            CarteData temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }
}
