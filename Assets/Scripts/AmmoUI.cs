using UnityEngine;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoText;
    private Weapon currentWeapon;

    public void SetWeapon(Weapon weapon)
    {
        currentWeapon = weapon;
    }

    void Update()
    {
        if (currentWeapon == null)
            return;
        
        // if (WeaponDatabase.GetWeapon(currentWeapon.weaponKey).contextual)
        // {
        //     ammoText.text = "";
        //     return;
        // }

        int current = currentWeapon.GetCurrentAmmo();
        int reserve = currentWeapon.GetReserveAmmo();

        ammoText.text = $"{current} / {reserve}";
    }
}