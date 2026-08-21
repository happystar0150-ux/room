using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationControll : MonoBehaviour
{

    // 버튼 연타 시 애니메이션 끊김 방지용으로 제작한 스크립트 입니다람쥐

    // 필요한 버튼에 갖다 붙여주세요


    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayMashingAnimation()
    {
        animator.Play("Pressed", -1, 0f);
    }
    
    
}
