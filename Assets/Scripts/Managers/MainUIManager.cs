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
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void ShowHand(List<CarteData> cards)
    {
        GameManager.Instance.mode = "deck";
        
        if (cards?.Count > 0)
        {
            foreach (var card in cards)
            {
                GameObject cardGO = Instantiate(cartePrefab, transform);
                if(cardGO.TryGetComponent<CardMain>(out var cardMain))
                {
                    cardMain.setAttributesInitCard(card);
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
        return GetComponentsInChildren<CardMain>(true)
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
