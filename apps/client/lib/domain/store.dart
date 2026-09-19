import 'package:flutter/material.dart';

import '../workbench/demo_tasks.dart';
import 'machine.dart';

DateTime day(DateTime d) => DateTime(d.year, d.month, d.day);
String dateText(DateTime d) =>
    '${d.year}/${d.month.toString().padLeft(2, '0')}/${d.day.toString().padLeft(2, '0')}';
String rangeText(DateTime start, int days) =>
    '${dateText(start)} — ${dateText(start.add(Duration(days: days - 1)))}';

class Medication {
  Medication({
    required this.id,
    required this.name,
    this.spec = '0.5g',
    this.doses = const [1, 0, 1],
    this.meal = '饭后',
    DateTime? start,
    this.duration = 30,
    this.active = true,
    this.calibrated = false,
  }) : start = day(start ?? DateTime.now());
  final String id;
  String name, spec, meal;
  List<int> doses;
  DateTime start;
  int duration;
  bool active, calibrated;
  Medication copy() => Medication(
    id: id,
    name: name,
    spec: spec,
    doses: List.of(doses),
    meal: meal,
    start: start,
    duration: duration,
    active: active,
    calibrated: calibrated,
  );
  int onDay(DateTime date, int slot) =>
      active &&
          !day(date).isBefore(start) &&
          day(date).isBefore(start.add(Duration(days: duration)))
      ? doses[slot]
      : 0;
  int total(DateTime date, int days) => List.generate(
    days,
    (i) => List.generate(
      3,
      (slot) => onDay(date.add(Duration(days: i)), slot),
    ).fold(0, (int a, b) => a + b),
  ).fold(0, (int a, b) => a + b);
}

class Patient {
  Patient({
    required this.id,
    required this.name,
    required this.bed,
    this.sex = '未填写',
    this.age,
    this.note = '',
    this.boxes = const [],
    List<Medication>? prescriptions,
    this.archived = false,
  }) : prescriptions = prescriptions ?? [];
  final String id;
  String name, bed, sex, note;
  int? age;
  List<String> boxes;
  List<Medication> prescriptions;
  bool archived;
}

enum SessionState {
  ready('待摆药'),
  active('进行中'),
  supplement('待补药'),
  completed('已完成'),
  review('待人工核对');

  const SessionState(this.label);
  final String label;
}

enum MedicationStepState {
  pending('待处理'),
  done('已完成'),
  skipped('待补药');

  const MedicationStepState(this.label);
  final String label;
}

class MedicationStep {
  MedicationStep(this.medication, this.target);
  final Medication medication;
  final int target;
  MedicationStepState state = MedicationStepState.pending;
  int actual = 0;
  String note = '';
}

class DispensingSession {
  DispensingSession({
    required this.id,
    required this.patientId,
    required this.patientName,
    required this.bed,
    required this.start,
    required this.days,
    required this.steps,
    required this.operator,
  });
  final String id, patientId, patientName, bed, operator;
  final DateTime start;
  final int days;
  final List<MedicationStep> steps;
  SessionState state = SessionState.ready;
  DateTime created = DateTime.now();
  bool synced = false;
  bool boxVerified = false;
  bool paused = false;
  String? tray;
  int index = 0;
  MachineFault fault = MachineFault.none;
  MedicationStep get current => steps[index];
  int get done =>
      steps.where((s) => s.state == MedicationStepState.done).length;
  int get skipped =>
      steps.where((s) => s.state == MedicationStepState.skipped).length;
}

class AuditEntry {
  AuditEntry(this.action, this.detail, this.actor) : time = DateTime.now();
  final String action, detail, actor;
  final DateTime time;
}

class TeamMember {
  TeamMember(this.name, this.role, {this.enabled = true});
  String name, role;
  bool enabled;
}

