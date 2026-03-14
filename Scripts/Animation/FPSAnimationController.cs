using UnityEngine;
using System;

namespace Kuuila.FPS.Animation
{
    /// <summary>
    /// 动画状态机控制器
    /// 管理所有武器类型的动画状态
    /// </summary>
    public class FPSAnimationController : MonoBehaviour
    {
        [Header("动画组件")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform weaponHolder;
        
        [Header("动画速度")]
        [SerializeField] private float idleSpeed = 1f;
        [SerializeField] private float walkSpeed = 1f;
        [SerializeField] private float sprintSpeed = 1.2f;
        [SerializeField] private float aimSpeed = 0.5f;
        
        [Header("平滑设置")]
        [SerializeField] private float transitionSpeed = 0.1f;
        
        // 动画参数哈希（性能优化）
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int IsSprinting = Animator.StringToHash("IsSprinting");
        private static readonly int IsCrouching = Animator.StringToHash("IsCrouching");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int Horizontal = Animator.StringToHash("Horizontal");
        private static readonly int WeaponType = Animator.StringToHash("WeaponType");
        private static readonly int IsAiming = Animator.StringToHash("IsAiming");
        private static readonly int IsFiring = Animator.StringToHash("IsFiring");
        private static readonly int IsReloading = Animator.StringToHash("IsReloading");
        private static readonly int SwitchWeapon = Animator.StringToHash("SwitchWeapon");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int AttackType = Animator.StringToHash("AttackType");
        private static readonly int Inspect = Animator.StringToHash("Inspect");
        
        // 当前状态
        private float currentVertical;
        private float currentHorizontal;
        private WeaponType currentWeaponType = WeaponType.Unarmed;
        private bool isAiming;
        private bool isReloading;
        
        // 事件
        public event Action OnReloadComplete;
        public event Action OnSwitchComplete;
        public event Action<int> OnAttackStart;
        public event Action<int> OnAttackEnd;
        
        // 属性
        public WeaponType CurrentWeapon => currentWeaponType;
        public bool IsReloading => isReloading;
        public bool IsAiming => isAiming;
        
        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        /// <summary>
        /// 更新移动动画
        /// </summary>
        public void UpdateMovement(float vertical, float horizontal, bool isMoving, bool isSprinting, bool isCrouching, bool isGrounded)
        {
            // 平滑插值
            currentVertical = Mathf.Lerp(currentVertical, vertical, transitionSpeed);
            currentHorizontal = Mathf.Lerp(currentHorizontal, horizontal, transitionSpeed);
            
            animator.SetFloat(Vertical, currentVertical);
            animator.SetFloat(Horizontal, currentHorizontal);
            animator.SetBool(IsMoving, isMoving);
            animator.SetBool(IsSprinting, isSprinting);
            animator.SetBool(IsCrouching, isCrouching);
            animator.SetBool(IsGrounded, isGrounded);
            
            // 调整动画速度
            float targetSpeed = isSprinting ? sprintSpeed : (isMoving ? walkSpeed : idleSpeed);
            if (isAiming) targetSpeed *= aimSpeed;
            animator.speed = targetSpeed;
        }
        
        /// <summary>
        /// 切换武器类型
        /// </summary>
        public void SwitchWeaponType(WeaponType type)
        {
            if (currentWeaponType == type) return;
            
            currentWeaponType = type;
            animator.SetInteger(WeaponType, (int)type);
            animator.SetTrigger(SwitchWeapon);
        }
        
        /// <summary>
        /// 设置瞄准状态
        /// </summary>
        public void SetAiming(bool aiming)
        {
            isAiming = aiming;
            animator.SetBool(IsAiming, aiming);
        }
        
        /// <summary>
        /// 播放开火动画
        /// </summary>
        public void PlayFire()
        {
            if (isReloading) return;
            
            animator.SetBool(IsFiring, true);
        }
        
        /// <summary>
        /// 停止开火动画
        /// </summary>
        public void StopFire()
        {
            animator.SetBool(IsFiring, false);
        }
        
        /// <summary>
        /// 播放换弹动画
        /// </summary>
        public void PlayReload()
        {
            if (isReloading) return;
            
            isReloading = true;
            animator.SetBool(IsReloading, true);
        }
        
        /// <summary>
        /// 播放近战攻击动画
        /// </summary>
        public void PlayMeleeAttack(int attackType = 0)
        {
            if (isReloading) return;
            
            animator.SetInteger(AttackType, attackType);
            animator.SetTrigger(Attack);
        }
        
        /// <summary>
        /// 播放检视动画
        /// </summary>
        public void PlayInspect()
        {
            animator.SetTrigger(Inspect);
        }
        
        /// <summary>
        /// 从动画事件调用 - 换弹完成
        /// </summary>
        public void OnReloadAnimationComplete()
        {
            isReloading = false;
            animator.SetBool(IsReloading, false);
            OnReloadComplete?.Invoke();
        }
        
        /// <summary>
        /// 从动画事件调用 - 切换完成
        /// </summary>
        public void OnSwitchAnimationComplete()
        {
            OnSwitchComplete?.Invoke();
        }
        
        /// <summary>
        /// 从动画事件调用 - 攻击开始
        /// </summary>
        public void OnAttackAnimationStart(int attackType)
        {
            OnAttackStart?.Invoke(attackType);
        }
        
        /// <summary>
        /// 从动画事件调用 - 攻击结束
        /// </summary>
        public void OnAttackAnimationEnd(int attackType)
        {
            OnAttackEnd?.Invoke(attackType);
        }
        
        /// <summary>
        /// 设置动画速度倍率
        /// </summary>
        public void SetAnimationSpeed(float multiplier)
        {
            animator.speed = multiplier;
        }
        
        /// <summary>
        /// 获取当前动画状态
        /// </summary>
        public AnimatorStateInfo GetCurrentState(int layer = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layer);
        }
        
        /// <summary>
        /// 检查是否在特定状态
        /// </summary>
        public bool IsInState(string stateName, int layer = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName);
        }
    }
    
    /// <summary>
    /// 武器类型枚举
    /// </summary>
    public enum WeaponType
    {
        Unarmed = 0,    // 空手
        Knife = 1,      // 刀
        Pistol = 2,     // 手枪
        Rifle = 3,      // 步枪/持枪
        Shotgun = 4,    // 霰弹枪
        Sniper = 5,     // 狙击枪
        Grenade = 6     // 手雷
    }
}
