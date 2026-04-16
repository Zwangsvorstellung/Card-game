using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using static IAAction;

/// Structure pour stocker les informations d'une attaque (joueur ou IA).
public struct AttackInfo
{
    public CardAI attackerAI;      // Attaquant si c'est l'IA (null si c'est le joueur)
    public CardUI attackerPlayer;  // Attaquant si c'est le joueur (null si c'est l'IA)
    public CardUI targetPlayer;    // Cible si c'est une carte joueur
    public CardAI targetAI;        // Cible si c'est une carte IA
    public int damage;
    public int precision;
    public bool isPlayerAttack;     // true si c'est une attaque du joueur, false si c'est l'IA
    
    public AttackInfo(CardAI attacker, CardUI target, int damage, int precision)
    {
        this.attackerAI = attacker;
        this.attackerPlayer = null;
        this.targetPlayer = target;
        this.targetAI = null;
        this.damage = damage;
        this.precision = precision;
        this.isPlayerAttack = false;
    }
    
    public AttackInfo(CardUI attacker, CardAI target, int damage, int precision)
    {
        this.attackerAI = null;
        this.attackerPlayer = attacker;
        this.targetPlayer = null;
        this.targetAI = target;
        this.damage = damage;
        this.precision = precision;
        this.isPlayerAttack = true;
    }
}

/// Gère le comportement de l'IA pour les tours de l'adversaire.
/// Utilise IAAction pour évaluer les meilleures actions et les exécute.
public class IA : MonoBehaviour
{
    private static IA instance;
    public static IA Instance => instance;
    private Coroutine aiTurnCoroutine;
    
    [Header("Paramètres")]
    [SerializeField] private float delayAction = 0.4f; // Délai entre chaque action de l'IA
    
