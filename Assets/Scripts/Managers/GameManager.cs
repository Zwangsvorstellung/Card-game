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
    public bool playerStarts;
    public int round;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    public void StartTurn()
    {
        numberOfAttacksUsedPlayer = 0;
        numberOfAttacksUsedIA = 0;

        Debug.Log($"[GAME] ===== DÉBUT DU TOUR {round} =====");
        Debug.Log($"[GAME] Compteurs réinitialisés - Attaques joueur: {numberOfAttacksUsedPlayer}, Attaques IA: {numberOfAttacksUsedIA}");
       
        playerStarts = Random.Range(0, 2) == 0;
        if(playerStarts)
            currentPlayerAction = "UI";
        else
            currentPlayerAction = "AI";

        if (playerStarts)
        {
            Debug.Log($"[GAME] → Le JOUEUR commence ce tour (Round {round})");
            Debug.Log($"[GAME] Mode: {mode} - Le joueur peut maintenant sélectionner ses cartes");
        }
        else
        {
            Debug.Log($"[GAME] → L'IA commence ce tour (Round {round})");
            IA.Instance.StartCoroutine(IA.Instance.StartAITurnCoroutine());
        }
    }

    public void initRound(){
        boardManager.ResetBoardForNextTurn();
    }

    public void EndTurn()
    {
        Debug.Log($"[GAME] ===== FIN DU TOUR {round} =====");
        Debug.Log($"[GAME] Attaques utilisées - Joueur: {numberOfAttacksUsedPlayer}/{MAX_NUMBER_ATK_ROUND}, IA: {numberOfAttacksUsedIA}/{MAX_NUMBER_ATK_ROUND}");
        Debug.Log($"[GAME] Scores actuels - Joueur: {playerScore}, IA: {scoreOpponent}");
        
        round++;

        // Chaque tour : choix aléatoire (joueur ou IA commence)
        // Cela rend le jeu plus imprévisible et équitable
        playerStarts = Random.Range(0, 2) == 0;
        if(playerStarts)
            currentPlayerAction = "UI";
        else
            currentPlayerAction = "AI";

        
        Debug.Log($"[GAME] Prochain tour ({round}) - Qui commence: {(playerStarts ? "JOUEUR" : "IA")} (aléatoire)");

        StartTurn();
    }

    void Start()
    {
        round = 1;
        mode = "selectDeck";

        playerScore = 10;
        scoreOpponent = 10;
        
        Debug.Log($"[GAME] ===== INITIALISATION DU JEU =====");
        Debug.Log($"[GAME] Round: {round}, Mode: {mode}");
        Debug.Log($"[GAME] Scores initiaux - Joueur: {playerScore}, IA: {scoreOpponent}");
        
        // faire 2 decks complets pour joueur A et joueur B
        List<CarteData> deckPlayerA = new List<CarteData>();
        List<CarteData> deckPlayerB = new List<CarteData>();

        // Charger toutes les cartes .asset dans Resources/CartesGenerees
        CarteScriptableObject[] cartesAssets = Resources.LoadAll<CarteScriptableObject>("CartesGenerees");
        
        // Mélanger toutes les cartes et les répartir entre les deux joueurs (deck partagé)
        List<CarteData> allCards = new List<CarteData>();
        foreach (var asset in cartesAssets)
        {
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
        Debug.Log($"[GAME] Mode: {mode} - En attente de sélection du deck par le joueur");
    }
    
    // Méthode pour récupérer les cartes sélectionnées dans l'ordre
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
