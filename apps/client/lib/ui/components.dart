import 'package:flutter/material.dart';

import '../theme.dart';

void openPage(BuildContext context, Widget page) =>
    Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => page));
void message(BuildContext context, String text) => ScaffoldMessenger.of(context)
  ..hideCurrentSnackBar()
  ..showSnackBar(SnackBar(content: Text(text)));
Future<bool> confirm(
  BuildContext context,
  String title,
  String text, {
  String action = '确认',
}) async =>
    await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        scrollable: true,
        title: Text(title),
        content: SizedBox(width: 420, child: Text(text)),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('取消'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text(action),
          ),
        ],
      ),
    ) ??
    false;

class PageFrame extends StatelessWidget {
  const PageFrame({
    super.key,
    required this.title,
    this.subtitle,
    required this.child,
    this.actions = const [],
    this.embedded = false,
  });
  final String title;
  final String? subtitle;
  final Widget child;
  final List<Widget> actions;
  final bool embedded;
  @override
  Widget build(BuildContext context) {
    final body = SafeArea(
      child: LayoutBuilder(
        builder: (context, bounds) => SingleChildScrollView(
          padding: EdgeInsets.all(bounds.maxWidth < 600 ? 20 : 36),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 1280),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Wrap(
                    spacing: 24,
                    runSpacing: 16,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      Text(title, style: MdisType.pageTitle),
                      ...actions,
                    ],
                  ),
                  if (subtitle != null) ...[
                    const SizedBox(height: 8),
                    Text(
                      subtitle!,
                      style: MdisType.body.copyWith(color: MdisColors.muted),
                    ),
                  ],
                  const SizedBox(height: 28),
                  child,
                ],
              ),
            ),
          ),
        ),
      ),
    );
    return embedded
        ? body
        : Scaffold(
            appBar: AppBar(
              title: const Text('Mdis'),
              backgroundColor: MdisColors.background,
              surfaceTintColor: Colors.transparent,
            ),
            body: body,
          );
  }
}

class Section extends StatelessWidget {
  const Section({super.key, this.title, required this.child});
  final String? title;
  final Widget child;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 20),
    child: Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(18),
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: SizedBox(
          width: double.infinity,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (title != null) ...[
                Text(title!, style: MdisType.sectionTitle),
                const SizedBox(height: 20),
              ],
              child,
            ],
          ),
        ),
      ),
    ),
  );
}

class ResponsiveColumns extends StatelessWidget {
  const ResponsiveColumns({super.key, required this.main, required this.side});
  final Widget main, side;
  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, bounds) {
      if (bounds.maxWidth < 900 ||
          MediaQuery.textScalerOf(context).scale(1) > 1.4) {
        return Column(children: [main, side]);
      }
      return Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(flex: 7, child: main),
          const SizedBox(width: 24),
          Expanded(flex: 3, child: side),
        ],
      );
    },
  );
}

class EmptyState extends StatelessWidget {
  const EmptyState(this.text, {super.key, this.action});
  final String text;
  final Widget? action;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.all(32),
    child: Center(
      child: Column(
        children: [
          const Icon(Icons.inbox_outlined, size: 36, color: MdisColors.muted),
          const SizedBox(height: 16),
          Text(text),
          if (action != null) ...[const SizedBox(height: 16), action!],
        ],
      ),
    ),
  );
}

class StatusPill extends StatelessWidget {
  const StatusPill(this.text, {super.key});
  final String text;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
    decoration: BoxDecoration(
      color: MdisColors.primarySoft,
      borderRadius: BorderRadius.circular(20),
    ),
    child: Text(text, style: MdisType.label),
  );
}

class InfoLine extends StatelessWidget {
  const InfoLine(this.label, this.value, {super.key});
  final String label, value;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 8),
    child: Wrap(
      spacing: 16,
      runSpacing: 4,
      children: [
        Text(label, style: MdisType.body.copyWith(color: MdisColors.muted)),
        Text(value, style: MdisType.bodyStrong),
      ],
    ),
  );
}

class FormFieldBox extends StatelessWidget {
  const FormFieldBox(
    this.label,
    this.controller, {
    super.key,
    this.validator,
    this.obscure = false,
    this.lines = 1,
    this.keyboardType,
    this.onChanged,
  });
  final String label;
  final TextEditingController controller;
  final String? Function(String?)? validator;
  final bool obscure;
  final int lines;
  final TextInputType? keyboardType;
  final ValueChanged<String>? onChanged;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 18),
    child: TextFormField(
      controller: controller,
      validator: validator,
      obscureText: obscure,
      maxLines: lines,
      keyboardType: keyboardType,
      onChanged: onChanged,
      style: MdisType.body,
      decoration: InputDecoration(
        labelText: label,
        labelStyle: MdisType.body,
        filled: true,
        fillColor: MdisColors.background,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide.none,
        ),
      ),
    ),
  );
}

String? requiredText(String? value) =>
    value == null || value.trim().isEmpty ? '请填写此项' : null;

class ActionRow extends StatelessWidget {
  const ActionRow({super.key, required this.children});
  final List<Widget> children;
  @override
  Widget build(BuildContext context) => Wrap(
    spacing: 12,
    runSpacing: 12,
    crossAxisAlignment: WrapCrossAlignment.center,
    children: children,
  );
}
