using UnityEngine;
using System;
using System.Collections;

namespace Kuuila.FPS.Weapon
{
    /// <summary>
    /// 近战武器控制器 - 刀
    /// </summary>
    public class MeleeWeapon : MonoBehaviour
    {
        [Header("近战设置")]
        [SerializeField] private float attackDamage = 50f;
        [SerializeField] private float attackRange = 3f;
        [SerializeField] private float attackRate = 1f;
        [SerializeField] private float attackCooldown = 0.3f;
        
        [Header("击退设置")]
        [SerializeField] private float knockbackForce = 5f;
        
        [Header("特效")]
        [SerializeField] private TrailRenderer swingTrail;
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] swingSounds;
        [SerializeField] private AudioClip[] hitSounds;
        [SerializeField] private AudioClip[] missSounds;
        
        [Header("动画")]
        [SerializeField] private Animation.FPSAnimationController animController;
        
        // 状态
        private bool isAttacking;
        private float lastAttackTime;
        private int currentAttackIndex;
        
        // 事件
        public event Action OnAttack;
        public event Action OnHit;
        public event Action OnMiss;
        
        // 属性
        public float Damage => attackDamage;
        public float Range => attackRange;
        public bool IsAttacking => isAttacking;
        
        private void Awake()
        {
            if (animController == null)
            {
                animController = GetComponentInParent<Animation.FPSAnimationController>();
            }
            
            // 初始化攻击索引
            currentAttackIndex = 0;
        }
        
        private void Update()
        {
            // 左键攻击
            if (Input.GetButtonDown("Fire1") && !isAttacking)
            {
                Attack();
            }
        }
        
        /// <summary>
        /// 攻击
        /// </summary>
        public void Attack()
        {
            if (isAttacking) return;
            if (Time.time - lastAttackTime < attackCooldown) return;
            
            StartCoroutine(AttackCoroutine());
        }
        
        private IEnumerator AttackCoroutine()
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            
            // 播放攻击动画
            animController?.PlayMeleeAttack(currentAttackIndex);
            
            // 循环攻击索引
            currentAttackIndex = (currentAttackTime + 1) % 3;
            
            // 播放挥动音效
            PlayRandomSound(swingSounds);
            
            // 挥动轨迹
            if (swingTrail != null)
            {
                StartCoroutine(ShowSwingTrail());
            }
            
            // 等待动画进行到命中帧
            yield return new WaitForSeconds(0.15f);
            
            // 检测命中
            CheckHit();
            
            // 等待动画完成
            yield return new WaitForSeconds(attackRate - 0.15f);
            
            isAttacking = false;
            
            OnAttack?.Invoke();
        }
        
        private void CheckHit()
        {
            Camera cam = Camera.main;
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            
            // 使用盒式射线检测覆盖攻击范围
            if (Physics.BoxCast(ray.origin, Vector3.one * 0.5f, ray.direction, 
                out RaycastHit hit, Quaternion.identity, attackRange))
            {
                // 命中
                OnHit?.Invoke();
                
                // 造成伤害
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(attackDamage, hit.point, hit.normal);
                }
                
                // 击退
                Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(ray.direction * knockbackForce, ForceMode.Impulse);
                }
                
                // 播放命中特效
                if (hitEffect != null)
                {
                    ParticleSystem effect = Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    effect.Play();
                    Destroy(effect.gameObject, 2f);
                }
                
                // 播放命中音效
                PlayRandomSound(hitSounds);
            }
            else
            {
                // 未命中
                OnMiss?.Invoke();
                PlayRandomSound(missSounds);
            }
        }
        
        private IEnumerator ShowSwingTrail()
        {
            swingTrail.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            swingTrail.gameObject.SetActive(false);
        }
        
        private void PlayRandomSound(AudioClip[] sounds)
        {
            if (sounds == null || sounds.Length == 0) return;
            if (audioSource == null) return;
            
            AudioClip clip = sounds[UnityEngine.Random.Range(0, sounds.Length)];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        /// <summary>
        /// 设置攻击力
        /// </summary>
        public void SetDamage(float damage)
        {
            attackDamage = damage;
        }
        
        /// <summary>
        /// 设置攻击范围
        /// </summary>
        public void SetRange(float range)
        {
            attackRange = range;
        }
    }
}
