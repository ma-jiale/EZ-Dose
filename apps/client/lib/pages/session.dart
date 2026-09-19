import 'package:flutter/material.dart';

import '../domain/store.dart';
import '../theme.dart';
import '../ui/components.dart';
import 'settings.dart';

class VerifyBoxPage extends StatefulWidget {
  const VerifyBoxPage({super.key, required this.session});
  final DispensingSession session;
  @override
  State<VerifyBoxPage> createState() => _VerifyBoxPageState();
}

class _VerifyBoxPageState extends State<VerifyBoxPage> {
  final code = TextEditingController();
  bool rfid = false;
  String? error;
  @override
  void dispose() {
    code.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context), s = widget.session;
    return PageFrame(
      title: '核对药盒',
      subtitle: '${s.bed} · ${s.patientName} · ${rangeText(s.start, s.days)}',
      child: ResponsiveColumns(
        main: Section(
          title: '确认药盒属于当前患者',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                height: 160,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: MdisColors.background,
                  borderRadius: BorderRadius.circular(16),
                ),
                child: const Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.qr_code_scanner, size: 48),
                    SizedBox(height: 12),
                    Text('扫描药盒标签，或输入编号核对'),
                  ],
                ),
              ),
              const SizedBox(height: 20),
              SegmentedButton<bool>(
                segments: const [
                  ButtonSegment(value: false, label: Text('患者编号')),
                  ButtonSegment(value: true, label: Text('RFID')),
                ],
                selected: {rfid},
                onSelectionChanged: (v) {
                  setState(() => rfid = v.first);
                  s.boxVerified = false;
                  code.clear();
                },
              ),
              const SizedBox(height: 20),
              FormFieldBox(
                rfid ? '药盒 RFID UID' : '患者编号 / PID:编号',
                code,
                onChanged: (_) {
                  if (s.boxVerified) {
                    s.boxVerified = false;
                    store.changed();
                  }
                },
              ),
              if (error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Text(error!, style: MdisType.bodyStrong),
                ),
              if (s.boxVerified)
                const Padding(
                  padding: EdgeInsets.only(bottom: 12),
                  child: StatusPill('药盒核对一致'),
                ),
              ActionRow(
                children: [
                  OutlinedButton(
                    onPressed: () {
                      final ok = store.verifyBox(s, code.text, rfid: rfid);
                      setState(() => error = ok ? null : '药盒不匹配，请重新检查患者及药盒编号');
                    },
                    child: const Text('核对药盒'),
                  ),
                  FilledButton(
                    onPressed: s.boxVerified && store.machine.connected
                        ? () {
                            Navigator.of(context).pushReplacement(
                              MaterialPageRoute<void>(
                                builder: (_) => SessionPage(session: s),
                              ),
                            );
                          }
                        : null,
                    child: const Text('进入摆药工作台'),
                  ),
                ],
              ),
            ],
          ),
        ),
        side: Column(
          children: [
            Section(
              title: '患者信息',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  InfoLine('床号', s.bed),
                  InfoLine('姓名', s.patientName),
                  InfoLine('患者编号', s.patientId),
                  InfoLine('药物', '${s.steps.length}种'),
                  InfoLine('周期', '${s.days}天'),
                ],
              ),
            ),
            Section(
              title: '分药机',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    store.machine.connected
                        ? '已连接 · ${store.machine.name}'
                        : '尚未连接',
                  ),
                  const SizedBox(height: 16),
                  OutlinedButton(
                    onPressed: () => openPage(context, const DevicesPage()),
                    child: const Text('管理设备'),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class SessionPage extends StatelessWidget {
  const SessionPage({super.key, required this.session});
  final DispensingSession session;
  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context), s = session;
    if (s.state == SessionState.completed ||
        s.state == SessionState.supplement) {
      return SessionResult(session: s);
    }
    final step = s.current, med = step.medication;
    return PopScope(
      canPop: !store.machine.running || s.paused,
      onPopInvokedWithResult: (didPop, result) {
        if (!didPop) message(context, '请先暂停分药；任务会保留在工作台');
      },
      child: PageFrame(
        title: '${s.bed}  |  ${s.patientName}',
        subtitle:
            '${rangeText(s.start, s.days)} · ${s.done} / ${s.steps.length}种药物已完成',
        actions: [
          StatusPill(
            s.state == SessionState.review
                ? '需要人工核对'
                : s.paused
                ? '已暂停'
                : s.state.label,
          ),
        ],
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            LinearProgressIndicator(
              value: s.done / s.steps.length,
              backgroundColor: MdisColors.line,
              color: MdisColors.primary,
              minHeight: 4,
            ),
            const SizedBox(height: 24),
            ResponsiveColumns(
              main: Section(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '当前药物',
                      style: MdisType.label.copyWith(color: MdisColors.muted),
                    ),
                    const SizedBox(height: 12),
                    Text(med.name, style: MdisType.pageTitle),
                    const SizedBox(height: 8),
                    Text('${med.spec} · ${med.meal}', style: MdisType.body),
                    const SizedBox(height: 28),
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text('${step.target}', style: MdisType.display),
                        const SizedBox(width: 8),
                        Padding(
                          padding: const EdgeInsets.only(bottom: 4),
                          child: Text('片', style: MdisType.sectionTitle),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Text(
                      '请将 ${step.target} 片${med.name}放入投药口',
                      style: MdisType.bodyStrong,
                    ),
                    const SizedBox(height: 24),
                    MedicationMatrix(session: s, medication: med),
                    const SizedBox(height: 24),
                    if (!med.calibrated)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 16),
                        child: OutlinedButton.icon(
                          onPressed: () => openPage(
                            context,
                            CalibrationPage(medication: med),
                          ),
                          icon: const Icon(Icons.tune),
                          label: const Text('首次使用，请先校准药物'),
                        ),
                      ),
                    if (s.tray != med.meal)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 16),
                        child: OutlinedButton(
                          onPressed: store.machine.running
                              ? null
                              : () async {
                                  if (await confirm(
                                    context,
                                    '核对药盘',
                                    '${s.bed} · ${s.patientName}\n当前为${med.meal}药物。请核对已放入对应药盘。',
                                    action: '药盘已核对',
                                  )) {
                                    s.tray = med.meal;
                                    store.log('核对药盘', '${s.id} · ${med.meal}');
                                  }
                                },
                          child: Text('确认${med.meal}药盘'),
                        ),
                      ),
                    if (!s.boxVerified)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 16),
                        child: OutlinedButton(
                          onPressed: () => Navigator.of(context)
                              .pushReplacement(
                                MaterialPageRoute<void>(
                                  builder: (_) => VerifyBoxPage(session: s),
                                ),
                              ),
                          child: const Text('重新核对药盒'),
                        ),
                      ),
                    if (s.state == SessionState.review)
                      Section(
                        title: '请人工核对',
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('执行结果不明确。请核对药盒实际片数，不要重复投药。'),
                            const SizedBox(height: 16),
                            FilledButton(
                              onPressed: () => _resolve(context, store, s),
                              child: const Text('记录核对结果'),
                            ),
                          ],
                        ),
                      )
                    else
                      ActionRow(
                        children: [
                          if (store.machine.running)
                            FilledButton(
                              onPressed: () =>
                                  s.paused ? store.resume(s) : store.pause(s),
                              child: Text(s.paused ? '继续分药' : '暂停分药'),
                            )
                          else
                            FilledButton(
                              onPressed:
                                  !store.machine.connected ||
                                      !s.boxVerified ||
                                      !med.calibrated ||
                                      s.tray != med.meal
                                  ? null
                                  : () async {
                                      final ok = await confirm(
                                        context,
                                        '确认开始分药',
                                        '${s.bed} · ${s.patientName}\n${med.name} ${med.spec}\n${step.target}片 · ${rangeText(s.start, s.days)}',
                                        action: '开始分药',
                                      );
                                      if (ok) {
                                        try {
                                          store.beginStep(s);
                                        } on StateError catch (e) {
                                          if (context.mounted) {
                                            message(context, e.message);
                                          }
                                        }
                                      }
                                    },
                              child: const Text('开始分药'),
                            ),
                          OutlinedButton(
                            onPressed: store.machine.running
                                ? null
                                : () async {
                                    if (await confirm(
                                      context,
                                      '稍后补药',
                                      '${med.name} · ${step.target}片\n此药会进入待补药清单，不会记为完成。',
                                      action: '跳过此药',
                                    )) {
                                      store.skip(s);
                                    }
                                  },
                            child: const Text('跳过，稍后补药'),
                          ),
                        ],
                      ),
                  ],
                ),
              ),
              side: Column(
                children: [
                  Section(
                    title: '分药机',
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        InfoLine('连接', store.machine.connected ? '已连接' : '未连接'),
                        InfoLine('药盒', s.boxVerified ? '已核对' : '待核对'),
                        InfoLine('药盘', s.tray ?? '待核对'),
                        InfoLine(
                          '机器',
                          s.state == SessionState.review
                              ? '等待人工核对'
                              : s.paused
                              ? '已暂停'
                              : store.machine.running
                              ? '正在分药'
                              : '等待投药',
                        ),
                        InfoLine('已分出', '${step.actual} / ${step.target}片'),
                        TextButton(
                          onPressed: () =>
                              openPage(context, const DevicesPage()),
                          child: const Text('设备详情'),
                        ),
                      ],
                    ),
                  ),
                  Section(
                    title: '本次药物',
                    child: Column(
                      children: [
                        for (var i = 0; i < s.steps.length; i++)
                          ListTile(
                            contentPadding: EdgeInsets.zero,
                            leading: Icon(
                              s.steps[i].state == MedicationStepState.done
                                  ? Icons.check_circle_outline
                                  : s.steps[i].state ==
                                        MedicationStepState.skipped
                                  ? Icons.schedule
                                  : Icons.circle_outlined,
                              size: 20,
                            ),
                            title: Text(
                              s.steps[i].medication.name,
                              style: i == s.index
                                  ? MdisType.bodyStrong
                                  : MdisType.body,
                            ),
                            subtitle: Text(
                              '${s.steps[i].target}片 · ${s.steps[i].state.label}',
                            ),
                          ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            Text(
              s.index + 1 < s.steps.length
                  ? '下一项：${s.steps[s.index + 1].medication.name} · ${s.steps[s.index + 1].target}片'
                  : '这是本次最后一项药物',
              style: MdisType.body.copyWith(color: MdisColors.muted),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _resolve(
    BuildContext context,
    AppStore store,
    DispensingSession s,
  ) async {
    final note = TextEditingController();
    bool complete = false;
    final route = DialogRoute<void>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setState) => AlertDialog(
          title: const Text('人工核对结果'),
          content: SizedBox(
            width: 480,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    '${s.current.medication.name} · 目标 ${s.current.target}片',
                  ),
                  const SizedBox(height: 16),
                  CheckboxListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('已逐格核对，目标药物全部到位'),
                    value: complete,
                    onChanged: (v) => setState(() => complete = v!),
                  ),
                  FormFieldBox(
                    '核对记录（必填）',
                    note,
                    lines: 3,
                    onChanged: (_) => setState(() {}),
                  ),
                  const Text('未确认全部到位的药物将保留为待补药。'),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('暂不处理'),
            ),
            FilledButton(
              onPressed: note.text.trim().isEmpty
                  ? null
                  : () {
                      store.resolve(s, complete: complete, note: note.text);
                      Navigator.pop(context);
                    },
              child: const Text('保存核对结果'),
            ),
          ],
        ),
      ),
    );
    await Navigator.of(context).push(route);
    await route.completed;
    note.dispose();
  }
}

