using UnityEngine;
using Kuuila.FPS.Animation;

namespace Kuuila.FPS.Core
{
    /// <summary>
    /// FPS角色核心组件
    /// 整合所有子系统
    /// </summary>
    [RequireComponent(typeof(Controller.FPSController))]
    public class FPSCharacter : MonoBehaviour
    {
        [Header("子系统")]
        [SerializeField] private Controller.FPSController controller;
        [SerializeField] private Weapon.WeaponManager weaponManager;
        [SerializeField] private Interaction.InteractionSystem interactionSystem;
        [SerializeField] private FPSAnimationController animationController;
        [SerializeField] private Controller.FPSUIManager uiManager;
        
        [Header("生命值")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float healthRegenRate = 2f;
        [SerializeField] private float healthRegenDelay = 3f;
        
        private float currentHealth;
        private float lastDamageTime;
        private bool isDead;
        
        // 事件
        public event System.Action<float> OnDamageTaken;
        public event System.Action OnDeath;
        public event System.Action<float, float> OnHealthChanged;
        
        // 属性
        public float Health => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;
        
        private void Awake()
        {
            // 自动获取组件
            if (controller == null) controller = GetComponent<Controller.FPSController>();
            if (weaponManager == null) weaponManager = GetComponentInChildren<Weapon.WeaponManager>();
            if (interactionSystem == null) interactionSystem = GetComponentInChildren<Interaction.InteractionSystem>();
            if (animationController == null) animationController = GetComponentInChildren<FPSAnimationController>();
            
            currentHealth = maxHealth;
        }
        
        private void Start()
        {
            // 绑定事件
            if (controller != null)
            {
                controller.OnJump += () => 
                {
                    animationController?.UpdateMovement(0, 0, false, false, false, true);
                };
            }
        }
        
        private void Update()
        {
            UpdateAnimation();
            UpdateHealthRegen();
        }
        
        private void UpdateAnimation()
        {
            if (animationController == null || controller == null) return;
            
            // 获取移动输入
            float vertical = Input.GetAxisRaw("Vertical");
            float horizontal = Input.GetAxisRaw("Horizontal");
            bool isMoving = vertical != 0 || horizontal != 0;
            
            animationController.UpdateMovement(
                vertical, 
                horizontal, 
                isMoving, 
                controller.IsSprinting, 
                controller.IsCrouching, 
                controller.IsGrounded
            );
        }
        
        private void UpdateHealthRegen()
        {
            if (isDead) return;
            
            // 延迟后开始恢复
            if (Time.time - lastDamageTime > healthRegenDelay && currentHealth < maxHealth)
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + healthRegenRate * Time.deltaTime);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }
        
        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead) return;
            
            currentHealth -= damage;
            lastDamageTime = Time.time;
            
            OnDamageTaken?.Invoke(damage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            if (uiManager != null)
            {
                uiManager.FlashDamage();
            }
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(float amount)
        {
            if (isDead) return;
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        private void Die()
        {
            isDead = true;
            OnDeath?.Invoke();
            
            // 禁用控制
            if (controller != null)
            {
                controller.enabled = false;
            }
            
            // 禁用武器
            if (weaponManager != null)
            {
                weaponManager.enabled = false;
            }
            
            // 禁用交互
            if (interactionSystem != null)
            {
                interactionSystem.DisableInteraction();
            }
        }
        
        /// <summary>
        /// 复活
        /// </summary>
        public void Respawn(Vector3 position)
        {
            isDead = false;
            currentHealth = maxHealth;
            
            // 启用控制
            if (controller != null)
            {
                controller.enabled = true;
                controller.Teleport(position, Quaternion.identity);
            }
            
            // 启用武器
            if (weaponManager != null)
            {
                weaponManager.enabled = true;
                weaponManager.ReplenishAllAmmo();
            }
            
            // 启用交互
            if (interactionSystem != null)
            {
                interactionSystem.EnableInteraction();
            }
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
