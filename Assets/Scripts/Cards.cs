using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class Cards : MonoBehaviour
{
    [SerializeField] GameObject cardPrefab;
    [SerializeField] List<Sprite> lstItems;
    [SerializeField] List<Sprite> lstCards;
    [SerializeField] List<Sprite> lstFounds;

    void Awake()
    {
        for (int i = 0; i < 18; i++)
        {
            GameObject card = Instantiate(cardPrefab, this.transform);
            card.name = "Card" + i;

            card.transform.Find("ImQuestion").GetComponent<Image>().enabled = true;
            card.transform.Find("ImItem").GetComponent<Image>().enabled = false;

        }
        List<Sprite> lstSprites = new List<Sprite>();
        lstSprites.AddRange(lstItems);
        lstSprites.AddRange(lstItems);

        foreach (Transform child in transform)
        {
            int index = Random.Range(0, lstSprites.Count);
            child.Find("ImItem").GetComponent<Image>().sprite = lstSprites[index];
            lstCards.Add(lstSprites[index]);
            lstSprites.RemoveAt(index);
        }

    }

    public void CardsHide()
    {
        foreach (Transform child in transform)
        {
            bool hide = true;

            foreach (var item in lstFounds)
            {
                if (item.name == child.transform.Find("ImItem").GetComponent<Image>().sprite.name)
                {
                    hide = false;
                    break;
                }
            }

            if (hide)
            {
                child.transform.Find("ImQuestion").GetComponent<Image>().enabled = true;
                child.transform.Find("ImItem").GetComponent<Image>().enabled = false;
                child.GetComponent<EventTrigger>().enabled = true;
            }
        }
        
    }

    public void AddToCardsFounds(Sprite sp)
    {
        lstFounds.Add(sp);
    }

    public void UpdateProgress()
    {
        int pourcent = (lstFounds.Count * 100) / 9;

        if (pourcent >= 100)
        {
            
        }
    }
    
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
