import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';

import 'font_collection.dart';

/// Load locally licensed Windows fonts; never redistribute them in assets.
Future<void> loadWindowsFonts() async {
  if (!Platform.isWindows) return;
  final root = Platform.environment['WINDIR'] ?? r'C:\Windows';
  for (final entry in const {
    'Segoe UI Variable': ['SegUIVar.ttf'],
    'Microsoft YaHei UI': ['msyh.ttc', 'msyhbd.ttc'],
    'Cascadia Mono': ['CascadiaMono.ttf'],
  }.entries) {
    final loader = FontLoader(entry.key);
    var loaded = 0;
    for (final name in entry.value) {
      try {
        final file = File('$root/Fonts/$name');
        final source = await file.readAsBytes();
        final bytes = name.endsWith('.ttc')
            ? selectCollectionFace(source, entry.key)
            : source;
        loader.addFont(Future.value(ByteData.sublistView(bytes)));
        loaded++;
      } on FileSystemException catch (error) {
        debugPrint('Mdis font unavailable: $name ($error)');
      } on FormatException catch (error) {
        debugPrint('Mdis font invalid: $name ($error)');
      }
    }
    if (loaded > 0) {
      await loader.load();
      debugPrint('Mdis font loaded: ${entry.key} ($loaded faces)');
    }
  }
}
