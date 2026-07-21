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

    // 버튼에 가구 데이터(이름, 이미지)를 주입하는 함수
    public void Setup(FurnitureData data)
    {
        containedData = data;

        // 이름 ui 연결
        if (nameText != null)
        {
            nameText.text = data.furnitureName;
        }
        
        // 아이콘 이미지 ui 연동
        if (iconImage != null && data.furnitureIcon != null )
        {
            iconImage.sprite = data.furnitureIcon;
        }
    }

    private void Awake()
    {
        // 버튼 컴포넌트를 가져와 클릭 이벤트를 연결
        Button btn = GetComponent<Button>();
        if ( btn != null )
        {
            btn.onClick.AddListener(OnClickItem);
        }
    }

    private void OnClickItem()
    {
        // 버튼이 눌리면 GameManager에게 가구 데이터 전환 요청
        if (GameManager.Instance != null && containedData != null)
        {
            GameManager.Instance.SwitchFurnitureData(containedData);
        }
    }
}
