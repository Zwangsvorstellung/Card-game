using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using GameStates;
using Capacity;

/// Calcule les scores d'attaque et de passivité pour aider l'IA à prendre des décisions optimales.
public static class IAAction
{
    /// Évalue le score d'une attaque potentielle de l'attaquant vers le défenseur.
    public static int RateAttack(
        CardAI attacker,
        CardUI defender,
        List<CardAI> attackerAllies,
        List<CardUI> defenderAllies)
    {
        if (attacker == null || defender == null) return 0;

        // score initial basé sur les dégâts potentiels
        int atk = attacker.attaqueValue;
        int def = defender.defenseValue;

        int baseDamage = Mathf.Max(1, atk - def);
        int scoring = baseDamage;

        // BONUS : CAPACITÉS QUI RÉDUISENT LA DÉFENSE
        // ======================================================
        // Ignorance Défensive / Percée Défensive : réduit la défense de la cible
        // Bonus supplémentaire car ces capacités rendent l'attaque plus efficace
        if (attacker.HasCapacity(Capacity.IGNORANCE_DEFENSIVE) || attacker.HasCapacity(Capacity.PERCEE_DEFENSIVE))
            scoring += 1;

        // BONUS OFFENSIFS (capacités de l'attaquant)
        // ======================================================
        // Frappe Puissante : inflige +1 dégât supplémentaire
        if (attacker.HasCapacity(Capacity.FRAPPE_PUISSANTE))
            scoring += 1;

        // Attaque Surprise : bonus si on attaque une cible différente de la précédente
        // Cela encourage la diversité des cibles et rend l'attaque plus imprévisible

        if (attacker.HasCapacity(Capacity.ATTAQUE_SURPRISE) &&
            !string.IsNullOrEmpty(attacker.lastTarget) &&
            attacker.lastTarget != defender.nameCard)
            scoring += 1;

        // Combo : Dégâts aléatoires entre 0 et 4 points
        if (attacker.HasCapacity(Capacity.COMBO))
            scoring += 2;

        // Frappe Gelée / Tentation : la cible ne pourra pas attaquer au prochain tour
        // Bonus stratégique car cela neutralise une menace ennemie
        if (attacker.HasCapacity(Capacity.FRAPPE_GELEE))
            scoring += 1;

        if (attacker.HasCapacity(Capacity.TENTATION))
            scoring -= 1; // perdre le contrôle de blocage rend l’attaque moins intéressante

        // Attaque Aléatoire : ignore 1 défense et attaque aléatoire
        // Bonus supplémentaire car cette capacité est puissante
        if (attacker.HasCapacity(Capacity.ALEATOIRE))
            scoring += 1;

        // PÉNALITÉS : RISQUES DE LA CIBLE
        // ======================================================
        // Vague Létale : Inflige ses dégats si elle est ciblée
        if (defender.HasCapacity(Capacity.VAGUE_LETALE))
        {
            scoring -= 2;
            Debug.Log($"[CIBLAGE] → attaquer Vague Létale est risqué");
        }

        // Attaque de Provocation (Jaycota) : inflige 1 dégât en retour
        // Pénalité car on prend des dégâts en attaquant
        if (defender.HasCapacity(Capacity.PROVOCATION))
            scoring--;

        // Aubaine (Zarla) : si attaquée, gagne +1 ATK temporaire au prochain combat
        // Pénalité car on ne veut pas booster l'ennemi
        if (defender.HasCapacity(Capacity.AUBAINE))
            scoring--;

        // Régénération : la cible récupère des PV si elle n'attaque pas
        // Pénalité optionnelle car cela peut réduire l'efficacité de l'attaque
        if (defender.HasCapacity(Capacity.REGENERATION))
            scoring--;

        // PÉNALITÉS : ALLIÉS DE LA CIBLE
        // ======================================================
        if (defenderAllies != null)
        {
            bool has_REFLEXION_PARTIELLE = defenderAllies.Any(a => a.HasCapacity(Capacity.REFLEXION_PARTIELLE));
            bool has_BOUCLIER_COLLECTIF = defenderAllies.Any(a => a.HasCapacity(Capacity.BOUCLIER_COLLECTIF) &&
                                                             a.IsAdjacentTo(defender) &&
                                                             a.stateOffensif != OffensiveState.ATK);

            // Réflexion partielle : un allié inflige 1 dégât de retour si la cible est attaquée
            // Pénalité car on prend des dégâts supplémentaires
            if (has_REFLEXION_PARTIELLE)
                scoring--;

            // Bouclier Collectif : réduit les dégâts n'attaque pas
            // Pour vérifier si l'allié n'attaque pas, on vérifie son stateOffensif
            if (has_BOUCLIER_COLLECTIF)
                scoring--;
        }

        // PÉNALITÉS : CAPACITÉS DE L'ATTAQUANT
        // ======================================================
        // Certaines capacités de l'attaquant sont plus utiles si on ne fait rien

        // Régénération : on perd la régénération si on attaque
        // Pénalité car on perd un avantage en attaquant
        if (attacker.HasCapacity(Capacity.REGENERATION))
            scoring--;

        // Aura de Force : si Cassandre n'attaque pas, elle booste les alliés adjacents
        // Pénalité si on attaque car on perd l'effet de boost pour les alliés
        if (attacker.HasCapacity(Capacity.AURA_DE_FORCE))
            scoring--;

        // Terreur Sélective : si Trahison n'attaque pas, inflige -1 ATK aux ennemis qui n'attaquent pas
        // Pénalité si on attaque car on perd l'effet de malus sur les ennemis
        if (attacker.HasCapacity(Capacity.TERREUR_SELECTIVE))
            scoring--;

        // BONUS MAJEUR : ÉLIMINATION
        // ======================================================
        if (atk >= defender.defenseValue)
            scoring += 4;

        return Mathf.Max(scoring, 0);
    }

