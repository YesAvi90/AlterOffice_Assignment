using TMPro;
using UnityEngine;

public class EnergyUI : MonoBehaviour
{
    public static EnergyUI Instance;

    [SerializeField] private TextMeshProUGUI energyText;

    void Awake() => Instance = this;

    public void UpdateEnergy(int energy)
    {
        energyText.text = $"Available Energy: {energy}";
    }
}
