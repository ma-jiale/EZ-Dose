package com.example.medicinecontrolsystem.ui.features.camera_auntie

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.medicinecontrolsystem.data.data_Patient
import com.example.medicinecontrolsystem.respository.AppRepository
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.receiveAsFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

// 1. 定义UI状态
data class CameraUiState(
    val patient: data_Patient? = null,
    val isFlashOn: Boolean = false,
    val isScanning: Boolean = true,
//    val showErrorDialog: Boolean = false,
//    val errorMessage: String = "",
//    val scannedBarcodes: List<String> = emptyList()
    val toastMessage: String? = null
)

// 2. 定义ViewModel可以接收的事件
sealed class CameraEvent {
    data class OnBarcodeScanned(val codes: List<String>) : CameraEvent()
    data class OnToggleFlash(val isOn: Boolean) : CameraEvent()
    object OnToastShown : CameraEvent()
    data class LoadPatientInfo(val patientId: Int?) : CameraEvent()
}

// 3. 创建ViewModel
class CameraViewModel : ViewModel() {

    private val _uiState = MutableStateFlow(CameraUiState())
    val uiState = _uiState.asStateFlow()

    private val _navigationEvent = Channel<Unit>()
    val navigationEvent = _navigationEvent.receiveAsFlow()

    fun handleEvent(event: CameraEvent) {
        when (event) {
            is CameraEvent.LoadPatientInfo -> loadPatientInfo(event.patientId)
            is CameraEvent.OnBarcodeScanned -> processScannedBarcodes(event.codes)
            is CameraEvent.OnToggleFlash -> toggleFlash(event.isOn)
//            CameraEvent.OnDialogDismiss -> dismissErrorDialog()
            CameraEvent.OnToastShown -> clearSnackbarMessage()

        }
    }

    private fun loadPatientInfo(patientId: Int?) {
        if (patientId == null) return
        // a. 在协程中执行
        viewModelScope.launch {
            // b. 调用 Repository 的新方法来获取病人信息
            val foundPatient = AppRepository.getPatientById(patientId)
            // c. 更新UI状态
            _uiState.update { it.copy(patient = foundPatient) }
        }
    }

    private fun toggleFlash(isOn: Boolean) {
        _uiState.update { it.copy(isFlashOn = isOn) }
    }

    //清除 Snackbar 消息
    private fun clearSnackbarMessage() {
        _uiState.update { it.copy(toastMessage = null) }
    }

    private fun processScannedBarcodes(codes: List<String>) {
        if (!_uiState.value.isScanning) return
        _uiState.update { it.copy(isScanning = false) }
        val patient = _uiState.value.patient
        if (patient == null) {
            // 如果病人信息还未加载，这是一个错误情况，但我们用 Snackbar 温和提示
            _uiState.update { it.copy(toastMessage = "未能获取到当前患者信息！") }
            // 短暂延迟后恢复扫描
            resumeScanningAfterDelay()
            return
    }
        // 1. 我们期望的 ID (Int 类型)
        val expectedId: Int = patient.patientId

        // 2. 将期望的 ID 转换为【字符串】形式，以便进行比较
        val expectedIdString: String = expectedId.toString()

        val validCodes = codes.filter { it.isNotBlank() }

//        val expectedId = patient.patientId
//        val validCodes = codes.filter { it.isNotBlank() }
        val matchCount = validCodes.count { it == expectedIdString }

        if (matchCount >= 2) {
            // 验证成功！
            viewModelScope.launch {
                _navigationEvent.send(Unit)
            }
        }else {
            // 验证失败
            val error = "扫描数量不足! (${matchCount}/2)"
            // 将错误信息设置到 snackbarMessage 中，UI层会监听到并显示 Snackbar
            _uiState.update { it.copy(toastMessage = error) }
            // 短暂延迟后，自动恢复扫描状态，让用户可以继续扫描
            resumeScanningAfterDelay()
        }
    }

    /**
     * 一个辅助函数，用于在延迟后将 isScanning 状态恢复为 true。
     */
    private fun resumeScanningAfterDelay() {
        viewModelScope.launch {
            delay(1500L) // 用户有 1.5 秒的时间阅读 Snackbar 提示
            _uiState.update { it.copy(isScanning = true) }
        }
    }
}
