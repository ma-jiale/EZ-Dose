package com.example.medicinecontrolsystem.ui.features.task_monitor_caregiver

import android.util.Log
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp
import androidx.navigation.NavController
import androidx.compose.foundation.Image // 确保导入
import androidx.compose.ui.draw.clip
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.painterResource // 确保导入
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import coil.compose.AsyncImage
import coil.request.ImageRequest
import com.example.medicinecontrolsystem.R
import com.example.medicinecontrolsystem.network.RetrofitInstance


// --- 筛选器组件 ---
@Composable
fun FilterChips(
    baseUnit: Dp,
    filters: TaskFilters,  //参数1：当前的筛选状态
    onTimePeriodSelected: (String) -> Unit,   //参数2：当时间段被选择时的回调函数
    onStatusSelected: (String) -> Unit,   //参数3：当状态被选择时的回调函数
    modifier: Modifier = Modifier
) {
    val timePeriods = listOf("全部", "早饭前", "早饭后", "午饭前","午饭后", "晚饭前", "晚饭后")
    val statuses = listOf("全部", "已服药", "待服药")

    Column(modifier = modifier.padding(horizontal = baseUnit * 0.5f, vertical = baseUnit * 0.5f)) {
        // 时间段筛选
        // 第一行：显示前4个选项
        Text("时间段", fontWeight = FontWeight.Bold, modifier = Modifier.width(baseUnit * 8f))

        Row(horizontalArrangement = Arrangement.spacedBy(baseUnit * 0.5f)) {
            timePeriods.take(4).forEach { period ->
                FilterChip(
                    label = period,
                    isSelected = filters.selectedTimePeriod == period,
                    onClick = { onTimePeriodSelected(period) }
                )
            }
        }

        Spacer(modifier = Modifier.height(baseUnit * 0.5f))

        // 第二行：显示剩下的选项
        Row(horizontalArrangement = Arrangement.spacedBy(baseUnit * 0.5f)) {
            timePeriods.drop(4).forEach { period ->
                FilterChip(
                    label = period,
                    isSelected = filters.selectedTimePeriod == period,
                    onClick = { onTimePeriodSelected(period) }
                )
            }
        }
        Spacer(modifier = Modifier.height(baseUnit * 0.5f))
        // 状态筛选
        Text("状态", fontWeight = FontWeight.Bold, modifier = Modifier.width(baseUnit * 4f))
        Row(verticalAlignment = Alignment.CenterVertically) {
            Row(horizontalArrangement = Arrangement.spacedBy(baseUnit * 0.5f)) {
                statuses.forEach { status ->
                    FilterChip(
                        label = status,
                        isSelected = filters.selectedStatus == status,
                        onClick = { onStatusSelected(status) }
                    )
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun FilterChip(label: String, isSelected: Boolean, onClick: () -> Unit) {
    //AssistChip是Material3提供的一个现成的Chip组件
    AssistChip(
        onClick = onClick,
        label = { Text(label) },
        colors = AssistChipDefaults.assistChipColors(
            containerColor = if (isSelected) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.surfaceVariant,
            labelColor = if (isSelected) MaterialTheme.colorScheme.onPrimary else MaterialTheme.colorScheme.onSurfaceVariant
        ),
        border = null
    )
}

// --- 阿姨任务分组卡片 ---
@Composable
fun AuntieTaskGroupCard(
    baseUnit: Dp,
    taskGroup: AuntieTaskGroup,   // 参数1: 这个卡片需要显示的数据 (一个阿姨的所有任务信息)
    navController: NavController,
    modifier: Modifier = Modifier
) {
    Column(modifier = Modifier.fillMaxWidth()) {
        Text(
            text = "阿姨：${taskGroup.auntieName}",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold
        )
//        Text(
//            text = "共 ${taskGroup.totalTasks} 位老人, 已完成 ${taskGroup.completedTasks} 位",
//            style = MaterialTheme.typography.bodyMedium,
//            color = Color.Gray
//        )
        Spacer(modifier = Modifier.height(baseUnit * 0.5f))

        // 任务列表
        Column(verticalArrangement = Arrangement.spacedBy(baseUnit * 0.5f)) {
            taskGroup.patientTasks.forEach { patientTask ->
                PatientTaskItem(baseUnit, task = patientTask, navController = navController)
            }
        }
    }
}

// --- 单个病人任务项 ---
@Composable
fun PatientTaskItem(
    baseUnit: Dp,
    task: PatientTask,
    navController: NavController,
    modifier: Modifier = Modifier
) {
    val imageUrl = "${RetrofitInstance.BASE_URL}static/images_patients/${task.patient.imageResourceId}"

    Card(
        modifier = modifier.fillMaxWidth()
            .height(baseUnit * 8f),
        shape = RoundedCornerShape(baseUnit * 1.5f),
    ) {
        Row(
            modifier = Modifier
                .fillMaxSize()
                .background(color = Color.White),
            verticalAlignment = Alignment.CenterVertically
        ) {
            AsyncImage(
                model = ImageRequest.Builder(LocalContext.current)
                    .data(imageUrl)
                    .crossfade(true)
                    .listener(
                        onError = { request, result ->
                            Log.e("CoilError", "Image load failed: ${request.data}", result.throwable)
                        }
                    )
                    .build(),
                contentDescription = task.patient.patientName,
                modifier = Modifier.size(baseUnit * 6f).clip(RoundedCornerShape(baseUnit * 0.5f)),
                // (可选) 添加占位符和错误图片
                placeholder = painterResource(id = R.drawable.placeholder_image),
                error = painterResource(id = R.drawable.error_image)
            )

            Column(modifier = Modifier.weight(1f).padding(start = baseUnit)) {
                Text(
                    text = task.patient.patientName, // <-- 使用病人自己的名字
                    fontWeight = FontWeight.W600,
                    fontSize = (baseUnit.value * 1.8).sp
                )
                Spacer(modifier = Modifier.height(baseUnit * 0.3f))

                Text(
                    text = task.patient.patientBedNumber, // <-- 同样使用 stringResource
                    fontWeight = FontWeight.W400,
                    fontSize = (baseUnit.value * 1.8).sp
                )
            }

            Spacer(modifier = Modifier.width(baseUnit * 1f))

            // 状态标签
            StatusTag(status = task.status, baseUnit)
        }
    }
}

@Composable
private fun StatusTag(status: String,baseUnit: Dp) {

    val (backgroundColor, textColor) = when (status) {
        "已服药" -> Color(0xFFE3F8E4) to Color(0xFF4CAF50)
        "待服药" -> Color(0xFFE0E0E0) to Color(0xFF616161)
        else -> Color.Gray to Color.White
    }
    Column(
        horizontalAlignment = Alignment.End,
        modifier = Modifier
            .padding(end = baseUnit)
            .defaultMinSize(minWidth = baseUnit * 8f) // 保证最小宽度，防止文字换行
    ) {
        Card(
            shape = RoundedCornerShape(baseUnit),
            modifier = Modifier.size(width = baseUnit * 8f, height = baseUnit * 2.7f),
        ) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(backgroundColor)
                    .padding(horizontal = baseUnit * 0.5f),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = status,
                    textAlign = TextAlign.Center,
                    fontSize = (baseUnit.value * 1.6).sp,
                    color = Color.White,
                    maxLines = 1,
                    fontWeight = FontWeight.Bold
                )
            }
        }
    }


}