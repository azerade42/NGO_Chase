using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CountdownCanvasController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _countdownText;
    
    private void OnEnable()
    {
        RoundManager.OnRoundMessageChangedRpc += UpdateCountdownText;
    }
    private void OnDisable()
    {
        RoundManager.OnRoundMessageChangedRpc -= UpdateCountdownText;
    }

    private void Start()
    {
        _countdownText.text = "Waiting for players...";
    }

    private void UpdateCountdownText(string message)
    {
        _countdownText.text = message;
    }
    
}
