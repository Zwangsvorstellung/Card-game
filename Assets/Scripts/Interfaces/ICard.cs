using TMPro;

public interface ICard
{
    string nameCard { get; }
    int defenseValue { get; set; }
    TMP_Text defenseText { get; }
}