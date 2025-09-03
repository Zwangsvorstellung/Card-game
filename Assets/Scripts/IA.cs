using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using static IAAction;

public class IA : MonoBehaviour
{
    private static IA instance;
    public static IA Instance => instance;
    
    private float delayAction = 0.5f;
    
    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
    }
    
    public void StartAITurn()
    {
        StartCoroutine(ExecuteAITurn());
    }
    
    private IEnumerator ExecuteAITurn()
    {
       // Debug.Log("[IA] Début du tour IA");

        GameManager.numberOfAttacksUsedIA = 0;

        List<CarteBoardInteraction> cardsIA = GetCardOpponent();
        List<CarteBoardInteraction> cardsPlayer = GetCardsPlayer();

        if (cardsIA.Count == 0)
        {
            Debug.Log("[IA] Aucune carte adverse trouvée");
            yield break;
        }

        const int maxAttaques = 2;
        const int seuilMinAttaque = 2; // seuil minimal pour qu'une attaque soit envisagée

        int attacksExecuted = 0;

        // On crée une liste temporaire pour gérer les cartes IA pouvant attaquer
        List<CarteBoardInteraction> cardsIADisponibles = new List<CarteBoardInteraction>(cardsIA);
        List<CarteBoardInteraction> cardsAAttaque = new List<CarteBoardInteraction>();

        while (attacksExecuted < maxAttaques && cardsIADisponibles.Count > 0)
        {
            int bestScoring = 0;
            CarteBoardInteraction bestAttacker = null;
            CarteBoardInteraction bestTarget = null;

            // Pour chaque carte IA disponible, on décide l'action
            foreach (var cardsIADispo in cardsIADisponibles)
            {
                if(cardsIADispo.freeze){
                    Debug.Log($"[IA] Carte {cardsIADispo.name} : freeze");
                    PanelManager.instance?.AddLog($"{cardsIADispo.name} : freeze");
                    continue;
                }
                var decision = IAAction.DecideAction(cardsIADispo, cardsIA, cardsPlayer);
                Debug.Log($"[IA] Carte {cardsIADispo.name} : attack={decision.attack}, score={decision.score}, target={(decision.target != null ? decision.target.name : "null")}");

                if (decision.attack && decision.score > bestScoring && decision.score >= seuilMinAttaque)
                {
                    bestScoring = decision.score;
                    bestAttacker = cardsIADispo;
                    bestTarget = decision.target;
                }
            }

            if (bestAttacker != null && bestTarget != null)
            {
                Debug.Log($"[IA] {bestAttacker.name} attaque la cible {bestTarget} avec un score {bestScoring}");
                ExecuteAttack(bestAttacker, bestTarget);

                attacksExecuted++;
                cardsAAttaque.Add(bestAttacker);
                // On retire le meilleur attaquant de la liste pour qu'il n'attaque qu'une fois
                cardsIADisponibles.Remove(bestAttacker);

                cardsPlayer.Remove(bestTarget);

                yield return new WaitForSeconds(delayAction);
            }
            else
            {
                break;
            } 
        }

        foreach (var cardIA in cardsIA)
        {
            if (!cardsAAttaque.Contains(cardIA))
            {
                Debug.Log($"[IA] {cardIA.name} : PASSER");
                ExecutePass(cardIA);
                yield return new WaitForSeconds(delayAction);
            }
        }

        //Debug.Log("[IA] Tour IA terminé");

        CarteBoardInteraction instance = FindAnyObjectByType<CarteBoardInteraction>();
        if (instance != null)
            instance.ApplyAllAttacks();

        yield return new WaitForSeconds(1f);
        CarteBoardInteraction.EndAITurn();
    }
    
    private List<CarteBoardInteraction> GetCardOpponent()
    {
        return CarteBoardInteraction.AllCardsInteractions
            .Where(c => c.isCardOpponent)
            .ToList();
    }

    private List<CarteBoardInteraction> GetCardsPlayer()
    {
        return CarteBoardInteraction.AllCardsInteractions
            .Where(c => c.isCardPlayer)
            .ToList();
    }
    
    private void ExecuteAttack(CarteBoardInteraction attacker, CarteBoardInteraction target)
    {        
        ApplyIAAttackVisualEffect(attacker);
        SimulateAIAttack(attacker, target);
    }
    
    private void SimulateAIAttack(CarteBoardInteraction attacker, CarteBoardInteraction target)
    {
        GameManager.numberOfAttacksUsedIA++;

        attacker.choiceDo = true;
        attacker.stateOffensif = "atk";
        PanelManager.instance.AddLog($"ATTAQUE IA ({GameManager.numberOfAttacksUsedIA}/{GameManager.numberOfAttacksMax})");

        ApplyAttack(attacker.nameCard, target);
    }

    private void SelectRandomTarget(string nameAttacker, int numberAtk)
    {
        var cardsPlayer = CarteBoardInteraction.AllCardsInteractions
            .Where(c => c.isCardPlayer)
            .ToList();

        if (cardsPlayer.Count > 0)
        {
            CarteBoardInteraction target = cardsPlayer[Random.Range(0, cardsPlayer.Count)];
            ApplyAttack(nameAttacker, target);
        }
    }
    
    private void ApplyAttack(string nameAttacker, CarteBoardInteraction target)
    {
        if (target == null) return;
        
        CarteBoardInteraction cardAttacker = CarteBoardInteraction.AllCardsInteractions
        .FirstOrDefault(c => c.nameCard == nameAttacker);

        if (cardAttacker == null) return;
    
        CarteUI carteUI = target.GetComponent<CarteUI>();
        if (carteUI == null) return;
        
        target.targetCount++;
        carteUI.ShowAttackIcon(target.targetCount);

        PanelManager.instance.AddLog($"{nameAttacker} : ATQ -> {target.nameCard}");
        
        CarteScriptableObject so = Resources.LoadAll<CarteScriptableObject>("CartesGenerees").FirstOrDefault(c => c.nom == nameAttacker);
        
        if (so != null && !string.IsNullOrEmpty(so.color))
        {
            switch (target.targetCount)
            {
                case 1:
                    carteUI.SetAtk1IconColor(so.color);
                    carteUI.SetAtk1IconTooltip(so.nom, so.atk);
                    break;
    
                case 2:
                    carteUI.SetAtk2IconColor(so.color);
                    carteUI.SetAtk2IconTooltip(so.nom, so.atk);
                    break;
            }
        }

        target.stateDefensif = "isAttacked";

        target.ComputeAndStoreDamageIA(cardAttacker, target, nameAttacker, target.nameCard);
    }

    private void ExecutePass(CarteBoardInteraction card)
    {
        // Simuler le clic sur la carte pour la sélectionner
        card.OnPointerClick(null);
        
        // Attendre un peu puis cliquer sur le bouton passer
        StartCoroutine(ClickPassButton(card));
    }
    
    private IEnumerator ClickPassButton(CarteBoardInteraction card)
    {
        yield return new WaitForSeconds(0.2f);

        Image imgCard = card.GetComponent<Image>() ?? card.GetComponentInChildren<Image>();
        imgCard.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        
        Button buttonPass = card.transform.Find("boutonPass")?.GetComponent<Button>();
        if (buttonPass != null && buttonPass.interactable)
        {          
            buttonPass.onClick.Invoke();
        }
        card.choiceDo = true;        
        card.stateOffensif = "passed";
    }
    
    public IEnumerator StartAITurnCoroutine()
    {
        yield return new WaitForSeconds(1f);
        StartAITurn();
    }
    
    private void ApplyIAAttackVisualEffect(CarteBoardInteraction card)
    {
        RectTransform rectTransform = card.GetComponent<RectTransform>();
                
        card.startPosition = rectTransform.anchoredPosition;
        
        LayoutElement layoutElement = card.GetComponent<LayoutElement>();
        
        Vector3 newPosition = card.startPosition + new Vector3(0, -50, 0);
        rectTransform.anchoredPosition = newPosition;
    }
} 
