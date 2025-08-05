using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class BoardManager : MonoBehaviour
{
    private static BoardManager instance;
    public static BoardManager Instance => instance ??= FindFirstObjectByType<BoardManager>();

    public GameObject cartePrefab;
    private List<GameObject> cartesInstanciees = new List<GameObject>();
    public Transform mainJoueurTransform;
    public Transform mainAdversaireTransform;
    private CarteBoardInteraction interactionMain;


    [SerializeField] private Button buttonNextStep;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
    }

    public void ShowCardsOnTable(List<CarteData> cartesAdversaire, List<CarteData> cartesJoueur)
    {        
        cartesInstanciees.ForEach(go => { if (go != null) Destroy(go); });
        cartesInstanciees.Clear();

        // Instancier les cartes de l'adversaire (4 premières)
        foreach (var carte in cartesAdversaire)
        {
            GameObject carteGO = Instantiate(cartePrefab, mainAdversaireTransform);
            CarteUI carteUI = carteGO.GetComponent<CarteUI>();
            carteUI.isCarteAdversaire = true;

            carteUI.ShowCard(carte);
            DisableCardInteractions(carteUI);
            cartesInstanciees.Add(carteGO);
        }

        // Instancier les cartes du joueur (4 dernières)
        foreach (var carte in cartesJoueur)
        {
            GameObject carteGO = Instantiate(cartePrefab, mainJoueurTransform);
            CarteUI carteUI = carteGO.GetComponent<CarteUI>();
            carteUI.isCartePlayer = true;

            carteUI.ShowCard(carte);
            EnableCardInteractions(carteUI);
            cartesInstanciees.Add(carteGO);
        }
    }
    
    // to do - une fonction pour disabled les cartes de l'adversaire et une autre pour les cartes du joueur
    private void DisableCardInteractions(CarteUI carteUI)
    {
        CarteBoardInteraction interactionBoard = carteUI.GetComponent<CarteBoardInteraction>();
        // Marquer comme carte adversaire (non sélectionnable)
        interactionBoard.isCardPlayer = carteUI.isCartePlayer;
        interactionBoard.isCardAdversaire = carteUI.isCarteAdversaire;

    }
    
    // to do - une fonction pour réactiver les cartes de l'adversaire et une autre pour les cartes du joueur
    private void EnableCardInteractions(CarteUI carteUI)
    {
        CarteBoardInteraction interactionBoard = carteUI.GetComponent<CarteBoardInteraction>();        

        // Marquer comme carte joueur (sélectionnable)
        interactionBoard.isCardPlayer = carteUI.isCartePlayer;
        interactionBoard.isCardAdversaire = carteUI.isCarteAdversaire;
    }

    // fonction de récupération des mains joueur
    public List<CarteUI> GetCartesJoueur()
    {
        return mainJoueurTransform.GetComponentsInChildren<CarteUI>(true).ToList();
    }

    public List<CarteUI> GetCartesAdversaire()
    {
        return mainAdversaireTransform.GetComponentsInChildren<CarteUI>(true).ToList();
    }

    public void ShowButtonNextStep(bool show)
    {
        buttonNextStep?.gameObject.SetActive(show);
    }

    public void OnButtonNextStepClicked()
    {
        ShowButtonNextStep(false);
        if(GameManager.iaActive)
            CarteBoardInteraction.isAITurn = false;

        CarteBoardInteraction interactionBoard = FindFirstObjectByType<CarteBoardInteraction>();        
        interactionBoard.ReplaceOpponentYellowCards();

        PrepareNextTurn();
    }

    public void PrepareNextTurn()
    {    
        GameManager.nombreAttaquesUtilisees = 0;
        
        foreach (var carte in CarteBoardInteraction.AllCardsInteractions){
    
            carte.ResetIcon(carte);
            carte.RestoreCardColor(carte);
            carte.ResetPosition();
            carte.DestroyButton();
            carte.nombreCiblages = 0;
            carte.choiceDo = false;
        }
        
        PanelManager.instance.AddLog($"=== TOUR {GameManager.numeroTour} ===");
        PanelManager.instance.AddLog($"Score: {GameManager.scoreJoueur} points");
    }
} 