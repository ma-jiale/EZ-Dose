import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mdis_client/domain/store.dart';
import 'package:mdis_client/pages/account.dart';
import 'package:mdis_client/pages/patients.dart';
import 'package:mdis_client/pages/records.dart';
import 'package:mdis_client/pages/session.dart';
import 'package:mdis_client/pages/settings.dart';
import 'package:mdis_client/theme.dart';

void main() {
  for (final layout in [
    (const Size(1440, 900), 1.0),
    (const Size(1024, 768), 1.0),
    (const Size(390, 844), 1.0),
    (const Size(1024, 768), 2.0),
  ]) {
    final size = layout.$1;
    testWidgets('all UI routes fit $size at ${layout.$2} text scale', (
      tester,
    ) async {
      tester.view.physicalSize = size;
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      final store = AppStore();
      addTearDown(store.dispose);
      final session = store.createSession(
        store.patients.first,
        day(DateTime.now()),
        7,
      );
      final pages = <Widget>[
        const PatientsPage(),
        PatientDetail(patientId: store.patients.first.id),
        const PatientForm(),
        PrescriptionForm(patientId: store.patients.first.id),
        BoxBindingPage(patientId: store.patients.first.id),
        VerifyBoxPage(session: session),
        SessionPage(session: session),
        SessionResult(session: store.sessions.first),
        const RecordsPage(),
        RecordDetail(session: store.sessions.first),
        const SettingsPage(),
        const DevicesPage(),
        const CalibrationPage(),
        const CountingPage(),
        const SyncPage(),
        const AccountPage(),
        const InstitutionPage(),
        const MembersPage(),
      ];
      for (final page in pages) {
        await tester.pumpWidget(
          AppScope(
            key: UniqueKey(),
            store: store,
            child: MaterialApp(
              theme: buildMdisTheme(),
              builder: (context, child) => MediaQuery(
                data: MediaQuery.of(context)
                    .copyWith(textScaler: TextScaler.linear(layout.$2)),
                child: child!,
              ),
              home: Scaffold(body: page),
            ),
          ),
        );
        await tester.pumpAndSettle();
        expect(
          tester.takeException(),
          isNull,
          reason: '${page.runtimeType} at $size',
        );
      }
    });
  }
  testWidgets('UI flow verifies box, calibrates, completes and skips', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1440, 1000);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final store = AppStore();
    addTearDown(store.dispose);
    final p = store.patients.first;
    p.prescriptions = [
      Medication(id: 'a', name: '药物甲', doses: [1, 0, 0]),
      Medication(id: 'b', name: '药物乙', doses: [1, 0, 0], calibrated: true),
    ];
    final session = store.createSession(p, day(DateTime.now()), 1);
    store.machine.connect();
    await tester.pumpWidget(
      AppScope(
        store: store,
        child: MaterialApp(
          theme: buildMdisTheme(),
          home: VerifyBoxPage(session: session),
        ),
      ),
    );
    Future<void> tap(String label) async {
      final finder = find.text(label).last;
      await tester.ensureVisible(finder);
      await tester.tap(finder);
      await tester.pumpAndSettle();
    }

    await tester.enterText(find.byType(TextFormField), '999999');
    await tap('核对药盒');
    expect(session.boxVerified, isFalse);
    await tester.enterText(find.byType(TextFormField), p.id);
    await tap('核对药盒');
    await tap('进入摆药工作台');
    await tap('首次使用，请先校准药物');
    await tap('开始校准');
    await tap('采集参考药片');
    await tap('确认校准');
    await tap('完成');
    expect(session.current.medication.calibrated, isTrue);
    await tap('确认饭后药盘');
    await tap('药盘已核对');
    await tap('开始分药');
    await tap('开始分药');
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
    expect(session.done, 1);
    await tap('跳过，稍后补药');
    await tap('跳过此药');
    expect(session.state, SessionState.supplement);
    expect(find.text('本次摆药仍需补药'), findsOneWidget);
    await tap('开始补药');
    expect(session.boxVerified, isFalse);
    expect(find.text('核对药盒'), findsWidgets);
    expect(tester.takeException(), isNull);
  });

  testWidgets('patient form validates duplicate bed and saves a new patient', (
    tester,
  ) async {
    final store = AppStore();
    addTearDown(store.dispose);
    await tester.pumpWidget(
      AppScope(
        store: store,
        child: MaterialApp(theme: buildMdisTheme(), home: const PatientForm()),
      ),
    );
    await tester.enterText(find.byType(TextFormField).at(0), '测试患者');
    await tester.enterText(find.byType(TextFormField).at(1), 'B-302');
    await tester.ensureVisible(find.text('保存患者'));
    await tester.tap(find.text('保存患者'));
    await tester.pumpAndSettle();
    expect(find.text('床号已被使用'), findsOneWidget);
    await tester.enterText(find.byType(TextFormField).at(1), 'D-100');
    await tester.ensureVisible(find.text('保存患者'));
    await tester.tap(find.text('保存患者'));
    await tester.pumpAndSettle();
    expect(store.patients.any((p) => p.bed == 'D-100'), isTrue);
  });
  testWidgets('device discovery and connect control only the mock device', (
    tester,
  ) async {
    final store = AppStore();
    addTearDown(store.dispose);
    await tester.pumpWidget(
      AppScope(
        store: store,
        child: MaterialApp(theme: buildMdisTheme(), home: const DevicesPage()),
      ),
    );
    await tester.tap(find.text('查找设备'));
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('连接'));
    await tester.tap(find.text('连接'));
    await tester.pumpAndSettle();
    expect(store.machine.connected, isTrue);
  });
}
