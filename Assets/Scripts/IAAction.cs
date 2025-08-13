using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

public static class IAAction
{

    public enum Capacite {
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

    public static List<Capacite> Capacites;

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
        int score = baseDamage;

        // 2) Réduire la DEF si attaque (Ignorance Défensive/Percée Défensive)
        // Percée Défensive / Ignorance Défensive (réduit défense cible) : bonus supplémentaire
        if (attacker.HasCapacite(Capacite.IgnoranceDefensive) || attacker.HasCapacite(Capacite.PerceeDefensive))
            score += 1;  // +1 en plus de réduire la défense

        // ---------------------------
        // 5) Bonus offensifs (capacités de l'attaquant)
        // ---------------------------

        // Frappe Puissante : +1 dégât supplémentaire
        if (attacker.HasCapacite(Capacite.FrappePuissante))
            score += 1;

        // Attaque Surprise : bonus si attaque une cible différente de la précédente
        if (attacker.HasCapacite(Capacite.AttaqueSurprise) && attacker.lastTarget != null && attacker.lastTarget != defender.name)
            score += 1;

        if (attacker.HasCapacite(Capacite.Combo) && attackerAllies != null)
        {
            // Cherche si un allié attaque une cible adjacente à la cible actuelle
            bool allyCombo = attackerAllies.Any(a =>
                a != attacker &&
                a.CurrentTarget != null &&
                CarteBoardInteraction.IsAdjacentTo(defender, a.CurrentTarget)  // <-- ici les 2 arguments
            );
            if (allyCombo)
                score += 1;
        }
        

        // Vague Létale : touche 2 ennemis aléatoires, avantage si plusieurs ennemis
        if (attacker.HasCapacite(Capacite.VagueLetale))
        {
            int ennemisTouches = Mathf.Min(2, defenderAllies?.Count ?? 0);
            score += ennemisTouches + 1; // bonus plus important
        }

        // Frappe Gelée / Tentation : la cible ne pourra pas attaquer au prochain tour -> bonus stratégique
        if (attacker.HasCapacite(Capacite.FrappeGelee) || attacker.HasCapacite(Capacite.Tentation))
            score += 1;

        // Aléatoire (ignore 1 défense et attaque aléatoire) : bonus supplémentaire
        if (attacker.HasCapacite(Capacite.AttaqueAleatoire))
            score += 1;
        // ---------------------------
        // 6) Bonus si on peut éliminer la cible (kill)
        // ---------------------------
        if (baseDamage >= defender.GetDefenseValue(defender))
        {
            score += 4; // prime importante pour élimination de la cible
        }

        // ---------------------------
        // 7) Risques : capacités de la cible elle-même
        // ---------------------------

        // Attaque de Provocation (Jaycota) : inflige 1 dégât en retour -> pénalité
        if (defender.HasCapacite(Capacite.AttaqueProvocation))
            score--;

        // Aubaine (Zarla) : si attaquée, gagne +1 ATK temporaire au prochain combat -> pénalité pour ne pas la booster
        if (defender.HasCapacite(Capacite.Aubaine))
            score--;

        // Régénération : cible récupère si elle n'attaque pas, peut réduire l'efficacité -> pénalité optionnelle
        if (defender.HasCapacite(Capacite.Regeneration))
            score--;

        // ---------------------------
        // 8) Risques venant des alliés de la cible (effets réactifs ou protections)
        // ---------------------------
        if (defenderAllies != null)
        {
            // Réflexion partielle : un allié inflige 1 dégât de retour si attaqué -> pénalité
            bool anyReflex = defenderAllies.Any(a => a.HasCapacite(Capacite.ReflexionPartielle));
            if (anyReflex)
                score--;

            // Protection (Minoson) : chance de transfert des dégâts -> pénalité légère
            bool anyProtection = defenderAllies.Any(a => a.HasCapacite(Capacite.Protection));
            if (anyProtection)
                score--;

            // Bouclier Collectif : réduit dégâts si allié adjacent pas attaquant -> pénalité si adjacent
            bool neighbourShield = defenderAllies.Any(a => 
                a.HasCapacite(Capacite.BouclierCollectif) && 
                CarteBoardInteraction.IsAdjacentTo(a, defender) && 
                !a.WillAttackThisTurn);
            if (neighbourShield)
                score--;
        }

        // ---------------------------
        // 9) Capacités de l'attaquant qui rendent l'attaque moins souhaitable
        // ---------------------------

        // Régénération : on perd la régénération si on attaque -> pénalité
        if (attacker.HasCapacite(Capacite.Regeneration))
            score--;

        // Aura de Force : si Cassandre n'attaque pas, elle booste alliés adjacents -> pénalité si on attaque
        if (attacker.HasCapacite(Capacite.AuraDeForce))
            score--;

        // Terreur Sélective : si Trahison n'attaque pas, malus -1 ATK ennemis qui n'attaquent pas -> pénalité
        if (attacker.HasCapacite(Capacite.TerreurSelective))
            score--;


        Debug.Log(score);
        // ---------------------------
        // 10) Clamp final, score minimum 0
        // ---------------------------
        if (score < 0) score = 0;

        return score;
    }


