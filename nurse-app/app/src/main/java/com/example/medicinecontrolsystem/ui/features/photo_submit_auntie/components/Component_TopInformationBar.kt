package com.example.medicinecontrolsystem.ui.features.photo_submit_auntie.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.tooling.preview.Devices
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.medicinecontrolsystem.R

import com.example.medicinecontrolsystem.data.data_Patient


@Composable
fun TopInformationBarPageSubmitting(
    patient: data_Patient,
    baseUnit: Dp, // 添加基础单位参数
    modifier: Modifier = Modifier
) {
    Row(
        modifier = modifier
            .fillMaxWidth()
            .padding(horizontal = baseUnit * 1.5f) // 相对水平间距
            .padding(bottom = baseUnit * 0.5f), // 添加底部间距
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Row(
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = patient.patientName,
                fontWeight = FontWeight.W800,
                fontSize = (baseUnit.value * 1.6).sp // 相对字体大小
            )
            Spacer(modifier = Modifier.width(baseUnit * 0.3f))
            Text(
                text = stringResource(R.string.whose_medicine_box),
                fontWeight = FontWeight.W400,
                fontSize = (baseUnit.value * 1.6).sp // 相对字体大小
            )
        }

        Row(
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = patient.patientBedNumber,
                fontWeight = FontWeight.W400,
                fontSize = (baseUnit.value * 1.6).sp, // 相对字体大小
                modifier = Modifier.align(Alignment.CenterVertically)
            )
        }
    }
}
@Composable
fun TopInformationBarP(modifier:Modifier = Modifier){
    Row(
        modifier = modifier
            .fillMaxWidth()
            .padding(horizontal = 20.dp),
        horizontalArrangement = Arrangement.SpaceBetween
    ){
        Row{
            Text(
                text = "王小花",
                fontWeight = FontWeight.W800,
                fontSize = 20.sp
            )
            Text(
                text = stringResource(R.string.whose_medicine_box),
                fontWeight = FontWeight.W400,
                fontSize = 20.sp
            )
        }

        Row(){
            Text(
                text = "床号 615",
                fontWeight = FontWeight.W400,
                fontSize = 20.sp,
                modifier = Modifier.align(Alignment.CenterVertically)
            )
        }
    }
}

@Preview(
    device = Devices.PIXEL_3A,
    showSystemUi = true,
    showBackground = true
)
@Composable
fun TopInformationBarPreview(){
    TopInformationBarP()
}