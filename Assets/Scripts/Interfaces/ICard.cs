using TMPro;

public interface ICard
{
    string nameCard { get; }
    int idCard { get; set; }
    int defenseValue { get; set; }
    int attaqueValue { get; set; }
    TMP_Text defenseText { get; }
    TMP_Text attaqueText { get; }

    string stateOffensif { get; }
    string stateDefensif { get; }
    bool isHiddenSlot { get; }

    bool isCardPlayer { get; set; }
}