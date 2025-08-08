using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class CarteUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Composants UI")]
    public Image imageCarte;
    public TMP_Text nomText; 
    public TMP_Text attaqueText;
    public TMP_Text defenseText;
    public TMP_Text nameCapacity;
    public TMP_Text descriptionCapacity;
    private CardAnimations cardAnimations;
    private Coroutine pulseCoroutine;
    private Coroutine swingCoroutine;
    private Coroutine pulseAtk1Coroutine;
    private Coroutine pulseAtk2Coroutine;


    [Header("Icônes d'état")]
    public GameObject atk1Icon; // Icône première attaque
    public GameObject atk2Icon; // Icône deuxième attaque
    public GameObject passedIcon; // Icône "passé"

    [Header("Identification")]
    public string carteID; // ID unique de la carte
    public int indexCarte; // Index dans la collection

    [Header("Effets visuels")]
    public Vector3 offsetHover = new Vector3(0, 450, 0);
    public float rotationMax = 8f;
    
    private Vector3 positionInitiale;
    public RectTransform rectTransform;
    public bool isSelect = false;
    public int indexHierarchieOriginal;
    private Color couleurNormale;
    private Color couleurAssombrie;

    public MainUIManager mainUIManager;

    public bool isCartePlayer = false;
    public bool isCarteAdversaire = false;

    void Awake()
    {
        imageCarte ??= GetComponent<Image>();

        couleurNormale = imageCarte.color;
        couleurAssombrie = couleurNormale * 0.7f;
        couleurAssombrie.a = couleurNormale.a;
        
        Transform atk1Transform = transform.Find("atk1");
        atk1Icon = atk1Transform.gameObject;
        Transform atk2Transform = transform.Find("atk2");
        atk2Icon = atk2Transform.gameObject;
        Transform passedTransform = transform.Find("passed");
        passedIcon = passedTransform.gameObject;

        cardAnimations = GetComponent<CardAnimations>();

        HideAllIcons();
    }

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        // Ne pas enregistrer positionInitiale ici, car le layout n'a pas encore positionné la carte
        indexHierarchieOriginal = transform.GetSiblingIndex();
        mainUIManager = MainUIManager.Instance;
    }

    public void setAttributesInitCard(CarteData data)
    {
        imageCarte.sprite = data.image;
        nomText?.SetText(data.nom);
        attaqueText?.SetText(data.attaque.ToString());
        defenseText?.SetText(data.defense.ToString());
        nameCapacity?.SetText(data.nameCapacity);
        descriptionCapacity?.SetText(data.descriptionCapacity);
        
        carteID = data.idCard.ToString();
        gameObject.name = $"CarteUI_{data.nom}_id{data.idCard}_inst{data.instanceId}";
        
        HideAllIcons();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //StartCoroutine(cardAnimations.ColorFlash());

        if(GameManager.mode == "deck"){

            if (!isSelect)
            {
                if (CountSelectedCards() < GameManager.MAX_CARTES_TAPIS){
                    SelectCard();
                    //if (pulseCoroutine == null)
                    //    pulseCoroutine = StartCoroutine(cardAnimations.Pulse(0.7f, 0.95f, 1f));
    
                }
            }
            else{

                if (pulseCoroutine != null)
                {
                    //StopCoroutine(pulseCoroutine);
                   // pulseCoroutine = null;
                    //rectTransform.localScale = new Vector3(0.8f, 0.8f, 1f);
                }
                DeselectCard();
            }

            int nombreCartesSelectionnees = CountSelectedCards();
            mainUIManager.ShowValidateButton(nombreCartesSelectionnees >= GameManager.MAX_CARTES_TAPIS);
        }
    }

    private void SelectCard()
    {
        // Désactiver le LayoutGroup du parent au premier clic (pour le board)
        HorizontalLayoutGroup layoutGroup = transform.parent?.GetComponent<HorizontalLayoutGroup>();
        layoutGroup.enabled = false;
        
        // Enregistre la position initiale à la première sélection, après le layout
        positionInitiale = rectTransform.anchoredPosition;
        
        isSelect = true;
        rectTransform.anchoredPosition = positionInitiale + offsetHover;
        
        // Met la carte au premier plan et applique la rotation
        transform.SetAsLastSibling();
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-rotationMax, rotationMax));
    }

    private void DeselectCard()
    {
        isSelect = false;
        rectTransform.anchoredPosition = positionInitiale;
        
        // Remet la carte à sa position d'origine dans la hiérarchie
        transform.SetSiblingIndex(indexHierarchieOriginal);
        
        // Remet la rotation normale
        transform.localRotation = Quaternion.identity;
    }

    private int CountSelectedCards()
    {
        Transform parentPanel = transform.parent;
        return parentPanel.GetComponentsInChildren<CarteUI>(true)
            .Count(carte => carte.isSelect);
    }

    // Méthode pour masquer toutes les icônes
    public void HideAllIcons()
    {
        atk1Icon.SetActive(false);
        atk2Icon.SetActive(false);
        passedIcon.SetActive(false);
    }
    // Méthode pour afficher l'icône "passé"
    public void AfficherIconePassed()
    {
        HideAllIcons();
        passedIcon.SetActive(true);
        RectTransform passedRect = passedIcon.GetComponent<RectTransform>();

        swingCoroutine = StartCoroutine(SwingSablier(passedRect));
    }

    private IEnumerator SwingSablier(RectTransform passedRect, float angleMax = 10f, float speed = 2f)
    {
        float elapsed = 0f;
        while (true)
        {
            // oscillation entre -angleMax et +angleMax (en degrés)
            float angle = Mathf.Sin(elapsed * speed) * angleMax;
            passedRect.localRotation = Quaternion.Euler(0, 0, angle);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator Pulse(RectTransform rectTransform, float minScale = 0.9f, float maxScale = 1.1f, float speed = 2f)
    {
        float elapsed = 0f;
        while (true)
        {
            float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(elapsed * speed) + 1f) / 2f);
            rectTransform.localScale = new Vector3(scale, scale, 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // Méthode pour afficher l'icône d'attaque (première ou deuxième)
    public void ShowAttackIcon(int numeroAttaque)
    {
        //passedIcon.SetActive(false);
        atk1Icon.SetActive(true);
        atk2Icon.SetActive(false);
    
        RectTransform atkRect = atk1Icon.GetComponent<RectTransform>();
        pulseAtk1Coroutine  = StartCoroutine(Pulse(atkRect));

        if (numeroAttaque == 2){
            atk2Icon.SetActive(true);
            RectTransform atkRect2 = atk2Icon.GetComponent<RectTransform>();
            pulseAtk2Coroutine  = StartCoroutine(Pulse(atkRect2));
        }
    }
    public void SetAtk1IconColor(string hexColor)
    {
        Image img = atk1Icon.GetComponent<Image>();
        Color color;
        if (!hexColor.StartsWith("#")) hexColor = "#" + hexColor;
        if (ColorUtility.TryParseHtmlString(hexColor, out color))
            img.color = color;
    }
    public void SetAtk2IconColor(string hexColor)
    {
        Image img = atk2Icon.GetComponent<Image>();
        Color color;
        if (!hexColor.StartsWith("#")) hexColor = "#" + hexColor;
        if (ColorUtility.TryParseHtmlString(hexColor, out color))
            img.color = color;
        
    }
    public void SetAtk1IconTooltip(string attackerName, int atk)
    {
        AttackIconTooltip tooltip = atk1Icon.GetComponent<AttackIconTooltip>();
        tooltip.attackName = attackerName + " -" + atk;
    }
    public void SetAtk2IconTooltip(string attackerName, int atk)
    {
        AttackIconTooltip tooltip = atk2Icon.GetComponent<AttackIconTooltip>();
        tooltip.attackName = attackerName + " -" + atk;
    }
}
