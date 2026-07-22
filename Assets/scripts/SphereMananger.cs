using UnityEngine;

public class SphereManager : MonoBehaviour
{
    public SphereTrigger sphere1;
    public SphereTrigger sphere2;

    public bool bothTriggered = false;

    void Update()
    {
        bothTriggered = sphere1.isTriggered && sphere2.isTriggered;
        Debug.Log("둘 다 트리거 상태: " + bothTriggered);
    }
}