# UIManager / MainController 实现说明

本次新增了两个核心脚本，用于把患者选择、扫码校验、分药流程与已有的硬件/处方模块串起来。

- `Assets/Scripts/MainIManager/MainController.cs`
  - 全局单例，启动即自动连接分药机（蓝牙）。
  - 从服务器拉取患者处方数据，生成 4x7 分药矩阵，驱动硬件完成分药。
  - 负责分盘切换提示、进度事件、完成/错误事件。
- `Assets/Scripts/UIManager/UIManager.cs`
  - 负责三个场景的 UI 绑定与事件：患者列表、扫码、分药界面。
  - 负责页面跳转、弹窗流程控制，以及把 UI 更新到最新的分药进度。

## 场景接线步骤（务必完成）

### 公共持久节点
1. 在首个场景（Home）中创建两个空物体，分别挂载 `MainController` 和 `DispenserController` 组件。
   - **无需手动设置**：这两个组件已在代码中自动实现了 `DontDestroyOnLoad`，场景切换时会自动保持不被销毁。
2. 将 `DispenserController` 组件拖到 `MainController` 的 `Dispenser Controller` 字段（或留空，脚本会自动查找）。
3. 在 `DispenserController` 的 Inspector 中填写蓝牙 MAC 地址（`Device Mac Address` 字段）。
4. 如需修改服务器地址或最大分药天数，在 `MainController` 的 Inspector 中调整 `Server Url` 与 `Max Dispensing Days`。

### Home 场景
- 在一个合适的空物体上挂载 `UIManager`（同一个脚本在三场景复用）。
- 填好字段：
  - `Patient List Root`：患者按钮父节点（VerticalLayout/Content）。
  - `Patient Button Prefab`：已做好的按钮预制，需包含 `Button` + 子节点 `Text`。
  - `Refresh Button`：刷新按钮。
  - `Home Hint Text`：提示文本（显示“请点击…”或“辛苦了…”）。
- 确认场景名与脚本中的 `homeSceneName` 一致（默认 Home）。

### Scan 场景
- 继续使用同一个 `UIManager`：
  - `Back To Home Button`：返回按钮。
  - `Light Bar`：光条的 `RectTransform`，脚本会循环上下移动。
  - `Light Bar Speed`：光条移动速度（像素/秒）。
  - `Scanner`：场景中的 `CheckPillBoxController` 组件实例。
  - `Correct Box Dialog` 与其 `Confirm Button`：扫码正确时的弹窗与确认按钮。
  - `Mismatch Dialog`、`Mismatch Home Button`、`Mismatch Retry Button`：错误药盒弹窗及按钮。
- 场景名需与 `scanSceneName` 一致（默认 Scan）。

### Dispense 场景
- 继续使用同一个 `UIManager`：
  - 左侧信息：`Total Pills Text`、`Medicine Name Text`、`Patient Name Text`、`Progress Percent Text`。
  - 右侧数药：`Pill Preview`（RawImage，用于摄像头画面，实际由 `PillCounterController` 驱动）、`Current Count Text`、`Capture Background Button`。
  - 分盘切换提示：`Plate Switch Dialog` 与 `Plate Switch Confirm Button`（当存在第二盘时弹出，确认后继续）。
  - 完成弹窗：`Complete Dialog` 与 `Complete Dialog Confirm Button`。
  - `Pill Counter Controller`：场景中的 `PillCounterController` 组件实例，便于点击“重新捕获背景”时触发。
- 场景名需与 `dispenseSceneName` 一致（默认 Dispense）。

### 其他组件摆放
- `CheckPillBoxController`：放在 Scan 场景，RawImage/状态文本等依旧由原脚本管理。
- `PillCounterController`：放在 Dispense 场景，保持其原有 UI 绑定（脚本会辅助调用 `CaptureBackground()`）。

## 运行时流程概览
1. 启动应用：`MainController` 自动尝试连接蓝牙并从服务器拉取当天处方，Home 列出患者卡片。
2. 刷新：点击刷新按钮会重新从服务器拉取，但已完成患者保持禁用状态。
3. 选择患者：点击未完成的患者卡片，切换到 Scan 场景。
4. 扫码：光条动画循环；扫码成功匹配患者 ID 后，自动打开舱门并弹出“放入药盒”确认框。
   - 确认：关闭舱门，生成分药计划，跳转到 Dispense 场景开始分药。
   - 不匹配：弹出“错误药盒”框，可重试或返回主页。
5. 分药：左侧实时显示药名、患者名、投入总数、进度百分比；右侧显示摄像头画面/当前计数。
   - 如果存在第二盘（餐后药），会弹出“更换盘”提示，确认后继续第二盘。
6. 完成：全部分完后自动打开舱门，弹出完成提示。点击确定后关闭舱门并返回 Home，当前患者卡片被禁用。

## 你需要做的事
- 确认三场景的名字与脚本中的 `homeSceneName` / `scanSceneName` / `dispenseSceneName` 一致，或在 Inspector 修改。
- 在 Inspector 逐一填好上面列出的引用，尤其是按钮、文本、预制体、弹窗节点，否则脚本不会生效。
- 确认 `DispenserController`、`CheckPillBoxController`、`PillCounterController` 已在各自场景配置正常（蓝牙地址、摄像头索引等）。
- 如需更换服务器地址，修改 `MainController.serverUrl`。
- 需要第二盘时，请在场景中提供一个“更换盘”提示弹窗并挂到对应字段。

