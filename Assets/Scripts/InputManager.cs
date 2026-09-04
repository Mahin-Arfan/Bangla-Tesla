using DG.Tweening.Core.Easing;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public bool boostPressed { get; private set; }
    public bool brakePressed { get; private set; }
    public bool rightPressed { get; private set; }
    public bool leftPressed { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
#if UNITY_EDITOR //KeyboardControl

        if (Input.GetKeyDown(KeyCode.W))
        {
            BoostButtonDown();
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            BoostButtonUp();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            BreakButtonDown();
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            BreakButtonUp();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            LeftButtonDown();
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            LeftButtonUp();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            RightButtonDown();
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            RightButtonUp();
        }

#endif
    }

    public void LeftButtonDown() { leftPressed = true; }
    public void LeftButtonUp() { leftPressed = false; }
    public void RightButtonDown() { rightPressed = true; }
    public void RightButtonUp() { rightPressed = false; }
    public void BoostButtonUp() { boostPressed = false; }
    public void BoostButtonDown() { boostPressed = true; }
    public void BreakButtonUp() { brakePressed = false; }
    public void BreakButtonDown() { brakePressed = true; }
}
