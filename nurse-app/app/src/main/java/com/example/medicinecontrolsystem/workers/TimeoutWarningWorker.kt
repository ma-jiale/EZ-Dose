package com.example.medicinecontrolsystem.workers

import android.app.Notification
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import androidx.core.app.ActivityCompat
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.work.Worker
import androidx.work.WorkerParameters
import com.example.medicinecontrolsystem.R
import com.example.medicinecontrolsystem.data.TimeSlot
import com.example.medicinecontrolsystem.ui.TimeoutWarningActivity
import com.example.medicinecontrolsystem.respository.AppRepository
import kotlinx.coroutines.runBlocking

class TimeoutWarningWorker(
    context: Context,
    params: WorkerParameters
) : Worker(context, params) {

    override fun doWork(): Result {

        // --- ⭐ 1. 智能判断逻辑 ⭐ ---

        // a. 根据当前时间，获取当前处于哪个时间段
        //    (我们假设Worker被唤醒的时间和警告时间点非常接近)
        val currentTimeSlot = AppRepository.findCurrentTimeSlot()

        // 如果当前不属于任何一个时间段，说明任务可能已经过时或出错了，直接成功返回
        if (currentTimeSlot == null) {
            return Result.success()
        }

        // b. 调用Repository，检查这个时间段是否【所有】任务都已完成
        val pendingTaskCount = runBlocking {
            AppRepository.getPendingTaskCountForTimeSlot(applicationContext, currentTimeSlot)
        }

        // c. 如果待办任务数量为0（即全部完成），则静默地成功返回，不打扰用户
        if (pendingTaskCount == 0) {
            return Result.success()
        }

        // --- 2. 如果还有未完成的任务，才执行后续的警告逻辑 ---
        // 创建一个意图(Intent)，用于启动我们的全屏警告Activity
        val fullScreenIntent = Intent(applicationContext, TimeoutWarningActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }

        // 创建一个 PendingIntent。这是一个“待定”的意图，我们把它交给系统，
        // 系统会在将来某个时间点（比如我们发出通知时）以我们App的权限来执行它。
        val fullScreenPendingIntent = PendingIntent.getActivity(
            applicationContext,
            123,
            fullScreenIntent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        // 复用之前的通知渠道
        val channelId = NotificationWorker.CHANNEL_ID

        // 构建通知
        val notification = NotificationCompat.Builder(applicationContext, channelId)
            .setSmallIcon(R.drawable.icon_record) // 使用警告图标
            .setContentTitle("任务即将超时警告！")
            .setContentText("您的送药任务即将超时，请立即处理！")
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setCategory(NotificationCompat.CATEGORY_ALARM) // 把它归类为“闹钟”，提升优先级
            .setDefaults(Notification.DEFAULT_ALL) // 默认声音和震动

            // ⭐⭐⭐ 核心！将 PendingIntent 设置为全屏意图 ⭐⭐⭐
            .setFullScreenIntent(fullScreenPendingIntent, true)

            .setAutoCancel(true)
            .build()

        // 显示通知
        val notificationManager = NotificationManagerCompat.from(applicationContext)

// 检查通知权限
        if (ActivityCompat.checkSelfPermission(
                applicationContext, // <-- 明确地传入 context
                android.Manifest.permission.POST_NOTIFICATIONS
            ) != PackageManager.PERMISSION_GRANTED
        ) {
            return Result.failure()
        }

// 使用一个不同的ID，避免覆盖掉准时提醒的通知
        notificationManager.notify(NotificationWorker.NOTIFICATION_ID + 1, notification)



        return Result.success()
    }
}