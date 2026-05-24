using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class PotionCauldron : NetworkBehaviour
{
    [Header("Visual")]
    [SerializeField]
    private Renderer _renderer;

    [Header("Progress")]
    [SerializeField]
    private Slider _progressBar;

    [SerializeField]
    private Image _progressFill;

    [Header("Cook Settings")]
    [SerializeField]
    private float _cookTime = 5f;

    [SerializeField]
    private float _burnTime = 10f;

    [Networked]
    public IngredientType CurrentPotion { get; set; }

    [Networked]
    public PotionState CurrentState { get; set; }

    [Networked]
    public float CookTimer { get; set; }

    private IngredientType _lastPotion =
        IngredientType.None;

    private PotionState _lastState =
        PotionState.Raw;

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (CurrentPotion ==
            IngredientType.None)
            return;

        CookTimer += Runner.DeltaTime;

        if (CookTimer >= _burnTime)
        {
            CurrentState =
                PotionState.Burned;
        }
        else if (CookTimer >= _cookTime)
        {
            CurrentState =
                PotionState.Cooked;
        }
    }

    public override void Render()
    {
        UpdateVisual();

        UpdateProgressBar();
    }

    public bool TryAddPotion(
        IngredientType ingredient
    )
    {
        if (CurrentPotion !=
            IngredientType.None)
            return false;

        CurrentPotion =
            ingredient;

        CurrentState =
            PotionState.Raw;

        CookTimer = 0f;

        return true;
    }

    public bool TryTakePotion(
        out IngredientType potion,
        out PotionState state
    )
    {
        potion = IngredientType.None;

        state = PotionState.Raw;

        if (CurrentPotion ==
            IngredientType.None)
            return false;

        potion =
            CurrentPotion;

        state =
            CurrentState;

        CurrentPotion =
            IngredientType.None;

        CurrentState =
            PotionState.Raw;

        CookTimer = 0f;

        return true;
    }

    private void UpdateVisual()
    {
        if (_lastPotion ==
            CurrentPotion &&
            _lastState ==
            CurrentState)
            return;

        _lastPotion =
            CurrentPotion;

        _lastState =
            CurrentState;

        if (_renderer == null)
            return;

        Color targetColor =
            Color.white;

        switch (CurrentPotion)
        {
            case IngredientType.Red:
                targetColor =
                    Color.red;
                break;

            case IngredientType.Blue:
                targetColor =
                    Color.blue;
                break;

            case IngredientType.Yellow:
                targetColor =
                    Color.yellow;
                break;

            case IngredientType.Green:
                targetColor =
                    Color.green;
                break;

            case IngredientType.Orange:
                targetColor =
                    new Color(1f, 0.5f, 0f);
                break;

            case IngredientType.Purple:
                targetColor =
                    new Color(0.5f, 0f, 1f);
                break;
        }

        if (CurrentState ==
            PotionState.Cooked)
        {
            targetColor *= 0.7f;
        }

        if (CurrentState ==
            PotionState.Burned)
        {
            targetColor =
                Color.black;
        }

        _renderer.material.color =
            targetColor;
    }

    private void UpdateProgressBar()
    {
        if (_progressBar == null)
            return;

        if (CurrentPotion ==
            IngredientType.None)
        {
            _progressBar.gameObject
                .SetActive(false);

            return;
        }

        _progressBar.gameObject
            .SetActive(true);

        if (CookTimer <= _cookTime)
        {
            _progressBar.value =
                Mathf.Clamp01(
                    CookTimer / _cookTime
                );

            if (_progressFill != null)
            {
                _progressFill.color =
                    Color.green;
            }
        }
        else
        {
            _progressBar.value = 1f;

            float burnProgress =
                Mathf.InverseLerp(
                    _cookTime,
                    _burnTime,
                    CookTimer
                );

            if (_progressFill != null)
            {
                _progressFill.color =
                    Color.Lerp(
                        Color.green,
                        Color.red,
                        burnProgress
                    );
            }
        }
    }
}