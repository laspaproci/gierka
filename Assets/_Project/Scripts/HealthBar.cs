using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;

    public void SetMaxHealth(int maxHealth)
    {
        if (slider != null)
        {
            slider.maxValue = maxHealth;
            slider.value    = maxHealth;
        }
    }

    public void SetHealth(int health)
    {
        if (slider != null)
            slider.value = health;
    }
}
