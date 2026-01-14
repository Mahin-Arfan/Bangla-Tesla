using UnityEngine;
using System.Collections;

public class PooledImpactEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    public float shakeIntensity = 0.2f;
    public float effectDuration = 0.5f;
    private Vector3 originalPosition;
    public void Activate(Vector3 position)
    {
        originalPosition = position;
        transform.position = position;
        gameObject.SetActive(true);

        StartCoroutine(DisableAfterTime());
    }

    void LateUpdate()
    {
        Vector3 randomShake = Random.insideUnitSphere * shakeIntensity;
        transform.position = originalPosition + randomShake;
        FaceCamera();
    }

    void FaceCamera()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                         Camera.main.transform.rotation * Vector3.up);
    }

    IEnumerator DisableAfterTime()
    {
        yield return new WaitForSeconds(effectDuration);
        gameObject.SetActive(false);
    }
}
