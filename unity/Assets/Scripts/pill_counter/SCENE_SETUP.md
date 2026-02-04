# Scene Setup Guide | 场景搭建指南

## English Version

### Quick Scene Setup (3 minutes)

#### 1. Create Main GameObject
1. In Hierarchy, right-click → `Create Empty`
2. Rename to `PillCounterSystem`

#### 2. Add Components
1. Select `PillCounterSystem`
2. Click `Add Component` → Search `PillCounterController`
3. Click `Add Component` → Search `PillCounterTest` (optional)

#### 3. Create UI (Optional but Recommended)

**Canvas Structure:**
```
Canvas (if not exists: UI → Canvas)
├── DisplayPanel (Panel)
│   ├── DisplayImage (RawImage) 
│   │   └── Set: Width=800, Height=600
│   └── Position: Center
│
├── InfoPanel (Panel)
│   ├── StatusText (Text)
│   │   └── Text: "Waiting..."
│   ├── PillCountText (Text)
│   │   └── Font Size: 24, Color: Green
│   └── Position: Top-Left
│
└── ButtonPanel (Panel)
    ├── CaptureButton (Button)
    │   └── Text: "Capture Background (B)"
    └── ResetButton (Button)
        └── Text: "Reset Background (R)"
    └── Position: Bottom
```

#### 4. Configure Inspector

Select `PillCounterSystem`, in `PillCounterController` component:

**Camera Settings:**
- Camera Index: `0` (try 1 or 2 if not working)
- Requested Width: `1280`
- Requested Height: `720`
- Requested FPS: `30`

**UI Components (Drag from Hierarchy):**
- Display Image: → `DisplayImage`
- Status Text: → `StatusText`
- Pill Count Text: → `PillCountText`
- Capture Background Button: → `CaptureButton`
- Reset Background Button: → `ResetButton`

#### 5. Test Run

1. Press `Play` ▶️
2. Wait for camera initialization (3-5 seconds)
3. Point camera at clean background
4. Wait for auto-capture (2 seconds) or press `B`
5. Place pills in view
6. Watch real-time counting!

---

## 中文版本

### 快速场景搭建（3分钟）

#### 1. 创建主GameObject
1. 在Hierarchy中右键 → `Create Empty`
2. 重命名为 `PillCounterSystem`

#### 2. 添加组件
1. 选中 `PillCounterSystem`
2. 点击 `Add Component` → 搜索 `PillCounterController`
3. 点击 `Add Component` → 搜索 `PillCounterTest`（可选）

#### 3. 创建UI（可选但推荐）

**Canvas结构：**
```
Canvas（如果没有：UI → Canvas）
├── DisplayPanel（Panel）
│   ├── DisplayImage（RawImage）
│   │   └── 设置：宽=800，高=600
│   └── 位置：居中
│
├── InfoPanel（Panel）
│   ├── StatusText（Text）
│   │   └── 文本："等待中..."
│   ├── PillCountText（Text）
│   │   └── 字体大小：24，颜色：绿色
│   └── 位置：左上角
│
└── ButtonPanel（Panel）
    ├── CaptureButton（Button）
    │   └── 文本："捕捉背景 (B)"
    └── ResetButton（Button）
        └── 文本："重置背景 (R)"
    └── 位置：底部
```

#### 4. 配置Inspector

选中 `PillCounterSystem`，在 `PillCounterController` 组件中：

**摄像头设置：**
- Camera Index：`0`（如果不工作尝试1或2）
- Requested Width：`1280`
- Requested Height：`720`
- Requested FPS：`30`

**UI组件（从Hierarchy拖拽）：**
- Display Image：→ `DisplayImage`
- Status Text：→ `StatusText`
- Pill Count Text：→ `PillCountText`
- Capture Background Button：→ `CaptureButton`
- Reset Background Button：→ `ResetButton`

#### 5. 测试运行

1. 按 `Play` ▶️
2. 等待摄像头初始化（3-5秒）
3. 将摄像头对准干净背景
4. 等待自动捕捉（2秒）或按 `B` 键
5. 在视野中放入药片
6. 观察实时计数！

---

## Keyboard Shortcuts | 快捷键

| Key | Function | 功能 |
|-----|----------|------|
| `B` | Capture Background | 捕捉背景 |
| `R` | Reset Background | 重置背景 |

---

## Visual Indicators | 可视化指示

| Color | Meaning | 含义 |
|-------|---------|------|
| 🟢 Green | Single pill | 单个药片 |
| 🔴 Red | Multiple pills | 多个药片 |
| 🟠 Orange | Reclassified | 重新分类 |
| 🟡 Yellow | Crop region | 裁切区域 |

---

## Minimal Setup (No UI) | 最小化配置（无UI）

If you don't want to create UI:

如果不想创建UI：

1. Create `PillCounterSystem` GameObject
   创建 `PillCounterSystem` GameObject
   
2. Add `PillCounterController` component
   添加 `PillCounterController` 组件
   
3. Set Camera Index = 0
   设置 Camera Index = 0
   
4. Press Play and use keyboard shortcuts
   按Play并使用快捷键

**That's it! Check Console for logs.**
**就这样！查看Console日志。**

---

## Troubleshooting | 问题排查

### Camera not working | 摄像头不工作
- Check Camera Index (try 0, 1, 2)
  检查Camera Index（尝试0, 1, 2）
- Check camera permissions
  检查摄像头权限
- View Console errors
  查看Console错误

### Background not capturing | 背景无法捕捉
- Keep camera steady
  保持摄像头稳定
- Remove objects from view
  移除画面中的物体
- Press `B` manually
  手动按 `B` 键

### Counting inaccurate | 计数不准确
- Check lighting (uniform)
  检查光照（均匀）
- Clean background
  干净的背景
- Spread pills apart
  分散药片
- Press `R` to reset
  按 `R` 重置

---

## Example Scene Hierarchy | 示例场景层级

```
Scene
├── Main Camera
├── Directional Light
├── Canvas
│   ├── DisplayPanel
│   │   └── DisplayImage (RawImage)
│   ├── InfoPanel
│   │   ├── StatusText
│   │   └── PillCountText
│   └── ButtonPanel
│       ├── CaptureButton
│       └── ResetButton
└── PillCounterSystem
    ├── PillCounterController
    └── PillCounterTest
```

---

## Next Steps | 下一步

### For Testing | 用于测试
→ Run and test immediately
→ 立即运行测试

### For Development | 用于开发
→ Read `README.md` for API details
→ 阅读 `README.md` 了解API详情

### For Production | 用于生产
→ Customize UI to match your app style
→ 自定义UI以匹配应用风格

---

## Tips | 提示

✅ **Best Environment | 最佳环境**
- Uniform lighting | 均匀光照
- Clean background | 干净背景
- Fixed camera | 固定摄像头
- 20-40cm height | 20-40厘米高度

✅ **Best Practice | 最佳实践**
- Capture empty background first | 先捕捉空背景
- Then place pills | 然后放入药片
- Keep pills separated | 保持药片分散
- Reset if inaccurate | 不准确时重置

---

## Performance | 性能

- **Resolution | 分辨率**: 1280x720 (recommended | 推荐)
- **FPS | 帧率**: 30 (smooth | 流畅)
- **CPU Usage | CPU使用**: ~15%
- **Memory | 内存**: ~50MB

For slower devices, use 640x480 @ 15 FPS
较慢设备使用 640x480 @ 15 FPS

---

**Created | 创建时间**: 2025-12-01  
**Version | 版本**: 1.0.0  
**Status | 状态**: ✅ Ready | 就绪
