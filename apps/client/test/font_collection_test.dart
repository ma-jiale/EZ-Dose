import 'dart:io';
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:mdis_client/font_collection.dart';

void main() {
  // Two faces with independent family names and absolute name-table offsets.
  Uint8List collection() {
    final bytes = Uint8List(256);
    final data = ByteData.sublistView(bytes);
    data.setUint32(0, 0x74746366);
    data.setUint32(4, 0x00010000);
    data.setUint32(8, 2);
    data.setUint32(12, 32);
    data.setUint32(16, 64);
    for (var i = 0; i < 2; i++) {
      final face = 32 + i * 32;
      final names = 96 + i * 64;
      data.setUint32(face, 0x00010000);
      data.setUint16(face + 4, 1);
      data.setUint32(face + 12, 0x6E616D65);
      data.setUint32(face + 20, names);
      data.setUint16(names + 2, 1);
      data.setUint16(names + 4, 18);
      data.setUint16(names + 6, 3);
      data.setUint16(names + 8, 1);
      data.setUint16(names + 10, 0x0409);
      data.setUint16(names + 12, 1);
      final family = i == 0 ? 'Other' : 'Microsoft YaHei UI';
      data.setUint16(names + 14, family.length * 2);
      for (var j = 0; j < family.length; j++) {
        data.setUint16(names + 18 + j * 2, family.codeUnitAt(j));
      }
    }
    return bytes;
  }

  test('selects UI face without changing source or absolute table offsets', () {
    final original = collection();
    final selected = selectCollectionFace(original, 'Microsoft YaHei UI');
    final header = ByteData.sublistView(selected);
    expect(header.getUint32(8), 1);
    expect(header.getUint32(12), 64);
    expect(selected.sublist(20), original.sublist(20));
    expect(ByteData.sublistView(original).getUint32(12), 32);
    expect(ByteData.sublistView(original).getUint32(8), 2);
  });

  test('does not silently substitute another font family', () {
    expect(
      () => selectCollectionFace(collection(), 'Missing'),
      throwsFormatException,
    );
    expect(
      () => selectCollectionFace(Uint8List(16), 'Missing'),
      throwsFormatException,
    );
  });

  test(
    'installed Windows regular and bold collections contain the UI face',
    () async {
      if (!Platform.isWindows) return;
      final root = Platform.environment['WINDIR'] ?? r'C:\Windows';
      for (final name in ['msyh.ttc', 'msyhbd.ttc']) {
        final file = File('$root/Fonts/$name');
        if (!await file.exists()) continue;
        final source = await file.readAsBytes();
        final result = selectCollectionFace(source, 'Microsoft YaHei UI');
        expect(ByteData.sublistView(result).getUint32(8), 1);
        expect(
          ByteData.sublistView(result).getUint32(12),
          isNot(ByteData.sublistView(source).getUint32(12)),
        );
      }
    },
  );
}
