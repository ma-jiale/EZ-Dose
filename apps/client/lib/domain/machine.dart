import 'dart:async';

import 'package:flutter/foundation.dart';

enum MachineFault { none, count, connection, uncertain }

/// Local UI transport. It never opens a serial port, Bluetooth, or a network.
abstract class MachineService extends ChangeNotifier {
  bool get connected;
  bool get running;
  bool get paused;
  String get name;
  void connect();
  void disconnect();
  void start(
    int count,
    void Function(int) progress,
    VoidCallback done,
    void Function(MachineFault) fault,
  );
  void pause();
  void resume();
  void stop();
}

class MockMachine extends MachineService {
  bool _connected = false;
  bool _running = false;
  bool _paused = false;
  Timer? _timer;
  MachineFault nextFault = MachineFault.none;
  @override
  bool get connected => _connected;
  @override
  bool get running => _running;
  @override
  bool get paused => _paused;
  @override
  String get name => '本地测试设备';
  @override
  void connect() {
    _connected = true;
    notifyListeners();
  }

  @override
  void disconnect() {
    stop();
    _connected = false;
    notifyListeners();
  }

  @override
  void start(
    int count,
    void Function(int) progress,
    VoidCallback done,
    void Function(MachineFault) fault,
  ) {
    if (!_connected || _running) throw StateError('设备未就绪');
    _running = true;
    _paused = false;
    var current = 0;
    final plannedFault = nextFault;
    nextFault = MachineFault.none;
    _timer = Timer.periodic(const Duration(milliseconds: 180), (_) {
      if (_paused) return;
      current++;
      progress(current);
      if (plannedFault != MachineFault.none && current >= (count / 2).ceil()) {
        stop();
        if (plannedFault == MachineFault.connection) _connected = false;
        fault(plannedFault);
        notifyListeners();
      } else if (current >= count) {
        stop();
        done();
      }
    });
    notifyListeners();
  }

  @override
  void pause() {
    if (_running) {
      _paused = true;
      notifyListeners();
    }
  }

  @override
  void resume() {
    if (_running && _connected) {
      _paused = false;
      notifyListeners();
    }
  }

  @override
  void stop() {
    _timer?.cancel();
    _timer = null;
    _running = false;
    _paused = false;
    notifyListeners();
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }
}
