import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../theme.dart';
import 'demo_tasks.dart';
import '../domain/store.dart';
import '../pages/patients.dart';
import '../pages/records.dart';
import '../pages/settings.dart';
import '../pages/account.dart';
import '../ui/components.dart';

class WorkbenchPage extends StatefulWidget {
  const WorkbenchPage({super.key});

  @override
  State<WorkbenchPage> createState() => _WorkbenchPageState();
}

class _WorkbenchPageState extends State<WorkbenchPage> {
  static const _pageSize = 6;
  TaskStatus _status = TaskStatus.pending;
  final _search = TextEditingController();
  final _scroll = ScrollController();
  String _query = '';
  String _section = '工作台';
  final _shellKey = GlobalKey<ScaffoldState>();
  int _page = 0;

  @override
  void dispose() {
    _search.dispose();
    _scroll.dispose();
    super.dispose();
  }

  void _setQuery(String query) => setState(() {
    _query = query.trim();
    _page = 0;
  });

  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, bounds) {
      final narrow = bounds.maxWidth < 600;
      final compact =
          bounds.maxWidth < 1200 ||
          MediaQuery.textScalerOf(context).scale(1) >= 1.5;
      return Scaffold(
        key: _shellKey,
        appBar: narrow
            ? AppBar(title: Text('Mdis', style: MdisType.pageTitle))
            : null,
        drawer: narrow ? Drawer(child: _sidebar(false)) : null,
        body: SafeArea(
          child: Row(
            children: [
              if (!narrow)
                SizedBox(width: compact ? 88 : 240, child: _sidebar(compact)),
              Expanded(
                child: switch (_section) {
                  '患者' => const PatientsPage(),
                  '记录' => const RecordsPage(),
                  '设置' => const SettingsPage(),
                  _ => _content(),
                },
              ),
            ],
          ),
        ),
      );
    },
  );

  Widget _sidebar(bool compact) => Container(
    color: MdisColors.sidebar,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: EdgeInsets.symmetric(
            horizontal: compact ? 24 : 36,
            vertical: 28,
          ),
          child: Row(
            mainAxisAlignment: compact
                ? MainAxisAlignment.center
                : MainAxisAlignment.start,
            children: [
              _BrandLogo(compact: compact),
              if (!compact) ...[
                const SizedBox(width: 12),
                Text('Mdis', style: MdisType.pageTitle),
              ],
            ],
          ),
        ),
        const SizedBox(height: 24),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            children: [
              _nav(Icons.home_rounded, '工作台', compact),
              _nav(Icons.person_outline_rounded, '患者', compact),
              _nav(Icons.article_outlined, '记录', compact),
              _nav(Icons.settings_outlined, '设置', compact),
            ],
          ),
        ),
      ],
    ),
  );

  Widget _nav(
    IconData icon,
    String label,
    bool compact, {
    bool active = false,
  }) {
    active = _section == label;
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Tooltip(
        message: compact ? label : '',
        child: Material(
          color: active ? MdisColors.accentSoft : Colors.transparent,
          borderRadius: BorderRadius.circular(14),
          child: InkWell(
            borderRadius: BorderRadius.circular(14),
            onTap: () {
              setState(() => _section = label);
              _shellKey.currentState?.closeDrawer();
            },
            child: Padding(
              padding: EdgeInsets.symmetric(
                horizontal: compact ? 10 : 20,
                vertical: 18,
              ),
              child: Row(
                mainAxisAlignment: compact
                    ? MainAxisAlignment.center
                    : MainAxisAlignment.start,
                children: [
                  Icon(icon, size: 26),
                  if (!compact) ...[
                    const SizedBox(width: 18),
                    Text(label, style: MdisType.cardTitle),
                  ],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _toolbar() {
    final store = AppScope.of(context);
    return Row(
      children: [
        Expanded(
          child: TextField(
            controller: _search,
            style: MdisType.body,
            onChanged: _setQuery,
            decoration: InputDecoration(
              hintText: '搜索床号或姓名',
              prefixIcon: const Icon(Icons.search_rounded, size: 22),
              suffixIcon: _query.isEmpty
                  ? null
                  : IconButton(
                      tooltip: '清除搜索',
                      onPressed: () {
                        _search.clear();
                        _setQuery('');
                      },
                      icon: const Icon(Icons.close, size: 18),
                    ),
            ),
          ),
        ),
        const SizedBox(width: 12),
        IconButton.filledTonal(
          tooltip: store.machine.connected ? '分药机已连接' : '分药机未连接',
          style: IconButton.styleFrom(
            backgroundColor: Colors.white,
            minimumSize: const Size(48, 48),
          ),
          onPressed: () => openPage(context, const DevicesPage()),
          icon: Badge(
            label: Icon(
              store.machine.connected ? Icons.check : Icons.close,
              size: 10,
              color: Colors.white,
            ),
            backgroundColor: MdisColors.muted,
            child: Icon(Icons.precision_manufacturing_outlined, size: 24),
          ),
        ),
        const SizedBox(width: 12),
        IconButton.filledTonal(
          tooltip: store.operatorName ?? '账户',
          onPressed: () => openPage(context, const AccountPage()),
          style: IconButton.styleFrom(
            backgroundColor: MdisColors.primarySoft,
            minimumSize: const Size(48, 48),
          ),
          icon: const Icon(Icons.person_rounded, color: MdisColors.muted),
        ),
      ],
    );
  }

  Widget _content() {
    final store = AppScope.of(context);
    final demoTasks = store.tasks;
    final tasks = demoTasks
        .where(
          (t) =>
              t.status == _status &&
              '${t.bed} ${t.name}'.toLowerCase().contains(_query.toLowerCase()),
        )
        .toList();
    final pages = math.max(1, (tasks.length / _pageSize).ceil());
    if (_page >= pages) _page = pages - 1;
    final visible = tasks.skip(_page * _pageSize).take(_pageSize).toList();
    final hour = DateTime.now().hour;
    final greeting = hour < 12
        ? '早上好'
        : hour < 18
        ? '下午好'
        : '晚上好';
    return LayoutBuilder(
      builder: (context, bounds) {
        final padding = bounds.maxWidth < 600 ? 20.0 : 36.0;
        return CustomScrollView(
          controller: _scroll,
          slivers: [
            SliverPadding(
              padding: EdgeInsets.fromLTRB(padding, padding, padding, 0),
              sliver: SliverToBoxAdapter(
                child: LayoutBuilder(
                  builder: (context, area) {
                    final greetingBlock = Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          store.operatorName == null
                              ? greeting
                              : '$greeting，${store.operatorName}',
                          style: MdisType.display,
                        ),
                        const SizedBox(height: 8),
                        Text(
                          '今天也辛苦了，一起为长者的健康努力。',
                          style: MdisType.body.copyWith(
                            color: MdisColors.muted,
                          ),
                        ),
                      ],
                    );
                    if (area.maxWidth < 900 ||
                        MediaQuery.textScalerOf(context).scale(1) > 1.3) {
                      return Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          greetingBlock,
                          const SizedBox(height: 24),
                          _toolbar(),
                        ],
                      );
                    }
                    return Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(child: greetingBlock),
                        const SizedBox(width: 24),
                        SizedBox(width: 440, child: _toolbar()),
                      ],
                    );
                  },
                ),
              ),
            ),
            SliverPadding(
              padding: EdgeInsets.fromLTRB(padding, 40, padding, padding),
              sliver: SliverToBoxAdapter(
                child: Center(
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 1280),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('今日摆药', style: MdisType.sectionTitle),
                        const SizedBox(height: 4),
                        Text(
                          '今日共${demoTasks.length}位患者，${demoTasks.where((t) => t.status == TaskStatus.pending).length}位待摆药。',
                          style: MdisType.body.copyWith(
                            color: MdisColors.muted,
                          ),
                        ),
                        const SizedBox(height: 24),
                        Container(
                          decoration: const BoxDecoration(
                            border: Border(
                              bottom: BorderSide(color: MdisColors.line),
                            ),
                          ),
                          child: SingleChildScrollView(
                            scrollDirection: Axis.horizontal,
                            child: Row(
                              children: TaskStatus.values.map((status) {
                                final selected = status == _status;
                                final count = demoTasks
                                    .where((t) => t.status == status)
                                    .length;
                                return Semantics(
                                  selected: selected,
                                  child: Container(
                                    decoration: BoxDecoration(
                                      border: Border(
                                        bottom: BorderSide(
                                          color: selected
                                              ? MdisColors.accent
                                              : Colors.transparent,
                                          width: 2,
                                        ),
                                      ),
                                    ),
                                    child: TextButton(
                                      key: ValueKey(status),
                                      style: TextButton.styleFrom(
                                        minimumSize: const Size(112, 52),
                                        foregroundColor: selected
                                            ? MdisColors.ink
                                            : MdisColors.muted,
                                        shape: const RoundedRectangleBorder(),
                                      ),
                                      onPressed: () => setState(() {
                                        _status = status;
                                        _page = 0;
                                      }),
                                      child: Text(
                                        '${status.label}   $count',
                                        style:
                                            (selected
                                                    ? MdisType.bodyStrong
                                                    : MdisType.body)
                                                .copyWith(
                                                  color: selected
                                                      ? MdisColors.ink
                                                      : MdisColors.muted,
                                                ),
                                      ),
                                    ),
                                  ),
                                );
                              }).toList(),
                            ),
                          ),
                        ),
                        const SizedBox(height: 24),
                        if (tasks.isEmpty)
                          Padding(
                            padding: const EdgeInsets.symmetric(vertical: 60),
                            child: Center(
                              child: Column(
                                children: [
                                  const Icon(
                                    Icons.search_off_rounded,
                                    color: MdisColors.muted,
                                    size: 32,
                                  ),
                                  const SizedBox(height: 12),
                                  const Text('没有匹配的患者'),
                                  TextButton(
                                    onPressed: () {
                                      _search.clear();
                                      _setQuery('');
                                    },
                                    child: const Text('清除搜索条件'),
                                  ),
                                ],
                              ),
                            ),
                          )
                        else
                          LayoutBuilder(
                            builder: (context, area) {
                              final columns =
                                  area.maxWidth >= 860 &&
                                      MediaQuery.textScalerOf(context)
                                              .scale(1) <
                                          1.5
                                  ? 2
                                  : 1;
                              return Wrap(
                                spacing: 16,
                                runSpacing: 16,
                                children: visible
                                    .map(
                                      (task) => SizedBox(
                                        width:
                                            (area.maxWidth -
                                                (columns - 1) * 16) /
                                            columns,
                                        child: _PatientCard(
                                          task: task,
                                          onOpen: () => _openPatient(task),
                                        ),
                                      ),
                                    )
                                    .toList(),
                              );
                            },
                          ),
                        const SizedBox(height: 24),
                        Container(
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(16),
                          ),
                          child: LayoutBuilder(
                            builder: (context, area) {
                              final label = Text(
                                '共 ${tasks.length} 位${_status.label}患者',
                                style: MdisType.body,
                              );
                              final controls = Wrap(
                                spacing: 8,
                                crossAxisAlignment: WrapCrossAlignment.center,
                                children: [
                                  _pageButton(
                                    Icons.chevron_left,
                                    '上一页',
                                    _page > 0
                                        ? () => _goToPage(_page - 1)
                                        : null,
                                  ),
                                  for (var i = 0; i < pages; i++)
                                    Semantics(
                                      selected: _page == i,
                                      child: IconButton(
                                        tooltip: '第 ${i + 1} 页',
                                        onPressed: () => _goToPage(i),
                                        style: IconButton.styleFrom(
                                          backgroundColor: _page == i
                                              ? MdisColors.primary
                                              : MdisColors.background,
                                          foregroundColor: _page == i
                                              ? Colors.white
                                              : MdisColors.ink,
                                          minimumSize: const Size(48, 48),
                                        ),
                                        icon: Text(
                                          '${i + 1}',
                                          style: MdisType.body.copyWith(
                                            color: _page == i
                                                ? Colors.white
                                                : MdisColors.ink,
                                          ),
                                        ),
                                      ),
                                    ),
                                  _pageButton(
                                    Icons.chevron_right,
                                    '下一页',
                                    _page + 1 < pages
                                        ? () => _goToPage(_page + 1)
                                        : null,
                                  ),
                                ],
                              );
                              if (area.maxWidth < 500 ||
                                  MediaQuery.textScalerOf(context).scale(1) >
                                      1.3) {
                                return Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    label,
                                    const SizedBox(height: 12),
                                    controls,
                                  ],
                                );
                              }
                              return Row(
                                mainAxisAlignment:
                                    MainAxisAlignment.spaceBetween,
                                children: [label, controls],
                              );
                            },
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ],
        );
      },
    );
  }

  Widget _pageButton(IconData icon, String tooltip, VoidCallback? onPressed) =>
      IconButton(
        tooltip: tooltip,
        onPressed: onPressed,
        style: IconButton.styleFrom(
          backgroundColor: MdisColors.background,
          minimumSize: const Size(48, 48),
        ),
        icon: Icon(icon),
      );

  void _goToPage(int page) {
    setState(() => _page = page);
    _scroll.animateTo(
      0,
      duration: const Duration(milliseconds: 180),
      curve: Curves.easeOut,
    );
  }

  void _openPatient(DemoTask task) {
    final store = AppScope.of(context);
    final patient = store.patients.firstWhere(
      (p) => p.id == task.patientId && !p.archived,
    );
    openPage(context, PatientDetail(patientId: patient.id));
  }
}

