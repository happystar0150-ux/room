using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CategoryButton : MonoBehaviour
{
    // 버튼이 담당할 카테고리 글자
    [Header("카테고리 지정")]
    public string categoryName;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if (btn != null )
        {
            // 버튼을 클릭하면 해당하는 카테고리 가구를 보여줌
            btn.onClick.AddListener(OnClickCategory);
        }
    }

    private void OnClickCategory()
    {
        // GameManager에게 이 카테고리 글자를 던져주며 목록을 갱신하라고 지시
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FilterFurnitureMenu(categoryName);
        }
    }
}