## 注意与限制
- Dispense 场景的错误处理目前仅打印日志并弹出完成样式的弹窗；可按需替换为独立错误提示。
- 场景切换使用同步 `LoadScene`（Home->Scan），异步 `LoadSceneAsync`（Scan->Dispense）；如需添加过渡，替换对应调用即可。
- 硬件事件依赖蓝牙回调：若模拟器/编辑器运行，会走 `DispenserController` 的模拟发送逻辑。
- 代码中的中文字符串直接用于 UI 文本，可根据设计替换。

## 变更摘要
- 新增 `MainController`：管理患者列表、分药计划、硬件驱动、盘切换、完成/错误事件，启动即自动连接蓝牙。
- 新增 `UIManager`：三场景共用，负责患者列表生成、扫码流程、分药进度绑定、弹窗控制、场景跳转。
- 新增自动刷新功能：当服务器处方数据更新后，患者列表自动刷新。

---

# 自动刷新功能说明

## 概述

`MainController` 现在支持自动定时刷新患者列表。当服务器上的处方信息更新后，Home 页的患者卡片会自动更新，无需手动点击刷新按钮。

## Inspector 配置

在 `MainController` 组件的 **Auto Refresh** 区域：

| 字段名 | 说明 | 默认值 |
|--------|------|--------|
| `Enable Auto Refresh` | 是否启用自动刷新 | ✅ 启用 |
| `Auto Refresh Interval` | 刷新间隔（秒） | 30 秒 |
| `Min Refresh Interval` | 最小允许间隔（秒），防止服务器过载 | 10 秒 |

## 自动暂停机制

自动刷新在以下情况会自动暂停，避免干扰：
- 正在进行分药操作时（`isDispensing = true`）
- 手动调用 `PauseAutoRefresh()` 时

## 代码调用示例

```csharp
// 获取 MainController 实例
var main = MainController.Instance;

// 启用/禁用自动刷新
main.SetAutoRefreshEnabled(true);   // 启用
main.SetAutoRefreshEnabled(false);  // 禁用

// 修改刷新间隔（单位：秒）
main.SetAutoRefreshInterval(60f);   // 改为每 60 秒刷新一次

// 手动控制暂停/恢复
main.PauseAutoRefresh();   // 暂停
main.ResumeAutoRefresh();  // 恢复

// 完全停止/启动
main.StopAutoRefresh();    // 停止协程
main.StartAutoRefresh();   // 启动协程
```

## 注意事项

1. **网络开销**：频繁刷新会增加网络请求，建议间隔不低于 30 秒
2. **分药期间**：分药过程中自动跳过刷新，确保硬件通信不受干扰
3. **场景切换**：`MainController` 使用 `DontDestroyOnLoad`，自动刷新在所有场景持续运行
4. **首次加载**：启动时会立即执行一次刷新，然后按间隔定时刷新
- 新增 `HomePageController`：Home 页面右半部分子页面堆叠切换控制器。

---

# HomePageController 使用说明

## 概述

`HomePageController` 用于管理 Home 场景右半部分的多个堆叠子页面切换。目前支持三个子页面：
- **PatientCardPage**：患者卡片信息页面
- **CountPillsPage**：药品计数/分药概览页面  
- **SettingPage**：应用设置页面

## 组件配置步骤

### 1. 创建子页面容器
在 Home 场景中，于右半部分创建三个 Panel/空物体作为子页面容器：
- `PatientCardPage` - 放置患者卡片相关 UI
- `CountPillsPage` - 放置药品计数相关 UI
- `SettingPage` - 放置设置相关 UI

**注意**：这三个页面应放在同一父节点下，且位置重叠（堆叠），通过激活/隐藏来切换。

### 2. 创建导航按钮
创建三个 Button 用于切换页面：
- 患者卡片按钮
- 药品计数按钮
- 设置按钮

### 3. 挂载脚本
1. 在 Home 场景中创建一个空物体或使用现有的管理器物体
2. 添加 `HomePageController` 组件
3. 在 Inspector 中配置以下字段：

| 字段名 | 说明 |
|--------|------|
| Patient Card Page | 患者卡片页面 GameObject |
| Count Pills Page | 药品计数页面 GameObject |
| Setting Page | 设置页面 GameObject |
| Patient Card Button | 切换到患者卡片页面的按钮 |
| Count Pills Button | 切换到药品计数页面的按钮 |
| Setting Button | 切换到设置页面的按钮 |
| Default Page | 启动时默认显示的页面 |
| Active Button Color | 激活按钮的高亮颜色 |
| Inactive Button Color | 未激活按钮的颜色 |
| Use Button Highlight | 是否启用按钮高亮效果 |

## 代码调用示例

```csharp
// Get reference to the controller
HomePageController pageController = FindObjectOfType<HomePageController>();

// Switch to a specific page
pageController.ShowPage(HomeSubPage.Setting);

// Use convenience methods
pageController.GoToPatientCardPage();
pageController.GoToCountPillsPage();
pageController.GoToSettingPage();

// Navigate sequentially
pageController.GoToNextPage();
pageController.GoToPreviousPage();

// Reset to default
pageController.ResetToDefaultPage();

// Subscribe to page change events
pageController.OnPageChanged += (newPage) => {
    Debug.Log($"Page changed to: {newPage}");
};
```

## 扩展新页面

如需添加新的子页面：

1. 在 `HomeSubPage` 枚举中添加新值
2. 在 `HomePageController` 中添加对应的 `SerializeField` 变量
3. 在 `InitializeCollections()` 方法中添加映射
4. 在 `SetupButtonListeners()` 中添加按钮监听
5. 可选：添加便捷方法如 `GoToNewPage()`
