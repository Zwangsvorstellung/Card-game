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
       // Debug.Log("[IA] Début du tour IA");

        GameManager.nombreAttaquesUtiliseesIA = 0;

        List<CarteBoardInteraction> cartesIA = GetCartesAdversaire();
        List<CarteBoardInteraction> cardsPlayer = GetCartesPlayer();

        if (cartesIA.Count == 0)
        {
            Debug.Log("[IA] Aucune carte adverse trouvée");
            yield break;
        }

        const int maxAttaques = 2;
        const int seuilMinAttaque = 2; // seuil minimal pour qu'une attaque soit envisagée

        int attaquesEffectuees = 0;

        // On crée une liste temporaire pour gérer les cartes IA pouvant attaquer
        List<CarteBoardInteraction> cartesIADisponibles = new List<CarteBoardInteraction>(cartesIA);
        List<CarteBoardInteraction> cartesAAttaque = new List<CarteBoardInteraction>();

        while (attaquesEffectuees < maxAttaques && cartesIADisponibles.Count > 0)
        {
            int meilleureScore = 0;
            CarteBoardInteraction meilleurAttaquant = null;
            CarteBoardInteraction meilleureCible = null;

            // Pour chaque carte IA disponible, on décide l'action
            foreach (var carteIA in cartesIADisponibles)
            {
                var decision = IAAction.DecideAction(carteIA, cartesIA, cardsPlayer);
                Debug.Log($"[IA] Carte {carteIA.name} : attack={decision.attack}, score={decision.score}, target={(decision.target != null ? decision.target.name : "null")}");

                if (decision.attack && decision.score > meilleureScore && decision.score >= seuilMinAttaque)
                {
                    meilleureScore = decision.score;
                    meilleurAttaquant = carteIA;
                    meilleureCible = decision.target;
                }
            }

            if (meilleurAttaquant != null && meilleureCible != null)
            {
                Debug.Log($"[IA] {meilleurAttaquant.name} attaque la cible {meilleureCible} avec un score {meilleureScore}");
                ExecuteAttack(meilleurAttaquant, meilleureCible);

                attaquesEffectuees++;
                cartesAAttaque.Add(meilleurAttaquant);
                // On retire le meilleur attaquant de la liste pour qu'il n'attaque qu'une fois
                cartesIADisponibles.Remove(meilleurAttaquant);

                // On peut aussi retirer la cible si tu veux éviter qu'elle soit attaquée plusieurs fois
                cardsPlayer.Remove(meilleureCible);

                yield return new WaitForSeconds(delaiAction);
            }
            else
            {
                break;
            } 
        }

        foreach (var carteIA in cartesIA)
        {
            if (!cartesAAttaque.Contains(carteIA))
            {
                Debug.Log($"[IA] {carteIA.name} : PASSER");
                ExecutePass(carteIA);
                yield return new WaitForSeconds(delaiAction);
            }
        }

        //Debug.Log("[IA] Tour IA terminé");

        // Appliquer toutes les attaques
        CarteBoardInteraction instance = FindAnyObjectByType<CarteBoardInteraction>();
        if (instance != null)
            instance.ApplyAllAttacks();

        yield return new WaitForSeconds(1f);
        CarteBoardInteraction.EndAITurn();
    }
    
    private List<CarteBoardInteraction> GetCartesAdversaire()
    {
        return CarteBoardInteraction.AllCardsInteractions
            .Where(c => c.isCardAdversaire)
            .ToList();
    }

    private List<CarteBoardInteraction> GetCartesPlayer()
    {
        return CarteBoardInteraction.AllCardsInteractions
            .Where(c => c.isCardPlayer)
            .ToList();
    }
    
    private void ExecuteAttack(CarteBoardInteraction attaquant, CarteBoardInteraction cible)
    {        
        ApplyIAAttackVisualEffect(attaquant);
        SimulateAIAttack(attaquant, cible);
    }
    
    private void SimulateAIAttack(CarteBoardInteraction attaquant, CarteBoardInteraction cible)
    {
        string nameCard = attaquant.GetComponent<CarteUI>()?.nomText?.text ?? "Carte IA";

        GameManager.nombreAttaquesUtiliseesIA++;

        attaquant.choiceDo = true;
        attaquant.stateOffensif = "atk";
        PanelManager.instance.AddLog($"ATTAQUE IA ({GameManager.nombreAttaquesUtiliseesIA}/{GameManager.nombreAttaquesMaximales})");

        ApplyAttack(nameCard, cible);
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
    
        CarteUI carteUI = target.GetComponent<CarteUI>();
        if (carteUI == null) return;
        
        target.targetCount++;
        carteUI.ShowAttackIcon(target.targetCount);

        PanelManager.instance.AddLog($"{nameAttacker} : ATQ -> {carteUI.nomText.text}");
        
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

        Image imageCarte = card.GetComponent<Image>() ?? card.GetComponentInChildren<Image>();
        imageCarte.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        
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
                
        card.startPosition = rectTransform.anchoredPosition;
        
        // Désactiver le LayoutElement pour que la carte ne soit plus affectée par le GridLayout
        LayoutElement layoutElement = card.GetComponent<LayoutElement>();
        //layoutElement.ignoreLayout = true;
        
        // Déplacer la carte vers le bas de 50 pixels
        Vector3 newPosition = card.startPosition + new Vector3(0, -50, 0);
        rectTransform.anchoredPosition = newPosition;
    }
} 
