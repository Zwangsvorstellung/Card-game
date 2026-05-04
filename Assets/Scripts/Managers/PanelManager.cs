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
    public float turnBannerFadeIn = 0.15f;
    public float turnBannerHold = 0.7f;
    public float turnBannerFadeOut = 0.2f;
    private Coroutine turnBannerCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // éviter les doublons
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

    public void ShowTurnBanner(string currentAction)
    {
        if (turnBannerPanel == null || turnBannerText == null || turnBannerCanvasGroup == null) return;

        string text = currentAction == "AI" ? "TOUR IA" : "TOUR JOUEUR";
        turnBannerText.SetText(text);
        turnBannerPanel.SetActive(true);

        if (turnBannerCoroutine != null) StopCoroutine(turnBannerCoroutine);
        turnBannerCoroutine = StartCoroutine(PlayTurnBannerCoroutine());
    }

    private IEnumerator PlayTurnBannerCoroutine()
    {
        turnBannerCanvasGroup.alpha = 0f;

        float t = 0f;
        while (t < turnBannerFadeIn)
        {
            t += Time.deltaTime;
            float p = turnBannerFadeIn <= 0f ? 1f : Mathf.Clamp01(t / turnBannerFadeIn);
            turnBannerCanvasGroup.alpha = p;
            yield return null;
        }

        turnBannerCanvasGroup.alpha = 1f;
        yield return new WaitForSeconds(turnBannerHold);

        t = 0f;
        while (t < turnBannerFadeOut)
        {
            t += Time.deltaTime;
            float p = turnBannerFadeOut <= 0f ? 0f : 1f - Mathf.Clamp01(t / turnBannerFadeOut);
            turnBannerCanvasGroup.alpha = p;
            yield return null;
        }

        turnBannerCanvasGroup.alpha = 0f;
        turnBannerPanel.SetActive(false);
        turnBannerCoroutine = null;
    }
}
