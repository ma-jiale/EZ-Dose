import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mdis_client/main.dart';
import 'package:mdis_client/workbench/demo_tasks.dart';

void main() {
  Future<void> launch(WidgetTester tester, Size size) async {
    tester.view.physicalSize = size;
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(const MdisApp());
  }

  testWidgets('pagination, search and two-state filter stay consistent', (
    tester,
  ) async {
    await launch(tester, const Size(1440, 900));
    expect(find.text('B-302'), findsOneWidget);
    expect(find.text('A-118'), findsNothing);
    expect(find.text('进行中'), findsNothing);
    expect(find.text('待补药'), findsNothing);
    await tester.ensureVisible(find.byTooltip('下一页'));
    await tester.tap(find.byTooltip('下一页'));
    await tester.pumpAndSettle();
    expect(find.text('A-118'), findsOneWidget);
    expect(find.text('B-302'), findsNothing);
    await tester.enterText(find.byType(TextField), 'b-302');
    await tester.pumpAndSettle();
    expect(find.text('B-302'), findsOneWidget);
    expect(find.text('A-118'), findsNothing);
    await tester.tap(find.byKey(const ValueKey('open-B-302')));
    await tester.pumpAndSettle();
    expect(find.text('本次摆药'), findsOneWidget);
    await tester.tap(find.byType(BackButton));
    await tester.pumpAndSettle();
    expect(find.text('B-302'), findsOneWidget);
    await tester.tap(find.byTooltip('清除搜索'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey(TaskStatus.completed)));
    await tester.pumpAndSettle();
    expect(find.text('A-101'), findsOneWidget);
    expect(find.text('B-302'), findsNothing);
    await tester.enterText(find.byType(TextField), 'no-match');
    await tester.pumpAndSettle();
    expect(find.text('没有匹配的患者'), findsOneWidget);
    await tester.tap(find.text('清除搜索条件'));
    await tester.pumpAndSettle();
    expect(find.text('A-101'), findsOneWidget);
    expect(find.byTooltip('第 2 页'), findsNothing);
    expect(find.byTooltip('分药机未连接'), findsOneWidget);
  });

  for (final size in [
    const Size(1440, 900),
    const Size(1024, 768),
    const Size(600, 800),
    const Size(390, 844),
  ]) {
    testWidgets('workbench fits $size', (tester) async {
      await launch(tester, size);
      expect(tester.takeException(), isNull);
      await tester.drag(find.byType(CustomScrollView), const Offset(0, -1000));
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
    });
  }

  testWidgets('large text remains usable on tablet', (tester) async {
    tester.view.physicalSize = const Size(1024, 768);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(
      MaterialApp(
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(context)
              .copyWith(textScaler: const TextScaler.linear(2)),
          child: child!,
        ),
        home: const MdisApp(),
      ),
    );
    expect(tester.takeException(), isNull);
  });
}
