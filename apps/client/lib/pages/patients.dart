import 'package:flutter/material.dart';

import '../domain/store.dart';
import '../theme.dart';
import '../ui/components.dart';
import 'session.dart';
import 'records.dart';

class PatientsPage extends StatefulWidget {
  const PatientsPage({super.key});
  @override
  State<PatientsPage> createState() => _PatientsPageState();
}

class _PatientsPageState extends State<PatientsPage> {
  String query = '';
  bool archived = false;
  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context);
    final patients = store.patients
        .where(
          (p) =>
              p.archived == archived &&
              '${p.name} ${p.bed} ${p.id}'.toLowerCase().contains(
                query.toLowerCase(),
              ),
        )
        .toList();
    return PageFrame(
      embedded: true,
      title: '患者',
      subtitle: '管理患者资料、药盒和处方',
      actions: [
        FilledButton.icon(
          onPressed: () => openPage(context, const PatientForm()),
          icon: const Icon(Icons.add),
          label: const Text('新增患者'),
        ),
      ],
      child: Column(
        children: [
          TextField(
            onChanged: (v) => setState(() => query = v),
            decoration: const InputDecoration(
              hintText: '搜索床号、姓名或患者编号',
              prefixIcon: Icon(Icons.search),
            ),
          ),
          const SizedBox(height: 16),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('查看已归档患者'),
            value: archived,
            onChanged: (v) => setState(() => archived = v),
          ),
          if (patients.isEmpty) const EmptyState('没有匹配的患者'),
          for (final p in patients)
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: Material(
                color: Colors.white,
                borderRadius: BorderRadius.circular(16),
                child: ListTile(
                  contentPadding: const EdgeInsets.all(18),
                  leading: const CircleAvatar(
                    backgroundColor: MdisColors.primarySoft,
                    child: Icon(Icons.person_outline, color: MdisColors.ink),
                  ),
                  title: Text(
                    '${p.bed}  ·  ${p.name}',
                    style: MdisType.cardTitle,
                  ),
                  subtitle: Text(
                    '${p.id}  ·  ${p.prescriptions.where((r) => r.active).length}种有效药物  ·  ${p.boxes.isEmpty ? '未绑定药盒' : '已绑定药盒'}',
                  ),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () =>
                      openPage(context, PatientDetail(patientId: p.id)),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class PatientDetail extends StatefulWidget {
  const PatientDetail({super.key, required this.patientId});
  final String patientId;
  @override
  State<PatientDetail> createState() => _PatientDetailState();
}

class _PatientDetailState extends State<PatientDetail> {
  DateTime start = day(DateTime.now());
  int? days;
  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context),
        p = AppScope.of(context).patient(widget.patientId);
    days ??= store.maxDays;
    final recent = store.latest(p.id);
    final locked = store.executing?.patientId == p.id;
    return PageFrame(
      title: '${p.bed}  |  ${p.name}',
      subtitle: '${p.id} · ${p.sex}${p.age == null ? '' : ' · ${p.age}岁'}',
      actions: [
        OutlinedButton(
          onPressed: locked
              ? null
              : () => openPage(context, PatientForm(patientId: p.id)),
          child: const Text('编辑资料'),
        ),
      ],
      child: ResponsiveColumns(
        main: Column(
          children: [
            Section(
              title: '本次摆药',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  ActionRow(
                    children: [
                      OutlinedButton.icon(
                        icon: const Icon(Icons.calendar_today_outlined),
                        label: Text(dateText(start)),
                        onPressed: () async {
                          final value = await showDatePicker(
                            context: context,
                            initialDate: start,
                            firstDate: day(DateTime.now()),
                            lastDate: DateTime.now().add(
                              const Duration(days: 90),
                            ),
                          );
                          if (value != null && mounted) {
                            setState(() => start = value);
                          }
                        },
                      ),
                      DropdownButton<int>(
                        value: days,
                        items: List.generate(
                          7,
                          (i) => DropdownMenuItem(
                            value: i + 1,
                            child: Text('${i + 1}天'),
                          ),
                        ),
                        onChanged: (v) => setState(() => days = v),
                      ),
                    ],
                  ),
                  InfoLine('覆盖日期', rangeText(start, days!)),
                  InfoLine(
                    '药物',
                    '${p.prescriptions.where((m) => m.total(start, days!) > 0).length}种',
                  ),
                  if (recent != null) InfoLine('最近任务', recent.state.label),
                  const SizedBox(height: 16),
                  ActionRow(
                    children: [
                      FilledButton(
                        onPressed:
                            p.archived ||
                                p.prescriptions.every(
                                  (m) => m.total(start, days!) == 0,
                                ) ||
                                (store.busy && !locked)
                            ? null
                            : () {
                                try {
                                  if (recent?.state ==
                                      SessionState.supplement) {
                                    store.supplement(recent!);
                                    openPage(
                                      context,
                                      VerifyBoxPage(session: recent),
                                    );
                                  } else if (recent != null &&
                                      (recent.state == SessionState.active ||
                                          recent.state ==
                                              SessionState.review)) {
                                    openPage(
                                      context,
                                      SessionPage(session: recent),
                                    );
                                  } else {
                                    final s = store.createSession(
                                      p,
                                      start,
                                      days!,
                                    );
                                    openPage(
                                      context,
                                      VerifyBoxPage(session: s),
                                    );
                                  }
                                } on StateError catch (e) {
                                  message(context, e.message);
                                }
                              },
                        child: Text(
                          recent?.state == SessionState.supplement
                              ? '继续补药'
                              : locked
                              ? '继续当前任务'
                              : '核对药盒并开始',
                        ),
                      ),
                      if (store.busy && !locked) const Text('请先完成或处理当前摆药任务'),
                    ],
                  ),
                ],
              ),
            ),
            Section(
              title: '处方',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (p.prescriptions.isEmpty) const EmptyState('尚无处方'),
                  for (final m in p.prescriptions)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Wrap(
                            spacing: 12,
                            runSpacing: 8,
                            crossAxisAlignment: WrapCrossAlignment.center,
                            children: [
                              Text(m.name, style: MdisType.cardTitle),
                              StatusPill(m.active ? '有效' : '已停用'),
                              TextButton(
                                onPressed: locked
                                    ? null
                                    : () => openPage(
                                        context,
                                        PrescriptionForm(
                                          patientId: p.id,
                                          medicationId: m.id,
                                        ),
                                      ),
                                child: const Text('编辑'),
                              ),
                            ],
                          ),
                          Text(
                            '${m.spec} · ${m.meal} · 早 ${m.doses[0]}片 / 午 ${m.doses[1]}片 / 晚 ${m.doses[2]}片',
                            style: MdisType.body,
                          ),
                          Text(
                            rangeText(m.start, m.duration),
                            style: MdisType.caption.copyWith(
                              color: MdisColors.muted,
                            ),
                          ),
                          const Divider(),
                        ],
                      ),
                    ),
                  OutlinedButton.icon(
                    onPressed: locked || p.archived
                        ? null
                        : () => openPage(
                            context,
                            PrescriptionForm(patientId: p.id),
                          ),
                    icon: const Icon(Icons.add),
                    label: const Text('新增处方'),
                  ),
                ],
              ),
            ),
          ],
        ),
        side: Column(
          children: [
            Section(
              title: '药盒',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  for (final box in p.boxes)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: Text(box, style: MdisType.monospace),
                    ),
                  if (p.boxes.isEmpty) const Text('尚未绑定药盒'),
                  const SizedBox(height: 16),
                  OutlinedButton(
                    onPressed: locked
                        ? null
                        : () => openPage(
                            context,
                            BoxBindingPage(patientId: p.id),
                          ),
                    child: const Text('管理药盒'),
                  ),
                  TextButton(
                    onPressed: () => showDialog<void>(
                      context: context,
                      builder: (context) => AlertDialog(
                        title: const Text('患者标签'),
                        content: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(
                              '${p.bed} · ${p.name}',
                              style: MdisType.sectionTitle,
                            ),
                            const SizedBox(height: 16),
                            SelectableText(
                              'PID:${p.id}',
                              style: MdisType.monospace,
                            ),
                            const SizedBox(height: 8),
                            const Text('将此编号用于药盒核对'),
                          ],
                        ),
                        actions: [
                          TextButton(
                            onPressed: () => Navigator.pop(context),
                            child: const Text('关闭'),
                          ),
                        ],
                      ),
                    ),
                    child: const Text('查看患者标签'),
                  ),
                ],
              ),
            ),
            Section(title: '备注', child: Text(p.note.isEmpty ? '暂无备注' : p.note)),
            Section(
              title: '历史记录',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  for (final s
                      in store.sessions
                          .where((s) => s.patientId == p.id)
                          .toList()
                          .reversed)
                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text(s.id),
                      subtitle: Text(
                        '${dateText(s.created)} · ${s.state.label}',
                      ),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => openPage(context, RecordDetail(session: s)),
                    ),
                  if (recent == null) const Text('暂无摆药记录'),
                ],
              ),
            ),
            TextButton(
              onPressed: locked
                  ? null
                  : () async {
                      final ok = await confirm(
                        context,
                        p.archived ? '恢复患者' : '归档患者',
                        '${p.bed} · ${p.name}\n归档会从当前患者及工作台列表中隐藏，历史记录仍保留。',
                      );
                      if (ok) {
                        p.archived = !p.archived;
                        store.log(p.archived ? '归档患者' : '恢复患者', p.id);
                      }
                    },
              child: Text(p.archived ? '恢复患者' : '归档患者'),
            ),
          ],
        ),
      ),
    );
  }
}

