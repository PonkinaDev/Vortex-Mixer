using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMenu : MonoBehaviour
{
    [SerializeField]
    private int _menuSceneIndex = 0;

    public async void Return()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkRunner runner =
                NetworkManager.Instance.Runner;

            if (runner != null)
            {
                await runner.Shutdown();
            }
        }

        SceneManager.LoadScene(_menuSceneIndex);
    }
}