    /// Évalue le score de rester passif (ne pas attaquer) pour une carte.
    /// Plus le score est élevé, plus il est avantageux de ne pas attaquer.
    public static int RatePassif(
        CardAI card,
        List<CardAI> allies,
        List<CardUI> opponents)
    {
        if (card == null) return 0;
        int scoring = 0;

        // Agilité Risquée : ne pas attaquer = intouchable
        // Très fort bonus car la carte devient invulnérable
        if (card.HasCapacity(Capacity.AGILITE_RISQUEE))
            scoring += 3;

        // Aura de Force : boost les alliés adjacents si la carte ne fait rien
        // Le bonus est proportionnel au nombre d'alliés boostés
        if (card.HasCapacity(Capacity.AURA_DE_FORCE))
        {
            // Compte les alliés adjacents (gauche/droite sur le plateau)
            int nbAdjacents = 0;
            if (allies != null)
            {
                nbAdjacents = allies.Count(a => a.IsAdjacentTo(card));
            }
            scoring += nbAdjacents; // +1 par allié boosté
        }
        // annule une attaque - gros gain
        if (card.HasCapacity(Capacity.TENTATION))
            scoring += 2;
        
        // Régénération : récupère des PV si on ne fait rien
        // Bonus modéré car la survie est importante
        if (card.HasCapacity(Capacity.REGENERATION))
            scoring += 2;

        // Terreur Sélective : inflige un malus ATK aux ennemis qui n'attaquent pas
        // Le bonus est proportionnel au nombre d'ennemis affectés
        if (card.HasCapacity(Capacity.TERREUR_SELECTIVE))
        {
            // Compte les ennemis qui n'ont pas attaqué (état "passed")
            int nbOpponentPassifs = 0;
            if (opponents != null)
            {
                nbOpponentPassifs = opponents.Count(e => e.stateOffensif == OffensiveState.PASSED);
            }
            scoring += nbOpponentPassifs;
        }
        // Onde de Choc Passive : inflige 1 dégât à un ennemi non-attaquant
        // Bonus fixe car la cible est aléatoire (pas trop puissant)
        if (card.HasCapacity(Capacity.ONDE_DE_CHOC_PASSIVE))
            scoring += 1;

        return scoring;
    }

