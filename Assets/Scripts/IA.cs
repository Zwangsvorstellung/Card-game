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
        
        GameManager.nombreAttaquesUtilisees = 0;
        
        List<CarteBoardInteraction> cartesAdversaires = GetCartesAdversaires();
        
        if (cartesAdversaires.Count == 0)
        {
            Debug.Log("[IA] Aucune carte adverse trouvée");
            yield break;
        }
        
        // Pour chaque carte adverse, faire un choix
        foreach (var carte in cartesAdversaires)
        {
            if (carte == null || !carte.enabled) 
                continue;
            
            yield return new WaitForSeconds(delaiAction);
            
            DecideCardAction(carte);
        }
        
        Debug.Log("[IA] Tour IA terminé");
        
        yield return new WaitForSeconds(1f);
        CarteBoardInteraction.EndAITurn();
    }
    
    private List<CarteBoardInteraction> GetCartesAdversaires()
    {
        return CarteBoardInteraction.AllCardsInteractions
            .Where(c => !c.isCardPlayer && c.enabled)
            .ToList();
    }
    
    private void DecideCardAction(CarteBoardInteraction carte)
    {
        string nomCarte = carte.GetComponent<CarteUI>()?.nomText?.text ?? "Carte IA";
        
        // Logique simple : 70% de chance d'attaquer, 30% de passer
        float random = Random.Range(0f, 1f);
        
        if (random < 0.7f && CanAttack())
        {
            // Attaquer
            Debug.Log($"[IA] {nomCarte} : ATTAQUE");
            ExecuteAttack(carte);
        }
        else
        {
            // Passer
            Debug.Log($"[IA] {nomCarte} : PASSER");
            ExecutePass(carte);
        }
    }
    
    private bool CanAttack()
    {
        return CarteBoardInteraction.GetRemainingAttacks() > 0;
    }
    
    private void ExecuteAttack(CarteBoardInteraction carte)
    {        
        ApplyIAAttackVisualEffect(carte);
        SimulateAIAttack(carte);
    }
    
    private void SimulateAIAttack(CarteBoardInteraction carte)
    {
        string nomCarte = carte.GetComponent<CarteUI>()?.nomText?.text ?? "Carte IA";
        
        // Incrémenter le compteur d'attaques
        GameManager.nombreAttaquesUtilisees++;
        
        // Marquer que la carte a fait son choix
        carte.choiceDo = true;
        
        // Rendre la carte non cliquable
        carte.enabled = false;
        
        PanelManager.instance.AddLog($"{nomCarte} : ATTAQUE IA ({GameManager.nombreAttaquesUtilisees}/{GameManager.nombreAttaquesMaximales})");
        
        SelectRandomTarget(nomCarte, GameManager.nombreAttaquesUtilisees);
    }

    private void SelectRandomTarget(string nomAttaquant, int numberAtk)
    {
        // Récupérer toutes les cartes du joueur
        var cartesJoueur = FindObjectsByType<CarteBoardInteraction>(FindObjectsSortMode.None)
            .Where(c => c.isCardPlayer)
            .ToList();
        if (cartesJoueur.Count > 0)
        {
            // Choisir une cible aléatoire
            var cible = cartesJoueur[Random.Range(0, cartesJoueur.Count)];
            ApplyAttack(nomAttaquant, cible.name, numberAtk);
        }
    }
    
    private void ApplyAttack(string nomAttaquant, string cibleName, int numberAtk){

        Color couleurAttaque = numberAtk == 1 ? new Color(0.8f, 0.8f, 1f, 1f) : new Color(1f, 0.8f, 0.8f, 1f);
        var carteJoueur = FindObjectsByType<CarteUI>(FindObjectsSortMode.None)
            .Where(c => c.name == cibleName);

        foreach (CarteUI carte in carteJoueur)
        {
            carte.ShowAttackIcon(1);
        }
    }

    private void ExecutePass(CarteBoardInteraction carte)
    {
        // Simuler le clic sur la carte pour la sélectionner
        carte.OnPointerClick(null);
        
        // Attendre un peu puis cliquer sur le bouton passer
        StartCoroutine(ClickPassButton(carte));
    }
    
    private IEnumerator ClickPassButton(CarteBoardInteraction carte)
    {
        yield return new WaitForSeconds(0.2f);
        
        // Chercher le bouton passer de cette carte
        var boutonPasser = carte.transform.Find("BoutonPasser");
        if (boutonPasser != null)
        {
            var button = boutonPasser.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }
    }
    
    // Méthode pour démarrer l'IA au début du jeu
    public IEnumerator StartAITurnCoroutine()
    {
        yield return new WaitForSeconds(1f);
        StartAITurn();
    }
    
    private void ApplyIAAttackVisualEffect(CarteBoardInteraction carte)
    {
        // Récupérer le RectTransform de la carte
        RectTransform rectTransform = carte.GetComponent<RectTransform>();
                
        if (!carte.positionInitialeEnregistree)
        {
            carte.positionInitiale = rectTransform.anchoredPosition;
            carte.positionInitialeEnregistree = true;
        }
        
        // Désactiver le LayoutElement pour que la carte ne soit plus affectée par le GridLayout
        LayoutElement layoutElement = carte.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;
        
        // Déplacer la carte vers le bas de 50 pixels
        Vector3 nouvellePosition = carte.positionInitiale + new Vector3(0, -50, 0);
        rectTransform.anchoredPosition = nouvellePosition;
    }
} 