import 'package:flutter/material.dart';

import '../domain/store.dart';
import '../theme.dart';
import '../ui/components.dart';

class AccountPage extends StatefulWidget {
  const AccountPage({super.key});
  @override
  State<AccountPage> createState() => _AccountPageState();
}

class _AccountPageState extends State<AccountPage> {
  final form = GlobalKey<FormState>();
  final username = TextEditingController(), password = TextEditingController();
  bool hidden = true, waiting = false;
  String? error;
  @override
  void dispose() {
    username.dispose();
    password.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context);
    if (store.operatorName != null) {
      return PageFrame(
        title: '我的账户',
        child: Section(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              InfoLine('姓名', store.operatorName!),
              InfoLine('机构', store.institution),
              const InfoLine('角色', '管理员'),
              const SizedBox(height: 20),
              ActionRow(
                children: [
                  OutlinedButton(
                    onPressed: store.busy
                        ? null
                        : () => openPage(context, const InstitutionPage()),
                    child: const Text('切换机构'),
                  ),
                  TextButton(
                    onPressed: store.busy
                        ? null
                        : () async {
                            if (await confirm(
                              context,
                              '退出账户',
                              '当前任务和记录仍保留在本机本次运行中。',
                            )) {
                              store.log('退出账户', store.operatorName!);
                              store.operatorName = null;
                              store.changed();
                            }
                          },
                    child: const Text('退出登录'),
                  ),
                ],
              ),
            ],
          ),
        ),
      );
    }
    return PageFrame(
      title: '登录 Mdis',
      subtitle: '使用机构分配的账户',
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 480),
          child: Section(
            child: Form(
              key: form,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  FormFieldBox('账号', username, validator: requiredText),
                  FormFieldBox(
                    '密码',
                    password,
                    obscure: hidden,
                    validator: (v) =>
                        v == null || v.length < 6 ? '请输入至少6位密码' : null,
                  ),
                  TextButton(
                    onPressed: () => setState(() => hidden = !hidden),
                    child: Text(hidden ? '显示密码' : '隐藏密码'),
                  ),
                  if (error != null)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: Text(error!),
                    ),
                  FilledButton(
                    onPressed: waiting
                        ? null
                        : () async {
                            if (!form.currentState!.validate()) return;
                            setState(() => waiting = true);
                            await Future<void>.delayed(
                              const Duration(milliseconds: 350),
                            );
                            if (!mounted) return;
                            setState(() => waiting = false);
                            // No fake server authentication. Local UI login is explicitly selected below.
                            setState(() => error = '认证服务未连接，请稍后重试或联系机构管理员');
                          },
                    child: Text(waiting ? '正在登录…' : '登录'),
                  ),
                  const SizedBox(height: 16),
                  TextButton(
                    onPressed: () => showDialog<void>(
                      context: context,
                      builder: (context) => AlertDialog(
                        title: const Text('找回账户'),
                        content: const Text('请联系机构管理员重置密码。'),
                        actions: [
                          TextButton(
                            onPressed: () => Navigator.pop(context),
                            child: const Text('知道了'),
                          ),
                        ],
                      ),
                    ),
                    child: const Text('忘记密码'),
                  ),
                  const Divider(),
                  OutlinedButton(
                    onPressed: () {
                      store.operatorName = '本机操作人';
                      store.log('进入本机工作区', '本机操作人');
                      openPage(context, const InstitutionPage());
                    },
                    child: const Text('进入本机工作区'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class InstitutionPage extends StatelessWidget {
  const InstitutionPage({super.key});
  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context);
    return PageFrame(
      title: '选择机构',
      subtitle: '当前本机工作区',
      child: Section(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.apartment),
              title: Text(store.institution, style: MdisType.cardTitle),
              subtitle: const Text('本机资料'),
              trailing: const Icon(Icons.check_circle_outline),
            ),
            const SizedBox(height: 16),
            const Text('其他机构需登录并获取授权后显示。'),
            const SizedBox(height: 20),
            FilledButton(
              onPressed: () {
                store.log('选择机构', store.institution);
                Navigator.pop(context);
              },
              child: const Text('进入机构'),
            ),
          ],
        ),
      ),
    );
  }
}

class MembersPage extends StatefulWidget {
  const MembersPage({super.key});
  @override
  State<MembersPage> createState() => _MembersPageState();
}

class _MembersPageState extends State<MembersPage> {
  @override
  Widget build(BuildContext context) {
    final store = AppScope.of(context);
    return PageFrame(
      title: '成员与权限',
      subtitle: store.institution,
      actions: [
        FilledButton.icon(
          onPressed: () => _edit(context, store),
          icon: const Icon(Icons.add),
          label: const Text('添加成员'),
        ),
      ],
      child: Column(
        children: [
          Section(
            title: '角色权限',
            child: const Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                InfoLine('管理员', '患者、处方、设备、成员和记录'),
                InfoLine('医生', '患者、处方和记录'),
                InfoLine('护理员', '患者查看、摆药和记录'),
              ],
            ),
          ),
          Section(
            child: Column(
              children: [
                for (final m in store.members)
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    title: Text(m.name),
                    subtitle: Text('${m.role} · ${m.enabled ? '启用' : '停用'}'),
                    trailing: OutlinedButton(
                      onPressed: () => _edit(context, store, member: m),
                      child: const Text('编辑'),
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _edit(
    BuildContext context,
    AppStore store, {
    TeamMember? member,
  }) async {
    final name = TextEditingController(text: member?.name);
    var role = member?.role ?? '护理员';
    var enabled = member?.enabled ?? true;
    final form = GlobalKey<FormState>();
    final route = DialogRoute<void>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setState) => AlertDialog(
          scrollable: true,
          title: Text(member == null ? '添加成员' : '编辑成员'),
          content: SizedBox(
            width: 440,
            child: Form(
              key: form,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  FormFieldBox(
                    '姓名 / 账号',
                    name,
                    validator: (v) =>
                        requiredText(v) ??
                        (store.members.any(
                              (m) => m != member && m.name == v!.trim(),
                            )
                            ? '成员已存在'
                            : null),
                  ),
                  DropdownButtonFormField<String>(
                    initialValue: role,
                    decoration: const InputDecoration(labelText: '角色'),
                    items: ['管理员', '医生', '护理员']
                        .map((r) => DropdownMenuItem(value: r, child: Text(r)))
                        .toList(),
                    onChanged: (v) => role = v!,
                  ),
                  SwitchListTile(
                    title: const Text('启用成员'),
                    value: enabled,
                    onChanged: (v) => setState(() => enabled = v),
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('取消'),
            ),
            FilledButton(
              onPressed: () {
                if (!form.currentState!.validate()) return;
                if (member?.role == '管理员' &&
                    member!.enabled &&
                    (role != '管理员' || !enabled) &&
                    store.members
                            .where((m) => m.role == '管理员' && m.enabled)
                            .length ==
                        1) {
                  message(context, '至少保留一位启用的管理员');
                  return;
                }
                if (member == null) {
                  store.members.add(
                    TeamMember(name.text.trim(), role, enabled: enabled),
                  );
                } else {
                  member.name = name.text.trim();
                  member.role = role;
                  member.enabled = enabled;
                }
                store.log('保存成员', name.text.trim());
                Navigator.pop(context);
              },
              child: const Text('保存'),
            ),
          ],
        ),
      ),
    );
    await Navigator.of(context).push(route);
    await route.completed;
    name.dispose();
  }
}
