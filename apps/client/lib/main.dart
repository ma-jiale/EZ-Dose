import 'package:flutter/material.dart';

void main() => runApp(const MdisApp());

class MdisApp extends StatelessWidget {
  const MdisApp({super.key});

  @override
  Widget build(BuildContext context) {
    return const MaterialApp(
      title: 'Mdis',
      home: Scaffold(body: Center(child: Text('Mdis'))),
    );
  }
}
