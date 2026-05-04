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
    public string attackerStateOffensif;
    public string attackerStateDefensif;
    public string targetStateOffensif;
    public string targetStateDefensif;
    public bool hasSoliciaOpponent;
    public bool hasMinosonOpponent;
    public bool hasBelindraOpponent;
    
    public AttackInfo(CardAI attacker, CardUI target, int damage, int precision, bool hasSoliciaOpponent, bool hasMinosonOpponent, bool hasBelindraOpponent)
    {
        this.attackerAI = attacker;
        this.attackerPlayer = null;
        this.targetPlayer = target;
        this.targetAI = null;
        this.damage = damage;
        this.precision = precision;
        this.isPlayerAttack = false;
        this.attackerStateOffensif = attacker.stateOffensif;
        this.attackerStateDefensif = attacker.stateDefensif;
        this.targetStateOffensif = target.stateOffensif;
        this.targetStateDefensif = target.stateDefensif;
        this.hasSoliciaOpponent = hasSoliciaOpponent;
        this.hasMinosonOpponent = hasMinosonOpponent;
        this.hasBelindraOpponent = hasBelindraOpponent;
    }
    
    public AttackInfo(CardUI attacker, CardAI target, int damage, int precision, bool hasSoliciaOpponent, bool hasMinosonOpponent, bool hasBelindraOpponent)
    {
        this.attackerAI = null;
        this.attackerPlayer = attacker;
        this.targetPlayer = null;
        this.targetAI = target;
        this.damage = damage;
        this.precision = precision;
        this.isPlayerAttack = true;
        this.attackerStateOffensif = attacker.stateOffensif;
        this.attackerStateDefensif = attacker.stateDefensif;
        this.targetStateOffensif = target.stateOffensif;
        this.targetStateDefensif = target.stateDefensif;
        this.hasSoliciaOpponent = hasSoliciaOpponent;
        this.hasMinosonOpponent = hasMinosonOpponent;
        this.hasBelindraOpponent = hasBelindraOpponent;
    }
}

