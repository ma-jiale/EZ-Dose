package com.example.medicinecontrolsystem.ui.features.home_auntie.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.Card
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.sp
import androidx.navigation.NavController
import com.example.medicinecontrolsystem.R

@Composable
fun TopInformationCard(
    // --- 新的参数列表，只接收原始数据 ---
    auntieName: String,
    timePhrase: String,
    formattedTime: String,
    completedTasks: Int,
    totalTasks: Int,
    navController: NavController,
    baseUnit: Dp,
    modifier: Modifier = Modifier
) {
    Card(modifier = modifier) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(
                    brush = Brush.horizontalGradient(
                        colors = listOf(Color(0xFFD9F0FF), Color(0xFFF2F7FB))
                    )
                )
                .padding(baseUnit * 1.5f)
        ) {
            Column(
                modifier = Modifier.fillMaxSize(),
                verticalArrangement = Arrangement.SpaceBetween
            ) {
                // 顶部：问候语和时间
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "$auntieName ${stringResource(R.string.greeting)}",
                        fontSize = (baseUnit.value * 2.5).sp,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        text = formattedTime,
                        fontSize = (baseUnit.value * 3.5).sp,
                        fontWeight = FontWeight.Bold
                    )
                }

                // 底部：任务进度
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.Bottom
                ) {
                    Column {
                        Text(
                            text = stringResource(R.string.current_task_progress),
                            fontSize = (baseUnit.value * 1.8).sp,
                            color = Color.Gray
                        )
                        Spacer(modifier = Modifier.height(baseUnit * 0.5f))
                        Text(
                            text = "$completedTasks / $totalTasks",
                            fontSize = (baseUnit.value * 2.8).sp,
                            fontWeight = FontWeight.Bold
                        )
                    }

                    // 这里可以放一个去往“提醒”页面的按钮，如果需要的话
                    // Button(onClick = { navController.navigate("reminder") }) { ... }
                }
            }
        }
    }
}