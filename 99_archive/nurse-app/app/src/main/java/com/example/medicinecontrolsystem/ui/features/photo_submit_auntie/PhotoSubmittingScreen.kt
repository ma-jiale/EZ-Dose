package com.example.medicinecontrolsystem.ui.features.photo_submit_auntie

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.min
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavController
import com.example.medicinecontrolsystem.ui.features.photo_submit_auntie.components.CenterImagePart
import com.example.medicinecontrolsystem.ui.features.photo_submit_auntie.components.TopInformationBarPageSubmitting

@Composable
fun PhotoSubmittingScreen(
    navController: NavController,
    patientId: Int?,
    onConfirmClick: (patientId: Int, remark: String) -> Unit,
    // 1. 接收ViewModel实例
    viewModel: PhotoSubmittingViewModel = viewModel()
) {
    // 2. 在页面第一次加载时，命令ViewModel去加载数据
    LaunchedEffect(patientId) {
        viewModel.loadPatientInfo(patientId)
    }

    // 3. 订阅统一的UI状态
    val uiState by viewModel.uiState.collectAsState()

    // 4. 处理返回键
    BackHandler {
        navController.navigate("home") {
            popUpTo("home") { inclusive = true }
        }
    }

    // 5. 根据UI状态，决定显示什么内容
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(color = Color.White),
        contentAlignment = Alignment.Center
    ) {
        when {
            uiState.isLoading -> {
                // 状态一：正在加载
                CircularProgressIndicator()
            }
            uiState.errorMessage != null -> {
                // 状态二：出现错误
                Text(text = uiState.errorMessage!!, color = Color.Red)
            }
            uiState.patient != null -> {
                // 状态三：加载成功，显示主内容
                val patient = uiState.patient!!
                val configuration = LocalConfiguration.current
                val screenHeight = configuration.screenHeightDp.dp
                val screenWidth = configuration.screenWidthDp.dp
                val baseUnit = min(screenHeight, screenWidth) / 40f

                Column(modifier = Modifier.fillMaxSize()) {
                    // 顶部区域
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .weight(0.8f)
                            .align(Alignment.CenterHorizontally)
                    ) {
                        TopInformationBarPageSubmitting(
                            patient = patient,
                            baseUnit = baseUnit
                        )
                    }

                    // 底部区域
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .weight(6f)
                    ) {
                        CenterImagePart(
                            baseUnit = baseUnit,
                            onConfirmClick = { remarkText ->
                                // 回调逻辑保持不变，但patientId现在更安全
                                onConfirmClick(patient.patientId, remarkText)
                            },
                            onCancelClick = {
                                navController.navigate("home"){
                                    popUpTo("home"){inclusive = true}
                                }
                            }
                        )
                    }
                }
            }
        }
    }
}