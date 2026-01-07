package com.example.medicinecontrolsystem.ui.features.task_monitor_caregiver

import android.app.Application
import android.util.Log
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.medicinecontrolsystem.data.*
import com.example.medicinecontrolsystem.respository.AppRepository
import kotlinx.coroutines.CoroutineExceptionHandler
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.util.Date

// --- 数据模型 ---

// 用于封装单个病人的任务信息
data class PatientTask(
    val patient: data_Patient, // 复用你现有的data_Patient
    val status: String,    //任务的当前状态
    val timeSlot: TimeSlot,  //任务所示的时间段
    val medicineBoxId: String  //药盒ID
)

// 用于封装一个阿姨和她负责的所有病人任务
data class AuntieTaskGroup(
    val auntieName: String,   //阿姨的名字
    val totalTasks: Int,           //这位阿姨今天的总任务数
    val completedTasks: Int,     //今天已完成的任务数
    val patientTasks: List<PatientTask>     //负责的所有具体任务的列表
)

// 定义筛选器的状态
data class TaskFilters(
    val selectedTimePeriod: String = "全部",    //当前被选中的时间段
    val selectedStatus: String = "全部"    //当前被选中的状态
)

// 任务监控页的完整UI状态
data class TaskMonitorUiState(
    val filteredTaskGroups: List<AuntieTaskGroup> = emptyList(),
    val filters: TaskFilters = TaskFilters(),   //当前的筛选器状态
    val isLoading: Boolean = true   //是否正在加载数据
)

// --- ViewModel ---

class TaskMonitorViewModel(application: Application) : AndroidViewModel(application) {

    private val exceptionHandler = CoroutineExceptionHandler { _, throwable ->
        // 当任何一个 viewModelScope.launch 失败时，这里会被调用
        Log.e("HomeViewModel", "Coroutine Exception: ${throwable.message}", throwable)
        // 你可以在这里更新UI状态，显示一个错误信息给用户
        _uiState.update { it.copy(isLoading = false) } // 比如出错时停止加载动画
    }

    private val _uiState = MutableStateFlow(TaskMonitorUiState())
    val uiState = _uiState.asStateFlow()

    // 用于缓存从数据源获取的原始数据，避免重复加载
    private var originalTaskGroups: List<AuntieTaskGroup> = emptyList()

    private var caregiverId: Int = -1

    init {
        startDataUpdater()
    }

