using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class GameManager2 : MonoBehaviour
{
    public static GameManager2 Instance { get; private set; }
    public static string mode;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }
    
    // Méthode pour récupérer les cartes sélectionnées dans l'ordre
    public List<CarteData> GetSelectedCards()
    {
        MainUIManager mainUIManager = GameObject.Find("MainUIManager").GetComponent<MainUIManager>();
        List<CarteData> mainList = mainPlayerA.ToList();
        
        return mainUIManager.transform.GetComponentsInChildren<CardMain>(true)
            .Where(card => card.isSelect)
            .OrderBy(card => card.transform.GetSiblingIndex())
            .Select(cardMain => mainList.FirstOrDefault(c => c.idCard.ToString() == cardMain.carteID))
            .Where(carteData => carteData != null)
            .ToList();
    }

}
