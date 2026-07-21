using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneRemapper : MonoBehaviour
{
    // 뼈와 연결해요

    // 새로 불러오는 파츠에 연결해요

    public void MapButtons(Transform targetVisualRoot)
    {
        // 캐릭터 본체(targetVisualRoot)의 뼈 정보를 저장
        var boneMap = new Dictionary<string, Transform>();
        foreach (var bone in targetVisualRoot.GetComponentsInChildren<Transform>())
        {
            if (!boneMap.ContainsKey(bone.name))
                boneMap[bone.name] = bone;
        }

        // 파츠가 가진 SkinnedMeshRenderer를 찾아요
        var smr = GetComponentInChildren<SkinnedMeshRenderer>();
        

        if (smr == null)
        {
            Debug.LogError($"{gameObject.name}에 SkinnedMeshRenderer가 없습니다");
            return;
        }

        var newBones = new Transform[smr.bones.Length];

        // 일치하는 뼈 이름을 찾아 매칭 해줘요
        for (int i = 0; i < smr.bones.Length; i++)
        {
            string boneName = smr.bones[i].name;
            if (boneMap.TryGetValue(boneName, out var targetBone))
            {
                newBones[i] = targetBone;
            }
            else
            {
                Debug.LogWarning($"{boneName} 뼈를 본체에서 찾을 수 없습니다");
            }
        }

        // 바꾸기 완료
        smr.bones = newBones;

        // 파츠의 루트 본도 본체 것으로 맞춰줌
        if (boneMap.TryGetValue(smr.rootBone.name, out var rootBone))
        {
            smr.rootBone = rootBone;
        }
    }
}
