# Kuuila Unity FPS Kit

🎮 **FPS游戏开发套件 - Unity组件库**

```
╔══════════════════════════════════════════════════════════════╗
║                                                              ║
║   🎮 Kuuila FPS Kit - Unity FPS Game Development Kit        ║
║                                                              ║
║   小而精，快速上手，完整功能                                  ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
```

## ✨ 特性

### 🏃 FPS控制器
- **第一人称移动** - 行走、冲刺、蹲伏
- **跳跃系统** - 可配置跳跃力和重力
- **视角控制** - 鼠标视角，可调节灵敏度
- **冲刺FOV** - 动态视野变化

### 🔫 武器系统
- **多武器支持** - 空手、手枪、步枪、刀
- **射击系统** - 射线检测、弹道轨迹、后坐力
- **换弹动画** - 完整的换弹流程
- **瞄准系统** - 瞄准FOV和位置调整
- **近战攻击** - 连击系统、命中检测

### 🎬 动画系统
- **动画状态机** - 完整的动画参数控制
- **移动动画** - 行走、冲刺、蹲伏过渡
- **武器切换** - 流畅的切换动画
- **射击动画** - 自动、半自动武器
- **近战动画** - 多连击支持

### 🖐️ 交互系统
- **射线交互** - 基于距离的交互检测
- **可交互接口** - 统一的交互接口
- **示例组件** - 门、物品拾取
- **UI提示** - 动态交互提示

### 🖼️ UI系统
- **准星** - 动态准星颜色变化
- **血量条** - 渐变颜色显示
- **弹药显示** - 当前/备弹显示
- **击杀信息** - 击杀通知系统

## 📦 组件结构

```
Scripts/
├── Controller/
│   ├── FPSController.cs      # 第一人称控制器
│   └── FPSUIManager.cs       # UI管理器
├── Animation/
│   └── FPSAnimationController.cs  # 动画控制器
├── Weapon/
│   ├── WeaponManager.cs      # 武器管理器
│   ├── FPSWeapon.cs          # 武器基类
│   └── MeleeWeapon.cs        # 近战武器
├── Interaction/
│   ├── InteractionSystem.cs  # 交互系统
│   └── InteractableExamples.cs # 示例组件
└── Core/
    └── FPSCharacter.cs       # 角色核心组件
```

## 🚀 快速开始

### 1. 设置玩家角色

```csharp
// 1. 创建玩家对象
GameObject player = new GameObject("Player");

// 2. 添加组件
player.AddComponent<CharacterController>();
player.AddComponent<FPSController>();
player.AddComponent<FPSCharacter>();

// 3. 添加子对象
GameObject cameraHolder = new GameObject("CameraHolder");
cameraHolder.transform.SetParent(player.transform);
Camera camera = cameraHolder.AddComponent<Camera>();
```

### 2. 配置武器

```csharp
// 创建武器数据
WeaponData rifle = new WeaponData
{
    weaponName = "AK-47",
    weaponType = WeaponType.Rifle,
    maxAmmo = 30,
    reserveAmmo = 120,
    fireRate = 0.1f,
    damage = 25f
};

// 添加到武器管理器
weaponManager.AddWeapon(rifle);
```

### 3. 创建可交互物体

```csharp
// 门
Door door = gameObject.AddComponent<Door>();

// 物品拾取
ItemPickup pickup = gameObject.AddComponent<ItemPickup>();
```

## 🎯 武器类型支持

| 类型 | 枚举值 | 说明 |
|------|--------|------|
| 空手 | `Unarmed` | 无武器状态 |
| 刀 | `Knife` | 近战武器，连击系统 |
| 手枪 | `Pistol` | 半自动，快速切换 |
| 步枪 | `Rifle` | 自动/半自动，高伤害 |
| 霰弹枪 | `Shotgun` | 近距离高伤害 |
| 狙击枪 | `Sniper` | 远距离，高精度 |

## 🎬 动画参数

### 必需的Animator参数

```
// 移动
IsMoving (Bool)
IsSprinting (Bool)
IsCrouching (Bool)
IsGrounded (Bool)
Vertical (Float)
Horizontal (Float)

// 武器
WeaponType (Int)
IsAiming (Bool)
IsFiring (Bool)
IsReloading (Bool)
SwitchWeapon (Trigger)

// 近战
Attack (Trigger)
AttackType (Int)
Inspect (Trigger)
```

### WeaponType枚举值

```
Unarmed = 0
Knife = 1
Pistol = 2
Rifle = 3
Shotgun = 4
Sniper = 5
Grenade = 6
```

## 🔧 配置示例

### FPSController配置

```csharp
[SerializeField] private float walkSpeed = 5f;
[SerializeField] private float sprintSpeed = 10f;
[SerializeField] private float crouchSpeed = 2.5f;
[SerializeField] private float mouseSensitivity = 2f;
[SerializeField] private float jumpForce = 8f;
```

### 武器数据配置

```csharp
[SerializeField] private int maxAmmo = 30;
[SerializeField] private float fireRate = 0.1f;
[SerializeField] private float damage = 25f;
[SerializeField] private float range = 100f;
[SerializeField] private bool isAutomatic = true;
```

## 🎮 输入映射

| 功能 | 默认按键 | 输入名称 |
|------|----------|----------|
| 移动 | WASD | Horizontal, Vertical |
| 视角 | 鼠标 | Mouse X, Mouse Y |
| 跳跃 | Space | Jump |
| 冲刺 | Left Shift | - |
| 蹲伏 | Left Ctrl/C | - |
| 射击 | Left Click | Fire1 |
| 瞄准 | Right Click | Fire2 |
| 换弹 | R | - |
| 交互 | E | - |
| 切换武器 | 1-5, 滚轮 | - |

## 📝 事件系统

### FPSController事件

```csharp
controller.OnGroundStateChanged += (isGrounded) => { };
controller.OnCrouchStateChanged += (isCrouching) => { };
controller.OnSprintStateChanged += (isSprinting) => { };
controller.OnJump += () => { };
```

### WeaponManager事件

```csharp
weaponManager.OnWeaponSwitched += (index, data) => { };
weaponManager.OnAimStart += () => { };
weaponManager.OnAimEnd += () => { };
```

### FPSCharacter事件

```csharp
character.OnDamageTaken += (damage) => { };
character.OnDeath += () => { };
character.OnHealthChanged += (current, max) => { };
```

## 🔌 扩展指南

### 创建自定义武器

```csharp
public class Shotgun : FPSWeapon
{
    [SerializeField] private int pellets = 8;
    [SerializeField] private float spread = 0.1f;
    
    protected override void Fire()
    {
        for (int i = 0; i < pellets; i++)
        {
            Vector3 direction = GetSpreadDirection();
            // 射线检测...
        }
    }
}
```

### 创建自定义交互

```csharp
public class Switch : MonoBehaviour, IToggleable
{
    public bool IsOn { get; private set; }
    
    public bool Interact(InteractionSystem interactor)
    {
        Toggle(interactor);
        return true;
    }
    
    public void Toggle(InteractionSystem interactor)
    {
        IsOn = !IsOn;
        // 切换逻辑...
    }
    
    public string GetInteractionPrompt() => IsOn ? "关闭" : "打开";
    public bool CanInteract(InteractionSystem i) => true;
}
```

## 📋 依赖

- Unity 2020.3+
- TextMeshPro (用于UI)

## 📜 License

MIT License

---

Made with ♠️ by Kuuila Team
