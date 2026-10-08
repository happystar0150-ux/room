using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFurnitureData", menuName = "Scriptable/FurnitureData")]
public class FurnitureData : ScriptableObject
{
    public string furnitureName; // ���� �̸�
    public GameObject furniturePrefab;

    // ����Ƽ �ν����� â���� �� ������ � �������� ��� �� ����
    public string categoryGroup;

    // ���� ��� ui�� ��� �̸����� �̹���
    public Sprite furnitureIcon;

    
}