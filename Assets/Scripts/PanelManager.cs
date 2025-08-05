using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PanelManager : MonoBehaviour
{
    public static PanelManager instance;
    
    // Configuration du panel
    private const int MAX_LOGS = 50;
    private const float PANEL_OPACITY = 0.85f;  
    private const float PANEL_WIDTH = 220f;
    private const float PANEL_MARGIN_TOP = 125f;
    private const float PANEL_MARGIN_BOTTOM = 25f;
    private const float PANEL_MARGIN_RIGHT = 60f;
    private const float LOGS_MARGIN = 10f;
    private const float LOGS_FONT_SIZE = 14f;
    
    // Configuration du panel de victoire
    private const float VICTORY_PANEL_WIDTH = 400f;
    private const float VICTORY_PANEL_HEIGHT = 300f;
    private const float VICTORY_PANEL_OPACITY = 0.8f;
    private const float VICTORY_TEXT_SIZE = 36f;
    private const float SCORE_TEXT_SIZE = 24f;
    private const float BUTTON_WIDTH = 150f;
    private const float BUTTON_HEIGHT = 40f;
    private const float BUTTON_TEXT_SIZE = 18f;
    
    private GameObject panelDroit;
    private bool estEnVueBoard = false;
    private TMP_Text zoneLogs;
    private List<string> logs = new List<string>();
    
    void Awake()
    {
        instance = this;
        CreateRightPanel();
    }
    
    void Update()
    {
        bool nouvelleVueBoard = CamController.Instance?.EstEnVueBoard() ?? false;
        
        if (nouvelleVueBoard != estEnVueBoard)
        {
            estEnVueBoard = nouvelleVueBoard;
            panelDroit?.SetActive(estEnVueBoard);
        }
    }
    
    private void CreateRightPanel()
    {
        // Chercher le Canvas dans la scène
        Canvas canvas = FindFirstObjectByType<Canvas>();
        
        // Créer le panel droit
        GameObject panelGO = new GameObject("PanelDroit");
        panelGO.transform.SetParent(canvas.transform, false);
        
        // Ajouter les composants UI
        Image imagePanel = panelGO.AddComponent<Image>();
        imagePanel.color = new Color(0, 0, 0, PANEL_OPACITY);
        
        RectTransform rectPanel = panelGO.GetComponent<RectTransform>();
        
        // Positionner le panel sur la droite avec les marges spécifiées
        rectPanel.anchorMin = new Vector2(1, 0); // Ancré en bas à droite
        rectPanel.anchorMax = new Vector2(1, 1); // Ancré en haut à droite
        rectPanel.pivot = new Vector2(1, 0.5f); // Pivot à droite
        rectPanel.anchoredPosition = new Vector2(0, 0); // Position de base
        
        rectPanel.offsetMin = new Vector2(-PANEL_MARGIN_RIGHT, PANEL_MARGIN_TOP);
        rectPanel.offsetMax = new Vector2(-PANEL_MARGIN_RIGHT, -PANEL_MARGIN_BOTTOM);
        
        // Forcer la largeur du panel
        rectPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, PANEL_WIDTH);
        
        CreateLogsArea(panelGO);
        
        panelDroit = panelGO;
        panelDroit.SetActive(false);
    }

    private void CreateLogsArea(GameObject panelParent)
    {
        // Créer le conteneur pour les logs
        GameObject logsContainer = new GameObject("LogsContainer");
        logsContainer.transform.SetParent(panelParent.transform, false);
        
        // Configuration du RectTransform du conteneur
        RectTransform rectLogs = logsContainer.AddComponent<RectTransform>();
        rectLogs.anchorMin = new Vector2(0, 0);
        rectLogs.anchorMax = new Vector2(1, 1);
        rectLogs.offsetMin = new Vector2(LOGS_MARGIN, LOGS_MARGIN);
        rectLogs.offsetMax = new Vector2(-LOGS_MARGIN, -LOGS_MARGIN);
        
        // Créer la zone de texte avec la méthode utilitaire
        zoneLogs = CreateUIText(logsContainer, "ZoneLogs", "Choisissez une action pour chaque carte en la sélectionnant.", 
                               LOGS_FONT_SIZE, Color.white, TextAlignmentOptions.TopLeft);
        // Charger la police Poppins si disponible
        TMP_FontAsset poppinsFont = Resources.Load<TMP_FontAsset>("Fonts/Poppins-Regular SDF");
        zoneLogs.font = poppinsFont;
        
    }
    
    public void AddLog(string message)
    {
        logs.Add(message);
        
        if (logs.Count > MAX_LOGS)
            logs.RemoveAt(0);
        
        zoneLogs?.SetText(string.Join("\n", logs));
    }
    
    public void ShowVictory(int score)
    {
        // Créer le panel de victoire
        GameObject panelVictoire = new GameObject("PanelVictoire");
        panelVictoire.transform.SetParent(transform, false);
        
        RectTransform rectVictoire = panelVictoire.AddComponent<RectTransform>();
        rectVictoire.anchorMin = new Vector2(0.5f, 0.5f);
        rectVictoire.anchorMax = new Vector2(0.5f, 0.5f);
        rectVictoire.sizeDelta = new Vector2(VICTORY_PANEL_WIDTH, VICTORY_PANEL_HEIGHT);
        rectVictoire.anchoredPosition = Vector2.zero;
        
        Image imageFond = panelVictoire.AddComponent<Image>();
        imageFond.color = new Color(0, 0, 0, VICTORY_PANEL_OPACITY);
        
        // Créer le texte de victoire
        GameObject texteVictoire = new GameObject("TexteVictoire");
        texteVictoire.transform.SetParent(panelVictoire.transform, false);
        
        RectTransform rectTexte = texteVictoire.AddComponent<RectTransform>();
        rectTexte.anchorMin = new Vector2(0.5f, 0.7f);
        rectTexte.anchorMax = new Vector2(0.5f, 0.7f);
        rectTexte.sizeDelta = new Vector2(350, 50);
        rectTexte.anchoredPosition = Vector2.zero;
        
        CreateUIText(texteVictoire, "TexteVictoire", "VICTOIRE !", VICTORY_TEXT_SIZE, Color.yellow, TextAlignmentOptions.Center);
        
        // Créer le texte du score
        GameObject texteScore = new GameObject("TexteScore");
        texteScore.transform.SetParent(panelVictoire.transform, false);
        
        RectTransform rectScore = texteScore.AddComponent<RectTransform>();
        rectScore.anchorMin = new Vector2(0.5f, 0.5f);
        rectScore.anchorMax = new Vector2(0.5f, 0.5f);
        rectScore.sizeDelta = new Vector2(350, 30);
        rectScore.anchoredPosition = Vector2.zero;
        
        CreateUIText(texteScore, "TexteScore", $"Score : {score}", SCORE_TEXT_SIZE, Color.white, TextAlignmentOptions.Center);
        
        // Créer le bouton de relance
        GameObject boutonRelance = new GameObject("BoutonRelance");
        boutonRelance.transform.SetParent(panelVictoire.transform, false);
        
        RectTransform rectBouton = boutonRelance.AddComponent<RectTransform>();
        rectBouton.anchorMin = new Vector2(0.5f, 0.3f);
        rectBouton.anchorMax = new Vector2(0.5f, 0.3f);
        rectBouton.sizeDelta = new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT);
        rectBouton.anchoredPosition = Vector2.zero;
        
        Image imageBouton = boutonRelance.AddComponent<Image>();
        imageBouton.color = new Color(0.2f, 0.6f, 1f, 1f);
        
        Button bouton = boutonRelance.AddComponent<Button>();
        bouton.onClick.AddListener(RestartGame);
        
        CreateUIText(boutonRelance, "TexteBouton", "Rejouer", BUTTON_TEXT_SIZE, Color.white, TextAlignmentOptions.Center);
    }

    public void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    // Méthodes utilitaires pour la création d'UI
    private TMP_Text CreateUIText(GameObject parent, string nom, string texte, float taille, Color couleur, TextAlignmentOptions alignement)
    {
        GameObject texteGO = new GameObject(nom);
        texteGO.transform.SetParent(parent.transform, false);
        
        RectTransform rectTexte = texteGO.AddComponent<RectTransform>();
        rectTexte.anchorMin = Vector2.zero;
        rectTexte.anchorMax = Vector2.one;
        rectTexte.offsetMin = Vector2.zero;
        rectTexte.offsetMax = Vector2.zero;
        
        TMP_Text tmpTexte = texteGO.AddComponent<TextMeshProUGUI>();
        tmpTexte.text = texte;
        tmpTexte.fontSize = taille;
        tmpTexte.color = couleur;
        tmpTexte.alignment = alignement;
        
        return tmpTexte;
    }
} 