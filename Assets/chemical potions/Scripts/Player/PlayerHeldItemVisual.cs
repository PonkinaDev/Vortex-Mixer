using UnityEngine;

public class PlayerHeldItemVisual : MonoBehaviour
{
    [SerializeField] private Transform _holdPoint;
    [SerializeField] private GameObject _potionPrefab;

    private GameObject _heldVisual;

    private IngredientType _lastIngredient = IngredientType.None;
    private PotionState _lastState = PotionState.Raw;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public void UpdateVisual(IngredientType ingredient, PotionState potionState)
    {
        if (!VisualChanged(ingredient, potionState))
            return;

        DestroyVisual();

        if (ingredient == IngredientType.None)
            return;

        _heldVisual = CreateVisual(ingredient, potionState);
    }

    private bool VisualChanged(IngredientType ingredient, PotionState potionState)
    {
        if (_lastIngredient == ingredient && _lastState == potionState)
            return false;

        _lastIngredient = ingredient;
        _lastState = potionState;
        return true;
    }

    private void DestroyVisual()
    {
        if (_heldVisual != null)
            Destroy(_heldVisual);
    }

    private GameObject CreateVisual(IngredientType ingredient, PotionState potionState)
    {
        if (_potionPrefab == null)
        {
            return null;
        }

        GameObject visual = Instantiate(_potionPrefab, _holdPoint);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        Color color = IngredientColorUtility.GetPotionColor(ingredient, potionState);
        ApplyColor(visual, color);

        return visual;
    }

    private void ApplyColor(GameObject visual, Color color)
    {
        Renderer[] renderers =
            visual.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Material[] materials = renderer.materials;

            if (materials.Length < 3)
                continue;

            Material targetMaterial = materials[1];

            if (targetMaterial.HasProperty("_BaseColor"))
            {
                targetMaterial.SetColor(
                    "_BaseColor",
                    color
                );
            }

            if (targetMaterial.HasProperty("_Color"))
            {
                targetMaterial.SetColor(
                    "_Color",
                    color
                );
            }
        }
    }
}