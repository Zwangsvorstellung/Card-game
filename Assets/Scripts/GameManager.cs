using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public MainUIManager mainUIManager;
    public Queue<CarteData> mainPlayerA;
    public Queue<CarteData> mainPlayerB;
    public Queue<CarteData> piochePlayerA;
    public Queue<CarteData> piochePlayerB;
    
    private List<int> cartesSelectionnees = new List<int>(); // Index des cartes sélectionnées
    private List<CarteData> cartesPlacees = new List<CarteData>(); // Cartes placées sur le tapis
    public const int MAX_CARTES_TAPIS = 4;

    public static GameManager Instance { get; private set; }

    public static string mode;
    public static bool iaActive = true;
    public static int scoreJoueur;
    public static int scoreAdversaire;
    public static int numeroTour;
    public static int nombreAttaquesUtilisees;
    public static int nombreAttaquesUtiliseesIA;
    public static int nombreAttaquesMaximales = 2;
    public static bool isEndturnPlayer = false;


    [SerializeField] private bool isEndturnPlayerdebug = false;
    [SerializeField] private string debugMode;
    [SerializeField] private int nombreAttaquesUtiliseesdebug;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    void Start()
    {
        mode = "selectDeck";

        scoreJoueur = 10;
        scoreAdversaire = 10;
        
        // CarteBoardInteraction.ShowScore(); // Désactivé pour ne pas afficher le score au lancement
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
        int nombreCartesMain = Mathf.Min(7, deckPlayerA.Count);
        int nombreCartesMainAdversaire = Mathf.Min(MAX_CARTES_TAPIS, deckPlayerB.Count);
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

        // afficher les mains avec leurs positions
        mainUIManager.ShowHand(mainPlayerA.ToList());
    }
    void Update()
    {
        debugMode = mode;
        nombreAttaquesUtiliseesdebug = nombreAttaquesUtilisees;
        isEndturnPlayerdebug = isEndturnPlayer;
    }
    private void SelectCard(int indexCarte)
    {
        cartesSelectionnees.Add(indexCarte);
    }

    private void DeselectCard(int indexCarte)
    {
        cartesSelectionnees.Remove(indexCarte);
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

    // Méthode pour récupérer les cartes sélectionnées dans l'ordre
    public List<CarteData> GetSelectedCards()
    {
        MainUIManager mainUIManager = GameObject.Find("MainUIManager").GetComponent<MainUIManager>();
        List<CarteData> mainList = mainPlayerA.ToList();
        
        return mainUIManager.transform.GetComponentsInChildren<CarteUI>(true)
            .Where(carte => carte.isSelect)
            .OrderBy(carte => carte.transform.GetSiblingIndex())
            .Select(carteUI => mainList.FirstOrDefault(c => c.idCard.ToString() == carteUI.carteID))
            .Where(carteData => carteData != null)
            .ToList();
    }
}
