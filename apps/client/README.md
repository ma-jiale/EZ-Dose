# Mdis Flutter Client

Flutter 3.47.1 / Dart 3.13.1，支持 Windows 与 Android 平板。

已实现工作台、患者与处方管理、药盒核对、逐药摆药与补药、记录、设备/校准/数药、设置、账户及成员管理 UI。延续黑白灰界面和 Windows 八级排版；系统字体从本机读取，头像缺失时显示占位。

当前为内存数据和本地模拟设备：无后端、真实认证、网络同步、持久化或物理设备操作。重启恢复虚构数据。登录表单不会将未验证密码当作认证成功，可通过“进入本机工作区”查看账户页面。

```sh
flutter pub get
flutter analyze
flutter test
flutter run -d windows
# 或启动平板模拟器后
flutter run -d emulator-5554
```

体验主流程：右上角设备 → 查找并连接“本地测试设备” → 打开 B-302 → 输入患者编号 `000001` 核对药盒 → 校准与药盘核对 → 开始分药。设备详情可指定下一次异常反馈，验证人工核对 UI。

[页面清单、V1 依据与验收路径](../../docs/client-ui.md) · [首页设计与字体规范](../../docs/workbench-design.md)

Windows 需要 C++ 桌面开发工具链；Android 需要 SDK/JDK。仅增加 Flutter SDK 自带的中文本地化支持；未修改后端、legacy、hardware。包名 mdis_client，正式发布前需确认应用标识。
