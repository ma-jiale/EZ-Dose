package com.example.medicinecontrolsystem.ui

import android.os.Build
import android.os.Bundle
import android.view.WindowManager
import android.widget.Button
import androidx.appcompat.app.AppCompatActivity
import com.example.medicinecontrolsystem.R

class TimeoutWarningActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // ⭐ 让Activity可以在锁屏之上显示、点亮屏幕
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O_MR1) {
            setShowWhenLocked(true)
            setTurnScreenOn(true)
        } else {
            window.addFlags(
                WindowManager.LayoutParams.FLAG_SHOW_WHEN_LOCKED or
                        WindowManager.LayoutParams.FLAG_TURN_SCREEN_ON or
                        WindowManager.LayoutParams.FLAG_ALLOW_LOCK_WHILE_SCREEN_ON
            )
        }

        setContentView(R.layout.activity_timeout)

        // 找到按钮并设置点击事件
        val dismissButton: Button = findViewById(R.id.dismiss_button)
        dismissButton.setOnClickListener {
            // 点击后，关闭当前Activity
            finish()
        }
    }
}