using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    
    public Animator animator;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Character rotation")]
    [Tooltip("Rotation speed (deg/s)")]
    public float rotationSpeed = 720f;

    private CharacterController _characterController;

    void Start()
    {
        _characterController = GetComponent<CharacterController>();

        if (_characterController == null)
            Debug.LogWarning("No character controller attached");

        if (cameraTransform == null)
            Debug.LogWarning("No camera transform");

        //TODO: remove this
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical);

        if (inputDirection.sqrMagnitude < 0.01f)
        {
            animator.SetFloat("Speed", 0);
            return;
        }


        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();


        Vector3 moveDirection = (camForward * inputDirection.z + camRight * inputDirection.x).normalized;


        if (_characterController)
        {
            _characterController.Move(moveDirection * (moveSpeed * Time.deltaTime));
        }
        else
        {

            transform.position += moveDirection * (moveSpeed * Time.deltaTime);
        }

        animator.SetFloat("Speed", moveSpeed * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}
