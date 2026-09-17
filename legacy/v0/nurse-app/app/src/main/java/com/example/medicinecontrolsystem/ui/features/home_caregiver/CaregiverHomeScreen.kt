package com.example.medicinecontrolsystem.ui.features.home_caregiver

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.min
import androidx.navigation.NavController
import com.example.medicinecontrolsystem.ui.features.home_caregiver.components.TopInformationCard
import com.example.medicinecontrolsystem.ui.features.home_caregiver.components.OverdueTasksCard
import com.example.medicinecontrolsystem.ui.features.home_caregiver.components.UpcomingTimeoutTasksCard

@Composable
fun CaregiverHomeScreen(
    navController: NavController,
    caregiverViewModel: CaregiverViewModel, // 唯一的ViewModel
    onLogout:() -> Unit
) {
    // 只需要订阅这一个统一的状态
    val uiState by caregiverViewModel.uiState.collectAsState()

    // 获取屏幕尺寸，计算基础单位 (baseUnit)
    val configuration = LocalConfiguration.current
    val screenHeight = configuration.screenHeightDp.dp
    val screenWidth = configuration.screenWidthDp.dp
    val baseUnit = min(screenHeight, screenWidth) / 40f

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(
                brush = Brush.verticalGradient(
                    colors = listOf(Color(0xFFFFFCF7), Color(0xFFE6F1FF))
                )
            )
    ) {
        // 如果数据还在加载中，可以显示一个加载圈 (可选)
        // if (uiState.isLoading) {
        //     CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
        // } else {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(horizontal = baseUnit * 1.5f)
        ) {
            Spacer(modifier = Modifier.height(baseUnit * 2))
            Button(
                onClick = onLogout,
                colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.error)
            ){
                Text("退出登录")
            }

            // 3. 将从uiState中获取的原始数据传递给子组件
            TopInformationCard(
                caregiverName = uiState.caregiverName,
                timePhrase = uiState.timePhrase,
                formattedTime = uiState.formattedTime,
                completedTasks = uiState.completedInProgressTaskCount,
                totalTasks = uiState.inProgressTaskCount,
                navController = navController,
                baseUnit = baseUnit,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(baseUnit * 20f)
            )

            Spacer(modifier = Modifier.height(baseUnit * 0.5f))

            LazyColumn(
                modifier = Modifier
                    .fillMaxSize()
                .padding(horizontal = baseUnit * 1.5f)
            ) {
                if (uiState.upcomingTimeoutTasks.isNotEmpty()) {
                    item {
                        // 这里我们假设你已经创建了一个 UpcomingTimeoutTasksCard
                        // 它的内部实现和 OverdueTasksCard 几乎一样
                        UpcomingTimeoutTasksCard(
                            tasks = uiState.upcomingTimeoutTasks,
                            baseUnit = baseUnit,
                            onViewAllClick = {
                                navController.navigate("caregiver_monitor") // 也可以跳转到监控页
                            }
                        )
                        Spacer(modifier = Modifier.height(baseUnit))
                    }
                }
                item {
                    OverdueTasksCard(
                        tasks = uiState.overdueTasks,
                        baseUnit = baseUnit,
                        // ⭐ 点击后可以跳转到任务监控页，并自动筛选出“超时”
                        onViewAllClick = {
                            navController.navigate("caregiver_monitor") // (未来可以实现)
                        }
                    )
                    Spacer(modifier = Modifier.height(baseUnit))
                }

        }

//            CenterInformation(
//                timePhrase = uiState.timePhrase,
//                formattedTime = uiState.formattedTime,
//                baseUnit = baseUnit,
//                modifier = Modifier
//                    .fillMaxWidth()
//                    .height(baseUnit * 3f)
//            )
//
//            Spacer(modifier = Modifier.height(baseUnit * 0.5f))
//
//            CaregiverPatientList(
//                navController = navController,
//                patients = uiState.patientsForDisplay,
//                patientStates = uiState.patientStatesWithTime,
//                baseUnit = baseUnit,
//                modifier = Modifier
//                    .fillMaxWidth()
//                    .weight(1f)
//            )
        }
        // }
    }
}