import 'package:flutter_test/flutter_test.dart';
import 'package:mdis_client/domain/store.dart';
import 'package:mdis_client/domain/machine.dart';

void main() {
  test(
    'snapshot respects prescription dates and does not mutate with edits',
    () {
      final store = AppStore();
      addTearDown(store.dispose);
      final p = store.patients.first;
      final start = day(DateTime.now());
      p.prescriptions = [
        Medication(
          id: 'x',
          name: '药物',
          start: start.add(const Duration(days: 2)),
          duration: 2,
          doses: [1, 0, 1],
        ),
      ];
      final s = store.createSession(p, start, 7);
      expect(s.steps.single.target, 4);
      p.prescriptions.single.name = '修改后的名称';
      expect(s.steps.single.medication.name, '药物');
      expect(s.steps.single.medication.onDay(start, 0), 0);
      expect(
        s.steps.single.medication.onDay(start.add(const Duration(days: 4)), 0),
        0,
      );
    },
  );

  testWidgets('pause, completion, skip and supplementation remain distinct', (
    tester,
  ) async {
    final store = AppStore();
    addTearDown(store.dispose);
    final p = store.patients.first;
    p.prescriptions = [
      Medication(id: 'one', name: '药物一', doses: [1, 0, 0], calibrated: true),
      Medication(id: 'two', name: '药物二', doses: [1, 0, 0], calibrated: true),
    ];
    final s = store.createSession(p, day(DateTime.now()), 2);
    store.machine.connect();
    expect(store.verifyBox(s, 'PID:${p.id}'), isTrue);
    s.tray = s.current.medication.meal;
    store.beginStep(s);
    expect(() => store.beginStep(s), throwsStateError);
    await tester.pump(const Duration(milliseconds: 180));
    store.pause(s);
    final actual = s.current.actual;
    await tester.pump(const Duration(seconds: 3));
    expect(s.current.actual, actual);
    expect(s.state, SessionState.active);
    store.resume(s);
    await tester.pump(const Duration(seconds: 1));
    expect(s.done, 1);
    store.skip(s);
    expect(s.state, SessionState.supplement);
    expect(s.done, 1);
    expect(s.synced, isFalse);
    store.supplement(s);
    expect(s.done, 1);
    expect(s.boxVerified, isFalse);
    expect(() => store.beginStep(s), throwsStateError);
    store.verifyBox(s, p.boxes.first, rfid: true);
    s.tray = s.current.medication.meal;
    store.beginStep(s);
    await tester.pump(const Duration(seconds: 1));
    expect(s.state, SessionState.completed);
    expect(s.done, 2);
    expect(s.synced, isFalse);
  });

  testWidgets('uncertain physical result cannot be replayed or skipped', (
    tester,
  ) async {
    final store = AppStore();
    addTearDown(store.dispose);
    final p = store.patients.first;
    p.prescriptions = [
      Medication(id: 'one', name: '药物一', doses: [1, 0, 0], calibrated: true),
    ];
    final s = store.createSession(p, day(DateTime.now()), 2);
    store.machine.connect();
    store.verifyBox(s, p.id);
    s.tray = s.current.medication.meal;
    store.machine.nextFault = MachineFault.uncertain;
    store.beginStep(s);
    await tester.pump(const Duration(seconds: 2));
    expect(s.state, SessionState.review);
    expect(() => store.beginStep(s), throwsStateError);
    expect(() => store.skip(s), throwsStateError);
    expect(() => store.resolve(s, complete: true, note: ''), throwsStateError);
    store.resolve(s, complete: false, note: '数量待核对，交接补药');
    expect(s.state, SessionState.supplement);
    expect(s.done, 0);
  });

  test(
    'wrong box clears verification and a second active session is blocked',
    () {
      final store = AppStore();
      addTearDown(store.dispose);
      final p = store.patients.first;
      final s = store.createSession(p, day(DateTime.now()), 1);
      expect(store.verifyBox(s, p.id), isTrue);
      expect(store.verifyBox(s, '999999'), isFalse);
      expect(s.boxVerified, isFalse);
      s.state = SessionState.active;
      expect(
        () => store.createSession(store.patients[1], day(DateTime.now()), 1),
        throwsStateError,
      );
    },
  );
}
