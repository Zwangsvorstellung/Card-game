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

    public void ShowCardsOnTable(List<CarteData> cardsOpponent, List<CarteData> cardsPlayer)
    {        
        cartesInstanciees.ForEach(go => { if (go != null) Destroy(go); });
        cartesInstanciees.Clear();

        // Instancier les cartes de l'adversaire (4 premières)
        foreach (var card in cardsOpponent)
        {
            GameObject carteGO = Instantiate(cartePrefab, mainAdversaireTransform);
            CarteUI carteUI = carteGO.GetComponent<CarteUI>();
            carteUI.isCarteAdversaire = true;
            carteUI.setAttributesInitCard(card);
            cartesInstanciees.Add(carteGO);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(mainAdversaireTransform as RectTransform);

        // Instancier les cartes du joueur (4 dernières)
        foreach (var card in cardsPlayer)
        {
            GameObject carteGO = Instantiate(cartePrefab, mainJoueurTransform);
            CarteUI carteUI = carteGO.GetComponent<CarteUI>();
            carteUI.isCartePlayer = true;
            carteUI.setAttributesInitCard(card);
            cartesInstanciees.Add(carteGO);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(mainJoueurTransform as RectTransform);
        
        foreach (var go in cartesInstanciees)
        {
            CarteUI carteUI = go.GetComponent<CarteUI>();
            SetCardPropertiesForGame(carteUI);
        }
    }
    
    public void SetCardPropertiesForGame(CarteUI carteUI)
    {
        CarteBoardInteraction interactionBoard = carteUI.GetComponent<CarteBoardInteraction>();
        RectTransform rectTransform = carteUI.GetComponent<RectTransform>();

        interactionBoard.isCardPlayer = carteUI.isCartePlayer;
        interactionBoard.isCardAdversaire = carteUI.isCarteAdversaire;
        interactionBoard.positionInitiale = (Vector3)carteUI.GetComponent<RectTransform>().anchoredPosition;

        Vector2 anchoredPos = carteUI.GetComponent<RectTransform>().anchoredPosition;

        // Stocke la position initiale (convertie en Vector3)
        interactionBoard.positionInitiale = (Vector3)anchoredPos;

        if(interactionBoard.isCardPlayer){
        // Crée une nouvelle position avec Y + 50
            interactionBoard.nouvellePosition = new Vector3(
                anchoredPos.x,
                anchoredPos.y + 50f,
                0f
            );
        }

        if(interactionBoard.isCardAdversaire){
            interactionBoard.nouvellePosition = new Vector3(
                anchoredPos.x,
                anchoredPos.y - 50f,
                0f
            );
        }
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
        foreach(CarteBoardInteraction card in CarteBoardInteraction.AllCardsInteractions){

            card.ResetIcon(card);
            card.RestoreCardColor(card);
            card.ResetPosition();
            card.DestroyButton();
            card.nombreCiblages = 0;
            card.stateDefensif = "notCibled";
            card.stateOffensif = "waitOrder";
            card.choiceDo = false;
            card.isSelected = false;
            card.layoutGroup.enabled = true;
        }
        
        PanelManager.instance.AddLog($"=== TOUR {GameManager.numeroTour} ===");
        PanelManager.instance.AddLog($"Score: {GameManager.scoreJoueur} points");
    }
} 