    // ⭐ 入口函数现在只负责一件事：设置ID
    fun loadTasksForCaregiver(caregiverId: Int) {
        if (this.caregiverId == caregiverId) return
        this.caregiverId = caregiverId
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true) }
            refreshData()
        }
    }

    /**
     * ⭐ 这是新的周期性刷新器
     */
    private fun startDataUpdater() {
        viewModelScope.launch {
            // 1. 先等待 Repository 初始化完成
            AppRepository.isInitialized.first { it }
            // b. 开始收听“后台轮询”发出的广播信号
            //    这使得你在Web后台修改数据后，App也能自动刷新
            launch { // 启动一个新的子协程来专门收听广播
                AppRepository.dataRefreshed.collect {
                    Log.d("CaregiverHomeViewModel", "接收到全局数据更新信号，正在刷新...")
                    refreshData()

                }
            }

            // 2. 初始化完成后，进入一个无限循环，周期性地刷新数据
            while (true) {
                // a. 只有当护工已经登录时，才执行刷新
                if (caregiverId != -1) {
                    refreshData()
                }
                delay(10 * 1000L)
            }
        }
    }

    private suspend fun refreshData() {
            Log.d("DataFlowDebug", "--- TaskMonitorViewModel.loadTasksForCaregiver ---")
            // --- 1. 获取真实数据源 ---

            // --- 1. 从 Repository 的【内存缓存】中获取所有需要的基础数据 ---
            val caregiverData = AppRepository.getCaregiverDataBundle(caregiverId)
            val myAunties = caregiverData.aunties
            Log.d("DataFlowDebug", "ViewModel 从 Repository 拿到 ${myAunties.size} 个阿姨")

            val allMyPatients = caregiverData.patients
            val allMySchedules = caregiverData.schedules

        val tasksResult = AppRepository.getTasksForDate(Date())
        if (tasksResult.isFailure) {
            Log.e("TaskMonitorVM", "获取任务状态失败", tasksResult.exceptionOrNull())
            _uiState.update { it.copy(isLoading = false) } // 失败时也要停止加载
            return // 提前退出函数
        }
        // a. 从 Result 中安全地取出数据
            val allTodayStates = tasksResult.getOrThrow()

//            // b. 找出当前护工手下的所有阿姨
//            val myAunties = initialAunties.filter { it.caregiverId == caregiverId }

            // --- 2. 遍历每个阿姨，为她们构建各自的任务组 (AuntieTaskGroup) ---

            originalTaskGroups = myAunties.map { auntie ->
                Log.d("DataFlowDebug", "正在为【${auntie.name}】构建任务组...")
                // --- 对于当前循环的这个 auntie ---

                // a. 找到她负责的所有病人ID
                val herPatients = allMyPatients.filter { it.auntieId == auntie.auntieId }
                val herPatientIds = herPatients.map { it.patientId }.toSet()
                val herTotalTasksSchedule = allMySchedules.filter { it.patientId in herPatientIds }
                Log.d("DataFlowDebug", "  - 为她找到 ${herPatients.size} 个病人")
                Log.d("DataFlowDebug", "  - 为这些病人找到 ${herTotalTasksSchedule.size} 条排班")


                // b. 将计划转换为 PatientTask 对象列表，并附上真实的状态
                val herPatientTasks = herTotalTasksSchedule.mapNotNull { scheduleLink ->
                    val patientData = herPatients.find { it.patientId == scheduleLink.patientId }
                    if (patientData == null) {
                        null // 如果找不到病人数据，就跳过这条计划
                    } else {
                        val taskKey = Pair(scheduleLink.patientId, scheduleLink.timeSlot)
                        val status = allTodayStates[taskKey]?.status ?: "待服药"

                        PatientTask(
                            patient = patientData,
                            status = status,
                            timeSlot = scheduleLink.timeSlot,
                            medicineBoxId = patientData.patientBarcode
                        )
                    }
                }

                // d. 计算已完成的任务数
                val completedCount = herPatientTasks.count { it.status == "已服药" }

                // e. 构建并返回这个阿姨的任务组
                AuntieTaskGroup(
                    auntieName = auntie.name,
                    totalTasks = herPatientTasks.size, // 新增总任务数字段
                    completedTasks = completedCount,   // completedPatients -> completedTasks
                    patientTasks = herPatientTasks
                )

            }
            Log.d("DataFlowDebug", "最终构建了 ${originalTaskGroups.size} 个阿姨的任务组")
            // --- 3. 应用初始筛选并更新UI ---
            applyFilters()
            _uiState.update { it.copy(isLoading = false) }
    }

    //负责响应用户的筛选操作
    fun onTimePeriodSelected(timePeriod: String) {
        _uiState.update { it.copy(filters = it.filters.copy(selectedTimePeriod = timePeriod)) }
        applyFilters()
    }

    fun onStatusSelected(status: String) {
        _uiState.update { it.copy(filters = it.filters.copy(selectedStatus = status)) }
        applyFilters()
    }

    private fun applyFilters() {
        val filters = _uiState.value.filters
        var filteredGroups = originalTaskGroups

        // 1. 根据时间段筛选
        if (filters.selectedTimePeriod != "全部") {
            filteredGroups = originalTaskGroups.map { group ->
                val filteredTasks = group.patientTasks.filter { task ->
                        task.timeSlot.displayName == filters.selectedTimePeriod

                }
                group.copy(patientTasks = filteredTasks)
            }.filter { it.patientTasks.isNotEmpty() } // 如果一个阿姨手下没有符合条件的任务，就不显示她
        }
        //2. 根据状态筛选
        if (filters.selectedStatus != "全部") {
            filteredGroups = filteredGroups.map { group ->
                val filteredTasks = group.patientTasks.filter { task ->
                    task.status == filters.selectedStatus
                }
                group.copy(patientTasks = filteredTasks)
            }.filter { it.patientTasks.isNotEmpty() }
        }
        _uiState.update { it.copy(filteredTaskGroups = filteredGroups)}
        Log.d("DataFlowDebug", "筛选应用完毕，UI State 已更新，显示 ${filteredGroups.size} 个分组。")
    }
}