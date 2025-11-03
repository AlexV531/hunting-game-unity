using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponButton : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;

    private ItemInstance weaponInstance;
    private LoadoutMenu manager;

    public void Initialize(ItemInstance weaponInstance, LoadoutMenu manager)
    {
        this.weaponInstance = weaponInstance;
        this.manager = manager;

        WeaponDefinition def = WeaponDatabase.GetWeapon(weaponInstance.key);

        icon.sprite = def.icon;
        nameText.text = def.itemName;
        button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        manager.OnWeaponSelected(weaponInstance);
    }
}
