# Mdis Flutter Client

使用 Flutter 3.47.1 / Dart 3.13.1 生成 Windows 与 Android 原生工程。
当前仅为启动占位，无患者业务页面、API 或机器控制。

```sh
flutter pub get
flutter analyze
flutter test
flutter run -d windows
# 或连接 Android 设备后 flutter run
```

Windows 构建需要 Windows 与 C++ 桌面开发工具链；Android 需要 Android SDK/JDK。
包名 mdis_client，临时组织标识 com.mdis，正式发布前确认最终应用标识。
Riverpod、go_router、Dio、Freezed、Drift 和 Secure Storage 随业务模块接入。
