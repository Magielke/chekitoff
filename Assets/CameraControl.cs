using Unity.Cinemachine;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Transform bodyTarget;   // obecny look at — tu³ów/œrodek
    [SerializeField] private Transform headTarget;   // g³owa/twarz z rigu
    [SerializeField] private float closeDistance = 3f;
    [SerializeField] private float farDistance = 8f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minRadius = 0.5f;
    [SerializeField] private float maxRadius = 10f;

    private Transform _lerpTarget;

    void Start()
    {
        // Stwórz pusty obiekt jako dynamiczny look at target
        _lerpTarget = new GameObject("DynamicLookAt").transform;
        virtualCamera.LookAt = _lerpTarget;
    }

    void Update()
    {
        // float scroll = Input.GetAxis("Mouse ScrollWheel");
        // if (scroll == 0f) return;
        //
        // var orbitalFollow = virtualCamera.GetComponent<CinemachineOrbitalFollow>();
        //
        // float newRadius = orbitalFollow.Radius - scroll * zoomSpeed;
        // orbitalFollow.Radius = Mathf.Clamp(newRadius, minRadius, maxRadius);
    }

    void LateUpdate()
    {
        float dist = Vector3.Distance(virtualCamera.transform.position, bodyTarget.position);

        float t = Mathf.InverseLerp(farDistance, closeDistance, dist);
        t = Mathf.SmoothStep(0f, 1f, t);

        _lerpTarget.position = Vector3.Lerp(bodyTarget.position, headTarget.position, t);

        // Aktualizuj te¿ Follow
        virtualCamera.Follow = _lerpTarget;
    }
}