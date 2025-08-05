using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;
    public GameObject tooltipPanel;
    public TMP_Text tooltipText;

    void Awake()
    {
        Instance = this;
        tooltipPanel.SetActive(false);
    }

    public void Show(string message, Vector3 position)
    {
        tooltipPanel.SetActive(true);
        tooltipText.text = message;
        tooltipPanel.transform.position = position + new Vector3(20, -30, 0);
    }

    public void Hide()
    {
        tooltipPanel.SetActive(false);
    }
} 