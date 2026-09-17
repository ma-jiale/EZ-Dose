package com.example.medicinecontrolsystem.respository

import android.content.Context // 用于获取资源ID
import android.util.Log
import com.example.medicinecontrolsystem.R
import com.example.medicinecontrolsystem.data.*
import com.example.medicinecontrolsystem.network.*
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import java.text.SimpleDateFormat
import java.util.*

// 定义一个数据包，方便一次性返回一个阿姨需要的所有数据
data class AuntieDataBundle(
    val patients: List<data_Patient>,
    val schedules: List<PatientScheduleLink>
)

//用于返回护工需要的所有数据
data class CaregiverDataBundle(
    val aunties: List<Auntie>,
    val patients: List<data_Patient>,
    val schedules: List<PatientScheduleLink>
)


// 使用 'object' 关键字创建一个单例
object AppRepository {

    private const val TAG = "InitializeDebug"



    // --- 1. 内存缓存 ---
    // App启动后，我们会把这些不常变动的数据从服务器拉取一次，缓存在内存里
    private var timeSlots: List<TimeSlot> = emptyList()
    private var allPatients: List<data_Patient> = emptyList() // 缓存所有病人
    private var allSchedules: List<PatientScheduleLink> = emptyList() // 缓存所有计划
    private var allCaregivers: List<Caregiver> = emptyList() // <-- 也需要缓存护工信息
    private var allAunties: List<Auntie> = emptyList() // <-- 也需要缓存阿姨信息

    private val _isInitialized = MutableStateFlow(false)
    val isInitialized = _isInitialized.asStateFlow()

    // ⭐ 2. 新增一个 SharedFlow，作为“数据已更新”的广播频道
    private val _dataRefreshed = MutableSharedFlow<Unit>()
    val dataRefreshed = _dataRefreshed.asSharedFlow()
    /**
     * App启动时必须调用的初始化函数
     * 它负责从服务器拉取所有基础配置数据
     */
    suspend fun initialize(context: Context) {
        if (_isInitialized.value) return

        // 调用核心的刷新逻辑
        val success = refreshAllData(context)

        if (success) {
            _isInitialized.value = true
        }
    }
    suspend fun refreshAllData(context: Context):Boolean {
        Log.d(TAG, "正在初始化Repository...")
        var isSuccess: Boolean
        try {
            // a. 获取所有时间段
            val timeslotsResponse = RetrofitInstance.api.getAllTimeSlots()
            if (timeslotsResponse.isSuccessful) {
                timeSlots = timeslotsResponse.body()?.map { it.toTimeSlot() } ?: emptyList()
                Log.d(TAG, "成功获取并缓存了 ${timeSlots.size} 个时间段")
            } else {
                Log.e(TAG, "获取时间段失败: ${timeslotsResponse.code()}")
            }

            // b. 获取所有病人
            val patientsResponse = RetrofitInstance.api.getPatients() // 假设有一个获取所有病人的接口
            if (patientsResponse.isSuccessful) {
                allPatients =
                    patientsResponse.body()?.map { it.toDataPatient(context) } ?: emptyList()
                Log.d(TAG, "成功获取并缓存了 ${allPatients.size} 个病人")
            } else {
                Log.e(TAG, "获取病人失败: ${patientsResponse.code()}")
            }

            // c. 获取所有用药计划
            val schedulesResponse = RetrofitInstance.api.getSchedules() // 假设有一个获取所有计划的接口
            if (schedulesResponse.isSuccessful) {
                allSchedules =
                    schedulesResponse.body()?.mapNotNull { it.toPatientScheduleLink(timeSlots) }
                        ?: emptyList()
                Log.d(
                    "DataFlowDebug",
                    "Repository 初始化：成功转换并缓存了 ${allSchedules.size} 条用药计划。"
                )
            } else {
                Log.e(TAG, "获取用药计划失败: ${schedulesResponse.code()}")
            }

            // d. 获取所有阿姨
            val auntiesResponse = RetrofitInstance.api.getAunties() // <-- 需在ApiService定义
            if (auntiesResponse.isSuccessful) {
                allAunties = auntiesResponse.body()?.map { it.toAuntie() } ?: emptyList()
                Log.d(TAG, "成功获取并缓存了 ${allAunties.size} 个阿姨")
            }

            // e. 获取所有护工
            val caregiversResponse = RetrofitInstance.api.getCaregivers() // <-- 需在ApiService定义
            if (caregiversResponse.isSuccessful) {
                allCaregivers = caregiversResponse.body()?.map { it.toCaregiver() } ?: emptyList()
                Log.d(TAG, "成功获取并缓存了 ${allCaregivers.size} 个护工")
            }

            if (timeslotsResponse.isSuccessful && patientsResponse.isSuccessful && schedulesResponse.isSuccessful && auntiesResponse.isSuccessful && caregiversResponse.isSuccessful) {
//                _isInitialized.value = true
                Log.d(TAG, "Repository 初始化成功！")
                _dataRefreshed.emit(Unit)
                isSuccess = true // 返回 true 表示成功
            } else {
                Log.e(TAG, "Repository 初始化失败，部分数据未能获取。")
                isSuccess = false
            }

        } catch (e: Exception) {
            Log.e(TAG, "初始化Repository时发生网络异常", e)
            isSuccess = false
        }
        return isSuccess
    }

