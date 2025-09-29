using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private GameInput gameInput;

    // Exposed input values
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool ToggleUIPressed { get; private set; }
    public bool InteractPressed { get; private set; }

    private void Awake()
    {
        gameInput = new GameInput();

        // Bind actions
        gameInput.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        gameInput.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        gameInput.Player.Look.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
        gameInput.Player.Look.canceled += ctx => LookInput = Vector2.zero;

        gameInput.Player.Jump.performed += ctx => JumpPressed = true;
        gameInput.Player.Jump.canceled += ctx => JumpPressed = false;

        gameInput.Player.Sprint.performed += ctx => SprintHeld = true;
        gameInput.Player.Sprint.canceled += ctx => SprintHeld = false;

        gameInput.Player.ToggleUI.performed += ctx => ToggleUIPressed = true;

        gameInput.Player.Interact.performed += ctx => InteractPressed = true;
        gameInput.Player.Interact.canceled += ctx => InteractPressed = false;
    }

    private void OnEnable() => gameInput.Enable();
    private void OnDisable() => gameInput.Disable();

    // Optional: reset one-shot inputs (like UI toggle) after reading
    public void ResetToggleUI() => ToggleUIPressed = false;
}
