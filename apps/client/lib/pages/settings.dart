import 'package:flutter/material.dart';

import '../domain/store.dart';
import '../domain/machine.dart';
import '../theme.dart';
import '../ui/components.dart';
import 'account.dart';

class SettingsPage extends StatefulWidget {
  const SettingsPage({super.key});
  @override
  State<SettingsPage> createState() => _SettingsPageState();
}

class _SettingsPageState extends State<SettingsPage> {
  final url = TextEditingController();
  final form = GlobalKey<FormState>();
  bool loaded = false;
  int days = 7, reminder = 2;
  @override
  void dispose() {
    url.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context);
    if (!loaded) {
      loaded = true;
      url.text = store.serverUrl;
      days = store.maxDays;
      reminder = store.reminderDays;
    }
    return PageFrame(
      embedded: true,
      title: '设置',
      subtitle: '设备、摆药偏好与账户',
      child: ResponsiveColumns(
        main: Column(
          children: [
            Section(
              title: '摆药偏好',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  DropdownButtonFormField<int>(
                    initialValue: days,
                    decoration: const InputDecoration(labelText: '默认摆药天数'),
                    items: List.generate(
                      7,
                      (i) => DropdownMenuItem(
                        value: i + 1,
                        child: Text('${i + 1}天'),
                      ),
                    ),
                    onChanged: store.busy ? null : (v) => days = v!,
                  ),
                  const SizedBox(height: 18),
                  DropdownButtonFormField<int>(
                    initialValue: reminder,
                    decoration: const InputDecoration(labelText: '提前提醒天数'),
                    items: List.generate(
                      15,
                      (i) => DropdownMenuItem(value: i, child: Text('$i天')),
                    ),
                    onChanged: (v) => reminder = v!,
                  ),
                  const SizedBox(height: 18),
                  FilledButton(
                    onPressed: store.busy
                        ? null
                        : () {
                            store.maxDays = days;
                            store.reminderDays = reminder;
                            store.log('保存摆药偏好', '$days天 / 提前$reminder天');
                            message(context, '偏好已保存');
                          },
                    child: const Text('保存偏好'),
                  ),
                ],
              ),
            ),
            Section(
              title: '服务连接',
              child: Form(
                key: form,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    FormFieldBox(
                      '服务器地址',
                      url,
                      validator: (v) {
                        if (v == null || v.trim().isEmpty) return null;
                        final uri = Uri.tryParse(v.trim());
                        return uri != null &&
                                ['http', 'https'].contains(uri.scheme) &&
                                uri.host.isNotEmpty
                            ? null
                            : '请输入有效的 HTTP 或 HTTPS 地址';
                      },
                    ),
                    ActionRow(
                      children: [
                        FilledButton(
                          onPressed: () {
                            if (form.currentState!.validate()) {
                              store.serverUrl = url.text.trim();
                              store.log('保存连接配置', '服务器地址已更新');
                              message(context, '连接配置已保存');
                            }
                          },
                          child: const Text('保存地址'),
                        ),
                        OutlinedButton(
                          onPressed: () =>
                              message(context, '服务尚未连接，当前没有可用的同步通道'),
                          child: const Text('检查连接'),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    InfoLine(
                      '待同步任务',
                      '${store.sessions.where((s) => !s.synced).length}条',
                    ),
                    TextButton(
                      onPressed: () => openPage(context, const SyncPage()),
                      child: const Text('查看同步队列'),
                    ),
                  ],
                ),
              ),
            ),
            Section(
              title: '显示与辅助功能',
              child: Column(
                children: [
                  DropdownButtonFormField<double>(
                    initialValue: store.textScale,
                    decoration: const InputDecoration(labelText: '文字大小'),
                    items: const [
                      DropdownMenuItem(value: 1, child: Text('标准')),
                      DropdownMenuItem(value: 1.25, child: Text('较大')),
                      DropdownMenuItem(value: 1.5, child: Text('大')),
                    ],
                    onChanged: (v) {
                      store.textScale = v!;
                      store.changed();
                    },
                  ),
                  const SizedBox(height: 16),
                  const InfoLine(
                    '字体',
                    'Segoe UI Variable / Microsoft YaHei UI',
                  ),
                ],
              ),
            ),
          ],
        ),
        side: Column(
          children: [
            Section(
              title: '设备与工具',
              child: Column(
                children: [
                  _link(
                    '分药机',
                    Icons.precision_manufacturing_outlined,
                    () => openPage(context, const DevicesPage()),
                  ),
                  _link(
                    '药物校准',
                    Icons.tune,
                    () => openPage(context, const CalibrationPage()),
                  ),
                  _link(
                    '辅助数药',
                    Icons.camera_alt_outlined,
                    () => openPage(context, const CountingPage()),
                  ),
                ],
              ),
            ),
            Section(
              title: '账户与机构',
              child: Column(
                children: [
                  _link(
                    store.operatorName ?? '登录账户',
                    Icons.account_circle_outlined,
                    () => openPage(context, const AccountPage()),
                  ),
                  _link(
                    '机构选择',
                    Icons.apartment,
                    () => openPage(context, const InstitutionPage()),
                  ),
                  _link(
                    '成员与权限',
                    Icons.group_outlined,
                    () => openPage(context, const MembersPage()),
                  ),
                ],
              ),
            ),
            Section(
              title: '关于 Mdis',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Mdis', style: MdisType.sectionTitle),
                  const SizedBox(height: 8),
                  const Text('版本 0.1.0'),
                  const SizedBox(height: 8),
                  const Text('帮助护理人员有序完成药物准备、核对与记录。'),
                  TextButton(
                    onPressed: () => showAboutDialog(
                      context: context,
                      applicationName: 'Mdis',
                      applicationVersion: '0.1.0',
                      applicationLegalese: '护理用药辅助系统',
                    ),
                    child: const Text('版本与许可'),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _link(String text, IconData icon, VoidCallback action) => ListTile(
    contentPadding: EdgeInsets.zero,
    leading: Icon(icon),
    title: Text(text),
    trailing: const Icon(Icons.chevron_right),
    onTap: action,
  );
}

class DevicesPage extends StatefulWidget {
  const DevicesPage({super.key});
  @override
  State<DevicesPage> createState() => _DevicesPageState();
}

class _DevicesPageState extends State<DevicesPage> {
  bool searching = false, discovered = false, details = false;
  String tray = '已收回';
  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context), machine = store.machine;
    return PageFrame(
      title: '分药机',
      subtitle: '连接设备并查看运行状态',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Section(
            title: '当前设备',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                InfoLine('连接状态', machine.connected ? '已连接' : '未连接'),
                InfoLine('设备', machine.connected ? machine.name : '未选择'),
                InfoLine(
                  '运行状态',
                  machine.running ? (machine.paused ? '已暂停' : '正在分药') : '空闲',
                ),
                InfoLine('托盘', tray),
                ActionRow(
                  children: [
                    if (machine.connected)
                      OutlinedButton(
                        onPressed: store.busy
                            ? null
                            : () async {
                                if (await confirm(
                                  context,
                                  '断开设备',
                                  '确认断开当前分药机？',
                                )) {
                                  machine.disconnect();
                                  store.log('断开设备', machine.name);
                                }
                              },
                        child: const Text('断开连接'),
                      ),
                    OutlinedButton(
                      onPressed: searching
                          ? null
                          : () async {
                              setState(() => searching = true);
                              await Future<void>.delayed(
                                const Duration(milliseconds: 500),
                              );
                              if (mounted) {
                                setState(() {
                                  searching = false;
                                  discovered = true;
                                });
                              }
                            },
                      child: Text(searching ? '正在查找…' : '查找设备'),
                    ),
                  ],
                ),
              ],
            ),
          ),
          if (discovered)
            Section(
              title: '可用设备',
              child: ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.precision_manufacturing_outlined),
                title: Text(machine.name),
                subtitle: const Text('本机软件设备 · 不连接物理分药机'),
                trailing: FilledButton(
                  onPressed: machine.connected || store.busy
                      ? null
                      : () {
                          machine.connect();
                          store.log('连接设备', machine.name);
                        },
                  child: Text(machine.connected ? '已连接' : '连接'),
                ),
              ),
            ),
          if (!discovered && !machine.connected)
            const EmptyState('点击“查找设备”选择分药机'),
          Section(
            title: '设备维护',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (store.busy)
                  const Padding(
                    padding: EdgeInsets.only(bottom: 12),
                    child: Text('当前有未结束的摆药任务，维护操作已锁定'),
                  ),
                ActionRow(
                  children: [
                    for (final action in ['推出托盘', '收回托盘', '清洁药仓', '复位设备'])
                      OutlinedButton(
                        onPressed: !machine.connected || store.busy
                            ? null
                            : () async {
                                if (await confirm(
                                  context,
                                  action,
                                  '请确认药仓已清空，手部远离运动机构。',
                                  action: '确认操作',
                                )) {
                                  if (mounted) {
                                    setState(
                                      () => tray = action == '推出托盘'
                                          ? '已推出'
                                          : '已收回',
                                    );
                                  }
                                  store.log('设备维护', action);
                                }
                              },
                        child: Text(action),
                      ),
                  ],
                ),
              ],
            ),
          ),
          ExpansionTile(
            title: const Text('设备详情'),
            initiallyExpanded: details,
            onExpansionChanged: (v) => details = v,
            children: [
              Section(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const InfoLine('传输', '本机软件模拟'),
                    const InfoLine('Windows 串口参数', '115200 · 8N1'),
                    const InfoLine('Android 传输', 'HC-06 蓝牙（待接入）'),
                    const SizedBox(height: 16),
                    DropdownButtonFormField<MachineFault>(
                      initialValue: machine.nextFault,
                      decoration: const InputDecoration(labelText: '下一次反馈测试'),
                      items: const [
                        DropdownMenuItem(
                          value: MachineFault.none,
                          child: Text('正常完成'),
                        ),
                        DropdownMenuItem(
                          value: MachineFault.count,
                          child: Text('计数异常'),
                        ),
                        DropdownMenuItem(
                          value: MachineFault.connection,
                          child: Text('连接中断'),
                        ),
                        DropdownMenuItem(
                          value: MachineFault.uncertain,
                          child: Text('结果不明确'),
                        ),
                      ],
                      onChanged: machine.running
                          ? null
                          : (v) {
                              machine.nextFault = v!;
                              store.changed();
                            },
                    ),
                  ],
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class CalibrationPage extends StatefulWidget {
  const CalibrationPage({super.key, this.medication});
  final Medication? medication;
  @override
  State<CalibrationPage> createState() => _CalibrationPageState();
}

class _CalibrationPageState extends State<CalibrationPage> {
  final diameter = TextEditingController(text: '9.0');
  final form = GlobalKey<FormState>();
  int phase = 0;
  @override
  void dispose() {
    diameter.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context);
    return PageFrame(
      title: '药物校准',
      subtitle: widget.medication?.name ?? '参考药片与识别参数',
      child: Section(
        child: Form(
          key: form,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                ['准备校准', '采集参考药片', '核对校准结果', '校准完成'][phase],
                style: MdisType.sectionTitle,
              ),
              const SizedBox(height: 20),
              LinearProgressIndicator(
                value: phase / 3,
                color: MdisColors.primary,
                backgroundColor: MdisColors.line,
              ),
              const SizedBox(height: 24),
              if (phase == 0) ...[
                const Text('先清空药盘，将标准参考药片准备在旁边。'),
                const SizedBox(height: 16),
                FormFieldBox(
                  '参考药片直径（mm）',
                  diameter,
                  keyboardType: TextInputType.number,
                  validator: (v) {
                    final n = double.tryParse(v ?? '');
                    return n != null && n.isFinite && n >= 1 && n <= 30
                        ? null
                        : '请输入1至30 mm';
                  },
                ),
              ],
              if (phase == 1) ...[
                Container(
                  height: 180,
                  alignment: Alignment.center,
                  color: MdisColors.background,
                  child: const Icon(Icons.camera_alt_outlined, size: 48),
                ),
                const SizedBox(height: 16),
                const Text('在本地测试设备上采集一颗参考药片，完成后核对规格。'),
              ],
              if (phase >= 2) ...[
                InfoLine('参考直径', '${diameter.text} mm'),
                InfoLine('药物', widget.medication?.name ?? '参考药片'),
                const InfoLine('样本', '1颗'),
                const SizedBox(height: 16),
              ],
              ActionRow(
                children: [
                  if (phase > 0 && phase < 3)
                    OutlinedButton(
                      onPressed: () => setState(() => phase = 0),
                      child: const Text('重新采集'),
                    ),
                  FilledButton(
                    onPressed: store.machine.running
                        ? null
                        : () {
                            if (phase == 0 && !form.currentState!.validate()) {
                              return;
                            }
                            if (phase == 2) {
                              store.referenceDiameter = double.parse(
                                diameter.text,
                              );
                              widget.medication?.calibrated = true;
                              if (widget.medication != null) {
                                for (final p in store.patients) {
                                  for (final m in p.prescriptions) {
                                    if (m.id == widget.medication!.id &&
                                        m.name == widget.medication!.name &&
                                        m.spec == widget.medication!.spec) {
                                      m.calibrated = true;
                                    }
                                  }
                                }
                              }
                              store.log(
                                '保存校准',
                                widget.medication?.name ?? '参考药片',
                              );
                            }
                            if (phase == 3) {
                              Navigator.pop(context);
                              return;
                            }
                            setState(() => phase++);
                          },
                    child: Text(['开始校准', '采集参考药片', '确认校准', '完成'][phase]),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class CountingPage extends StatefulWidget {
  const CountingPage({super.key});
  @override
  State<CountingPage> createState() => _CountingPageState();
}

class _CountingPageState extends State<CountingPage> {
  final count = TextEditingController();
  bool captured = false;
  @override
  void dispose() {
    count.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context);
    return PageFrame(
      title: '辅助数药',
      subtitle: '数药结果需人工核对，不会自动记为摆药完成',
      child: Section(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              height: 240,
              color: MdisColors.background,
              alignment: Alignment.center,
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.camera_alt_outlined, size: 48),
                  const SizedBox(height: 16),
                  Text(captured ? '已采集本地测试样本' : '等待采集药盘图像'),
                ],
              ),
            ),
            const SizedBox(height: 24),
            ActionRow(
              children: [
                OutlinedButton(
                  onPressed: () {
                    setState(() {
                      captured = true;
                      count.text = '12';
                    });
                  },
                  child: Text(captured ? '重新采集' : '采集样本'),
                ),
                TextButton(
                  onPressed: () => openPage(context, const CalibrationPage()),
                  child: const Text('校准药片'),
                ),
              ],
            ),
            const SizedBox(height: 20),
            if (captured) ...[
              Text('${count.text} 片', style: MdisType.display),
              const SizedBox(height: 16),
              FormFieldBox(
                '人工核对数量',
                count,
                keyboardType: TextInputType.number,
                onChanged: (_) => setState(() {}),
              ),
              FilledButton(
                onPressed: () {
                  final n = int.tryParse(count.text);
                  if (n == null || n < 0 || n > 1000) {
                    message(context, '请输入0至1000的整数');
                    return;
                  }
                  store.log('辅助数药', '人工核对 $n 片');
                  message(context, '已记录核对数量');
                },
                child: const Text('记录核对数量'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class SyncPage extends StatelessWidget {
  const SyncPage({super.key});
  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context),
        items = AppScope.of(context).sessions.where((s) => !s.synced).toList();
    return PageFrame(
      title: '同步队列',
      subtitle: '业务结果与同步状态独立保存',
      child: Section(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const InfoLine('服务连接', '未连接'),
            const Text('恢复服务连接后再上传；同步不会重新执行分药。'),
            const SizedBox(height: 20),
            if (items.isEmpty) const EmptyState('没有待同步记录'),
            for (final s in items)
              ListTile(
                contentPadding: EdgeInsets.zero,
                title: Text('${s.bed} · ${s.patientName}'),
                subtitle: Text('${s.id} · ${s.state.label}'),
                trailing: const StatusPill('待同步'),
              ),
            OutlinedButton(
              onPressed: () {
                store.log('检查同步连接', '服务未连接');
                message(context, '没有可用的同步通道，记录仍保留为待同步');
              },
              child: const Text('检查同步连接'),
            ),
          ],
        ),
      ),
    );
  }
}
