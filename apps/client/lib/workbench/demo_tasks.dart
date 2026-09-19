// Entirely fictional development fixtures; never clinical records.
// These are homepage task categories, not dispensing-session state definitions.
enum TaskStatus {
  pending('待摆药', '查看患者'),
  completed('已完成', '查看结果');

  const TaskStatus(this.label, this.action);
  final String label;
  final String action;
}

class DemoTask {
  const DemoTask(
    this.bed,
    this.name,
    this.medications,
    this.status, {
    this.photoAsset,
    this.patientId,
    this.period,
    this.days = 7,
    this.note,
  });
  final String bed;
  final String name;
  final int medications;
  final TaskStatus status;
  final String? photoAsset;
  final String? patientId, period, note;
  final int days;
}

const demoTasks = [
  DemoTask('B-302', '王桂芳', 5, TaskStatus.pending),
  DemoTask('A-205', '张秀英', 4, TaskStatus.pending),
  DemoTask('B-308', '陈淑珍', 6, TaskStatus.pending),
  DemoTask('A-112', '赵玉兰', 3, TaskStatus.pending),
  DemoTask('C-206', '刘桂英', 4, TaskStatus.pending),
  DemoTask('B-215', '周秀兰', 5, TaskStatus.pending),
  DemoTask('B-301', '徐文华', 5, TaskStatus.pending),
  DemoTask('A-118', '孙月娥', 4, TaskStatus.pending),
  DemoTask('C-203', '高志明', 3, TaskStatus.pending),
  DemoTask('A-101', '罗惠芳', 3, TaskStatus.completed),
  DemoTask('A-103', '梁国强', 4, TaskStatus.completed),
  DemoTask('B-206', '宋玉兰', 2, TaskStatus.completed),
];
