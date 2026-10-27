using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonImageChanger : MonoBehaviour
{
    // Kéo đối tượng cần đổi hình vào đây trong Unity Inspector
    public GameObject targetObject; 
    
    // Kéo hình ảnh mới (Sprite) vào đây
    public Sprite newSprite; 

    // Hàm này sẽ được gán vào Button
    public void ChangeImage()
    {
        if (targetObject != null && newSprite != null)
        {
            Image img = targetObject.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = newSprite;
            }
        }
    }
}
