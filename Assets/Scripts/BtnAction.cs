using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class BtnAction : MonoBehaviour
{
    AudioSource audioHoverButton;
    public void onClickMenu()
    {
        // permet de reset pour que les animations Hover des boutons fonctionnent
        Time.timeScale = 1;
        SceneManager.LoadScene("MenuScene");
    }
    public void onClickButtonMemory() => SceneManager.LoadScene("MemoryGameScene");
    public void onClickButtonCard() => SceneManager.LoadScene("CardGamesMainScene");

    public void onClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
        Debug.Log("Quitting game");
#endif
    }

    void Awake()
    {
        audioHoverButton = gameObject.GetComponent<AudioSource>();
    }

    public void onHoverSound()
    {
        audioHoverButton.Play();
    }


}
