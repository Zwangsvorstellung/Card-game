using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

public static class IAAction
{
    public enum Capacity {
        AgiliteRisque,
        FrappeGelee,
        AuraDeForce,
        ReflexionPartielle,
        FrappePuissante,
        Tentation,
        MalusAttaque,
        AttaqueProvocation,
        AttaqueSurprise,
        PerceeDefensive,
        Regeneration,
        BouclierCollectif,
        TerreurSelective,
        IgnoranceDefensive,
        OndeDeChocPassive,
        Aubaine,
        AttaqueAleatoire,
        VagueLetale,
        Combo,
        Protection
    }

    public static List<Capacity> Capacites;

    // signature : attacker = carte qui envisagerait d'attaquer
    // defender = carte visée
    // attackerAllies = autres cartes du même camp que attacker (inclut ou non attacker selon ton implémentation)
    // defenderAllies = autres cartes du camp adverse (inclut ou non defender selon ton implémentation)
    public static int RateAttack(
        CarteBoardInteraction attacker,
        CarteBoardInteraction defender,
        List<CarteBoardInteraction> attackerAllies,
        List<CarteBoardInteraction> defenderAllies)
    {
        // 0) Garde-fous : si null on renvoie 0
        if (attacker == null || defender == null) return 0;

        // 1) Valeurs de base
        int atk = attacker.GetAttackValue(attacker);
        int def = defender.GetDefenseValue(defender);

        int baseDamage = 5;

        if (atk - def < 0)
            baseDamage = 1;

        // 4) Score initial = dégâts potentiels
        int scoring = baseDamage;

        // 2) Réduire la DEF si attaque (Ignorance Défensive/Percée Défensive)
        // Percée Défensive / Ignorance Défensive (réduit défense cible) : bonus supplémentaire
        if (attacker.HasCapacity(Capacity.IgnoranceDefensive) || attacker.HasCapacity(Capacity.PerceeDefensive))
            scoring += 1;  // +1 en plus de réduire la défense

        // ---------------------------
        // 5) Bonus offensifs (capacités de l'attaquant)
        // ---------------------------

        // Frappe Puissante : +1 dégât supplémentaire
        if (attacker.HasCapacity(Capacity.FrappePuissante))
            scoring += 1;

        // Attaque Surprise : bonus si attaque une cible différente de la précédente
        if (attacker.HasCapacity(Capacity.AttaqueSurprise) && attacker.lastTarget != null && attacker.lastTarget != defender.name)
            scoring += 1;

        if (attacker.HasCapacity(Capacity.Combo) && attackerAllies != null)
        {
            // Cherche si un allié attaque une cible adjacente à la cible actuelle
            bool allyCombo = attackerAllies.Any(a =>
                a != attacker &&
                a.CurrentTarget != null &&
                CarteBoardInteraction.IsAdjacentTo(defender, a.CurrentTarget)
            );
            if (allyCombo)
                scoring += 1;
        }
        

        // Vague Létale : touche 2 ennemis aléatoires, avantage si plusieurs ennemis
        if (attacker.HasCapacity(Capacity.VagueLetale))
        {
            int ennemisTouches = Mathf.Min(2, defenderAllies?.Count ?? 0);
            scoring += ennemisTouches + 1; // bonus plus important
        }

        // Frappe Gelée / Tentation : la cible ne pourra pas attaquer au prochain tour -> bonus stratégique
        if (attacker.HasCapacity(Capacity.FrappeGelee) || attacker.HasCapacity(Capacity.Tentation))
            scoring += 1;

        // Aléatoire (ignore 1 défense et attaque aléatoire) : bonus supplémentaire
        if (attacker.HasCapacity(Capacity.AttaqueAleatoire))
            scoring += 1;
        // ---------------------------
        // 6) Bonus si on peut éliminer la cible (kill)
        // ---------------------------
        if (baseDamage >= defender.GetDefenseValue(defender))
        {
            scoring += 4; // prime importante pour élimination de la cible
        }

        // ---------------------------
        // 7) Risques : capacités de la cible elle-même
        // ---------------------------

        // Attaque de Provocation (Jaycota) : inflige 1 dégât en retour -> pénalité
        if (defender.HasCapacity(Capacity.AttaqueProvocation))
            scoring--;

        // Aubaine (Zarla) : si attaquée, gagne +1 ATK temporaire au prochain combat -> pénalité pour ne pas la booster
        if (defender.HasCapacity(Capacity.Aubaine))
            scoring--;

        // Régénération : cible récupère si elle n'attaque pas, peut réduire l'efficacité -> pénalité optionnelle
        if (defender.HasCapacity(Capacity.Regeneration))
            scoring--;

        // ---------------------------
        // 8) Risques venant des alliés de la cible (effets réactifs ou protections)
        // ---------------------------
        if (defenderAllies != null)
        {
            // Réflexion partielle : un allié inflige 1 dégât de retour si attaqué -> pénalité
            bool anyReflex = defenderAllies.Any(a => a.HasCapacity(Capacity.ReflexionPartielle));
            if (anyReflex)
                scoring--;

            // Protection (Minoson) : chance de transfert des dégâts -> pénalité légère
            bool anyProtection = defenderAllies.Any(a => a.HasCapacity(Capacity.Protection));
            if (anyProtection)
                scoring--;

            // Bouclier Collectif : réduit dégâts si allié adjacent pas attaquant -> pénalité si adjacent
            bool neighbourShield = defenderAllies.Any(a => 
                a.HasCapacity(Capacity.BouclierCollectif) && 
                CarteBoardInteraction.IsAdjacentTo(a, defender) && 
                !a.WillAttackThisTurn);
            if (neighbourShield)
                scoring--;
        }

        // ---------------------------
        // 9) Capacités de l'attaquant qui rendent l'attaque moins souhaitable
        // ---------------------------

        // Régénération : on perd la régénération si on attaque -> pénalité
        if (attacker.HasCapacity(Capacity.Regeneration))
            scoring--;

        // Aura de Force : si Cassandre n'attaque pas, elle booste alliés adjacents -> pénalité si on attaque
        if (attacker.HasCapacity(Capacity.AuraDeForce))
            scoring--;

        // Terreur Sélective : si Trahison n'attaque pas, malus -1 ATK ennemis qui n'attaquent pas -> pénalité
        if (attacker.HasCapacity(Capacity.TerreurSelective))
            scoring--;


        //Debug.Log(scoring);
        // ---------------------------
        // 10) Clamp final, score minimum 0
        // ---------------------------
        if (scoring < 0) scoring = 0;

        return scoring;
    }

