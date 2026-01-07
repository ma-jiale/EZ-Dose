package com.example.medicinecontrolsystem.ui.features.home_auntie

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.min
import androidx.compose.ui.unit.sp
import androidx.navigation.NavController
import com.example.medicinecontrolsystem.network.RetrofitInstance
import com.example.medicinecontrolsystem.ui.features.home_auntie.components.CenterInformation
import com.example.medicinecontrolsystem.ui.features.home_auntie.components.CenterInformation2
import com.example.medicinecontrolsystem.ui.features.home_auntie.components.PatientInformationItem
//import com.example.medicinecontrolsystem.ui.features.home_auntie.components.PatientInformationList
import com.example.medicinecontrolsystem.ui.features.home_auntie.components.TimeBar
import com.example.medicinecontrolsystem.ui.features.home_auntie.components.TopInformationCard
import kotlinx.coroutines.launch

@Composable
fun HomeScreen(
    navController: NavController,//导航控制器
    homeViewModel: HomeViewModel,// 唯一的ViewModel,
    onLogout:() -> Unit,
    modifier: Modifier = Modifier
) {
    // 1. 只需要订阅这一个统一的状态
    val uiState by homeViewModel.uiState.collectAsState()

    // 2. 获取屏幕尺寸，计算基础单位 (baseUnit)
    val configuration = LocalConfiguration.current
    val screenHeight = configuration.screenHeightDp.dp
    val screenWidth = configuration.screenWidthDp.dp
    val baseUnit = min(screenHeight, screenWidth) / 40f

    // --- ⭐ 添加临时的测试状态和UI ---
    val scope = rememberCoroutineScope()
    var cloudResponseText by remember { mutableStateOf("点击按钮从云端服务器获取消息") }

    Box(
        modifier = modifier
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
//            /********************************************** 在某个方便的位置，加上我们的测试UI*/
//            Spacer(modifier = Modifier.height(32.dp))
//            Text(text = "云端服务器测试:")
//            Text(text = cloudResponseText)
//            Button(onClick = {
//                scope.launch {
//                    cloudResponseText = "正在连接云端..."
//                    try {
//                        // 调用我们【新定义】的接口方法
//                        val response = RetrofitInstance.api.getCloudTestMessage()
//                        if (response.isSuccessful && response.body() != null) {
//                            val cloudResponse = response.body()!!
//                            // 将获取到的内容格式化后显示
//                            cloudResponseText = "状态: ${cloudResponse.status}\n" +
//                                    "内容: ${cloudResponse.message.content}\n" +
//                                    "时间: ${cloudResponse.message.timestamp}"
//                        } else {
//                            cloudResponseText = "服务器错误: ${response.code()} - ${response.message()}"
//                        }
//                    } catch (e: Exception) {
//                        cloudResponseText = "网络异常: ${e.message}"
//                    }
//                }
//            }) {
//                Text("连接云端服务器")
//            }
//            /********************************************** */
            Spacer(modifier = Modifier.height(baseUnit * 2))
            Button(
                onClick = onLogout,
                colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.error)
            ) {
                Text("退出登录")
            }

            AnimatedVisibility(visible = uiState.isTaskInProgress) {
                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(bottom = baseUnit * 0.5f), // 和下面的组件隔开一点距离
                    shape = RoundedCornerShape(baseUnit),
                    colors = CardDefaults.cardColors(containerColor = Color(0xFFFFA726)) // 醒目的橙色
                ) {
                    Text(
                        text = "当前有送药任务正在进行，请尽快完成！",
                        color = Color.White,
                        fontWeight = FontWeight.Bold,
                        fontSize = (baseUnit.value * 1.6).sp
                    )
                }

            }

            // 3. 将从uiState中获取的原始数据传递给子组件
            TopInformationCard(
                auntieName = uiState.auntieName,
                timePhrase = uiState.timePhrase,
                formattedTime = uiState.formattedTime,
                completedTasks = uiState.completedTasksInProgress,
                totalTasks = uiState.totalTasksInProgress,
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
            ) {
                item {
                    CenterInformation(
                        timePhrase = uiState.timePhrase,
                        formattedTime = uiState.formattedTime,
                        baseUnit = baseUnit,
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(baseUnit * 3f)
                    )
                    Spacer(modifier = Modifier.height(baseUnit * 0.5f))
                }

                items(uiState.inProgressTasks) { task ->
                    Box(modifier = Modifier) {
                        PatientInformationItem(
                            patient = task.patient,
                            isTaken = (task.status != "待服药"),
                            baseUnit = baseUnit,
                            onNavigateToSubmit = {
                                if (task.status == "待服药") {
                                    navController.navigate("photo_submit/${task.patient.patientId}/${task.timeSlot.name}")
                                }
                            },
                            onNavigateToScan = {
                                if (task.status == "待服药") {
                                    navController.navigate("camera/${task.patient.patientId}/${task.timeSlot.name}")
                                }
                            }
                        )
                        Spacer(modifier = Modifier.height(baseUnit * 0.5f))
                    }
                }


                // 1. 显示超时任务列表 (如果有的话)
                if (uiState.overdueTaskGroups.isNotEmpty()) {
                    item {
                        Spacer(modifier = Modifier.height(baseUnit * 2f))

                        CenterInformation2(
                            timePhrase = uiState.timePhrase,
                            formattedTime = uiState.formattedTime,
                            baseUnit = baseUnit,
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(baseUnit * 3f)
                        )
                        Spacer(modifier = Modifier.height(baseUnit * 0.5f))
                    }

                    uiState.overdueTaskGroups.forEach { (timeSlot, tasksInGroup) ->
                        // i. 时间段子标题
                        item {
                            Text(
                                text = timeSlot.displayName, // e.g., "早饭前"
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold,
                                modifier = Modifier.padding(bottom = baseUnit * 0.5f)
                            )
                        }
                        items(tasksInGroup) { task ->
                            Box(modifier = Modifier) {
                                PatientInformationItem(
                                    patient = task.patient,
                                    isTaken = (task.status != "待服药"),
                                    baseUnit = baseUnit,
                                    onNavigateToSubmit = {
                                        if (task.status == "待服药") {
                                            navController.navigate("photo_submit/${task.patient.patientId}/${task.timeSlot.name}")
                                        }
                                    },

                                    // ⭐ b. 实现 onNavigateToScan 回调
                                    onNavigateToScan = {
                                        if (task.status == "待服药") {
                                            navController.navigate("camera/${task.patient.patientId}/${task.timeSlot.name}")
                                        }
                                    }
                                )
                                Spacer(modifier = Modifier.height(baseUnit))
                            }
                        }
                    }
                }
            }

        }
    }
}