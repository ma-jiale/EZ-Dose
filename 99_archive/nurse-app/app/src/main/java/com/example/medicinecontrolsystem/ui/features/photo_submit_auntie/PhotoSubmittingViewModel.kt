package com.example.medicinecontrolsystem.ui.features.photo_submit_auntie

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.medicinecontrolsystem.data.data_Patient
import com.example.medicinecontrolsystem.respository.AppRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

// 1. 定义UI状态数据类
data class PhotoSubmittingUiState(
    val patient: data_Patient? = null, // 当前正在处理的病人
    val isLoading: Boolean = true,
    val errorMessage: String? = null // 用于显示错误信息
)

// 2. 创建ViewModel
class PhotoSubmittingViewModel : ViewModel() {

    private val _uiState = MutableStateFlow(PhotoSubmittingUiState())
    val uiState = _uiState.asStateFlow()

    /**
     * 当页面启动时，根据传入的patientId加载病人数据。
     * @param patientId 从导航参数中获取的病人ID。
     */
    fun loadPatientInfo(patientId: Int?) {
        if (patientId == null) {
            _uiState.update {
                it.copy(errorMessage = "无效的患者ID", isLoading = false)
            }
            return
        }

        viewModelScope.launch {
            // a. 调用 Repository 的 getPatientById 方法
            val foundPatient = AppRepository.getPatientById(patientId)


            if (foundPatient != null) {
                _uiState.update {
                    it.copy(patient = foundPatient, isLoading = false, errorMessage = null)
                }
            } else {
                _uiState.update {
                    it.copy(errorMessage = "未找到ID为 $patientId 的患者", isLoading = false)
                }
            }
        }
    }
}