/// In-memory UI state only. No authentication, backend, persistence or hardware.
class AppStore extends ChangeNotifier {
  AppStore({MockMachine? machine}) : machine = machine ?? MockMachine() {
    this.machine.addListener(_machineChanged);
    for (var i = 0; i < demoTasks.length; i++) {
      final t = demoTasks[i];
      final id = (i + 1).toString().padLeft(6, '0');
      final patient = Patient(
        id: id,
        name: t.name,
        bed: t.bed,
        boxes: ['5303859E$id'],
        prescriptions: List.generate(
          t.medications,
          (n) => Medication(
            id: '$id-$n',
            name: _names[n],
            spec: _specs[n],
            doses: n == 0
                ? [1, 0, 1]
                : n == 1
                ? [0, 0, 1]
                : [1, 0, 0],
            meal: n < 2 ? '饭前' : '饭后',
            calibrated: n != 0,
          ),
        ),
      );
      patients.add(patient);
      if (t.status == TaskStatus.completed) {
        final session = _snapshot(patient, day(DateTime.now()), 7);
        for (final s in session.steps) {
          s.state = MedicationStepState.done;
          s.actual = s.target;
        }
        session.state = SessionState.completed;
        session.synced = true;
        sessions.add(session);
      }
    }
  }
  static const _names = [
    '二甲双胍片',
    '阿托伐他汀钙片',
    '氨氯地平片',
    '维生素B1片',
    '碳酸钙片',
    '维生素D片',
  ];
  static const _specs = ['0.5g', '10mg', '5mg', '10mg', '0.5g', '400IU'];
  final MockMachine machine;
  final List<Patient> patients = [];
  final List<DispensingSession> sessions = [];
  final List<AuditEntry> audit = [];
  final List<TeamMember> members = [
    TeamMember('马佳乐', '管理员'),
    TeamMember('护理员一', '护理员'),
  ];
  String? operatorName;
  String institution = '颐康养老院';
  String serverUrl = '';
  int maxDays = 7, reminderDays = 2;
  double referenceDiameter = 9;
  double textScale = 1;
  int _serial = 100;
  bool cameraAllowed = false;
  String nextId() => (++_serial).toString().padLeft(6, '0');
  void changed() => notifyListeners();
  void log(String action, String detail) {
    audit.insert(0, AuditEntry(action, detail, operatorName ?? '本机操作人'));
    notifyListeners();
  }

  void _machineChanged() => notifyListeners();
  Patient patient(String id) => patients.firstWhere((p) => p.id == id);
  DispensingSession? latest(String id) {
    final found = sessions.where((s) => s.patientId == id);
    return found.isEmpty ? null : found.last;
  }

  List<DemoTask> get tasks => patients
      .where(
        (p) =>
            !p.archived &&
            (p.prescriptions.any(
                  (r) => r.total(day(DateTime.now()), maxDays) > 0,
                ) ||
                latest(p.id) != null),
      )
      .map((p) {
        final s = latest(p.id);
        return DemoTask(
          p.bed,
          p.name,
          s?.steps.length ??
              p.prescriptions
                  .where((r) => r.total(day(DateTime.now()), maxDays) > 0)
                  .length,
          s?.state == SessionState.completed
              ? TaskStatus.completed
              : TaskStatus.pending,
          patientId: p.id,
          days: s?.days ?? maxDays,
          period: rangeText(
            s?.start ?? day(DateTime.now()),
            s?.days ?? maxDays,
          ),
          note:
              s == null ||
                  s.state == SessionState.ready ||
                  s.state == SessionState.completed
              ? null
              : s.paused
              ? '已暂停 · 继续当前任务'
              : '${s.state.label} · ${s.done}/${s.steps.length}种已完成',
        );
      })
      .toList();

  DispensingSession? get executing {
    for (final s in sessions) {
      if (s.state == SessionState.active || s.state == SessionState.review) {
        return s;
      }
    }
    return null;
  }

  bool get busy => executing != null;
  DispensingSession _snapshot(Patient p, DateTime start, int days) {
    final meds = p.prescriptions.where((m) => m.total(start, days) > 0).toList()
      ..sort((a, b) => a.meal.compareTo(b.meal));
    return DispensingSession(
      id: 'MD-${nextId()}',
      patientId: p.id,
      patientName: p.name,
      bed: p.bed,
      start: day(start),
      days: days,
      operator: operatorName ?? '本机操作人',
      steps: meds
          .map((m) => MedicationStep(m.copy(), m.total(start, days)))
          .toList(),
    );
  }

  DispensingSession createSession(Patient p, DateTime start, int days) {
    if (busy) throw StateError('请先处理当前任务');
    if (days < 1 || days > 7 || p.archived) throw StateError('任务参数无效');
    final existing = latest(p.id);
    if (existing != null && existing.state != SessionState.completed) {
      return existing;
    }
    final s = _snapshot(p, start, days);
    if (s.steps.isEmpty) throw StateError('所选日期没有有效处方');
    sessions.add(s);
    log('创建摆药任务', '${p.bed} · ${s.id}');
    return s;
  }

