using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using static IAAction;
using static CardName;
using static OffensiveState;
using static DefensiveState;
using static PlayerActionState;

/// Gère le comportement de l'IA pour les tours de l'adversaire. - Utilise IAAction pour évaluer les meilleures actions et les exécute.
public class IA : MonoBehaviour
{
    private static IA instance;
    public static IA Instance => instance;
    private Coroutine aiTurnCoroutine;
    
    [Header("Paramètres")]
    [SerializeField] private float delayAction = 0.2f;
    
    private static List<AttackInfo> aiAttacks = new List<AttackInfo>();

    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
    }
    
    public void StartAITurn()
    {
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
        int aiCardsCount = BoardManager.cardsOnBoardAI.Count(c => !c.isHiddenSlot);
        if (aiCardsCount == 0)
        {
            Debug.Log("[IA] Aucune carte IA trouvée - Arrêt du tour");
            yield break;
        }

        // Seuil minimal de score pour qu'une attaque soit envisagée
        const int seuilMinAttaque = 1;
        int attackSaved = 0;

        // Création de copies des listes pour pouvoir les modifier sans affecter les originaux
        List<CardAI> cardsAIOnBoard = BoardManager.cardsOnBoardAI.Where(c => !c.isHiddenSlot).ToList();
        List<CardUI> cardsUIOnBoard = BoardManager.cardsOnBoardUI.Where(c => !c.isHiddenSlot).ToList();

        bool hasSoliciaOpponent = cardsUIOnBoard.Any(c => c.nameCard == CardName.SOLICIA);
        bool hasBelindraOpponentStatePassed = cardsUIOnBoard.Any(c => c.nameCard == CardName.BELINDRA && c.stateOffensif == OffensiveState.PASSED);
        //Debug.Log($"[IA] Boucle d'exécution - Max attaques: {GameManager.MAX_NUMBER_ATK_ROUND}, Cartes dispo: {cardsAIOnBoard.Count}");
        
        while (attackSaved < GameManager.MAX_NUMBER_ATK_ROUND && cardsAIOnBoard.Count > 0)
        {
            int bestScoring = 0;
            CardAI bestAttacker = null;
            CardUI bestTarget = null;

            // Pour chaque carte IA disponible, on évalue la meilleure action
            foreach (var cardAI in cardsAIOnBoard)
            {
                if (cardAI.isFrozen) continue;

                var decision = IAAction.DecideAction(cardAI, cardsAIOnBoard, cardsUIOnBoard);
                
                /*Debug.Log($"[IA]   {cardAI.nameCard} (ATK:{cardAI.attaqueValue}, DEF:{cardAI.defenseValue}) → " +
                         $"attack={decision.attack}, score={decision.score}, " +
                         $"target={decision.target.nameCard}");*/

                // Si la décision est d'attaquer et que le score est suffisant
                if (decision.attack && decision.score > bestScoring && decision.score >= seuilMinAttaque)
                {
                    bestScoring = decision.score;
                    bestAttacker = cardAI;
                    bestTarget = decision.target;
                }
            }

            if (bestAttacker != null && bestTarget != null)
            {
                // Debug.Log($"[IA] ✓ Meilleure action trouvée: {bestAttacker.nameCard} → {bestTarget.nameCard} (score: {bestScoring})");
                SaveAttack(bestAttacker, bestTarget, hasSoliciaOpponent, hasBelindraOpponentStatePassed);
                attackSaved++;
                
                cardsAIOnBoard.Remove(bestAttacker);
                cardsUIOnBoard.Remove(bestTarget);

                yield return new WaitForSeconds(delayAction);
            }
            else
            {
                Debug.Log($"[IA] Aucune attaque intéressante (score max: {bestScoring}, seuil: {seuilMinAttaque})");
                break;
            }
        }

        // Les cartes restantes qui n'ont pas attaqué passent leur tour
        foreach (var cardAI in cardsAIOnBoard)
        {
            if (!cardAI.actionChoiceDo)
            {
                ExecutePass(cardAI);
                yield return new WaitForSeconds(delayAction);
            }
        }

        yield return new WaitForSeconds(1f);
        
        foreach (var attack in aiAttacks)
        {
            Debug.Log($"[IA] - {attack.attackerAI.nameCard} → {attack.targetPlayer.nameCard} ({attack.damage} dégâts)");
        }

        GameManager.Instance.isEndturnAI = true;

        var aiCards = BoardManager.cardsOnBoardAI.Where(c => !c.isHiddenSlot);
        int visibleAICards = aiCards.Count();
        int attacksCount = aiCards.Count(c => c.stateOffensif == OffensiveState.ATK);
        int passesCount = aiCards.Count(c => c.stateOffensif == OffensiveState.PASSED);

        //Debug.Log($"[BOARD] Toutes les cartes IA ont fait leur choix ({visibleAICards} cartes)");
        //Debug.Log($"[BOARD] Résumé - Attaques: {attacksCount}, Passes: {passesCount}");

        // Si l'IA a commencé le tour, on rend ensuite la main au joueur.
        if (!GameManager.Instance.isEndturnPlayer)
        {
            GameManager.Instance.currentPlayerAction = PlayerActionState.UI;
            PanelManager.Instance?.ShowTurnBanner(PlayerActionState.UI);
            Debug.Log("[GAME] Transition de tour: IA -> JOUEUR");
        }

        aiTurnCoroutine = null;
    }
    /// Applique toutes les attaques (joueur + IA) de manière séquentielle
    public IEnumerator ApplyAllAttacksCoroutine()
    {
        Debug.Log($"[ATTACK] ===== APPLICATION DES ATTAQUES =====");
        List<AttackInfo> playerAttacks = GetPlayerAttacks();
        // aiAttacks - les attaques de l'IA
        
        List<AttackInfo> allAttacks = new();

        bool aiStart = GameManager.Instance.aiStart;
        allAttacks.AddRange(aiStart ? aiAttacks : playerAttacks);
        allAttacks.AddRange(aiStart ? playerAttacks : aiAttacks);

        // 3. Application séquentielle avec pause pour laisser l'Update() afficher le jaune
        foreach (var attack in allAttacks)
        {
            ApplySingleAttack(attack);
            yield return new WaitForSeconds(0.8f);
        }
        yield return new WaitForSeconds(0.5f);
        
        aiAttacks.Clear();
        playerAttacks.Clear();

        GameManager.Instance.confirmEndRound();
    }

    public IEnumerator ApplyAllBonus()
    {
        //Debug.Log($"[ATTACK] ===== APPLICATION DES BONUS =====");
        List<CardAI> cardsAIOnBoard = BoardManager.cardsOnBoardAI.Where(c => !c.isHiddenSlot).ToList();
        List<CardUI> cardsUIOnBoard = BoardManager.cardsOnBoardUI.Where(c => !c.isHiddenSlot).ToList();
        
        bool aiStart = GameManager.Instance.aiStart;

        var first = aiStart ? cardsAIOnBoard : cardsUIOnBoard;
        var second = aiStart ? cardsUIOnBoard : cardsAIOnBoard;
        ApplyBonus(first);
        ApplyBonus(second);

        //ApplyAllMalusEndTurn(cardsUIOnBoard);
        //ApplyAllMalusEndTurn(cardsAIOnBoard);
        yield return new WaitForSeconds(0.5f);
    }
    public void ApplyBonus(IEnumerable<ICard> cards)
    {
        foreach (var card in cards)
        {
            switch (card.stateOffensif)
            {
                case OffensiveState.PASSED:
                    ApplyBonusPassed(card);
                    break;
                case OffensiveState.ATK:
                    ApplyBonusAtk(card);
                    break;
            }
            switch (card.stateDefensif)
            {
                case DefensiveState.CIBLED:
                    ApplyBonusCibled(card);
                    break;
                case DefensiveState.NOT_CIBLED:
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
        if (card.nameCard == CardName.CLOREL)
        {
            buffDf(card);
        }
        if (card.nameCard == CardName.CASSANDRE && card is CardUI ui)
        {
            var (left, right) = BoardManager.Instance.GetAdjacentCards(ui);

            if (left != null) buffAtk(left);
            if (right != null) buffAtk(right);
        }
        // on annule une attaque si Désir "passed"
        if (card.nameCard == CardName.DESIR)
        {
            if(card.isCardPlayer){
                int index = Random.Range(0, aiAttacks.Count);
                aiAttacks.RemoveAt(index);
            }
            else{
                var playerAttacks = GetPlayerAttacks();
                int index = Random.Range(0, playerAttacks.Count);
                playerAttacks.RemoveAt(index);
            }
        }
        // si Trahison passed, on fait - 1DF pour les passed adversaires
        if (card.nameCard == CardName.TRAHISON)
        {
            List<ICard> targets = new();

            if (card.isCardPlayer)
                targets.AddRange(BoardManager.cardsOnBoardAI.Where(c => !c.isHiddenSlot && c.stateOffensif == OffensiveState.PASSED));
            else
                targets.AddRange(BoardManager.cardsOnBoardUI.Where(c => !c.isHiddenSlot && c.stateOffensif == OffensiveState.PASSED));

            foreach (var c in targets)
            {
                debuffDf(c);
            }
        }
        // si Ambroise passed, on fait -1DF à un adversaire aléatoire passif
        if (card.nameCard == CardName.AMBROISE)
        {
            List<ICard> enemyCards = new();
            if (card.isCardPlayer)
            {
                enemyCards = BoardManager.cardsOnBoardAI.Cast<ICard>().ToList();
            }
            else
            {
                enemyCards = BoardManager.cardsOnBoardUI.Cast<ICard>().ToList();
            }

            var targets = enemyCards.Where(c => !c.isHiddenSlot && c.stateOffensif == OffensiveState.PASSED).ToList();

            if (targets.Count > 0)
            {
                var randomCard = targets[Random.Range(0, targets.Count)];
                debuffDf(randomCard);
            }
        }
    }
    public void ApplyBonusCibled(ICard card)
    {
        if (card.nameCard == CardName.ZARLA)
        {
            buffAtk(card);
        }
    }
    public void ApplyBonusNotCibled(ICard card)
    {
        if (card.nameCard == CardName.ZARLA)
        {
            buffDf(card);
        }
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
        
        List<CardAI> cardsAIOnBoard = BoardManager.cardsOnBoardAI.Where(c => !c.isHiddenSlot && c.stateOffensif == OffensiveState.PASSED).ToList();
        List<CardUI> cardsUIOnBoard = BoardManager.cardsOnBoardUI.Where(c => !c.isHiddenSlot && c.stateOffensif == OffensiveState.PASSED).ToList();
        bool aiStart = GameManager.Instance.aiStart;

        yield return new WaitForSeconds(0.5f);
    }


    /// Save une attaque de l'IA : met à jour les états et applique les effets visuels.
    private void SaveAttack(CardAI attacker, CardUI target, bool hasSoliciaOpponent, bool hasBelindraOpponentStatePassed)
    {
        if (attacker == null || target == null) return;

        // Applique l'effet visuel de l'attaque
        ApplyIAAttackVisualEffect(attacker);
        
        // Simule l'attaque : met à jour les compteurs et les états
        SimulateAIAttack(attacker, target);

        // Applique l'attaque : met à jour l'UI et calcule les dégâts
        ApplyAttack(attacker, target, hasSoliciaOpponent, hasBelindraOpponentStatePassed);
    }
    /// Simule une attaque de l'IA : met à jour les états et enregistre l'attaque.
    private void SimulateAIAttack(CardAI attacker, CardUI target)
    {
        GameManager.Instance.numberOfAttacksUsedIA++;

        // Met à jour l'état de l'attaquant
        attacker.actionChoiceDo = true;
        attacker.stateOffensif = OffensiveState.ATK;
        target.stateDefensif = DefensiveState.CIBLED;
        
        // Met à jour la cible de l'attaquant
        attacker.target = target.nameCard;
        attacker.targetID = target.idCard;
        
        // Met à jour la dernière cible
        attacker.lastTarget = target.nameCard;
    }
    /// Applique une attaque : met à jour l'interface utilisateur et calcule les dégâts.
    private void ApplyAttack(CardAI attacker, CardUI target, bool hasSoliciaOpponent, bool hasBelindraOpponentStatePassed)
    {
        if (attacker == null || target == null || attacker.isHiddenSlot || target.isHiddenSlot) return;
        
        if (target.atk1Icon != null)
        {
            target.atk1Icon.SetActive(true);
        }

        // Calcule les dégâts potentiels
        int damage = attacker.attaqueValue;
        target.defenseValue -= damage;
        target.defenseValue = Mathf.Max(0, target.defenseValue);
  
        // Enregistre l'attaque dans les logs du tour
        string attackLog = $"{attacker.nameCard} → {target.nameCard} (ATK:{attacker.attaqueValue} vs DEF:{target.defenseValue}) = {damage} dégâts";
        BoardManager.Instance.roundDamage.Add(attackLog);
        
        aiAttacks.Add(new AttackInfo(attacker, target, damage, hasSoliciaOpponent, hasBelindraOpponentStatePassed));
    }

    private void ExecutePass(CardAI card)
    {
        if (card.isHiddenSlot) return;
        
        card.actionChoiceDo = true;
        card.stateOffensif = OffensiveState.PASSED;
        card.imageCarte.color = new Color(0.4f, 0.4f, 0.4f, 1f);

        Vector3 startPosition = card.rectTransform.anchoredPosition;
        Vector3 newPosition = startPosition + new Vector3(0, +30, 0);
        card.rectTransform.anchoredPosition = newPosition;
    }

    /// Déplace légèrement la carte vers le bas pour indiquer l'attaque. AI
    private void ApplyIAAttackVisualEffect(CardAI card)
    {        
        Vector3 startPosition = card.rectTransform.anchoredPosition;
        Vector3 newPosition = startPosition + new Vector3(0, -50, 0);
        card.rectTransform.anchoredPosition = newPosition;

        Transform atkTransform = card.rectTransform.transform.Find("Atk");
        atkTransform.gameObject.SetActive(true);
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

        string attackerName = attack.AttackerName;
        string targetName = attack.TargetName;

        int attackerDefense = attack.isPlayerAttack ? attack.attackerPlayer.defenseValue : attack.attackerAI.defenseValue;
        int targetDefense = attack.isPlayerAttack ? attack.targetAI.defenseValue : attack.targetPlayer.defenseValue;
        int attackerAtk = attack.isPlayerAttack ? attack.attackerPlayer.attaqueValue : attack.attackerAI.attaqueValue;
        int targetAtk = attack.isPlayerAttack ? attack.targetAI.attaqueValue : attack.targetPlayer.attaqueValue;
        string lastTargetName = attack.isPlayerAttack ? attack.attackerPlayer.lastTarget : attack.attackerAI.lastTarget;

        // ===== BASE =====
        int damage = attack.damage;

        // si on vise une carte qui attaque, aucun dégat
        if (attack.targetStateOffensif == OffensiveState.ATK
                && attack.targetStateDefensif == DefensiveState.CIBLED
                && targetName != CardName.ZAO)
        {
            damage = 0;
            Debug.Log($"[CIBLAGE] → {targetName} non touché par {attackerName} car en ATK");
        }
        // Ondine inflige uniquement des dégâts si elle est ciblée
        if(attackerName == CardName.ONDINE && attack.attackerStateDefensif != DefensiveState.CIBLED){
            damage = 0;
            Debug.Log($"[ATTACK] → {attackerName} ne peut infliger de dégâts car elle n'est pas ciblée");
        }
        // sauf Zao qui est inversée
        if (targetName == CardName.ZAO && attack.targetStateOffensif == OffensiveState.PASSED){
            damage = 0;
            Debug.Log($"[CIBLAGE] → {targetName} non touché par {attackerName} car en PASSED");
        }
        // la cible attaquée par Hiver sera gelée
        if(attackerName == CardName.HIVER){
            if (attack.isPlayerAttack)
                targetAI.freezeAtTurn = GameManager.Instance.round + 1;
            else
                targetPlayer.freezeAtTurn = GameManager.Instance.round + 1;

            Debug.Log($"[ATTACK] → {attackerName} froze {targetName}");
        }
        // Neo gagne + 1 ATK à chaque tour à son attaque si cible différente
        if(attackerName == CardName.NEO && targetName != lastTargetName){
            attackerAtk = attackerAtk + 1;
            Debug.Log($"[ATTACK] → {attackerName} gagne 1 ATK car cible {targetName} différente de la dernière ({lastTargetName})");
        }
        // Belindra : réduit les dégâts de 1 pour les alliés de Belindra si elle passe son tour
        if(attack.hasBelindraOpponentStatePassed){
            damage = damage - 1;
            Debug.Log($"[CIBLAGE] → Présence de Belindra, inflige -1 dégât à {targetName}");
        }
        // anaxagore -1 de DF à chaque attaque
        if(attackerName == CardName.ANAXAGORE){
            targetDefense = targetDefense - 1;
            Debug.Log($"[ATTACK] → {attackerName} attaque, inflige -1 DF à {targetName}");
        }
        // vilaine -1 ATK à sa cible sur le tour courant
        if(attackerName == CardName.VILAINE){
            targetAtk = targetAtk - 1;
            Debug.Log($"[ATTACK] → {attackerName} attaque, inflige -1 ATK à {targetName}");
        }
        // Ruby dégat aléatoire entre 0 et 4 points
        if(attackerName == CardName.RUBY){
            damage = Random.Range(0, 5);
            Debug.Log($"[ATTACK] → {attackerName} inflige {damage} dégâts à {targetName}");
        }
        // 1 chance sur 2 de gagner ou perdre 1 de df à chaque attaque
        if(attackerName == CardName.TRIOMPHE){
            int defenseRandom = Random.Range(-1, 2);
            if(defenseRandom == -1){
                attackerDefense = attackerDefense - 1;
                Debug.Log($"[ATTACK] → {attackerName} perd 1 DF (DF {attackerDefense}, : {attackerDefense})");
            }
            if(defenseRandom == 1){
                attackerDefense = attackerDefense + 1;
                Debug.Log($"[ATTACK] → {attackerName} gagne 1 DF (DF {attackerDefense} : {attackerDefense})");
            }
        }
        // Tyroine vise un adversaire aléatoirement et ignore 1 point DF
        if(attackerName == CardName.TYROINE){
            damage = damage + 1;
            Debug.Log($"[ATTACK] → {attackerName} ignore 1 DF - cible {targetName})");
        }
        // Xiang ignore 1 point de DF
        if(attackerName == CardName.XIANG){
            damage = damage + 1;
            Debug.Log($"[ATTACK] → {attackerName} ignore 1 DF)");
        }
        // Quand un allié est attaqué si présence Solicia, inflige -1 DF à l'attaquant
        if(attack.hasSoliciaOpponent){
            attackerDefense = attackerDefense - 1;
            Debug.Log($"[ATTACK] → présence de Solicia, inflige -1 DF à {attackerName}");
        }
        // si Minoson attaque, il donne 1DF lui appartenant à un allié attaqué
        if(attackerName == CardName.MINOSON && attack.attackerStateOffensif == OffensiveState.ATK){
        
            List<ICard> targets = new();
            var targetsAI = BoardManager.cardsOnBoardAI.Where(c => !c.isHiddenSlot && c.stateDefensif == DefensiveState.CIBLED).Cast<ICard>().ToList();
            var targetsUI = BoardManager.cardsOnBoardUI.Where(c => !c.isHiddenSlot && c.stateDefensif == DefensiveState.CIBLED).Cast<ICard>().ToList();

            targets = attack.isPlayerAttack ? targetsUI : targetsAI;
   
            if(targets.Count > 0){
                var randomCard = targets[Random.Range(0, targets.Count)];
                buffDf(randomCard);
                attackerDefense = attackerDefense - 1;
                Debug.Log($"[ATTACK] → {attackerName} transfère 1DF à {randomCard.nameCard}");
            }else{
                Debug.Log($"[ATTACK] → {attackerName} pas d'allie attaqué");
            }
        }
        if(targetName == CardName.JAYCOTA){
            attackerDefense = attackerDefense - 1;
            Debug.Log($"[DEFENSE] → {targetName} ciblé, inflige -1 DF en retour à {attackerName})");
        }

        targetDefense = Mathf.Max(0, targetDefense - damage);
        attackerDefense = Mathf.Max(0, attackerDefense);

        if (attack.isPlayerAttack)
        {
            targetAI.defenseValue = targetDefense;
            attackerPlayer.defenseValue = attackerDefense;
            targetAI.attaqueValue = targetAtk;
            attackerPlayer.attaqueValue = attackerAtk;
            targetAI.defenseText.SetText(targetDefense.ToString());
            attackerPlayer.defenseText.SetText(attackerDefense.ToString());
            targetAI.attaqueText.SetText(targetAtk.ToString());
            attackerPlayer.attaqueText.SetText(attackerAtk.ToString());
        }
        else
        {
            targetPlayer.defenseValue = targetDefense;
            attackerAI.defenseValue = attackerDefense;
            targetPlayer.attaqueValue = targetAtk;
            attackerAI.attaqueValue = attackerAtk;
            targetPlayer.defenseText.SetText(targetDefense.ToString());
            attackerAI.defenseText.SetText(attackerDefense.ToString());
            targetPlayer.attaqueText.SetText(targetAtk.ToString());
            attackerAI.attaqueText.SetText(attackerAtk.ToString());
        }

        Debug.Log($"[ATTACK] → {attackerName} inflige {damage} à {targetName}");
        Debug.Log($"[TARGET] {targetName} DEF: {targetDefense} ATK: {targetAtk} | [ATTACKER] {attackerName} DEF: {attackerDefense} ATK: {attackerAtk}");

        if (attack.isPlayerAttack)
        {
            if (targetDefense <= 0)
                targetAI.isYellow = true;

            if (attackerDefense <= 0)
                attackerPlayer.isYellow = true;
        }
        else
        {
            if (targetDefense <= 0)
                targetPlayer.isYellow = true;

            if (attackerDefense <= 0)
                attackerAI.isYellow = true;
        }
    }
    
    private static List<AttackInfo> GetPlayerAttacks()
    {
        List<AttackInfo> playerAttacks = new List<AttackInfo>();
        
        var activeAICards = BoardManager.cardsOnBoardAI.Where(c => !c.isHiddenSlot).ToList();
        var aiCardsById = activeAICards.ToDictionary(c => c.idCard);
        var atkCardsUI = BoardManager.cardsOnBoardUI.Where(c => !c.isHiddenSlot && c.stateOffensif == OffensiveState.ATK).ToList();

        bool hasSoliciaOpponent = activeAICards.Any(c => c.nameCard == CardName.SOLICIA);
        bool hasBelindraOpponentStatePassed = activeAICards.Any(c => c.nameCard == CardName.BELINDRA && c.stateOffensif == OffensiveState.PASSED);
        
        foreach (var cardUI in atkCardsUI)
        {
            if (cardUI.targetID == 0)
            {
                Debug.LogWarning($"[ATTACK UI] targetID à 0 pour {cardUI.nameCard}");
                continue;
            }

            if (!aiCardsById.TryGetValue(cardUI.targetID, out CardAI target))
            {
                Debug.LogWarning($"[ATTACK UI] targetID introuvable: {cardUI.targetID} pour {cardUI.nameCard}");
                continue;
            }

            Debug.Log($"[ATTACK UI] {cardUI.nameCard} → {target.nameCard} " + $"(ATK:{cardUI.attaqueValue} - DEF:{target.defenseValue}");

            playerAttacks.Add(new AttackInfo(cardUI, target, cardUI.attaqueValue, hasSoliciaOpponent, hasBelindraOpponentStatePassed));
        }

        return playerAttacks;
    }

    /// BUFF ET DEBUFF
    public static void buffAtk(ICard card)
    {
        card.attaqueValue++;
        if (card is CardUI ui)
            ui.attaqueText.SetText(ui.attaqueValue.ToString());
        if (card is CardAI ai)
            ai.attaqueText.SetText(ai.attaqueValue.ToString());
    }
    public static void buffDf(ICard card)
    {
        card.defenseValue++;
        if (card is CardUI ui)
            ui.defenseText.SetText(ui.defenseValue.ToString());
        if (card is CardAI ai)
            ai.defenseText.SetText(ai.defenseValue.ToString());

    }
    public static void debuffAtk(ICard card)
    {
        card.attaqueValue--;
        if (card is CardUI ui)
            ui.attaqueText.SetText(ui.attaqueValue.ToString());
        if (card is CardAI ai)
            ai.attaqueText.SetText(ai.attaqueValue.ToString());
    }
    public static void debuffDf(ICard card)
    {
        card.defenseValue--;
        if (card is CardUI ui)
            ui.defenseText.SetText(ui.defenseValue.ToString());
        if (card is CardAI ai)
            ai.defenseText.SetText(ai.defenseValue.ToString());
    }
} 