    public static int RatePassif(
        CarteBoardInteraction card,
        List<CarteBoardInteraction> allies,
        List<CarteBoardInteraction> enemies)
    {
        if (card == null) return 0;

        int score = 0;

        // Agilité Risquée : ne pas attaquer = intouchable (gros bonus)
        if (card.HasCapacite(Capacite.AgiliteRisque))
        {
            score += 3; // Très fort bonus à rester passif
        }

        // Aura de Force : boost alliés adjacents si card ne fait rien
        if (card.HasCapacite(Capacite.AuraDeForce))
        {
            // On compte alliés adjacents (gauche/droite)
            int nbAdjacents = 0;
            if (allies != null)
            {
                nbAdjacents = allies.Count(a => CarteBoardInteraction.IsAdjacentTo(card, a));
            }
            score += nbAdjacents; // +1 par allié boosté
        }

        // Régénération : récupère PV si ne pas attaquer
        if (card.HasCapacite(Capacite.Regeneration))
        {
            score += 2; // Bonus modéré pour régénération passive
        }

        // Terreur Sélective : malus ATK infligé aux ennemis si card ne fait rien
        if (card.HasCapacite(Capacite.TerreurSelective))
        {
            // On compte ennemis n’ayant pas attaqué (approximation)
            int nbEnnemisPassifs = 0;
            if (enemies != null)
            {
                nbEnnemisPassifs = enemies.Count(e => !e.HasAttackedThisTurn);
            }
            score += nbEnnemisPassifs; // Bonus proportionnel au nombre d'ennemis affaiblis
        }

        // Onde de Choc Passive : inflige 1 dégât à un ennemi non-attaquant si ne fait rien
        if (card.HasCapacite(Capacite.OndeDeChocPassive))
        {
            // Bonus fixe car cible aléatoire ennemie (pas trop puissant)
            score += 1;
        }

        // Tu peux ajouter ici d'autres capacités passives similaires...

        return score;
    }

    public static (bool attack, CarteBoardInteraction target, int score) DecideAction(
        CarteBoardInteraction attacker,
        List<CarteBoardInteraction> allies,
        List<CarteBoardInteraction> enemies)
    {
        int maxAttackScore = 0;
        CarteBoardInteraction bestTarget = null;

        int enemyThreat = EvaluateEnemyThreat(enemies);
        int enemyPassiveBonus = EvaluateEnemyPassiveBonus(enemies);

        foreach (var enemy in enemies)
        {
            CarteUI carteAtk = attacker.GetComponent<CarteUI>();
            CarteUI carteEnemy = enemy.GetComponent<CarteUI>();

            int attackScore = RateAttack(attacker, enemy, allies, enemies);
            Debug.Log($"Attaquant: {carteAtk.nomText.text} ennemi: {carteEnemy.nomText.text} score attaque: {attackScore}");
            if (attackScore > maxAttackScore)
            {
                maxAttackScore = attackScore;
                bestTarget = enemy;
            }
        }

        int passifScore = RatePassif(attacker, allies, enemies);

        // Ajustement du score passif en fonction de la menace ennemie
        passifScore += (5 - enemyThreat) - enemyPassiveBonus;

        bool shouldAttack = maxAttackScore > passifScore;

        Debug.Log($"PassifScore: {passifScore}, maxAttackScore: {maxAttackScore}, shouldAttack: {shouldAttack}");

        return (shouldAttack, bestTarget, shouldAttack ? maxAttackScore : passifScore);
    }
    

    public static int EvaluateEnemyThreat(List<CarteBoardInteraction> enemies)
    {
        if (enemies == null || enemies.Count == 0) return 0;

        int threatScore = 0;
        foreach (var enemy in enemies)
        {
            if (enemy.HasCapacite(Capacite.FrappePuissante))
                threatScore += 2;

            if (enemy.HasCapacite(Capacite.AttaqueSurprise))
                threatScore += 1;

            if (enemy.HasCapacite(Capacite.VagueLetale))
                threatScore += 2;

            if (enemy.HasCapacite(Capacite.Combo))
                threatScore += 1;

            if (enemy.HasCapacite(Capacite.Tentation))
                threatScore += 1;

            // Ajoute d'autres critères d'agressivité si nécessaire...
        }

        return threatScore;
    }

    // Évalue les bonus passifs des ennemis (plus c’est élevé, plus ils profitent de la passivité)
    public static int EvaluateEnemyPassiveBonus(List<CarteBoardInteraction> enemies)
    {
        if (enemies == null || enemies.Count == 0) return 0;

        int passiveBonusScore = 0;

        foreach (var enemy in enemies)
        {
            if (enemy.HasCapacite(Capacite.AgiliteRisque))
                passiveBonusScore += 3;

            if (enemy.HasCapacite(Capacite.AuraDeForce))
                passiveBonusScore += 2;

            if (enemy.HasCapacite(Capacite.Regeneration))
                passiveBonusScore += 2;

            if (enemy.HasCapacite(Capacite.TerreurSelective))
                passiveBonusScore += 1;

            if (enemy.HasCapacite(Capacite.OndeDeChocPassive))
                passiveBonusScore += 1;

            // Ajouter autres bonus passifs pertinents...
        }

        return passiveBonusScore;
    }
}