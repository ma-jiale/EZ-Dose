package com.example.medicinecontrolsystem.network

import com.example.medicinecontrolsystem.data.TimeSlot
import com.google.gson.annotations.SerializedName
import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Query


// --- 1. ⭐ 定义与云端服务器返回的JSON【完全匹配】的数据类 ---

// a. 定义内层的 'message' 对象
data class TestMessageContent(
    @SerializedName("type") val type: String,
    @SerializedName("content") val content: String,
    @SerializedName("timestamp") val timestamp: String
)

// b. 定义最外层的完整响应对象
data class CloudTestResponse(
    @SerializedName("status") val status: String,
    @SerializedName("message") val message: TestMessageContent
)
// --- 数据传输对象 (DTOs) ---


/**
 * 对应 server.py 中 /login 接口的响应体
 */
data class LoginResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("userId") val userId: Int?,
    @SerializedName("role") val role: String?,
    @SerializedName("name") val name: String?,
    @SerializedName("error") val error: String?
)

/**
 * 对应 server.py 中 /timeslots 接口返回的列表中的单个元素
 * 它的字段必须和 timeslots.csv 的表头对应
 */
data class TimeSlotResponse(
    @SerializedName("name") val name: String,
    @SerializedName("displayName") val displayName: String,
    @SerializedName("startHour") val startHour: String, // CSV读出来是字符串，先用String接收
    @SerializedName("endHour") val endHour: String,
    @SerializedName("startMinute") val startMinute: String
)

/**
 * 对应 server.py 中 /patients 接口返回的列表中的单个元素
 * 它的字段必须和 patients.csv 的表头对应
 */
data class PatientResponse(
    @SerializedName("patientId") val patientId: String,
    @SerializedName("auntieId") val auntieId: String,
    @SerializedName("imageResourceId") val imageResourceId: String,
    @SerializedName("patientName") val patientName: String,
    @SerializedName("patientBedNumber") val patientBedNumber: String,
    @SerializedName("patientBarcode") val patientBarcode: String
)

/**
 * 对应 server.py 中 /schedules 接口返回的列表中的单个元素
 * 它的字段必须和 schedules.csv 的表头对应
 */
data class ScheduleResponse(
    @SerializedName("patientId") val patientId: String,
    @SerializedName("timeSlotName") val timeSlotName: String
)

/**
 * 对应 server.py 中 /tasks 接口返回的列表中的单个元素
 * 它的字段必须和 tasks_yyyy-MM-dd.csv 的表头对应
 */
data class TaskStateResponse(
    @SerializedName("patientId") val patientId: String,
    @SerializedName("timeSlotName") val timeSlotName: String,
    @SerializedName("status") val status: String,
    @SerializedName("completionTime") val completionTime: String?, // 可能为空
    @SerializedName("remark") val remark: String? // 可能为空
)

// ⭐ 1. 新增：定义护工数据的网络响应模型
//    字段名必须和 caregivers.csv 的表头完全一致
data class CaregiverResponse(
    @SerializedName("caregiverId") val caregiverId: String,
    @SerializedName("name") val name: String,
    @SerializedName("username") val username: String,
    @SerializedName("password") val password: String
)

// ⭐ 2. 新增：定义阿姨数据的网络响应模型
//    字段名必须和 aunties.csv 的表头完全一致
data class AuntieResponse(
    @SerializedName("auntieId") val auntieId: String,
    @SerializedName("name") val name: String,
    @SerializedName("username") val username: String,
    @SerializedName("password") val password: String,
    @SerializedName("caregiverId") val caregiverId: String
)

// 这个类现在既用于响应，也用于请求
data class TaskState(
//    val patientId: Int,
//    val timeSlot: TimeSlot, // 在App内部我们使用完整的TimeSlot对象
    var status: String,
    var completionTime: String?,
    var remark: String?
)

// 用于网络请求的请求体
data class TaskUpdateRequest(
    val date: String,
    val patientId: Int,
    val timeSlotName: String,
    val status: String,
    val completionTime: String?,
    val remark: String?
)

data class LoginRequest(
    val username: String,
    val password: String
)

//定义更新任务时需要发送的数据体
data class UpdateTaskRequest(
    val date: String,
    val patientId: Int,
    val timeSlotName: String,
    val status: String,
    val completionTime: String?,
    val remark: String?
)

