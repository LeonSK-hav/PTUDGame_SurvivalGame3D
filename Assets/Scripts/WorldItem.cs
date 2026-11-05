using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [Header("Item Info")]
    public ItemData itemData;
    public int count = 1;

    [Header("Settings")]
    public float pullSpeed = 12f; // Tốc độ hút item khi đến gần
    public float pickupRange = 1.5f; // Khoảng cách đủ gần để cộng vào kho

    private bool canBePickedUp = false;
    private bool isBeingPulled = false;
    private Transform playerTransform;
    
    private Rigidbody rb;
    private Collider itemCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();

        // Tự động tìm Player dựa trên Tag "Player" trong Scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    public void Init(ItemData data, int amount)
    {
        itemData = data;
        count = amount;
        
        // Đợi 0.5 giây để vật phẩm kịp rơi xuống mặt đất ổn định rồi mới cho phép nhặt
        Invoke(nameof(EnablePickup), 0.5f);
    }

    void EnablePickup()
    {
        canBePickedUp = true;
    }

    void Update()
    {
        if (!canBePickedUp || playerTransform == null) return;

        // Tính khoảng cách từ vật phẩm đến người chơi
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Nếu người chơi đến gần trong phạm vi hút (ví dụ: 5 mét)
        if (distanceToPlayer <= 5f)
        {
            isBeingPulled = true;
        }
       /*
        // Nếu đang trong trạng thái bị hút, bay thẳng về phía người chơi
        if (isBeingPulled)
        {
            // Tắt trọng lực để item bay mượt mà không bị rơi xuống nữa
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }
            if (itemCollider != null)
            {
                itemCollider.isTrigger = true; // Chuyển thành trigger để bay xuyên qua mọi thứ
            }

            // Di chuyển item về phía player
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position + Vector3.up * 1f, pullSpeed * Time.deltaTime);

            // Khi bay đến sát sạt người chơi thì tiến hành nhặt vào kho và xóa object
            if (distanceToPlayer <= pickupRange)
            {
                TryPickUp();
            }
        }*/
    }

}