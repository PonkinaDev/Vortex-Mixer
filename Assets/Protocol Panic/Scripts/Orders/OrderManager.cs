using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderManager : NetworkBehaviour
{
    public static OrderManager Instance;

    [Header("UI")]
    [SerializeField] private Transform _orderContainer;
    [SerializeField] private GameObject _orderCardPrefab;
    [SerializeField] private TMP_Text _totalMoneyText;

    [Header("Settings")]
    [SerializeField] private int _maxOrders = 3;

    [Networked] public int TotalMoney { get; set; }

    // =========================
    // NETWORKED ORDERS
    // =========================
    [Networked, Capacity(10)]
    public NetworkArray<OrderNetworkData> Orders => default;

    private readonly System.Collections.Generic.List<GameObject> _spawnedCards = new();

    public override void Spawned()
    {
        Instance = this;

        if (HasStateAuthority)
        {
            for (int i = 0; i < _maxOrders; i++)
                GenerateOrder();
        }

        UpdateUI();
    }

    public override void Render()
    {
        UpdateUI();
    }

    // =========================
    // DELIVERY (HOST ONLY LOGIC)
    // =========================
    public bool TryDeliver(IngredientType ingredient, PotionState potionState)
    {
        if (!HasStateAuthority)
            return false;

        if (potionState != PotionState.Cooked)
            return false;

        for (int i = 0; i < Orders.Length; i++)
        {
            if (Orders[i].Ingredient == ingredient)
            {
                TotalMoney += Orders[i].Reward;

                RemoveOrder(i);
                GenerateOrder();

                return true;
            }
        }

        return false;
    }

    // =========================
    // ORDER GENERATION (HOST ONLY)
    // =========================
    private void GenerateOrder()
    {
        IngredientType ingredient = GetRandomIngredient();

        int reward = IsPrimary(ingredient) ? 10 : 20;

        for (int i = 0; i < Orders.Length; i++)
        {
            if (Orders[i].Ingredient == IngredientType.None)
            {
                Orders.Set(i, new OrderNetworkData
                {
                    Ingredient = ingredient,
                    Reward = reward
                });
                return;
            }
        }

        // fallback (si no hay slots vacíos)
        Orders.Set(0, new OrderNetworkData
        {
            Ingredient = ingredient,
            Reward = reward
        });
    }

    private void RemoveOrder(int index)
    {
        Orders.Set(index, new OrderNetworkData
        {
            Ingredient = IngredientType.None,
            Reward = 0
        });
    }

    // =========================
    // UI (CLIENT + HOST)
    // =========================
    private void UpdateUI()
    {
        if (_totalMoneyText != null)
            _totalMoneyText.text = "Money: $" + TotalMoney;

        ClearCards();

        for (int i = 0; i < Orders.Length; i++)
        {
            var order = Orders[i];

            if (order.Ingredient == IngredientType.None)
                continue;

            GameObject card = Instantiate(_orderCardPrefab, _orderContainer);
            _spawnedCards.Add(card);

            Image potionImage = card.transform
                .Find("PotionImage")
                .GetComponent<Image>();

            TMP_Text rewardText = card.transform
                .Find("RewardText")
                .GetComponent<TMP_Text>();

            potionImage.color = GetColor(order.Ingredient);
            rewardText.text = "$" + order.Reward;
        }
    }

    private void ClearCards()
    {
        foreach (var card in _spawnedCards)
        {
            if (card != null)
                Destroy(card);
        }

        _spawnedCards.Clear();
    }

    // =========================
    // HELPERS
    // =========================
    private IngredientType GetRandomIngredient()
    {
        int random = Random.Range(0, 6);

        return random switch
        {
            0 => IngredientType.Red,
            1 => IngredientType.Blue,
            2 => IngredientType.Yellow,
            3 => IngredientType.Green,
            4 => IngredientType.Orange,
            _ => IngredientType.Purple
        };
    }

    private bool IsPrimary(IngredientType ingredient)
    {
        return ingredient == IngredientType.Red ||
               ingredient == IngredientType.Blue ||
               ingredient == IngredientType.Yellow;
    }

    private Color GetColor(IngredientType ingredient)
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

    // =========================
    // NETWORK STRUCT
    // =========================
    public struct OrderNetworkData : INetworkStruct
    {
        public IngredientType Ingredient;
        public int Reward;
    }
}