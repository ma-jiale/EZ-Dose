import 'package:flutter_test/flutter_test.dart';
import 'package:mdis_client/main.dart';

void main() {
  testWidgets('Mdis bootstrap starts', (tester) async {
    await tester.pumpWidget(const MdisApp());
    expect(find.text('Mdis'), findsOneWidget);
  });
}
