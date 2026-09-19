import 'dart:typed_data';

/// Select a named face in a TrueType collection without rewriting font tables.
/// All table offsets remain absolute, so redirecting the first face is enough.
/// The source bytes are never modified or saved back to disk.
Uint8List selectCollectionFace(Uint8List source, String family) {
  final data = ByteData.sublistView(source);
  if (data.getUint32(0) != 0x74746366) {
    throw const FormatException('Expected a TrueType collection');
  }
  final count = data.getUint32(8);
  for (var face = 0; face < count; face++) {
    final offset = data.getUint32(12 + face * 4);
    final tables = data.getUint16(offset + 4);
    for (var table = 0; table < tables; table++) {
      final record = offset + 12 + table * 16;
      if (data.getUint32(record) != 0x6E616D65) continue; // name
      final names = data.getUint32(record + 8);
      final records = data.getUint16(names + 2);
      final strings = names + data.getUint16(names + 4);
      for (var i = 0; i < records; i++) {
        final entry = names + 6 + i * 12;
        final platform = data.getUint16(entry);
        final nameId = data.getUint16(entry + 6);
        if ((platform != 0 && platform != 3) || (nameId != 1 && nameId != 16)) {
          continue;
        }
        final length = data.getUint16(entry + 8);
        final start = strings + data.getUint16(entry + 10);
        final name = String.fromCharCodes([
          for (var j = 0; j < length; j += 2) data.getUint16(start + j),
        ]);
        if (name != family) continue;
        final selected = Uint8List.fromList(source);
        final header = ByteData.sublistView(selected);
        header.setUint32(4, 0x00010000); // TTC v1: no signature metadata.
        header.setUint32(8, 1);
        header.setUint32(12, offset);
        return selected;
      }
    }
  }
  throw FormatException('Font family not found in collection: $family');
}
