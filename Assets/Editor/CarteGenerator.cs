using UnityEngine;
using UnityEditor;

public class CarteGenerator
{
    [MenuItem("Cartes/Générer 20 Cartes de Test")]
    public static void GenererCartes()
    {
        string path = "Assets/CartesGenerees/";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets", "CartesGenerees");
        }

        (int idCard, string nom, string nameCapacity, string descriptionCapacity, int atk, int def, int capacityId, string color)[] cartes =
        {
            (1, "Zao", "Agilité Risquée","Si Zao attaque, elle ne peut esquiver. Si passive, elle est intouchable", 4, 3, 1, "ad0e0e"),
            (2, "Hiver", "Frappe Gelée","Quand Hiver attaque, la cible ne peut attaquer au tour suivant", 3, 4, 2, "dedfdf"),
            (3, "Cassandre", "Aura de Force","+1 d'ATQ aux alliés adjacents si Cassandre passe son tour", 4, 4, 3, "d9af4e"),
            (4, "Solicia", "Réflexion partielle","Quand un allié est attaqué, inflige 1 dégât à l’attaquant.", 3, 6, 4, "47a658"),
            (5, "Triomphe", "Frappe Puissante","1 chance sur 2 d'acquérir un malus ou bonus de DF de 1 à l'attaque", 4, 5, 5, "854b22"),
            (6, "Désir", "Tentation", "Lorsque Désir passe son tour, un adversaire au hasard ne peut pas attaquer au tour suivant",3, 4, 6, "262752"),
            (7, "Vilaine", "Malus d’attaque", "Inflige -1 d'attaque à sa cible sur le tour courant", 5, 3, 7, "ebbc38"),
            (8, "Jaycota", "Attaque de Provocation", "Si attaquée, inflige 1 dégât à l’attaquant en représaille", 2, 6, 8, "6f0000"),
            (9, "Neo", "Attaque Surprise","Augmentation de l'ATQ de 1 à chaque tour si Neo vise une cible différente",  4, 4, 9,"94a2a7"),
            (10, "Anaxagore", "Percée Défensive", "Réduit la défense de la cible de 1 à l'attaque", 4, 5, 10,"3b4d65"),
            (11, "Clorel", "Régénération", "Gagne 1 point de défense si il n’attaque pas", 5, 4, 11,"372507"),
            (12, "Belindra", "Bouclier collectif", "Réduction des dégâts reçus d'1 point des alliés adjacents si n’attaque pas", 3, 7, 12,"2e869c"),
            (13, "Trahison", "Terreur Sélective", "Si n’attaque pas, les adversaires passifs subissent -1 DF", 4, 4, 13,"afe0cf"),
            (14, "Xiang", "Ignorance Défensive", "Ignore 1 point de défense adverse", 4, 4, 14,"8341dc"),
            (15, "Ambroise", "Onde de Choc Passive", "Si Ambroise n’attaque pas, inflige -1 DF à un adversaire aléatoire passif", 3, 4, 15,"b40000"),
            (16, "Zarla", "Aubaine", "Si pas ciblée, elle gagne +1 DF. Si attaquée, elle gagne +1 ATK", 4, 3, 16,"552842"),
            (17, "Tyroine", "Aléatoire", "Vise un adversaire aléatoirement et ignore 1 point DF", 4, 5, 17,"0d3175"),
            (18, "Ondine", "Vague létale", "Inflige les 3 points de dégâts aléatoirement", 3, 5, 18,"91c6da"),
            (19, "Ruby", "Combo", "Si inflige des dégâts, inflige aussi 1 dégât à un ennemi adjacent à la cible.", 4, 4, 19,"901412"),
            (20, "Minoson", "Protection", "Quand un allié est attaqué, 50 % de chance que 50 % des dégâts soient transférés à Minoson", 3, 4, 20,"3f0f07")
        };

        foreach (var (idCard, nom, nameCapacity, descriptionCapacity, atk, def, cap, color) in cartes)
        {
            CarteScriptableObject carte = ScriptableObject.CreateInstance<CarteScriptableObject>();
            carte.idCard = idCard;
            carte.nom = nom;
            carte.nameCapacity = nameCapacity;
            carte.descriptionCapacity = descriptionCapacity;
            carte.atk = atk;
            carte.def = def;
            carte.capacityId = cap;
            carte.image = Resources.Load<Sprite>("SpritesCartes/" + nom);
            carte.color = color;

            AssetDatabase.CreateAsset(carte, path + nom + ".asset");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("20 cartes générées !");
    }
}
