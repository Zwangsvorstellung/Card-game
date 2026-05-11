using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class PanelManager : MonoBehaviour
{
    public static PanelManager Instance;

    [Header("UI Panels")]
    public GameObject instructionText;
    public GameObject turnLogPanel;   
    public TMP_Text logPanel;
    private List<string> logs = new List<string>();
    public GameObject endGamePanel;   
    public TMP_Text logResultEndGame;
    public GameObject validateDeck;   
    public GameObject nextStep;   

    [Header("Turn Banner")]
    public GameObject turnBannerPanel;
    public TMP_Text turnBannerText;
    public CanvasGroup turnBannerCanvasGroup;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionnel : ne pas détruire sur changement de scène
        // DontDestroyOnLoad(gameObject);
    }

    public void ShowInstructionText() => instructionText?.SetActive(true);
    public void HideInstructionText() => instructionText?.SetActive(false);

    public void ShowTurnLogPanel() => turnLogPanel?.SetActive(true);
    public void HideTurnLogPanel() => turnLogPanel?.SetActive(false);

    public void ShowValidateDeck() => validateDeck?.SetActive(true);
    public void HideValidateDeck() => validateDeck?.SetActive(false);

    public void ShowButtonNextStep() => nextStep?.SetActive(true);
    public void HideButtonNextStep() => nextStep?.SetActive(false);

    public void ShowTurnBanner(PlayerActionState player)
    {
        turnBannerPanel.SetActive(true);
        turnBannerCanvasGroup.alpha = 1f;
        turnBannerText.text = player.ToString() == PlayerActionState.AI.ToString() ? "IA" : "Joueur";
    }
    public void OffTurnBanner(){
        turnBannerPanel.SetActive(false);
    }
}
