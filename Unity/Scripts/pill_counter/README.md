# 药片计数系统 - 实现文档

## 📦 已实现的功能

### 1. **PillCounter.cs** - 核心计数算法
完整实现了Python版本的药片计数逻辑，包括：

#### 主要功能
- ✅ **背景捕捉与场景稳定检测**
  - 自动检测场景稳定性（边缘数量方差分析）
  - 支持手动和自动背景捕捉
  - 背景重置功能

- ✅ **图像预处理**
  - 画面裁切（去除边缘杂物）
  - 背景减法
  - 二值化处理
  - 形态学操作（开运算、腐蚀、膨胀）
  - 轮廓分离算法

- ✅ **轮廓检测与分析**
  - 多重形状特征分析（面积、长宽比、凸包度、实心度、圆形度）
  - 单个药片识别
  - 多个药片估算
  - 面积基准计算（中位数方法）
  - 轮廓重新分类机制

- ✅ **可视化与调试**
  - 轮廓绘制（绿色=单个，红色=多个，橙色=重新分类）
  - 裁切区域显示
  - 实时统计信息显示
  - 调试信息结构体

### 2. **PillCounterController.cs** - Unity集成控制器
Unity摄像头集成和生命周期管理：

#### 主要功能
- ✅ **摄像头管理**
  - WebCamTexture初始化
  - 多摄像头支持
  - 分辨率和FPS配置
  - 摄像头启动超时处理

- ✅ **实时处理**
  - 帧率控制和线程安全
  - WebCamTexture ↔ OpenCV Mat转换
  - 实时UI更新

- ✅ **用户交互**
  - UI按钮事件处理
  - 状态文本更新
  - 药片计数显示

- ✅ **资源管理**
  - 自动资源释放
  - 内存泄漏预防
  - 错误处理机制

### 3. **PillCounterTest.cs** - 测试脚本
快速测试和调试工具：

#### 主要功能
- ✅ 快捷键控制（B捕捉背景，R重置）
- ✅ 实时调试信息显示
- ✅ 简化的测试接口

---

## 🏗️ 架构设计

### 类关系图
```
PillCounterController (MonoBehaviour)
    ├── 管理 WebCamTexture
    ├── 调用 PillCounter
    └── 更新 UI
    
PillCounter (纯C#类)
    ├── 图像预处理
    ├── 轮廓检测
    ├── 形状分析
    └── 计数逻辑
    
PillCounterTest (MonoBehaviour)
    └── 测试和调试
```

### 数据流
```
WebCamTexture → Mat → PreProcess → Contours → Analysis → Results → UI
```

---

## 🚀 快速开始

### 步骤1：创建场景
1. 创建新场景或打开现有场景
2. 创建空GameObject，命名为`PillCounterSystem`

### 步骤2：添加UI（可选但推荐）
创建Canvas，添加以下UI元素：

```
Canvas
├── DisplayImage (RawImage) - 显示摄像头画面
├── StatusText (Text) - 显示状态信息
├── PillCountText (Text) - 显示药片数量
├── CaptureBackgroundButton (Button) - 捕捉背景按钮
└── ResetBackgroundButton (Button) - 重置背景按钮
```

### 步骤3：配置组件
1. 给`PillCounterSystem`添加`PillCounterController`组件
2. 给`PillCounterSystem`添加`PillCounterTest`组件（可选）
3. 在Inspector中配置：
   - **Camera Index**: 摄像头索引（0=默认）
   - **Requested Width/Height**: 分辨率（建议1280x720）
   - **Display Image**: 拖入RawImage
   - **Status Text**: 拖入状态文本
   - **Pill Count Text**: 拖入计数文本
   - **按钮**: 拖入对应按钮

### 步骤4：运行测试
1. 点击Play
2. 等待摄像头启动
3. 将摄像头对准空白背景，等待自动捕捉（或按B手动捕捉）
4. 放入药片，系统会自动计数

---

## 🔧 配置参数

