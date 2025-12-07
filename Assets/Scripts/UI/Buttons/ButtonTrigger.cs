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
    public void OnClickConfirm() => PlayerActionManager.Instance.ConfirmSelection(gameObject);
    public void OnClickNextStep() => PlayerActionManager.Instance.GetNextStep();

    public void OnClickPassed() => BoardManager.Instance.OnPassed(gameObject);
    public void OnClickAttack() => BoardManager.Instance.OnAttack(gameObject);

    // ==================== Menu / navigation ====================
    public void OnClickHome() => PlayerActionManager.Instance.LoadMenu();
    public void OnClickMemoryGame() => PlayerActionManager.Instance.LoadMemoryGame();
    public void OnClickCardGame() => PlayerActionManager.Instance.LoadCardGame();
    public void OnClickQuit() => PlayerActionManager.Instance.QuitGame();

    // ==================== Sons ====================
    public void OnHoverSound() => PlayerActionManager.Instance.PlayHoverSound(hoverAudio);
}
