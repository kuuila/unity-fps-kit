using UnityEngine;
using System;

namespace Kuuila.FPS.Core
{
    /// <summary>
    /// FPS控制器 - 第一人称移动、视角、物理
    /// 支持蹲伏、跳跃、冲刺
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float acceleration = 10f;
        
        [Header("视角设置")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 90f;
        [SerializeField] private float minLookAngle = -90f;
        [SerializeField] private Transform cameraHolder;
        
        [Header("跳跃设置")]
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCheckDistance = 0.2f;
        
        [Header("蹲伏设置")]
        [SerializeField] private float crouchHeight = 1f;
        [SerializeField] private float normalHeight = 2f;
        [SerializeField] private float crouchTransitionSpeed = 10f;
        
        [Header("冲刺设置")]
        [SerializeField] private float sprintFOV = 75f;
        [SerializeField] private float normalFOV = 60f;
        [SerializeField] private float fovTransitionSpeed = 10f;
        
        // 组件引用
        private CharacterController characterController;
        private Camera playerCamera;
        
        // 状态
        private Vector3 velocity;
        private float verticalRotation;
        private bool isGrounded;
        private bool isCrouching;
        private bool isSprinting;
        private float currentSpeed;
        private float targetHeight;
        
        // 输入
        private float horizontalInput;
        private float verticalInput;
        private float mouseX;
        private float mouseY;
        
        // 事件
        public event Action<bool> OnGroundStateChanged;
        public event Action<bool> OnCrouchStateChanged;
        public event Action<bool> OnSprintStateChanged;
        public event Action OnJump;
        
        // 属性
        public bool IsGrounded => isGrounded;
        public bool IsCrouching => isCrouching;
        public bool IsSprinting => isSprinting;
        public Vector3 Velocity => velocity;
        public float CurrentSpeed => currentSpeed;
        
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerCamera = cameraHolder.GetComponentInChildren<Camera>();
            
            if (playerCamera == null)
            {
                Debug.LogError("[FPSController] 未找到摄像机!");
            }
            
            targetHeight = normalHeight;
            currentSpeed = walkSpeed;
            
            // 锁定光标
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        private void Update()
        {
            HandleInput();
            CheckGround();
            HandleMovement();
            HandleLook();
            HandleCrouch();
            HandleSprintFOV();
        }
        
        private void HandleInput()
        {
            // 移动输入
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
            
            // 鼠标输入
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            // 跳跃
            if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
            {
                Jump();
            }
            
            // 蹲伏
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
            {
                ToggleCrouch();
            }
            
            // 冲刺
            bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && !isCrouching && isGrounded;
            SetSprint(wantsSprint);
        }
        
        private void CheckGround()
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics.CheckSphere(transform.position + Vector3.down * 0.1f, 
                                            groundCheckDistance, groundMask);
            
            if (wasGrounded != isGrounded)
            {
                OnGroundStateChanged?.Invoke(isGrounded);
            }
        }
        
        private void HandleMovement()
        {
            // 计算目标速度
            float targetSpeed = isCrouching ? crouchSpeed : 
                               (isSprinting ? sprintSpeed : walkSpeed);
            
            // 平滑过渡速度
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
            
            // 计算移动方向
            Vector3 moveDirection = transform.right * horizontalInput + 
                                   transform.forward * verticalInput;
            moveDirection.Normalize();
            
            // 应用移动
            Vector3 horizontalVelocity = moveDirection * currentSpeed;
            
            // 重力
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -0.5f;
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
            }
            
            // 组合速度
            Vector3 finalVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
            characterController.Move(finalVelocity * Time.deltaTime);
        }
        
        private void HandleLook()
        {
            // 水平旋转（身体）
            transform.Rotate(Vector3.up * mouseX);
            
            // 垂直旋转（摄像机）
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, minLookAngle, maxLookAngle);
            
            if (cameraHolder != null)
            {
                cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            }
        }
        
        private void Jump()
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            OnJump?.Invoke();
        }
        
        private void ToggleCrouch()
        {
            SetCrouch(!isCrouching);
        }
        
        public void SetCrouch(bool crouch)
        {
            if (isCrouching == crouch) return;
            
            isCrouching = crouch;
            targetHeight = isCrouching ? crouchHeight : normalHeight;
            
            // 如果正在冲刺，取消冲刺
            if (isCrouching && isSprinting)
            {
                SetSprint(false);
            }
            
            OnCrouchStateChanged?.Invoke(isCrouching);
        }
        
        private void HandleCrouch()
        {
            // 平滑过渡高度
            float currentHeight = characterController.height;
            float newHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            
            // 调整中心点
            float heightDelta = newHeight - currentHeight;
            characterController.height = newHeight;
            characterController.center = new Vector3(0, newHeight / 2f, 0);
            
            // 调整摄像机位置
            if (cameraHolder != null)
            {
                Vector3 camPos = cameraHolder.localPosition;
                camPos.y = Mathf.Lerp(camPos.y, targetHeight - 0.2f, crouchTransitionSpeed * Time.deltaTime);
                cameraHolder.localPosition = camPos;
            }
        }
        
        private void SetSprint(bool sprint)
        {
            if (isSprinting == sprint) return;
            
            // 不能蹲伏时冲刺
            if (sprint && isCrouching) return;
            
            isSprinting = sprint;
            OnSprintStateChanged?.Invoke(isSprinting);
        }
        
        private void HandleSprintFOV()
        {
            if (playerCamera == null) return;
            
            float targetFOV = isSprinting ? sprintFOV : normalFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
        }
        
        // 公共方法
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = true;
        }
        
        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = sensitivity;
        }
        
        public void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
        
        private void OnDrawGizmosSelected()
        {
            // 地面检测可视化
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.1f, groundCheckDistance);
        }
    }
}
