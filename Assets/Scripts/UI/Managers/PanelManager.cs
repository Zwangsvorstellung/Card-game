using UnityEngine;
using TMPro;

public class PanelManager : MonoBehaviour
{
    public static PanelManager Instance;

    [Header("UI Panels")]
    public GameObject instructionText;

    public GameObject turnLogPanel;   
    public TMP_Text logPanel;
  

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

    public void ShowInstructionText()
    {
        if (instructionText != null)
            instructionText.SetActive(true);
    }

    public void HideInstructionText()
    {
        if (instructionText != null)
            instructionText.SetActive(false);
    }

    public void ShowTurnLogPanel() => turnLogPanel?.SetActive(true);
    public void HideTurnLogPanel() => turnLogPanel?.SetActive(false);
    
}
