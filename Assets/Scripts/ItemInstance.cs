[System.Serializable]
public class ItemInstance
{
    public int key; // Links to database
    public int stackSize = 1;

    public virtual bool Compare(ItemInstance other)
    {
        if (other == null)
            return false;

        // Extend this if you want to include other data
        return key == other.key;
    }
}
