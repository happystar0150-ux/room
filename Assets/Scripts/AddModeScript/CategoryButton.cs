using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CategoryButton : MonoBehaviour
{
    // ��ư�� ����� ī�װ�� ����
    [Header("ī�װ�� ����")]
    public string categoryName;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if (btn != null )
        {
            // ��ư�� Ŭ���ϸ� �ش��ϴ� ī�װ�� ������ ������
            btn.onClick.AddListener(OnClickCategory);
        }
    }

    private void OnClickCategory()
    {
        // GameManager���� �� ī�װ�� ���ڸ� �����ָ� ����� �����϶�� ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FilterFurnitureMenu(categoryName);
        }
    }
}