    // --- 2. 对外提供数据的方法 ---

    /**
     * (给 HomeViewModel 使用)
     * 从缓存中筛选出某个特定阿姨的数据包
     */
    fun getAuntieDataBundle(auntieId: Int): AuntieDataBundle {
        Log.d(TAG, "--- AppRepository.getAuntieDataBundle ---")
        Log.d(TAG, "请求的 auntieId: $auntieId")
        Log.d(TAG, "总病人数 (缓存): ${allPatients.size}")
        Log.d(TAG, "总排班数 (缓存): ${allSchedules.size}")
        val myPatients = allPatients.filter { it.auntieId == auntieId }
        Log.d(TAG, "筛选后，为阿姨 $auntieId 找到 ${myPatients.size} 个病人")

        val myPatientIds = myPatients.map { it.patientId }.toSet()
        val mySchedules = allSchedules.filter { it.patientId in myPatientIds }
        Log.d(TAG, "筛选后，为这些病人找到 ${mySchedules.size} 条排班")
        return AuntieDataBundle(myPatients, mySchedules)
    }

    // --- ⭐ 2. 新增一个专门为护工端服务的数据获取方法 ---
    fun getCaregiverDataBundle(caregiverId: Int): CaregiverDataBundle {
        Log.d(TAG, "--- AppRepository.getCaregiverDataBundle ---")
        Log.d(TAG, "请求的 caregiverId: $caregiverId")
        Log.d(TAG, "总阿姨数 (缓存): ${allAunties.size}")
        // a. 从缓存中筛选出该护工手下的所有阿姨
        val myAunties = allAunties.filter { it.caregiverId == caregiverId }
        Log.d(TAG, "筛选后，找到 ${myAunties.size} 个阿姨")
        val myAuntieIds = myAunties.map { it.auntieId }.toSet()
        Log.d(TAG, "总病人数 (缓存): ${allPatients.size}")
        // b. 从缓存中筛选出这些阿姨手下的所有病人
        val myPatients = allPatients.filter { it.auntieId in myAuntieIds }
        Log.d(TAG, "筛选后，找到 ${myPatients.size} 个病人")
        val myPatientIds = myPatients.map { it.patientId }.toSet()

        // c. 从缓存中筛选出这些病人的所有用药计划
        val mySchedules = allSchedules.filter { it.patientId in myPatientIds }

        return CaregiverDataBundle(myAunties, myPatients, mySchedules)
    }



    /**
     * 辅助函数，根据当前时间，从缓存的 timeSlots 列表中找到对应的时间段
     */
    fun findCurrentTimeSlot(): TimeSlot? {
        val now = Calendar.getInstance()
        val currentHour = now.get(Calendar.HOUR_OF_DAY)
        val currentMinute = now.get(Calendar.MINUTE)
        val currentTimeInMinutes = currentHour * 60 + currentMinute

        return timeSlots.find {
            val startTimeInMinutes = it.startHour * 60 + it.startMinute
            val endTimeInMinutes = it.endHour * 60
            currentTimeInMinutes >= startTimeInMinutes && currentTimeInMinutes < endTimeInMinutes
        }
    }

    fun findTimeSlotByName(name: String): TimeSlot? {
        // timeSlots 列表是在 initialize() 时从服务器获取并缓存的
        return timeSlots.find { it.name == name }
    }

    fun getCaregiverName(caregiverId: Int): String {
        return allCaregivers.find { it.caregiverId == caregiverId }?.name ?: "未知护工"
    }
    fun getAuntieIdsForCaregiver(caregiverId: Int): Set<Int> {
        return allAunties.filter { it.caregiverId == caregiverId }.map { it.auntieId }.toSet()
    }

    fun getPatientsForAunties(auntieIds: Set<Int>): List<data_Patient> {
        return allPatients.filter { it.auntieId in auntieIds }
    }

    fun getSchedulesForPatients(patientIds: Set<Int>): List<PatientScheduleLink> {
        return allSchedules.filter { it.patientId in patientIds }
    }

