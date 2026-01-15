# 药片计数系统 - 快速开始指南

## 🚀 5分钟快速集成

### 步骤1：准备场景（1分钟）

1. **创建或打开Unity场景**
2. **创建主要GameObject**
   - 右键 Hierarchy → Create Empty
   - 命名为 `PillCounterSystem`

### 步骤2：创建UI（2分钟）

右键Hierarchy → UI → Canvas，然后创建以下UI元素：

```
Canvas
├── Panel (背景面板，可选)
│   └── DisplayImage (RawImage)
│       - Width: 800, Height: 600
│       - 用于显示摄像头画面
│
├── StatusText (Text)
│   - 位置: 左上角
│   - 显示状态信息
│
├── PillCountText (Text)
│   - 位置: 左上角StatusText下方
│   - Font Size: 24
│   - Color: Green
│
├── ButtonPanel (Panel, 可选)
│   ├── CaptureBackgroundButton (Button)
│   │   - Text: "捕捉背景 (B)"
│   │
│   └── ResetBackgroundButton (Button)
│       - Text: "重置背景 (R)"
```

### 步骤3：添加脚本（1分钟）

1. **选中`PillCounterSystem` GameObject**
2. **在Inspector中点击`Add Component`**
3. **搜索并添加 `PillCounterController`**
4. **再次`Add Component`，添加 `PillCounterTest`**（测试用，可选）

### 步骤4：配置组件（1分钟）

在`PillCounterController`组件中：

#### 摄像头设置
- **Camera Index**: `0` （如果有多个摄像头，尝试1, 2...）
- **Requested Width**: `1280`
- **Requested Height**: `720`
- **Requested FPS**: `30`

#### UI组件（拖拽对应的UI元素）
- **Display Image**: 拖入 `DisplayImage (RawImage)`
- **Status Text**: 拖入 `StatusText`
- **Pill Count Text**: 拖入 `PillCountText`
- **Capture Background Button**: 拖入 `CaptureBackgroundButton`
- **Reset Background Button**: 拖入 `ResetBackgroundButton`

### 步骤5：运行测试（立即）

1. **点击Unity的Play按钮▶️**
2. **等待摄像头启动**（几秒钟）
3. **将摄像头对准空白桌面**
4. **等待自动捕捉背景**（约2秒稳定后）或**按键盘B键手动捕捉**
5. **在摄像头视野内放入药片**
6. **观察实时计数结果**

---

## 🎮 控制方式

### 键盘快捷键
- **B** - 手动捕捉背景
- **R** - 重置背景（重新开始）

### UI按钮
- **捕捉背景** - 点击手动捕捉当前画面作为背景
- **重置背景** - 点击清除背景，重新捕捉

---

## 🎯 最简单的测试场景

如果不想创建完整UI，最小化配置：

### 最小配置（无UI）
```
Hierarchy:
└── PillCounterSystem
    └── PillCounterController (Camera Index = 0)
```

这样也可以工作！使用快捷键B和R控制，查看Console日志。

---

## 📊 实时信息显示

### StatusText 显示内容
- 等待背景: `"等待场景稳定... (边缘数: 1234)"`
- 背景已捕捉: `"检测中 - 单个:3 多个:1"`
- 错误状态: `"错误: [错误信息]"`

### PillCountText 显示内容
- `"药片数量: 5"`

### 可视化颜色
- **绿色轮廓** - 单个药片
- **红色轮廓** - 多个药片（粘连）
- **橙色轮廓** - 重新分类的药片
- **黄色矩形** - 裁切区域边界

---

## 🔧 常见调整

### 摄像头找不到？
```csharp
// 在PillCounterController的Inspector中
Camera Index = 1  // 尝试不同的索引
```

### 画质不好？
```csharp
Requested Width = 1920
Requested Height = 1080
```

### 帧率太低？
```csharp
Requested Width = 640
Requested Height = 480
Requested FPS = 15
```

---

## 📝 代码使用示例

### 最简单的使用
```csharp
using EZDose.PillCounter;

public class MyScript : MonoBehaviour
{
    public PillCounterController pillCounter;
    
    void Update()
    {
        // 获取当前药片数量
        int count = pillCounter.GetCurrentPillCount();
        Debug.Log($"药片: {count}");
    }
}
```

---

## 🎬 使用流程

```
1. 启动应用
   ↓
2. 等待摄像头初始化（3-5秒）
   ↓
3. 对准空白背景
   ↓
4. 系统自动检测稳定并捕捉背景（2秒）
   或按B键手动捕捉
   ↓
5. 放入药片
   ↓
6. 实时显示计数结果
   ↓
7. 如需重新计数，按R键重置
```

---

## ⚠️ 注意事项

### 环境要求
1. **光照均匀** - 避免强烈阴影
2. **背景纯净** - 空白桌面最佳
3. **摄像头稳定** - 不要晃动
4. **药片分散** - 避免完全重叠

### 最佳实践
1. 先对准空白背景捕捉
2. 然后再放入药片
3. 如果计数不准，按R重置再来一次
4. 药片尽量平铺，不要堆叠

---

## 🐛 问题排查

### 问题1: 摄像头黑屏
**解决**: 
- 检查Camera Index（尝试0, 1, 2）
- 检查摄像头权限
- 查看Console错误信息

### 问题2: 无法捕捉背景
**解决**:
- 确保摄像头不晃动
- 按B键手动捕捉
- 移除画面中的杂物

### 问题3: 计数不准
**解决**:
- 检查光照（是否均匀）
- 药片是否分散
- 按R键重置背景重试

---

## 📱 移动端配置

### Android
1. **File → Build Settings → Android**
2. **Edit → Project Settings → Player**
3. **Other Settings → Camera Usage Description**: "用于药片计数"
4. **Build and Run**

### iOS
1. **File → Build Settings → iOS**
2. **Edit → Project Settings → Player**
3. **Other Settings → Camera Usage Description**: "用于药片计数"
4. **Build and Run in Xcode**

---

## 🎓 学习资源

- **完整文档**: 查看 `README.md`
- **Python参考**: 查看 `../PythonRef/pill_counter.py`
- **测试脚本**: 查看 `PillCounterTest.cs`

---

## ✅ 检查清单

- [ ] 创建了PillCounterSystem GameObject
- [ ] 添加了PillCounterController组件
- [ ] 配置了摄像头索引
- [ ] 创建了UI元素（可选）
- [ ] 连接了UI引用（可选）
- [ ] 点击Play测试
- [ ] 摄像头画面正常显示
- [ ] 能够捕捉背景
- [ ] 药片计数正常工作

---

**准备好了吗？点击Play开始测试！** 🚀

**预计首次运行时间**: < 10秒  
**预计学习时间**: < 5分钟
