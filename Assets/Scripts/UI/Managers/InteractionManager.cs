using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

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

        List<CarteData> selectedCards = GameManager.Instance.GetSelectedCards();
        HashSet<int> selectedCardIds = selectedCards.Select(c => c.idCard).ToHashSet();
        List<CarteData> unselectedCards = GameManager.Instance.mainPlayerA
            .Where(c => !selectedCardIds.Contains(c.idCard)).ToList();

        GameManager.Instance.mainPlayerA = new Queue<CarteData>(selectedCards);

        foreach (var card in unselectedCards)
            GameManager.Instance.piochePlayerA.Enqueue(card);

        // Génère 4 cartes aléatoires pour l’opposant
        List<CarteData> opponentCards = GameManager.Instance?.mainPlayerB.Take(4).ToList() ?? new List<CarteData>();

        BoardManager.Instance.ShowCardsOnTable(opponentCards, selectedCards);
        CamController.Instance.GoToBoardView();

        if (buttonObject != null)
            buttonObject.SetActive(false);

        GameManager.SetMode("select");

        MainUIManager mainUIManager = FindFirstObjectByType<MainUIManager>();
        mainUIManager?.gameObject.SetActive(false);
    }

    public void PlayHoverSound(AudioSource audio) => audio?.Play();
}