String _dateRange() {
  final start = DateTime.now();
  final end = DateTime(start.year, start.month, start.day + 6);
  return '${start.month}月${start.day}日 - ${end.month}月${end.day}日';
}

class _PatientCard extends StatelessWidget {
  const _PatientCard({required this.task, required this.onOpen});
  final DemoTask task;
  final VoidCallback onOpen;

  @override
  Widget build(BuildContext context) => Material(
    color: Colors.white,
    borderRadius: BorderRadius.circular(16),
    child: Padding(
      padding: const EdgeInsets.all(18),
      child: LayoutBuilder(
        builder: (context, bounds) {
          final stacked =
              bounds.maxWidth < 350 ||
              MediaQuery.textScalerOf(context).scale(1) >= 1.5;
          final placeholder = Container(
            color: MdisColors.primarySoft,
            child: const Center(
              child: Icon(
                Icons.person_rounded,
                size: 48,
                color: MdisColors.avatar,
              ),
            ),
          );
          final avatar = ExcludeSemantics(
            child: ClipRRect(
              borderRadius: BorderRadius.circular(10),
              child: SizedBox(
                width: 88,
                height: 100,
                child: task.photoAsset == null
                    ? placeholder
                    : Image.asset(
                        task.photoAsset!,
                        fit: BoxFit.cover,
                        errorBuilder: (context, error, stack) => placeholder,
                      ),
              ),
            ),
          );
          final info = Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Wrap(
                spacing: 12,
                runSpacing: 4,
                crossAxisAlignment: WrapCrossAlignment.center,
                children: [
                  Text(task.bed, style: MdisType.sectionTitle),
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Container(width: 1, height: 20, color: MdisColors.line),
                      const SizedBox(width: 12),
                      Text(task.name, style: MdisType.sectionTitle),
                    ],
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                runSpacing: 4,
                crossAxisAlignment: WrapCrossAlignment.center,
                children: [
                  Text(
                    task.period ?? _dateRange(),
                    style: MdisType.body.copyWith(color: MdisColors.muted),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 2,
                    ),
                    decoration: BoxDecoration(
                      color: MdisColors.background,
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text(
                      '${task.days}天',
                      style: MdisType.caption.copyWith(color: MdisColors.muted),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 4),
              if (task.note != null) Text(task.note!, style: MdisType.caption),
              Text(
                '${task.medications}种药物',
                style: MdisType.body.copyWith(color: MdisColors.muted),
              ),
            ],
          );
          final action = IconButton.filled(
            key: ValueKey('open-${task.bed}'),
            tooltip: '${task.status.action} · ${task.bed} ${task.name}',
            onPressed: onOpen,
            style: IconButton.styleFrom(
              backgroundColor: MdisColors.primary,
              minimumSize: const Size(48, 48),
            ),
            icon: const Icon(Icons.chevron_right_rounded, size: 28),
          );
          if (stacked) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [avatar, action],
                ),
                const SizedBox(height: 16),
                info,
              ],
            );
          }
          return Row(
            children: [
              avatar,
              const SizedBox(width: 20),
              Expanded(child: info),
              const SizedBox(width: 12),
              action,
            ],
          );
        },
      ),
    ),
  );
}

/// The symbol and application name are separate UI elements.
class _BrandLogo extends StatelessWidget {
  const _BrandLogo({required this.compact});
  final bool compact;

  @override
  Widget build(BuildContext context) => Image.asset(
    'assets/branding/mdis-symbol.png',
    width: compact ? 32 : 40,
    height: compact ? 32 : 40,
    fit: BoxFit.contain,
    color: MdisColors.sidebar,
    colorBlendMode: BlendMode.modulate,
    filterQuality: FilterQuality.high,
    semanticLabel: compact ? 'Mdis' : null,
    excludeFromSemantics: !compact,
  );
}
