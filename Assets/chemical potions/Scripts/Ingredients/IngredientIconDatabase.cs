using UnityEngine;

[CreateAssetMenu(fileName = "IngredientIconDatabase", menuName = "Game/Ingredient Icon Database")]
public class IngredientIconDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public IngredientType type;
        public Sprite icon;
    }

    [SerializeField] private Entry[] _entries;

    public Sprite Get(IngredientType type)
    {
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].type == type)
                return _entries[i].icon;
        }

        return null;
    }
}