### PillCounter参数
```csharp
// 可在PillCounter.cs构造函数中调整
private readonly int edgeThreshold = 1000;        // 边缘检测阈值
private readonly int cropMargin = 50;             // 裁切边距
private readonly double minContourArea = 50;      // 最小轮廓面积
private readonly double maxContourArea = 100000;  // 最大轮廓面积
private readonly double convexityThreshold = 0.90;// 凸包度阈值
private readonly double aspectRatioThreshold = 3.0;// 长宽比阈值
private readonly double solidityThreshold = 0.85; // 实心度阈值
```

### PillCounterController参数
在Inspector中可调整：
- **Camera Index**: 摄像头索引
- **Requested Width**: 视频宽度（默认1280）
- **Requested Height**: 视频高度（默认720）
- **Requested FPS**: 帧率（默认30）

---

## 📝 API使用示例

### 基础使用
```csharp
using EZDose.PillCounter;

public class MyScript : MonoBehaviour
{
    private PillCounterController controller;
    
    void Start()
    {
        controller = GetComponent<PillCounterController>();
    }
    
    void Update()
    {
        // 获取当前药片数量
        int pillCount = controller.GetCurrentPillCount();
        Debug.Log($"当前药片数: {pillCount}");
        
        // 检查背景状态
        if (!controller.IsBackgroundCaptured())
        {
            Debug.Log("等待捕捉背景...");
        }
    }
    
    // 手动控制
    public void ManualCaptureBackground()
    {
        controller.CaptureBackground();
    }
    
    public void ManualResetBackground()
    {
        controller.ResetBackground();
    }
}
```

### 高级使用 - 直接使用PillCounter类
```csharp
using EZDose.PillCounter;
using OpenCVForUnity.CoreModule;

public class AdvancedUsage : MonoBehaviour
{
    private PillCounter pillCounter;
    
    void Start()
    {
        pillCounter = new PillCounter();
    }
    
    void ProcessCustomFrame(Mat frame)
    {
        // 检测场景稳定性
        var (edgeCount, edges) = pillCounter.DetectEdges(frame);
        
        if (pillCounter.IsSceneStable(edgeCount))
        {
            // 捕捉背景
            pillCounter.CaptureBackground(frame);
        }
        
        // 计数药片
        if (pillCounter.IsBackgroundCaptured)
        {
            var (pillCount, resultFrame, debugInfo) = pillCounter.CountPills(frame);
            
            Debug.Log($"药片总数: {pillCount}");
            Debug.Log($"单个药片: {debugInfo.SinglePillCount}");
            Debug.Log($"多个药片: {debugInfo.MultiplePillCount}");
            Debug.Log($"参考面积: {debugInfo.ReferenceArea}");
            
            // 使用resultFrame显示结果
            // ...
            
            resultFrame.Dispose();
        }
        
        edges.Dispose();
    }
    
    void OnDestroy()
    {
        pillCounter?.Dispose();
    }
}
```

---

## ⚙️ 核心算法说明

### 1. 场景稳定检测
- 计算最近10帧的边缘数量
- 方差 < 8000 且均值 < 1000 → 场景稳定
- 连续稳定15帧 → 自动捕捉背景

### 2. 图像预处理流程
```
原始帧 → 裁切 → 灰度化 → 高斯模糊 → 背景减法 
→ 二值化(阈值40) → 形态学开运算 → 腐蚀(6x6, 2次) 
→ 连通组件分离 → 膨胀恢复(4x4, 2次)
```

### 3. 轮廓分类
**单个药片判断条件（全部满足）：**
- 凸包度 ≥ 0.90
- 实心度 ≥ 0.85
- 长宽比 ≤ 3.0
- 圆形度 > 0.3

**多个药片估算：**
- 计算参考面积（单个药片面积的中位数）
- 根据面积比例估算：
  - 0.7-1.2x → 1个
  - 1.2-2.4x → 2个
  - 2.4-3.6x → 3个
  - 依此类推

