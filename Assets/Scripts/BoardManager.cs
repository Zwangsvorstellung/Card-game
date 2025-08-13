using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

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
        interactionBoard.isCardOpponent = carteUI.isCarteAdversaire;
        interactionBoard.startPosition = (Vector3)carteUI.GetComponent<RectTransform>().anchoredPosition;

        Vector2 anchoredPos = carteUI.GetComponent<RectTransform>().anchoredPosition;

        interactionBoard.startPosition = (Vector3)anchoredPos;

        if(interactionBoard.isCardPlayer){
            
            interactionBoard.newPosition = new Vector3(
                anchoredPos.x,
                anchoredPos.y + 50f,
                0f
            );
        }

        if(interactionBoard.isCardOpponent){
            interactionBoard.newPosition = new Vector3(
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
        if (GameManager.iaActive)
            CarteBoardInteraction.isAITurn = false;

        StartCoroutine(NextStepSequence());
    }

    private IEnumerator FadeAllCards(float fromAlpha, float toAlpha, float duration)
    {
        var cartesJaunes = CarteBoardInteraction.AllCardsInteractions
        .Where(c => c.yellowCard && (c.isCardPlayer || c.isCardOpponent))
        .ToList();

        foreach (var card in cartesJaunes)
        {
            var anim = card.GetComponent<CardAnimations>();
            var img = card.GetComponentInChildren<Image>();

            if (anim != null && img != null)
            {
                anim.targetImage = img;
                yield return StartCoroutine(anim.Fade(card.GetComponent<CarteUI>(), fromAlpha, toAlpha, duration));
            }
        }
    }

    private IEnumerator NextStepSequence()
    {
        // 1) Fade des cartes de 1 à 0 (disparition)
        yield return StartCoroutine(FadeAllCards(1f, 0f, 0.5f));
        yield return new WaitForSeconds(1f);

        // 2) Remplacement des cartes après le fade
        CarteBoardInteraction interactionBoard = FindFirstObjectByType<CarteBoardInteraction>();
        interactionBoard.ReplaceOpponentYellowCards();

        // 3) Préparation du prochain tour (pas de fade ici)
        PrepareNextTurn();
    }
    

    public void PrepareNextTurn()
    {    
        GameManager.numberOfAttacksUsed = 0;
        foreach(CarteBoardInteraction card in CarteBoardInteraction.AllCardsInteractions){

            card.ResetIcon(card);
            card.RestoreCardColor(card);
            card.ResetPosition();
            card.DestroyButton();
            card.targetCount = 0;
            card.stateDefensif = "notCibled";
            card.stateOffensif = "waitOrder";
            card.choiceDo = false;
            card.isSelected = false;
            card.freeze = false;
            card.layoutGroup.enabled = true;
        }
        
        PanelManager.instance.AddLog($"=== TOUR {GameManager.currentRound} ===");
        PanelManager.instance.AddLog($"Score: {GameManager.playerScore} points");
    }
} 