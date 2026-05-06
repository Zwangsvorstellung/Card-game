public enum PlayerActionState
{
    NONE,
    UI,
    AI
}

public enum OffensiveState
{
    ATK,
    PASSED,
    WAIT,
    WAIT_ORDER,
    SELECT_TARGET,
    HIDDEN
}

public enum DefensiveState
{
    NOT_CIBLED,
    CIBLED,
    HIDDEN,
}

public enum GameMode
{
    DECK,
    SELECT_DECK,
    GAME_OVER,
    SELECT_CARD_TO_PLAY_ACTION,
    HAS_CARD_SELECTED_TO_ACTION,
    SELECT_CARD_OPPONENT_TO_ATTACK
}