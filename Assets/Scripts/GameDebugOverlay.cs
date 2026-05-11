using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameDebugOverlay : MonoBehaviour
{
    bool showDebug = false;
    GUIStyle labelStyle;
    GUIStyle titleStyle;
    Texture2D blackTexture;
    Vector2 scrollPos;

    void Awake()
    {
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

            titleStyle = new GUIStyle(labelStyle)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
        }

        int width = 620;
        int height = Mathf.Min(Screen.height, 780);
        int x = Screen.width - width;
        int y = 0;
        int lineHeight = 22;
        int line = y + 10;

        GUI.DrawTexture(new Rect(x, y, width, height), blackTexture);

        Rect viewRect = new Rect(x + 8, y + 8, width - 16, height - 16);
        GUILayout.BeginArea(viewRect);
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(viewRect.width), GUILayout.Height(viewRect.height));

        if (GameManager.Instance == null)
        {
            GUILayout.Label("GameManager NOT INITIALIZED!", labelStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }

        GameManager gm = GameManager.Instance;

        GUILayout.Label("Round: " + gm.round, labelStyle);
        GUILayout.Label("Mode: " + (gm.mode.ToString() ?? "null"), labelStyle);
        GUILayout.Label("Current Action: " + (gm.currentPlayerAction.ToString() ?? "null"), labelStyle);
        GUILayout.Space(8);

        GUILayout.Label("Player Attacks: " + gm.numberOfAttacksUsedPlayer, labelStyle);
        GUILayout.Label("AI Attacks: " + gm.numberOfAttacksUsedIA, labelStyle);
        GUILayout.Space(8);

        int mainPlayerUI = gm.mainPlayerUI != null ? gm.mainPlayerUI.Count : 0;
        int mainPlayerAI = gm.mainPlayerAI != null ? gm.mainPlayerAI.Count : 0;
        GUILayout.Label("Player Hand: " + mainPlayerUI, labelStyle);
        GUILayout.Label("AI Hand: " + mainPlayerAI, labelStyle);

        int piochePlayerUI = gm.piochePlayerUI != null ? gm.piochePlayerUI.Count : 0;
        int piochePlayerAI = gm.piochePlayerAI != null ? gm.piochePlayerAI.Count : 0;
        GUILayout.Label("Player Deck: " + piochePlayerUI, labelStyle);
        GUILayout.Label("AI Deck: " + piochePlayerAI, labelStyle);

        int totalPlayer = mainPlayerUI + piochePlayerUI;
        int totalAI = mainPlayerAI + piochePlayerAI;
        GUILayout.Label("Total Player Cards: " + totalPlayer, labelStyle);
        GUILayout.Label("Total AI Cards: " + totalAI, labelStyle);
        GUILayout.Space(10);

        DrawQueue("PLAYER HAND (mainPlayerUI)", gm.mainPlayerUI);
        DrawQueue("PLAYER DECK (piochePlayerUI)", gm.piochePlayerUI);
        DrawQueue("AI HAND (mainPlayerAI)", gm.mainPlayerAI);
        DrawQueue("AI DECK (piochePlayerAI)", gm.piochePlayerAI);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawQueue(string title, Queue<CarteData> queue)
    {
        GUILayout.Label(title, titleStyle);

        if (queue == null)
        {
            GUILayout.Label(" - null", labelStyle);
            GUILayout.Space(6);
            return;
        }

        if (queue.Count == 0)
        {
            GUILayout.Label(" - empty", labelStyle);
            GUILayout.Space(6);
            return;
        }

        int index = 0;
        foreach (CarteData card in queue.ToList())
        {
            string name = card != null ? card.nom : "null";
            string id = card != null ? card.idCard.ToString() : "-";
            GUILayout.Label($" {index:00}. {name} | id:{id}", labelStyle);
            index++;
        }

        GUILayout.Space(8);
    }
}