package com.example.medicinecontrolsystem.ui.features.home_auntie

import android.app.Application
import android.util.Log
import androidx.compose.runtime.Recomposer
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.medicinecontrolsystem.data.data_Patient
import com.example.medicinecontrolsystem.data.SessionManager
import com.example.medicinecontrolsystem.data.TimeSlot
import com.example.medicinecontrolsystem.network.TaskState
import com.example.medicinecontrolsystem.respository.AppRepository
import com.example.medicinecontrolsystem.ui.features.task_monitor_caregiver.PatientTask
import com.google.android.gms.tasks.Task
import kotlinx.coroutines.CoroutineExceptionHandler
import kotlinx.coroutines.Job
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

/**
 * HomeScreen 的统一UI状态数据类。
 * UI层只需要订阅这一个对象，就能获取所有需要展示的数据。
 */

data class HomeUiState(
    //阿姨的名字、时间描述
    val auntieName:String = "",
    val timePhrase: String = "上午",
    val formattedTime: String = "00:00",
    //当前时间段已完成的任务数和总任务数
    val completedTasksToday: Int = 0,
    val totalTasksToday: Int = 0,

    //专门用于顶部进度卡片
    val completedTasksInProgress: Int = 0,
    val totalTasksInProgress: Int = 0,
    //存储所有任务的状态。Key是(病人ID, 时间段枚举)的复合键，Value是状态字符串("待服药"/"已服药")
    val patientTakingStates: Map<Pair<Int, TimeSlot>, TaskState> = emptyMap(),

//    val selectedTimeBarIndex: Int = 0,

    val overdueTaskGroups: Map<TimeSlot, List<PatientTask>> = emptyMap(),
    val inProgressTasks: List<PatientTask> = emptyList(), // 用于存放正在进行的任务
    //是否正在加载数据
    val isLoading: Boolean = true,
    //让UI层知道当前是哪个时间段的任务
    val currentTimeSlot: TimeSlot? = null,

    val isTaskInProgress: Boolean = false
)



/**
 * 定义HomeViewModel - 主页的唯一 ViewModel。
 */

class HomeViewModel(application: Application) : AndroidViewModel(application) {

    private val sessionManager = SessionManager(application.applicationContext)

    // 定义一个协程异常处理器。
    // 当 viewModelScope 中任何一个未被捕获的异常发生时，这个处理器会被调用。
    // 这是一种保护机制，可以防止App因协程内的意外错误而崩溃，并能打印出详细的错误日志。
    private val exceptionHandler = CoroutineExceptionHandler { _, throwable ->
        // 当任何一个 viewModelScope.launch 失败时，这里会被调用
        Log.e("HomeViewModel", "Coroutine Exception: ${throwable.message}", throwable)
        // 你可以在这里更新UI状态，显示一个错误信息给用户
        _uiState.update { it.copy(isLoading = false) } // 比如出错时停止加载动画
    }
    // 创建一个私有的、可变的 StateFlow，作为UI状态的唯一真实来源。
    // 只有ViewModel内部可以修改它。
    private val _uiState = MutableStateFlow(HomeUiState())

    // 对外暴露一个公有的、不可变的 StateFlow。
    // UI层（Composable）只能读取这个uiState，不能修改它，保证了单向数据流。
    val uiState: StateFlow<HomeUiState> = _uiState.asStateFlow()

    //内部变量，用于保存当前登录的阿姨的ID
    private var currentAuntieId: Int = -1

    init {
        startClockAndTaskUpdater()
    }

