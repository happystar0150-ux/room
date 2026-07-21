using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    [Header("기본 장착 파츠")]
    public GameObject defaultEyePrefab; // 기본 눈
    public GameObject defaultMouthPrefab; // 기본 입

    [Header("현재 장착된 파츠")]
    public GameObject currentHair; // 현재 적용된 머리카락 오브젝트 저장용

    private void Start()
    {
        // 눈이랑 입 소환
        SpawnPart(defaultEyePrefab);
        SpawnPart(defaultMouthPrefab);

        // 그 외 추가할 것 있으면 추가

    }

    // 파츠 소환하고 뼈 연결
    public GameObject SpawnPart(GameObject prefab)
    {
        if (prefab == null) return null;

        GameObject part = Instantiate(prefab, transform); // 본체의 자식으로 생성
        var remapper = part.GetComponent<BoneRemapper>();

        if (remapper != null)
        {
            remapper.MapButtons(this.transform); // 뼈 연결
        }
        return part;
    }

    // 머리카락 교체
    public void ChangeHair(GameObject hairPrefab)
    {
        if (currentHairPrefab == hairPrefab) return; // 이미 입고 있으면 무시

        if (currentHair != null) // 기존에 있던 머리카락 삭제
        {
            Destroy(currentHair);
        }

        currentHair = SpawnPart(hairPrefab); // 새로운 머리카락 저장
        currentHairPrefab = hairPrefab; // 현재 입은 프리팹 정보 업데이트

        // 캐릭터에 붙은 애니메이터를 다시 찾아서 연결
        RefreshAnimator();
    }

    // 현재 입고 있는 프리팹이 뭔지 기억
    private GameObject currentHairPrefab;

    public GameObject GetCurrentHairPrefab()
    {
        return currentHairPrefab;
    }

    // 머리카락 삭제
    public void RemoveHair()
    {
        if (currentHair != null)
        {
            Destroy(currentHair);
            currentHair = null;
            currentHairPrefab = null;

            RefreshAnimator();
        }
    }

    private void RefreshAnimator()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
        }
    }
    
}
