using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private PointSelector pointSelector;
    [SerializeField] private Button[] shopButtons;
    [SerializeField] private GameObject notEnoughCoinsPopup;

    private const string PURCHASED_KEY = "PrefabPurchased_";
    private const string SELECTED_KEY = "SelectedPrefabIndex";

    private void Start()
    {
        // Ensure first prefab (index 0) is always unlocked
        if (!IsPurchased(0))
            Purchase(0);

        int savedIndex = PlayerPrefs.GetInt(SELECTED_KEY, 0);
        pointSelector.SetCurrentPrefabIndex(savedIndex);

        // Setup button listeners
        for (int i = 0; i < shopButtons.Length; i++)
        {
            int index = i;

            shopButtons[i].onClick.AddListener(() => OnShopButtonClick(index));
            if (IsPurchased(i))
            {
                HideCoinTag(shopButtons[i]);
            }

            
            UpdateButtonVisual(shopButtons[i], index);
        }

        if (notEnoughCoinsPopup != null)
            notEnoughCoinsPopup.SetActive(false);
    }

    private void OnShopButtonClick(int index)
    {
        if (!IsPurchased(index))
        {
            bool success = GameManager.Instance.SpendCoins(10);
            if (success)
            {
                Purchase(index);
                Debug.Log($"Purchased prefab {index} for 10 coins");

                HideCoinTag(shopButtons[index]); // Hide coin UI
                pointSelector.SetCurrentPrefabIndex(index); // Set as selected
                PlayerPrefs.SetInt(SELECTED_KEY, index);    // Save selected index
                PlayerPrefs.Save();

                UpdateAllButtonVisuals(); // Update all buttons
            }
            else
            {
                Debug.Log("Not enough coins to purchase");
                StartCoroutine(ShowNotEnoughCoinsPopup());
                return;
            }
        }
        else
        {
            pointSelector.SetCurrentPrefabIndex(index);
            PlayerPrefs.SetInt(SELECTED_KEY, index);
            PlayerPrefs.Save();

            UpdateAllButtonVisuals();
            Debug.Log($"Selected prefab at index {index}");
        }
    }
    private void UpdateAllButtonVisuals()
    {
        for (int i = 0; i < shopButtons.Length; i++)
        {
            UpdateButtonVisual(shopButtons[i], i);
        }
    }


    private void Purchase(int index)
    {
        PlayerPrefs.SetInt(PURCHASED_KEY + index, 1);
        PlayerPrefs.Save();
    }

    private bool IsPurchased(int index)
    {
        return PlayerPrefs.GetInt(PURCHASED_KEY + index, 0) == 1;
    }
    private IEnumerator ShowNotEnoughCoinsPopup()
    {
        if (notEnoughCoinsPopup != null)
        {
            notEnoughCoinsPopup.SetActive(true);
            yield return new WaitForSeconds(2f);
            notEnoughCoinsPopup.SetActive(false);
        }
    }
    private void HideCoinTag(Button button)
    {
        foreach (Transform child in button.transform)
        {
            if (child.CompareTag("Coin"))
            {
                child.gameObject.SetActive(false);
                break;
            }
        }
    }

    private void UpdateButtonVisual(Button button, int index)
    {
        
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        int selectedIndex = PlayerPrefs.GetInt(SELECTED_KEY, 0);

        if (!IsPurchased(index))
        {
            label.text = "Buy";
        }
        else if (index == selectedIndex)
        {
            label.text = "Selected";
        }
        else
        {
            label.text = "Select";
        }
    }
}
