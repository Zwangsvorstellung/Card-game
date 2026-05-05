# Cycle de Vie de l'Application - Documentation Complète

## 📋 Table des Matières
1. [Initialisation de l'Application](#1-initialisation-de-lapplication)
2. [Phase de Sélection du Deck](#2-phase-de-sélection-du-deck)
3. [Phase de Combat - Début de Tour](#3-phase-de-combat---début-de-tour)
4. [Phase de Combat - Tour du Joueur](#4-phase-de-combat---tour-du-joueur)
5. [Phase de Combat - Tour de l'IA](#5-phase-de-combat---tour-de-lia)
6. [Phase de Combat - Application des Attaques](#6-phase-de-combat---application-des-attaques)
7. [Fin de Tour et Transition](#7-fin-de-tour-et-transition)
8. [Fichiers Clés et Responsabilités](#8-fichiers-clés-et-responsabilités)

---

## 1. Initialisation de l'Application

### 1.1 Démarrage Unity
```
Unity Start → GameManager.Start()
```

**Fichier** : `Assets/Scripts/Managers/GameManager.cs`

**Actions** :
- Initialise `round = 1`
- Définit `mode = "selectDeck"`
- Charge toutes les cartes depuis `Resources/CartesGenerees`
- Crée 2 decks (PlayerA et PlayerB) avec toutes les cartes
- Mélange les decks (`Shuffle()`)
- Distribue 7 cartes dans `mainPlayerA` et `mainPlayerB`
- Met le reste dans `piochePlayerA` et `piochePlayerB`
- Affiche la main du joueur : `mainUIManager.ShowHand(mainPlayerA.ToList())`

**État** : `mode = "selectDeck"`

---

## 2. Phase de Sélection du Deck

### 2.1 Sélection des Cartes par le Joueur
```
Joueur clique sur des cartes → CardMain.SelectCardMain()
```

**Fichier** : `Assets/Scripts/UI/Panels/CardMain.cs`

**Actions** :
- Le joueur sélectionne jusqu'à 4 cartes (`MAX_CARTES_TAPIS = 4`)
- Chaque carte sélectionnée a `isSelect = true`
- Le bouton de validation apparaît quand 4 cartes sont sélectionnées

**État** : `mode = "selectDeck"`

### 2.2 Validation du Deck
```
Joueur clique sur "Valider" → PlayerActionManager.ConfirmSelection()
```

**Fichier** : `Assets/Scripts/Managers/PlayerActionManager.cs`

**Actions** :
- Récupère les cartes sélectionnées : `GameManager.GetSelectedCards()`
- Met les cartes sélectionnées dans `mainPlayerA`
- Met les cartes non sélectionnées dans `piochePlayerA`
- Génère 4 cartes aléatoires pour l'adversaire depuis `mainPlayerB`
- Appelle `BoardManager.SetupBoardCards(opponentCards, selectedCards)`
- Change la vue vers le plateau : `CamController.GoToBoardView()`
- Change le mode : `GameManager.Instance.mode = "selectCardToPlayAction"`
- Démarre le tour : `GameManager.Instance.StartTurn()`

**État** : `mode = "selectCardToPlayAction"`

---

## 3. Phase de Combat - Début de Tour

### 3.1 Initialisation du Tour
```
GameManager.StartTurn()
```

**Fichier** : `Assets/Scripts/Managers/GameManager.cs`

**Actions** :
- Réinitialise les compteurs : `numberOfAttacksUsedPlayer = 0`, `numberOfAttacksUsedIA = 0`
- Détermine qui commence :
  - **Chaque tour** : Choix aléatoire (`Random.Range(0, 2) == 0`)
  - Cela rend le jeu plus imprévisible et équitable

**Si le joueur commence** :
- `mode = "selectCardToPlayAction"`
- Le joueur peut commencer à jouer

**Si l'IA commence** :
- Appelle `IA.Instance.StartAITurn()`
- L'IA joue automatiquement

**État** : Dépend de qui commence

---

## 4. Phase de Combat - Tour du Joueur

### 4.1 Sélection d'une Carte
```
Joueur clique sur une carte → CardUI.OnPointerClick()
→ PlayerActionManager.ClickOnBoardCard()
→ BoardManager.selectCardOnBoard()
→ CardUI.selectCard()
```

**Fichiers** :
- `Assets/Scripts/UI/Panels/CardUI.cs`
- `Assets/Scripts/Managers/PlayerActionManager.cs`
- `Assets/Scripts/Managers/BoardManager.cs`

**Actions** :
- Désélectionne toutes les autres cartes : `BoardManager.DeselectAllOtherCards()`
- Sélectionne la carte : `CardUI.Select()`
- Affiche les boutons d'action : `CardUI.ShowActionButtons()` (Attaquer/Passer)
- Change le mode : `mode = "hasCardSelectedToAction"`
- Met à jour l'état : `stateOffensif = "waitOrder"`

**État** : `mode = "hasCardSelectedToAction"`

### 4.2 Choix d'Action : Attaquer
```
Joueur clique sur "Attaquer" → PlayerActionManager.ClickOnAttack()
→ CardUI.OnAttack()
```

**Fichiers** :
- `Assets/Scripts/Managers/PlayerActionManager.cs`
- `Assets/Scripts/UI/Panels/CardUI.cs`

**Actions** :
- Cache les boutons d'action : `HideActionButtons()`
- Active l'icône d'attaque : `atk.SetActive(true)`
- Met à jour l'état : `stateOffensif = "selectTarget"`
- Marque la carte : `actionChoiceDo = true`
- Incrémente le compteur : `GameManager.Instance.numberOfAttacksUsedPlayer++`
- Change le mode : `mode = "selectCardOpponentToAttack"`

**État** : `mode = "selectCardOpponentToAttack"`

### 4.3 Sélection de la Cible
```
Joueur clique sur une carte ennemie → CardAI.OnPointerClick()
→ PlayerActionManager.ClickSelectTargetOnBoard()
→ BoardManager.selectCardOpponentOnBoard()
→ CardAI.isSelectCard()
→ CardUI.SetDataTarget()
```

**Fichiers** :
- `Assets/Scripts/UI/Panels/CardAI.cs`
- `Assets/Scripts/Managers/PlayerActionManager.cs`
- `Assets/Scripts/Managers/BoardManager.cs`
- `Assets/Scripts/UI/Panels/CardUI.cs`

**Actions** :
- La carte ennemie est sélectionnée : `CardAI.isSelectCard()`
- Met à jour la cible : `CardUI.SetDataTarget(cardAI)`
  - `target = cardAI.nameCard`
  - `targetID = cardAI.idCard`
- Change le mode : `mode = "selectCardToPlayAction"`

**État** : `mode = "selectCardToPlayAction"`

**Note** : L'attaque est **stockée** mais **pas encore appliquée**. Les dégâts seront appliqués à la fin du tour.

### 4.4 Choix d'Action : Passer
```
Joueur clique sur "Passer" → PlayerActionManager.ClickOnPassed()
→ CardUI.OnPassed()
```

**Fichiers** :
- `Assets/Scripts/Managers/PlayerActionManager.cs`
- `Assets/Scripts/UI/Panels/CardUI.cs`

**Actions** :
- Cache les boutons d'action : `HideActionButtons()`
- Active l'icône "passé" : `passedIcon.SetActive(true)`
- Met à jour l'état : `stateOffensif = "passed"`
- Marque la carte : `actionChoiceDo = true`
- Assombrit la carte : `imageCarte.color = new Color(0.4f, 0.4f, 0.4f, 1f)`
- Change le mode : `mode = "selectCardToPlayAction"`

**État** : `mode = "selectCardToPlayAction"`

### 4.5 Vérification de la Fin du Tour du Joueur
```
BoardManager.Update() → Vérifie si toutes les cartes ont fait leur choix
```

**Fichier** : `Assets/Scripts/Managers/BoardManager.cs`

**Condition** :
```csharp
if (cardsOnBoardUI.All(card => card.actionChoiceDo))
{
    GameManager.Instance.isEndturnPlayer = true;
}
```

**Actions** :
- Si toutes les cartes ont fait leur choix → `isEndturnPlayer = true`
- Si l'IA est active → Appelle `BoardManager.MarkEndOfTurn()`

**État** : `isEndturnPlayer = true`

---

## 5. Phase de Combat - Tour de l'IA

### 5.1 Démarrage du Tour de l'IA
```
BoardManager.MarkEndOfTurn() → StartAI() → IA.StartAITurnCoroutine()
→ IA.StartAITurn() → IA.ExecuteAITurn()
```

**Fichiers** :
- `Assets/Scripts/Managers/BoardManager.cs`
- `Assets/Scripts/Gameplay/IA.cs`

**Actions** :
- Vérifie qu'il y a des cartes IA : `BoardManager.cardsOnBoardAI.Count > 0`
- Crée des copies des listes de cartes
- Entre dans la boucle principale d'exécution

**État** : Tour de l'IA en cours

### 5.2 Évaluation des Actions pour Chaque Carte IA
```
Pour chaque CardAI → IAAction.DecideAction()
```

**Fichier** : `Assets/Scripts/IAAction.cs`

**Processus** :
1. **Pour chaque ennemi (CardUI)** :
   - Calcule le score d'attaque : `IAAction.RateAttack(attacker, opponent, allies, opponents)`
   - Évalue les dégâts potentiels, bonus, pénalités, capacités spéciales
   - Garde la meilleure cible

2. **Calcule le score passif** :
   - `IAAction.RatePassif(attacker, allies, opponents)`
   - Évalue les avantages de ne pas attaquer (régénération, auras, etc.)

3. **Décide** :
   - Compare `maxAttackScore` vs `passifScore`
   - Retourne `(shouldAttack, bestTarget, score)`

**Résultat** : Décision pour chaque carte IA

### 5.3 Exécution des Actions de l'IA
```
IA.ExecuteAITurn() → Boucle principale
```

**Fichier** : `Assets/Scripts/Gameplay/IA.cs`

**Processus** :
1. **Trouve la meilleure action globale** :
   - Parcourt toutes les cartes IA disponibles
   - Pour chaque carte, récupère la décision : `IAAction.DecideAction()`
   - Garde la meilleure attaque (score > seuil minimum)

2. **Exécute l'attaque** :
   - `IA.SaveAttack(bestAttacker, bestTarget)`
   - `IA.SimulateAIAttack()` : Met à jour les états
   - `IA.ApplyAttack()` : Met à jour l'UI et stocke l'attaque
   - Stocke dans `aiAttacksThisTurn` (liste des attaques IA)

3. **Passe les autres cartes** :
   - Pour les cartes restantes qui n'ont pas attaqué
   - `IA.ExecutePass(cardAI)` : Met à jour l'état "passed"

4. **Vérifie la fin du tour** :
   - Si `isEndturnPlayer && isEndturnAI` → Appelle `ApplyAllAttacksInRandomOrder()`

**État** : `isEndturnAI = true` (quand toutes les cartes IA ont fait leur choix)

---

## 6. Phase de Combat - Application des Attaques

### 6.1 Application Aléatoire des Attaques
```
IA.ApplyAllAttacksInRandomOrder()
```

**Fichier** : `Assets/Scripts/Gameplay/IA.cs`

**Processus** :
1. **Récupère les attaques** :
   - Attaques du joueur : `GetPlayerAttacks()` (depuis `CardUI` avec `stateOffensif == "selectTarget"`)
   - Attaques de l'IA : `aiAttacksThisTurn` (liste stockée pendant le tour)

2. **Détermine l'ordre aléatoire** :
   ```csharp
   bool playerStarts = Random.Range(0, 2) == 0;
   ```
   - **Si joueur commence** : Attaques joueur → Attaques IA
   - **Si IA commence** : Attaques IA → Attaques joueur

3. **Applique chaque attaque** :
   - `ApplySingleAttack(attack)` pour chaque attaque dans l'ordre
   - Calcule les dégâts : `damage = attacker.attaqueValue - target.defenseValue`
   - Applique les dégâts : `target.defenseValue -= damage`
   - Met à jour l'UI : `target.defenseText.SetText(newDefense.ToString())`
   - Si défense <= 0 → Carte éliminée

4. **Nettoie** :
   - Vide `aiAttacksThisTurn`
   - Nettoie les attaques du joueur

**Résultat** : Tous les dégâts sont appliqués dans un ordre aléatoire

---

## 7. Fin de Tour et Transition

### 7.1 Vérification de la Fin du Tour
```
BoardManager.Update() → Vérifie les états
```

**Fichier** : `Assets/Scripts/Managers/BoardManager.cs`

**Conditions** :
- `isEndturnPlayer = true` : Toutes les cartes joueur ont fait leur choix
- `isEndturnAI = true` : Toutes les cartes IA ont fait leur choix

**Quand les deux sont vrais** :
- Les attaques sont appliquées (voir section 6)
- Le tour se termine

### 7.2 Passage au Tour Suivant
```
GameManager.EndTurn() → GameManager.StartTurn()
```

**Fichier** : `Assets/Scripts/Managers/GameManager.cs`

**Actions** :
- Incrémente le round : `round++`
- Détermine qui commence le prochain tour :
  - **Choix aléatoire** : `playerStarts = Random.Range(0, 2) == 0`
  - Chaque tour est indépendant (pas d'alternance fixe)
- Réinitialise les compteurs
- Démarre le nouveau tour : `StartTurn()`

**État** : Nouveau tour commence

---

## 8. Fichiers Clés et Responsabilités

### 8.1 Gestion du Jeu
| Fichier | Responsabilité |
|---------|----------------|
| `GameManager.cs` | Gestion globale du jeu, tours, scores, decks |
| `BoardManager.cs` | Gestion du plateau, cartes, fin de tour |
| `PlayerActionManager.cs` | Gestion des interactions joueur (clics, boutons) |

### 8.2 Intelligence Artificielle
| Fichier | Responsabilité |
|---------|----------------|
| `IAAction.cs` | Évaluation des actions (scoring, décisions) |
| `IA.cs` | Exécution automatique des actions de l'IA |

### 8.3 Cartes
| Fichier | Responsabilité |
|---------|----------------|
| `CardUI.cs` | Cartes du joueur (UI, interactions, actions) |
| `CardAI.cs` | Cartes de l'IA (UI, états, cibles) |
| `CarteData.cs` | Données des cartes (ATK, DEF, capacités) |

### 8.4 États et Modes
| Mode | Description |
|------|-------------|
| `"selectDeck"` | Sélection du deck initial |
| `"selectCardToPlayAction"` | Sélection d'une carte pour jouer |
| `"hasCardSelectedToAction"` | Carte sélectionnée, choix d'action |
| `"selectCardOpponentToAttack"` | Sélection de la cible d'attaque |

### 8.5 États des Cartes
| État Offensif | Description |
|---------------|-------------|
| `"wait"` | En attente de sélection |
| `"waitOrder"` | Sélectionnée, en attente d'action |
| `"atk"` | A choisi d'attaquer |
| `"passed"` | A choisi de passer |

| État Défensif | Description |
|---------------|-------------|
| `"notCibled"` | Pas ciblée |
| `"cibled"` | Ciblée par une attaque |
| `"isAttacked"` | A reçu une attaque |

---

## 9. Flux Complet d'un Tour

```
┌─────────────────────────────────────────────────────────────┐
│                    DÉBUT DU TOUR                              │
│              GameManager.StartTurn()                         │
└─────────────────────────────────────────────────────────────┘
                        │
                        ▼
        ┌───────────────────────────────┐
        │   Qui commence ?              │
        └───────────────────────────────┘
                │              │
        ┌───────┘              └───────┐
        ▼                              ▼
┌───────────────┐            ┌───────────────┐
│  JOUEUR       │            │     IA        │
│  commence     │            │  commence     │
└───────────────┘            └───────────────┘
        │                              │
        ▼                              ▼
┌──────────────────┐        ┌──────────────────┐
│ Sélection carte  │        │ IAAction.Decide  │
│ → Attaquer/Passer│        │ → Exécute auto   │
│ → Choisir cible  │        │ → Stocke attaques│
└──────────────────┘        └──────────────────┘
        │                              │
        └──────────────┬───────────────┘
                       ▼
        ┌──────────────────────────────┐
        │  Les deux ont fini ?          │
        │  isEndturnPlayer &&            │
        │  isEndturnAI                  │
        └──────────────────────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  ApplyAllAttacksInRandomOrder│
        │  → Ordre aléatoire            │
        │  → Applique tous les dégâts   │
        └──────────────────────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  GameManager.EndTurn()      │
        │  → Nouveau round             │
        │  → Alternance                │
        └──────────────────────────────┘
                       │
                       ▼
        ┌──────────────────────────────┐
        │  RETOUR AU DÉBUT             │
        └──────────────────────────────┘
```

---

## 10. Points d'Attention

### 10.1 Stockage des Attaques
- **Joueur** : Les attaques sont stockées dans `CardUI.target` et `CardUI.targetID`
- **IA** : Les attaques sont stockées dans `IA.aiAttacksThisTurn` (liste `AttackInfo`)

### 10.2 Application des Dégâts
- Les dégâts ne sont **PAS** appliqués immédiatement
- Ils sont appliqués à la fin du tour dans `ApplyAllAttacksInRandomOrder()`
- L'ordre d'application est **aléatoire** (joueur ou IA commence)

### 10.3 Fin de Tour
- Le tour se termine quand **les deux joueurs** ont fini
- `isEndturnPlayer = true` ET `isEndturnAI = true`
- Les attaques sont alors appliquées

### 10.4 Limites d'Attaques
- Maximum : `MAX_NUMBER_ATK_ROUND = 2` attaques par tour
- Compteurs : `numberOfAttacksUsedPlayer` et `numberOfAttacksUsedIA`

---

## 11. Recherche Rapide d'Éléments

### Pour trouver où une action est déclenchée :
- **Clic joueur** → Chercher dans `PlayerActionManager.cs`
- **Action IA** → Chercher dans `IA.cs`
- **Évaluation IA** → Chercher dans `IAAction.cs`

### Pour trouver où un état est modifié :
- **États des cartes** → `CardUI.cs` ou `CardAI.cs`
- **États globaux** → `GameManager.cs` ou `BoardManager.cs`

### Pour trouver où les dégâts sont appliqués :
- **Application des dégâts** → `IA.ApplyAllAttacksInRandomOrder()` dans `IA.cs`
- **Calcul des dégâts** → `IA.ApplySingleAttack()` dans `IA.cs`

---

**Dernière mise à jour** : Basé sur l'architecture actuelle du projet
