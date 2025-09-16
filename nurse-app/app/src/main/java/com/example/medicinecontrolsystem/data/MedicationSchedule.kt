package com.example.medicinecontrolsystem.data

import java.time.chrono.JapaneseEra.values
import java.util.Calendar

data class TimeSlot(
    val name: String, // 枚举的名字现在变成了一个普通的字段
    val displayName: String,
    val startHour: Int,
    val endHour: Int,
    val startMinute: Int = 0
) {
//    BEFORE_BREAKFAST("早饭前",14,15 ),
//    AFTER_BREAKFAST("早饭后",7,8),
//    BEFORE_LUNCH("午饭前",11,12),
//    AFTER_LUNCH("午饭后",12,13),
//    BEFORE_DINNER("晚饭前",17,18),
//    AFTER_DINNER("晚饭后",18,19);

    fun getFormattedStartTime(): String {
        return "%02d:%02d".format(startHour, startMinute)
    }
}

//    companion object {
//        /**
//         * 一个辅助函数，可以根据当前的小时，找出它属于哪个TimeSlot。
//         * @param hour 当前的小时 (0-23)。
//         * @return 匹配的TimeSlot，如果没有匹配则返回null。
//         */
//        fun fromHour(hour: Int): TimeSlot? {
//            // values() 是一个内置函数，可以获取所有枚举常量的列表
//            return values().find { hour in it.startHour until it.endHour }
//        }
//        fun fromCurrentTime(): TimeSlot? {
//            // 1. 获取当前的日历实例
//            val now = Calendar.getInstance()
//            val currentHour = now.get(Calendar.HOUR_OF_DAY)
//            val currentMinute = now.get(Calendar.MINUTE)
//
//            // 2. 将当前时间转换为一个总分钟数，方便比较
//            val currentTimeInMinutes = currentHour * 60 + currentMinute
//
//            // 3. 遍历所有TimeSlot，找到第一个匹配的
//            return values().find { timeSlot ->
//                // a. 计算时间段的开始总分钟数
//                val startTimeInMinutes = timeSlot.startHour * 60 + timeSlot.startMinute
//                // b. 计算时间段的结束总分钟数 (我们假设结束时间是整点)
//                val endTimeInMinutes = timeSlot.endHour * 60
//
//                // c. 检查当前的总分钟数是否落在这个区间内
//                currentTimeInMinutes >= startTimeInMinutes && currentTimeInMinutes < endTimeInMinutes
//            }
//        }
//    }
//}

data class PatientScheduleLink(
    val patientId: Int,
    val timeSlot: TimeSlot
)

// “服药计划”数据源
//val initialSchedules = listOf(
//    // 病人1 (李栋梁) 需要在早、中、晚饭前服药
//    PatientScheduleLink(1, TimeSlot.BEFORE_BREAKFAST),
//    PatientScheduleLink(1, TimeSlot.BEFORE_LUNCH),
//    PatientScheduleLink(1, TimeSlot.BEFORE_DINNER),
//
//    // 病人2 (王小美) 需要在早饭后和晚饭后服药
//    PatientScheduleLink(2, TimeSlot.AFTER_BREAKFAST),
//    PatientScheduleLink(2, TimeSlot.AFTER_DINNER),
//
//    // 病人3(张桂芳) 需要在午饭后和睡前(晚饭后)服药
//    PatientScheduleLink(3, TimeSlot.AFTER_LUNCH),
//    PatientScheduleLink(3, TimeSlot.AFTER_DINNER),
//
//    // ... 为其他病人添加服药计划
//    PatientScheduleLink(6, TimeSlot.BEFORE_BREAKFAST)
//)