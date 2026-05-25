using Fusion;
using UnityEngine;

public class PotionMixer : NetworkBehaviour
{
    [Networked]
    public IngredientType CurrentColor
    {
        get;
        set;
    }

    [SerializeField]
    private Material _potionMaterial;

    private IngredientType _lastVisualColor =
        IngredientType.None;

    public override void Render()
    {
        UpdateVisual();
    }

    public bool TryAddIngredient(
        IngredientType ingredient
    )
    {
        if (ingredient ==
            IngredientType.None)
        {
            return false;
        }

        if (IngredientUtility.IsSecondary(
                CurrentColor))
        {
            return false;
        }

        if (CurrentColor ==
            IngredientType.None)
        {
            CurrentColor = ingredient;
            return true;
        }

        bool success =
            IngredientUtility.TryMix(
                CurrentColor,
                ingredient,
                out IngredientType result
            );

        if (!success)
            return false;

        CurrentColor = result;

        return true;
    }

    private void UpdateVisual()
    {
        if (_lastVisualColor ==
            CurrentColor)
        {
            return;
        }

        _lastVisualColor =
            CurrentColor;

        if (_potionMaterial == null)
            return;

        _potionMaterial.color =
            IngredientColorUtility.GetColor(
                CurrentColor
            );
    }
}