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

        // Vérification GameManager
        if (GameManager.Instance == null)
        {
            GUILayout.Label("GameManager NOT INITIALIZED!", labelStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }

        GameManager gm = GameManager.Instance;

        // Affichage infos
        GUILayout.Label("Round: " + gm.round, labelStyle);
        GUILayout.Label("Mode: " + (gm.mode ?? "null"), labelStyle);
        GUILayout.Label("Current Action: " + (gm.currentPlayerAction ?? "null"), labelStyle);
        GUILayout.Space(8);

        GUILayout.Label("Player Attacks: " + gm.numberOfAttacksUsedPlayer, labelStyle);
        GUILayout.Label("AI Attacks: " + gm.numberOfAttacksUsedIA, labelStyle);
        GUILayout.Space(8);

        int mainPlayerA = gm.mainPlayerA != null ? gm.mainPlayerA.Count : 0;
        int mainPlayerB = gm.mainPlayerB != null ? gm.mainPlayerB.Count : 0;
        GUILayout.Label("Player Hand: " + mainPlayerA, labelStyle);
        GUILayout.Label("AI Hand: " + mainPlayerB, labelStyle);

        int piochePlayerA = gm.piochePlayerA != null ? gm.piochePlayerA.Count : 0;
        int piochePlayerB = gm.piochePlayerB != null ? gm.piochePlayerB.Count : 0;
        GUILayout.Label("Player Deck: " + piochePlayerA, labelStyle);
        GUILayout.Label("AI Deck: " + piochePlayerB, labelStyle);

        int totalPlayer = mainPlayerA + piochePlayerA;
        int totalAI = mainPlayerB + piochePlayerB;
        GUILayout.Label("Total Player Cards: " + totalPlayer, labelStyle);
        GUILayout.Label("Total AI Cards: " + totalAI, labelStyle);
        GUILayout.Space(10);

        DrawQueue("PLAYER HAND (mainPlayerA)", gm.mainPlayerA);
        DrawQueue("PLAYER DECK (piochePlayerA)", gm.piochePlayerA);
        DrawQueue("AI HAND (mainPlayerB)", gm.mainPlayerB);
        DrawQueue("AI DECK (piochePlayerB)", gm.piochePlayerB);

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