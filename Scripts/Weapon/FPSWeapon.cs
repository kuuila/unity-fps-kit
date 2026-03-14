using UnityEngine;
using System;
using System.Collections;

namespace Kuuila.FPS.Weapon
{
    /// <summary>
    /// FPS武器基类
    /// 处理射击、换弹、瞄准等逻辑
    /// </summary>
    public class FPSWeapon : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] protected Transform muzzlePoint;
        [SerializeField] protected Transform casingEjectPoint;
        [SerializeField] protected ParticleSystem muzzleFlash;
        [SerializeField] protected ParticleSystem casingEffect;
        [SerializeField] protected TrailRenderer bulletTrail;
        [SerializeField] protected AudioSource audioSource;
        
        [Header("音效")]
        [SerializeField] protected AudioClip fireSound;
        [SerializeField] protected AudioClip reloadSound;
        [SerializeField] protected AudioClip emptySound;
        [SerializeField] protected AudioClip drawSound;
        [SerializeField] protected AudioClip holsterSound;
        
        // 数据
        protected WeaponData weaponData;
        protected WeaponManager weaponManager;
        
        // 状态
        protected int currentAmmo;
        protected int reserveAmmo;
        protected bool isFiring;
        protected bool isReloading;
        protected bool isAiming;
        protected float lastFireTime;
        
        // 动画
        protected Animation.FPSAnimationController animationController;
        protected Vector3 originalPosition;
        protected Quaternion originalRotation;
        
        // 事件
        public event Action OnFire;
        public event Action OnReloadStart;
        public event Action OnReloadComplete;
        public event Action OnAmmoChanged;
        public event Action<bool> OnAimChanged;
        
        // 属性
        public WeaponData Data => weaponData;
        public int CurrentAmmo => currentAmmo;
        public int ReserveAmmo => reserveAmmo;
        public int TotalAmmo => currentAmmo + reserveAmmo;
        public bool IsReloading => isReloading;
        public bool IsFiring => isFiring;
        public bool CanAim => weaponData.canAim;
        public bool IsAiming => isAiming;
        public bool IsEmpty => currentAmmo <= 0;
        
        public virtual void Initialize(WeaponData data, WeaponManager manager)
        {
            weaponData = data;
            weaponManager = manager;
            
            currentAmmo = data.maxAmmo;
            reserveAmmo = data.startingAmmo;
            
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
            
            // 获取动画控制器
            animationController = GetComponentInParent<Animation.FPSAnimationController>();
        }
        
        protected virtual void Update()
        {
            HandleAiming();
            HandleFiring();
        }
        
        #region 射击
        
        public virtual void StartFire()
        {
            if (isReloading) return;
            if (currentAmmo <= 0)
            {
                PlayEmptySound();
                return;
            }
            
            isFiring = true;
        }
        
        public virtual void StopFire()
        {
            isFiring = false;
            
            if (animationController != null)
            {
                animationController.StopFire();
            }
        }
        
        protected virtual void HandleFiring()
        {
            if (!isFiring) return;
            
            // 检查射速
            if (Time.time - lastFireTime < weaponData.fireRate) return;
            
            // 检查弹药
            if (currentAmmo <= 0)
            {
                StopFire();
                PlayEmptySound();
                return;
            }
            
            Fire();
        }
        
        protected virtual void Fire()
        {
            lastFireTime = Time.time;
            currentAmmo--;
            
            // 播放动画
            animationController?.PlayFire();
            
            // 播放特效
            PlayMuzzleFlash();
            PlayCasingEffect();
            PlayFireSound();
            
            // 射线检测
            PerformRaycast();
            
            // 后坐力
            ApplyRecoil();
            
            OnFire?.Invoke();
            OnAmmoChanged?.Invoke();
        }
        
        protected virtual void PerformRaycast()
        {
            Camera cam = Camera.main;
            Vector3 rayOrigin = cam.transform.position;
            
            // 计算射击精度
            float accuracy = isAiming ? weaponData.aimAccuracy : weaponData.baseAccuracy;
            float spread = (1f - accuracy) * 0.1f;
            
            Vector3 rayDirection = cam.transform.forward + 
                new Vector3(UnityEngine.Random.Range(-spread, spread), 
                           UnityEngine.Random.Range(-spread, spread), 0);
            
            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, weaponData.range))
            {
                // 播放命中效果
                SpawnImpactEffect(hit);
                
                // 造成伤害
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                damageable?.TakeDamage(weaponData.damage, hit.point, hit.normal);
                
                // 弹道轨迹
                if (bulletTrail != null)
                {
                    StartCoroutine(SpawnBulletTrail(hit.point));
                }
            }
            else
            {
                // 弹道轨迹到远处
                if (bulletTrail != null)
                {
                    StartCoroutine(SpawnBulletTrail(rayOrigin + rayDirection * weaponData.range));
                }
            }
        }
        
        protected virtual IEnumerator SpawnBulletTrail(Vector3 target)
        {
            TrailRenderer trail = Instantiate(bulletTrail, muzzlePoint.position, Quaternion.identity);
            trail.gameObject.SetActive(true);
            
            float time = 0;
            Vector3 start = muzzlePoint.position;
            
            while (time < 0.1f)
            {
                trail.transform.position = Vector3.Lerp(start, target, time / 0.1f);
                time += Time.deltaTime;
                yield return null;
            }
            
            Destroy(trail.gameObject);
        }
        
        protected virtual void SpawnImpactEffect(RaycastHit hit)
        {
            // 子类实现
        }
        
        protected virtual void ApplyRecoil()
        {
            // 子类实现
        }
        
        #endregion
        
        #region 换弹
        
        public virtual void Reload()
        {
            if (isReloading) return;
            if (currentAmmo >= weaponData.maxAmmo) return;
            if (reserveAmmo <= 0) return;
            
            StartCoroutine(ReloadCoroutine());
        }
        
        protected virtual IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            OnReloadStart?.Invoke();
            
            // 播放动画
            animationController?.PlayReload();
            
            // 播放音效
            PlayReloadSound();
            
            // 等待换弹时间（从动画获取更准确）
            yield return new WaitForSeconds(2f);
            
            // 计算需要补充的弹药
            int ammoNeeded = weaponData.maxAmmo - currentAmmo;
            int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);
            
            currentAmmo += ammoToReload;
            reserveAmmo -= ammoToReload;
            
            isReloading = false;
            OnReloadComplete?.Invoke();
            OnAmmoChanged?.Invoke();
        }
        
        public void ReplenishAmmo()
        {
            reserveAmmo = weaponData.reserveAmmo;
            OnAmmoChanged?.Invoke();
        }
        
        #endregion
        
        #region 瞄准
        
        public virtual void SetAiming(bool aiming)
        {
            isAiming = aiming && weaponData.canAim;
            OnAimChanged?.Invoke(isAiming);
        }
        
        protected virtual void HandleAiming()
        {
            Vector3 targetPos = isAiming ? weaponData.aimPosition : originalPosition;
            Quaternion targetRot = isAiming ? Quaternion.identity : originalRotation;
            
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * 10f);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRot, Time.deltaTime * 10f);
        }
        
        #endregion
        
        #region 动画事件
        
        public virtual void Draw()
        {
            PlaySound(drawSound);
        }
        
        public virtual void Holster()
        {
            StopFire();
            PlaySound(holsterSound);
        }
        
        public virtual void Inspect()
        {
            animationController?.PlayInspect();
        }
        
        #endregion
        
        #region 特效和音效
        
        protected virtual void PlayMuzzleFlash()
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
        }
        
        protected virtual void PlayCasingEffect()
        {
            if (casingEffect != null)
            {
                casingEffect.Play();
            }
        }
        
        protected virtual void PlayFireSound()
        {
            PlaySound(fireSound);
        }
        
        protected virtual void PlayReloadSound()
        {
            PlaySound(reloadSound);
        }
        
        protected virtual void PlayEmptySound()
        {
            PlaySound(emptySound);
        }
        
        protected virtual void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 可伤害接口
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
    }
}
