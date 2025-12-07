using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class CardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Composants UI")]
    public Image imageCarte;
    public TMP_Text nomText; 
    public TMP_Text attaqueText;
    public TMP_Text defenseText;
    public TMP_Text nameCapacity;
    public TMP_Text descriptionCapacity;

    [Header("Icônes d'état")]
    public GameObject atk1Icon; // Icône première attaque
    public GameObject atk2Icon; // Icône deuxième attaque
    public GameObject passedIcon; // Icône "passé"
    public GameObject freezeIcon; // Icône "freeze"

    public RectTransform rectTransform;
    private Vector3 startPosition;
    public Vector3 newPosition;


    public bool isCardPlayer = false;
    public bool isCardOpponent = false;
    public bool isSelect = false;

    public string nameCard = "";

    [Header("Identification")]
    public string carteID; // ID unique de la carte
    public int indexCarte; // Index dans la collection


    void Awake()
    {

    }

    void Start()
    {

    }

    public void setAttributesInitCard(CarteData data)
    {
        imageCarte.sprite = data.image;
        nameCard = data.nom;
        nomText?.SetText(data.nom);
        attaqueText?.SetText(data.attaque.ToString());
        defenseText?.SetText(data.defense.ToString());
        nameCapacity?.SetText(data.nameCapacity);
        descriptionCapacity?.SetText(data.descriptionCapacity);
        
        carteID = data.idCard.ToString();
        gameObject.name = $"CarteUI_{data.nom}_id{data.idCard}_inst{data.instanceId}";
        
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
        if(GameManager.mode == "deck"){

            if (!isSelect)
            {
                if (MainUIManager.Instance.CountSelectedCards() < GameManager.MAX_CARTES_TAPIS){
                    SelectCardMain();
                    //if (pulseCoroutine == null)
                    //    pulseCoroutine = StartCoroutine(cardAnimations.Pulse(0.7f, 0.95f, 1f));
    
                }
            }
            else{

               /* if (pulseCoroutine != null)
                {
                    //StopCoroutine(pulseCoroutine);
                   // pulseCoroutine = null;
                    //rectTransform.localScale = new Vector3(0.8f, 0.8f, 1f);
                }
                */
               // DeselectCard();
            }

          //  int numberCardsSelect = CountSelectedCards();
         //   mainUIManager.ShowValidateButton(numberCardsSelect >= GameManager.MAX_CARTES_TAPIS);
        }
    }

    private void SelectCardMain()
    {              
        /*
         // Désactiver le LayoutGroup du parent au premier clic (pour le board)
        HorizontalLayoutGroup layoutGroup = transform.parent?.GetComponent<HorizontalLayoutGroup>();
        layoutGroup.enabled = false;
        
        // Enregistre la position initiale à la première sélection, après le layout
        startPosition = rectTransform.anchoredPosition;
        
        isSelect = true;
        rectTransform.anchoredPosition = startPosition + offsetHover;
        
        // Met la carte au premier plan et applique la rotation
        transform.SetAsLastSibling();
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-rotationMax, rotationMax));
    */
    
    }
}
