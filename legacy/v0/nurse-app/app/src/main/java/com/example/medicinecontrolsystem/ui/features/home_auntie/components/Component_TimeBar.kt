package com.example.medicinecontrolsystem.ui.features.home_auntie.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Card
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.sp

@Composable
fun TimeBar(
    // --- 新的参数列表 ---
    selectedIndex: Int,
    onSelectionChanged: (Int) -> Unit, // 点击后通知上级的回调
    baseUnit: Dp,
    modifier: Modifier = Modifier
) {
    val timePeriods = listOf("早晨", "中午", "晚上", "睡前")

    Card(
        modifier = modifier,
        shape = RoundedCornerShape(baseUnit * 2f)
    ) {
        Row(
            modifier = Modifier
                .fillMaxSize()
                .background(Color.White),
            verticalAlignment = Alignment.CenterVertically
        ) {
            timePeriods.forEachIndexed { index, period ->
                TimeBarItem(
                    text = period,
                    isSelected = (selectedIndex == index),
                    onClick = { onSelectionChanged(index) },
                    baseUnit = baseUnit,
                    modifier = Modifier.weight(1f)
                )
            }
        }
    }
}

@Composable
private fun TimeBarItem(
    text: String,
    isSelected: Boolean,
    onClick: () -> Unit,
    baseUnit: Dp,
    modifier: Modifier = Modifier
) {
    val backgroundColor = if (isSelected) Color(0xFFFFD700) else Color.White
    val textColor = if (isSelected) Color.White else Color.Black

    Box(
        modifier = modifier
            .fillMaxHeight()
            .clip(RoundedCornerShape(baseUnit * 2f))
            .background(backgroundColor)
            .clickable(onClick = onClick),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = text,
            color = textColor,
            fontSize = (baseUnit.value * 1.8).sp,
            fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Normal
        )
    }
}