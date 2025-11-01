using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class AmountUI : MonoBehaviour
{
    public TMP_InputField amountInput;
    public Button confirmButton;
    public Button cancelButton;

    private int maxAmount;
    private UnityAction<int> onConfirmAction;

    public void OpenPrompt(int max, UnityAction<int> onConfirm)
    {
        maxAmount = max;
        onConfirmAction = onConfirm;

        amountInput.text = "1";
        gameObject.SetActive(true);

        amountInput.onValueChanged.RemoveAllListeners();
        amountInput.onValueChanged.AddListener(ValidateAmount);

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(Confirm);

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void ValidateAmount(string value)
    {
        if (int.TryParse(value, out int entered))
        {
            entered = Mathf.Clamp(entered, 1, maxAmount);
            amountInput.text = entered.ToString();
        }
    }

    private void Confirm()
    {
        int amount = Mathf.Clamp(int.Parse(amountInput.text), 1, maxAmount);
        gameObject.SetActive(false);
        onConfirmAction?.Invoke(amount);
    }
}
