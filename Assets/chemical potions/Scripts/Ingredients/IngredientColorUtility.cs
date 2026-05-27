using UnityEngine;

public static class IngredientColorUtility
{
    public static Color GetColor(IngredientType ingredient)
    {
        return ingredient switch
        {
            IngredientType.Red => Color.red,
            IngredientType.Blue => Color.blue,
            IngredientType.Yellow => Color.yellow,
            IngredientType.Green => Color.green,
            IngredientType.Orange => new Color(1f, 0.5f, 0f),
            IngredientType.Purple => new Color(0.5f, 0f, 1f),
            _ => Color.white
        };
    }

    public static Color GetPotionColor(
        IngredientType ingredient,
        PotionState state
    )
    {
        Color color = GetColor(ingredient);

        if (state == PotionState.Cooked)
            color *= 0.7f;

        if (state == PotionState.Burned)
            color = Color.black;

        return color;
    }
}