// --- 3. 在 ApiService 接口中定义新的 /login 请求方法 ---
interface ApiService {
    // @POST("login") 表示这是一个向 /login 路径发起的 POST 请求
    // @Body 表示 loginRequest 这个对象会被转换成JSON，并作为请求的主体发送
    @POST("login")
    suspend fun login(@Body loginRequest: LoginRequest): Response<LoginResponse>

    @GET("patients")
    suspend fun getPatientsForAuntie(@Query("auntieId") auntieId: Int): Response<List<PatientResponse>>

    @GET("patients")
    suspend fun getPatients(): Response<List<PatientResponse>>

    @GET("schedules")
    suspend fun getSchedulesForAuntie(@Query("auntieId") auntieId: Int): Response<List<ScheduleResponse>>

    @GET("schedules")
    suspend fun getSchedules(): Response<List<ScheduleResponse>>

    // ⭐ 新增：获取所有时间段
    @GET("timeslots")
    suspend fun getAllTimeSlots(): Response<List<TimeSlotResponse>>

    // ⭐ 新增：获取某一天的任务状态
    @GET("tasks")
    suspend fun getTasksForDate(
        @Query("date") date: String, // "yyyy-MM-dd"
        @Query("auntieId") auntieId: Int
    ): Response<List<TaskStateResponse>>

    // ⭐ 3. 新增：获取所有护工列表的接口
    @GET("caregivers")
    suspend fun getCaregivers(): Response<List<CaregiverResponse>>

    // ⭐ 4. 新增：获取所有阿姨列表的接口
    @GET("aunties")
    suspend fun getAunties(): Response<List<AuntieResponse>>


    // ⭐ 1. 定义获取每日任务状态的接口
    @GET("tasks")
    suspend fun getTasksForDate(
        // @Query 会把参数拼接成 URL?date=...&auntieId=...
        // 但我们服务器端的 get_tasks 暂时只用了 date，不过把 auntieId 传过去也没问题
        @Query("date") date: String // 格式 "yyyy-MM-dd"
    ): Response<List<TaskStateResponse>>

    // ⭐ 2. 定义更新单个任务状态的接口
    @PUT("task")
    suspend fun updateTask(@Body updatedTask: UpdateTaskRequest): Response<Unit>

    // ⭐ 新增一个方法，用于从云端服务器的根路径获取测试消息
    @GET(".") // "." 表示直接访问 BASE_URL 本身
    suspend fun getCloudTestMessage(): Response<CloudTestResponse>
}


// 用来设置和修改服务器API
object NetworkManager {
    private var currentBaseUrl: String = "https://ixd.sjtu.edu.cn/flask/"
    private var _api: ApiService? = null

    fun updateBaseUrl(newUrl: String) {
        currentBaseUrl = if (newUrl.endsWith("/")) newUrl else "$newUrl/"
        _api = null // 重置，强制重新创建
        println("NetworkManager: Base URL updated to $currentBaseUrl")
    }

    fun getBaseUrl(): String = currentBaseUrl

    fun getApiService(): ApiService {
        if (_api == null) {
            println("NetworkManager: Creating new ApiService with URL: $currentBaseUrl")
            _api = Retrofit.Builder()
                .baseUrl(currentBaseUrl)
                .addConverterFactory(GsonConverterFactory.create())
                .build()
                .create(ApiService::class.java)
        }
        return _api!!
    }

    // 添加测试连接的方法
    suspend fun testConnection(): Boolean {
        return try {
            // 直接使用getAllTimeSlots接口进行测试，因为这个接口在所有服务器都存在
            val response = getApiService().getAllTimeSlots()
            val isSuccess = response.isSuccessful
            println("NetworkManager: Connection test result: $isSuccess, code: ${response.code()}")
            isSuccess
        } catch (e: Exception) {
            println("NetworkManager: Connection test failed: ${e.message}")
            false
        }
    }
}

// 修改 RetrofitInstance - 移除 lazy 初始化
object RetrofitInstance {
    const val BASE_URL = "https://ixd.sjtu.edu.cn/flask/"

    // 改为每次都从 NetworkManager 获取最新的实例
    val api: ApiService
        get() = NetworkManager.getApiService()
}

