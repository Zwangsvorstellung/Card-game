
using UnityEngine;

[CreateAssetMenu(fileName = "NouvelleCarte", menuName = "Cartes/Carte")]
public class CarteScriptableObject : ScriptableObject
{
    public int idCard;
    public string nom;
    [TextArea]
    public string nameCapacity;
    public string descriptionCapacity;
    public int atk;
    public int def;
    public int capacityId;
    public Sprite image;
    public string color;
}
