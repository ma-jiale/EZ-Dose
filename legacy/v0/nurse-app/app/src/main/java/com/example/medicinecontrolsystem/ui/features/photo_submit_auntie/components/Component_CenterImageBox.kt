package com.example.medicinecontrolsystem.ui.features.photo_submit_auntie.components

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Card
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.heightIn
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Info
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.TextFieldDefaults
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import com.example.medicinecontrolsystem.R
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.runtime.getValue
import androidx.compose.runtime.setValue
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.tooling.preview.Devices
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp


@Composable
fun CenterImagePart(
    baseUnit: Dp, // 添加基础单位参数
    modifier: Modifier = Modifier,
    onConfirmClick: (remark: String) -> Unit,
    onCancelClick: () -> Unit

) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFD9F0FF)),
    ) {
        Column(
            modifier = Modifier.fillMaxWidth(),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(modifier = Modifier.height(baseUnit * 4f))
            Text(
                text = "识别成功！",
                color = Color.Black,
                fontWeight = FontWeight.W600,
                fontSize = (baseUnit.value * 3).sp

            )
            Spacer(modifier = Modifier.height(baseUnit * 2f))

//            // 图片容器
//            Box(
//                modifier = Modifier
//                    .fillMaxWidth()
//                    .padding(top = baseUnit), // 相对顶部间距
//                contentAlignment = Alignment.Center
//            ) {
//                Image(
//                    painter = painterResource(R.drawable.zj03hicr),
//                    contentDescription = "taken photo",
//                    contentScale = ContentScale.Crop,
//                    modifier = Modifier
//                        .fillMaxWidth(0.9f) // 宽度占屏幕90%
//                        .aspectRatio(1.5f) // 保持1.5:1的宽高比
//                        .clip(RoundedCornerShape(baseUnit * 0.8f)) // 相对圆角
//                )
//            }

            // 按钮行
//            Row(
//                modifier = modifier
//                    .fillMaxWidth()
//                    .padding(horizontal = baseUnit * 2f) // 相对水平间距
//                    .padding(vertical = baseUnit), // 相对垂直间距
//                horizontalArrangement = Arrangement.SpaceEvenly
//            ) {
//                // 添加照片按钮
//                ActionButton(
//                    text = stringResource(R.string.adding_photo),
//                    width = baseUnit * 17f,
//                    height = baseUnit * 3.5f,
//                    backgroundColor = Color(0xFF03A9F4),
//                    textColor = Color.White,
//                    fontWeight = FontWeight.W800,
//                    baseUnit = baseUnit,
//                    fontSize = 1.6
//                )
//
//                // 重新扫描按钮
//                ActionButton(
//                    text = stringResource(R.string.scanning_again),
//                    width = baseUnit * 17f,
//                    height = baseUnit * 3.5f,
//                    backgroundColor = Color.White,
//                    textColor = Color.Black,
//                    fontWeight = FontWeight.W400,
//                    baseUnit = baseUnit,
//                    fontSize = 1.6
//                )
//            }

            // 备注输入框
            Box(
                modifier = Modifier
                    .fillMaxWidth(0.9f) // 宽度占屏幕90%
                    .padding(horizontal = baseUnit * 1f)
            ) {
                AdvancedInputField(
                    onConfirm = onConfirmClick,
                    onCancelClick = onCancelClick,
                    placeholder = "备注",
                    leadingIcon = {
                        Icon(
                            Icons.Default.Info,
                            contentDescription = "备注图标",
                            modifier = Modifier.size(baseUnit * 1.8f) // 相对图标大小
                        )
                    },
                    baseUnit = baseUnit // 传递基础单位

                )
            }
        }
    }
}

// 封装可复用的按钮组件
@Composable
fun ActionButton(
    text: String,
    width: Dp,
    height: Dp,
    backgroundColor: Color,
    textColor: Color,
    fontWeight: FontWeight,
    baseUnit: Dp, // 基础单位参数
    fontSize: Double
) {
    Card(
        shape = RoundedCornerShape(baseUnit * 2f), // 相对圆角
        modifier = Modifier
            .size(width = width, height = height)
            .padding(end = baseUnit * 0.5f), // 按钮间间距
    ) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(color = backgroundColor),
            contentAlignment = Alignment.Center
        ) {
            Text(
                text = text,
                textAlign = TextAlign.Center,
                fontSize = (baseUnit.value * fontSize).sp, // 相对字体大小
                fontWeight = fontWeight,
                color = textColor,
                maxLines = 1,
            )
        }
    }
}


