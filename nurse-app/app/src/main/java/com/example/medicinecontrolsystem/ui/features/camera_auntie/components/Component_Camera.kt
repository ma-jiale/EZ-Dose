package com.example.medicinecontrolsystem.ui.features.camera_auntie.components

import android.media.AudioManager
import android.media.ToneGenerator
import android.util.Log
import androidx.annotation.OptIn
import androidx.camera.core.CameraControl
import androidx.camera.core.CameraSelector
import androidx.camera.core.ExperimentalGetImage
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.ImageProxy
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.FlashlightOff
import androidx.compose.material.icons.filled.FlashlightOn
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalLifecycleOwner
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import com.google.mlkit.vision.barcode.BarcodeScanning
import com.google.mlkit.vision.common.InputImage
import java.util.concurrent.Executors

// 顶部提示
@Composable
fun TopHintText() {
    Text(
        text = "请将桌面条形码与药盒条形码同时放入框内",
        style = MaterialTheme.typography.titleMedium,
        color = Color.Black,
        modifier = Modifier.padding(16.dp)
    )
}

// 手电按钮
@Composable
fun FlashlightToggleButton(
    isFlashOn: Boolean,
    onToggle: () -> Unit
) {
    IconButton(onClick = onToggle) {
        Icon(
            imageVector = if (isFlashOn) Icons.Default.FlashlightOn else Icons.Default.FlashlightOff,
            contentDescription = "闪光灯",
            tint = if (isFlashOn) Color.Yellow else Color.Gray,
            modifier = Modifier.size(40.dp)
        )
    }
}

// 相机预览
@Composable
fun CameraPreviewBox(
    isFlashOn: Boolean,
    onCameraControlReady: (CameraControl) -> Unit,
    onBarcodesScanned: (List<String>) -> Unit
) {
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    val cameraProviderFuture = remember { ProcessCameraProvider.getInstance(context) }

    AndroidView(
        factory = { ctx ->
            val previewView = androidx.camera.view.PreviewView(ctx).apply {
                scaleType = androidx.camera.view.PreviewView.ScaleType.FILL_CENTER
            }

            cameraProviderFuture.addListener({
                val cameraProvider = cameraProviderFuture.get()
                val preview = Preview.Builder().build().also {
                    it.setSurfaceProvider(previewView.surfaceProvider)
                }

                val imageAnalysis = ImageAnalysis.Builder()
                    .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
                    .build()
                    .also {
                        it.setAnalyzer(Executors.newSingleThreadExecutor(), BarcodeAnalyzer(onBarcodesScanned))
                    }

                val camera = cameraProvider.bindToLifecycle(
                    lifecycleOwner,
                    CameraSelector.DEFAULT_BACK_CAMERA,
                    preview,
                    imageAnalysis
                )

                onCameraControlReady(camera.cameraControl)
                camera.cameraControl.enableTorch(isFlashOn)

            }, ContextCompat.getMainExecutor(context))

            previewView
        },
        modifier = Modifier
            .fillMaxWidth()
            .aspectRatio(3f / 4f)
    )
}

// 条码分析器
class BarcodeAnalyzer(
    private val onScanned: (List<String>) -> Unit
) : ImageAnalysis.Analyzer {
    //时间戳变量，用于节流
    private var lastAnalyzedTimestamp = 0L

    private val scanner = BarcodeScanning.getClient()

    @OptIn(ExperimentalGetImage::class)
    override fun analyze(imageProxy: ImageProxy) {
        //如果距离上次分析成功的时间小于500毫秒，则直接跳过，但必须关闭imageProxy
        val currentTimestamp = System.currentTimeMillis()
        if(currentTimestamp - lastAnalyzedTimestamp < 500L){
            imageProxy.close()
            return
        }

        val mediaImage = imageProxy.image
        if (mediaImage != null) {
            val inputImage =
                InputImage.fromMediaImage(mediaImage, imageProxy.imageInfo.rotationDegrees)
            scanner.process(inputImage)
                .addOnSuccessListener { barcodes ->
                    val codes = barcodes.mapNotNull { it.rawValue?.trim() }
                    val uniqueCodes = codes.distinct()

                    val uniquePositions = barcodes
                        .mapNotNull { it.boundingBox }
                        .distinctBy { it.centerY() to it.centerX() }

                    val finalCodes = when {
                        uniqueCodes.size == 1 && uniquePositions.size >= 2 -> listOf(uniqueCodes[0], uniqueCodes[0])
                        uniqueCodes.size >= 2 -> uniqueCodes.take(2)
                        else -> uniqueCodes
                    }

                    if (finalCodes.isNotEmpty()) {
                        lastAnalyzedTimestamp = currentTimestamp
                        onScanned(finalCodes)
                    }
                }
                .addOnFailureListener { Log.e("Barcode", "识别失败", it) }
                .addOnCompleteListener { imageProxy.close() }
        } else {
            imageProxy.close()
        }
    }
}


// 提示音
fun playBeep() {
    try {
        val toneGen = ToneGenerator(AudioManager.STREAM_MUSIC, 100)
        toneGen.startTone(ToneGenerator.TONE_PROP_BEEP, 150)
    } catch (e: Exception) {
        Log.e("Beep", "无法播放提示音", e)
    }
}