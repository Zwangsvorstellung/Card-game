
public struct AttackInfo
{
    public CardAI attackerAI;      // Attaquant si c'est l'IA (null si c'est le joueur)
    public CardUI attackerPlayer;  // Attaquant si c'est le joueur (null si c'est l'IA)
    public CardUI targetPlayer;    // Cible si c'est une carte joueur
    public CardAI targetAI;        // Cible si c'est une carte IA
    public int damage;
    public bool isPlayerAttack;     // true si c'est une attaque du joueur, false si c'est l'IA
    public string attackerStateOffensif;
    public string attackerStateDefensif;
    public string targetStateOffensif;
    public string targetStateDefensif;
    public bool hasSoliciaOpponent;
    public bool hasBelindraOpponentStatePassed;

    public string AttackerName
    {
        get
        {
            if (isPlayerAttack)
                return attackerPlayer.nameCard;
            else
                return attackerAI.nameCard;
        }
    }

    public string TargetName
    {
        get
        {
            if (isPlayerAttack)
                return targetAI.nameCard;
            else
                return targetPlayer.nameCard;
        }
    }
    
    public AttackInfo(CardAI attacker, CardUI target, int damage, bool hasSoliciaOpponent, bool hasBelindraOpponentStatePassed)
    {
        this.attackerAI = attacker;
        this.attackerPlayer = null;
        this.targetPlayer = target;
        this.targetAI = null;
        this.damage = damage;
        this.isPlayerAttack = false;
        this.attackerStateOffensif = attacker.stateOffensif;
        this.attackerStateDefensif = attacker.stateDefensif;
        this.targetStateOffensif = target.stateOffensif;
        this.targetStateDefensif = target.stateDefensif;
        this.hasSoliciaOpponent = hasSoliciaOpponent;
        this.hasBelindraOpponentStatePassed = hasBelindraOpponentStatePassed;
    }
    
    public AttackInfo(CardUI attacker, CardAI target, int damage, bool hasSoliciaOpponent, bool hasBelindraOpponentStatePassed)
    {
        this.attackerAI = null;
        this.attackerPlayer = attacker;
        this.targetPlayer = null;
        this.targetAI = target;
        this.damage = damage;
        this.isPlayerAttack = true;
        this.attackerStateOffensif = attacker.stateOffensif;
        this.attackerStateDefensif = attacker.stateDefensif;
        this.targetStateOffensif = target.stateOffensif;
        this.targetStateDefensif = target.stateDefensif;
        this.hasSoliciaOpponent = hasSoliciaOpponent;
        this.hasBelindraOpponentStatePassed = hasBelindraOpponentStatePassed;
    }
}