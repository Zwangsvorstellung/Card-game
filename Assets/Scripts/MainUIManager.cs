using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUIManager : MonoBehaviour
{
    public static MainUIManager Instance { get; private set; }

    [SerializeField] private GameObject cartePrefab;
    [SerializeField] private Button boutonAction;

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
                if (cardGO.TryGetComponent<CarteUI>(out var cardUI))
                {
                    cardUI.ShowCard(card);
                }
            }
        }
    }

    public void ShowValidateButton(bool show)
    {
        boutonAction?.gameObject.SetActive(show);
    }
}
