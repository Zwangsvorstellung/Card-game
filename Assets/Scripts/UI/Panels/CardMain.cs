using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class CardMain : MonoBehaviour, IPointerClickHandler
{
    [Header("Composants UI")]
    public Image imageCarte;
    public TMP_Text nomText;
    public TMP_Text attaqueText;
    public TMP_Text defenseText;
    public TMP_Text nameCapacity;
    public TMP_Text descriptionCapacity;

    public RectTransform rectTransform;
    private Vector3 startPosition;

    public bool isSelect = false;
    public int indexHierarchieOriginal;
    public string nameCard = "";

    [Header("Identification")]
    public string carteID; // ID unique de la carte

    [Header("Effets visuels")]
    public Vector3 offsetHover;
    public float rotationMax;
    

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        // Ne pas enregistrer startPosition ici, car le layout n'a pas encore positionné la carte
        indexHierarchieOriginal = transform.GetSiblingIndex();
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
        gameObject.name = $"CardMain{data.nom}_id{data.idCard}_inst{data.instanceId}";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayerActionManager.Instance.ClickOnMainCard(this);
    }

    public void SelectCardMain()
    {              
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
    }

    public void DeselectCardMain()
    {
        isSelect = false;
        rectTransform.anchoredPosition = startPosition;
        
        // Remet la carte à sa position d'origine dans la hiérarchie
        transform.SetSiblingIndex(indexHierarchieOriginal);
        transform.localRotation = Quaternion.identity;
    }
}
