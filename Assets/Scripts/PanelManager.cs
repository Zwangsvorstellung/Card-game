using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PanelManager : MonoBehaviour
{
    public static PanelManager instance;
    
    // Configuration du panel
    private const int MAX_LOGS = 150;
    private const float PANEL_OPACITY = 0.85f;  
    private const float PANEL_WIDTH = 400f;
    private const float PANEL_MARGIN_TOP = 125f;
    private const float PANEL_MARGIN_BOTTOM = 25f;
    private const float PANEL_MARGIN_RIGHT = 60f;
    private const float LOGS_MARGIN = 20f;
    private const float LOGS_FONT_SIZE = 18f;
    
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
    private TMP_Text zoneLogs;
    private List<string> logs = new List<string>();
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateRightPanel();
    }
    
    void Update()
    {
        bool nouvelleVueBoard = CamController.Instance?.ViewBoardOn ?? false;
        
        if (panelDroit != null && panelDroit.activeSelf != nouvelleVueBoard)
        {
            panelDroit?.SetActive(nouvelleVueBoard);
        }
    }
    
    private void CreateRightPanel()
    {
        // Chercher le Canvas dans la scène
        Canvas canvas = FindFirstObjectByType<Canvas>();
        
        // Créer le panel droit
        GameObject panelGO = new GameObject("PanelDroit");

        Transform boardRoot = GameObject.Find("BoardManager")?.transform;
        if (boardRoot != null)
            panelGO.transform.SetParent(boardRoot, false);
        else
            panelGO.transform.SetParent(canvas.transform, false); // Fallback

        
        // Ajouter les composants UI
        Image imagePanel = panelGO.AddComponent<Image>();
        imagePanel.color = new Color(0, 0, 0, PANEL_OPACITY);
        
        RectTransform rectPanel = panelGO.GetComponent<RectTransform>();
        
        rectPanel.anchorMin = new Vector2(1, 0.5f);
        rectPanel.anchorMax = new Vector2(1, 0.5f);
        rectPanel.pivot = new Vector2(1, 0.5f);
        rectPanel.anchoredPosition = new Vector2(-PANEL_MARGIN_RIGHT, 0);
        rectPanel.sizeDelta = new Vector2(PANEL_WIDTH, 0);

        // Forcer la largeur du panel
        rectPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, PANEL_WIDTH);
        float hauteurPanel = canvas.GetComponent<RectTransform>().rect.height - (PANEL_MARGIN_TOP + PANEL_MARGIN_BOTTOM);
        rectPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, hauteurPanel);
        CreateLogsArea(panelGO);
        
        panelDroit = panelGO;
        panelDroit.SetActive(false);
    }

    private void CreateLogsArea(GameObject panelParent)
    {
        GameObject logsContainer = new GameObject("LogsContainer");
        logsContainer.transform.SetParent(panelParent.transform, false);

        // === RectTransform du conteneur principal ===
        RectTransform rectLogs = logsContainer.AddComponent<RectTransform>();
        rectLogs.anchorMin = new Vector2(0, 0);
        rectLogs.anchorMax = new Vector2(1, 1);
        rectLogs.offsetMin = new Vector2(LOGS_MARGIN, LOGS_MARGIN);
        rectLogs.offsetMax = new Vector2(-LOGS_MARGIN, -LOGS_MARGIN);

        // === Image de fond + Mask ===
        Image bgImage = logsContainer.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.25f);
        Mask mask = logsContainer.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // === ScrollRect ===
        ScrollRect scrollRect = logsContainer.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // === Scrollbar ===
        GameObject scrollbarGO = new GameObject("ScrollbarVertical");
        scrollbarGO.transform.SetParent(logsContainer.transform, false);
        RectTransform scrollbarRect = scrollbarGO.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(6f, 0);
        scrollbarRect.anchoredPosition = Vector2.zero;

        Image scrollbarImage = scrollbarGO.AddComponent<Image>();
        scrollbarImage.color = new Color(1f, 1f, 1f, 0.1f);

        Scrollbar scrollbar = scrollbarGO.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // === Handle ===
        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(scrollbarGO.transform, false);
        RectTransform handleRect = handleGO.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        Image handleImage = handleGO.AddComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.35f);
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;

        // Connect Scrollbar au ScrollRect
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // === Content ===
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(logsContainer.transform, false);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        scrollRect.content = contentRect;

        // Layout et Fitter
        VerticalLayoutGroup layout = contentGO.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 5f;

        ContentSizeFitter fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Zone de texte
        zoneLogs = CreateUIText(contentGO, "ZoneLogs", "Choisissez une action pour chaque carte en la sélectionnant.",
                                LOGS_FONT_SIZE, Color.white, TextAlignmentOptions.TopLeft);

        TMP_FontAsset poppinsFont = Resources.Load<TMP_FontAsset>("Fonts/Poppins-Regular SDF");
        if (poppinsFont != null)
            zoneLogs.font = poppinsFont;

        zoneLogs.enableWordWrapping = true;
        zoneLogs.overflowMode = TextOverflowModes.Overflow;

        // RectTransform du texte
        RectTransform zoneLogsRect = zoneLogs.GetComponent<RectTransform>();
        zoneLogsRect.anchorMin = new Vector2(0, 1);
        zoneLogsRect.anchorMax = new Vector2(1, 1);
        zoneLogsRect.pivot = new Vector2(0.5f, 1);
        zoneLogsRect.anchoredPosition = Vector2.zero;
        zoneLogsRect.sizeDelta = new Vector2(0, 0);
    }

    
    public void AddLog(string message)
    {
        logs.Add(message);
        
       // if (logs.Count > MAX_LOGS)
           // logs.RemoveAt(0);
        
        zoneLogs?.SetText(string.Join("\n", logs));

        // ➤ Forcer le scroll tout en bas
        ScrollRect scroll = zoneLogs?.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases(); // Important pour forcer le layout à se mettre à jour
            scroll.verticalNormalizedPosition = 0f;
        }
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
    private TMP_Text CreateUIText(GameObject parent, string name, string text, float size, Color color, TextAlignmentOptions align)
    {
        GameObject texteGO = new GameObject(name);
        texteGO.transform.SetParent(parent.transform, false);
        
        RectTransform rectTexte = texteGO.AddComponent<RectTransform>();
        rectTexte.anchorMin = Vector2.zero;
        rectTexte.anchorMax = Vector2.one;
        rectTexte.offsetMin = Vector2.zero;
        rectTexte.offsetMax = Vector2.zero;
        
        TMP_Text tmpTexte = texteGO.AddComponent<TextMeshProUGUI>();
        tmpTexte.text = text;
        tmpTexte.fontSize = size;
        tmpTexte.color = color;
        tmpTexte.alignment = align;
        
        return tmpTexte;
    }
} 