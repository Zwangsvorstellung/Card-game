using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

public class IA : MonoBehaviour
{
    private static IA instance;
    public static IA Instance => instance;
    
    private float delaiAction = 0.5f; // Délai entre chaque action de l'IA
    
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
        Debug.Log("[IA] Début du tour IA");
        GameManager.nombreAttaquesUtiliseesIA = 0;
        
        List<CarteBoardInteraction> opponentCards = GetCartesAdversaires();
        
        if (opponentCards.Count == 0)
        {
            Debug.Log("[IA] Aucune carte adverse trouvée");
            yield break;
        }
        
        // Pour chaque carte adverse, faire un choix
        foreach (var card in opponentCards)
        {
            if (card == null) 
                continue;
            
            yield return new WaitForSeconds(delaiAction);
            
            DecideCardAction(card);
        }
        
        Debug.Log("[IA] Tour IA terminé");

        CarteBoardInteraction instance = FindObjectOfType<CarteBoardInteraction>();
        if (instance != null)
            instance.ApplyAllAttacks();
        
        yield return new WaitForSeconds(1f);
        CarteBoardInteraction.EndAITurn();
    }
    
    private List<CarteBoardInteraction> GetCartesAdversaires()
    {
        return CarteBoardInteraction.AllCardsInteractions
            .Where(c => c.isCardAdversaire)
            .ToList();
    }
    
    private void DecideCardAction(CarteBoardInteraction card)
    {
        string nameCard = card.GetComponent<CarteUI>()?.nomText?.text ?? "Carte IA";
        
        // Logique simple : 70% de chance d'attaquer, 30% de passer
        float random = Random.Range(0f, 1f);
        
        if (random < 0.7f && GameManager.nombreAttaquesUtiliseesIA < 2)
        {
            Debug.Log($"[IA] {nameCard} : ATTAQUE");
            ExecuteAttack(card);
        }
        else
        {
            Debug.Log($"[IA] {nameCard} : PASSER");
            ExecutePass(card);
        }
    }
    
    private void ExecuteAttack(CarteBoardInteraction card)
    {        
        ApplyIAAttackVisualEffect(card);
        SimulateAIAttack(card);
    }
    
    private void SimulateAIAttack(CarteBoardInteraction card)
    {
        string nameCard = card.GetComponent<CarteUI>()?.nomText?.text ?? "Carte IA";
        
        GameManager.nombreAttaquesUtiliseesIA++;
        
        card.choiceDo = true;        
        card.stateOffensif = "atk";
        PanelManager.instance.AddLog($"ATTAQUE IA ({GameManager.nombreAttaquesUtiliseesIA}/{GameManager.nombreAttaquesMaximales})");
        
        SelectRandomTarget(nameCard, GameManager.nombreAttaquesUtiliseesIA);
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
        .FirstOrDefault(c => c.carteUI?.nomText?.text == nameAttacker);

        if (cardAttacker == null) return;
    
        CarteUI carteUI = cible.GetComponent<CarteUI>();
        if (carteUI == null) return;
        
        target.nombreCiblages++;
        carteUI.ShowAttackIcon(target.nombreCiblages);

        PanelManager.instance.AddLog($"{nameAttacker} : ATQ -> {carteUI.nomText.text}");
        
        CarteScriptableObject so = Resources.LoadAll<CarteScriptableObject>("CartesGenerees").FirstOrDefault(c => c.nom == nameAttacker);
        
        if (so != null && !string.IsNullOrEmpty(so.color))
        {
            switch (target.nombreCiblages)
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

        // État de la carte cible
        target.stateDefensif = "isAttacked";

        // Calcul des dégâts
        target.ComputeAndStoreDamageIA(cardAttacker, target, nameAttacker, carteUI.nomText.text);
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
        
        Button boutonPasser = card.transform.Find("BoutonPasser")?.GetComponent<Button>();
        if (boutonPasser != null && boutonPasser.interactable)
        {          
            boutonPasser.onClick.Invoke();
        }
        card.choiceDo = true;        
        card.stateOffensif = "passed";
    }
    
    // Méthode pour démarrer l'IA au début du jeu
    public IEnumerator StartAITurnCoroutine()
    {
        yield return new WaitForSeconds(1f);
        StartAITurn();
    }
    
    private void ApplyIAAttackVisualEffect(CarteBoardInteraction card)
    {
        RectTransform rectTransform = card.GetComponent<RectTransform>();
                
        card.positionInitiale = rectTransform.anchoredPosition;
        
        // Désactiver le LayoutElement pour que la carte ne soit plus affectée par le GridLayout
        LayoutElement layoutElement = card.GetComponent<LayoutElement>();
        //layoutElement.ignoreLayout = true;
        
        // Déplacer la carte vers le bas de 50 pixels
        Vector3 newPosition = card.positionInitiale + new Vector3(0, -50, 0);
        rectTransform.anchoredPosition = newPosition;
    }
} 