class MedicationMatrix extends StatelessWidget {
  const MedicationMatrix({
    super.key,
    required this.session,
    required this.medication,
  });
  final DispensingSession session;
  final Medication medication;
  @override
  Widget build(BuildContext context) => SingleChildScrollView(
    scrollDirection: Axis.horizontal,
    child: DataTable(
      horizontalMargin: 0,
      columnSpacing: 20,
      headingTextStyle: MdisType.label,
      dataTextStyle: MdisType.body,
      columns: [
        const DataColumn(label: Text('时段')),
        for (var d = 0; d < session.days; d++)
          DataColumn(
            label: Text(
              '${session.start.add(Duration(days: d)).month}/${session.start.add(Duration(days: d)).day}',
            ),
          ),
      ],
      rows: [
        for (var slot = 0; slot < 3; slot++)
          DataRow(
            cells: [
              DataCell(Text(['早', '午', '晚'][slot])),
              for (var d = 0; d < session.days; d++)
                DataCell(
                  Text(
                    medication.onDay(
                              session.start.add(Duration(days: d)),
                              slot,
                            ) ==
                            0
                        ? '—'
                        : '${medication.onDay(session.start.add(Duration(days: d)), slot)}',
                  ),
                ),
            ],
          ),
      ],
    ),
  );
}

