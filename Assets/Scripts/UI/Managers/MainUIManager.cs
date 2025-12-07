using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class MainUIManager : MonoBehaviour
{
    public static MainUIManager Instance { get; private set; }

    [SerializeField] private GameObject cartePrefab;
    [SerializeField] private Button confirmButton;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowHand(List<CarteData> cards)
    {
        GameManager.mode = "deck";
        if (cards?.Count > 0)
        {
            foreach (var card in cards)
            {
                GameObject cardGO = Instantiate(cartePrefab, transform);
                if(cardGO.TryGetComponent<CardUI>(out var cardUI))
                {
                    cardUI.setAttributesInitCard(card);
                }
            }
        }
    }

    public void ShowValidateButton(bool show)
    {
        confirmButton?.gameObject.SetActive(show);
    }

    public int CountSelectedCards()
    {
        return GetComponentsInChildren<CardUI>(true)
            .Count(card => card.isSelect);
    }

    public VerticalLayoutGroup getStateLayout()
    {
        return GetComponent<VerticalLayoutGroup>();
    }

    public void desactivatedLayoutGroup()
    {
        GetComponent<VerticalLayoutGroup>().enabled = false;
    }

    public void activatedLayoutGroup()
    {
        GetComponent<VerticalLayoutGroup>().enabled = true;
    }
}
