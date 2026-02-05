using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Classe statique gérant l'évaluation des actions de l'IA pour les cartes.
/// Calcule les scores d'attaque et de passivité pour aider l'IA à prendre des décisions optimales.
/// </summary>
public static class IAAction
{
    /// <summary>
    /// Enumération de toutes les capacités spéciales que peuvent posséder les cartes.
    /// </summary>
    public enum Capacity {
        AgiliteRisque,          // Ne pas attaquer = intouchable
        FrappeGelee,            // La cible ne peut pas attaquer au prochain tour
        AuraDeForce,            // Boost alliés adjacents si ne fait rien
        ReflexionPartielle,     // Inflige 1 dégât de retour si attaqué
        FrappePuissante,        // +1 dégât supplémentaire
        Tentation,              // La cible ne peut pas attaquer au prochain tour
        MalusAttaque,           // Réduit l'attaque de l'ennemi
        AttaqueProvocation,     // Inflige 1 dégât en retour (Jaycota)
        AttaqueSurprise,        // Bonus si attaque une cible différente
        PerceeDefensive,        // Réduit la défense de la cible
        Regeneration,           // Récupère des PV si ne fait rien
        BouclierCollectif,      // Réduit dégâts si allié adjacent pas attaquant
        TerreurSelective,       // Malus ATK ennemis qui n'attaquent pas
        IgnoranceDefensive,     // Ignore une partie de la défense
        OndeDeChocPassive,      // Inflige 1 dégât à un ennemi non-attaquant
        Aubaine,                // Gagne +1 ATK temporaire si attaquée (Zarla)
        AttaqueAleatoire,       // Ignore 1 défense et attaque aléatoire
        VagueLetale,            // Touche 2 ennemis aléatoires
        Combo,                  // Bonus si allié attaque cible adjacente
        Protection              // Chance de transfert des dégâts (Minoson)
    }

    public static List<Capacity> Capacites;