    // --- 核心业务逻辑 ---
    private fun startClockAndTaskUpdater() {
        var lastCheckedSlot: TimeSlot? = null
        viewModelScope.launch(exceptionHandler)  {
            AppRepository.isInitialized.first { it }
            // b. 开始收听“后台轮询”发出的广播信号
            //    这使得你在Web后台修改数据后，App也能自动刷新
            launch { // 启动一个新的子协程来专门收听广播
                AppRepository.dataRefreshed.collect {
                    Log.d("HomeViewModel", "接收到全局数据更新信号，正在刷新...")
                    if (currentAuntieId != -1) {
                        refreshCurrentTasks()
                    }
                }
            }
            while (true) {
                // a. 只有当阿姨已经登录时 (currentAuntieId 已被设置)，才执行刷新
                if (currentAuntieId != -1) {
                    val newCurrentSlot = AppRepository.findCurrentTimeSlot()
                    // 只有在时间段变化时，或者这是第一次加载(lastCheckedSlot==null)，才刷新
                    if (newCurrentSlot != lastCheckedSlot) {
                        refreshCurrentTasks()
                        lastCheckedSlot = newCurrentSlot
                    }
                }

                // b. 更新UI时间
                val calendar = Calendar.getInstance()
                val newTimePhrase = if (calendar.get(Calendar.HOUR_OF_DAY) in 0..11) "上午" else "下午"
                val newFormattedTime = SimpleDateFormat("H:mm", Locale.getDefault()).format(Date())
                _uiState.update { it.copy(timePhrase = newTimePhrase, formattedTime = newFormattedTime) }
                delay(1000 *1) // 每1秒检查一次
            }
        }
    }

    /**
     * 当阿姨登录后，由UI层调用，用于加载这位阿姨的所有相关数据。
     */
    fun loadDataForAuntie(auntieId: Int) {
        viewModelScope.launch(exceptionHandler) {
            // 1. 等待 Repository 发出“我已准备好”的信号。
            Log.d("ViewModelWait", "ViewModel 正在等待 Repository 初始化完成...")
            // .first 会挂起当前协程（不阻塞线程），直到 isInitialized 的值变为 true
            AppRepository.isInitialized.first { it == true }
            Log.d("ViewModelWait", "Repository 已就绪！ViewModel 开始执行后续操作。")

            if (currentAuntieId == auntieId && !_uiState.value.isLoading) return@launch // 防止重复加载

            currentAuntieId = auntieId
            _uiState.update { it.copy(isLoading = true) }

            val savedUser = sessionManager.getSession()
            if (savedUser != null && savedUser.id == auntieId) {
                _uiState.update { it.copy(auntieName = savedUser.name) }
            }
            // 调用 suspend 刷新函数
//            refreshCurrentTasks()
        }
    }

