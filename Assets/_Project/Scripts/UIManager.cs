using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject hpBarPrefab;  // przypisz w inspektorze
    private readonly Dictionary<ulong, Slider> bars = new();

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    public void RegisterPlayer(ulong clientId)
    {
        var go = Instantiate(hpBarPrefab, transform);
        var slider = go.GetComponent<Slider>();
        slider.maxValue = 100;
        slider.value    = 100;
        bars[clientId]   = slider;
    }

    public void UpdateHpDisplay(ulong clientId, int newHp)
    {
        if (bars.TryGetValue(clientId, out var slider))
            slider.value = newHp;
    }

    public void UnregisterPlayer(ulong clientId)
    {
        if (bars.TryGetValue(clientId, out var slider))
        {
            Destroy(slider.gameObject);
            bars.Remove(clientId);
        }
    }
}