    /// Évalue le score d'une attaque potentielle de l'attaquant vers le défenseur.
    /// Plus le score est élevé, plus l'attaque est avantageuse.
    /// <param name="attacker">La carte qui envisagerait d'attaquer (CardAI)</param>
    /// <param name="defender">La carte visée par l'attaque (CardUI)</param>
    /// <param name="attackerAllies">Liste des alliés de l'attaquant (autres cartes du même camp)</param>
    /// <param name="defenderAllies">Liste des alliés du défenseur (autres cartes du camp adverse)</param>
    /// <returns>Un score entier représentant la valeur de cette attaque (0 = minimum)</returns>
    public static int RateAttack(
        CardAI attacker,
        CardUI defender,
        List<CardAI> attackerAllies,
        List<CardUI> defenderAllies)
    {
        // ==========================================
        // 0) VÉRIFICATIONS PRÉLIMINAIRES
        // ==========================================
        if (attacker == null || defender == null) return 0;

        // ==========================================
        // 1) CALCUL DES DÉGÂTS DE BASE
        // ==========================================
        // Récupération des valeurs d'attaque et de défense
        int atk = attacker.attaqueValue;
        int def = defender.defenseValue;

        // Calcul des dégâts nets (attaque - défense)
        // Si les dégâts sont négatifs, on fixe à 1 (minimum de dégâts)
        int baseDamage = atk - def;
        if (baseDamage < 0)
            baseDamage = 1;

        // Le score initial est basé sur les dégâts potentiels
        int scoring = baseDamage;

        // ==========================================
        // 2) BONUS : CAPACITÉS QUI RÉDUISENT LA DÉFENSE
        // ==========================================
        // Ignorance Défensive / Percée Défensive : réduit la défense de la cible
        // Bonus supplémentaire car ces capacités rendent l'attaque plus efficace
        if (attacker.HasCapacity(Capacity.IgnoranceDefensive) || attacker.HasCapacity(Capacity.PerceeDefensive))
            scoring += 1;

        // ==========================================
        // 3) BONUS OFFENSIFS (capacités de l'attaquant)
        // ==========================================

        // Frappe Puissante : inflige +1 dégât supplémentaire
        if (attacker.HasCapacity(Capacity.FrappePuissante))
            scoring += 1;

        // Attaque Surprise : bonus si on attaque une cible différente de la précédente
        // Cela encourage la diversité des cibles et rend l'attaque plus imprévisible
        if (attacker.HasCapacity(Capacity.AttaqueSurprise) && !string.IsNullOrEmpty(attacker.lastTarget) && attacker.lastTarget != defender.nameCard)
            scoring += 1;

        // Combo : bonus si un allié attaque une cible adjacente à la cible actuelle
        // Cela crée une synergie entre les attaques des alliés
        if (attacker.HasCapacity(Capacity.Combo) && attackerAllies != null && defenderAllies != null)
        {
            // Parcourt tous les alliés pour vérifier s'ils attaquent une cible adjacente
            // Note : a.target est un string (nom de la cible), il faut trouver le CardUI correspondant
            bool allyCombo = attackerAllies.Any(a =>
            {
                // Ignore l'attaquant lui-même et les alliés sans cible
                if (a == attacker || string.IsNullOrEmpty(a.target))
                    return false;
                
                // Trouve le CardUI correspondant au nom de la cible dans la liste des ennemis
                CardUI allyTarget = defenderAllies.FirstOrDefault(opp => opp.nameCard == a.target);
                
                // Si l'allié a une cible et qu'elle est adjacente à la cible actuelle, combo activé
                return allyTarget != null && defender.IsAdjacentTo(defender, allyTarget);
            });
            
            if (allyCombo)
                scoring += 1;
        }

        // Vague Létale : touche jusqu'à 2 ennemis aléatoires
        // Plus il y a d'ennemis, plus le bonus est important
        if (attacker.HasCapacity(Capacity.VagueLetale))
        {
            int ennemisTouches = Mathf.Min(2, defenderAllies?.Count ?? 0);
            scoring += ennemisTouches + 1; // Bonus proportionnel au nombre d'ennemis touchés
        }

        // Frappe Gelée / Tentation : la cible ne pourra pas attaquer au prochain tour
        // Bonus stratégique car cela neutralise une menace ennemie
        if (attacker.HasCapacity(Capacity.FrappeGelee) || attacker.HasCapacity(Capacity.Tentation))
            scoring += 1;

        // Attaque Aléatoire : ignore 1 défense et attaque aléatoire
        // Bonus supplémentaire car cette capacité est puissante
        if (attacker.HasCapacity(Capacity.AttaqueAleatoire))
            scoring += 1;
        // ==========================================
        // 4) BONUS MAJEUR : ÉLIMINATION DE LA CIBLE (KILL)
        // ==========================================
        // Si l'attaque totale est supérieure ou égale à la défense totale, on peut tuer la cible
        // C'est un objectif prioritaire, donc bonus important
        if (atk >= defender.defenseValue)
        {
            scoring += 4; // Prime importante pour élimination de la cible
        }

        // ==========================================
        // 5) PÉNALITÉS : RISQUES DE LA CIBLE
        // ==========================================
        // Ces capacités de la cible rendent l'attaque moins souhaitable

        // Attaque de Provocation (Jaycota) : inflige 1 dégât en retour
        // Pénalité car on prend des dégâts en attaquant
        if (defender.HasCapacity(Capacity.AttaqueProvocation))
            scoring--;

        // Aubaine (Zarla) : si attaquée, gagne +1 ATK temporaire au prochain combat
        // Pénalité car on ne veut pas booster l'ennemi
        if (defender.HasCapacity(Capacity.Aubaine))
            scoring--;

        // Régénération : la cible récupère des PV si elle n'attaque pas
        // Pénalité optionnelle car cela peut réduire l'efficacité de l'attaque
        if (defender.HasCapacity(Capacity.Regeneration))
            scoring--;

        // ==========================================
        // 6) PÉNALITÉS : RISQUES DES ALLIÉS DE LA CIBLE
        // ==========================================
        // Les alliés de la cible peuvent avoir des capacités défensives qui rendent l'attaque risquée
        if (defenderAllies != null)
        {
            // Réflexion partielle : un allié inflige 1 dégât de retour si la cible est attaquée
            // Pénalité car on prend des dégâts supplémentaires
            bool anyReflex = defenderAllies.Any(a => a.HasCapacity(Capacity.ReflexionPartielle));
            if (anyReflex)
                scoring--;

            // Protection (Minoson) : chance de transfert des dégâts vers un autre allié
            // Pénalité légère car cela peut réduire l'efficacité de l'attaque
            bool anyProtection = defenderAllies.Any(a => a.HasCapacity(Capacity.Protection));
            if (anyProtection)
                scoring--;

            // Bouclier Collectif : réduit les dégâts si un allié adjacent n'attaque pas
            // Vérifie si un allié avec Bouclier Collectif est adjacent et ne va pas attaquer
            // Note : a et defender sont des CardUI, on utilise la méthode IsAdjacentTo de CardUI
            // Pour vérifier si l'allié n'attaque pas, on vérifie son stateOffensif
            bool neighbourShield = defenderAllies.Any(a => 
                a.HasCapacity(Capacity.BouclierCollectif) && 
                defender.IsAdjacentTo(a, defender) && 
                a.stateOffensif != "atk");
            if (neighbourShield)
                scoring--;
        }

        // ==========================================
        // 7) PÉNALITÉS : CAPACITÉS DE L'ATTAQUANT QUI RENDENT L'ATTAQUE MOINS SOUHAITABLE
        // ==========================================
        // Certaines capacités de l'attaquant sont plus utiles si on ne fait rien

        // Régénération : on perd la régénération si on attaque
        // Pénalité car on perd un avantage en attaquant
        if (attacker.HasCapacity(Capacity.Regeneration))
            scoring--;

        // Aura de Force : si Cassandre n'attaque pas, elle booste les alliés adjacents
        // Pénalité si on attaque car on perd l'effet de boost pour les alliés
        if (attacker.HasCapacity(Capacity.AuraDeForce))
            scoring--;

        // Terreur Sélective : si Trahison n'attaque pas, inflige -1 ATK aux ennemis qui n'attaquent pas
        // Pénalité si on attaque car on perd l'effet de malus sur les ennemis
        if (attacker.HasCapacity(Capacity.TerreurSelective))
            scoring--;


        // ==========================================
        // 8) FINALISATION DU SCORE
        // ==========================================
        Debug.Log($"[IA] Score d'attaque de {attacker.nameCard} vers {defender.nameCard}: {scoring}");
        
        // Le score ne peut pas être négatif (clamp à 0)
        if (scoring < 0) scoring = 0;

        return scoring;
    }

