package com.example.medicinecontrolsystem.ui.features.camera_auntie

import android.Manifest
import android.content.pm.PackageManager
import androidx.activity.compose.BackHandler
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.core.CameraControl
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavController
import com.example.medicinecontrolsystem.ui.features.camera_auntie.components.CameraPreviewBox
import com.example.medicinecontrolsystem.ui.features.camera_auntie.components.CenteredToast
import com.example.medicinecontrolsystem.ui.features.camera_auntie.components.FlashlightToggleButton
import com.example.medicinecontrolsystem.ui.features.camera_auntie.components.TopHintText
import com.example.medicinecontrolsystem.ui.features.camera_auntie.components.playBeep
import com.example.medicinecontrolsystem.ui.features.home_auntie.HomeViewModel
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.collect
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CameraScreen(
    navController: NavController,
    patientId: Int?,
    viewModel: CameraViewModel = viewModel(),
    timeSlotName: String?,
) {
    // 1. 在屏幕首次进入组合时，命令ViewModel加载病人信息
    LaunchedEffect(patientId) {
        viewModel.handleEvent(CameraEvent.LoadPatientInfo(patientId))
    }

    // 2. 订阅UI状态
    val uiState by viewModel.uiState.collectAsState()
    val context = LocalContext.current
    var cameraControl by remember { mutableStateOf<CameraControl?>(null) }

    // 3. 处理相机权限
    var hasPermission by remember {
        mutableStateOf(ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED)
    }
    val permissionLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.RequestPermission(),
        onResult = { isGranted -> hasPermission = isGranted }
    )

//    val homeUiState by homeViewModel.uiState.collectAsState()
//    val currentTimeSlot = homeUiState.currentTimeSlot

    LaunchedEffect(Unit) {
        if (!hasPermission) {
            permissionLauncher.launch(Manifest.permission.CAMERA)
        }
    }

//    val snackbarHostState = remember { SnackbarHostState() }
//    val scope = rememberCoroutineScope()

    LaunchedEffect(Unit) {
        viewModel.navigationEvent.collect { // collect 会挂起并等待 Channel 发出信号

            // 确认我们能拿到所有需要的信息
            val patientToNavigate = uiState.patient
            if (patientToNavigate != null && timeSlotName != null) {
                playBeep()

                val route = "photo_submit/${patientToNavigate.patientId}/$timeSlotName"

                navController.navigate(route) {
                    popUpTo("camera/${patientId}/$timeSlotName") { inclusive = true }
                    launchSingleTop = true
                }
            }
        }
    }

    // c. 监听 Snackbar 消息
    LaunchedEffect(uiState.toastMessage) {
        uiState.toastMessage?.let { message ->
            if (uiState.toastMessage != null) {
                // 等待一段时间（比如2秒），让用户有时间看
                delay(2000L)
                viewModel.handleEvent(CameraEvent.OnToastShown)
            }
        }
    }


    // 5. 处理系统返回键
    BackHandler { navController.popBackStack() }

    // 6. 构建UI骨架
    Scaffold(topBar = { TopAppBar(title = { Text("扫码核对") })  }
    ) { innerPadding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .background(Brush.verticalGradient(listOf(Color(0xFFFFFCF7), Color(0xFFE6F1FF)))),
            contentAlignment = Alignment.Center
        ) {
            if (hasPermission) {
                // --- 主内容UI ---
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    modifier = Modifier.fillMaxSize()
                ) {
                    TopHintText()
                    Spacer(modifier = Modifier.height(16.dp))

                    CameraPreviewBox(
                        isFlashOn = uiState.isFlashOn,
                        onCameraControlReady = { ctrl -> cameraControl = ctrl },
                        onBarcodesScanned = { codes ->
                            viewModel.handleEvent(CameraEvent.OnBarcodeScanned(codes))
                        }
                    )
                    Spacer(modifier = Modifier.height(32.dp))

                    FlashlightToggleButton(
                        isFlashOn = uiState.isFlashOn,
                        onToggle = {
                            val newFlashState = !uiState.isFlashOn
                            cameraControl?.enableTorch(newFlashState)
                            viewModel.handleEvent(CameraEvent.OnToggleFlash(newFlashState))
                        }
                    )
                }
            } else {
                // --- 请求权限UI ---
                Column(
                    modifier = Modifier.fillMaxSize(),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center
                ) {
                    Text("请授予相机权限以继续使用扫码功能")
                    Spacer(modifier = Modifier.height(16.dp))
                    Button(onClick = { permissionLauncher.launch(Manifest.permission.CAMERA) }) {
                        Text("请求权限")
                    }
                }
            }
            CenteredToast(
                visible = uiState.toastMessage != null, // 当消息不为null时可见
                text = uiState.toastMessage ?: ""  // 显示消息文本
            )
        }
    }
}