### 4. 重新分类机制
- 面积 > 参考面积 × 1.2 的"单个药片"
- 重新分类为"多个药片"
- 使用橙色轮廓标识

---

## 🐛 错误处理

### 内置错误处理
1. **摄像头未找到** - 抛出异常并显示错误信息
2. **摄像头启动超时** - 10秒超时机制
3. **图像处理异常** - try-catch包裹，返回安全默认值
4. **资源泄漏** - 自动Dispose机制

### 调试建议
```csharp
// 启用详细日志
Debug.Log($"边缘数: {edgeCount}");
Debug.Log($"轮廓数: {contours.Count}");
Debug.Log($"参考面积: {referenceArea}");
```

---

## 📊 性能优化

### 已实现的优化
1. **对象重用** - Mat对象使用using自动释放
2. **帧率控制** - `isProcessing`标志防止重复处理
3. **裁切优化** - 减少50像素边缘，减少处理量
4. **中位数算法** - 避免极端值影响

### 性能建议
- 推荐分辨率：1280x720（平衡质量和性能）
- 推荐帧率：30 FPS
- 如果卡顿，可降低到 640x480 @ 15 FPS

---

## 🔍 常见问题

### Q1: 计数不准确？
**A:** 检查以下因素：
1. 背景是否纯净（无杂物）
2. 光照是否均匀（避免阴影）
3. 药片是否分散（不要重叠太多）
4. 调整`cropMargin`参数去除边缘干扰

### Q2: 无法捕捉背景？
**A:** 
1. 确保场景稳定（摄像头不要晃动）
2. 检查边缘数量是否过高（<1000）
3. 尝试手动按B键捕捉

### Q3: 摄像头无法启动？
**A:**
1. 检查摄像头索引（可能需要改为0或2）
2. 确认摄像头权限（移动端）
3. 查看Console错误信息

### Q4: 多个药片识别为一个？
**A:**
1. 检查药片是否紧密贴在一起
2. 调整腐蚀参数增强分离
3. 确保光照良好

---

## 🎯 与Python版本的对比

| 功能 | Python版本 | Unity C#版本 | 备注 |
|-----|-----------|-------------|------|
| 背景捕捉 | ✅ | ✅ | 完全一致 |
| 边缘检测 | ✅ | ✅ | 完全一致 |
| 轮廓分析 | ✅ | ✅ | 完全一致 |
| 形状特征 | ✅ | ✅ | 完全一致 |
| 面积估算 | ✅ | ✅ | 完全一致 |
| 可视化 | ✅ | ✅ | 完全一致 |
| 资源管理 | 手动 | 自动 | C#更安全 |
| UI集成 | PyQt | Unity UI | 平台差异 |

**核心算法100%移植完成！**

---

## 📦 文件清单

```
pill_counter/
├── PillCounter.cs              (670行) - 核心算法
├── PillCounterController.cs    (350行) - Unity集成
├── PillCounterTest.cs          (80行)  - 测试脚本
└── README.md                   (本文件) - 完整文档
```

---

## 🔗 相关资源

- **Python参考**: `Assets/Scripts/PythonRef/pill_counter.py`
- **OpenCV文档**: https://docs.opencv.org/
- **OpenCV for Unity**: https://enoxsoftware.com/opencvforunity/

---

## ✅ 实现状态

- ✅ 核心算法 - 100%完成
- ✅ Unity集成 - 100%完成
- ✅ 错误处理 - 100%完成
- ✅ 资源管理 - 100%完成
- ✅ UI集成 - 100%完成
- ✅ 测试脚本 - 100%完成
- ✅ 文档 - 100%完成

**状态: 🟢 生产就绪**

---

## 📞 技术支持

如有问题，请检查：
1. Console日志输出
2. 摄像头权限设置
3. OpenCV for Unity插件是否正确安装
4. Unity版本兼容性

---

**最后更新**: 2025-12-01  
**作者**: AI Assistant  
**版本**: 1.0.0