    fun getAllTimeSlots(): List<TimeSlot> {
        return timeSlots
    }

    fun getPatientCountForTimeSlot(context: Context, timeSlot: TimeSlot): Int {
        // 1. 获取当前登录的用户信息
        val sessionManager = SessionManager(context)
        val loggedInUser = sessionManager.getSession()

        // 如果没有用户登录，或者登录的不是阿姨，则任务数为0
        if (loggedInUser == null || loggedInUser.role != UserRole.AUNTIE) {
            return 0
        }
        val currentAuntieId = loggedInUser.id

        // 2. 从【缓存】中筛选数据
        val myPatientIds = allPatients
            .filter { it.auntieId == currentAuntieId }
            .map { it.patientId }
            .toSet()

        val count = allSchedules.count {
            it.timeSlot == timeSlot && it.patientId in myPatientIds
        }
        return count
    }

    /**
     * (给 TimeoutWarningWorker 使用)
     * 计算在指定时间段，当前登录的阿姨【未完成】的任务数量。
     */
    suspend fun getPendingTaskCountForTimeSlot(context: Context, timeSlot: TimeSlot): Int {
        val sessionManager = SessionManager(context)
        val loggedInUser = sessionManager.getSession()
        if (loggedInUser == null || loggedInUser.role != UserRole.AUNTIE) {
            return 0
        }
        val currentAuntieId = loggedInUser.id

        // 从【内存缓存】中筛选数据
        val myPatientIds = allPatients
            .filter { it.auntieId == currentAuntieId }
            .map { it.patientId }
            .toSet()

        val myTaskPatientIds = allSchedules
            .filter { it.timeSlot == timeSlot && it.patientId in myPatientIds }
            .map { it.patientId }
            .toSet()

        if (myTaskPatientIds.isEmpty()) {
            return 0
        }

//        // ⭐ 任务状态的判断，暂时还依赖 CsvDataManager
//        val csvDataManager = CsvDataManager(context)
//        val allTaskStates = csvDataManager.loadTaskStates()

        // ⭐ 3. 通过网络，获取今天的最新任务状态
        val tasksResult = getTasksForDate(Date()) // 调用我们已经写好的 getTasksForDate 函数
        if (tasksResult.isFailure) {
            // 如果网络请求失败，我们认为没有待办任务，避免错误地发出警告
            return 0
        }
        val allTaskStates = tasksResult.getOrThrow()

        val pendingCount = myTaskPatientIds.count { patientId ->
            val taskKey = Pair(patientId, timeSlot)
            val currentState = allTaskStates[taskKey]?.status
            currentState == "待服药" || currentState == null
        }

        return pendingCount
    }

    /**
     * ⭐ 新增函数：根据病人ID，从缓存中查找并返回单个病人信息。
     */
    fun getPatientById(patientId: Int): data_Patient? {
        // 直接在已经缓存好的 allPatients 列表中查找
        return allPatients.find { it.patientId == patientId }


    }
    fun isTaskOverdue(timeSlot: TimeSlot): Boolean {
        // 1. 获取代表“现在”的日历
        val now = Calendar.getInstance()

        // 2. 创建一个代表“任务结束时间”的日历
        val taskEndTime = Calendar.getInstance().apply {
            set(Calendar.HOUR_OF_DAY, timeSlot.endHour)
            set(Calendar.MINUTE, 0) // 我们以结束小时的0分作为结束点
            set(Calendar.SECOND, 0)
        }

        // 3. 使用 Calendar 的 .after() 方法进行比较
        return now.after(taskEndTime)
    }

    // ⭐ --- 新增：任务状态管理函数 --- ⭐

