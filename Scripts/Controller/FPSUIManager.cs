using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Kuuila.FPS.UI
{
    /// <summary>
    /// FPS游戏UI管理器
    /// 准星、血量、弹药、提示等UI
    /// </summary>
    public class FPSUIManager : MonoBehaviour
    {
        [Header("准星")]
        [SerializeField] private Image crosshair;
        [SerializeField] private Image crosshairDot;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private Color interactColor = Color.green;
        
        [Header("血量")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Image healthFill;
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color midHealthColor = Color.yellow;
        [SerializeField] private Color lowHealthColor = Color.red;
        
        [Header("弹药")]
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text reserveAmmoText;
        [SerializeField] private GameObject ammoPanel;
        
        [Header("武器")]
        [SerializeField] private TMP_Text weaponNameText;
        [SerializeField] private Image weaponIcon;
        
        [Header("提示")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TMP_Text promptText;
        
        [Header("伤害指示")]
        [SerializeField] private Image damageIndicator;
        [SerializeField] private float indicatorFadeSpeed = 2f;
        
        [Header("击杀信息")]
        [SerializeField] private Transform killFeedParent;
        [SerializeField] private GameObject killFeedItemPrefab;
        [SerializeField] private int maxKillFeedItems = 5;
        
        // 引用
        private Controller.FPSController playerController;
        private Weapon.WeaponManager weaponManager;
        private Interaction.InteractionSystem interactionSystem;
        
        private void Start()
        {
            // 自动查找组件
            playerController = FindObjectOfType<Controller.FPSController>();
            weaponManager = FindObjectOfType<Weapon.WeaponManager>();
            interactionSystem = FindObjectOfType<Interaction.InteractionSystem>();
            
            // 绑定事件
            if (interactionSystem != null)
            {
                interactionSystem.OnTargetChanged += OnInteractableChanged;
            }
            
            if (weaponManager != null)
            {
                weaponManager.OnWeaponSwitched += OnWeaponSwitched;
            }
            
            // 初始化UI
            UpdateHealth(100f, 100f);
            UpdateAmmo(30, 90);
        }
        
        private void Update()
        {
            UpdateDamageIndicator();
        }
        
        #region 准星
        
        public void SetCrosshairNormal()
        {
            if (crosshair != null) crosshair.color = normalColor;
            if (crosshairDot != null) crosshairDot.color = normalColor;
        }
        
        public void SetCrosshairHit()
        {
            if (crosshair != null) crosshair.color = hitColor;
            if (crosshairDot != null) crosshairDot.color = hitColor;
        }
        
        public void SetCrosshairInteract()
        {
            if (crosshair != null) crosshair.color = interactColor;
            if (crosshairDot != null) crosshairDot.color = interactColor;
        }
        
        #endregion
        
        #region 血量
        
        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (healthBar != null)
            {
                healthBar.value = currentHealth / maxHealth;
            }
            
            if (healthText != null)
            {
                healthText.text = Mathf.Ceil(currentHealth).ToString();
            }
            
            if (healthFill != null)
            {
                float healthPercent = currentHealth / maxHealth;
                
                if (healthPercent > 0.6f)
                    healthFill.color = fullHealthColor;
                else if (healthPercent > 0.3f)
                    healthFill.color = midHealthColor;
                else
                    healthFill.color = lowHealthColor;
            }
        }
        
        public void FlashDamage()
        {
            if (damageIndicator != null)
            {
                Color color = damageIndicator.color;
                color.a = 0.5f;
                damageIndicator.color = color;
            }
        }
        
        private void UpdateDamageIndicator()
        {
            if (damageIndicator != null && damageIndicator.color.a > 0)
            {
                Color color = damageIndicator.color;
                color.a = Mathf.MoveTowards(color.a, 0, indicatorFadeSpeed * Time.deltaTime);
                damageIndicator.color = color;
            }
        }
        
        #endregion
        
        #region 弹药
        
        public void UpdateAmmo(int current, int reserve)
        {
            if (ammoText != null)
            {
                ammoText.text = current.ToString();
            }
            
            if (reserveAmmoText != null)
            {
                reserveAmmoText.text = $"/ {reserve}";
            }
            
            // 低弹药警告
            if (current <= 5)
            {
                if (ammoText != null) ammoText.color = Color.red;
            }
            else
            {
                if (ammoText != null) ammoText.color = Color.white;
            }
        }
        
        public void ShowAmmoPanel(bool show)
        {
            if (ammoPanel != null)
            {
                ammoPanel.SetActive(show);
            }
        }
        
        #endregion
        
        #region 武器
        
        private void OnWeaponSwitched(int index, Weapon.WeaponData data)
        {
            if (weaponNameText != null)
            {
                weaponNameText.text = data.weaponName;
            }
            
            // 近战武器隐藏弹药面板
            ShowAmmoPanel(data.weaponType != Weapon.WeaponType.Knife);
        }
        
        #endregion
        
        #region 交互
        
        private void OnInteractableChanged(Interaction.IInteractable interactable)
        {
            if (interactionPrompt != null)
            {
                bool show = interactable != null;
                interactionPrompt.SetActive(show);
                
                if (show && promptText != null)
                {
                    promptText.text = interactable.GetInteractionPrompt();
                }
            }
            
            // 改变准星颜色
            if (interactable != null)
            {
                SetCrosshairInteract();
            }
            else
            {
                SetCrosshairNormal();
            }
        }
        
        #endregion
        
        #region 击杀信息
        
        public void AddKillFeed(string killer, string victim, string weapon = "")
        {
            if (killFeedParent == null || killFeedItemPrefab == null) return;
            
            // 创建击杀信息项
            GameObject item = Instantiate(killFeedItemPrefab, killFeedParent);
            TMP_Text text = item.GetComponentInChildren<TMP_Text>();
            
            if (text != null)
            {
                text.text = string.IsNullOrEmpty(weapon) 
                    ? $"{killer} 击杀了 {victim}"
                    : $"{killer} [{weapon}] {victim}";
            }
            
            // 限制数量
            if (killFeedParent.childCount > maxKillFeedItems)
            {
                Destroy(killFeedParent.GetChild(0).gameObject);
            }
            
            // 自动销毁
            Destroy(item, 5f);
        }
        
        #endregion
    }
}
