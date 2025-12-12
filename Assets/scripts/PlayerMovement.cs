using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Refs")]
    private CharacterController cc;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private Interactor interactor;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField, Range(0.1f, 5f)] private float sensitivityModifier = 1f;
    [SerializeField] private float Gravity = 9.8f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float sprintTransitionSpeed = 5f;

    private float verticalVelocity;
    private float speed;
    private float verticalLookRotation = 0f;
    private bool uiMode = false;
    public bool isWanted = false;
    private void Start()
    {
        cc = GetComponent<CharacterController>();
        SetGameplayMode();
    }

    private void Update()
    {
        if (inputHandler.ToggleUIPressed)
        {
            uiMode = !uiMode;
            if (uiMode) SetUIMode();
            else SetGameplayMode();
            inputHandler.ResetToggleUI();
        }
       
        if (interactor.isInteracting)
        {
            SetUIMode();
            return;
        }

        if (!uiMode)
        {
            HandleMovement();
            HandleLook();
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = inputHandler.MoveInput;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (inputHandler.SprintHeld)
            speed = Mathf.Lerp(speed, sprintSpeed, sprintTransitionSpeed * Time.deltaTime);
        else
            speed = Mathf.Lerp(speed, walkSpeed, sprintTransitionSpeed * Time.deltaTime);

        move *= speed;
        move.y = CalculateVerticalVelocity();
        cc.Move(move * Time.deltaTime);
    }

    private void HandleLook()
    {
        float factor = 50f;
        Vector2 look = inputHandler.LookInput * mouseSensitivity * sensitivityModifier * Time.deltaTime * factor;
        transform.Rotate(Vector3.up * look.x);
        verticalLookRotation -= look.y;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
        cameraHolder.localRotation = Quaternion.Euler(verticalLookRotation, 0f, 0f);
    }

    private float CalculateVerticalVelocity()
    {
        if (cc.isGrounded)
        {
            verticalVelocity = -1f;
            if (inputHandler.JumpPressed)
                verticalVelocity = Mathf.Sqrt(jumpHeight * Gravity * 2);
        }
        else
        {
            verticalVelocity -= Gravity * Time.deltaTime;
        }
        return verticalVelocity;
    }

    private void SetUIMode()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // NEW: Notify PlayerShooting that UI mode is active
        PlayerShooting.Instance.SetUIMode(true);
    }

    private void SetGameplayMode()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // NEW: Notify PlayerShooting that UI mode is inactive
        PlayerShooting.Instance.SetUIMode(false);
    }


    public void EnableUIMode()
    {
        uiMode = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void EnableGameplayMode()
    {
        uiMode = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}