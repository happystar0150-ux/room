using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RemoveHairButton : MonoBehaviour
{
    private Button button;
    private CharacterManager manager;

    void Start()
    {
        button = GetComponent<Button>();
        manager = FindFirstObjectByType<CharacterManager>();

        button.onClick.AddListener(OnClickRemove);
    }

    void OnClickRemove()
    {
        // 머리카락 제거 실행
        manager.RemoveHair();

        // 다른 머리카락 버튼 재활성화
        HairButton[] allButtons = FindObjectsByType<HairButton>(FindObjectsSortMode.None);
        foreach (var btn in allButtons)
        {
            btn.UpdateButtonState();
        }

        // 본인 비활성화
        button.interactable = false;
    }

    // 다른 머리카락 버튼 눌렀을 때 다시 활성화
    public void UpdateButtonState()
    {
        // 현재 입고 있는 머리카락이 없으면 비활성화
        button.interactable = (manager.GetCurrentHairPrefab() != null);
    }
}
