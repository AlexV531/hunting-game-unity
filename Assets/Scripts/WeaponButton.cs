using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponButton : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    private WeaponDefinition weapon;
    private LoadoutManager manager;

    public void Initialize(WeaponDefinition weapon, LoadoutManager manager)
    {
        this.weapon = weapon;
        this.manager = manager;

        icon.sprite = weapon.icon;
        nameText.text = weapon.weaponName;
        button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        manager.OnWeaponSelected(weapon);
    }
}
