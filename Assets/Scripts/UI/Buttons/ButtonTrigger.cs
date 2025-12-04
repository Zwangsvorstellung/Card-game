using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class ButtonTrigger : MonoBehaviour
{
    public string buttonText = "Button";
    public AudioSource hoverAudio;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();

        TMP_Text textComponent = button.GetComponentInChildren<TMP_Text>();
        if (textComponent != null) textComponent.text = buttonText;
    }

    // ==================== Board / cartes ====================
    public void OnClickConfirm() => InteractionManager.Instance.ConfirmSelection(gameObject);

    // ==================== Menu / navigation ====================
    public void OnClickHome() => InteractionManager.Instance.LoadMenu();
    public void OnClickMemoryGame() => InteractionManager.Instance.LoadMemoryGame();
    public void OnClickCardGame() => InteractionManager.Instance.LoadCardGame();
    public void OnClickQuit() => InteractionManager.Instance.QuitGame();

    // ==================== Sons ====================
    public void OnHoverSound() => InteractionManager.Instance.PlayHoverSound(hoverAudio);
}