/// Gère le comportement de l'IA pour les tours de l'adversaire. - Utilise IAAction pour évaluer les meilleures actions et les exécute.
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
    
    public void StartAITurn()
    {
        //Debug.Log($"[IA] ===== StartAITurn() =====");

        if (aiTurnCoroutine != null)
        {
            StopCoroutine(aiTurnCoroutine);
            aiTurnCoroutine = null;
        }
        aiTurnCoroutine = StartCoroutine(ExecuteAITurn());
    }
    
    /// évalue les actions possibles et choisit les meilleures. système de scoring pour décider entre attaquer et rester passif.
    private IEnumerator ExecuteAITurn()
    {
        //Debug.Log($"[IA] ===== DÉBUT DU TOUR IA ExecuteAITurn =====");
        int aiCardsCount = BoardManager.cardsOnBoardAI.Count(c => c != null && !c.isHiddenSlot);
        
        if (aiCardsCount == 0)
        {
            Debug.Log("[IA] ⚠️ Aucune carte IA trouvée - Arrêt du tour");
            yield break;
        }

        // Seuil minimal de score pour qu'une attaque soit envisagée
        const int seuilMinAttaque = 1;
        int attackSaved = 0;

        // Création de copies des listes pour pouvoir les modifier sans affecter les originaux
        List<CardAI> cardsAIOnBoard = BoardManager.cardsOnBoardAI.Where(c => c != null && !c.isHiddenSlot).ToList();
        List<CardUI> cardsUIOnBoard = BoardManager.cardsOnBoardUI.Where(c => c != null && !c.isHiddenSlot).ToList();

        bool hasSoliciaOpponent = cardsUIOnBoard.Any(c => c.nameCard == "Solicia");
        bool hasMinosonOpponent = cardsUIOnBoard.Any(c => c.nameCard == "Minoson");
        bool hasBelindraOpponent = cardsUIOnBoard.Any(c => c.nameCard == "Belindra" && c.stateOffensif == "passed");
        
        Debug.Log($"[IA] Boucle d'exécution - Max attaques: {GameManager.MAX_NUMBER_ATK_ROUND}, Cartes dispo: {cardsAIOnBoard.Count}");
        
        while (attackSaved < GameManager.MAX_NUMBER_ATK_ROUND && cardsAIOnBoard.Count > 0)
        {
            Debug.Log($"[IA] --- Itération {attackSaved + 1}/{GameManager.MAX_NUMBER_ATK_ROUND} ---");
            
            int bestScoring = 0;
            CardAI bestAttacker = null;
            CardUI bestTarget = null;

            // Pour chaque carte IA disponible, on évalue la meilleure action
            Debug.Log($"[IA] Évaluation de {cardsAIOnBoard.Count} cartes IA...");
            foreach (var cardAI in cardsAIOnBoard)
            {
                if (cardAI.isFrozen) continue;

                // Décision : attaquer ou passif
                var decision = IAAction.DecideAction(cardAI, cardsAIOnBoard, cardsUIOnBoard);
                
                Debug.Log($"[IA]   {cardAI.nameCard} (ATK:{cardAI.attaqueValue}, DEF:{cardAI.defenseValue}) → " +
                         $"attack={decision.attack}, score={decision.score}, " +
                         $"target={decision.target.nameCard}");

                // Si la décision est d'attaquer et que le score est suffisant
                if (decision.attack && decision.score > bestScoring && decision.score >= seuilMinAttaque)
                {
                    bestScoring = decision.score;
                    bestAttacker = cardAI;
                    bestTarget = decision.target;
                }
            }

            // Si on a trouvé une bonne attaque, on la garde
            if (bestAttacker != null && bestTarget != null)
            {
                Debug.Log($"[IA] ✓ Meilleure action trouvée: {bestAttacker.nameCard} → {bestTarget.nameCard} (score: {bestScoring})");
                
                SaveAttack(bestAttacker, bestTarget, hasSoliciaOpponent, hasMinosonOpponent, hasBelindraOpponent);
                attackSaved++;
                Debug.Log($"[IA] Attaques sauvegardées: {attackSaved}/{GameManager.MAX_NUMBER_ATK_ROUND}");
                
                // Retire l'attaquant de la liste pour qu'il n'attaque qu'une fois
                cardsAIOnBoard.Remove(bestAttacker);
                // Retire la cible de la liste
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

        yield return new WaitForSeconds(1f);
        
        Debug.Log($"[IA] Attaques stockées: {aiAttacksThisTurn.Count}");
        foreach (var attack in aiAttacksThisTurn)
        {
            Debug.Log($"[IA] - {attack.attackerAI.nameCard} → {attack.targetPlayer.nameCard} ({attack.damage} dégâts)");
        }

        GameManager.Instance.isEndturnAI = true;
        Debug.Log($"[BOARD] ===== FIN DU TOUR IA =====");

        var aiCards = BoardManager.cardsOnBoardAI.Where(c => c != null && !c.isHiddenSlot);
        int visibleAICards = aiCards.Count();
        int attacksCount = aiCards.Count(c => c.stateOffensif == "atk");
        int passesCount = aiCards.Count(c => c.stateOffensif == "passed");

        Debug.Log($"[BOARD] Toutes les cartes IA ont fait leur choix ({visibleAICards} cartes)");
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
        // aiAttacksThisTurn - les attaques de l'IA
        
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

        yield return new WaitForSeconds(0.5f);
        
        aiAttacksThisTurn.Clear();
        playerAttacks.Clear();

        GameManager.Instance.confirmEndRound();
    }

    public void ApplyBonus(IEnumerable<ICard> cards)
    {
        foreach (var card in cards)
        {
            switch (card.stateOffensif)
            {
                case "passed":
                    ApplyBonusPassed(card);
                    break;
                case "atk":
                    ApplyBonusAtk(card);
                    break;
            }
            switch (card.stateDefensif)
            {
                case "cibled":
                    ApplyBonusCibled(card);
                    break;
                case "notCibled":
                    ApplyBonusNotCibled(card);
                    break;
            }
        }
    }

    public void ApplyBonusAtk(ICard card)
    {
    }

    public void ApplyBonusPassed(ICard card)
    {
        if (card.nameCard == "Clorel")
        {
            buffDf(card);
        }
        if (card.nameCard == "Cassandre")
        {
            var (left, right) = BoardManager.Instance.GetAdjacentCards(card);

            if (left != null) buffAtk(left);
            if (right != null) buffAtk(right);
        }
        if (card.nameCard == "Désir")
        {
            if(card.isCardPlayer){
                int index = Random.Range(0, aiAttacksThisTurn.Count);
                aiAttacksThisTurn.RemoveAt(index);
            }
            else{
                var playerAttacks = GetPlayerAttacks();
                int index = Random.Range(0, playerAttacks.Count);
                playerAttacks.RemoveAt(index);
            }
        }
        if (card.nameCard == "Trahison")
        {
            List<ICard> targets = new();

            if (card.isCardPlayer)
                targets.AddRange(BoardManager.cardsOnBoardAI.Where(c => c != null && !c.isHiddenSlot && c.stateOffensif == "passed"));
            else
                targets.AddRange(BoardManager.cardsOnBoardUI.Where(c => c != null && !c.isHiddenSlot && c.stateOffensif == "passed"));

            foreach (var c in targets)
            {
                debuffDf(c);
            }
        }
        if (card.nameCard == "Ambroise")
        {
            var targets = (card.isCardPlayer
                ? BoardManager.cardsOnBoardAI.Cast<ICard>()
                : BoardManager.cardsOnBoardUI.Cast<ICard>())
                .Where(c => c != null && c.stateOffensif == "passed")
                .ToList();

            if (targets.Count > 0)
            {
                var randomCard = targets[Random.Range(0, targets.Count)];
                debuffDf(randomCard);
            }
        }
    }
    public void ApplyBonusCibled(ICard card)
    {
        if (card.nameCard == "Zarla")
        {
            buffAtk(card);
        }
    }
    public void ApplyBonusNotCibled(ICard card)
    {
        if (card.nameCard == "Zarla")
        {
            buffDf(card);
        }
    }

    public IEnumerator ApplyAllBonus()
    {
        Debug.Log($"[ATTACK] ===== APPLICATION DES BONUS =====");
        
        List<CardAI> cardsAIOnBoard = BoardManager.cardsOnBoardAI.Where(c => c != null && !c.isHiddenSlot).ToList();
        List<CardUI> cardsUIOnBoard = BoardManager.cardsOnBoardUI.Where(c => c != null && !c.isHiddenSlot).ToList();
        
        bool aiStart = GameManager.Instance.aiStart;

        if (aiStart)
        {
            ApplyBonus(cardsAIOnBoard);
            ApplyBonus(cardsUIOnBoard);
        }
        else
        {
            ApplyBonus(cardsUIOnBoard);
            ApplyBonus(cardsAIOnBoard);
        }

        //ApplyAllMalusEndTurn(cardsUIOnBoard);
        //ApplyAllMalusEndTurn(cardsAIOnBoard);

        yield return new WaitForSeconds(0.5f);
    }

    public IEnumerator ApplyAllMalusEndTurn(IEnumerable<ICard> cards)
    {
        Debug.Log($"[ATTACK] ===== APPLICATION DES MALUS A LA FIN DU TOUR =====");

        foreach (var card in cards)
        {
            if (card is CardUI ui)
            {
                if (ui.freezeAtTurn == GameManager.Instance.round + 1)
                {
                    ui.isFrozen = true;
                }
            }

            if (card is CardAI ai)
            {
                if (ai.freezeAtTurn == GameManager.Instance.round + 1)
                {
                    ai.isFrozen = true;
                }
            }
        }

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

        yield return new WaitForSeconds(0.5f);
    }
    

    /// Save une attaque de l'IA : met à jour les états et applique les effets visuels.
    /// <param name="attacker">La carte IA qui attaque (CardAI)</param>
    /// <param name="target">La carte joueur ciblée (CardUI)</param>
    private void SaveAttack(CardAI attacker, CardUI target, bool hasSoliciaOpponent, bool hasMinosonOpponent, bool hasBelindraOpponent)
    {
        if (attacker == null || target == null) return;

        // Applique l'effet visuel de l'attaque
        ApplyIAAttackVisualEffect(attacker);
        
        // Simule l'attaque : met à jour les compteurs et les états
        SimulateAIAttack(attacker, target);

        // Applique l'attaque : met à jour l'UI et calcule les dégâts
        ApplyAttack(attacker, target, hasSoliciaOpponent, hasMinosonOpponent, hasBelindraOpponent);
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
    private void ApplyAttack(CardAI attacker, CardUI target, bool hasSoliciaOpponent, bool hasMinosonOpponent, bool hasBelindraOpponent)
    {
        if (attacker == null || target == null || attacker.isHiddenSlot || target.isHiddenSlot) return;
        
        // Compte le nombre d'attaques déjà reçues par la cible
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
       // Debug.Log($"[IA]   Attaques IA stockées: {aiAttacksThisTurn.Count + 1}");
        
        // Stocke l'attaque pour l'appliquer à la fin du tour
        int precision = 100;
        aiAttacksThisTurn.Add(new AttackInfo(attacker, target, damage, precision, hasSoliciaOpponent, hasMinosonOpponent, hasBelindraOpponent));
    }

    /// Fait passer le tour d'une carte IA
    private void ExecutePass(CardAI card)
    {
        if (card == null || card.isHiddenSlot) return;
        
        // Met à jour l'état de la carte
        card.actionChoiceDo = true;
        card.stateOffensif = "passed";
        
        card.imageCarte.color = new Color(0.4f, 0.4f, 0.4f, 1f);

        // Sauvegarde la position de départ
        Vector3 startPosition = card.rectTransform.anchoredPosition;
        
        // Déplace la carte vers le bas pour l'effet visuel
        Vector3 newPosition = startPosition + new Vector3(0, +30, 0);
        card.rectTransform.anchoredPosition = newPosition;
        
        Debug.Log($"[IA] ✓ {card.nameCard} passe son tour (ATK:{card.attaqueValue}, DEF:{card.defenseValue})");
    }
    
    /// Coroutine pour démarrer le tour de l'IA après un délai.
    public IEnumerator StartAITurnCoroutine()
    {
        yield return new WaitForSeconds(2f);
        StartAITurn();
    }
    
    /// Déplace légèrement la carte vers le bas pour indiquer l'attaque. AI
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
    }

    /// Applique une seule attaque et met à jour les dégâts de la cible.
    private static void ApplySingleAttack(AttackInfo attack)
    {
        CardUI attackerPlayer = null;
        CardAI attackerAI = null;

        CardUI targetPlayer = null;
        CardAI targetAI = null;

        if (attack.isPlayerAttack)
        {
            attackerPlayer = attack.attackerPlayer;
            targetAI = attack.targetAI;
        }
        else
        {
            attackerAI = attack.attackerAI;
            targetPlayer = attack.targetPlayer;
        }

        // ===== VARIABLES ACTIVES =====
        string attackerName;
        string targetName;

        int attackerDefense;
        int targetDefense;

        int attackerAtk;
        int targetAtk;

        bool isPlayerAttack = attack.isPlayerAttack;

        if (isPlayerAttack)
        {
            attackerName = attackerPlayer.nameCard;
            targetName = targetAI.nameCard;

            attackerDefense = attackerPlayer.defenseValue;
            targetDefense = targetAI.defenseValue;

            attackerAtk = attackerPlayer.attaqueValue;
            targetAtk = targetAI.attaqueValue;
        }
        else
        {
            attackerName = attackerAI.nameCard;
            targetName = targetPlayer.nameCard;

            attackerDefense = attackerAI.defenseValue;
            targetDefense = targetPlayer.defenseValue;

            attackerAtk = attackerAI.attaqueValue;
            targetAtk = targetPlayer.attaqueValue;
        }

        // ===== BASE =====
        int damage = attack.damage;

        // ===== MODS SPECIFIQUES =====

        // si on vise une carte qui attaque, aucun dégat
        if (attack.targetStateOffensif == "atk"
                && attack.targetStateDefensif == "isAttacked" 
                && targetName != "Zao")
        {
            damage = 0;
            Debug.Log($"[CIBLAGE] → {targetName} non touché par {attackerName} car en ATK");
        }

        // Ondine inflige uniquement des dégâts si elle est ciblée
        if(attackerName  == "Ondine" && attack.targetStateDefensif != "isAttacked"){
            damage = 0;
            Debug.Log($"[ATTACK] → {attackerName} ne peut infliger de dégâts car elle n'est pas ciblée");
        }

        // sauf Zao qui est inversée
        if (targetName == "Zao" && attack.targetStateOffensif == "passed"){
            damage = 0;
            Debug.Log($"[CIBLAGE] → {targetName} non touché par {attackerName} car en PASSED");
        }

        // la cible attaquée par Hiver sera gelée
        if(attackerName == "Hiver"){
            if (isPlayerAttack)
                targetAI.freezeAtTurn = GameManager.Instance.round + 1;
            else
                targetPlayer.freezeAtTurn = GameManager.Instance.round + 1;

            Debug.Log($"[ATTACK] → {attackerName} froze {targetName}");
        }

        // Neo gagne + 1 ATK à chaque tour à son attaque si cible différente
        if(attackerName == "Neo"){
            if (isPlayerAttack && targetName != attackerPlayer.lastTarget){
                attackerAtk = attackerPlayer.attaqueValue + 1;
                Debug.Log($"[ATTACK] → {attackerName} gagne 1 ATK car cible {targetName} différente de la dernière ({attackerPlayer.lastTarget})");

            }

            if (!isPlayerAttack && targetName != attackerAI.lastTarget){
                attackerAtk = attackerAI.attaqueValue + 1;
                Debug.Log($"[ATTACK] → {attackerName} gagne 1 ATK car cible {targetName} différente de la dernière ({attackerAI.lastTarget})");
            }

        }

        // Belindra : réduit les dégâts de 1 pour les alliés de Belindra si elle passe son tour
        if(attack.hasBelindraOpponent){
            damage = damage - 1;
            Debug.Log($"[CIBLAGE] → Présence de Belindra, inflige -1 dégât à {targetName}");
        }

        // anaxagore -1 de DF à chaque attaque
        if(attackerName == "Anaxagore"){
            targetDefense = targetDefense - 1;
            Debug.Log($"[ATTACK] → {attackerName} attaque, inflige -1 DF à {targetName}");
        }

        // vilaine -1 ATK à sa cible sur le tour courant
        if(attackerName == "Vilaine"){
            attackerAtk = attackerAtk - 1;
            Debug.Log($"[ATTACK] → {attackerName} attaque, inflige -1 ATK à {targetName}");
        }

        // Ruby dégat aléatoire entre 0 et 4 points
        if(attackerName == "Ruby"){
            damage = Random.Range(0, 5);
            Debug.Log($"[ATTACK] → {attackerName} inflige {damage}] dégâts à {targetName}");
        }

        // 1 chance sur 2 de gagner ou perdre 1 de df à chaque attaque
        if(attackerName == "Triomphe"){
            int defenseRandom = Random.Range(-1, 2);
            if(defenseRandom == -1){
                attackerDefense = attackerDefense - 1;
                Debug.Log($"[ATTACK] → {attackerName} perd 1 DF (DF avant: {attackerDefense}, DF après: {attackerDefense})");
            }
            if(defenseRandom == 1){
                attackerDefense = attackerDefense + 1;
                Debug.Log($"[ATTACK] → {attackerName} gagne 1 DF (DF avant: {attackerDefense}, DF après: {attackerDefense})");
            }
        }

        // Tyroine vise un adversaire aléatoirement et ignore 1 point DF
        if(attackerName == "Tyroine"){
            damage = damage + 1;
            Debug.Log($"[ATTACK] → {attackerName} ignore 1 DF - cible {targetName})");
        }
        
        // Xiang ignore 1 point de DF
        if(attackerName == "Xiang"){
            damage = damage + 1;
            Debug.Log($"[ATTACK] → {attackerName} ignore 1 DF)");
        }

        // Quand un allié est attaqué si présence Solicia, inflige -1 DF à l'attaquant
        if(attack.hasSoliciaOpponent){
            attackerDefense = attackerDefense - 1;
            Debug.Log($"[ATTACK] → présence de Solicia, inflige -1 DF à {attackerName}");
        }

        if(attack.hasMinosonOpponent){
           int mustTouchMinoson = Random.Range(-1, 2);
           if(mustTouchMinoson == 1){
                int half = damage / 2;

                targetDefense = targetDefense - half;

                if (attack.isPlayerAttack)
                {
                    CardAI minoson = BoardManager.cardsOnBoardAI.FirstOrDefault(c => c != null && !c.isHiddenSlot && c.nameCard == "Minoson");
                    if (minoson != null)
                    {
                        minoson.defenseValue = minoson.defenseValue + half;
                        minoson.defenseText.SetText(minoson.defenseValue.ToString());
                        Debug.Log($"[ATTACK] → {attackerName} transfère 50% des dégâts à Minoson IA (dégâts: {damage}, 50%: {half})");
                    }
                }
                else
                {
                    CardUI minoson = BoardManager.cardsOnBoardUI.FirstOrDefault(c => c != null && !c.isHiddenSlot && c.nameCard == "Minoson");
                    if (minoson != null)
                    {
                        minoson.defenseValue = minoson.defenseValue + half;
                        minoson.defenseText.SetText(minoson.defenseValue.ToString());
                        Debug.Log($"[ATTACK] → {attackerName} transfère 50% des dégâts à Minoson joueur (dégâts: {damage}, 50%: {half})");
                    }
                }
           }
           else
           {
                Debug.Log($"[ATTACK] → {attackerName} pas de transfert de dégâts à Minoson");
           }
        }

        if(targetName == "Jaycota"){
            attackerDefense = attackerDefense - 1;
            Debug.Log($"[ATTACK] → {targetName} ciblé, inflige -1 DF en retour à {attackerName})");
        }

        // ===== APPLICATION DES MODS =====

        targetDefense = targetDefense - damage;
        attackerDefense = attackerDefense - damage;

        if (targetDefense < 0) targetDefense = 0;
        if (attackerDefense < 0) attackerDefense = 0;

       // ===== WRITE BACK =====
        if (isPlayerAttack)
        {
            targetAI.defenseValue = targetDefense;
            attackerPlayer.defenseValue = attackerDefense;

            targetAI.defenseText.SetText(targetDefense.ToString());
            attackerPlayer.defenseText.SetText(attackerDefense.ToString());

            targetAI.attaqueText.SetText(targetAtk.ToString());
            attackerPlayer.attaqueText.SetText(attackerAtk.ToString());
        }
        else
        {
            targetPlayer.defenseValue = targetDefense;
            attackerAI.defenseValue = attackerDefense;

            targetPlayer.defenseText.SetText(targetDefense.ToString());
            attackerAI.defenseText.SetText(attackerDefense.ToString());

            targetPlayer.attaqueText.SetText(targetAtk.ToString());
            attackerAI.attaqueText.SetText(attackerAtk.ToString());
        }

        Debug.Log($"[ATTACK] → {attackerName} inflige {damage} à {targetName}");
        Debug.Log($"[targetDefense]   DEF avant: {(targetDefense + damage)}, DEF après: {targetDefense}");
        Debug.Log($"[attackerDefense]   DEF avant: {(attackerDefense + damage)}, DEF après: {attackerDefense}");

        // ===== ELIMINATION =====
        if (targetDefense <= 0)
        {
            if (isPlayerAttack) targetAI.isYellow = true;
            else targetPlayer.isYellow = true;
        }

        if (attackerDefense <= 0)
        {
            if (isPlayerAttack) attackerPlayer.isYellow = true;
            else attackerAI.isYellow = true;
        }
    }
    
    /// Récupère les attaques du joueur
    private static List<AttackInfo> GetPlayerAttacks()
    {
        List<AttackInfo> playerAttacks = new List<AttackInfo>();
        int precision = 100;
        
        int playerCardsCount = BoardManager.cardsOnBoardUI.Count(c => c != null && !c.isHiddenSlot);
        var activeAICards = BoardManager.cardsOnBoardAI.Where(c => c != null && !c.isHiddenSlot).ToList();
        var aiCardsById = activeAICards.ToDictionary(c => c.idCard);

        bool hasSoliciaOpponent = activeAICards.Any(c => c.nameCard == "Solicia");
        bool hasMinosonOpponent = activeAICards.Any(c => c.nameCard == "Minoson");
        bool hasBelindraOpponent = activeAICards.Any(c => c.nameCard == "Belindra" && c.stateOffensif == "passed");

        Debug.Log($"[ATTACK] Récupération des attaques du joueur depuis {playerCardsCount} cartes...");
        
        // Parcourt les cartes du joueur qui ont attaqué
        foreach (var cardUI in BoardManager.cardsOnBoardUI.Where(c => c != null && !c.isHiddenSlot && c.stateOffensif == "atk"))
        {
            if (cardUI.targetID == 0)
            {
                Debug.LogWarning($"[ATTACK] Pas de targetID pour {cardUI.nameCard}");
                continue;
            }

            // Trouve la cible IA correspondante
            if (!aiCardsById.TryGetValue(cardUI.targetID, out CardAI target))
            {
                Debug.LogWarning($"[ATTACK] Target ID introuvable: {cardUI.targetID} pour {cardUI.nameCard}");
                continue;
            }

            //int damage = cardUI.attaqueValue - target.defenseValue;
            // if (damage < 0) damage = 1;

            int damage = cardUI.attaqueValue;
            
            Debug.Log($"[ATTACK] Attaque joueur trouvée: {cardUI.nameCard} → {target.nameCard} " +
                        $"(ATK:{cardUI.attaqueValue} vs DEF:{target.defenseValue} = {damage} dégâts)");
            
            playerAttacks.Add(new AttackInfo(cardUI, target, damage, precision, hasSoliciaOpponent, hasMinosonOpponent, hasBelindraOpponent));
        }

        return playerAttacks;
    }

    /// BUFF ET DEBUFF ///
    public void buffAtk(ICard card)
    {
        card.attaqueValue++;
        card.attaqueText.SetText(card.attaqueValue.ToString());
    }
    public void buffDf(ICard card)
    {
        card.defenseValue++;
        card.defenseText.SetText(card.defenseValue.ToString());
    }
    public void debuffAtk(ICard card)
    {
        card.attaqueValue--;
        card.attaqueText.SetText(card.attaqueValue.ToString());
    }
    public void debuffDf(ICard card)
    {
        card.defenseValue--;
        card.defenseText.SetText(card.defenseValue.ToString());
    }
} 
