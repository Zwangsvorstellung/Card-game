using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class BoutonValider : MonoBehaviour
{
    public string txtButton = "Valider";
    private Button button;
    
    void Start()
    {        
        button = GetComponent<Button>();
        button.GetComponentInChildren<TMP_Text>().text = txtButton;
        button.onClick.AddListener(OnButtonClick);
    }
    
    public void OnButtonClick()
    {   
        GameObject.Find("TexteConsigne")?.SetActive(false);

        List<CarteData> selectedCards = GameManager.Instance.GetSelectedCards(); // du joueur
        HashSet<int>    selectedCardIds = selectedCards.Select(c => c.idCard).ToHashSet();
        List<CarteData> unselectedCards = GameManager.Instance.mainPlayerA.Where(c => !selectedCardIds.Contains(c.idCard)).ToList();
        
        GameManager.Instance.mainPlayerA = new Queue<CarteData>(selectedCards);

        foreach (CarteData card in unselectedCards)
        {
            GameManager.Instance.piochePlayerA.Enqueue(card);
        }
        
        // Generate 4 random cards for the opponent
        List<CarteData> opponentCards = GenerateOpponentCards();

        // Show the cards on the table via the BoardManager
        BoardManager.Instance.ShowCardsOnTable(opponentCards, selectedCards);

        CamController.Instance.GoToBoardView();
        gameObject.SetActive(false);

        GameManager.SetMode("select");

        // Hide the starting hand panel (MainUIManager)
        MainUIManager mainUIManager = FindFirstObjectByType<MainUIManager>();
        mainUIManager.gameObject.SetActive(false);
    }
    
    private List<CarteData> GenerateOpponentCards()
    {
        return GameManager.Instance?.mainPlayerB.Take(4).ToList() ?? new List<CarteData>();
    }
} 
