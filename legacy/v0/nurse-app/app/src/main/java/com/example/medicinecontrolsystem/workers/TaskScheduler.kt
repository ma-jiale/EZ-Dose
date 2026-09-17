package com.example.medicinecontrolsystem.workers

import android.content.Context
import android.util.Log
import androidx.work.*
import com.example.medicinecontrolsystem.data.TimeSlot
import com.example.medicinecontrolsystem.respository.AppRepository
import java.util.Calendar
import java.util.concurrent.TimeUnit

object TaskScheduler {

    private const val TAG = "TaskScheduler"

    fun scheduleAllTasks(context: Context) {
        val workManager = WorkManager.getInstance(context)
        val allTimeSlots = AppRepository.getAllTimeSlots()

        if (allTimeSlots.isEmpty()) {
            Log.w(TAG, "时间段列表为空，无法调度任何提醒任务。")
            return
        }
        Log.d(TAG, "===== 开始调度 ${allTimeSlots.size} 个时间段的任务 =====")

        allTimeSlots.forEach { timeSlot ->
            Log.d(TAG, "--- 正在处理时间段: ${timeSlot.name} (${timeSlot.displayName}) ---")

            // --- 1. ⭐ 公共计算：在循环开始时，先获取一次当前时间 ---
            val now = Calendar.getInstance()

            // --- 2. ⭐ 处理“准时开始”的通知 ---
            val patientCount = AppRepository.getPatientCountForTimeSlot(context, timeSlot)
            Log.d(TAG, "[准时提醒] 计算出病人数量为: $patientCount")
            if (patientCount > 0) {
                val uniqueWorkName = "notification_task_${timeSlot.name}"
                val inputData = workDataOf(
                    NotificationWorker.KEY_CUSTOM_TITLE to "送药任务提醒",
                    NotificationWorker.KEY_CUSTOM_CONTENT to "${timeSlot.displayName}的送药任务已开始，您有 ${patientCount} 位老人需要给药。"
                )

                // a. 计算目标时间
                val targetTime = Calendar.getInstance().apply {
                    set(Calendar.HOUR_OF_DAY, timeSlot.startHour)
                    set(Calendar.MINUTE, timeSlot.startMinute)
                    set(Calendar.SECOND, 0)
                }

                // b. 检查是否已过时
                if (targetTime.before(now)) {
                    targetTime.add(Calendar.DAY_OF_YEAR, 1)
                }
                val initialDelay = targetTime.timeInMillis - now.timeInMillis
                Log.i(TAG, "[准时提醒] 任务'${timeSlot.name}' 将在 ${initialDelay / 1000 / 60} 分钟后首次执行。")
                // c. 构建并调度任务
                val periodicWorkRequest = PeriodicWorkRequestBuilder<NotificationWorker>(1, TimeUnit.DAYS)
                    .setInitialDelay(initialDelay, TimeUnit.MILLISECONDS)
                    .setInputData(inputData)
                    .build()

                workManager.enqueueUniquePeriodicWork(
                    uniqueWorkName,
                    ExistingPeriodicWorkPolicy.REPLACE,
                    periodicWorkRequest
                )
            } // <-- if (patientCount > 0) 在这里正确地结束了

            // --- 3. ⭐ 处理“超时警告”的通知 ---
            //    这部分逻辑现在位于 if 的外部，总会被执行
            val uniqueTimeoutWorkName = "timeout_warning_task_${timeSlot.name}"

            val warningTargetTime = Calendar.getInstance().apply {
                set(Calendar.HOUR_OF_DAY, timeSlot.endHour)
                set(Calendar.MINUTE, 0)
                set(Calendar.SECOND, 0)
                add(Calendar.MINUTE, -15)
            }

            if (warningTargetTime.before(now)) {
                warningTargetTime.add(Calendar.DAY_OF_YEAR, 1)
            }
            val timeoutInitialDelay = warningTargetTime.timeInMillis - now.timeInMillis
            Log.w(TAG, "[超时警告] 任务'${timeSlot.name}' 将在 ${timeoutInitialDelay / 1000 / 60} 分钟后首次执行。")
            val timeoutWorkRequest = PeriodicWorkRequestBuilder<TimeoutWarningWorker>(1, TimeUnit.DAYS)
                .setInitialDelay(timeoutInitialDelay, TimeUnit.MILLISECONDS)
                .build()

            workManager.enqueueUniquePeriodicWork(
                uniqueTimeoutWorkName,
                ExistingPeriodicWorkPolicy.REPLACE,
                timeoutWorkRequest
            )
        }
        Log.i(TAG, "===== 所有任务调度完毕 =====")
    }
}