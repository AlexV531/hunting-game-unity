public enum AmmoType
{
    None,
    Bullet,
    Bolt,
    Arrow
}

[System.Serializable]
public class AmmoDefinition : ItemDefinition
{
    public AmmoType ammoType;
}