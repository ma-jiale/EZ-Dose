package com.example.medicinecontrolsystem.ui.features.home_caregiver.components

import android.util.Log
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.defaultMinSize
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.AsyncImage
import coil.request.ImageRequest
import com.example.medicinecontrolsystem.R
import com.example.medicinecontrolsystem.network.RetrofitInstance
import com.example.medicinecontrolsystem.ui.features.task_monitor_caregiver.PatientTask


@Composable
fun UpcomingTimeoutTasksPatientItem(
    task: PatientTask, // 它接收一个 PatientTask 对象
    baseUnit: Dp,
    modifier: Modifier = Modifier
) {
    val imageUrl = "${RetrofitInstance.BASE_URL}static/images_patients/${task.patient.imageResourceId}"

    Card(
        modifier = modifier
            .fillMaxWidth()
            .height(baseUnit * 8f), // 固定的卡片高度
        shape = RoundedCornerShape(baseUnit),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxSize(),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // 病人头像
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

            // 病人姓名和床位
            Column(
                modifier = Modifier
                    .weight(1f)
                    .padding(start = baseUnit)
            ) {
                Text(
                    text = task.patient.patientName,
                    fontWeight = FontWeight.W600,
                    fontSize = (baseUnit.value * 1.8).sp
                )
                Spacer(modifier = Modifier.height(baseUnit * 0.3f))
                Text(
                    text = task.patient.patientBedNumber,
                    fontWeight = FontWeight.W400,
                    fontSize = (baseUnit.value * 1.8).sp
                )
            }

            // 右侧的状态信息
            Column(
                horizontalAlignment = Alignment.End,
                modifier = Modifier
                    .padding(end = baseUnit)
                    .defaultMinSize(minWidth = baseUnit * 8f)
            ) {
                // 在顶部显示具体是哪个时间段超时了
                Text(
                    text = task.timeSlot.displayName,
                    fontSize = (baseUnit.value * 1.5).sp,
                    color = Color(0xFFFF6A00),
                    fontWeight = FontWeight.Bold
                )
                Spacer(modifier = Modifier.height(baseUnit * 0.2f))

                // “任务即将超时”的橙色状态卡片
                Card(
                    shape = RoundedCornerShape(baseUnit),
                    modifier = Modifier.size(width = baseUnit * 8f, height = baseUnit * 2.7f),
                ) {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .background(Color(0xFFFF6A00))
                            .padding(horizontal = baseUnit * 0.5f),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = stringResource(id = R.string.task_upcomingtimeout),
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
    }
}

@Composable
fun UpcomingTimeoutTasksCard(
    tasks: List<PatientTask>,
    baseUnit: Dp,
    onViewAllClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    if (tasks.isEmpty()) return

    Column(
        modifier = modifier.fillMaxWidth()
    ) {
        // 1. 标题行
        Row(
            verticalAlignment = Alignment.CenterVertically,
            modifier = Modifier.padding(bottom = baseUnit * 0.5f)
        ) {
            Icon(
                imageVector = Icons.Default.Warning,
                contentDescription = "超时",
                tint = Color(0xFFFF6A00)
            )
            Spacer(modifier = Modifier.width(baseUnit * 0.5f))
            Text(
                "即将超时 (${tasks.size}项)",
                color = Color(0xFFFF6A00),
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
            )
            Spacer(modifier = Modifier.weight(1f))
//            Text("查看全部", modifier = Modifier.clickable { onViewAllClick() }, color = Color.Gray)
        }

        // 2. 即将超时任务列表
        //    用一个 Column 来排列所有的 UpcomingTimeoutTasksPatientItem
        Column(verticalArrangement = Arrangement.spacedBy(baseUnit * 0.5f)) {
            tasks.forEach { task ->
                UpcomingTimeoutTasksPatientItem(
                    task = task,
                    baseUnit = baseUnit
                )
            }
        }
    }
}
