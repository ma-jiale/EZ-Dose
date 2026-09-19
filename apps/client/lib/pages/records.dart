import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../domain/store.dart';
import '../theme.dart';
import '../ui/components.dart';
import 'session.dart';

class RecordsPage extends StatefulWidget {
  const RecordsPage({super.key});
  @override
  State<RecordsPage> createState() => _RecordsPageState();
}

class _RecordsPageState extends State<RecordsPage> {
  String query = '';
  SessionState? status;
  bool audits = false;
  DateTimeRange? range;
  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context);
    final records = store.sessions
        .where(
          (s) =>
              (status == null || s.state == status) &&
              '${s.bed} ${s.patientName} ${s.id}'.toLowerCase().contains(
                query.toLowerCase(),
              ) &&
              (range == null ||
                  (!day(s.created).isBefore(range!.start) &&
                      !day(s.created).isAfter(range!.end))),
        )
        .toList()
        .reversed
        .toList();
    return PageFrame(
      embedded: true,
      title: '记录',
      subtitle: '查看摆药结果与操作轨迹',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          ActionRow(
            children: [
              ChoiceChip(
                label: const Text('摆药记录'),
                selected: !audits,
                onSelected: (_) => setState(() => audits = false),
              ),
              ChoiceChip(
                label: const Text('操作日志'),
                selected: audits,
                onSelected: (_) => setState(() => audits = true),
              ),
            ],
          ),
          const SizedBox(height: 20),
          if (audits)
            Section(
              child: store.audit.isEmpty
                  ? const EmptyState('暂无操作日志')
                  : Column(
                      children: [
                        for (final a in store.audit)
                          ListTile(
                            contentPadding: EdgeInsets.zero,
                            title: Text(a.action, style: MdisType.bodyStrong),
                            subtitle: Text(
                              '${a.detail}\n${dateText(a.time)} ${a.time.hour.toString().padLeft(2, '0')}:${a.time.minute.toString().padLeft(2, '0')} · ${a.actor}',
                            ),
                          ),
                      ],
                    ),
            )
          else ...[
            TextField(
              onChanged: (v) => setState(() => query = v),
              decoration: const InputDecoration(
                hintText: '搜索床号、姓名或任务编号',
                prefixIcon: Icon(Icons.search),
              ),
            ),
            const SizedBox(height: 16),
            ActionRow(
              children: [
                DropdownButton<SessionState?>(
                  value: status,
                  items: [
                    const DropdownMenuItem(value: null, child: Text('全部状态')),
                    for (final s in SessionState.values)
                      DropdownMenuItem(value: s, child: Text(s.label)),
                  ],
                  onChanged: (v) => setState(() => status = v),
                ),
                OutlinedButton.icon(
                  onPressed: () async {
                    final r = await showDateRangePicker(
                      context: context,
                      firstDate: DateTime(2020),
                      lastDate: DateTime(2100),
                      initialDateRange: range,
                    );
                    if (r != null && mounted) setState(() => range = r);
                  },
                  icon: const Icon(Icons.calendar_today),
                  label: Text(
                    range == null
                        ? '选择日期'
                        : '${dateText(range!.start)} — ${dateText(range!.end)}',
                  ),
                ),
                if (range != null)
                  TextButton(
                    onPressed: () => setState(() => range = null),
                    child: const Text('清除日期'),
                  ),
                OutlinedButton(
                  onPressed: records.isEmpty
                      ? null
                      : () => _export(context, records),
                  child: const Text('导出清单'),
                ),
              ],
            ),
            const SizedBox(height: 20),
            if (records.isEmpty) const EmptyState('没有匹配的摆药记录'),
            for (final s in records)
              Section(
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  title: Text(
                    '${s.bed} · ${s.patientName}',
                    style: MdisType.cardTitle,
                  ),
                  subtitle: Text(
                    '${rangeText(s.start, s.days)}\n${s.id} · ${s.state.label} · ${s.synced ? '已同步' : '待同步'}',
                  ),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => openPage(context, RecordDetail(session: s)),
                ),
              ),
          ],
        ],
      ),
    );
  }

  void _export(BuildContext context, List<DispensingSession> records) {
    String csvCell(String v) => '"${v.replaceAll('"', '""')}"';
    final csv = [
      '任务编号,床号,姓名,开始日期,天数,状态,同步状态',
      ...records.map(
        (s) => [
          s.id,
          s.bed,
          s.patientName,
          dateText(s.start),
          '${s.days}',
          s.state.label,
          s.synced ? '已同步' : '待同步',
        ].map(csvCell).join(','),
      ),
    ].join('\n');
    showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('导出记录清单'),
        content: SizedBox(
          width: 680,
          child: SingleChildScrollView(
            child: SelectableText(csv, style: MdisType.monospace),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('关闭'),
          ),
          FilledButton(
            onPressed: () async {
              await Clipboard.setData(ClipboardData(text: csv));
              if (context.mounted) message(context, '已复制 CSV，可粘贴到表格或文件中');
            },
            child: const Text('复制 CSV'),
          ),
        ],
      ),
    );
  }
}

class RecordDetail extends StatelessWidget {
  const RecordDetail({super.key, required this.session});
  final DispensingSession session;
  @override
  Widget build(BuildContext context) {
    AppScope.of(context);
    final s = session;
    return PageFrame(
      title: '摆药记录',
      subtitle: s.id,
      actions: [StatusPill(s.state.label)],
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Section(
            title: '任务信息',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                InfoLine('患者', '${s.bed} · ${s.patientName}'),
                InfoLine('覆盖日期', rangeText(s.start, s.days)),
                InfoLine('操作人', s.operator),
                InfoLine('创建时间', dateText(s.created)),
                InfoLine('业务状态', s.state.label),
                InfoLine('同步状态', s.synced ? '已同步' : '待同步'),
              ],
            ),
          ),
          for (final step in s.steps)
            Section(
              title: step.medication.name,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  InfoLine('规格', step.medication.spec),
                  InfoLine('分配', '目标 ${step.target}片 · 已确认 ${step.actual}片'),
                  StatusPill(step.state.label),
                  MedicationMatrix(session: s, medication: step.medication),
                  if (step.note.isNotEmpty) Text('核对备注：${step.note}'),
                ],
              ),
            ),
          if (s.state != SessionState.completed)
            FilledButton(
              onPressed: () {
                if (s.state == SessionState.supplement) {
                  openPage(context, SessionResult(session: s));
                } else if (s.state == SessionState.ready) {
                  openPage(context, VerifyBoxPage(session: s));
                } else {
                  openPage(context, SessionPage(session: s));
                }
              },
              child: const Text('继续处理任务'),
            ),
        ],
      ),
    );
  }
}
