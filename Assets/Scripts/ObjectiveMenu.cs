using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ObjectiveMenu : UIMenu
{
    public ItemTradingPanelUI inventoryPanel;
    public GameObject singleItemBox;
    public GameObject multiItemBox;
    public GameObject singleItemAndPlayerInventoryBox;
    public GameObject multiItemAndPlayerInventoryBox;
    public Button closeButton;
    public Button submitButton;
    public TMP_Text title;
    public TMP_Text description;
    // public bool requiresPlayerInventory;
    private Inventory playerInventory;
    private Inventory submissionInventory;
    // private int numTargets;
    public Func<bool> OnItemSubmitted;
    public Func<bool> OnObjectiveComplete;

    protected override void Start()
    {
        base.Start();

        closeButton.onClick.AddListener(CloseMenu);
        submitButton.onClick.AddListener(OnSubmit);
    }

    public void OpenObjectiveMenu(Inventory playerInventory, Inventory submissionInventory, string title, string description, int numTargets, bool requiresPlayerInventory)
    {
        base.OpenMenu();

        this.title.text = title;
        this.description.text = description;
        this.playerInventory = playerInventory;
        this.submissionInventory = submissionInventory;

        if (!requiresPlayerInventory)
        {
            if (numTargets == 1)
            {
                singleItemBox.SetActive(true);
                multiItemBox.SetActive(false);
                singleItemAndPlayerInventoryBox.SetActive(false);
                inventoryPanel.otherInventoryPanel = singleItemBox.GetComponent<InventoryPanelUI>();
            }
            else
            {
                singleItemBox.SetActive(false);
                multiItemBox.SetActive(true);
                singleItemAndPlayerInventoryBox.SetActive(false);
                inventoryPanel.otherInventoryPanel = multiItemBox.GetComponent<InventoryPanelUI>();
            }
        }
        else
        {
            if (numTargets == 1)
            {
                singleItemBox.SetActive(false);
                multiItemBox.SetActive(false);
                singleItemAndPlayerInventoryBox.SetActive(true);
                ItemAndPlayerInventoryUI itemAndPlayerInventoryUI = singleItemAndPlayerInventoryBox.GetComponent<ItemAndPlayerInventoryUI>();
                inventoryPanel.inventoryPanel = itemAndPlayerInventoryUI.playerInventoryPanel;
                inventoryPanel.otherInventoryPanel = itemAndPlayerInventoryUI.itemBox;
            }
            else
            {
                singleItemBox.SetActive(false);
                multiItemBox.SetActive(false);
                singleItemAndPlayerInventoryBox.SetActive(false);
                multiItemAndPlayerInventoryBox.SetActive(true);
                ItemAndPlayerInventoryUI itemAndPlayerInventoryUI = multiItemAndPlayerInventoryBox.GetComponent<ItemAndPlayerInventoryUI>();
                inventoryPanel.inventoryPanel = itemAndPlayerInventoryUI.playerInventoryPanel;
                inventoryPanel.otherInventoryPanel = itemAndPlayerInventoryUI.itemBox;
            }
        }

        inventoryPanel.PopulateInventories(
            playerInventory,
            submissionInventory
        );
    }

    public void OnSubmit()
    {
        List<ItemInstance> submissionItems = submissionInventory.GetItems();
        if (submissionItems.Count > 0)
        {
            if (OnItemSubmitted())
            {
                Debug.Log("Objective complete");
                OnObjectiveComplete();
                inventoryPanel.PopulateInventories(
                    playerInventory,
                    submissionInventory
                );
            }
        }
    }
}