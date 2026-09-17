# Mdis Design System 初始约定

本文件是待实现设计规范，颜色数值、字体家族与组件尺寸须经 Windows/Pad 原型验证后定稿。
语义颜色：surface、text、primary、success、warning、danger、offline；禁止状态仅用颜色区分。
字体层级：页面标题、患者名、药物名、投入数量、正文、辅助信息；投药数量优先级最高。
间距以 4/8 为基础尺度；圆角区分容器和控件；动效用于状态连续性，遵守减少动效偏好。
基础组件：Sidebar、PatientCard、Button、StatusBadge、MachineStatus、确认对话框、错误提示。
所有组件包含正常、禁用、忙碌、错误与键盘焦点状态；支持触控、键盘与辅助技术。
在组件库确立前不大量实现业务页面。Flutter 默认 Material 空白页只作启动验证，不代表最终视觉方向。
