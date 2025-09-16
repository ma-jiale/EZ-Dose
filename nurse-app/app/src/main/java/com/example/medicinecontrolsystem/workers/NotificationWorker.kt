package com.example.medicinecontrolsystem.workers

import android.Manifest
import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import androidx.core.app.ActivityCompat
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.work.Worker
import androidx.work.WorkerParameters
import com.example.medicinecontrolsystem.R
import androidx.work.ForegroundInfo // <-- 需要导入


// 定义 NotificationWorker 类，它继承自 Worker。
// 它的构造函数接收一个 Context 和一个 WorkerParameters 对象，这是WorkManager的要求。
class NotificationWorker(
    private val context: Context,
    workerParams: WorkerParameters
) : Worker(context, workerParams) {

    companion object {
        const val NOTIFICATION_ID = 101  // 给这个通知一个唯一的ID，方便后续更新或取消它
        const val CHANNEL_ID = "MedicineTaskChannel"  // 给通知渠道一个唯一的ID

        // 定义两个Key，用于从传入的数据中存取时间段名称和病人数量
        const val KEY_TIME_SLOT_NAME = "key_time_slot_name"
        const val KEY_PATIENT_COUNT = "key_patient_count"

        const val KEY_CUSTOM_TITLE = "key_custom_title"
        const val KEY_CUSTOM_CONTENT = "key_custom_content"
        //前台服务ID
        const val FOREGROUND_NOTIFICATION_ID = 100
    }

    // 在 doWork() 之前，当Worker刚被创建时，我们会先把自己设为前台服务
    override fun getForegroundInfo(): ForegroundInfo {
        return ForegroundInfo(
            FOREGROUND_NOTIFICATION_ID,
            createForegroundNotification()
        )
    }

    // ⭐ 创建一个专门用于“前台服务正在运行”的通知
    // 这个通知的目的是告诉用户，我们的App正在后台等待任务，而不是真正的任务提醒
    private fun createForegroundNotification(): Notification {
        val channelId = CHANNEL_ID // 我们可以复用同一个渠道
        createNotificationChannel() // 确保渠道存在

        return NotificationCompat.Builder(context, channelId)
            .setContentTitle("用药提醒服务正在运行")
            .setContentText("正在等待下一个送药时间...")
            .setSmallIcon(R.drawable.icon_bell) // 使用同一个图标
            .setOngoing(true) // 设置为“正在进行”，用户不能轻易划掉
            .build()
    }
    /**
     * 这是Worker的核心方法，所有的后台任务逻辑都写在这里。
     * 当WorkManager触发这个任务时，doWork()方法会在一个后台线程中被执行。
     * @return 返回一个Result，告诉WorkManager任务的执行结果（成功、失败或重试）。
     */
    override fun doWork(): Result {

        val customTitle = inputData.getString(KEY_CUSTOM_TITLE)
        val customContent = inputData.getString(KEY_CUSTOM_CONTENT)

        val notificationTitle: String
        val notificationContent: String

        // ⭐ 2. 智能判断使用哪套文本
        if (customTitle != null && customContent != null) {
            // 如果自定义的内容存在，就使用它们
            notificationTitle = customTitle
            notificationContent = customContent
        } else {
            // 1. 从输入数据中获取通知内容
            val timeSlotName = inputData.getString(KEY_TIME_SLOT_NAME) ?: "新的"
            val patientCount = inputData.getInt(KEY_PATIENT_COUNT, 0)
            notificationTitle = "送药任务提醒"
            notificationContent = "${timeSlotName}的送药任务已开始，您有 ${patientCount} 位老人需要给药。"
        }
        // 2. 创建通知渠道（仅在 Android 8.0 及以上需要）
        createNotificationChannel()

        // 3. 构建通知
        val notification = NotificationCompat.Builder(context, CHANNEL_ID)
            .setSmallIcon(R.drawable.icon_bell)
            .setContentTitle("送药任务提醒")
            .setContentTitle(notificationTitle)
            .setContentText(notificationContent)
            .setPriority(NotificationCompat.PRIORITY_HIGH) // 设置高优先级，使其可能以横幅形式弹出
            .setAutoCancel(true) // 用户点击后自动消失
            .setDefaults(Notification.DEFAULT_ALL)
            .build()

        // 4. 显示通知
        with(NotificationManagerCompat.from(context)) {
            // 检查通知权限（Android 13+ 需要）
            if (ActivityCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) {
                // 如果没有权限，任务虽然执行了但无法显示通知。
                // 真实应用中需要引导用户去开启权限。
                return Result.failure()
            }
            notify(NOTIFICATION_ID, notification)
        }

        // 5. 表示任务成功完成
        return Result.success()
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val name = "送药任务提醒"
            val descriptionText = "用于提醒阿姨按时给药"
            val importance = NotificationManager.IMPORTANCE_HIGH
            val channel = NotificationChannel(CHANNEL_ID, name, importance).apply {
                description = descriptionText
                lockscreenVisibility = Notification.VISIBILITY_PUBLIC
            }
            // 注册渠道
            val notificationManager: NotificationManager =
                context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            notificationManager.createNotificationChannel(channel)
        }
    }
}