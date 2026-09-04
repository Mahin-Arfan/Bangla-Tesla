using Unity.VisualScripting;
using UnityEngine;

public class BillBoardScript : MonoBehaviour
{
    private Camera mainCamera;

    [Header("Settings")]
    [Tooltip("If true, the UI only rotates horizontally (useful for standing markers).")]
    [SerializeField] private bool lockYAxis = false;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;
        float distanceToPlayer = Vector3.Distance(transform.position, mainCamera.transform.position);
        if(distanceToPlayer < 2f)
        {
            gameObject.SetActive(false);
            return;
        }

        if (lockYAxis)
        {
            Vector3 targetPosition = new Vector3(mainCamera.transform.position.x, transform.position.y, mainCamera.transform.position.z);
            transform.LookAt(targetPosition);
            transform.Rotate(0, 180, 0);
        }
        else
        {
            transform.forward = mainCamera.transform.forward;
        }
    }
}