    /**
     * 根据当前时间段，刷新需要执行的任务列表和所有相关的UI状态。
     * 返回一个 Job 对象，代表了这个后台刷新任务。
     */
    private suspend fun refreshCurrentTasks(){

        val TAG = "DataFlowDebug"
        Log.d(TAG, "--- HomeViewModel.refreshCurrentTasks 开始 ---")

        if (currentAuntieId == -1) return
        _uiState.update { it.copy(isLoading = true) }

        // --- 1. ⭐ 从 Repository 获取所有需要的数据 ---
        val auntieData = AppRepository.getAuntieDataBundle(currentAuntieId)
        val myPatients = auntieData.patients
        val allMyTasksToday = auntieData.schedules
        Log.d(TAG, "ViewModel 从 Repository 拿到了 ${myPatients.size} 个病人和 ${allMyTasksToday.size} 条排班")

        val tasksResult = AppRepository.getTasksForDate(Date())
        if (tasksResult.isFailure) {
            Log.e(TAG, "获取任务状态失败", tasksResult.exceptionOrNull())
            _uiState.update { it.copy(isLoading = false) }
            return
        }
        val allTodayStates = tasksResult.getOrThrow()
        Log.d(TAG, "从服务器获取了 ${allTodayStates.size} 条任务状态")

        // ⭐ 使用 Repository 提供的方法
        val now = Calendar.getInstance()
        val currentRunningTimeSlot = AppRepository.findCurrentTimeSlot()

        // --- 3. 遍历所有任务计划，将它们分类 ---
        val overdueTasksList = mutableListOf<PatientTask>()
        val inProgressTasksList = mutableListOf<PatientTask>()

        allMyTasksToday.forEach { scheduleLink ->
            val taskKey = Pair(scheduleLink.patientId, scheduleLink.timeSlot)
            val status = allTodayStates[taskKey]?.status ?: "待服药"
            val patientData = myPatients.find { it.patientId == scheduleLink.patientId }
            patientData?.let { pat ->
                val patientTask = PatientTask(
                    patient = pat,
                    timeSlot = scheduleLink.timeSlot,
                    status = status,
                    medicineBoxId = pat.patientBarcode
                )

                // 如果任务状态是“已服药”，我们就不关心它了，直接跳过
//                if (status == "已服药") return@forEach

                // 获取任务对应的时间段信息
                val timeSlot = scheduleLink.timeSlot
                val taskEndTime = Calendar.getInstance().apply {
                    set(Calendar.HOUR_OF_DAY, timeSlot.endHour)
                    set(Calendar.MINUTE, 0) // 我们以结束小时的0分作为结束点
                }

                // --- 核心分类逻辑 ---
                // a. 先判断任务是否属于“正在进行”的范畴
                if (currentRunningTimeSlot == scheduleLink.timeSlot) {
                    // 如果是进行中的任务 (无论状态是“待服药”还是“已服药”)，都加入 inProgressTasksList
                    inProgressTasksList.add(patientTask)
                }
                // b. 如果不是进行中的任务，再判断它是不是一个“超时未完成”的任务
                else if (now.after(taskEndTime) && status == "待服药") {
                    overdueTasksList.add(patientTask)
                }
            }
            // c. 其他情况 (未来的任务、已完成的超时任务) 都会被自动忽略！
        }
        Log.d(TAG, "分类完成：找到 ${overdueTasksList.size} 个超时任务，${inProgressTasksList.size} 个进行中任务")


        // --- 4. 计算统计数据 ---
        val groupedOverdueTasks = overdueTasksList.groupBy { it.timeSlot }

        val totalTasksTodayCount = allMyTasksToday.size
        val completedTasksTodayCount = allMyTasksToday.count {
            val taskKey = Pair(it.patientId, it.timeSlot)
            allTodayStates[taskKey]?.status == "已服药"
        }
        val totalInProgress = inProgressTasksList.size
        // b. 已完成数，是 inProgressTasksList 中状态为“已服药”的数量
        val completedInProgress = inProgressTasksList.count { it.status == "已服药" }
        val isTaskInProgressNow =
            inProgressTasksList.any { it.status == "待服药" } || overdueTasksList.isNotEmpty()

        // --- 5. 更新UI State ---
        _uiState.update { currentState ->
            Log.d(TAG, "正在更新UI State...")
            currentState.copy(
//                    auntieName = auntieName,
                totalTasksToday = totalTasksTodayCount,
                completedTasksToday = completedTasksTodayCount,
                totalTasksInProgress = totalInProgress,
                completedTasksInProgress = completedInProgress,
                patientTakingStates = allTodayStates,
                overdueTaskGroups = groupedOverdueTasks,
                inProgressTasks = inProgressTasksList,
                isTaskInProgress = isTaskInProgressNow, // 应用内横幅提醒的逻辑也更新了
                isLoading = false,
                currentTimeSlot = AppRepository.findCurrentTimeSlot()
            )
        }
        Log.d(TAG, "--- HomeViewModel.refreshCurrentTasks 结束 ---")
    }
    /**
     * 当用户点击“确定”服药后调用。
     * 负责将指定任务的状态修改为“已服药”并刷新UI。
     */
    suspend fun markPatientAsTaken(patientId: Int, timeSlot: TimeSlot, remark: String){

        val TAG = "MedSystemDebug"
        viewModelScope.launch(exceptionHandler) {
            val currentTime = SimpleDateFormat("HH:mm", Locale.getDefault()).format(Date())

            // 1. 创建一个代表【新状态】的 TaskState 对象
            val newState = TaskState(
                status = "已服药",
                completionTime = currentTime,
                remark = remark.ifBlank { null }
            )

            // 2. ⭐ 调用 AppRepository 中【新的、修正后的】更新方法
            val updateResult = AppRepository.updateTask(
                patientId = patientId,
                timeSlot = timeSlot,
                newState = newState,
                date = Date() // 传入今天的日期
            )

            // 3. 更新成功后，刷新UI
            if (updateResult.isSuccess) {
                refreshCurrentTasks()
            }else {
                Log.e(TAG, "更新任务失败", updateResult.exceptionOrNull())
            }
                Log.d(TAG, "<== markPatientAsTaken: 执行结束")
        }
    }
}