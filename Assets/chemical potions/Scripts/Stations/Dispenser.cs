using UnityEngine;

public class Dispenser : MonoBehaviour
{
    [SerializeField]
    private IngredientType _ingredientType;

    public IngredientType IngredientType =>
        _ingredientType;
}