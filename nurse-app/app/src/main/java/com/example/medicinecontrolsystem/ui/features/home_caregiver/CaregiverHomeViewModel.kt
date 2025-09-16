package com.example.medicinecontrolsystem.ui.features.home_caregiver

import android.app.Application
import android.util.Log
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.work.OneTimeWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.workDataOf
import com.example.medicinecontrolsystem.data.data_Patient

import com.example.medicinecontrolsystem.data.TimeSlot
import com.example.medicinecontrolsystem.data.initialCaregivers
import com.example.medicinecontrolsystem.data.initialAunties

import com.example.medicinecontrolsystem.respository.AppRepository
import com.example.medicinecontrolsystem.ui.features.task_monitor_caregiver.PatientTask
import com.example.medicinecontrolsystem.workers.NotificationWorker
import kotlinx.coroutines.CoroutineExceptionHandler
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale
import kotlin.collections.forEach

/**
 * HomeScreen 的统一UI状态数据类。
 * UI层只需要订阅这一个对象，就能获取所有需要展示的数据。
 */
data class CaregiverUiState(

    val caregiverName: String = "护工",
    val overdueTasks: List<PatientTask> = emptyList(), // 超时未完成的任务列表
    val upcomingTimeoutTasks: List<PatientTask> = emptyList(), // 即将超时的任务
    val inProgressTaskCount: Int = 0, // 正在进行中的任务总数
    val completedInProgressTaskCount: Int = 0, // 正在进行中已完成的任务数
    val patientsForDisplay: List<data_Patient> = emptyList(),
    val patientStatesWithTime: Map<Int, Pair<String,String?>> = emptyMap(),
    val totalTasksToday: Int = 0,
    val completedTasksToday: Int = 0,
    val isLoading: Boolean = true,

    val isUpcomingTimeoutNotified: Boolean = false,

    val timePhrase: String = "上午",
    val formattedTime: String = "00:00",

)

/**
 * HomeViewModel - 主页的唯一 ViewModel。
 * 它整合了之前 Time, Task, State, TimeBar 四个ViewModel的核心职责。
 */
class CaregiverViewModel(application: Application) : AndroidViewModel(application){
//    private val csvManager = CsvDataManager(application.applicationContext)

    private val exceptionHandler = CoroutineExceptionHandler { _, throwable ->
        // 当任何一个 viewModelScope.launch 失败时，这里会被调用
        Log.e("HomeViewModel", "Coroutine Exception: ${throwable.message}", throwable)
        // 你可以在这里更新UI状态，显示一个错误信息给用户
        _uiState.update { it.copy(isLoading = false) } // 比如出错时停止加载动画
    }

    private val _uiState = MutableStateFlow(CaregiverUiState())
    val uiState = _uiState.asStateFlow()

    private var currentCaregiverId: Int = -1

    init {
        startClockUpdater()
    }

    fun loadDataForCaregiver(caregiverId: Int) {
        if (this.currentCaregiverId == caregiverId) return // 防止重复加载
        this.currentCaregiverId = caregiverId
    }

