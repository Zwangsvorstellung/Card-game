using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class BoutonValider : MonoBehaviour
{
    public string texteBouton = "Valider";
    private Button bouton;
    
    void Start()
    {        
        bouton = GetComponent<Button>();
        bouton.GetComponentInChildren<TMP_Text>().text = texteBouton;
        bouton.onClick.AddListener(OnBoutonClic);
    }
    
    public void OnBoutonClic()
    {   
        GameObject.Find("TexteConsigne")?.SetActive(false);

        List<CarteData> cartesSelectionnees = GameManager.Instance.GetSelectedCards(); // du joueur
        var cartesSelectionneesIds = cartesSelectionnees.Select(c => c.idCard).ToHashSet();
        var cartesNonSelectionnees = GameManager.Instance.mainPlayerA.Where(c => !cartesSelectionneesIds.Contains(c.idCard)).ToList();
        
        // Mettre à jour la main du joueur avec les 4 cartes sélectionnées
        GameManager.Instance.mainPlayerA = new Queue<CarteData>(cartesSelectionnees);
        // Ajouter les cartes non sélectionnées à la pioche existante
        foreach (var carte in cartesNonSelectionnees)
        {
            GameManager.Instance.piochePlayerA.Enqueue(carte);
        }
        // Générer 4 cartes aléatoires pour l'adversaire
        List<CarteData> cartesAdversaire = GenerateOpponentCards();

        // Masquer toutes les cartes UI
        //MasquerToutesLesCartes();

        // Afficher les cartes sur la table via BoardManager     
        BoardManager.Instance.ShowCardsOnTable(cartesAdversaire, cartesSelectionnees);

        CamController.Instance.GoToBoardView();
        gameObject.SetActive(false);

        GameManager.mode = "select";

        // Masquer le panel de la main de départ (MainUIManager)
        MainUIManager mainUIManager = FindFirstObjectByType<MainUIManager>();
        mainUIManager.gameObject.SetActive(false);
    }
    
    private List<CarteData> GenerateOpponentCards()
    {
        return GameManager.Instance?.mainPlayerB.Take(4).ToList() ?? new List<CarteData>();
    }
    
    private void MasquerToutesLesCartes() => BoardManager.Instance?.GetCartesJoueur()?.ForEach(c => c.gameObject.SetActive(false));
} 