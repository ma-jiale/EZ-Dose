import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';

import 'theme.dart';
import 'domain/store.dart';
import 'windows_fonts.dart';
import 'workbench/workbench_page.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await loadWindowsFonts();
  runApp(const MdisApp());
}

class MdisApp extends StatefulWidget {
  const MdisApp({super.key});
  @override
  State<MdisApp> createState() => _MdisAppState();
}

class _MdisAppState extends State<MdisApp> {
  final store = AppStore();
  @override
  void dispose() {
    store.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AppScope(
    store: store,
    child: MaterialApp(
      title: 'Mdis',
      locale: const Locale('zh', 'CN'),
      supportedLocales: const [Locale('zh', 'CN')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      debugShowCheckedModeBanner: false,
      theme: buildMdisTheme(),
      builder: (context, child) {
        final settings = AppScope.of(context);
        final media = MediaQuery.of(context);
        return MediaQuery(
          data: media.copyWith(
            textScaler: TextScaler.linear(
              media.textScaler.scale(1) * settings.textScale,
            ),
          ),
          child: child!,
        );
      },
      home: const WorkbenchPage(),
    ),
  );
}
