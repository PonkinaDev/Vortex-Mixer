using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ConnectionUI : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private GameObject _buttonsPanel;

    private void Start()
    {
        _hostButton.onClick.AddListener(OnHostClicked);
        _joinButton.onClick.AddListener(OnJoinClicked);

        SetStatus("Ready to connect");
    }

    private void OnHostClicked()
    {
        SetStatus("Starting as Host...");
        HideButtons();
        NetworkManager.Instance.StartHost();
    }

    private void OnJoinClicked()
    {
        SetStatus("Connecting as Client...");
        HideButtons();
        NetworkManager.Instance.StartClient();
    }

    private void SetStatus(string msg)
    {
        if (_statusText != null)
            _statusText.text = msg;
    }

    private void HideButtons()
    {
        if (_buttonsPanel != null)
            _buttonsPanel.SetActive(false);
    }
}