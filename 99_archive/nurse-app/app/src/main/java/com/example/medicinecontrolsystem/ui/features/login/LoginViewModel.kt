package com.example.medicinecontrolsystem.ui.features.login

import android.app.Application
import androidx.lifecycle.AndroidViewModel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.medicinecontrolsystem.data.LoggedInUser
import com.example.medicinecontrolsystem.data.SessionManager
import com.example.medicinecontrolsystem.data.UserRole // 暂时用于模拟用户
import com.example.medicinecontrolsystem.network.LoginRequest // 导入
import com.example.medicinecontrolsystem.network.RetrofitInstance
import com.example.medicinecontrolsystem.network.NetworkManager
import com.example.medicinecontrolsystem.data.initialAunties
import com.example.medicinecontrolsystem.data.initialCaregivers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

// 定义登录页面的UI状态
data class LoginUiState(
    val usernameInput: String = "",
    val passwordInput: String = "",
    val loginError: String? = null, // null表示没有错误
    val isLoading: Boolean = false,
    val loggedInUserId: Int? = null,
    val loggedInUserRole: UserRole? = null,
    val logoutRequested: Boolean = false,
    val networkMessage: String? = null
)

//定义一个类，并继承安卓架构组件中的View Model，具备了生命周期感知的能力
class LoginViewModel(application: Application) : AndroidViewModel(application) {

    private val sessionManager = SessionManager(application.applicationContext)

    private val _uiState = MutableStateFlow(LoginUiState())
    val uiState = _uiState.asStateFlow()

    // UI层通过这个方法来通知ViewModel更新输入框内容
    fun onUsernameChange(username: String) {
        _uiState.update { it.copy(usernameInput = username) }
    }

    fun onPasswordChange(password: String) {
        _uiState.update { it.copy(passwordInput = password) }
    }
    init {
        val savedUser = sessionManager.getSession()
        if (savedUser != null) {
            _uiState.update {
                it.copy(
                    loggedInUserId = savedUser.id,
                    loggedInUserRole = savedUser.role
                )
            }
        }
    }

    // UI层调用这个方法来触发登录
    fun login() {
        viewModelScope.launch {
            // 开始登录，显示加载状态
            _uiState.update { it.copy(isLoading = true, loginError = null) }

            val username = _uiState.value.usernameInput
            val password = _uiState.value.passwordInput

            // 创建一个请求对象
            val loginRequest = LoginRequest(username, password)

            try {
                // 使用 NetworkManager 获取最新的 API 实例，而不是 RetrofitInstance
                val response = NetworkManager.getApiService().login(loginRequest)

                // 处理服务器的响应
                if (response.isSuccessful && response.body() != null) {
                    val loginResponse = response.body()!!
                    if (loginResponse.success) {
                        // --- 登录成功 ---
                        val role = UserRole.valueOf(loginResponse.role!!.uppercase())
                        val userId = loginResponse.userId!!
                        val name = loginResponse.name!!

                        // a. 保存会话到 SharedPreferences
                        sessionManager.saveSession(LoggedInUser(userId, role, name))

                        // b. 更新UI状态以触发导航
                        _uiState.update {
                            it.copy(
                                isLoading = false,
                                loggedInUserRole = role,
                                loggedInUserId = userId
                            )
                        }
                    } else {
                        // --- 登录失败 (服务器返回的业务错误) ---
                        _uiState.update {
                            it.copy(
                                isLoading = false,
                                loginError = loginResponse.error ?: "未知错误"
                            )
                        }
                    }
                } else {
                    // --- 登录失败 (网络错误，比如404, 500) ---
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            loginError = "服务器错误: ${response.code()}"
                        )
                    }
                }
            } catch (e: Exception) {
                // --- 登录失败 (网络异常，比如无法连接服务器) ---
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        loginError = "网络连接失败: ${e.message}"
                    )
                }
            }
        }
    }

//            //先使用固定密码
//            if (password != "123456") {
//                _uiState.update { it.copy(isLoading = false, loginError = "密码错误！") }
//                return@launch
//            }
//
//            //先在auntie列表查找用户
//            val auntie = initialAunties.find { it.username == username }
//            if (auntie != null) {
//                _uiState.update {
//                    it.copy(
//                        isLoading = false,
//                        loggedInUserRole = UserRole.AUNTIE,
//                        loggedInUserId = auntie.auntieId
//                    )
//                }
//                sessionManager.saveSession(LoggedInUser(auntie.auntieId, UserRole.AUNTIE))
//                return@launch
//            }
//
//            //如果不是auntie，再在caregiver列表查找用户
//            val caregiver = initialCaregivers.find { it.username == username }
//            if (caregiver != null) {
//                _uiState.update {
//                    it.copy(
//                        isLoading = false,
//                        loggedInUserRole = UserRole.CAREGIVER,
//                        loggedInUserId = caregiver.caregiverId
//                    )
//                }
//                sessionManager.saveSession(LoggedInUser(caregiver.caregiverId, UserRole.CAREGIVER))
//                return@launch
//            }
//
//            //如果都没找到说明用户名不存在
//            _uiState.update { it.copy(isLoading = false, loginError = "用户名不存在！") }
//        }
//    }


    fun logout(){
        viewModelScope.launch{
            _uiState.update{
                it.copy(
                    usernameInput = "",
                    passwordInput = "",
                    loginError = null,
                    logoutRequested = true,
                    loggedInUserRole = null,
                    loggedInUserId = null,
                )
            }
        }
    }

    fun onNavigationComplete(){
        _uiState.update { it.copy(loggedInUserRole = null, logoutRequested = false) }
    }

    fun updateServerUrl(url: String) {
        viewModelScope.launch {
            try {
                NetworkManager.updateBaseUrl(url)

                // 测试新的连接
                val isConnected = NetworkManager.testConnection()
                if (isConnected) {
                    _uiState.update {
                        it.copy(networkMessage = "服务器连接成功: $url")
                    }
                } else {
                    _uiState.update {
                        it.copy(networkMessage = "服务器连接失败，请检查地址和网络")
                    }
                }

                // 3秒后清除消息
                kotlinx.coroutines.delay(3000)
                _uiState.update { it.copy(networkMessage = null) }
            } catch (e: Exception) {
                _uiState.update {
                    it.copy(networkMessage = "设置失败: ${e.message}")
                }
            }
        }
    }

    // 新增：获取当前服务器URL
    fun getCurrentServerUrl(): String {
        return NetworkManager.getBaseUrl()
    }

    // 新增：清除网络消息
    fun clearNetworkMessage() {
        _uiState.update { it.copy(networkMessage = null) }
    }

}