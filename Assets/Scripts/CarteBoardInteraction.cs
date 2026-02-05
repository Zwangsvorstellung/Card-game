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
    private static Color colorAtk2 = new Color(1f, 0.8f, 0.8f, 1f);
    private static List<CarteBoardInteraction> coloredCards = new List<CarteBoardInteraction>();
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
    
    
    // BoardManager
    public void ApplyAllAttacks()
    {
        /*
        int indexBelindraOpponent = -1;
        int indexBelindraPlayer = -1;
        CarteBoardInteraction belindraOpponent = null;
        CarteBoardInteraction belindraPlayer = null;
        CarteBoardInteraction zarlaCard = null;

        // Préparer dictionnaires par camp
        var playerCards = AllCardsInteractions.Where(c => c.isCardPlayer).ToList();
        var opponentCards = AllCardsInteractions.Where(c => c.isCardOpponent).ToList();

        // Identifier Zarla et Belindra
        foreach (var card in AllCardsInteractions)
        {
            switch (card.nameCard)
            {
                case "Zarla":
                    zarlaCard ??= card;
                    break;
                case "Belindra" when card.stateOffensif == "passed":
                    var index = card.GetComponent<CardUI>().indexHierarchieOriginal;
                    if (card.isCardPlayer)
                    {
                        belindraPlayer = card;
                        indexBelindraPlayer = index;
                    }
                    else
                    {
                        belindraOpponent = card;
                        indexBelindraOpponent = index;
                    }
                    break;
            }
        }

        foreach (var attaque in attaquesDuTour)
        {
            if (attaque.target == null) continue;

            var target = attaque.target;
            var attacker = attaque.attacker;
            var attackerName = attacker?.nameCard ?? "NULL";
            var targetName = target.nameCard ?? "NULL";

            // --- Minoson ---
            var minoson = (target.isCardPlayer ? playerCards : opponentCards)
                .FirstOrDefault(c => c.nameCard == "Minoson");
            if (minoson != null && targetName != "Minoson" && UnityEngine.Random.value < 0.5f)
            {
                target = minoson;
                targetName = minoson.nameCard;
                Debug.Log($"[ApplyAllAttacks] {minoson.nameCard} intercepte l'attaque destinée à {attaque.target.nameCard}.");
            }

            // --- Belindra ---
            CarteBoardInteraction leftCard = null, rightCard = null;
            if ((belindraOpponent != null && target.isCardOpponent) || (belindraPlayer != null && target.isCardPlayer))
            {
                var indexBelindra = target.isCardPlayer ? indexBelindraPlayer : indexBelindraOpponent;
                (leftCard, rightCard) = BoardManager.Instance.GetAdjacentCards(indexBelindra, target.isCardPlayer ? "player" : "opponent");

                if (leftCard != null) ApplyAttackMalus(leftCard, leftCard.nameCard);
                if (rightCard != null) ApplyAttackMalus(rightCard, rightCard.nameCard);
                //PanelManager.instance.AddLog("Présence de Belindra");
            }

            // --- Zarla ---
            if (zarlaCard != null && target.nameCard == "Zarla") ApplyAttackBonus(target, "Zarla");

            // --- Jaycota ---
            if (targetName == "Jaycota") 
            { 
                target.malusDfs++; 
                Debug.Log($"[ApplyAllAttacks] {targetName} malus défense appliqué."); 
            }

            // --- Tyroine ---
            if (attackerName == "Tyroine")
            {
                target.malusDfs++;
                int currentDef = target.GetDefenseValue(target);
                int newDef = Mathf.Max(0, currentDef - 1);
                target.cardUI?.defenseText?.SetText(newDef.ToString());
                target.SetDefenseValue(newDef);
                Debug.Log($"[ApplyAllAttacks] {targetName} défense réduite par Tyroine (-1 DF).");
            }

            // --- Neo ---
            if (attackerName == "Neo" && targetName != attacker.lastTarget && !string.IsNullOrEmpty(attacker.lastTarget))
            {
                ApplyAttackBonus(attacker, targetName);
                attacker.resetBonusAtk = false;
                Debug.Log($"[ApplyAllAttacks] {targetName} nouvelle cible bonus attaque pour {attackerName}.");
            }

            // --- Hiver ---
            //if (attackerName == "Hiver" && freezeIcon != null && !freezeIcon.activeSelf)
            if (attackerName == "Hiver")
            { 
                target.isFreeze = true; 
                //PanelManager.instance?.AddLog($"{target.nameCard} est gelée et ne pourra pas attaquer au tour prochain");
                Debug.Log($"[ApplyAllAttacks] {target.nameCard} is frozen by Hiver."); 
                target.freezeNumberLoop = GameManager.currentRound;
                Debug.Log($"[currentRound] {target.freezeNumberLoop}"); 
            }

            // --- Anaxagore ---
            if (attackerName == "Anaxagore") 
            { 
                target.malusDfs++;
                int currentDef = target.GetDefenseValue(target);
                int newDef = Mathf.Max(0, currentDef - 1);
                target.cardUI?.defenseText?.SetText(newDef.ToString());
                target.SetDefenseValue(newDef);
                Debug.Log($"[ApplyAllAttacks] {targetName} défense réduite par Anaxagore (-1 DF)."); 
            }

            // --- Ambroise (effet différé) ---
            if (GameManager.ambroiseEffectPending)
            {
                var passedOpponents = AllCardsInteractions
                    .Where(c => c.isCardOpponent && !attaquesDuTour.Any(a => a.attacker == c))
                    .ToList();
                    
                if(passedOpponents.Count > 0)
                {
                    var randomTarget = passedOpponents[Random.Range(0, passedOpponents.Count)];
                    randomTarget.malusDfs++;
                    int currentDef = randomTarget.GetDefenseValue(randomTarget);
                    int newDef = Mathf.Max(0, currentDef - 1);
                    randomTarget.cardUI?.defenseText?.SetText(newDef.ToString());
                    randomTarget.SetDefenseValue(newDef);
                    
                    randomTarget.UpdateMalusDefenseColor(randomTarget);
                    //PanelManager.instance?.AddLog($"   → Onde de Choc Passive d'Ambroise : -1 DF à {randomTarget.nameCard}");
                }
                
                GameManager.ambroiseEffectPending = false;
            }

            // --- Trahison (effet différé) ---
            if (GameManager.trahisonEffectPending)
            {
                var passedOpponents = AllCardsInteractions
                    .Where(c => c.isCardOpponent && !attaquesDuTour.Any(a => a.attacker == c))
                    .ToList();
                    
                if(passedOpponents.Count > 0)
                {
                    foreach (var passiveOpponent in passedOpponents)
                    {
                        passiveOpponent.malusDfs++;
                        int currentDef = passiveOpponent.GetDefenseValue(passiveOpponent);
                        int newDef = Mathf.Max(0, currentDef - 1);
                        passiveOpponent.cardUI?.defenseText?.SetText(newDef.ToString());
                        passiveOpponent.SetDefenseValue(newDef);
                        
                        passiveOpponent.UpdateMalusDefenseColor(passiveOpponent);
                        //PanelManager.instance?.AddLog($"   → Terreur Sélective de Trahison : -1 DF à {passiveOpponent.nameCard}");
                    }
                    
                }
                
                GameManager.trahisonEffectPending = false;
            }

            // --- Vilaine ---
            if (attackerName == "Vilaine") 
            { 
                // Malus d'attaque : inflige -1 ATK à sa cible sur le tour courant
                target.malusAtk++;
                int currentAtkValue = target.GetAttackValue(target);
                int newAtkValue = Mathf.Max(0, currentAtkValue - 1);
                target.SetAttaqueValue(newAtkValue);
                
                target.UpdateMalusAtqColor(target);
            }

            if (targetName == "Solicia") 
            { 
                // Réflexion partielle : inflige 1 dégât à l'attaquant
                attacker.ApplyDamageToTarget(1, targetName);
            }

            // --- Zao : intouchable si a passé son tour ---
            if (targetName == "Zao" && target.stateOffensif == "passed")
            {
                Debug.Log($"[ApplyAllAttacks] {target.nameCard} esquive l'attaque de {attackerName} (Zao - mode passé).");
                continue;
            }

            // --- Esquive : une carte en mode attaque esquive les attaques --- (sauf ZAO)
            if (target.stateOffensif == "atk" && targetName != "Zao")
            {
                Debug.Log($"[ApplyAllAttacks] {target.nameCard} esquive l'attaque de {attackerName} (mode attaque).");
                continue;
            }

            // --- Ruby : inflige 1 dégât aux ennemis adjacents si elle inflige des dégâts ---
            if (attackerName == "Ruby" && attaque.damage > 0)
            {
                int targetIndex = target.GetComponent<CardUI>().indexHierarchieOriginal;

                (leftCard, rightCard) = BoardManager.Instance.GetAdjacentCards(
                    targetIndex, 
                    target.isCardPlayer ? "player" : "opponent"
                );

                // Crée une liste des cartes adjacentes disponibles
                var adjacentEnemies = new List<CarteBoardInteraction>(2);
                if (leftCard != null) adjacentEnemies.Add(leftCard);
                if (rightCard != null) adjacentEnemies.Add(rightCard);

                if (adjacentEnemies.Count > 0)
                {
                    var chosenTarget = adjacentEnemies[UnityEngine.Random.Range(0, adjacentEnemies.Count)];
                    chosenTarget.ApplyDamageToTarget(1, attackerName);
                }
            }

            // --- Dégâts ---
            target.ApplyDamageToTarget(attaque.damage, attackerName);
        }

        */
    }

    public void ResetPosition()
    {
        rectTransform.anchoredPosition = startPosition;
    }
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

Début du tour IA → StartAITurn() → ExecuteAITurn() (ligne 28)
IA décide les actions → IAAction.DecideAction() (ligne 66)
Évalue chaque carte IA
Choisit attaquer ou passer
IA exécute les attaques → ExecuteAttack() → SimulateAIAttack() → ApplyAttack() (ligne 161)
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