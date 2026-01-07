package com.example.medicinecontrolsystem.data

data class Auntie(val auntieId: Int, val name: String, val username: String, val caregiverId: Int)

val initialAunties = listOf(
    // 阿姨一：李阿姨，ID为1，属于1号护工
    Auntie(1, "李阿姨", "auntie01", 1),

    // ⭐ 新增：阿姨二：张阿姨，ID为2，也属于1号护工
    Auntie(2, "张阿姨", "auntie02", 1)
)