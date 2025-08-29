using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Refs")]
    private CharacterController cc;
    [SerializeField] private Transform cameraHolder;

    [Header("Input")]
    private float moveInput;
    private float turnInput;
    private float mouseX;
    private float mouseY;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float Gravity = 9.8f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float sprintTransitionSpeed = 5f;
    private float verticalVelocity;
    private float speed;
    private float verticalLookRotation = 0f;

   
    private bool uiMode = false;

    private void Start()
    {
        cc = GetComponent<CharacterController>();
        SetGameplayMode(); // start locked & hidden
    }

    private void Update()
    {
        // Toggle UI mode with Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            uiMode = !uiMode;
            if (uiMode) SetUIMode();
            else SetGameplayMode();
        }

        if (!uiMode)
        {
            inputManager();
            Movement();
            CameraLook();
        }
    }

    private void inputManager()
    {
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");

        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
    }

    private void Movement()
    {
        groundMovement();
    }

    private void groundMovement()
    {
        Vector3 move = transform.right * turnInput + transform.forward * moveInput;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = Mathf.Lerp(speed, sprintSpeed, sprintTransitionSpeed * Time.deltaTime);
        }
        else
        {
            speed = Mathf.Lerp(speed, walkSpeed, sprintTransitionSpeed * Time.deltaTime);
        }

        move *= speed;

        // Apply gravity BEFORE moving
        move.y = verticalForceCalculator();

        cc.Move(move * Time.deltaTime);
    }

    private void CameraLook()
    {
        // Rotate player horizontally
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera vertically
        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
        cameraHolder.localRotation = Quaternion.Euler(verticalLookRotation, 0f, 0f);
    }

    private float verticalForceCalculator()
    {
        if (cc.isGrounded)
        {
            verticalVelocity = -1f;

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * Gravity * 2);
            }
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
    }

    private void SetGameplayMode()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