    // Liste des attaques de l'IA pour ce tour (sera appliquée à la fin du tour)
    private static List<AttackInfo> aiAttacksThisTurn = new List<AttackInfo>();
    
    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
    }
    
    /// Démarre le tour de l'IA
    public void StartAITurn()
    {
        Debug.Log($"[IA] ===== StartAITurn() =====");

        if (aiTurnCoroutine != null)
        {
            StopCoroutine(aiTurnCoroutine);
            aiTurnCoroutine = null;
        }

        aiTurnCoroutine = StartCoroutine(ExecuteAITurn());
    }
    
    /// Exécute le tour de l'IA : évalue les actions possibles et choisit les meilleures.
    /// Utilise un système de scoring pour décider entre attaquer et rester passif.
    private IEnumerator ExecuteAITurn()
    {
        Debug.Log($"[IA] ===== DÉBUT DU TOUR IA ExecuteAITurn =====");
        int aiCardsCount = BoardManager.cardsOnBoardAI.Count(c => c != null && !c.isHiddenSlot);
        
        if (aiCardsCount == 0)
        {
            Debug.Log("[IA] ⚠️ Aucune carte IA trouvée - Arrêt du tour");
            yield break;
        }

        // Seuil minimal de score pour qu'une attaque soit envisagée
        const int seuilMinAttaque = 1;
        int attacksExecuted = 0;

        // Création de copies des listes pour pouvoir les modifier sans affecter les originaux
        List<CardAI> cardsAIOnBoard = BoardManager.cardsOnBoardAI.Where(c => c != null && !c.isHiddenSlot).ToList();
        List<CardUI> cardsUIOnBoard = BoardManager.cardsOnBoardUI.Where(c => c != null && !c.isHiddenSlot).ToList();
        
        // Boucle principale : on continue tant qu'on n'a pas atteint le maximum d'attaques
        // et qu'il reste des cartes IA disponibles
        Debug.Log($"[IA] Boucle d'exécution - Max attaques: {GameManager.MAX_NUMBER_ATK_ROUND}, Cartes disponibles: {cardsAIOnBoard.Count}");
        
        while (attacksExecuted < GameManager.MAX_NUMBER_ATK_ROUND && cardsAIOnBoard.Count > 0)
        {
            Debug.Log($"[IA] --- Itération {attacksExecuted + 1}/{GameManager.MAX_NUMBER_ATK_ROUND} ---");
            
            int bestScoring = 0;
            CardAI bestAttacker = null;
            CardUI bestTarget = null;

            // Pour chaque carte IA disponible, on évalue la meilleure action
            Debug.Log($"[IA] Évaluation de {cardsAIOnBoard.Count} cartes IA...");
            foreach (var cardAI in cardsAIOnBoard)
            {
                if (cardAI.isFrozen) continue;

                // Décision : attaquer ou rester passif ?
                var decision = IAAction.DecideAction(cardAI, cardsAIOnBoard, cardsUIOnBoard);
                
                Debug.Log($"[IA]   {cardAI.nameCard} (ATK:{cardAI.attaqueValue}, DEF:{cardAI.defenseValue}) → " +
                         $"attack={decision.attack}, score={decision.score}, " +
                         $"target={(decision.target != null ? decision.target.nameCard : "null")}");

                // Si la décision est d'attaquer et que le score est suffisant
                if (decision.attack && decision.score > bestScoring && decision.score >= seuilMinAttaque)
                {
                    bestScoring = decision.score;
                    bestAttacker = cardAI;
                    bestTarget = decision.target;
                }
            }

            // Si on a trouvé une bonne attaque, on l'exécute
            if (bestAttacker != null && bestTarget != null)
            {
                Debug.Log($"[IA] ✓ Meilleure action trouvée: {bestAttacker.nameCard} → {bestTarget.nameCard} (score: {bestScoring})");
                
                // Exécute l'attaque
                ExecuteAttack(bestAttacker, bestTarget);

                attacksExecuted++;
                Debug.Log($"[IA] Attaques exécutées: {attacksExecuted}/{GameManager.MAX_NUMBER_ATK_ROUND}");
                
                // Retire l'attaquant de la liste pour qu'il n'attaque qu'une fois
                cardsAIOnBoard.Remove(bestAttacker);
                
                // Retire la cible de la liste si elle est éliminée (optionnel)
                 cardsUIOnBoard.Remove(bestTarget);

                yield return new WaitForSeconds(delayAction);
            }
            else
            {
                Debug.Log($"[IA] Aucune attaque intéressante trouvée (score max: {bestScoring}, seuil: {seuilMinAttaque})");
                break;
            }
        }

        // Les cartes restantes qui n'ont pas attaqué passent leur tour
        Debug.Log($"[IA] Cartes restantes à passer: {cardsAIOnBoard.Count(c => !c.actionChoiceDo)}");
        foreach (var cardAI in cardsAIOnBoard)
        {
            if (!cardAI.actionChoiceDo)
            {
                ExecutePass(cardAI);
                yield return new WaitForSeconds(delayAction);
            }
        }

        // Attente finale avant de terminer le tour
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"[IA] Attaques stockées: {aiAttacksThisTurn.Count}");
        foreach (var attack in aiAttacksThisTurn)
        {
            if (attack.attackerAI != null)
                Debug.Log($"[IA]   - {attack.attackerAI.nameCard} → {attack.targetPlayer.nameCard} ({attack.damage} dégâts)");
        }

        GameManager.Instance.isEndturnAI = true;
        Debug.Log($"[BOARD] ===== FIN DU TOUR IA =====");
        int visibleAICards = BoardManager.cardsOnBoardAI.Count(c => c != null && !c.isHiddenSlot);
        Debug.Log($"[BOARD] Toutes les cartes IA ont fait leur choix ({visibleAICards} cartes)");
        int attacksCount = BoardManager.cardsOnBoardAI.Count(c => c != null && !c.isHiddenSlot && c.stateOffensif == "atk");
        int passesCount = BoardManager.cardsOnBoardAI.Count(c => c != null && !c.isHiddenSlot && c.stateOffensif == "passed");
        Debug.Log($"[BOARD] Résumé - Attaques: {attacksCount}, Passes: {passesCount}");

        // Si l'IA a commencé le tour, on rend ensuite la main au joueur.
        if (!GameManager.Instance.isEndturnPlayer)
        {
            GameManager.Instance.currentPlayerAction = "UI";
            PanelManager.Instance?.ShowTurnBanner("UI");
            Debug.Log("[GAME] Transition de tour: IA -> JOUEUR");
        }

        aiTurnCoroutine = null;
    }

    /// Applique toutes les attaques (joueur + IA) de manière séquentielle
    public IEnumerator ApplyAllAttacksCoroutine()
    {
        Debug.Log($"[ATTACK] ===== APPLICATION DES ATTAQUES =====");
        
        // 1. Récupération des attaques du joueur
        List<AttackInfo> playerAttacks = GetPlayerAttacks();
        
        // 2. Préparation de la liste globale
        List<AttackInfo> allAttacks = new List<AttackInfo>();
        bool aiStart = GameManager.Instance.aiStart;

        if (aiStart)
        {
            allAttacks.AddRange(aiAttacksThisTurn);
            allAttacks.AddRange(playerAttacks);
        }
        else
        {
            allAttacks.AddRange(playerAttacks);
            allAttacks.AddRange(aiAttacksThisTurn);
        }

        // 3. Application séquentielle avec pause pour laisser l'Update() afficher le jaune
        foreach (var attack in allAttacks)
        {
            ApplySingleAttack(attack);
            yield return new WaitForSeconds(0.8f);
        }

        // Petite pause supplémentaire avant de tout réinitialiser
        yield return new WaitForSeconds(0.5f);
        
        aiAttacksThisTurn.Clear();
        
        // Fin du round
        GameManager.Instance.initRound();
        GameManager.Instance.EndTurn();
    }

    // début du tour
    public void ApplyBonusAI(List<CardAI> cards)
    {
        int newDefense;

        foreach (var card in cards)
        {
            Debug.Log($"[AI] Bonus appliqué à {card.nameCard}");

            if(card.nameCard == "Clorel"){
                newDefense = card.defenseValue+1;
                card.defenseValue = newDefense;
                card.defenseText.SetText(newDefense.ToString());
            }

        }
    }

    public void ApplyBonusUI(List<CardUI> cards)
    {
        int newDefense;

        foreach (var card in cards)
        {
            Debug.Log($"[AI] Bonus appliqué à {card.nameCard}");

            if(card.nameCard == "Clorel"){
                newDefense = card.defenseValue+1;
                card.defenseValue = newDefense;
                card.defenseText.SetText(newDefense.ToString());
            }

            // Neo + 1 ATK si cible différente
            if(card.nameCard == "Neo"){}

            // freeze une carte adverse au hasard (prendre entre 1 et 4)
            if(card.nameCard == "Désir"){}

        }
    }

    /// Applique les bonus
    public IEnumerator ApplyAllBonus()
    {
        Debug.Log($"[ATTACK] ===== APPLICATION DES BONUS =====");
        
        List<CardAI> cardsAIOnBoard = BoardManager.cardsOnBoardAI.Where(c => c != null && !c.isHiddenSlot && c.stateOffensif == "passed").ToList();
        List<CardUI> cardsUIOnBoard = BoardManager.cardsOnBoardUI.Where(c => c != null && !c.isHiddenSlot && c.stateOffensif == "passed").ToList();
        
        bool aiStart = GameManager.Instance.aiStart;

        if (aiStart)
        {
            ApplyBonusAI(cardsAIOnBoard);
            ApplyBonusUI(cardsUIOnBoard);
        }
        else
        {
            ApplyBonusUI(cardsUIOnBoard);
            ApplyBonusAI(cardsAIOnBoard);
        }

        // Petite pause supplémentaire avant de tout réinitialiser
        yield return new WaitForSeconds(0.5f);
    }

    /// Applique les malus à la fin du tour
    public IEnumerator ApplyAllMalusEndTurn()
    {
        Debug.Log($"[ATTACK] ===== APPLICATION DES MALUS A LA FIN DU TOUR =====");
        
        List<CardAI> cardsAIOnBoard = BoardManager.cardsOnBoardAI.Where(c => c != null && !c.isHiddenSlot && c.stateOffensif == "passed").ToList();
        List<CardUI> cardsUIOnBoard = BoardManager.cardsOnBoardUI.Where(c => c != null && !c.isHiddenSlot && c.stateOffensif == "passed").ToList();
        bool aiStart = GameManager.Instance.aiStart;

        if (aiStart)
        {
        }
        else
        {
        }

        // Petite pause supplémentaire avant de tout réinitialiser
        yield return new WaitForSeconds(0.5f);
    }

    public IEnumerator ApplyAllBonusEndTurn()
    {
        Debug.Log($"[ATTACK] ===== APPLICATION DES BONUS A LA FIN DU TOUR =====");
        
        List<CardAI> cardsAIOnBoard = BoardManager.cardsOnBoardAI.Where(c => c != null && !c.isHiddenSlot && c.stateOffensif == "passed").ToList();
        List<CardUI> cardsUIOnBoard = BoardManager.cardsOnBoardUI.Where(c => c != null && !c.isHiddenSlot && c.stateOffensif == "passed").ToList();
        bool aiStart = GameManager.Instance.aiStart;

        if (aiStart)
        {
        }
        else
        {
        }

        // Petite pause supplémentaire avant de tout réinitialiser
        yield return new WaitForSeconds(0.5f);
    }
    

    /// Exécute une attaque de l'IA : met à jour les états et applique les effets visuels.
    /// <param name="attacker">La carte IA qui attaque (CardAI)</param>
    /// <param name="target">La carte joueur ciblée (CardUI)</param>
    private void ExecuteAttack(CardAI attacker, CardUI target)
    {
        if (attacker == null || target == null) return;

        // Applique l'effet visuel de l'attaque
        ApplyIAAttackVisualEffect(attacker);
        
        // Simule l'attaque : met à jour les compteurs et les états
        SimulateAIAttack(attacker, target);

        // Applique l'attaque : met à jour l'UI et calcule les dégâts
        ApplyAttack(attacker, target);
    }
    
    /// Simule une attaque de l'IA : met à jour les états et enregistre l'attaque.
    /// <param name="attacker">La carte IA qui attaque</param>
    /// <param name="target">La carte joueur ciblée</param>
    private void SimulateAIAttack(CardAI attacker, CardUI target)
    {
        GameManager.Instance.numberOfAttacksUsedIA++;

        // Met à jour l'état de l'attaquant
        attacker.actionChoiceDo = true;
        attacker.stateOffensif = "atk";
        
        // Met à jour la cible de l'attaquant (pour le suivi)
        attacker.target = target.nameCard;
        attacker.targetID = target.idCard;
        
        // Met à jour la dernière cible (pour Attaque Surprise)
        attacker.lastTarget = target.nameCard;
        attacker.lastTargetID = target.idCard;
    }
    
    /// Applique une attaque : met à jour l'interface utilisateur et calcule les dégâts.
    /// <param name="attacker">La carte IA qui attaque</param>
    /// <param name="target">La carte joueur ciblée</param>
    private void ApplyAttack(CardAI attacker, CardUI target)
    {
        if (attacker == null || target == null || attacker.isHiddenSlot || target.isHiddenSlot) return;
        
        // Compte le nombre d'attaques déjà reçues par la cible
        // (en comptant les icônes d'attaque actives)
        int attackCount = 0;
        if (target.atk1Icon != null && target.atk1Icon.activeSelf) attackCount++;
        if (target.atk2Icon != null && target.atk2Icon.activeSelf) attackCount++;
        attackCount++; // Ajoute la nouvelle attaque
        
        // Affiche les icônes d'attaque appropriées
        if (target.atk1Icon != null)
        {
            target.atk1Icon.SetActive(true);
        }
        if (attackCount >= 2 && target.atk2Icon != null)
        {
            target.atk2Icon.SetActive(true);
        }
        
        // Met à jour l'état défensif de la cible
        target.stateDefensif = "isAttacked";
        
        // Calcule les dégâts potentiels
        int damage = attacker.attaqueValue - target.defenseValue;
        if (damage < 0) damage = 1; // Minimum 1 dégât
        
        // Enregistre l'attaque dans les logs du tour
        string attackLog = $"{attacker.nameCard} → {target.nameCard} (ATK:{attacker.attaqueValue} vs DEF:{target.defenseValue}) = {damage} dégâts";
        BoardManager.Instance.roundDamage.Add(attackLog);
        
        Debug.Log($"[IA] ✓ Attaque stockée: {attacker.nameCard} → {target.nameCard} ({damage} dégâts)");
        Debug.Log($"[IA]   Attaques IA stockées: {aiAttacksThisTurn.Count + 1}");
        
        // Stocke l'attaque pour l'appliquer à la fin du tour (avec les attaques du joueur)
        int precision = 100;
        aiAttacksThisTurn.Add(new AttackInfo(attacker, target, damage, precision));
    }

    /// Fait passer le tour d'une carte IA
    private void ExecutePass(CardAI card)
    {
        if (card == null || card.isHiddenSlot) return;
        
        // Met à jour l'état de la carte
        card.actionChoiceDo = true;
        card.stateOffensif = "passed";
        
        // Effet visuel optionnel : assombrir la carte
        card.imageCarte.color = new Color(0.4f, 0.4f, 0.4f, 1f);

        // Sauvegarde la position de départ
        Vector3 startPosition = card.rectTransform.anchoredPosition;
        
        // Déplace la carte vers le bas pour l'effet visuel
        Vector3 newPosition = startPosition + new Vector3(0, +30, 0);
        card.rectTransform.anchoredPosition = newPosition;
        
        Debug.Log($"[IA] ✓ {card.nameCard} passe son tour (ATK:{card.attaqueValue}, DEF:{card.defenseValue})");
    }
    
    /// Coroutine pour démarrer le tour de l'IA après un délai.
    /// Utile pour attendre la fin d'une animation ou d'un effet.
    public IEnumerator StartAITurnCoroutine()
    {
        yield return new WaitForSeconds(2f);
        StartAITurn();
    }
    
    /// Applique un effet visuel lors d'une attaque de l'IA.
    /// Déplace légèrement la carte vers le bas pour indiquer l'attaque.
    private void ApplyIAAttackVisualEffect(CardAI card)
    {
        if (card == null || card.rectTransform == null) return;
        
        // Sauvegarde la position de départ
        Vector3 startPosition = card.rectTransform.anchoredPosition;
        
        // Déplace la carte vers le bas pour l'effet visuel
        Vector3 newPosition = startPosition + new Vector3(0, -50, 0);
        card.rectTransform.anchoredPosition = newPosition;

        Transform atkTransform = card.rectTransform.transform.Find("Atk");
        if (atkTransform != null)
        {
            atkTransform.gameObject.SetActive(true);
        }

        // TODO: Optionnellement, ajouter une animation de retour à la position initiale
    }
    
    /// Applique une seule attaque et met à jour les dégâts de la cible.
    private static void ApplySingleAttack(AttackInfo attack)
    {
        string attackerName;
        string targetName;
        int newDefense;
        int precision = attack.precision;
        
        if (attack.isPlayerAttack)
        {
            // Attaque du joueur vers l'IA
            if (attack.targetAI == null || attack.attackerPlayer == null) return;
            
            attackerName = attack.attackerPlayer.nameCard;
            targetName = attack.targetAI.nameCard;
            
            // Applique les dégâts à la cible IA
            newDefense = attack.targetAI.defenseValue - attack.damage;
            if (newDefense < 0) newDefense = 0;
            
            // Met à jour la défense de la cible IA
            attack.targetAI.defenseValue = newDefense;
            attack.targetAI.defenseText.SetText(newDefense.ToString());
            

            Debug.Log($"[ATTACK] → {attackerName} inflige {attack.damage} dégâts à {targetName}");
            Debug.Log($"[ATTACK]   DEF avant: {(attack.targetAI.defenseValue + attack.damage)}, " +
                    $"DEF après: {newDefense}");
            
            if (newDefense <= 0)
            {
                attack.targetAI.isYellow = true;
                Debug.Log($"[ATTACK] ⚠️ {targetName} est ÉLIMINÉE ! (DEF: 0)");
            }
        }
        else
        {
            // Attaque de l'IA vers le joueur
            if (attack.targetPlayer == null || attack.attackerAI == null) return;
            
            attackerName = attack.attackerAI.nameCard;
            targetName = attack.targetPlayer.nameCard;
            
            // Applique les dégâts à la cible joueur
            newDefense = attack.targetPlayer.defenseValue - attack.damage;
            if (newDefense < 0) newDefense = 0;
            
            // Met à jour la défense de la cible joueur
            attack.targetPlayer.defenseValue = newDefense;
            if (attack.targetPlayer.defenseText != null)
            {
                attack.targetPlayer.defenseText.SetText(newDefense.ToString());
            }

            Debug.Log($"[ATTACK] → {attackerName} inflige {attack.damage} dégâts à {targetName}");
            Debug.Log($"[ATTACK]   DEF avant: {(attack.targetPlayer.defenseValue + attack.damage)}, " +
                    $"DEF après: {newDefense}");
            
            if (newDefense <= 0)
            {
                attack.targetPlayer.isYellow = true;
                Debug.Log($"[ATTACK] ⚠️ {targetName} est ÉLIMINÉE ! (DEF: 0)");
            }
        }
        
    }
    
    /// Récupère les attaques du joueur depuis le système existant.
    private static List<AttackInfo> GetPlayerAttacks()
    {
        List<AttackInfo> playerAttacks = new List<AttackInfo>();
        int precision = 100;
        
        int playerCardsCount = BoardManager.cardsOnBoardUI.Count(c => c != null && !c.isHiddenSlot);
        Debug.Log($"[ATTACK] Récupération des attaques du joueur depuis {playerCardsCount} cartes...");
        
        // Parcourt les cartes du joueur qui ont attaqué
        foreach (var cardUI in BoardManager.cardsOnBoardUI.Where(c => c != null && !c.isHiddenSlot))
        {
            if (cardUI.stateOffensif == "atk" && !string.IsNullOrEmpty(cardUI.target))
            {
                // Trouve la cible IA correspondante
                CardAI target = BoardManager.cardsOnBoardAI.FirstOrDefault(c => c != null && !c.isHiddenSlot && c.idCard == cardUI.targetID);
                if (target == null)
                    target = BoardManager.cardsOnBoardAI.FirstOrDefault(c => c != null && !c.isHiddenSlot && c.nameCard == cardUI.target);
                if (target != null)
                {
                    // Calcule les dégâts
                    //int damage = cardUI.attaqueValue - target.defenseValue;
                   // if (damage < 0) damage = 1;

                   int damage = cardUI.attaqueValue;
                    
                    Debug.Log($"[ATTACK] Attaque joueur trouvée: {cardUI.nameCard} → {target.nameCard} " +
                             $"(ATK:{cardUI.attaqueValue} vs DEF:{target.defenseValue} = {damage} dégâts)");
                    
                    // Ajoute l'attaque du joueur
                    playerAttacks.Add(new AttackInfo(cardUI, target, damage, precision));
                }
                else
                {
                    Debug.LogWarning($"[ATTACK] ⚠️ Cible '{cardUI.target}' non trouvée pour {cardUI.nameCard}");
                }
            }
        }
        
        return playerAttacks;
    }
} 
