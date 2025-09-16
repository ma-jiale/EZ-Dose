//// file: data/CsvDataManager.kt
//package com.example.medicinecontrolsystem.data
//
//import android.content.Context
//import com.example.medicinecontrolsystem.respository.AppRepository
//import java.io.File
//import java.io.IOException
//import java.text.SimpleDateFormat
//import java.util.Date             // 确保导入
//import java.util.Locale
//
//data class TaskState(
//    val status: String,
//    val completionTime: String?,
//    val remark: String? // 新增备注字段
//)
//
///**
// * 负责将App的动态状态数据持久化到CSV文件。
// * 这是一个专门处理文件读写的“数据管家”。
// */
//class CsvDataManager(context: Context) {
//
//    /**
//     * 定义我们用来存储病人状态的CSV文件名和它在App私有目录中的位置
//     */
//
//    private val filesDir = context.filesDir // 先把目录存起来
//
//    // ⭐ 2. 新增一个私有函数，用于获取当天的文件名
//    private fun getTodaysStateFile(): File {
//        // a. 定义日期的格式，例如 "yyyy-MM-dd" 会得到 "2025-08-07"
//        val dateFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())
//        // b. 获取当前日期并格式化
//        val todayDateString = dateFormat.format(Date())
//        // c. 拼接成完整的文件名，例如 "patient_states_2025-08-07.csv"
//        val fileName = "patient_states_$todayDateString.csv"
//        // d. 返回一个指向这个文件的 File 对象
//        return File(filesDir, fileName)
//    }
//    // --- 私有辅助函数 ---
//
//
//    /**
//     *  将一个状态条目（ID, 状态, 时间）转换为CSV的一行字符串
//     */
//
//    private fun stateToCsvLine(taskKey: Pair<Int, TimeSlot>, state: TaskState): String {
//        val id = taskKey.first
//        val timeSlotName = taskKey.second.name // 使用枚举的名称作为唯一标识
//        val status = state.status
//        val completionTime = state.completionTime ?: ""
//        val remark = state.remark?.replace("\"","\"\"") ?:""
//
//        return "$id,$timeSlotName,$status,$completionTime,\"$remark\""
//    }
//
//    /**
//     * 从CSV的一行字符串解析出状态条目
//     */
//
//    private fun csvLineToState(csvLine: String): Pair<Pair<Int, TimeSlot>, TaskState>? {
//        return try {
//            val fields = csvLine.split(",", limit = 5)
//            //将一行文本按逗号分割为一个字符串列表
//            if (fields.size == 5) {  //如果分割的段数为4
//                val id = fields[0].toInt()
//                val timeSlotName = fields[1]
//                val timeSlot = AppRepository.findTimeSlotByName(timeSlotName)
//                // 3. 如果找到了，才继续执行
//                if (timeSlot != null) {
//                    val status = fields[2]
//                    val time = if (fields[3].isNotBlank()) fields[3] else null
//                    val remark = if (fields[4].isNotBlank()) fields[4].removeSurrounding("\"")
//                        .replace("\"\"", "\"") else null
//                    val taskState = TaskState(status, time, remark)
//                    Pair(Pair(id, timeSlot), taskState)
//                }else{
//                    null
//                }
//            } else {
//                null // 如果格式不正确（字段数不对），则忽略此行
//            }
//        } catch (e: Exception) {
//            null // 如果解析出错（比如ID不是数字），则忽略此行
//        }
//    }
//
//    // --- 公共接口方法 ---
//
//    /**
//     * 将字符串写入到CSV文件中
//     * */
//    fun saveTaskStates(states: Map<Pair<Int, TimeSlot>, TaskState>) {
//        try {
//            // 1. 将Map中的每一项转换为CSV行
//            // 2. 用换行符将所有行连接成一个大的字符串
//            val csvData = states.entries.joinToString("\n") { (taskKey, state) ->
//                stateToCsvLine(taskKey, state)
//            }
//            // 3. 将整个字符串一次性写入文件
//            getTodaysStateFile().writeText(csvData)
//        } catch (e: IOException) {
//            // 在真实应用中，这里应该有更完善的错误日志记录
//            e.printStackTrace()
//        }
//    }
//
//    /**
//     * 从CSV文件加载所有病人的状态。
//     * 如果文件不存在（App首次启动），会自动根据initialPatients创建一个默认的状态文件。
//     * @return 一个Map，Key是病人ID，Value是包含状态和问题时间的Pair。
//     */
//    fun loadTaskStates(): Map<Pair<Int, TimeSlot>, TaskState> {
//        val todaysFile = getTodaysStateFile()
//        // 检查文件是否存在
//        // ⭐ 核心修改：如果文件不存在，就直接返回一个空的Map
//        if (!todaysFile.exists()) {
//            return emptyMap()
//        }
//
//        // 如果文件存在，正常读取
//        return try {
//            todaysFile.readLines()
//                .mapNotNull { csvLineToState(it) }
//                .toMap()
//        } catch (e: IOException) {
//            e.printStackTrace()
//            emptyMap()
//        }
////        if (!getTodaysStateFile().exists()) {
////            // App首次启动，文件不存在
////            // 为所有初始病人创建一个默认的“待服药”状态
////            val defaultStates = initialSchedules.associate { scheduleLink ->
////                val taskKey = Pair(scheduleLink.patientId, scheduleLink.timeSlot)
////                val defaultState = TaskState("待服药", null,null)
////                taskKey to defaultState
////            }
////            // 将这个默认状态立即保存到新创建的文件中
////            saveTaskStates(defaultStates)
////            // 返回这个默认状态
////            return defaultStates
////        }
////
////        // 如果文件已存在，则读取并解析其内容
////        return try {
////            getTodaysStateFile().readLines() // 读取文件的每一行
////                .mapNotNull { csvLineToState(it) } // 对每一行进行解析，并过滤掉解析失败的行
////                .toMap() // 将解析成功的(ID, StatePair)列表转换为Map
////        } catch (e: IOException) {
////            e.printStackTrace()
////            emptyMap() // 如果读取文件时发生IO错误，返回一个空的Map作为安全降级
////        }
//    }
//}