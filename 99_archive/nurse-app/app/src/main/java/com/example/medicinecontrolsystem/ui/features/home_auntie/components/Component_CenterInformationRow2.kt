package com.example.medicinecontrolsystem.ui.features.home_auntie.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.sp
import com.example.medicinecontrolsystem.R

@Composable
fun CenterInformation2(
    // --- 新的参数列表 ---
    timePhrase: String,
    formattedTime: String,
    baseUnit: Dp,
    modifier: Modifier = Modifier
) {
    Row(
        modifier = modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(
            text = stringResource(R.string.overTasks),
            fontSize = (baseUnit.value * 1.8).sp,
            fontWeight = FontWeight.W600
        )
    }
}