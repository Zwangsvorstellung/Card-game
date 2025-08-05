using UnityEngine;

[System.Serializable]
public class CarteData
{
    public int idCard;
    public string nom;
    public string nameCapacity;
    public string descriptionCapacity;
    public int attaque;
    public int defense;
    public int capacityId;
    public Sprite image; 
    public readonly string instanceId = System.Guid.NewGuid().ToString();

    public CarteData(int idCard, string nom, string nameCapacity, string descriptionCapacity, int atk, int def, int capacityId, Sprite image)
    {
        this.idCard = idCard;
        this.nom = nom;
        this.nameCapacity = nameCapacity;
        this.descriptionCapacity = descriptionCapacity;
        attaque = atk;
        defense = def;
        this.capacityId = capacityId;
        this.image = image;
    }

    // Redéfinition de ToString pour faciliter le debug (affichage dans les logs)
    public override string ToString()
    {
        return $"Carte: {nom} (ID: {idCard}, Inst: {instanceId}, ATK: {attaque}, DEF: {defense})";
    }
}