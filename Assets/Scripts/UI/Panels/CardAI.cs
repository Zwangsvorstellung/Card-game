using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class CardAI : MonoBehaviour, IPointerClickHandler
{
    [Header("Composants UI")]
    public Image imageCarte;
    public TMP_Text nomText; 
    public TMP_Text attaqueText;
    public TMP_Text defenseText;
    public TMP_Text nameCapacity;
    public TMP_Text descriptionCapacity;

    public CardsAnimation cardsAnimation;

    public int attaqueValue;
    public int defenseValue;

    [Header("Icônes d'état")]
    public GameObject atk1Icon; // Icône première attaque
    public GameObject atk2Icon; // Icône deuxième attaque
    public GameObject passedIcon; // Icône "passé"
    public GameObject freezeIcon; // Icône "freeze"

    public RectTransform rectTransform;
    private Vector3 startPosition;
    public Vector3 offsetClick;

    //private LayoutElement layoutElement;
    //public LayoutGroup layoutGroup;

    public bool isCardOpponent = true;
    public bool isSelect = false;
    public bool isFrozen = false;
    public bool actionChoiceDo = false;
    public string stateOffensif;
    public string stateDefensif;
    public string target;
    public int targetID;
    public string lastTarget;
    public int lastTargetID;

    public string nameAttacker;
    public int idAttacker;

    [Header("Identification")]
    public string nameCard;
    public string instanceId; // ID unique de la carte
    public int idCard;
    public int indexCarte; // Index dans la collection

    void Awake()
    {
        stateDefensif = "notCibled";
        stateOffensif = "wait";
        cardsAnimation = GetComponent<CardsAnimation>();
    }

    void Update()
    {
        if(stateOffensif == "passed"){
            passedIcon.SetActive(true);
            RectTransform passedRect = passedIcon.GetComponent<RectTransform>();
            StartCoroutine(CardsAnimation.SwingSablier(passedRect));
        }
        else{
            passedIcon.SetActive(false);
        }

        if(stateDefensif == "cibled"){
            atk1Icon.SetActive(true);
        }else{
            atk1Icon.SetActive(false);
        }

        if(isFrozen){
            freezeIcon.SetActive(true);
        }else{
           freezeIcon.SetActive(false);
        }
    }

    void OnEnable() => BoardManager.cardsOnBoardAI.Add(this);
    void OnDisable() => BoardManager.cardsOnBoardAI.Remove(this);

    public void setAttributesInitCardAI(CarteData data)
    {
        imageCarte.sprite = data.image;
        nameCard = data.nom;
        nomText?.SetText(data.nom);
        attaqueText?.SetText(data.attaque.ToString());
        defenseText?.SetText(data.defense.ToString());
        nameCapacity?.SetText(data.nameCapacity);
        descriptionCapacity?.SetText(data.descriptionCapacity);

        attaqueValue = data.attaque;
        defenseValue = data.defense;
        
        instanceId = data.instanceId;
        idCard = data.idCard;
        gameObject.name = $"CarteAI_{data.nom}_id{data.idCard}_inst{data.instanceId}";   
        
        HideAllIcons();
    }

    public void HideAllIcons()
    {
        atk1Icon.SetActive(false);
        atk2Icon.SetActive(false);
        passedIcon.SetActive(false);
        freezeIcon.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayerActionManager.Instance.ClickSelectTargetOnBoard(this);
    }

    public int isSelectCard()
    {   
        if(stateDefensif != "cibled"){

            StartCoroutine(cardsAnimation.ColorFlash(this, Color.red, 0.5f));
            StartCoroutine(cardsAnimation.Shake(0.3f, 5f));

            imageCarte.color = new Color(0.5f, 0.7f, 1f, 1f);

            stateDefensif = "cibled";
            CardUI cardUI = BoardManager.Instance.getDataAttacker();
            nameAttacker = cardUI.nameCard;
            idAttacker = cardUI.idCard;

            return idAttacker;
        }
        return 0;
    }

    /// <summary>
    /// Vérifie si deux cartes IA sont adjacentes (pour les alliés).
    /// </summary>
    public bool IsAdjacentTo(CardAI a, CardAI b)
    {
        if (a == null || b == null) return false;
        return Mathf.Abs(a.indexCarte - b.indexCarte) == 1;
    }
    
    /// <summary>
    /// Vérifie si une carte IA est adjacente à une carte joueur (pour les attaques).
    /// </summary>
    public bool IsAdjacentTo(CardAI a, CardUI b)
    {
        if (a == null || b == null) return false;
        return Mathf.Abs(a.indexCarte - b.indexCarte) == 1;
    }

    public bool HasCapacity(IAAction.Capacity cap)
    {
        if (nameCapacity == null) return false;

        // Comparaison rapide : le texte contient le nom de l'enum
        return nameCapacity.text.Contains(cap.ToString());
    }

}
