using UnityEngine;

public class GameDebugOverlay : MonoBehaviour
{
    bool showDebug = true;
    GUIStyle labelStyle;
    Texture2D blackTexture;

    void Awake()
    {
        // Crée une texture noire opaque
        blackTexture = new Texture2D(1, 1);
        blackTexture.SetPixel(0, 0, Color.black);
        blackTexture.Apply();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            showDebug = !showDebug;
    }

    void OnGUI()
    {
        if (!showDebug) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
        }

        int width = 400;
        int height = 450;
        int x = Screen.width - width;
        int y = 0;
        int lineHeight = 24;
        int line = y + 10;

        // Fond opaque avec Texture2D
        GUI.DrawTexture(new Rect(x, y, width, height), blackTexture);

        // Vérification GameManager
        if (GameManager.Instance == null)
        {
            GUI.Label(new Rect(x + 10, line, width - 20, 20), "GameManager NOT INITIALIZED!", labelStyle);
            return;
        }

        GameManager gm = GameManager.Instance;

        // Affichage infos
        GUI.Label(new Rect(x + 10, line, width - 20, 20), "Round: " + gm.round, labelStyle); line += lineHeight;
        GUI.Label(new Rect(x + 10, line, width - 20, 20), "Mode: " + (gm.mode ?? "null"), labelStyle); line += lineHeight;
        GUI.Label(new Rect(x + 10, line, width - 20, 20), "Current Action: " + (gm.currentPlayerAction ?? "null"), labelStyle); line += lineHeight + 10;

        GUI.Label(new Rect(x + 10, line, width - 20, 20), "Player Attacks: " + gm.numberOfAttacksUsedPlayer, labelStyle); line += lineHeight;
        GUI.Label(new Rect(x + 10, line, width - 20, 20), "AI Attacks: " + gm.numberOfAttacksUsedIA, labelStyle); line += lineHeight + 10;

        int mainPlayerA = gm.mainPlayerA != null ? gm.mainPlayerA.Count : 0;
        int mainPlayerB = gm.mainPlayerB != null ? gm.mainPlayerB.Count : 0;
        GUI.Label(new Rect(x + 10, line, width - 20, 20), "Player Hand: " + mainPlayerA, labelStyle); line += lineHeight;
        GUI.Label(new Rect(x + 10, line, width - 20, 20), "AI Hand: " + mainPlayerB, labelStyle); line += lineHeight;

        int piochePlayerA = gm.piochePlayerA != null ? gm.piochePlayerA.Count : 0;
        int piochePlayerB = gm.piochePlayerB != null ? gm.piochePlayerB.Count : 0;
        GUI.Label(new Rect(x + 10, line, width - 20, 20), "Player Deck: " + piochePlayerA, labelStyle); line += lineHeight;
        GUI.Label(new Rect(x + 10, line, width - 20, 20), "AI Deck: " + piochePlayerB, labelStyle); line += lineHeight;

        //GUI.Label(new Rect(x + 10, line, width - 20, 20), "Player Score: " + GameManager.playerScore, labelStyle); line += lineHeight;
        //GUI.Label(new Rect(x + 10, line, width - 20, 20), "AI Score: " + GameManager.scoreOpponent, labelStyle); line += lineHeight + 10;

        int totalPlayer = mainPlayerA + piochePlayerA;
        int totalAI = mainPlayerB + piochePlayerB;
        GUI.Label(new Rect(x + 10, line, width - 20, 20), "Total Player Cards: " + totalPlayer, labelStyle); line += lineHeight;
        GUI.Label(new Rect(x + 10, line, width - 20, 20), "Total AI Cards: " + totalAI, labelStyle); line += lineHeight;
    }
}