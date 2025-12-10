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
        PanelManager.Instance.HideInstructionText();
        PanelManager.Instance.HideValidateDeck();

        List<CarteData> selectedCards = GameManager2.Instance.GetSelectedCards();
        HashSet<int> selectedCardIds = selectedCards.Select(c => c.idCard).ToHashSet();
        List<CarteData> unselectedCards = GameManager2.Instance.mainPlayerA
            .Where(c => !selectedCardIds.Contains(c.idCard)).ToList();

        GameManager2.Instance.mainPlayerA = new Queue<CarteData>(selectedCards);

        foreach (var card in unselectedCards)
            GameManager2.Instance.piochePlayerA.Enqueue(card);

        // Génère 4 cartes aléatoires pour l’opposant
        List<CarteData> opponentCards = GameManager2.Instance?.mainPlayerB.Take(4).ToList() ?? new List<CarteData>();

        BoardManager.Instance.SetupBoardCards(opponentCards, selectedCards);
        CamController.Instance.GoToBoardView();

        buttonObject.SetActive(false);

        GameManager2.SetMode("select");

        MainUIManager.Instance.gameObject.SetActive(false);
    }

    public void GetNextStep()
    {
        PanelManager.Instance.HideButtonNextStep();
        //BoardManager.Instance.StartCoroutine(BoardManager.Instance.HandleNextTurnTransition());
        CarteBoardInteraction.isAITurn = false;
    }

    public void ClickOnPassed()
    {
    }

    public void ClickOnAttack()
    {
    }

    public void ClickOnMainCard(CardMain cardMain)
    {
        if(GameManager2.mode == "deck"){

            if(!cardMain.isSelect)
            {
                if (MainUIManager.Instance.CountSelectedCards() < GameManager2.MAX_CARTES_TAPIS){
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
            MainUIManager.Instance.ShowValidateButton(numberCardsSelect >= GameManager2.MAX_CARTES_TAPIS);
        }
    }

    public void ClickOnBoardCard(CardUI cardUI)
    {
        BoardManager.Instance.selectCardOnBoard(cardUI);
    }


    public void PlayHoverSound(AudioSource audio) => audio?.Play();
}
