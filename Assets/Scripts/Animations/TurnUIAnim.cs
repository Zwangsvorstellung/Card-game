using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

public class TurnUIAnim : MonoBehaviour
{
    [Header("UI")]
    public RectTransform panel;
    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup;

    [Header("Timing")]
    public float inTime = 0.35f;
    public float stayTime = 0.5f;
    public float outTime = 0.25f;

    Vector2 startPos;
    Vector2 centerPos;
    Vector2 endPos;

    void Awake()
    {
        centerPos = Vector2.zero;
        startPos = new Vector2(-Screen.width, 0);
        endPos = new Vector2(Screen.width, 0);

        panel.anchoredPosition = startPos;
        canvasGroup.alpha = 0;
    }

    // -------------------------
    // INTRO (début de partie)
    // -------------------------
    public IEnumerator ShowIntro(bool playerFirst)
    {
        return PlayAnim(playerFirst ? "VOUS COMMENCEZ" : "L'IA COMMENCE");
    }

    // -------------------------
    // SWITCH (changement joueur)
    // -------------------------
    public IEnumerator SwitchTurn(bool isPlayerTurn)
    {
        return PlayAnim(isPlayerTurn ? "À VOUS DE JOUER" : "TOUR DE L'IA");
    }

    // -------------------------
    // FIN DE TOUR (résolution)
    // -------------------------
    public IEnumerator EndTurn()
    {
        return PlayAnim("RÉSOLUTION DES ACTIONS");
    }

    // -------------------------
    // FIN DE PARTIE
    // -------------------------
    public IEnumerator EndGame(bool playerWon)
    {
        return PlayAnim(playerWon ? "VICTOIRE" : "DÉFAITE");
    }

    // -------------------------
    // ANIMATION UNIQUE
    // -------------------------
    IEnumerator PlayAnim(string message)
    {
        text.text = message;
        text.color = message.Contains("IA") ? Color.red : Color.white;

        panel.anchoredPosition = startPos;
        canvasGroup.alpha = 1;

        text.transform.localScale = Vector3.one;
        text.transform.DOKill();

        Sequence seq = DOTween.Sequence();

        // entrée
        seq.Append(panel.DOAnchorPos(centerPos, inTime).SetEase(Ease.OutCubic));
        seq.Join(canvasGroup.DOFade(1, 0.15f));
        seq.Join(text.transform.DOScale(1.15f, 0.2f));

        // affichage
        seq.AppendInterval(stayTime);

        // sortie
        seq.Append(canvasGroup.DOFade(0, outTime));
        seq.Join(panel.DOAnchorPos(endPos, outTime).SetEase(Ease.InCubic));

        yield return seq.WaitForCompletion();
    }


/*
    IEnumerator PlayAnim(string message)
    {
        text.text = message;

        panel.anchoredPosition = startPos;
        canvasGroup.alpha = 1;

        text.transform.localScale = Vector3.one;
        text.transform.DOKill();
        panel.DOKill();
        canvasGroup.DOKill();

        Sequence seq = DOTween.Sequence();

        // entrée plus “vivante”
        seq.Append(panel.DOAnchorPos(centerPos, inTime).SetEase(Ease.OutBack));

        seq.Join(canvasGroup.DOFade(1, 0.15f));

        // punch léger du texte (impact)
        seq.Join(text.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 8, 1));

        // petit hover micro (donne du “life”)
        seq.Join(panel.DOScale(1.02f, 0.2f).SetLoops(2, LoopType.Yoyo));

        // affichage
        seq.AppendInterval(stayTime);

        // sortie plus clean (pas juste fade)
        seq.Append(canvasGroup.DOFade(0, outTime));

        seq.Join(panel.DOAnchorPos(endPos, outTime).SetEase(Ease.InCubic));

        // reset scale propre
        seq.OnComplete(() =>
        {
            panel.localScale = Vector3.one;
        });

        yield return seq.WaitForCompletion();
    }
*/
}


//Début de partie

/*
StartCoroutine(turnUI.ShowIntro(playerFirst));
Changement de joueur
StartCoroutine(turnUI.SwitchTurn(true));  // joueur
StartCoroutine(turnUI.SwitchTurn(false)); // IA
Fin de tour
StartCoroutine(turnUI.EndTurn());
Fin de partie
StartCoroutine(turnUI.EndGame(true));  // victoire
StartCoroutine(turnUI.EndGame(false)); // défaite
*/