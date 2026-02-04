using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class PlayerActionManager : MonoBehaviour
{
    public static PlayerActionManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void LoadMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MenuScene");
    }

    public void LoadMemoryGame() => SceneManager.LoadScene("MemoryGameScene");
    public void LoadCardGame() => SceneManager.LoadScene("CardGamesMainScene");

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        Debug.Log("Quitting game");
    }

    public void ConfirmSelection(GameObject buttonObject)
    {
        Debug.Log($"[PLAYER] ===== VALIDATION DU DECK =====");
        
        GameManager.Instance.mode = "selectCardToPlayAction";

        PanelManager.Instance.HideInstructionText();
        PanelManager.Instance.HideValidateDeck();

        List<CarteData> selectedCards = GameManager.Instance.GetSelectedCards();
        Debug.Log($"[PLAYER] Cartes sélectionnées: {selectedCards.Count}");
        foreach (var card in selectedCards)
        {
            Debug.Log($"[PLAYER]   - {card.nom} (ATK:{card.attaque}, DEF:{card.defense})");
        }
        
        HashSet<int> selectedCardIds = selectedCards.Select(c => c.idCard).ToHashSet();
        List<CarteData> unselectedCards = GameManager.Instance.mainPlayerA
            .Where(c => !selectedCardIds.Contains(c.idCard)).ToList();

        GameManager.Instance.mainPlayerA = new Queue<CarteData>(selectedCards);

        foreach (var card in unselectedCards)
            GameManager.Instance.piochePlayerA.Enqueue(card);

        // Génère 4 cartes aléatoires pour l'opposant
        var opponentDeck = GameManager.Instance.mainPlayerB;
        List<CarteData> opponentCards = new();

        if (opponentDeck != null)
        {
            int count = Mathf.Min(4, opponentDeck.Count);

            for (int i = 0; i < count; i++)
            {
                opponentCards.Add(opponentDeck.Dequeue());
            }
        }
        
        Debug.Log($"[PLAYER] Cartes IA générées: {opponentCards.Count}");
        foreach (var card in opponentCards)
        {
            Debug.Log($"[PLAYER]   - {card.nom} (ATK:{card.attaque}, DEF:{card.defense})");
        }

        BoardManager.Instance.SetupBoardCards(opponentCards, selectedCards);
        CamController.Instance.GoToBoardView();

        buttonObject.SetActive(false);

        MainUIManager.Instance.gameObject.SetActive(false);
        
        Debug.Log($"[PLAYER] Passage au plateau de combat - Mode: {GameManager.Instance.mode}");
        GameManager.Instance.StartTurn();

    }

    public void GetNextStep()
    {
        PanelManager.Instance.HideButtonNextStep();
        //BoardManager.Instance.StartCoroutine(BoardManager.Instance.HandleNextTurnTransition());
    }

    public void ClickOnPassed(GameObject buttonObject)
    {
        CardUI card = buttonObject.GetComponentInParent<CardUI>();
        Debug.Log($"[PLAYER] Carte {card.nameCard} choisit de PASSER");
        card.OnPassed();
        GameManager.Instance.mode = "selectCardToPlayAction";
        Debug.Log($"[PLAYER] Mode changé: {GameManager.Instance.mode}");
    }

    public void ClickOnAttack(GameObject buttonObject)
    {
        CardUI card = buttonObject.GetComponentInParent<CardUI>();
        Debug.Log($"[PLAYER] Carte {card.nameCard} choisit d'ATTAQUER (ATK:{card.attaqueValue})");
        card.OnAttack();
        GameManager.Instance.mode = "selectCardOpponentToAttack";
        Debug.Log($"[PLAYER] Mode changé: {GameManager.Instance.mode} - Sélection de la cible requise");
    }

    public void ClickOnMainCard(CardMain cardMain)
    {
        if(GameManager.Instance.mode == "deck"){

            if(!cardMain.isSelect)
            {
                if (MainUIManager.Instance.CountSelectedCards() < GameManager.MAX_CARTES_TAPIS){
                    cardMain.SelectCardMain();
                    //if (pulseCoroutine == null) pulseCoroutine = StartCoroutine(cardAnimations.Pulse(0.7f, 0.95f, 1f));
                }
            }
            else{
               /* if (pulseCoroutine != null)
                {
                    //StopCoroutine(pulseCoroutine);
                   // pulseCoroutine = null;
                    //rectTransform.localScale = new Vector3(0.8f, 0.8f, 1f);
                }
                */
                cardMain.DeselectCardMain();
            }

            int numberCardsSelect = MainUIManager.Instance.CountSelectedCards();
            MainUIManager.Instance.ShowValidateButton(numberCardsSelect >= GameManager.MAX_CARTES_TAPIS);
        }
    }

    public void ClickOnBoardCard(CardUI cardUI)
    {
        Debug.Log($"[PLAYER] Clic sur carte joueur: {cardUI.nameCard} (État: {cardUI.stateOffensif})");
        BoardManager.Instance.selectCardOnBoard(cardUI);
    }

    public void ClickSelectTargetOnBoard(CardAI cardAI)
    {
        Debug.Log($"[PLAYER] Clic sur carte IA (cible): {cardAI.nameCard} (DEF:{cardAI.defenseValue})");
        BoardManager.Instance.selectCardOpponentOnBoard(cardAI);
    }


    public void PlayHoverSound(AudioSource audio) => audio?.Play();
}
