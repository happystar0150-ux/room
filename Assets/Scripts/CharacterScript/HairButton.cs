using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HairButton : MonoBehaviour
{
    public GameObject hairPrefab; // �� ��ư�� ������ �Ӹ�ī�� ������
    private Button button;
    private CharacterManager manager;

    private void Awake()
    {
        button = GetComponent<Button>();
        manager = FindFirstObjectByType<CharacterManager>(); // ������ �Ŵ��� ã��

        // ��ư Ŭ�� �� �̺�Ʈ ����
        button.onClick.AddListener(OnClickHairButton);

        // ���� üũ (���� �԰� �ִٸ� ��ư ��Ȱ��ȭ)
        UpdateButtonState();
    }

    void OnClickHairButton()
    {
        // �Ŵ������� �Ӹ�ī�� �ٲ��ּ��� ��û
        manager.ChangeHair(hairPrefab);

        // ��� ��ư ���� ������Ʈ
        HairButton[] allButtons = FindObjectsByType<HairButton>(FindObjectsSortMode.None);
        foreach (var btn in allButtons)
        {
            btn.UpdateButtonState();
        }

        // �Ӹ�ī�� ���ֱ� ��ư�� ����
        RemoveHairButton removeBtn = FindFirstObjectByType<RemoveHairButton>();
        if (removeBtn != null) removeBtn.UpdateButtonState();
    }

    // ���� �԰� �ִ� �Ͱ� ������ ��ư ��Ȱ��ȭ
    public void UpdateButtonState()
    {
        if (manager == null || button == null) return;

        // ���� �Ŵ����� �԰� �ִ� �Ӹ�ī��� �� ������ �̸��� ��
        // ������ ��ư�� ��Ȱ��ȭ, �ٸ��� Ȱ��ȭ
        if (manager.GetCurrentHairPrefab() == hairPrefab)
        {
            button.interactable = false;
        }
        else
        {
            button.interactable = true;
        }
    }

   
}
