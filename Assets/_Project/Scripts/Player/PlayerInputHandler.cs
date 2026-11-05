using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AWildSheepChase.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        private InputAction moveAction;
        private InputAction lookStickAction;
        private InputAction pointerPositionAction;
        private InputAction pointerDeltaAction;
        private InputAction jumpAction;
        private InputAction dashAction;
        private InputAction attackAction;
        private InputAction rangedAction;
        private InputAction useItemAction;

        public Vector2 Move => moveAction.ReadValue<Vector2>();
        public Vector2 LookStick => lookStickAction.ReadValue<Vector2>();
        public Vector2 PointerPosition => pointerPositionAction.ReadValue<Vector2>();

        /// <summary>true เมื่อผู้เล่นใช้ gamepad ล่าสุด (ใช้เลือกว่าจะเล็งด้วยเมาส์หรือ stick)</summary>
        public bool UsingGamepad { get; private set; }

        public event Action JumpPressed;
        public event Action DashPressed;
        public event Action AttackPressed;
        public event Action RangedPressed;
        public event Action UseItemPressed;

        private void Awake()
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick", processors: "stickDeadzone");

            lookStickAction = new InputAction("LookStick", InputActionType.Value);
            lookStickAction.AddBinding("<Gamepad>/rightStick", processors: "stickDeadzone");

            pointerPositionAction = new InputAction("PointerPosition", InputActionType.Value);
            pointerPositionAction.AddBinding("<Mouse>/position");

            pointerDeltaAction = new InputAction("PointerDelta", InputActionType.Value);
            pointerDeltaAction.AddBinding("<Mouse>/delta");

            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            dashAction = new InputAction("Dash", InputActionType.Button);
            dashAction.AddBinding("<Keyboard>/leftShift");
            dashAction.AddBinding("<Gamepad>/buttonEast");

            attackAction = new InputAction("Attack", InputActionType.Button);
            attackAction.AddBinding("<Mouse>/leftButton");
            attackAction.AddBinding("<Gamepad>/buttonWest");

            rangedAction = new InputAction("Ranged", InputActionType.Button);
            rangedAction.AddBinding("<Mouse>/rightButton");
            rangedAction.AddBinding("<Gamepad>/rightTrigger");

            useItemAction = new InputAction("UseItem", InputActionType.Button);
            useItemAction.AddBinding("<Keyboard>/q");
            useItemAction.AddBinding("<Gamepad>/buttonNorth");
        }

        private void OnEnable()
        {
            jumpAction.performed += OnJumpPerformed;
            dashAction.performed += OnDashPerformed;
            attackAction.performed += OnAttackPerformed;
            rangedAction.performed += OnRangedPerformed;
            useItemAction.performed += OnUseItemPerformed;
            lookStickAction.performed += OnLookStickPerformed;
            pointerDeltaAction.performed += OnPointerDeltaPerformed;

            moveAction.Enable();
            lookStickAction.Enable();
            pointerPositionAction.Enable();
            pointerDeltaAction.Enable();
            jumpAction.Enable();
            dashAction.Enable();
            attackAction.Enable();
            rangedAction.Enable();
            useItemAction.Enable();
        }

        private void OnDisable()
        {
            jumpAction.performed -= OnJumpPerformed;
            dashAction.performed -= OnDashPerformed;
            attackAction.performed -= OnAttackPerformed;
            rangedAction.performed -= OnRangedPerformed;
            useItemAction.performed -= OnUseItemPerformed;
            lookStickAction.performed -= OnLookStickPerformed;
            pointerDeltaAction.performed -= OnPointerDeltaPerformed;

            moveAction.Disable();
            lookStickAction.Disable();
            pointerPositionAction.Disable();
            pointerDeltaAction.Disable();
            jumpAction.Disable();
            dashAction.Disable();
            attackAction.Disable();
            rangedAction.Disable();
            useItemAction.Disable();
        }

        private void OnDestroy()
        {
            moveAction?.Dispose();
            lookStickAction?.Dispose();
            pointerPositionAction?.Dispose();
            pointerDeltaAction?.Dispose();
            jumpAction?.Dispose();
            dashAction?.Dispose();
            attackAction?.Dispose();
            rangedAction?.Dispose();
            useItemAction?.Dispose();
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            UsingGamepad = ctx.control.device is Gamepad;
            JumpPressed?.Invoke();
        }

        private void OnDashPerformed(InputAction.CallbackContext ctx)
        {
            UsingGamepad = ctx.control.device is Gamepad;
            DashPressed?.Invoke();
        }

        private void OnAttackPerformed(InputAction.CallbackContext ctx)
        {
            UsingGamepad = ctx.control.device is Gamepad;
            AttackPressed?.Invoke();
        }

        private void OnRangedPerformed(InputAction.CallbackContext ctx)
        {
            UsingGamepad = ctx.control.device is Gamepad;
            RangedPressed?.Invoke();
        }

        private void OnUseItemPerformed(InputAction.CallbackContext ctx)
        {
            UsingGamepad = ctx.control.device is Gamepad;
            UseItemPressed?.Invoke();
        }

        private void OnLookStickPerformed(InputAction.CallbackContext ctx) => UsingGamepad = true;

        private void OnPointerDeltaPerformed(InputAction.CallbackContext ctx) => UsingGamepad = false;
    }
}