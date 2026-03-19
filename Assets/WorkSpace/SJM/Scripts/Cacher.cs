using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;

//TODO : 주요 클래스들의 호출 타이밍을 정확하게 조절하지 못해 생긴 문제.
//       초기화 순서를 정확하게 조절하고, 초기화가 완료되었는지 확인하는 방법을 찾아야함.
//       각 동적 클래스에 들어가는 Getter의 FindObjectOfType 호출을 최소화 하기 위한 임시방편.
public static class Cacher
{
    static GameManager gm;

    static Cacher()
    {
        gm = GameObject.FindObjectOfType<GameManager>();
    }
    public static GameManager gameManager
    {
        get
        {
            return gm;
        }
    }
}