@Composable
fun AdvancedInputField(
    initialValue: String = "",
    keyboardType: KeyboardType = KeyboardType.Text,
    maxLength: Int = 100,
    placeholder: String = "请输入内容...",
    leadingIcon: @Composable (() -> Unit)? = null,
    onValueChange: (String) -> Unit = {},
    onDismiss: () -> Unit = {},
    onConfirm: (String) -> Unit = {},
    onCancelClick: ()->Unit,
    baseUnit: Dp, // 添加基础单位参数
    modifier: Modifier = Modifier
) {
    var inputText by remember { mutableStateOf(initialValue) }
    var isError by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf("") }
    val focusRequester = remember { FocusRequester() }

    Column(
        modifier = modifier
            .background(Color(0xFFD9F0FF))
    ) {
        OutlinedTextField(
            value = inputText,
            onValueChange = {
                if (it.length <= maxLength) {
                    inputText = it
                    isError = false
                    onValueChange(it)
                } else {
                    isError = true
                    errorMessage = "超过最大长度 ($maxLength 字符)"
                }
            },
            modifier = Modifier
                .fillMaxWidth()
                .focusRequester(focusRequester)
                .heightIn(min = baseUnit * 20f)
                .background(color = Color(0xFFD9F0FF), RoundedCornerShape(baseUnit * 0.4f)), // 文本框内部背景（白色）
            singleLine = keyboardType != KeyboardType.Text,
            keyboardOptions = KeyboardOptions(keyboardType = keyboardType),
            isError = isError,
            colors = TextFieldDefaults.colors( // 关键修改：设置文本区域背景色
                unfocusedContainerColor = Color.White,
                focusedContainerColor = Color.White,
                errorContainerColor = Color.White
            ),
            shape = MaterialTheme.shapes.medium, // 保持默认形状
            label = {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    leadingIcon?.invoke()
                    if (leadingIcon != null) {
                        Spacer(modifier = Modifier.width(baseUnit * 0.5f))
                    }
                    Text(
                        text = placeholder,
                        color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f),
                        fontSize = (baseUnit.value * 1.3).sp // 相对字体大小
                    )
                }
            },
            placeholder = {
                Text(
                    text = "在此输入$placeholder...",
                    color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.4f)
                )
            },
            supportingText = {
                // 移除Column背景设置，直接显示文本
                if (isError) {
                    Text(
                        text = errorMessage,
                        color = MaterialTheme.colorScheme.error,
                        modifier = Modifier.fillMaxWidth()
                    )
                } else {
                    Text(
                        text = "${inputText.length}/$maxLength",
                        modifier = Modifier.fillMaxWidth()
                    )
                }
            }
        )
        // 按钮区域
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(top = baseUnit), // 相对顶部间距
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Button(
                onClick = {onConfirm(inputText)},
                modifier = Modifier
                    .size(width = baseUnit *18f,height = baseUnit * 4f),
                shape = RoundedCornerShape(baseUnit * 2f),
                colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFFFD700))
            ) {
                Box(
                    modifier = Modifier.fillMaxSize(), // 让 Box 占满整个 Button 的内容区域
                    contentAlignment = Alignment.Center // 强制将 Box 内部的任何东西都居中
                ) {
                    Text(
                        text = "确定",
                        color = Color.White,
                        fontWeight = FontWeight.W600,
                        fontSize = (baseUnit.value * 2).sp
                    )
                }
            }

            Spacer(modifier = Modifier.height(baseUnit * 0.75f))

            // 取消按钮
            Button(
                onClick = onCancelClick,
                modifier = Modifier
                    .size(width = baseUnit *18f,height = baseUnit * 4f),
                shape = RoundedCornerShape(baseUnit * 2f),
                colors = ButtonDefaults.buttonColors(containerColor = Color.White)
            ) {
                Box(
                    modifier = Modifier.fillMaxSize(), // 让 Box 占满整个 Button 的内容区域
                    contentAlignment = Alignment.Center // 强制将 Box 内部的任何东西都居中
                ) {
                    Text(
                        text = "取消",
                        color = Color(0xFFFFD700),
                        fontWeight = FontWeight.W600,
                        fontSize = (baseUnit.value * 2).sp
                    )
                }
            }


            Spacer(modifier = Modifier.padding(baseUnit))
        }
    }

//    LaunchedEffect(Unit) {
//        focusRequester.requestFocus()
//    }
}
@Preview(
    device = Devices.PIXEL_3A,
    showSystemUi = true,
    showBackground = true
)
@Composable
fun CenterImagePartPreview(){
}