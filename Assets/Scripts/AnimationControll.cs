using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationControll : MonoBehaviour
{

    // ��ư ��Ÿ �� �ִϸ��̼� ���� ���������� ������ ��ũ��Ʈ �Դϴٶ���

    // �ʿ��� ��ư�� ���� �ٿ��ּ���


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
