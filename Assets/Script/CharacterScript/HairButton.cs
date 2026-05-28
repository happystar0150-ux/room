using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HairButton : MonoBehaviour
{
    public GameObject hairPrefab; // 이 버튼이 입혀줄 머리카락 프리팹
    private Button button;
    private CharacterManager manager;

    private void Awake()
    {
        button = GetComponent<Button>();
        manager = FindFirstObjectByType<CharacterManager>(); // 씬에서 매니저 찾기

        // 버튼 클릭 시 이벤트 연결
        button.onClick.AddListener(OnClickHairButton);

        // 상태 체크 (현재 입고 있다면 버튼 비활성화)
        UpdateButtonState();
    }

    void OnClickHairButton()
    {
        // 매니저에게 머리카락 바꿔주세요 요청
        manager.ChangeHair(hairPrefab);

        // 모든 버튼 상태 업데이트
        HairButton[] allButtons = FindObjectsByType<HairButton>(FindObjectsSortMode.None);
        foreach (var btn in allButtons)
        {
            btn.UpdateButtonState();
        }

        // 머리카락 없애기 버튼도 갱신
        RemoveHairButton removeBtn = FindFirstObjectByType<RemoveHairButton>();
        if (removeBtn != null) removeBtn.UpdateButtonState();
    }

    // 현재 입고 있는 것과 같으면 버튼 비활성화
    public void UpdateButtonState()
    {
        if (manager == null || button == null) return;

        // 현재 매니저가 입고 있는 머리카락과 내 프리팹 이름을 비교
        // 같으면 버튼을 비활성화, 다르면 활성화
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
