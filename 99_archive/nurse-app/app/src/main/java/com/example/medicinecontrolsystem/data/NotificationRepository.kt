//package com.example.medicinecontrolsystem.data
//
//import android.content.Context
//
//// 使用 'object' 关键字创建一个单例，方便在各处直接调用
//object NotificationRepository {
//
//    /**
//     * 计算在指定的时间段，当前登录的阿姨【所有】的任务数量。
//     *
//     * @param context 上下文，用于访问 SessionManager.
//     * @param timeSlot 要查询的时间段.
//     * @return 返回计算出的病人总任务数量。
//     */
//    fun getPatientCountForTimeSlot(context: Context, timeSlot: TimeSlot): Int {
//        // 1. 获取当前登录的用户信息
//        val sessionManager = SessionManager(context)
//        val loggedInUser = sessionManager.getSession()
//
//        // 如果没有用户登录，或者登录的不是阿姨，则任务数为0
//        if (loggedInUser == null || loggedInUser.role != UserRole.AUNTIE) {
//            return 0
//        }
//        val currentAuntieId = loggedInUser.id
//
//        // 2. 找出这位阿姨负责的所有病人的ID
//        val myPatientIds = patients
//            .filter { it.auntieId == currentAuntieId }
//            .map { it.patientId }
//            .toSet()
//
//        // 3. 从总的用药计划中，筛选出属于这位阿姨和这个时间段的任务
//        val count = initialSchedules.count { scheduleLink ->
//            scheduleLink.timeSlot == timeSlot && scheduleLink.patientId in myPatientIds
//        }
//
//        return count
//    }
//
//    /**
//     * 【新增函数】计算在指定时间段，当前登录的阿姨【未完成】的任务数量。
//     * 这是超时警告Worker需要使用的函数。
//     *
//     * @param context 上下文，用于访问 SessionManager 和 CsvDataManager.
//     * @param timeSlot 要查询的时间段.
//     * @return 返回计算出的未完成任务的数量。
//     */
//    fun getPendingTaskCountForTimeSlot(context: Context, timeSlot: TimeSlot): Int {
//        // 1. 获取当前登录的用户信息
//        val sessionManager = SessionManager(context)
//        val loggedInUser = sessionManager.getSession()
//        if (loggedInUser == null || loggedInUser.role != UserRole.AUNTIE) {
//            return 0
//        }
//        val currentAuntieId = loggedInUser.id
//
//        // 2. 找出这位阿姨在这个时间段的所有任务所对应的病人ID
//        val myPatientIds = patients
//            .filter { it.auntieId == currentAuntieId }
//            .map { it.patientId }
//            .toSet()
//
//        val myTaskPatientIds = initialSchedules
//            .filter { it.timeSlot == timeSlot && it.patientId in myPatientIds }
//            .map { it.patientId }
//            .toSet()
//
//        // 如果这个时间段本来就没有任务，直接返回0
//        if (myTaskPatientIds.isEmpty()) {
//            return 0
//        }
//
//        // 3. 从当天的CSV文件中加载所有任务的【当前状态】
//        val csvDataManager = CsvDataManager(context)
//        val allTaskStates = csvDataManager.loadTaskStates()
//
//        // 4. 计算这些任务中有多少个的状态是“待服药”
//        val pendingCount = myTaskPatientIds.count { patientId ->
//            val taskKey = Pair(patientId, timeSlot)
//            // 如果状态是"待服药"，或者在Map里根本找不到（也算作待办），则计数
//            val currentState = allTaskStates[taskKey]?.status
//            currentState == "待服药" || currentState == null
//        }
//
//        return pendingCount
//    }
//}