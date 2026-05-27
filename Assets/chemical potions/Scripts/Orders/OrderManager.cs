using Fusion;
using UnityEngine;

public class OrderManager : NetworkBehaviour
{
    public static OrderManager Instance
    {
        get;
        private set;
    }

    [Networked]
    public int TotalMoney
    {
        get;
        set;
    }

    [Networked, Capacity(10)]
    public NetworkArray<OrderNetworkData> Orders =>
        default;

    [SerializeField]
    private int _maxOrders = 3;

    private bool _uiDirty = true;

    public bool IsUiDirty => _uiDirty;

    public override void Spawned()
    {
        Instance = this;

        if (HasStateAuthority)
            GenerateInitialOrders();
    }

    public bool TryDeliver(
        IngredientType ingredient,
        PotionState potionState
    )
    {
        if (!CanDeliver(potionState))
            return false;

        int orderIndex =
            FindOrderIndex(ingredient);

        if (orderIndex < 0)
            return false;

        CompleteOrder(orderIndex);

        return true;
    }

    public void ConsumeUiDirty()
    {
        _uiDirty = false;
    }

    private void GenerateInitialOrders()
    {
        for (int i = 0; i < _maxOrders; i++)
            GenerateOrder();
    }

    private bool CanDeliver(
        PotionState potionState
    )
    {
        if (!HasStateAuthority)
            return false;

        return potionState ==
               PotionState.Cooked;
    }

    private int FindOrderIndex(
        IngredientType ingredient
    )
    {
        for (int i = 0; i < Orders.Length; i++)
        {
            if (Orders[i].Ingredient ==
                ingredient)
            {
                return i;
            }
        }

        return -1;
    }

    private void CompleteOrder(int index)
    {
        TotalMoney += Orders[index].Reward;

        RemoveOrder(index);
        GenerateOrder();

        MarkDirty();
    }

    private void GenerateOrder()
    {
        IngredientType ingredient =
            IngredientUtility
                .GetRandomIngredient();

        OrderNetworkData order = new()
        {
            Ingredient = ingredient,
            Reward =
                IngredientUtility
                    .GetReward(
                        ingredient
                    )
        };

        int emptySlot =
            FindEmptyOrderSlot();

        if (emptySlot >= 0)
        {
            Orders.Set(emptySlot, order);
            MarkDirty();
            return;
        }

        Orders.Set(0, order);

        MarkDirty();
    }

    private int FindEmptyOrderSlot()
    {
        for (int i = 0; i < Orders.Length; i++)
        {
            if (Orders[i].Ingredient ==
                IngredientType.None)
            {
                return i;
            }
        }

        return -1;
    }

    private void RemoveOrder(int index)
    {
        Orders.Set(index, new OrderNetworkData
        {
            Ingredient =
                IngredientType.None,

            Reward = 0
        });
    }

    private void MarkDirty()
    {
        _uiDirty = true;
    }

    public struct OrderNetworkData
        : INetworkStruct
    {
        public IngredientType Ingredient;
        public int Reward;
    }
}