    private fun refreshData() {
        viewModelScope.launch {
            if (currentCaregiverId == -1) return@launch

            _uiState.update { it.copy(isLoading = true) }

            val caregiverName = AppRepository.getCaregiverName(currentCaregiverId)
            val myAuntieIds = AppRepository.getAuntieIdsForCaregiver(currentCaregiverId) // 假设新增了此方法
            val allMyPatients = AppRepository.getPatientsForAunties(myAuntieIds) // 假设新增了此方法
            val myPatientIds = allMyPatients.map { it.patientId }.toSet()
            val allMyTasksToday = AppRepository.getSchedulesForPatients(myPatientIds) // 假设新增了此方法

            // --- 2. 从服务器获取【任务状态】 ---
            val tasksResult = AppRepository.getTasksForDate(Date())
            if (tasksResult.isFailure) {
                Log.e("CaregiverVM", "获取任务状态失败", tasksResult.exceptionOrNull())
                return@launch
            }
            val allTodayStates = tasksResult.getOrThrow()

            // --- 2. 遍历所有任务，识别“异常事件” ---
            val overdueTasksList = mutableListOf<PatientTask>()
            val upcomingTimeoutTasksList = mutableListOf<PatientTask>()
            var inProgressCount = 0
            var completedInProgressCount = 0

            allMyTasksToday.forEach { scheduleLink ->
                val taskKey = Pair(scheduleLink.patientId, scheduleLink.timeSlot)
                val status = allTodayStates[taskKey]?.status ?: "待服药"
                val timeSlot = scheduleLink.timeSlot
                val patientData = allMyPatients.find { it.patientId == scheduleLink.patientId } ?: return@forEach

                val patientTask = PatientTask(patientData, status, timeSlot, patientData.patientBarcode)
                val taskEndTime = Calendar.getInstance().apply { set(Calendar.HOUR_OF_DAY, timeSlot.endHour); set(Calendar.MINUTE, 0) }
                val now = Calendar.getInstance()

                if (now.after(taskEndTime) && status == "待服药") {
                    overdueTasksList.add(patientTask)
                } else if (status == "待服药") { // 如果未超时且未完成
                    val minutesUntilEnd = (taskEndTime.timeInMillis - now.timeInMillis) / 1000 / 60
                    if (minutesUntilEnd < 15) { // 假设15分钟为即将超时
                        upcomingTimeoutTasksList.add(patientTask)
                    }
                }

                if (AppRepository.findCurrentTimeSlot() == timeSlot) {
                    inProgressCount++
                    if (status == "已服药") {
                        completedInProgressCount++
                    }
                }
            }
//                if (now.after(taskEndTime) && status == "待服药") {
//                    val patientData = allMyPatients.find { it.patientId == scheduleLink.patientId }
//
//                    // 2. 使用一个 if-not-null 判断，来确保我们找到了病人
//                    if (patientData != null) {
//                        // 3. 只有在确认 patientData 不为空后，才执行添加操作
//                        overdueTasksList.add(
//                            PatientTask(patientData, status, timeSlot, patientData.patientBarcode)
//                        )
//                    }
//                } else {
//                    val minutesUntilEnd = (taskEndTime.timeInMillis - now.timeInMillis) / 1000 / 60
//                    if (minutesUntilEnd < 16 && status == "待服药") { // 加上 status 判断更严谨
//                        // ⭐⭐⭐ 核心修复：在这里也使用 allMyPatients ⭐⭐⭐
//                        val patientData =
//                            allMyPatients.find { it.patientId == scheduleLink.patientId }
//                        if (patientData != null) {
//                            upcomingTimeoutTasksList.add(
//                                PatientTask(
//                                    patientData,
//                                    status,
//                                    timeSlot,
//                                    patientData.patientBarcode
//                                )
//                            )
//                        }
//                    }
//
//                    // 检查是否是“正在进行中”的任务，并统计其完成情况
//                    if (currentRunningTimeSlot == timeSlot) {
//                        inProgressCount++
//                        if (status == "已服药") {
//                            completedInProgressCount++
//                        }
//                    }
//                }

                // --- 3. 计算总览统计数据 ---
                val totalTasksTodayCount = allMyTasksToday.size
                val completedTasksTodayCount = allMyTasksToday.count {scheduleLink ->
                    // a. 根据当前的计划，构建出要去 Map 里查找的 Key
                    val taskKey = Pair(scheduleLink.patientId, scheduleLink.timeSlot)

                    // b. 去 Map 里查找这个任务的状态，并判断是否为“已服药”
                    allTodayStates[taskKey]?.status == "已服药"
                }

                // --- 4. 更新UI State ---
                _uiState.update { currentState ->
                    val hadNoUpcomingTasks = currentState.upcomingTimeoutTasks.isEmpty()
                    val hasUpcomingTasksNow = upcomingTimeoutTasksList.isNotEmpty()
                    val shouldNotify = hadNoUpcomingTasks && hasUpcomingTasksNow && !currentState.isUpcomingTimeoutNotified
                    // a. 判断是否需要发送通知
//                    val shouldNotify =
//                        // i.  之前没有“即将超时”的任务
//                        currentState.upcomingTimeoutTasks.isEmpty() &&
//                                // ii. 现在有了“即将超时”的任务
//                                upcomingTimeoutTasksList.isNotEmpty() &&
//                                // iii. 并且我们之前没有为这批任务发送过通知
//                                !currentState.isUpcomingTimeoutNotified

                    // b. 如果需要发送，就去调度任务
                    if (shouldNotify) {
                        scheduleUpcomingTimeoutNotification(upcomingTimeoutTasksList.size)
                    }

                    currentState.copy(
                        caregiverName = caregiverName,
                        overdueTasks = overdueTasksList,
                        upcomingTimeoutTasks = upcomingTimeoutTasksList,
                        inProgressTaskCount = inProgressCount,
                        completedInProgressTaskCount = completedInProgressCount,
                        totalTasksToday = totalTasksTodayCount,
                        completedTasksToday = completedTasksTodayCount,
                        isUpcomingTimeoutNotified = if (shouldNotify) true else upcomingTimeoutTasksList.isNotEmpty(),
                        isLoading = false
                    )
                }
            }
        }

//     1. 整合自 TimeViewModel
    private fun startClockUpdater() {
        viewModelScope.launch {
            AppRepository.isInitialized.first { it }
            // b. 开始收听“后台轮询”发出的广播信号
            //    这使得你在Web后台修改数据后，App也能自动刷新
            launch { // 启动一个新的子协程来专门收听广播
                AppRepository.dataRefreshed.collect {
                    Log.d("CaregiverHomeViewModel", "接收到全局数据更新信号，正在刷新...")
                    if (currentCaregiverId != -1) {
                        refreshData()
                    }
                }
            }
            while (true) {
                val calendar = Calendar.getInstance()
                val hour = calendar.get(Calendar.HOUR_OF_DAY)

                val newTimePhrase = if (hour in 0..11) "上午" else "下午"
                val newFormattedTime = SimpleDateFormat("h:mm", Locale.getDefault()).format(Date())

                // 使用 .update 更新状态，保证线程安全
                _uiState.update { currentState ->
                    currentState.copy(
                        timePhrase = newTimePhrase,
                        formattedTime = newFormattedTime
                    )
                }
                refreshData()
                delay(1000 * 1)
            }
        }
    }
    private fun scheduleUpcomingTimeoutNotification(taskCount: Int) {
        val workManager = WorkManager.getInstance(getApplication())

        // 准备要传递给 Worker 的数据
        val inputData = workDataOf(
            // 这里可以定制护工端收到的通知内容
            NotificationWorker.KEY_CUSTOM_TITLE to "送药任务即将超时",
            NotificationWorker.KEY_CUSTOM_CONTENT to "您有送药任务即将超时，请及时处理！"
        )

        // 创建一个【立即执行】的一次性任务请求
        val notificationWorkRequest = OneTimeWorkRequestBuilder<NotificationWorker>()
            .setInputData(inputData)
            .build()

        // 将任务加入队列
        workManager.enqueue(notificationWorkRequest)
    }

}