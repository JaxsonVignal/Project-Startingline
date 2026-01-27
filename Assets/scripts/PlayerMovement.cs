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

        // NEW: Subscribe to save/load events
        SaveLoad.OnSaveGame += SavePlayerPosition;
        SaveLoad.OnLoadGame += LoadPlayerPosition;
    }

    private void OnDestroy()
    {
        // NEW: Unsubscribe from events
        SaveLoad.OnSaveGame -= SavePlayerPosition;
        SaveLoad.OnLoadGame -= LoadPlayerPosition;
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
        uiMode = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (PlayerShooting.Instance != null)
            PlayerShooting.Instance.SetUIMode(true);
    }

    private void SetGameplayMode()
    {
        uiMode = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (PlayerShooting.Instance != null)
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

    // NEW: Save player position to SaveData
    private void SavePlayerPosition()
    {
        if (SaveGameManager.data != null)
        {
            SaveGameManager.data.playerPosX = transform.position.x;
            SaveGameManager.data.playerPosY = transform.position.y;
            SaveGameManager.data.playerPosZ = transform.position.z;
            SaveGameManager.data.playerRotY = transform.eulerAngles.y;

            Debug.Log($"Saved player position: ({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2}), Rotation Y: {transform.eulerAngles.y:F2}");
        }
    }

    // NEW: Load player position from SaveData
    private void LoadPlayerPosition(SaveData data)
    {
        if (data != null)
        {
            Vector3 loadedPosition = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);

            // IMPORTANT: Disable CharacterController before teleporting
            if (cc != null)
            {
                cc.enabled = false;
                transform.position = loadedPosition;
                transform.rotation = Quaternion.Euler(0f, data.playerRotY, 0f);
                cc.enabled = true;
            }
            else
            {
                transform.position = loadedPosition;
                transform.rotation = Quaternion.Euler(0f, data.playerRotY, 0f);
            }

            // Reset vertical look rotation
            verticalLookRotation = 0f;
            if (cameraHolder != null)
            {
                cameraHolder.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }

            Debug.Log($"Loaded player position: ({loadedPosition.x:F2}, {loadedPosition.y:F2}, {loadedPosition.z:F2}), Rotation Y: {data.playerRotY:F2}");
        }
    }
}