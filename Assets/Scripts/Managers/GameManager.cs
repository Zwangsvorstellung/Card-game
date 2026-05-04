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

    public List<CarteData> deckPlayerA;
    public List<CarteData> deckPlayerB;

    public int numberOfAttacksUsedPlayer;
    public int numberOfAttacksUsedIA;

    public MainUIManager mainUIManager;
    public BoardManager boardManager;
    public Queue<CarteData> mainPlayerA;
    public Queue<CarteData> mainPlayerB;
    public Queue<CarteData> piochePlayerA;
    public Queue<CarteData> piochePlayerB;

    public static int playerScore;
    public static int scoreOpponent;
    public string currentPlayerAction;

    public bool isEndturnPlayer = false;
    public bool isEndturnAI = false;

    public string mode;
    public bool aiStart;
    public int round;
    public bool isGameOver;

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
        mode = "selectDeck";
        isGameOver = false;
        playerScore = 10;
        scoreOpponent = 10;
                
        deckPlayerA = new List<CarteData>();
        deckPlayerB = new List<CarteData>();

        // Charger toutes les cartes .asset dans Resources/CartesGenerees
        CarteScriptableObject[] cartesAssets = Resources.LoadAll<CarteScriptableObject>("CartesGenerees");
        
        // Mélanger toutes les cartes et les répartir entre les deux joueurs (deck partagé)
        List<CarteData> allCards = new List<CarteData>();
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
            deckPlayerA.Add(dataA);
            deckPlayerB.Add(dataB);
            cardsAdded++;
        }
        Shuffle(deckPlayerA);
        Shuffle(deckPlayerB);

        // Convertir en Queue et distribuer 7 cartes pour la main de chaque joueur
        mainPlayerA = new Queue<CarteData>();
        mainPlayerB = new Queue<CarteData>();
        piochePlayerA = new Queue<CarteData>();
        piochePlayerB = new Queue<CarteData>();

        // Distribuer les cartes dans les mains
        int nombreCartesMain = Mathf.Min(MAX_CARTES_TAPIS_SELECT_DECK, deckPlayerA.Count);
        int nombreCartesMainAdversaire = Mathf.Min(MAX_CARTES_TAPIS_SELECT_DECK, deckPlayerB.Count);

        for (int i = 0; i < nombreCartesMain; i++)
        {
            mainPlayerA.Enqueue(deckPlayerA[i]);
        }
        for (int i = 0; i < nombreCartesMainAdversaire; i++)
        {
            mainPlayerB.Enqueue(deckPlayerB[i]);
        }

        // Le reste va dans la pioche
        for (int i = nombreCartesMain; i < deckPlayerA.Count; i++)
        {
            piochePlayerA.Enqueue(deckPlayerA[i]);
        }
        for (int i = nombreCartesMainAdversaire; i < deckPlayerB.Count; i++)
        {
            piochePlayerB.Enqueue(deckPlayerB[i]);
        }

        mainUIManager.ShowHand(mainPlayerA.ToList());
        
        Debug.Log($"[GAME] Decks créés - Joueur: {mainPlayerA.Count} cartes, IA: {mainPlayerB.Count} cartes");
        Debug.Log($"[GAME] Pioches créées - Joueur: {piochePlayerA.Count} cartes, IA: {piochePlayerB.Count} cartes");
    }

    public void StartTurn()
    {
        if (isGameOver || CheckGameOver())
            return;

        numberOfAttacksUsedPlayer = 0;
        numberOfAttacksUsedIA = 0;

        Debug.Log($"[GAME] ===== DÉBUT DU TOUR {round} =====");
        Debug.Log($"[GAME] Compteurs réinitialisés - Attaques joueur: {numberOfAttacksUsedPlayer}, Attaques IA: {numberOfAttacksUsedIA}");
    
        aiStart = Random.Range(0, 2) == 0;

        if(aiStart)
        {
            currentPlayerAction = "AI";
            Debug.Log($"[GAME] → L'IA commence ce tour (Round {round})");
        }
        else
        {
            currentPlayerAction = "UI";
            Debug.Log($"[GAME] → Le JOUEUR commence ce tour (Round {round})");
        }

        PanelManager.Instance?.ShowTurnBanner(currentPlayerAction);
    }

    public void initRound(){
        boardManager.ResetBoardForNextTurn();
    }

    public void EndTurn()
    {
        if (isGameOver || CheckGameOver())
            return;

        Debug.Log($"[GAME] ===== FIN DU TOUR {round} =====");
        Debug.Log($"[GAME] Attaques utilisées - Joueur: {numberOfAttacksUsedPlayer}/{MAX_NUMBER_ATK_ROUND}, IA: {numberOfAttacksUsedIA}/{MAX_NUMBER_ATK_ROUND}");
        Debug.Log($"[GAME] Scores actuels - Joueur: {playerScore}, IA: {scoreOpponent}");
        
        round++;

        // Chaque tour : choix aléatoire (joueur ou IA commence)
        aiStart = Random.Range(0, 2) == 0;
        if(aiStart)
            currentPlayerAction = "AI";
        else
            currentPlayerAction = "UI";

        Debug.Log($"[GAME] Prochain tour ({round}) - Qui commence: {(aiStart ? "IA" : "JOUEUR")} (aléatoire)");

        StartTurn();
    }

    public bool CheckGameOver()
    {
        if (isGameOver) return true;

        int playerBoard = BoardManager.cardsOnBoardUI.Count(c => c != null && !c.isHiddenSlot);
        int aiBoard = BoardManager.cardsOnBoardAI.Count(c => c != null && !c.isHiddenSlot);
        int playerHand = mainPlayerA?.Count ?? 0;
        int aiHand = mainPlayerB?.Count ?? 0;
        int playerDeck = piochePlayerA?.Count ?? 0;
        int aiDeck = piochePlayerB?.Count ?? 0;

        bool playerHasNoCards = playerBoard == 0 && playerHand == 0 && playerDeck == 0;
        bool aiHasNoCards = aiBoard == 0 && aiHand == 0 && aiDeck == 0;

        if (!playerHasNoCards && !aiHasNoCards) return false;

        isGameOver = true;
        currentPlayerAction = "NONE";
        mode = "gameOver";
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
                         $"Joueur - Plateau:{playerBoard} Main:{playerHand} Pioche:{playerDeck}\n" +
                         $"IA - Plateau:{aiBoard} Main:{aiHand} Pioche:{aiDeck}";
        string popupMessage = $"{resultMessage}\n{details}";

        PanelManager.Instance.endGamePanel.SetActive(true);
        PanelManager.Instance.logResultEndGame.SetText(popupMessage);
        
        return true;
    }
    
    public List<CarteData> GetSelectedCards()
    {
        MainUIManager mainUIManager = GameObject.Find("MainUIManager").GetComponent<MainUIManager>();
        List<CarteData> mainList = mainPlayerA.ToList();
        
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
