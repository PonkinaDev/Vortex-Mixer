using UnityEngine;

public static class IngredientUtility
{
    private const int PrimaryReward = 10;
    private const int SecondaryReward = 20;

    private static readonly IngredientType[] RandomIngredients =
    {
        IngredientType.Red,
        IngredientType.Blue,
        IngredientType.Yellow,
        IngredientType.Green,
        IngredientType.Orange,
        IngredientType.Purple
    };

    public static IngredientType GetRandomIngredient()
    {
        int index =
            Random.Range(
                0,
                RandomIngredients.Length
            );

        return RandomIngredients[index];
    }

    public static bool IsPrimary(
        IngredientType ingredient
    )
    {
        return ingredient ==
                   IngredientType.Red
               || ingredient ==
                   IngredientType.Blue
               || ingredient ==
                   IngredientType.Yellow;
    }

    public static bool IsSecondary(
        IngredientType ingredient
    )
    {
        return ingredient ==
                   IngredientType.Green
               || ingredient ==
                   IngredientType.Orange
               || ingredient ==
                   IngredientType.Purple;
    }

    public static int GetReward(
        IngredientType ingredient
    )
    {
        return IsPrimary(ingredient)
            ? PrimaryReward
            : SecondaryReward;
    }

    public static bool TryMix(
        IngredientType a,
        IngredientType b,
        out IngredientType result
    )
    {
        result = IngredientType.None;

        if (a == b)
            return false;

        if ((a == IngredientType.Red &&
             b == IngredientType.Blue) ||
            (a == IngredientType.Blue &&
             b == IngredientType.Red))
        {
            result = IngredientType.Purple;
            return true;
        }

        if ((a == IngredientType.Blue &&
             b == IngredientType.Yellow) ||
            (a == IngredientType.Yellow &&
             b == IngredientType.Blue))
        {
            result = IngredientType.Green;
            return true;
        }

        if ((a == IngredientType.Red &&
             b == IngredientType.Yellow) ||
            (a == IngredientType.Yellow &&
             b == IngredientType.Red))
        {
            result = IngredientType.Orange;
            return true;
        }

        return false;
    }
}