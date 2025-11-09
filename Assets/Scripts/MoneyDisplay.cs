using UnityEngine;
using TMPro;

public class MoneyDisplay : MonoBehaviour
{
    public TextMeshProUGUI moneyText;

    private void OnEnable()
    {
        FirstPersonController.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
    }

    private void OnDisable()
    {
        FirstPersonController.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
    }

    
    private void HandleLocalPlayerSpawned(FirstPersonController player)
    {
        // Now safe to use Player.LocalPlayer
        Debug.Log("Local player event proced in money display");
        moneyText.text = "$" + player.Money.ToString();
        player.OnMoneyChanged += UpdateMoneyText;
    }
    

    private void UpdateMoneyText(int newMoney)
    {
        moneyText.text = "$" + newMoney.ToString();
    }
}