class SessionResult extends StatelessWidget {
  const SessionResult({super.key, required this.session});
  final DispensingSession session;
  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context), s = session;
    final complete = s.state == SessionState.completed;
    return PageFrame(
      title: complete ? '本次摆药已完成' : '本次摆药仍需补药',
      subtitle: '${s.bed} · ${s.patientName} · ${rangeText(s.start, s.days)}',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Section(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(
                  complete ? Icons.check_circle_outline : Icons.schedule,
                  size: 48,
                ),
                const SizedBox(height: 16),
                Text(
                  '${s.done}种已完成${s.skipped > 0 ? '，${s.skipped}种待补药' : ''}',
                  style: MdisType.sectionTitle,
                ),
                const SizedBox(height: 12),
                const Text('请取出药盒，核对标签与各时段药物。'),
                const SizedBox(height: 12),
                StatusPill(s.synced ? '已同步' : '待同步'),
              ],
            ),
          ),
          Section(
            title: '药物清单',
            child: Column(
              children: [
                for (final step in s.steps)
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    title: Text(step.medication.name),
                    subtitle: Text(
                      '${step.target}片 · ${step.state.label}${step.note.isEmpty ? '' : ' · ${step.note}'}',
                    ),
                  ),
              ],
            ),
          ),
          ActionRow(
            children: [
              if (!complete)
                FilledButton(
                  onPressed: store.busy
                      ? null
                      : () {
                          store.supplement(s);
                          Navigator.of(context).pushReplacement(
                            MaterialPageRoute<void>(
                              builder: (_) => VerifyBoxPage(session: s),
                            ),
                          );
                        },
                  child: const Text('开始补药'),
                ),
              OutlinedButton(
                onPressed: () =>
                    Navigator.of(context).popUntil((r) => r.isFirst),
                child: const Text('返回工作台'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
