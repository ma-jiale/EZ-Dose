package com.example.medicinecontrolsystem.ui.features.home_auntie.components

import android.util.Log
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Create
import androidx.compose.material.icons.rounded.KeyboardArrowDown
import androidx.compose.material.icons.rounded.KeyboardArrowUp
import androidx.compose.material.icons.rounded.PlayArrow
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
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
import androidx.navigation.NavController
import coil.compose.AsyncImage
import coil.request.ImageRequest
import com.example.medicinecontrolsystem.R
import com.example.medicinecontrolsystem.data.TimeSlot
import com.example.medicinecontrolsystem.data.data_Patient // 确保导入的是你的数据类
import com.example.medicinecontrolsystem.network.RetrofitInstance
import com.example.medicinecontrolsystem.ui.features.task_monitor_caregiver.PatientTask

/**
 * 负责显示整个病人信息列表的容器组件。
 * @param navController 用于处理导航事件。
 * @param patients 从ViewModel获取的病人基础信息列表。
 * @param patientStates 从ViewModel获取的病人实时用药状态Map。
 */
//@Composable
//fun PatientInformationList(
//    navController: NavController?,
//    tasks: List<PatientTask>,
//    patientStates: Map<Pair<Int, TimeSlot>, String>, //所有任务的状态
//    baseUnit: Dp,
//    modifier: Modifier = Modifier,
//    onNavigateToSubmit: (task: PatientTask) -> Unit,
//    onNavigateToScan: (task: PatientTask) -> Unit
//) {
//    LazyColumn(
//        modifier = modifier,
//        verticalArrangement = Arrangement.spacedBy(baseUnit * 0.5f),
//
//    ) {
//        items(tasks) { task ->
//            // 1. 从 task 对象中获取 patientId 和 timeSlot
//            val patientId = task.patient.patientId
//            val timeSlot = task.timeSlot
//
//            // 2. 使用这两个值来构建 taskKey
//            val taskKey = Pair(patientId, timeSlot)
//
//            // 3. 后续逻辑保持不变
//            val state = patientStates[taskKey] ?: "待服药"
//            val isTaken = (state == "已服药")
//
//            PatientInformationItem(
//                patient = task.patient,
//                isTaken = isTaken,
//                baseUnit = baseUnit,
//                onNavigateToSubmit = { onNavigateToSubmit(task) },
//                onNavigateToScan = { onNavigateToScan(task) }
//            )
//        }
//    }
//}

/**
 * 负责渲染列表中单个病人信息的UI组件。
 * @param patient 该列表项对应的病人数据。
 * @param isTaken 根据实时状态计算出的布尔值，表示是否已服药。
 * @param navController 用于处理内部按钮的导航。
 */
@Composable
fun PatientInformationItem(
    patient: data_Patient,
    isTaken: Boolean,
    onNavigateToSubmit: () -> Unit,
    onNavigateToScan: () -> Unit,
    baseUnit: Dp
) {
    // 使用 remember 管理单个列表项的展开状态
    var expanded by remember { mutableStateOf(false) }
    val imageUrl = "${RetrofitInstance.BASE_URL}static/images_patients/${patient.imageResourceId}"

    Column(modifier = Modifier.fillMaxWidth()) {
        // Card1 - 主卡片，始终可见
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .height(baseUnit * 8f),
            shape = RoundedCornerShape(baseUnit),
        ) {
            Row(
                modifier = Modifier
                    .fillMaxSize()
                    .background(color = Color.White),
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
                    contentDescription = patient.patientName,
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
                        text = patient.patientName,
                        fontWeight = FontWeight.W600,
                        fontSize = (baseUnit.value * 1.8).sp
                    )
                    Spacer(modifier = Modifier.height(baseUnit * 0.3f))
                    Text(
                        text = patient.patientBedNumber,
                        fontWeight = FontWeight.W400,
                        fontSize = (baseUnit.value * 1.8).sp
                    )
                }

                // 用药状态卡片
                Card(
                    shape = RoundedCornerShape(baseUnit),
                    modifier = Modifier.size(width = baseUnit * 8f, height = baseUnit * 2.7f),
                ) {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .background(
                                color = if (isTaken) Color(0xFFFFD700) else Color(0xFF989898)
                            ),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = stringResource(
                                if (isTaken) R.string.have_taking_medicine
                                else R.string.not_taking_medicine
                            ),
                            textAlign = TextAlign.Center,
                            fontSize = (baseUnit.value * 1.8).sp,
                            color = Color.White
                        )
                    }
                }

                // 展开/折叠图标按钮
                IconButton(onClick = { expanded = !expanded }) {
                    Icon(
                        imageVector = if (expanded) Icons.Rounded.KeyboardArrowUp else Icons.Rounded.KeyboardArrowDown,
                        contentDescription = "Expand",
                        modifier = Modifier.padding(horizontal = baseUnit * 0.5f)
                    )
                }
            }
        }

        // Card2 - 可展开的卡片，带有动画效果
        AnimatedVisibility(visible = expanded) {
            Box(
                modifier = Modifier.fillMaxWidth(),
                contentAlignment = Alignment.CenterEnd
            ){
                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(baseUnit * 3.5f),
                    shape = RoundedCornerShape(baseUnit),
                ) {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .background(color = Color(0xFFD9F0FF)),
                        contentAlignment = Alignment.Center
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .align(Alignment.Center),
                            horizontalArrangement = Arrangement.SpaceEvenly,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            // “扫描核对”按钮
                            Row(
                                verticalAlignment = Alignment.CenterVertically,
                                modifier = Modifier.clickable { onNavigateToScan() }
                            ) {
                                Text(
                                    text = stringResource(R.string.scanning_check),
                                    fontSize = (baseUnit.value * 1.8).sp,
                                    fontWeight = FontWeight.W400,
                                    modifier = Modifier.padding(end = baseUnit * 0.5f)
                                )
                                Icon(
                                    Icons.Rounded.PlayArrow,
                                    contentDescription = null,
                                    modifier = Modifier.size(baseUnit * 2f)
                                )
                            }
                        }
                    }
            }

            }
        }
    }
}