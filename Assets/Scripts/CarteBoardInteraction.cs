using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class CarteBoardInteraction : MonoBehaviour
{
    public List<IAAction.Capacity> capacites; // la liste des capacités de la carte

    [SerializeField] public bool isCardPlayer = false;
    [SerializeField] public bool isCardOpponent = false;
    [SerializeField] public bool isSelected = false;
    [SerializeField] public int isCibledCount = 0; 
    [SerializeField] public string stateOffensif = "";
    [SerializeField] public string stateDefensif = "";  
    [SerializeField] public string lastTarget = "";
    [SerializeField] public string currentTarget = "";  
    [SerializeField] public int bonusAtk;  
    [SerializeField] public int bonusDfs;  
    [SerializeField] public int malusAtk;  
    [SerializeField] public int malusDfs; 

    [SerializeField] public int freezeNumberLoop = 0;   
    [SerializeField] public bool resetBonusAtk = true;  
    [SerializeField] public string nameCard;  

    public static readonly List<CarteBoardInteraction> AllCardsInteractions = new();

    public bool choiceDo = false;
    public CardUI cardUI;
    private LayoutElement layoutElement;
    public LayoutGroup layoutGroup;
    public GameObject freezeIcon; // Icône "freeze"

    public Vector3 startPosition; 
    public Vector3 newPosition;
    public RectTransform rectTransform;
    //private bool ignorePointer  = false;

    public bool yellowCard = false; 
    private static CarteBoardInteraction attackingCard = null; 
    public static int numberOfAttacksMax = 2;
  
    private Vector3 targetHoverOffset = new Vector3(0, -50, 0);
    public GameObject buttonAtk;
    public GameObject buttonPass;
    private static Color colorAtk1 = new Color(0.8f, 0.8f, 1f, 1f);
    private static List<CarteBoardInteraction> targetCards = new List<CarteBoardInteraction>();
    private struct AttaqueInfo
    {
        public CarteBoardInteraction attacker;
        public CarteBoardInteraction target;
        public int damage;
        public AttaqueInfo(CarteBoardInteraction attacker, CarteBoardInteraction target, int damage)
        {
            this.attacker = attacker;
            this.target = target;
            this.damage = damage;
        }
    }
    private static List<AttaqueInfo> attaquesDuTour = new List<AttaqueInfo>();
  

    public CarteBoardInteraction CurrentTarget;

    public bool WillAttackThisTurn = false;


    
    public void OnPointerClick(PointerEventData eventData)
    {
        //StartCoroutine(cardAnimations.Rotate360());
        //StartCoroutine(cardAnimations.Wobble());
        //StartCoroutine(cardAnimations.Flip());
        //StartCoroutine(cardAnimations.Rotate());
        //StartCoroutine(cardAnimations.PopScale());
        //StartCoroutine(cardAnimations.Bounce(0.5f, 30f));

       // targetImage = cardUI.GetComponentInChildren<Image>();
        //StartCoroutine(cardAnimations.Glow(cardUI));
       // StartCoroutine(cardAnimations.Fade(cardUI, 1f, 0f, 0.5f));

    }


//    private IEnumerator ReenablePointerAfterDelay(float delay)
 //   {
  //      yield return new WaitForSeconds(delay);
   //     ignorePointer = false;
    //}
}

/*
est une carte joueur
est une carte adversaire
est selectionné
nombre de fois ciblée
état offensif
état defensif
derniere cible
cible actuelle
bonus atk
malus atk
bonus defense
malus defense
est freeze
freeze sur le tour numéro
reset bonus atk (?)
nom de la carte
defense max
attaque max
defense courante
attaque courante
a fait son action du tour
est une carte jaune
va attaquer ce tour
*/
/*
Cycle de vie des actions — joueur

Joueur sélectionne une carte → OnPointerClick() → OnAttaque()
Joueur sélectionne une cible → SelectTarget() (ligne 1060)
Incrémente numberOfAttacksUsed
Affiche les icônes d'attaque
Appelle ComputeAndStoreDamage() (ligne 1123)
Calcule les dégâts (ATK de l'attaquant)
Stocke l'attaque dans attaquesDuTour (ligne 989)
N'applique pas encore les dégâts
Vérifie la fin de tour → CheckEndOfTurn() (ligne 1134)
Si toutes les cartes ont fait leur choix → isEndturnPlayer = true
Update() détecte isEndturnPlayer → MarkEndOfTurn() (ligne 135-136)
Affiche le résumé des dégâts
Lance le tour de l'IA si active
N'appelle pas ApplyAllAttacks()
Les attaques restent dans attaquesDuTour et ne sont pas appliquées



Cycle de vie des actions — IA

IA décide les actions → IAAction.DecideAction() (ligne 66)
Évalue chaque carte IA
Choisit attaquer ou passer
IA exécute les attaques → SaveAttack() → SimulateAIAttack() → ApplyAttack() (ligne 161)
Appelle ComputeAndStoreDamageIA() (ligne 198)
Calcule les dégâts
Stocke dans attaquesDuTour (ligne 1007)
N'applique pas encore les dégâts
IA passe les autres cartes → ExecutePass() (ligne 201)
Fin du tour IA → ApplyAllAttacks() (ligne 111)
Parcourt attaquesDuTour
Applique les effets spéciaux (Minoson, Belindra, Zarla, etc.)
Vérifie l'esquive (ligne 815)
Condition : (playerCards.Contains(target) || opponentCards.Contains(target)) && targetName != "Zao"
Cette condition est toujours vraie (sauf pour Zao)
Toutes les attaques sont esquivées → continue (ligne 818)
Les dégâts ne sont jamais appliqués
Vide attaquesDuTour (ligne 844)
Constat
Joueur : les attaques sont stockées dans attaquesDuTour mais ApplyAllAttacks() n'est jamais appelé, donc les dégâts ne sont pas appliqués.
IA : ApplyAllAttacks() est appelé, mais la condition d'esquive (ligne 815) bloque toutes les attaques, donc les dégâts ne sont pas appliqués.
Dans les deux cas, les dégâts ne sont pas appliqués, mais pour des raisons différentes.

*/