    /// Compare les scores d'attaque et de passivité pour prendre la meilleure décision.
    /// <param name="attacker">La carte qui doit décider de son action (CardAI)</param>
    /// <param name="allies">Liste des alliés de la carte</param>
    /// <param name="opponents">Liste des ennemis disponibles comme cibles</param>
    /// <returns>Un tuple contenant : (bool attack, CardUI target, int score)
    /// - attack : true si la carte doit attaquer, false si elle doit rester passive
    /// - target : la meilleure cible si attack=true, null sinon
    /// - score : le score de l'action choisie</returns>
    public static (bool attack, CardUI target, int score) DecideAction(
        CardAI attacker,
        List<CardAI> allies,
        List<CardUI> opponents)
    {
        int maxAttackScore = 0;
        CardUI bestTarget = null;

        // Évalue la menace globale des ennemis et leurs bonus passifs
        int opponentThreat = EvaluateEnemyThreat(opponents);
        int opponentPassiveBonus = EvaluateEnemyPassiveBonus(opponents);

        // Parcourt tous les ennemis pour trouver la meilleure cible
        foreach (CardUI opponent in opponents)
        {
            int attackScore = RateAttack(attacker, opponent, allies, opponents);
            //Debug.Log($"Attaquant: {attacker.nameCard} ennemi: {opponent.nameCard} score attaque: {attackScore}");
            
            // Garde la cible avec le meilleur score
            if (attackScore > maxAttackScore + 1)
            {
                maxAttackScore = attackScore;
                bestTarget = opponent;
            }
        }

        // Calcule le score de rester passif
        int passifScore = RatePassif(attacker, allies, opponents);
        // Ajuste le score passif en fonction de la situation - (valeur réduite pour éviter que l'IA reste trop passive)
        passifScore += Mathf.Max(-2, (2 - opponentThreat) - opponentPassiveBonus);
        // Décide : attaquer si le score d'attaque est au moins proche du passif (léger biais agressif)
        bool shouldAttack = maxAttackScore >= passifScore;

        return (shouldAttack, bestTarget, shouldAttack ? maxAttackScore : passifScore);
    }
    
    /// Évalue la menace représentée par les ennemis. - Plus le score est élevé, plus les ennemis sont dangereux.
    public static int EvaluateEnemyThreat(List<CardUI> opponents)
    {
        if (opponents == null || opponents.Count == 0) return 0;

        int score = 0;
        foreach (var opponent in opponents)
        {
            // Capacités offensives qui augmentent la menace
            if (opponent.HasCapacity(Capacity.FRAPPE_PUISSANTE))
                score += 2; // Très dangereux

            if (opponent.HasCapacity(Capacity.ATTAQUE_SURPRISE))
                score += 1; // Imprévisible

            if (opponent.HasCapacity(Capacity.VAGUE_LETALE))
                score += 2; // Peut toucher plusieurs cibles

            if (opponent.HasCapacity(Capacity.COMBO))
                score += 1; // Synergie avec les alliés

            if (opponent.HasCapacity(Capacity.TENTATION))
                score += 1; // Peut bloquer nos attaques
        }
        return score;
    }

    /// Évalue les bonus passifs des ennemis. - Plus le score est élevé, plus les ennemis profitent de rester passifs.
    public static int EvaluateEnemyPassiveBonus(List<CardUI> opponents)
    {
        if (opponents == null || opponents.Count == 0) return 0;

        int score = 0;
        foreach (var opponent in opponents)
        {
            // Capacités qui donnent des avantages quand l'ennemi reste passif
            if (opponent.HasCapacity(Capacity.AGILITE_RISQUEE))
                score += 3; // Devient intouchable

            if (opponent.HasCapacity(Capacity.AURA_DE_FORCE))
                score += 2; // Boost ses alliés

            if (opponent.HasCapacity(Capacity.REGENERATION))
                score += 2; // Récupère des PV

            if (opponent.HasCapacity(Capacity.TERREUR_SELECTIVE))
                score += 1; // Affaiblit nos cartes passives

            if (opponent.HasCapacity(Capacity.ONDE_DE_CHOC_PASSIVE))
                score += 1; // Inflige des dégâts passifs
        }
        return score;
    }
}