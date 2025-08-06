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
        
        List<CarteBoardInteraction> cartesAdversaires = GetCartesAdversaires();
        
        if (cartesAdversaires.Count == 0)
        {
            Debug.Log("[IA] Aucune carte adverse trouvée");
            yield break;
        }
        
        // Pour chaque carte adverse, faire un choix
        foreach (var carte in cartesAdversaires)
        {
            if (carte == null) 
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
            .Where(c => c.isCardAdversaire)
            .ToList();
    }
    
    private void DecideCardAction(CarteBoardInteraction carte)
    {
        string nomCarte = carte.GetComponent<CarteUI>()?.nomText?.text ?? "Carte IA";
        
        // Logique simple : 70% de chance d'attaquer, 30% de passer
        float random = Random.Range(0f, 1f);
        
        if (random < 0.7f && GameManager.nombreAttaquesUtiliseesIA < 2)
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
    
    private void ExecuteAttack(CarteBoardInteraction carte)
    {        
        ApplyIAAttackVisualEffect(carte);
        SimulateAIAttack(carte);
    }
    
    private void SimulateAIAttack(CarteBoardInteraction carte)
    {
        string nomCarte = carte.GetComponent<CarteUI>()?.nomText?.text ?? "Carte IA";
        
        GameManager.nombreAttaquesUtiliseesIA++;
        
        carte.choiceDo = true;        
        carte.stateOffensif = "atk";
        PanelManager.instance.AddLog($"{nomCarte} : ATTAQUE IA ({GameManager.nombreAttaquesUtiliseesIA}/{GameManager.nombreAttaquesMaximales})");
        
        SelectRandomTarget(nomCarte, GameManager.nombreAttaquesUtiliseesIA);
    }

    private void SelectRandomTarget(string nomAttaquant, int numberAtk)
    {
        var cartesJoueur = CarteBoardInteraction.AllCardsInteractions
            .Where(c => c.isCardPlayer)
            .ToList();

        if (cartesJoueur.Count > 0)
        {
            CarteBoardInteraction cible = cartesJoueur[Random.Range(0, cartesJoueur.Count)];
            ApplyAttack(nomAttaquant, cible);
        }
    }
    
    private void ApplyAttack(string nomAttaquant, CarteBoardInteraction cible)
    {
        CarteBoardInteraction attaquant = CarteBoardInteraction.AllCardsInteractions.FirstOrDefault(c => c.name == nomAttaquant);
        if (cible != null)
        {
            CarteUI carteUI = cible.GetComponent<CarteUI>();
            if (carteUI != null)
            {
                cible.nombreCiblages = cible.nombreCiblages++;
                carteUI.ShowAttackIcon(cible.nombreCiblages);
                PanelManager.instance.AddLog($"{nomAttaquant} : ATTAQUE -> ({cibleName})");
                
                CarteScriptableObject[] cartesAssets = Resources.LoadAll<CarteScriptableObject>("CartesGenerees");

                var so = System.Array.Find(cartesAssets, c => c.nom == nomAttaquant);
                if (so != null && !string.IsNullOrEmpty(so.color))
                {
                    if (cible.nombreCiblages == 1)
                    {
                        carteUI.SetAtk1IconColor(so.color);
                        carteUI.SetAtk1IconTooltip(so.nom, so.atk);
                    }
                    else if (cible.nombreCiblages == 2)
                    {
                        carteUI.SetAtk2IconColor(so.color);
                        carteUI.SetAtk2IconTooltip(so.nom, so.atk);
                    }
                }
            }

            cible.stateDefensif = "isAttacked";
            cible.ComputeAndStoreDamageIA(attaquant, cible, nomAttaquant, cibleName);
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
        carte.choiceDo = true;        
        carte.stateOffensif = "passed";
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
                
        carte.positionInitiale = rectTransform.anchoredPosition;
        
        // Désactiver le LayoutElement pour que la carte ne soit plus affectée par le GridLayout
        LayoutElement layoutElement = carte.GetComponent<LayoutElement>();
        //layoutElement.ignoreLayout = true;
        
        // Déplacer la carte vers le bas de 50 pixels
        Vector3 nouvellePosition = carte.positionInitiale + new Vector3(0, -50, 0);
        rectTransform.anchoredPosition = nouvellePosition;
    }
} 
