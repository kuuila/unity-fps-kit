using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Kuuila.FPS.Weapon
{
    /// <summary>
    /// 武器管理器
    /// 管理所有武器的装备、切换、射击
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        [Header("武器设置")]
        [SerializeField] private List<WeaponData> weapons = new List<WeaponData>();
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private Transform weaponDropPoint;
        
        [Header("切换设置")]
        [SerializeField] private float switchTime = 0.5f;
        [SerializeField] private bool canSwitchWhileReloading = false;
        
        [Header("输入设置")]
        [SerializeField] private KeyCode[] weaponKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5 };
        [SerializeField] private KeyCode lastWeaponKey = KeyCode.Q;
        
        // 当前状态
        private int currentWeaponIndex = -1;
        private int lastWeaponIndex = -1;
        private bool isSwitching = false;
        private bool isAiming = false;
        
        // 组件
        private FPSWeapon currentWeapon;
        private Camera playerCamera;
        
        // 事件
        public event Action<int, WeaponData> OnWeaponSwitched;
        public event Action<WeaponData> OnWeaponPickedUp;
        public event Action<WeaponData> OnWeaponDropped;
        public event Action OnAimStart;
        public event Action OnAimEnd;
        
        // 属性
        public FPSWeapon CurrentWeapon => currentWeapon;
        public WeaponData CurrentWeaponData => currentWeaponIndex >= 0 ? weapons[currentWeaponIndex] : null;
        public bool IsSwitching => isSwitching;
        public bool IsAiming => isAiming;
        public int WeaponCount => weapons.Count;
        
        private void Start()
        {
            playerCamera = Camera.main;
            
            // 初始化武器
            InitializeWeapons();
            
            // 装备第一把武器
            if (weapons.Count > 0)
            {
                SwitchWeapon(0);
            }
        }
        
        private void Update()
        {
            HandleInput();
            HandleAiming();
        }
        
        private void HandleInput()
        {
            if (isSwitching) return;
            
            // 数字键切换武器
            for (int i = 0; i < weaponKeys.Length && i < weapons.Count; i++)
            {
                if (Input.GetKeyDown(weaponKeys[i]))
                {
                    SwitchWeapon(i);
                    break;
                }
            }
            
            // 滚轮切换
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0)
            {
                SwitchToNextWeapon();
            }
            else if (scroll < 0)
            {
                SwitchToPreviousWeapon();
            }
            
            // Q键切换上一把武器
            if (Input.GetKeyDown(lastWeaponKey))
            {
                SwitchToLastWeapon();
            }
            
            // 射击
            if (currentWeapon != null)
            {
                if (Input.GetButtonDown("Fire1"))
                {
                    currentWeapon.StartFire();
                }
                else if (Input.GetButtonUp("Fire1"))
                {
                    currentWeapon.StopFire();
                }
                
                // 换弹
                if (Input.GetKeyDown(KeyCode.R))
                {
                    currentWeapon.Reload();
                }
                
                // 检视
                if (Input.GetKeyDown(KeyCode.T))
                {
                    currentWeapon.Inspect();
                }
            }
        }
        
        private void HandleAiming()
        {
            if (currentWeapon == null) return;
            
            bool wantsAim = Input.GetButton("Fire2") && currentWeapon.CanAim;
            
            if (wantsAim != isAiming)
            {
                isAiming = wantsAim;
                currentWeapon.SetAiming(isAiming);
                
                if (isAiming)
                    OnAimStart?.Invoke();
                else
                    OnAimEnd?.Invoke();
            }
        }
        
        private void InitializeWeapons()
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i].weaponPrefab != null)
                {
                    GameObject weaponObj = Instantiate(weapons[i].weaponPrefab, weaponHolder);
                    weaponObj.transform.localPosition = weapons[i].positionOffset;
                    weaponObj.transform.localRotation = Quaternion.Euler(weapons[i].rotationOffset);
                    weaponObj.SetActive(false);
                    
                    FPSWeapon weapon = weaponObj.GetComponent<FPSWeapon>();
                    if (weapon != null)
                    {
                        weapon.Initialize(weapons[i], this);
                    }
                    
                    weapons[i].weaponInstance = weapon;
                }
            }
        }
        
        /// <summary>
        /// 切换到指定武器
        /// </summary>
        public void SwitchWeapon(int index)
        {
            if (index < 0 || index >= weapons.Count) return;
            if (index == currentWeaponIndex) return;
            if (isSwitching) return;
            if (!canSwitchWhileReloading && currentWeapon != null && currentWeapon.IsReloading) return;
            
            StartCoroutine(SwitchWeaponCoroutine(index));
        }
        
        private IEnumerator SwitchWeaponCoroutine(int newIndex)
        {
            isSwitching = true;
            
            // 保存上一把武器
            lastWeaponIndex = currentWeaponIndex;
            
            // 收起当前武器
            if (currentWeapon != null)
            {
                currentWeapon.Holster();
                yield return new WaitForSeconds(switchTime * 0.5f);
                currentWeapon.gameObject.SetActive(false);
            }
            
            // 装备新武器
            currentWeaponIndex = newIndex;
            currentWeapon = weapons[newIndex].weaponInstance;
            
            if (currentWeapon != null)
            {
                currentWeapon.gameObject.SetActive(true);
                currentWeapon.Draw();
                
                // 恢复瞄准状态
                if (isAiming)
                {
                    currentWeapon.SetAiming(true);
                }
            }
            
            yield return new WaitForSeconds(switchTime * 0.5f);
            
            isSwitching = false;
            OnWeaponSwitched?.Invoke(newIndex, weapons[newIndex]);
        }
        
        public void SwitchToNextWeapon()
        {
            int nextIndex = (currentWeaponIndex + 1) % weapons.Count;
            SwitchWeapon(nextIndex);
        }
        
        public void SwitchToPreviousWeapon()
        {
            int prevIndex = currentWeaponIndex - 1;
            if (prevIndex < 0) prevIndex = weapons.Count - 1;
            SwitchWeapon(prevIndex);
        }
        
        public void SwitchToLastWeapon()
        {
            if (lastWeaponIndex >= 0 && lastWeaponIndex != currentWeaponIndex)
            {
                SwitchWeapon(lastWeaponIndex);
            }
        }
        
        /// <summary>
        /// 添加武器
        /// </summary>
        public void AddWeapon(WeaponData weaponData)
        {
            weapons.Add(weaponData);
            OnWeaponPickedUp?.Invoke(weaponData);
        }
        
        /// <summary>
        /// 丢弃当前武器
        /// </summary>
        public void DropCurrentWeapon()
        {
            if (currentWeapon == null || weapons.Count <= 1) return;
            
            WeaponData droppedWeapon = weapons[currentWeaponIndex];
            
            // 创建地面武器
            if (droppedWeapon.pickupPrefab != null && weaponDropPoint != null)
            {
                Instantiate(droppedWeapon.pickupPrefab, weaponDropPoint.position, weaponDropPoint.rotation);
            }
            
            // 移除武器
            Destroy(currentWeapon.gameObject);
            weapons.RemoveAt(currentWeaponIndex);
            
            // 切换到第一把武器
            currentWeaponIndex = -1;
            SwitchWeapon(0);
            
            OnWeaponDropped?.Invoke(droppedWeapon);
        }
        
        /// <summary>
        /// 获取武器数据
        /// </summary>
        public WeaponData GetWeaponData(int index)
        {
            if (index >= 0 && index < weapons.Count)
                return weapons[index];
            return null;
        }
        
        /// <summary>
        /// 重新装填所有武器
        /// </summary>
        public void ReplenishAllAmmo()
        {
            foreach (var weapon in weapons)
            {
                if (weapon.weaponInstance != null)
                {
                    weapon.weaponInstance.ReplenishAmmo();
                }
            }
        }
    }
    
    /// <summary>
    /// 武器数据
    /// </summary>
    [System.Serializable]
    public class WeaponData
    {
        public string weaponName = "Weapon";
        public WeaponType weaponType = WeaponType.Rifle;
        public GameObject weaponPrefab;
        public GameObject pickupPrefab;
        
        [Header("位置偏移")]
        public Vector3 positionOffset = Vector3.zero;
        public Vector3 rotationOffset = Vector3.zero;
        
        [Header("弹药设置")]
        public int maxAmmo = 30;
        public int reserveAmmo = 120;
        public int startingAmmo = 90;
        
        [Header("射击设置")]
        public float fireRate = 0.1f;
        public float damage = 25f;
        public float range = 100f;
        public bool isAutomatic = true;
        
        [Header("精度设置")]
        public float baseAccuracy = 0.9f;
        public float aimAccuracy = 0.98f;
        public float recoilAmount = 0.5f;
        
        [Header("瞄准设置")]
        public bool canAim = true;
        public float aimFOV = 50f;
        public Vector3 aimPosition;
        
        // 运行时实例
        [System.NonSerialized] public FPSWeapon weaponInstance;
    }
    
    public enum WeaponType
    {
        Unarmed,
        Knife,
        Pistol,
        Rifle,
        Shotgun,
        Sniper,
        Grenade
    }
}
