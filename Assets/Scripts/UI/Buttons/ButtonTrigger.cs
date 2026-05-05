using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ButtonTrigger : MonoBehaviour{
    public string buttonText = "Button";
    public AudioSource hoverAudio;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        //button.onClick.AddListener(() => Debug.Log($"[BTN] Click reçu sur {name} ({buttonText})"));

        TMP_Text textComponent = button.GetComponentInChildren<TMP_Text>();
        if (textComponent != null) textComponent.text = buttonText;
    }

    // ==================== Board / cartes ====================
    public void OnClickConfirm() => PlayerActionManager.Instance.ConfirmSelection(gameObject);
    public void OnClickNextStep() => PlayerActionManager.Instance.GetNextStep();

    public void OnClickPassed() => PlayerActionManager.Instance.ClickOnPassed(gameObject);
    public void OnClickAttack() => PlayerActionManager.Instance.ClickOnAttack(gameObject);

    // ==================== Menu / navigation ====================
    public void OnClickHome() => PlayerActionManager.Instance.LoadMenu();
    public void OnClickMemoryGame() => PlayerActionManager.Instance.LoadMemoryGame();
    public void OnClickCardGame() => PlayerActionManager.Instance.LoadCardGame();
    public void OnClickReplay() => PlayerActionManager.Instance.ReplayCurrentGame();
    public void OnClickQuit() => PlayerActionManager.Instance.QuitGame();

    // ==================== Sons ====================
    public void OnHoverSound() => PlayerActionManager.Instance.PlayHoverSound(hoverAudio);
}