class PatientForm extends StatefulWidget {
  const PatientForm({super.key, this.patientId});
  final String? patientId;
  @override
  State<PatientForm> createState() => _PatientFormState();
}

class _PatientFormState extends State<PatientForm> {
  final form = GlobalKey<FormState>();
  final name = TextEditingController(),
      bed = TextEditingController(),
      age = TextEditingController(),
      note = TextEditingController();
  String sex = '未填写';
  bool loaded = false;
  @override
  void dispose() {
    name.dispose();
    bed.dispose();
    age.dispose();
    note.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context);
    if (!loaded) {
      loaded = true;
      if (widget.patientId != null) {
        final p = store.patient(widget.patientId!);
        name.text = p.name;
        bed.text = p.bed;
        age.text = p.age?.toString() ?? '';
        note.text = p.note;
        sex = p.sex;
      }
    }
    return PageFrame(
      title: widget.patientId == null ? '新增患者' : '编辑患者',
      child: Section(
        child: Form(
          key: form,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              FormFieldBox('患者姓名', name, validator: requiredText),
              FormFieldBox(
                '床号',
                bed,
                validator: (v) =>
                    requiredText(v) ??
                    (store.patients.any(
                          (p) =>
                              p.id != widget.patientId &&
                              !p.archived &&
                              p.bed.toLowerCase() == v!.trim().toLowerCase(),
                        )
                        ? '床号已被使用'
                        : null),
              ),
              DropdownButtonFormField<String>(
                initialValue: sex,
                decoration: const InputDecoration(labelText: '性别'),
                items: ['未填写', '女', '男']
                    .map((s) => DropdownMenuItem(value: s, child: Text(s)))
                    .toList(),
                onChanged: (v) => sex = v!,
              ),
              const SizedBox(height: 18),
              FormFieldBox(
                '年龄（可选）',
                age,
                keyboardType: TextInputType.number,
                validator: (v) =>
                    v!.isEmpty ||
                        (int.tryParse(v) != null &&
                            int.parse(v) >= 0 &&
                            int.parse(v) <= 120)
                    ? null
                    : '请输入0至120的整数',
              ),
              FormFieldBox('备注', note, lines: 3),
              FilledButton(
                onPressed: () {
                  if (!form.currentState!.validate()) return;
                  if (widget.patientId != null &&
                      store.executing?.patientId == widget.patientId) {
                    message(context, '当前任务结束后才可修改患者资料');
                    return;
                  }
                  final p = widget.patientId == null
                      ? Patient(
                          id: store.nextId(),
                          name: name.text.trim(),
                          bed: bed.text.trim(),
                        )
                      : store.patient(widget.patientId!);
                  p.name = name.text.trim();
                  p.bed = bed.text.trim();
                  p.sex = sex;
                  p.age = int.tryParse(age.text);
                  p.note = note.text.trim();
                  if (widget.patientId == null) store.patients.add(p);
                  store.log('保存患者', p.id);
                  Navigator.pop(context);
                },
                child: const Text('保存患者'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class PrescriptionForm extends StatefulWidget {
  const PrescriptionForm({
    super.key,
    required this.patientId,
    this.medicationId,
  });
  final String patientId;
  final String? medicationId;
  @override
  State<PrescriptionForm> createState() => _PrescriptionFormState();
}

class _PrescriptionFormState extends State<PrescriptionForm> {
  final form = GlobalKey<FormState>();
  final name = TextEditingController(),
      spec = TextEditingController(),
      duration = TextEditingController(text: '30');
  final doses = List.generate(3, (_) => TextEditingController(text: '0'));
  String meal = '饭后';
  bool active = true, loaded = false;
  DateTime start = day(DateTime.now());
  @override
  void dispose() {
    for (final c in [name, spec, duration, ...doses]) {
      c.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context),
        p = AppScope.of(context).patient(widget.patientId);
    if (!loaded) {
      loaded = true;
      if (widget.medicationId != null) {
        final m = p.prescriptions.firstWhere(
          (m) => m.id == widget.medicationId,
        );
        name.text = m.name;
        spec.text = m.spec;
        duration.text = '${m.duration}';
        for (var i = 0; i < 3; i++) {
          doses[i].text = '${m.doses[i]}';
        }
        meal = m.meal;
        active = m.active;
        start = m.start;
      }
    }
    return PageFrame(
      title: widget.medicationId == null ? '新增处方' : '编辑处方',
      subtitle: '${p.bed} · ${p.name}',
      child: Section(
        child: Form(
          key: form,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              FormFieldBox('药品名称', name, validator: requiredText),
              FormFieldBox('规格', spec, validator: requiredText),
              for (var i = 0; i < 3; i++)
                FormFieldBox(
                  '${['早', '午', '晚'][i]} · 每次片数',
                  doses[i],
                  keyboardType: TextInputType.number,
                  validator: (v) =>
                      int.tryParse(v ?? '') != null &&
                          int.parse(v!) >= 0 &&
                          int.parse(v) <= 20
                      ? null
                      : '请输入0至20的整数，半片剂量需人工处理',
                ),
              DropdownButtonFormField<String>(
                initialValue: meal,
                decoration: const InputDecoration(labelText: '用药时机'),
                items: ['饭前', '饭后', '随餐']
                    .map((s) => DropdownMenuItem(value: s, child: Text(s)))
                    .toList(),
                onChanged: (v) => meal = v!,
              ),
              const SizedBox(height: 18),
              OutlinedButton.icon(
                icon: const Icon(Icons.calendar_today),
                label: Text('开始日期 ${dateText(start)}'),
                onPressed: () async {
                  final date = await showDatePicker(
                    context: context,
                    initialDate: start,
                    firstDate: DateTime(2020),
                    lastDate: DateTime(2100),
                  );
                  if (date != null && mounted) setState(() => start = date);
                },
              ),
              const SizedBox(height: 18),
              FormFieldBox(
                '疗程天数',
                duration,
                keyboardType: TextInputType.number,
                validator: (v) =>
                    int.tryParse(v ?? '') != null &&
                        int.parse(v!) >= 1 &&
                        int.parse(v) <= 365
                    ? null
                    : '请输入1至365天',
              ),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text('启用此处方'),
                value: active,
                onChanged: (v) => setState(() => active = v),
              ),
              FilledButton(
                onPressed: () {
                  if (!form.currentState!.validate()) return;
                  if (doses.every((d) => int.parse(d.text) == 0)) {
                    message(context, '至少一个时段需要大于0片');
                    return;
                  }
                  if (store.executing?.patientId == p.id) {
                    message(context, '当前任务结束后才可修改处方');
                    return;
                  }
                  final m = Medication(
                    id: widget.medicationId ?? store.nextId(),
                    name: name.text.trim(),
                    spec: spec.text.trim(),
                    doses: doses.map((d) => int.parse(d.text)).toList(),
                    meal: meal,
                    start: start,
                    duration: int.parse(duration.text),
                    active: active,
                  );
                  final index = p.prescriptions.indexWhere((r) => r.id == m.id);
                  if (index < 0) {
                    p.prescriptions.add(m);
                  } else {
                    p.prescriptions[index] = m;
                  }
                  store.log('保存处方', '${p.id} · ${m.name}');
                  Navigator.pop(context);
                },
                child: const Text('保存处方'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class BoxBindingPage extends StatefulWidget {
  const BoxBindingPage({super.key, required this.patientId});
  final String patientId;
  @override
  State<BoxBindingPage> createState() => _BoxBindingPageState();
}

class _BoxBindingPageState extends State<BoxBindingPage> {
  final uid = TextEditingController();
  String? error;
  @override
  void dispose() {
    uid.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context),
        p = AppScope.of(context).patient(widget.patientId);
    return PageFrame(
      title: '药盒管理',
      subtitle: '${p.bed} · ${p.name}',
      child: Section(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            for (final box in p.boxes)
              ListTile(
                contentPadding: EdgeInsets.zero,
                title: Text(box, style: MdisType.monospace),
                trailing: IconButton(
                  tooltip: '解除绑定',
                  icon: const Icon(Icons.link_off),
                  onPressed: () async {
                    if (await confirm(context, '解除药盒绑定', box)) {
                      p.boxes = List.of(p.boxes)..remove(box);
                      store.log('解除药盒绑定', p.id);
                    }
                  },
                ),
              ),
            const SizedBox(height: 16),
            FormFieldBox('RFID UID', uid),
            if (error != null) Text(error!),
            const SizedBox(height: 12),
            FilledButton(
              onPressed: () {
                final code = uid.text.trim().toUpperCase();
                if (!RegExp(r'^[0-9A-F]{8,32}$').hasMatch(code) ||
                    code.length.isOdd) {
                  setState(() => error = '请输入8至32位、偶数长度的十六进制 UID');
                  return;
                }
                if (store.patients.any((p) => p.boxes.contains(code))) {
                  setState(() => error = '这个药盒已被绑定，请先核对归属');
                  return;
                }
                p.boxes = [...p.boxes, code];
                uid.clear();
                setState(() => error = null);
                store.log('绑定药盒', '${p.id} · $code');
              },
              child: const Text('绑定药盒'),
            ),
          ],
        ),
      ),
    );
  }
}
