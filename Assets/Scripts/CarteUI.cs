using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class CarteUI : MonoBehaviour
{
    [Header("Composants UI")]
    public Image imageCarte;
    public TMP_Text nomText; 
    public TMP_Text attaqueText;
    public TMP_Text defenseText;
    public TMP_Text nameCapacity;
    public TMP_Text descriptionCapacity;
    private Coroutine pulseCoroutine;
    private Coroutine pulseAtk1Coroutine;
    private Coroutine pulseAtk2Coroutine;

    [Header("Icônes d'état")]
    public GameObject atk1Icon; // Icône première attaque
    public GameObject atk2Icon; // Icône deuxième attaque
    public GameObject passedIcon; // Icône "passé"
    public GameObject freezeIcon; // Icône "freeze"

    [Header("Identification")]
    public string carteID; // ID unique de la carte
    public int indexCarte; // Index dans la collection
    
    private Vector3 startPosition;
    public bool isSelect = false;
    public int indexHierarchieOriginal;
    private Color colorDefault;
    private Color colorDimmed;

    public bool isCardPlayer = false;
    public bool isCardOpponent = false;
    public string nameCard = "";

    void Awake()
    {
        imageCarte ??= GetComponent<Image>();

        colorDefault = imageCarte.color;
        colorDimmed = colorDefault * 0.7f;
        colorDimmed.a = colorDefault.a;
        Transform atk1Transform = transform.Find("atk1");
        atk1Icon = atk1Transform.gameObject;
        Transform atk2Transform = transform.Find("atk2");
        atk2Icon = atk2Transform.gameObject;
        Transform freezeTransform = transform.Find("freezeIcon");
        freezeIcon = freezeTransform.gameObject;
        freezeIcon.GetComponent<Image>().color = Color.red;
        Transform passedTransform = transform.Find("passed");
        passedIcon = passedTransform.gameObject;
        //cardsAnimation = GetComponent<CardsAnimation>();
    }

    void Start()
    {
        // test
        // Ne pas enregistrer startPosition ici, car le layout n'a pas encore positionné la carte
        indexHierarchieOriginal = transform.GetSiblingIndex();
    }

    /*private int CountSelectedCards()
    {
        Transform parentPanel = transform.parent;
        return parentPanel.GetComponentsInChildren<CarteUI>(true)
            .Count(carte => carte.isSelect);
    }
    */


    public void ShowAttackIcon(int numberAtk)
    {
        //passedIcon.SetActive(false);
        atk1Icon.SetActive(true);
        atk2Icon.SetActive(false);
    
        RectTransform atkRect = atk1Icon.GetComponent<RectTransform>();
        //pulseAtk1Coroutine  = StartCoroutine(Pulse(atkRect));

        if (numberAtk == 2){
            atk2Icon.SetActive(true);
            RectTransform atkRect2 = atk2Icon.GetComponent<RectTransform>();
            //pulseAtk2Coroutine  = StartCoroutine(Pulse(atkRect2));
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
        TooltipTrigger tooltip = atk1Icon.GetComponent<TooltipTrigger>();
        tooltip.attackName = attackerName + " -" + atk;
    }
    public void SetAtk2IconTooltip(string attackerName, int atk)
    {
        TooltipTrigger tooltip = atk2Icon.GetComponent<TooltipTrigger>();
        tooltip.attackName = attackerName + " -" + atk;
    }
}