    /// Évalue le score de rester passif (ne pas attaquer) pour une carte.
    /// Plus le score est élevé, plus il est avantageux de ne pas attaquer.
    /// <param name="card">La carte qui envisagerait de rester passive (CardAI)</param>
    /// <param name="allies">Liste des alliés de la carte</param>
    /// <param name="opponents">Liste des ennemis</param>
    /// <returns>Un score entier représentant la valeur de rester passif</returns>
    public static int RatePassif(
        CardAI card,
        List<CardAI> allies,
        List<CardUI> opponents)
    {
        if (card == null) return 0;

        int scoring = 0;

        // Agilité Risquée : ne pas attaquer = intouchable
        // Très fort bonus car la carte devient invulnérable
        if (card.HasCapacity(Capacity.AgiliteRisque))
        {
            scoring += 3; // Très fort bonus à rester passif
        }

        // Aura de Force : boost les alliés adjacents si la carte ne fait rien
        // Le bonus est proportionnel au nombre d'alliés boostés
        if (card.HasCapacity(Capacity.AuraDeForce))
        {
            // Compte les alliés adjacents (gauche/droite sur le plateau)
            int nbAdjacents = 0;
            if (allies != null)
            {
                nbAdjacents = allies.Count(a => card.IsAdjacentTo(card, a));
            }
            scoring += nbAdjacents; // +1 par allié boosté
        }

        // Régénération : récupère des PV si on ne fait rien
        // Bonus modéré car la survie est importante
        if (card.HasCapacity(Capacity.Regeneration))
        {
            scoring += 2; // Bonus modéré pour régénération passive
        }

        // Terreur Sélective : inflige un malus ATK aux ennemis qui n'attaquent pas
        // Le bonus est proportionnel au nombre d'ennemis affectés
        if (card.HasCapacity(Capacity.TerreurSelective))
        {
            // Compte les ennemis qui n'ont pas attaqué (état "passed")
            int nbOpponentPassifs = 0;
            if (opponents != null)
            {
                nbOpponentPassifs = opponents.Count(e => e.stateOffensif == "passed");
            }
            scoring += nbOpponentPassifs; // Bonus proportionnel au nombre d'ennemis affaiblis
        }

        // Onde de Choc Passive : inflige 1 dégât à un ennemi non-attaquant
        // Bonus fixe car la cible est aléatoire (pas trop puissant)
        if (card.HasCapacity(Capacity.OndeDeChocPassive))
        {
            scoring += 1;
        }
        
        return scoring;
    }

