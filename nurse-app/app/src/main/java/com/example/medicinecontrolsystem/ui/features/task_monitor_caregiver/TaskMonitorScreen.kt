package com.example.medicinecontrolsystem.ui.features.task_monitor_caregiver

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.min
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavController


@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TaskMonitorScreen(
    navController: NavController,
    viewModel: TaskMonitorViewModel = viewModel()
) {
    val uiState by viewModel.uiState.collectAsState()

    val configuration = LocalConfiguration.current
    val screenHeight = configuration.screenHeightDp.dp
    val screenWidth = configuration.screenWidthDp.dp
    val baseUnit = min(screenHeight, screenWidth) / 40f

    Scaffold(
        topBar = {
            TopAppBar(title = { Text("任务监控") })
        }
    ) { innerPadding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(
                    brush = Brush.verticalGradient(
                        colors = listOf(Color(0xFFFFFCF7), Color(0xFFE6F1FF))
                    )
                )
        ) {
            Column(modifier = Modifier.padding(innerPadding).padding(horizontal = baseUnit * 1.5f)) {
                // 筛选器区域
                FilterChips(
                    filters = uiState.filters,
                    onTimePeriodSelected = { viewModel.onTimePeriodSelected(it) },
                    onStatusSelected = { viewModel.onStatusSelected(it) } ,
                    baseUnit = baseUnit
                )

                // 任务列表区域
                LazyColumn(
                    modifier = Modifier.weight(1f)
                ) {
                    items(uiState.filteredTaskGroups) { taskGroup ->
                        AuntieTaskGroupCard(
                            taskGroup = taskGroup,
                            navController = navController,
                            baseUnit = baseUnit
                        )
                    }
                }
            }
        }
    }
}
