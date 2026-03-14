using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Kuuila.FPS.Interaction
{
    /// <summary>
    /// 交互系统
    /// 处理玩家与场景物体的交互
    /// </summary>
    public class InteractionSystem : MonoBehaviour
    {
        [Header("交互设置")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private float interactionCooldown = 0.5f;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        
        [Header("射线设置")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private bool useCenterScreen = true;
        
        [Header("UI设置")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TMPro.TMP_Text promptText;
        
        // 状态
        private IInteractable currentInteractable;
        private float lastInteractionTime;
        private bool canInteract = true;
        
        // 事件
        public event Action<IInteractable> OnTargetChanged;
        public event Action<IInteractable> OnInteract;
        
        // 属性
        public IInteractable CurrentInteractable => currentInteractable;
        public bool HasInteractable => currentInteractable != null;
        
        private void Start()
        {
            // 默认使用主摄像机
            if (rayOrigin == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    rayOrigin = cam.transform;
                }
            }
            
            // 隐藏提示
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }
        
        private void Update()
        {
            if (!canInteract) return;
            
            // 检测可交互物体
            CheckForInteractable();
            
            // 处理交互输入
            HandleInput();
        }
        
        private void CheckForInteractable()
        {
            IInteractable interactable = null;
            
            if (useCenterScreen && rayOrigin != null)
            {
                // 从屏幕中心发射射线
                Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer))
                {
                    interactable = hit.collider.GetComponent<IInteractable>();
                }
            }
            else
            {
                // 从角色前方发射射线
                Ray ray = new Ray(transform.position, transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer))
                {
                    interactable = hit.collider.GetComponent<IInteractable>();
                }
            }
            
            // 更新当前可交互物体
            if (interactable != currentInteractable)
            {
                currentInteractable = interactable;
                OnTargetChanged?.Invoke(currentInteractable);
                UpdatePrompt();
            }
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(interactionKey) && currentInteractable != null)
            {
                Interact();
            }
        }
        
        /// <summary>
        /// 执行交互
        /// </summary>
        public void Interact()
        {
            if (currentInteractable == null) return;
            if (Time.time - lastInteractionTime < interactionCooldown) return;
            
            // 执行交互
            bool success = currentInteractable.Interact(this);
            
            if (success)
            {
                lastInteractionTime = Time.time;
                OnInteract?.Invoke(currentInteractable);
                
                // 触发冷却
                StartCoroutine(CooldownCoroutine());
            }
        }
        
        private IEnumerator CooldownCoroutine()
        {
            canInteract = false;
            yield return new WaitForSeconds(interactionCooldown);
            canInteract = true;
        }
        
        private void UpdatePrompt()
        {
            if (interactionPrompt == null) return;
            
            if (currentInteractable != null)
            {
                interactionPrompt.SetActive(true);
                
                if (promptText != null)
                {
                    promptText.text = $"[{interactionKey}] {currentInteractable.GetInteractionPrompt()}";
                }
            }
            else
            {
                interactionPrompt.SetActive(false);
            }
        }
        
        /// <summary>
        /// 强制交互
        /// </summary>
        public void ForceInteract(IInteractable interactable)
        {
            if (interactable != null)
            {
                currentInteractable = interactable;
                Interact();
            }
        }
        
        /// <summary>
        /// 设置交互范围
        /// </summary>
        public void SetInteractionRange(float range)
        {
            interactionRange = range;
        }
        
        /// <summary>
        /// 禁用交互
        /// </summary>
        public void DisableInteraction()
        {
            canInteract = false;
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }
        
        /// <summary>
        /// 启用交互
        /// </summary>
        public void EnableInteraction()
        {
            canInteract = true;
        }
        
        private void OnDrawGizmosSelected()
        {
            // 可视化交互范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
            
            // 可视化射线
            if (rayOrigin != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(rayOrigin.position, rayOrigin.position + rayOrigin.forward * interactionRange);
            }
        }
    }
    
    /// <summary>
    /// 可交互接口
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// 执行交互
        /// </summary>
        /// <returns>是否成功交互</returns>
        bool Interact(InteractionSystem interactor);
        
        /// <summary>
        /// 获取交互提示文本
        /// </summary>
        string GetInteractionPrompt();
        
        /// <summary>
        /// 是否可以交互
        /// </summary>
        bool CanInteract(InteractionSystem interactor);
    }
    
    /// <summary>
    /// 可拾取物品
    /// </summary>
    public interface IPickable : IInteractable
    {
        /// <summary>
        /// 获取物品数据
        /// </summary>
        object GetPickupData();
        
        /// <summary>
        /// 拾取时的效果
        /// </summary>
        void OnPickedUp(InteractionSystem interactor);
    }
    
    /// <summary>
    /// 可开关物品
    /// </summary>
    public interface IToggleable : IInteractable
    {
        /// <summary>
        /// 是否处于开启状态
        /// </summary>
        bool IsOn { get; }
        
        /// <summary>
        /// 切换状态
        /// </summary>
        void Toggle(InteractionSystem interactor);
    }
}