    public static int RatePassif(
        CarteBoardInteraction card,
        List<CarteBoardInteraction> allies,
        List<CarteBoardInteraction> opponents)
    {
        if (card == null) return 0;

        int scoring = 0;

        // Agilité Risquée : ne pas attaquer = intouchable (gros bonus)
        if (card.HasCapacity(Capacity.AgiliteRisque))
        {
            scoring += 3; // Très fort bonus à rester passif
        }

        // Aura de Force : boost alliés adjacents si card ne fait rien
        if (card.HasCapacity(Capacity.AuraDeForce))
        {
            // On compte alliés adjacents (gauche/droite)
            int nbAdjacents = 0;
            if (allies != null)
            {
                nbAdjacents = allies.Count(a => CarteBoardInteraction.IsAdjacentTo(card, a));
            }
            scoring += nbAdjacents; // +1 par allié boosté
        }

        // Régénération : récupère PV si ne pas attaquer
        if (card.HasCapacity(Capacity.Regeneration))
        {
            scoring += 2; // Bonus modéré pour régénération passive
        }

        // Terreur Sélective : malus ATK infligé aux ennemis si card ne fait rien
        if (card.HasCapacity(Capacity.TerreurSelective))
        {
            // On compte ennemis n’ayant pas attaqué (approximation)
            int nbOpponentPassifs = 0;
            if (opponents != null)
            {
                nbOpponentPassifs = opponents.Count(e => e.stateOffensif == "passed");
            }
            scoring += nbOpponentPassifs; // Bonus proportionnel au nombre d'ennemis affaiblis
        }

        // Onde de Choc Passive : inflige 1 dégât à un ennemi non-attaquant si ne fait rien
        if (card.HasCapacity(Capacity.OndeDeChocPassive))
        {
            // Bonus fixe car cible aléatoire ennemie (pas trop puissant)
            scoring += 1;
        }
        return scoring;
    }

    public static (bool attack, CarteBoardInteraction target, int score) DecideAction(
        CarteBoardInteraction attacker,
        List<CarteBoardInteraction> allies,
        List<CarteBoardInteraction> opponents)
    {
        int maxAttackScore = 0;
        CarteBoardInteraction bestTarget = null;

        int opponentThreat = EvaluateEnemyThreat(opponents);
        int opponentPassiveBonus = EvaluateEnemyPassiveBonus(opponents);

        foreach (CarteBoardInteraction opponent in opponents)
        {
            int attackScore = RateAttack(attacker, opponent, allies, opponents);
            Debug.Log($"Attaquant: {attacker.nameCard} ennemi: {opponent.nameCard} score attaque: {attackScore}");
            if (attackScore > maxAttackScore)
            {
                maxAttackScore = attackScore;
                bestTarget = opponent;
            }
        }

        int passifScore = RatePassif(attacker, allies, opponents);

        // Ajustement du score passif en fonction de la menace ennemie
        passifScore += (5 - opponentThreat) - opponentPassiveBonus;

        bool shouldAttack = maxAttackScore > passifScore;

        Debug.Log($"PassifScore: {passifScore}, maxAttackScore: {maxAttackScore}, shouldAttack: {shouldAttack}");

        return (shouldAttack, bestTarget, shouldAttack ? maxAttackScore : passifScore);
    }
    
    public static int EvaluateEnemyThreat(List<CarteBoardInteraction> opponents)
    {
        if (opponents == null || opponents.Count == 0) return 0;

        int threatScore = 0;
        foreach (var opponent in opponents)
        {
            if (opponent.HasCapacity(Capacity.FrappePuissante))
                threatScore += 2;

            if (opponent.HasCapacity(Capacity.AttaqueSurprise))
                threatScore += 1;

            if (opponent.HasCapacity(Capacity.VagueLetale))
                threatScore += 2;

            if (opponent.HasCapacity(Capacity.Combo))
                threatScore += 1;

            if (opponent.HasCapacity(Capacity.Tentation))
                threatScore += 1;
        }

        return threatScore;
    }

    // Évalue les bonus passifs des ennemis (plus c’est élevé, plus ils profitent de la passivité)
    public static int EvaluateEnemyPassiveBonus(List<CarteBoardInteraction> opponents)
    {
        if (opponents == null || opponents.Count == 0) return 0;

        int passiveBonusScore = 0;

        foreach (var opponent in opponents)
        {
            if (opponent.HasCapacity(Capacity.AgiliteRisque))
                passiveBonusScore += 3;

            if (opponent.HasCapacity(Capacity.AuraDeForce))
                passiveBonusScore += 2;

            if (opponent.HasCapacity(Capacity.Regeneration))
                passiveBonusScore += 2;

            if (opponent.HasCapacity(Capacity.TerreurSelective))
                passiveBonusScore += 1;

            if (opponent.HasCapacity(Capacity.OndeDeChocPassive))
                passiveBonusScore += 1;
        }

        return passiveBonusScore;
    }
}