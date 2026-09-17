# AGENT.md — AI 编程 Agent 工作规则

## 项目概览

- **项目定位**：EZ-Dose 养老院智能摆药（分药）机后端服务器，采用 Flask + SQLite 架构。
- **双重角色**：
  - **Web 管理端**：为护士和管理员提供可视化管理后台（用户、患者、处方、发药记录及操作日志）。
  - **摆药机 API**：为摆药机硬件终端提供患者/处方同步、发药日志上报以及视觉标定（药片面积与实物照）接口。
- **阶段目标**：项目处于 MVP 阶段，功能完整且可用。当前分支 `feature/ux-improvement` 的目标是**在不破坏任何现有功能和 API 行为的前提下，实现 UI/UX 的全面升级与体验优化**。

---

## 运行环境与虚拟环境要求 ⚠️

> [!IMPORTANT]
> **Python 运行环境限制**：在执行任何 Python 命令（例如运行服务器、执行测试脚本、安装依赖包等）时，**必须且一定要**使用 `pill-dispenser` 虚拟环境。

在每次执行 Python 相关操作前，请确保已激活该环境：
```bash
# 激活 conda 虚拟环境
conda activate pill-dispenser
```
或通过 conda 指定环境运行：
```bash
conda run -n pill-dispenser python <script.py>
```

---

## 黄金法则（最高优先级，先读这一段）

1. `design-system.md` 是 UI 的**唯一事实来源**。任何 UI 改动前必须先读完它。
2. **只能**使用设计系统里定义的 token 和组件：禁止自定义十六进制色值、禁止间距魔法数字、禁止野生 CSS。
3. **默认只改 UI**（模板 / 样式 / 前端交互）。未经我明确许可，不得改动后端逻辑、路由、数据库结构或 API 同步逻辑。
4. **一次只做一个页面或一个组件**，禁止一次性重构整个应用。
5. **先出方案再写代码**：先用自然语言说明"准备改哪些文件、怎么改、为什么"，等我确认后再动手。
6. 改完后给出 **diff 摘要**，并逐条说明"每处改动解决了什么体验问题"。

---

## 核心业务逻辑与技术保护区（严禁破坏）

在进行 UI/UX 优化时，以下关键核心技术实现必须得到保护，其交互行为和参数不能被修改或破坏：

### 1. 精臣 (JC) 打印机 SDK 及 WebSocket 接口
- **核心文件**：位于 [static/js/printer/](file:///D:/WorkSpace/Dispenser/EZ-Dose-server/static/js/printer/) 中的三个核心 JS 文件。
- **运行机制**：前端通过 WebSocket 与本地运行的精臣打印服务进行通信，实现单张/批量标签的即时物理打印。
- **UI 优化要求**：
  - 可以美化 [patients.html](file:///D:/WorkSpace/Dispenser/EZ-Dose-server/templates/patients.html) 中的“连接打印机”模态框、打印数量输入框和状态指示灯。
  - **绝对不能**破坏 `PrinterManager` 相关的事件监听（`onServiceDisconnected`, `onPrinterDisconnected` 等）和打印执行逻辑（`printLabel()`）。

### 2. ReportLab PDF 标签及条形码生成
- **核心逻辑**：[main.py](file:///D:/WorkSpace/Dispenser/EZ-Dose-server/main.py) 中的 `DashedLabelFlowable` 和 `ZeroPaddingDocTemplate` 类，用于批量导出 A4/特定规格的 PDF 标签。
- **排版参数**：标签尺寸严格遵循 22mm x 22mm 方形，条形码使用 6 位零填充患者 ID 生成 Code128 码。
- **UI 优化要求**：
  - 这是后端生成的 PDF 文件，不要修改 `main.py` 中 PDF 绘制的坐标值和逻辑，除非明确要求调整 PDF 导出排版。

### 3. 数据同步 COALESCE 保护机制
- **核心路由**：`/packer/prescriptions/upload`。
- **保护设计**：设备同步上传处方时，若客户端发送的 `pill_size_area` 或 `image_resource_id` 为空/零，服务端通过 SQLite `COALESCE(?, pill_size_area)` 机制予以保留，确保摆药机在多次同步中不会意外覆盖已在设备端校准完成的药片视觉数据。
- **UI 优化要求**：
  - 修改处方表单和编辑逻辑时，不得影响该接口的数据同步行为。

---

## 技术栈与约束

- **后端**：Flask + Jinja2（保持服务端渲染，严禁引入 React/Vue/重型构建工具）。
- **样式**：Tailwind CSS (Utility-first) + Flowbite (基于 Tailwind 的组件库)。
- **交互**：优先使用 **Alpine.js**（轻量交互）和 **HTMX**（异步局部刷新），尽量减少手写原生复杂 JS。
- **图标**：统一使用 Heroicons 或 Lucide（通过 CDN 或 SVG 嵌入，全站统一）。
- **可访问性**：必须确保对比度符合 WCAG AA 标准，所有交互组件均需包含完整的状态样式（Default, Hover, Focus, Active, Disabled, Loading）。

---

## 文件与目录约定

- **模板目录**：[templates/](file:///D:/WorkSpace/Dispenser/EZ-Dose-server/templates/)
- **静态资源**：[static/](file:///D:/WorkSpace/Dispenser/EZ-Dose-server/static/)
- **Tailwind 开发约定**：
  - 样式输入文件：`static/src/main.css`
  - 编译输出文件：`static/dist/main.css`
  - 配置文件：根目录下的 `tailwind.config.js`
- **重构规则**：改动一个页面时，优先将可复用的部分（如导航栏、输入组件等）抽离为 Jinja 宏或 partials，禁止简单粗暴地复制粘贴 HTML。

---

## 常用命令

在使用以下 Python 常用命令前，**请确保已激活 `pill-dispenser` 环境**：

```bash
# 激活环境
conda activate pill-dispenser

# 1. 初始化依赖（如果尚未安装）
npm init -y
npm install -D tailwindcss postcss autoprefixer flowbite alpinejs

# 2. 开发时实时监听并编译 Tailwind 样式
npx tailwindcss -i ./static/src/main.css -o ./static/dist/main.css --watch

# 3. 生产环境构建编译（压缩样式）
npx tailwindcss -i ./static/src/main.css -o ./static/dist/main.css --minify

# 4. 运行 Flask 服务器
python main.py
```

---

## 优化任务自检清单

- [ ] 确保 `tailwind.config.js` 和 `package.json` 配置正确，样式编译正常。
- [ ] 所有交互元素均定义了 Default/Hover/Focus/Active/Disabled 状态，异步提交包含 Loading 反馈。
- [ ] 页面在移动端和桌面端响应式布局均正常，信息层级清晰。
- [ ] 所有表单均有实时校验与人话级错误提示。
- [ ] 各种表格和列表增加了优雅的空状态（Empty State）展示。
- [ ] **精臣打印机连接及交互在 UI 美化后测试依然完全正常**。
- [ ] **ReportLab 导出的 PDF 标签格式与条形码数据正确无误**。
- [ ] 未改动任何后端接口路由及数据库逻辑。
- [ ] **确认所有 Python 操作均在 `pill-dispenser` 虚拟环境下执行**。
