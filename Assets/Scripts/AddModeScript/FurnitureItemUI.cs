using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro


public class FurnitureItemUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;

    private FurnitureData containedData;
    private Button itemButton;

    // =========================================================
    // 초기화
    // =========================================================

    private void Awake()
    {
        // 먼저 자기 자신에서 찾음
        itemButton =
            GetComponent<Button>();

        // 자기 자신에 없다면 자식에서 찾음
        if (itemButton == null)
        {
            itemButton =
                GetComponentInChildren<Button>();
        }

        if (itemButton != null)
        {
            itemButton.onClick.AddListener(
                OnClickItem
            );
        }
        else
        {
            Debug.LogError(
                $"[{gameObject.name}] FurnitureItemUI에 Button을 찾지 못했습니다."
            );
        }
    }

    private void OnDestroy()
    {
        if (itemButton != null)
        {
            itemButton.onClick.RemoveListener(
                OnClickItem
            );
        }
    }

    // =========================================================
    // 데이터 설정
    // =========================================================

    public void Setup(FurnitureData data)
    {
        containedData = data;

        if (data == null)
        {
            if (itemButton != null)
                itemButton.interactable = false;

            return;
        }

        // 이름
        if (nameText != null)
        {
            nameText.text =
                data.furnitureName;
        }

        // 아이콘
        if (iconImage != null)
        {
            if (data.furnitureIcon != null)
            {
                iconImage.sprite =
                    data.furnitureIcon;
            }
        }

        // 데이터가 정상적으로 들어오면
        // 버튼을 다시 활성화
        if (itemButton != null)
        {
            itemButton.interactable = true;
        }
    }

    // =========================================================
    // 버튼 클릭
    // =========================================================

    private void OnClickItem()
    {
        if (containedData == null)
        {
            Debug.LogWarning(
                $"[{gameObject.name}] FurnitureData가 설정되지 않았습니다."
            );

            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning(
                "GameManager.Instance가 없습니다."
            );

            return;
        }

        GameManager.Instance.SwitchFurnitureData(
            containedData
        );
    }
}
