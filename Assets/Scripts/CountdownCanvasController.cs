using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CountdownCanvasController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _countdownText;
    
    private void OnEnable()
    {        
        RoundManager.OnCountdownMessageChangedRpc += UpdateCountdownText;
    }
    private void OnDisable()
    {
        RoundManager.OnCountdownMessageChangedRpc -= UpdateCountdownText;
    }

    private void Start()
    {
        _countdownText.text = string.Empty;
    }

    private void UpdateCountdownText(string count)
    {
        _countdownText.text = count.ToString();
    }
    
}
