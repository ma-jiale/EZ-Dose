package com.example.medicinecontrolsystem.ui.features.login

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.example.medicinecontrolsystem.data.UserRole

@Composable
fun LoginScreen(
    viewModel: LoginViewModel = viewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    var showNetworkDialog by remember { mutableStateOf(false) }

    Box(
        modifier = Modifier.fillMaxSize(),
        contentAlignment = Alignment.Center
    ) {
        Column(
            modifier = Modifier.padding(32.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            Text("欢迎使用", style = MaterialTheme.typography.headlineSmall)
            Spacer(modifier = Modifier.height(48.dp))

            // 用户名输入框
            OutlinedTextField(
                value = uiState.usernameInput,
                onValueChange = { viewModel.onUsernameChange(it) },
                label = { Text("用户名") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )
            Spacer(modifier = Modifier.height(16.dp))

            // 密码输入框
            OutlinedTextField(
                value = uiState.passwordInput,
                onValueChange = { viewModel.onPasswordChange(it) },
                label = { Text("密码") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true,
                visualTransformation = PasswordVisualTransformation()
            )
            Spacer(modifier = Modifier.height(24.dp))

            // 登录按钮
            Button(
                onClick = { viewModel.login() },
                modifier = Modifier.fillMaxWidth(),
                enabled = !uiState.isLoading
            ) {
                if (uiState.isLoading) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(24.dp),
                        color = MaterialTheme.colorScheme.onPrimary
                    )
                } else {
                    Text("登录")
                }
            }

            // 错误提示
            if (uiState.loginError != null) {
                Spacer(modifier = Modifier.height(16.dp))
                Text(
                    text = uiState.loginError!!,
                    color = MaterialTheme.colorScheme.error
                )
            }

            // 网络设置成功消息
            if (uiState.networkMessage != null) {
                Spacer(modifier = Modifier.height(16.dp))
                Text(
                    text = uiState.networkMessage!!,
                    color = MaterialTheme.colorScheme.primary
                )
            }
        }

        // 网络设置按钮 - 右下角
        FloatingActionButton(
            onClick = { showNetworkDialog = true },
            modifier = Modifier
                .align(Alignment.BottomEnd)
                .padding(16.dp),
            containerColor = MaterialTheme.colorScheme.secondary
        ) {
            Icon(
                imageVector = Icons.Default.Settings,
                contentDescription = "网络设置"
            )
        }
    }

    // 网络设置对话框
    if (showNetworkDialog) {
        NetworkSettingsDialog(
            currentUrl = viewModel.getCurrentServerUrl(),
            onDismiss = { showNetworkDialog = false },
            onConfirm = { newUrl ->
                viewModel.updateServerUrl(newUrl)
                showNetworkDialog = false
            }
        )
    }
}

@Composable
private fun NetworkSettingsDialog(
    currentUrl: String,
    onDismiss: () -> Unit,
    onConfirm: (String) -> Unit
) {
    var serverUrl by remember { mutableStateOf(currentUrl) }
    var isValidUrl by remember { mutableStateOf(true) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = {
            Row(
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(
                    imageVector = Icons.Default.Settings,
                    contentDescription = null,
                    modifier = Modifier.padding(end = 8.dp)
                )
                Text("网络设置")
            }
        },
        text = {
            Column {
                Text(
                    text = "当前服务器地址:",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(bottom = 8.dp)
                )

                OutlinedTextField(
                    value = serverUrl,
                    onValueChange = {
                        serverUrl = it
                        isValidUrl = isValidServerUrl(it)
                    },
                    label = { Text("服务器URL") },
                    placeholder = { Text("http://192.168.1.100:5050") },
                    isError = !isValidUrl,
                    modifier = Modifier.fillMaxWidth()
                )

                if (!isValidUrl) {
                    Text(
                        text = "请输入有效的URL地址",
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall,
                        modifier = Modifier.padding(top = 4.dp)
                    )
                }

                Text(
                    text = "示例: http://192.168.1.100:5050",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(top = 8.dp)
                )
            }
        },
        confirmButton = {
            TextButton(
                onClick = {
                    if (isValidUrl && serverUrl.isNotBlank()) {
                        onConfirm(serverUrl)
                    }
                },
                enabled = isValidUrl && serverUrl.isNotBlank()
            ) {
                Text("保存")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("取消")
            }
        }
    )
}

private fun isValidServerUrl(url: String): Boolean {
    return try {
        if (url.isBlank()) return false
        val pattern = Regex("^https?://[\\w.-]+(:\\d+)?/?.*$")
        pattern.matches(url)
    } catch (e: Exception) {
        false
    }
}
