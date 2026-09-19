import 'package:flutter/material.dart';

abstract final class MdisColors {
  static const background = Color(0xFFF7F7F7);
  static const surface = Colors.white;
  static const sidebar = Color(0xFFF3F3F3);
  static const accent = Color(0xFF343536);
  static const accentSoft = Color(0xFFECECED);
  static const avatar = Color(0xFFAAADB0);
  static const ink = Color(0xFF242526);
  static const muted = Color(0xFF717478);
  static const line = Color(0xFFE0E1E3);
  static const primary = Color(0xFF343536);
  static const primarySoft = Color(0xFFECECED);
}

/// Windows typography: size / line height, weight and zero tracking.
abstract final class MdisType {
  static const _base = TextStyle(
    fontFamily: 'Segoe UI Variable',
    fontFamilyFallback: ['Microsoft YaHei UI', 'Microsoft YaHei', 'sans-serif'],
    color: MdisColors.ink,
    fontSize: 14,
    height: 20 / 14,
    letterSpacing: 0,
    fontWeight: FontWeight.w400,
  );
  static final display = _base.copyWith(
    fontSize: 32,
    height: 40 / 32,
    fontWeight: FontWeight.w600,
  );
  static final pageTitle = _base.copyWith(
    fontSize: 24,
    height: 32 / 24,
    fontWeight: FontWeight.w600,
  );
  static final sectionTitle = _base.copyWith(
    fontSize: 20,
    height: 28 / 20,
    fontWeight: FontWeight.w600,
  );
  static final cardTitle = _base.copyWith(
    fontSize: 16,
    height: 24 / 16,
    fontWeight: FontWeight.w600,
  );
  static final bodyStrong = _base.copyWith(
    fontSize: 14,
    height: 20 / 14,
    fontWeight: FontWeight.w600,
  );
  static final body = _base.copyWith(fontSize: 14, height: 20 / 14);
  static final label = _base.copyWith(
    fontSize: 12,
    height: 16 / 12,
    fontWeight: FontWeight.w600,
  );
  static final caption = _base.copyWith(fontSize: 12, height: 16 / 12);
  static final monospace = body.copyWith(fontFamily: 'Cascadia Mono');
}

ThemeData buildMdisTheme() => ThemeData(
  useMaterial3: true,
  scaffoldBackgroundColor: MdisColors.background,
  fontFamily: 'Segoe UI Variable',
  fontFamilyFallback: const [
    'Microsoft YaHei UI',
    'Microsoft YaHei',
    'sans-serif',
  ],
  textTheme: TextTheme(
    displayLarge: MdisType.display,
    displayMedium: MdisType.display,
    displaySmall: MdisType.display,
    headlineLarge: MdisType.pageTitle,
    headlineMedium: MdisType.pageTitle,
    headlineSmall: MdisType.pageTitle,
    titleLarge: MdisType.sectionTitle,
    titleMedium: MdisType.cardTitle,
    titleSmall: MdisType.bodyStrong,
    bodyLarge: MdisType.body,
    bodyMedium: MdisType.body,
    bodySmall: MdisType.caption,
    labelLarge: MdisType.bodyStrong,
    labelMedium: MdisType.label,
    labelSmall: MdisType.label,
  ),
  colorScheme: const ColorScheme.light(
    primary: MdisColors.primary,
    onPrimary: Colors.white,
    secondary: MdisColors.accent,
    onSecondary: MdisColors.ink,
    outline: MdisColors.line,
    surface: MdisColors.surface,
    onSurface: MdisColors.ink,
  ),
  dividerColor: MdisColors.line,
  filledButtonTheme: FilledButtonThemeData(
    style: FilledButton.styleFrom(
      minimumSize: const Size(48, 48),
      textStyle: MdisType.bodyStrong,
    ),
  ),
  outlinedButtonTheme: OutlinedButtonThemeData(
    style: OutlinedButton.styleFrom(
      minimumSize: const Size(48, 48),
      textStyle: MdisType.bodyStrong,
      side: const BorderSide(color: MdisColors.line),
    ),
  ),
  textButtonTheme: TextButtonThemeData(
    style: TextButton.styleFrom(
      minimumSize: const Size(48, 48),
      textStyle: MdisType.bodyStrong,
    ),
  ),
  snackBarTheme: SnackBarThemeData(
    contentTextStyle: MdisType.body.copyWith(color: Colors.white),
  ),
  tooltipTheme: TooltipThemeData(
    textStyle: MdisType.caption.copyWith(color: Colors.white),
  ),
  inputDecorationTheme: InputDecorationTheme(
    filled: true,
    fillColor: Colors.white,
    hintStyle: MdisType.body.copyWith(color: MdisColors.muted),
    border: OutlineInputBorder(
      borderRadius: BorderRadius.circular(28),
      borderSide: BorderSide.none,
    ),
    enabledBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(28),
      borderSide: BorderSide.none,
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(28),
      borderSide: const BorderSide(color: MdisColors.accent),
    ),
    contentPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
  ),
);
