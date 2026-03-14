using UnityEngine;
using Kuuila.FPS.Interaction;

namespace Kuuila.FPS.Interaction
{
    /// <summary>
    /// 门交互示例
    /// </summary>
    public class Door : MonoBehaviour, IToggleable
    {
        [Header("门设置")]
        [SerializeField] private bool isOpen = false;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float openSpeed = 2f;
        [SerializeField] private bool canOpenFromBothSides = true;
        
        [Header("音效")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private bool isAnimating;
        
        public bool IsOn => isOpen;
        
        private void Start()
        {
            closedRotation = transform.rotation;
            openRotation = Quaternion.Euler(transform.eulerAngles + Vector3.up * openAngle);
        }
        
        public bool Interact(InteractionSystem interactor)
        {
            if (isAnimating) return false;
            
            Toggle(interactor);
            return true;
        }
        
        public void Toggle(InteractionSystem interactor)
        {
            isOpen = !isOpen;
            StartCoroutine(AnimateDoor());
            
            // 播放音效
            if (audioSource != null)
            {
                audioSource.clip = isOpen ? openSound : closeSound;
                audioSource.Play();
            }
        }
        
        private System.Collections.IEnumerator AnimateDoor()
        {
            isAnimating = true;
            
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = isOpen ? openRotation : closedRotation;
            
            float elapsed = 0f;
            
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * openSpeed;
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed);
                yield return null;
            }
            
            transform.rotation = targetRotation;
            isAnimating = false;
        }
        
        public string GetInteractionPrompt()
        {
            return isOpen ? "关闭门" : "打开门";
        }
        
        public bool CanInteract(InteractionSystem interactor)
        {
            return !isAnimating;
        }
    }
    
    /// <summary>
    /// 物品拾取示例
    /// </summary>
    public class ItemPickup : MonoBehaviour, IPickable
    {
        [Header("物品设置")]
        [SerializeField] private string itemName = "物品";
        [SerializeField] private int amount = 1;
        [SerializeField] private GameObject pickupEffect;
        
        [Header("旋转动画")]
        [SerializeField] private bool rotateOnIdle = true;
        [SerializeField] private float rotationSpeed = 50f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;
        
        private Vector3 startPosition;
        
        private void Start()
        {
            startPosition = transform.position;
        }
        
        private void Update()
        {
            if (rotateOnIdle)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
                transform.position = startPosition + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            }
        }
        
        public bool Interact(InteractionSystem interactor)
        {
            // 触发拾取
            OnPickedUp(interactor);
            return true;
        }
        
        public object GetPickupData()
        {
            return new { itemName, amount };
        }
        
        public void OnPickedUp(InteractionSystem interactor)
        {
            // 播放特效
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }
            
            // 销毁物体
            Destroy(gameObject);
        }
        
        public string GetInteractionPrompt()
        {
            return $"拾取 {itemName}";
        }
        
        public bool CanInteract(InteractionSystem interactor)
        {
            return true;
        }
    }
}
