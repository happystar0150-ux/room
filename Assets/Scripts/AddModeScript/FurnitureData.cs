using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFurnitureData", menuName = "Scriptable/FurnitureData")]
public class FurnitureData : ScriptableObject
{
    public string furnitureName; // 가구 이름
    public GameObject furniturePrefab;

    // 유니티 인스펙터 창에서 이 가구가 어떤 종류인지 고를 수 있음
    public string categoryGroup;

    // 가구 목록 ui에 띄울 미리보기 이미지
    public Sprite furnitureIcon;

    
}