    /**
     * 从服务器获取某一天的所有任务状态。
     */
    suspend fun getTasksForDate(date: Date): Result<Map<Pair<Int, TimeSlot>, TaskState>> {
        val dateString = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(date)
        return try {
            val response = RetrofitInstance.api.getTasksForDate(dateString)
            if (response.isSuccessful) {
                val tasksFromServer = response.body() ?: emptyList()
                // 将服务器返回的 List<TaskStateResponse> 转换为我们 App 内部需要的 Map
                val tasksMap = tasksFromServer.mapNotNull { it.toTaskStateEntry(timeSlots) }.toMap()
                Result.success(tasksMap)
            } else {
                Result.failure(Exception("获取任务状态失败: ${response.code()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    /**
     * 向服务器提交一个任务状态的更新。
     */
    suspend fun updateTask(
        patientId: Int,       // <-- 明确传入 patientId
        timeSlot: TimeSlot,   // <-- 明确传入 timeSlot
        newState: TaskState,  // <-- 传入只包含状态信息的 TaskState 对象
        date: Date
    ): Result<Unit> {
        val dateString = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(date)
        // 将 App 内部的 TaskState 转换为网络请求需要的 UpdateTaskRequest
        val requestBody = newState.toUpdateRequest(dateString, patientId, timeSlot)
        return try {
            val response = RetrofitInstance.api.updateTask(requestBody)
            if (response.isSuccessful) {
                Result.success(Unit)
            } else {
                Result.failure(Exception("更新任务失败: ${response.code()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

}

// --- 3. 数据模型转换函数 (非常关键！) ---
// 这些函数负责将网络层的数据(String类型)转换为App业务层的数据(Int, Enum等)

fun TimeSlotResponse.toTimeSlot(): TimeSlot {
    return TimeSlot(
        name = this.name,
        displayName = this.displayName,
        startHour = this.startHour.toIntOrNull() ?: 0,
        endHour = this.endHour.toIntOrNull() ?: 0,
        startMinute = this.startMinute.toIntOrNull() ?: 0
    )
}

fun PatientResponse.toDataPatient(context: Context): data_Patient {
    // 这是一个关键的转换：根据服务器传来的图片名 "patient1"，找到 R.drawable.patient1
    val imageResId = context.resources.getIdentifier(
        this.imageResourceId,
        "drawable",
        context.packageName
    ).let { if (it == 0) R.drawable.patient1 else it } // 如果找不到，给一个默认图标

    return data_Patient(
        patientId = this.patientId.toIntOrNull() ?: -1,
        auntieId = this.auntieId.toIntOrNull() ?: -1,
        imageResourceId = this.imageResourceId,
        // ⭐ 注意：为了继续使用资源ID，我们需要修改 data_Patient 的定义
        //    一个更简单的做法是，直接让 data_Patient 使用 String
        patientName = this.patientName, // 暂时占位
        patientBedNumber = this.patientBedNumber, // 暂时占位
        patientBarcode = this.patientBarcode
    )
}

fun ScheduleResponse.toPatientScheduleLink(allTimeSlots: List<TimeSlot>): PatientScheduleLink? {
    // 根据服务器传来的 timeSlotName (e.g., "BEFORE_BREAKFAST")，
    // 从我们缓存的 allTimeSlots 列表中找到对应的 TimeSlot 对象
    val timeSlot = allTimeSlots.find { it.name == this.timeSlotName }

    return if (timeSlot != null) {
        PatientScheduleLink(
            patientId = this.patientId.toIntOrNull() ?: -1,
            timeSlot = timeSlot
        )
    } else {
        null // 如果找不到对应的时间段，返回null
    }
}

/**
 * 将网络层的 AuntieResponse 对象，转换为App业务层的 Auntie 对象。
 */
fun AuntieResponse.toAuntie(): Auntie {
    return Auntie(
        auntieId = this.auntieId.toIntOrNull() ?: -1, // 将字符串ID转为整数
        name = this.name,
        username = this.username,
        // password 字段通常在业务模型中不需要，所以我们丢弃它
        caregiverId = this.caregiverId.toIntOrNull() ?: -1 // 将字符串ID转为整数
    )
}

/**
 * 将网络层的 CaregiverResponse 对象，转换为App业务层的 Caregiver 对象。
 */
fun CaregiverResponse.toCaregiver(): Caregiver {
    return Caregiver(
        caregiverId = this.caregiverId.toIntOrNull() ?: -1, // 将字符串ID转为整数
        name = this.name,
        username = this.username
        // password 字段被丢弃
    )
}

fun TaskStateResponse.toTaskStateEntry(allTimeSlots: List<TimeSlot>): Pair<Pair<Int, TimeSlot>, TaskState>? {
    val patientId = this.patientId.toIntOrNull() ?: return null
    val timeSlot = allTimeSlots.find { it.name == this.timeSlotName } ?: return null

    val taskState = TaskState(
        status = this.status,
        completionTime = this.completionTime?.ifBlank { null },
        remark = this.remark?.ifBlank { null }
    )

    val taskKey = Pair(patientId, timeSlot)
    return taskKey to taskState
}

fun TaskState.toUpdateRequest(date: String, patientId: Int, timeSlot: TimeSlot): UpdateTaskRequest {
    return UpdateTaskRequest(
        date = date,
        patientId = patientId,
        timeSlotName = timeSlot.name, // <-- 将 TimeSlot 对象转回 String 名字

        // this 指代的是调用这个函数的 TaskState 对象
        status = this.status,
        completionTime = this.completionTime,
        remark = this.remark
    )
}