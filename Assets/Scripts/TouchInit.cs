using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public class TouchInit : MonoBehaviour
{
    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }
}