using Fusion;
using UnityEngine;

public class VictoryManager : MonoBehaviour
{
    [SerializeField]
    private int _targetMoney = 300;

    [SerializeField]
    private int _victorySceneIndex = 2;

    private bool _gameEnded;

    private void Update()
    {
        if (_gameEnded)
            return;

        if (NetworkManager.Instance == null)
            return;

        NetworkRunner runner =
            NetworkManager.Instance.Runner;

        if (runner == null)
            return;

        if (!runner.IsServer)
            return;

        if (OrderManager.Instance == null)
            return;

        int money =
            OrderManager.Instance.TotalMoney;

        if (money < _targetMoney)
            return;

        _gameEnded = true;

        runner.LoadScene(
            SceneRef.FromIndex(_victorySceneIndex)
        );
    }
}