  bool verifyBox(DispensingSession s, String input, {bool rfid = false}) {
    var code = input.trim().toUpperCase();
    if (!rfid) {
      code = code.replaceFirst(RegExp(r'^(PID|BOX):'), '');
      if (RegExp(r'^\d{1,6}$').hasMatch(code)) code = code.padLeft(6, '0');
    }
    final match = rfid
        ? patient(s.patientId).boxes.contains(code)
        : code == s.patientId;
    s.boxVerified = match;
    if (match) {
      log('药盒核对', '${s.id} · ${rfid ? 'RFID' : '编号'}');
    } else {
      notifyListeners();
    }
    return match;
  }

  void beginStep(DispensingSession s) {
    if ((executing != null && executing != s) ||
        !machine.connected ||
        !s.boxVerified ||
        s.state == SessionState.review ||
        s.current.state != MedicationStepState.pending ||
        machine.running ||
        s.tray != s.current.medication.meal ||
        !s.current.medication.calibrated) {
      throw StateError('请先完成设备、药盒、托盘和药物核对');
    }
    s.state = SessionState.active;
    s.current.actual = 0;
    s.paused = false;
    log(
      '开始分药',
      '${s.id} · ${s.current.medication.name} · ${s.current.target}片',
    );
    machine.start(
      s.current.target,
      (n) {
        s.current.actual = n;
        notifyListeners();
      },
      () {
        s.current.state = MedicationStepState.done;
        _advance(s);
      },
      (fault) {
        s.fault = fault;
        s.state = SessionState.review;
        s.boxVerified = fault != MachineFault.connection;
        log('需要人工核对', s.id);
      },
    );
  }

  void pause(DispensingSession s) {
    machine.pause();
    s.paused = true;
    log('暂停任务', s.id);
  }

  void resume(DispensingSession s) {
    if (!machine.connected || s.state == SessionState.review) return;
    machine.resume();
    s.paused = false;
    log('继续任务', s.id);
  }

  void skip(DispensingSession s) {
    if (machine.running ||
        s.state == SessionState.review ||
        s.current.state != MedicationStepState.pending) {
      throw StateError('执行中或结果不明确时不能跳过');
    }
    s.current.state = MedicationStepState.skipped;
    s.current.note = '药品不足，等待补药';
    log('稍后补药', '${s.id} · ${s.current.medication.name}');
    _advance(s);
  }

  void resolve(
    DispensingSession s, {
    required bool complete,
    required String note,
  }) {
    if (s.state != SessionState.review || note.trim().isEmpty) {
      throw StateError('请填写核对记录');
    }
    s.current.note = note.trim();
    s.current.state = complete
        ? MedicationStepState.done
        : MedicationStepState.skipped;
    if (complete) s.current.actual = s.current.target;
    s.fault = MachineFault.none;
    s.state = SessionState.active;
    log('人工核对', '${s.id} · ${complete ? '已完成' : '待补药'} · $note');
    _advance(s);
  }

  void _advance(DispensingSession s) {
    final next = s.steps.indexWhere(
      (step) => step.state == MedicationStepState.pending,
    );
    if (next < 0) {
      s.state = s.skipped > 0
          ? SessionState.supplement
          : SessionState.completed;
      s.paused = false;
      log('结束本次任务', '${s.id} · ${s.state.label}');
    } else {
      s.index = next;
      s.state = SessionState.active;
      notifyListeners();
    }
  }

  void supplement(DispensingSession s) {
    if (s.state != SessionState.supplement || s.skipped == 0) {
      throw StateError('没有待补药步骤');
    }
    if (busy && executing != s) throw StateError('请先处理当前任务');
    for (final step in s.steps) {
      if (step.state == MedicationStepState.skipped) {
        step.state = MedicationStepState.pending;
        step.actual = 0;
      }
    }
    s.index = s.steps.indexWhere((x) => x.state == MedicationStepState.pending);
    s.state = SessionState.ready;
    s.boxVerified = false;
    s.tray = null;
    log('准备补药', s.id);
  }

  @override
  void dispose() {
    machine.removeListener(_machineChanged);
    machine.dispose();
    super.dispose();
  }
}

class AppScope extends InheritedNotifier<AppStore> {
  const AppScope({super.key, required AppStore store, required super.child})
    : super(notifier: store);
  static AppStore of(BuildContext context) =>
      context.dependOnInheritedWidgetOfExactType<AppScope>()!.notifier!;
}
