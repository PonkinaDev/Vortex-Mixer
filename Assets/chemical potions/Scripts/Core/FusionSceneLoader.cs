using Fusion;
using UnityEngine;

public class FusionSceneLoader : MonoBehaviour, ISceneLoader
{
    [SerializeField] private int _gameSceneIndex = 1;

    private NetworkRunner _runner;

    public void Initialize(NetworkRunner runner)
    {
        _runner = runner;
    }

    public void LoadGameScene()
    {

        _runner.LoadScene(SceneRef.FromIndex(_gameSceneIndex));
    }
}