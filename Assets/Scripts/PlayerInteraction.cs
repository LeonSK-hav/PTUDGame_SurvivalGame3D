using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField, Min(0f)] private float interactDistance = 3f;
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField] private Transform playerRoot;

    [Header("References")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private Image handIcon;
    [SerializeField] private TextMeshProUGUI promptText;

    private void Awake()
    {
        if (playerRoot == null)
        {
            CharacterController characterController = GetComponentInParent<CharacterController>();
            playerRoot = characterController != null ? characterController.transform : transform.root;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        SetPromptVisible(false);
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            ClearTarget();
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red);

        if (Time.timeScale <= 0f || IsGameplayInputBlocked())
        {
            ClearTarget();
            return;
        }

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            interactDistance,
            interactionLayers,
            QueryTriggerInteraction.Ignore);

        int closestValidHit = -1;
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].collider.transform;
            if (playerRoot != null &&
                (hitTransform == playerRoot || hitTransform.IsChildOf(playerRoot)))
            {
                continue;
            }

            if (closestValidHit < 0 || hits[i].distance < hits[closestValidHit].distance)
            {
                closestValidHit = i;
            }
        }

        if (closestValidHit < 0)
        {
            ClearTarget();
            return;
        }

        RaycastHit hit = hits[closestValidHit];
        WorldItem worldItem = hit.collider.GetComponentInParent<WorldItem>();
        if (worldItem == null || worldItem.itemData == null || worldItem.count <= 0)
        {
            ClearTarget();
            return;
        }

        UpdatePrompt(worldItem);

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && interactKey != Key.None && keyboard[interactKey].wasPressedThisFrame)
        {
            TryPickUp(worldItem);
        }
    }

    private bool IsGameplayInputBlocked()
    {
        if (UIManager.Instance != null)
        {
            return UIManager.Instance.IsGameplayInputBlocked;
        }

        return Cursor.visible || Cursor.lockState != CursorLockMode.Locked;
    }

    private void UpdatePrompt(WorldItem worldItem)
    {
        SetPromptVisible(true);

        if (promptText != null)
        {
            promptText.text = $"[{interactKey}] Nhặt {worldItem.itemData.itemName} x{worldItem.count}";
        }
    }

    private void TryPickUp(WorldItem worldItem)
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (inventoryManager == null)
        {
            Debug.LogError("Không tìm thấy InventoryManager để nhặt vật phẩm.", this);
            return;
        }

        if (!inventoryManager.AddItem(worldItem.itemData, worldItem.count))
        {
            return;
        }

        Destroy(worldItem.gameObject);
        ClearTarget();
    }

    private void ClearTarget()
    {
        SetPromptVisible(false);
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (interactionPrompt != null && interactionPrompt.activeSelf != isVisible)
        {
            interactionPrompt.SetActive(isVisible);
        }

        if (handIcon != null && handIcon.gameObject.activeSelf != isVisible)
        {
            handIcon.gameObject.SetActive(isVisible);
        }
    }
}
