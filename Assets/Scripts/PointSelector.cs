using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine;

public class PointSelector : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private GameObject[] pointPrefabs; // Array of prefabs
    [SerializeField] private Transform pointsParent;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Transform originTransform;

    [Header("UI")]
    [SerializeField] private TMP_Text pointAText;
    [SerializeField] private TMP_Text pointBText;
    [SerializeField] private TMP_Text pointATextOUT;
    [SerializeField] private TMP_Text pointBTextOUT;

    public Transform OriginTransform => originTransform;

    private List<GameObject> selectedPoints = new List<GameObject>();

    private int currentPrefabIndex = 0; // Default to the first prefab

    private void Start()
    {
        // Load selected prefab index or fallback to 0
        currentPrefabIndex = PlayerPrefs.GetInt("SelectedPrefabIndex", 0);
    }

    private void Update()
    {
        if (Touchscreen.current == null || selectedPoints.Count >= 2)
            return;

        foreach (var touch in Touchscreen.current.touches)
        {
            if (touch.press.wasPressedThisFrame)
            {
                Ray ray = arCamera.ScreenPointToRay(touch.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
                {
                    GameObject point = Instantiate(pointPrefabs[currentPrefabIndex], hit.point, Quaternion.identity, pointsParent);
                    Handheld.Vibrate();  // Haptic feedback on touch

                    if (GameManager.Instance.ColoredPointsPurchased)
                    {
                        Renderer rend = point.GetComponent<Renderer>();
                        if (rend != null)
                            rend.material.color = Color.cyan;
                    }

                    selectedPoints.Add(point);
                    DisplayCoordinates(hit.point);

                    if (selectedPoints.Count == 2)
                        StageManager.Instance.ReceivePoints(selectedPoints);
                }
            }
        }
    }

    public void SetCurrentPrefabIndex(int index)
    {
        if (index >= 0 && index < pointPrefabs.Length && IsPrefabPurchased(index))
        {
            currentPrefabIndex = index;
            PlayerPrefs.SetInt("SelectedPrefabIndex", index);
            PlayerPrefs.Save();
        }
    }

    public bool IsPrefabPurchased(int index)
    {
        return PlayerPrefs.GetInt("PrefabPurchased_" + index, 0) == 1;
    }
    private void DisplayCoordinates(Vector3 worldPos)
    {
        Vector3 localPos = originTransform.InverseTransformPoint(worldPos);
        int roundedX = Mathf.RoundToInt(localPos.x);
        int roundedY = Mathf.RoundToInt(localPos.y);

        string text = $"({roundedX}, {roundedY})";

        if (selectedPoints.Count == 1)
            pointAText.text = $"Point A: {text}";
        else if (selectedPoints.Count == 2)
            pointBText.text = $"Point B: {text}";
    }

    public void ClearPoints()
    {
        foreach (var point in selectedPoints)
            Destroy(point);

        selectedPoints.Clear();
        pointAText.text = "Point A:";
        pointBText.text = "Point B:";
        pointATextOUT.text = "Point A:";
        pointBTextOUT.text = "Point B:";
    }
}