    /// Décide de l'action optimale pour une carte : attaquer ou rester passif.
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

        // Parcourt tous les ennemis pour trouver la meilleure cible d'attaque
        foreach (CardUI opponent in opponents)
        {
            int attackScore = RateAttack(attacker, opponent, allies, opponents);
            Debug.Log($"Attaquant: {attacker.nameCard} ennemi: {opponent.nameCard} score attaque: {attackScore}");
            
            // Garde la cible avec le meilleur score
            if (attackScore > maxAttackScore)
            {
                maxAttackScore = attackScore;
                bestTarget = opponent;
            }
        }

        // Calcule le score de rester passif
        int passifScore = RatePassif(attacker, allies, opponents);

        // Ajuste le score passif en fonction de la situation :
        // - Si les ennemis sont peu menaçants, rester passif est plus avantageux
        // - Si les ennemis ont de forts bonus passifs, il vaut mieux les attaquer
        passifScore += (5 - opponentThreat) - opponentPassiveBonus;

        // Décide : attaquer si le score d'attaque est supérieur au score passif
        bool shouldAttack = maxAttackScore > passifScore;

        return (shouldAttack, bestTarget, shouldAttack ? maxAttackScore : passifScore);
    }
    
    /// Évalue la menace globale représentée par les ennemis.
    /// Plus le score est élevé, plus les ennemis sont dangereux.
    /// <param name="opponents">Liste des cartes ennemies</param>
    /// <returns>Un score de menace (0 = aucune menace)</returns>
    public static int EvaluateEnemyThreat(List<CardUI> opponents)
    {
        if (opponents == null || opponents.Count == 0) return 0;

        int threatScore = 0;
        foreach (var opponent in opponents)
        {
            // Capacités offensives qui augmentent la menace
            if (opponent.HasCapacity(Capacity.FrappePuissante))
                threatScore += 2; // Très dangereux

            if (opponent.HasCapacity(Capacity.AttaqueSurprise))
                threatScore += 1; // Imprévisible

            if (opponent.HasCapacity(Capacity.VagueLetale))
                threatScore += 2; // Peut toucher plusieurs cibles

            if (opponent.HasCapacity(Capacity.Combo))
                threatScore += 1; // Synergie avec les alliés

            if (opponent.HasCapacity(Capacity.Tentation))
                threatScore += 1; // Peut bloquer nos attaques
        }

        return threatScore;
    }

    /// Évalue les bonus passifs des ennemis.
    /// Plus le score est élevé, plus les ennemis profitent de rester passifs.
    /// Cela incite à les attaquer plutôt que de rester passif soi-même.
    /// <param name="opponents">Liste des cartes ennemies</param>
    /// <returns>Un score de bonus passif (0 = aucun bonus)</returns>
    public static int EvaluateEnemyPassiveBonus(List<CardUI> opponents)
    {
        if (opponents == null || opponents.Count == 0) return 0;

        int passiveBonusScore = 0;

        foreach (var opponent in opponents)
        {
            // Capacités qui donnent des avantages quand l'ennemi reste passif
            if (opponent.HasCapacity(Capacity.AgiliteRisque))
                passiveBonusScore += 3; // Devient intouchable

            if (opponent.HasCapacity(Capacity.AuraDeForce))
                passiveBonusScore += 2; // Boost ses alliés

            if (opponent.HasCapacity(Capacity.Regeneration))
                passiveBonusScore += 2; // Récupère des PV

            if (opponent.HasCapacity(Capacity.TerreurSelective))
                passiveBonusScore += 1; // Affaiblit nos cartes passives

            if (opponent.HasCapacity(Capacity.OndeDeChocPassive))
                passiveBonusScore += 1; // Inflige des dégâts passifs
        }

        return passiveBonusScore;
    }
}