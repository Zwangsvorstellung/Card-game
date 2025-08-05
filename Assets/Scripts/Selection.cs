using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;

public class Selection : MonoBehaviour
{
    public static int total = 0;
    Cards cards;
    static Sprite spCard1, spCard2;
    AudioSource audioFound, audioNotFound;

    void Awake()
    {
        cards = GameObject.Find("PanelCards").GetComponent<Cards>();
        audioFound = gameObject.GetComponent<AudioSource>();
        audioNotFound = GameObject.Find("PanelCards").GetComponent<AudioSource>();
    }

    public void SelectCard()
    {
        GetComponent<EventTrigger>().enabled = false;
        transform.Find("ImQuestion").GetComponent<Image>().enabled = false;
        transform.Find("ImItem").GetComponent<Image>().enabled = true;
        total++;

        if (total == 1)
        {
            spCard1 = transform.Find("ImItem").GetComponent<Image>().sprite;
            audioNotFound.Play();
        }

        if (total == 2)
        {
            spCard2 = transform.Find("ImItem").GetComponent<Image>().sprite;

            if (IsSameCard(spCard1, spCard2))
                cards.AddToCardsFounds(spCard1);
            
            audioNotFound.Play();
        }

        if (total == 3)
        {
            cards.CardsHide();
            total = 0;
            SelectCard();
        }
    }

    bool IsSameCard(Sprite s1, Sprite s2)
    {
        if (s1.name == s2.name) return true;
        return false;
    }
}
