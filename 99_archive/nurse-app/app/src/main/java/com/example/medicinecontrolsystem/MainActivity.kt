@file:Suppress("NAME_SHADOWING")

package com.example.medicinecontrolsystem


import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import android.util.Log

import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.animation.AnimatedContentTransitionScope
import androidx.compose.animation.core.tween

import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.ui.Modifier
import androidx.compose.runtime.Composable
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.SideEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.ContextCompat
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import com.example.medicinecontrolsystem.data.TimeSlot
import com.example.medicinecontrolsystem.data.UserRole
import com.example.medicinecontrolsystem.ui.features.camera_auntie.CameraScreen
import com.example.medicinecontrolsystem.ui.features.home_auntie.HomeViewModel
import com.example.medicinecontrolsystem.ui.theme.MedicineControlSystemTheme
import com.example.medicinecontrolsystem.ui.features.home_auntie.HomeScreen
import com.example.medicinecontrolsystem.ui.features.home_caregiver.CaregiverHomeScreen
import com.example.medicinecontrolsystem.ui.features.home_caregiver.CaregiverViewModel
import com.example.medicinecontrolsystem.ui.features.photo_submit_auntie.PhotoSubmittingScreen
import com.example.medicinecontrolsystem.ui.features.login.LoginScreen
import com.example.medicinecontrolsystem.ui.features.login.LoginViewModel
import com.example.medicinecontrolsystem.ui.features.task_monitor_caregiver.TaskMonitorScreen
import com.example.medicinecontrolsystem.ui.features.task_monitor_caregiver.TaskMonitorViewModel
import com.example.medicinecontrolsystem.workers.TaskScheduler
import kotlinx.coroutines.launch
import android.Manifest
import androidx.lifecycle.lifecycleScope
import com.example.medicinecontrolsystem.respository.AppRepository
import kotlinx.coroutines.delay


class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // ⭐⭐⭐ b. 在这里，启动一个与Activity生命周期绑定的协程来初始化Repository ⭐⭐⭐
        lifecycleScope.launch {
            // 将 applicationContext 传进去，因为它需要用 Context 来查找 drawable 资源
            AppRepository.initialize(applicationContext)

            TaskScheduler.scheduleAllTasks(applicationContext)

            while (true) {
                // d. 设置轮询间隔，比如3秒
                delay(3 * 1000L)

                val refreshSuccess = AppRepository.refreshAllData(applicationContext)

                // ⭐⭐⭐ 核心修复：在这里增加重新调度的逻辑 ⭐⭐⭐
                // c. 如果数据刷新成功了
                if (refreshSuccess) {
                    // d. 就【再次】调用任务调度器
                    Log.d("MainActivity", "后台数据已刷新，正在重新调度所有通知任务...")
                    TaskScheduler.scheduleAllTasks(applicationContext)

                }
            }

        }

        setContent {
            MedicineControlSystemTheme {

                // a. 检查当前是否是 Android 13+
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                    val context = LocalContext.current // 获取当前 Composable 的上下文

                    // b. 创建一个 state 来追踪权限状态
                    var hasNotificationPermission by remember {
                        mutableStateOf(
                            ContextCompat.checkSelfPermission(
                                context,
                                Manifest.permission.POST_NOTIFICATIONS
                            ) == PackageManager.PERMISSION_GRANTED
                        )
                    }

                    // c. 创建权限请求启动器
                    val permissionLauncher = rememberLauncherForActivityResult(
                        contract = ActivityResultContracts.RequestPermission(),
                        onResult = { isGranted ->
                            hasNotificationPermission = isGranted
                        }
                    )

                    // d. 使用 SideEffect 在每次重组时检查并请求权限
                    //    SideEffect 适用于那些不需要在协程中运行的 Compose 副作用
                    SideEffect {
                        if (!hasNotificationPermission) {
                            permissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
                        }
                    }
                }

                //创建一个总的导航控制器，负责所有顶层页面的跳转
                val navController = rememberNavController()

                //获取LoginViewModel
                val loginViewModel: LoginViewModel = viewModel()

                //订阅LoginViewModel的状态
                val loginState by loginViewModel.uiState.collectAsState()

                //使用Launched Effect监听登录和退出事件
                LaunchedEffect(loginState.loggedInUserRole,loginState.logoutRequested) {
                    if(loginState.loggedInUserRole != null){
                        val userId = loginState.loggedInUserId
                        val destination = when (loginState.loggedInUserRole){
                            UserRole.AUNTIE -> "auntie_app/$userId"
                            UserRole.CAREGIVER -> "caregiver_app/$userId"
                            else -> "null"
                        }
                        navController.navigate(destination){
                            popUpTo("login"){inclusive = true}
                        }
                        loginViewModel.onNavigationComplete()
                    }

                    if(loginState.logoutRequested){
                        navController.navigate("login"){
                            popUpTo(0){inclusive = true}
                        }
                        loginViewModel.onNavigationComplete()
                    }
                }

                NavHost(navController = navController, startDestination = "login") {
                    composable("login") {
                        LoginScreen(viewModel = loginViewModel)
                    }
                    composable("auntie_app/{auntieId}") {backStackEntry ->
                        val auntieId = backStackEntry.arguments?.getString("auntieId")?.toIntOrNull() ?: -1
                        if (auntieId != -1){
                            AuntieAppMain(onLogout = {loginViewModel.logout()}, auntieId = auntieId)
                        }
                    }
                    composable("caregiver_app/{caregiverId}") {backStackEntry ->
                        val caregiverId = backStackEntry.arguments?.getString("caregiverId")?.toIntOrNull() ?: -1
                        if (caregiverId != -1) {
                            CaregiverAppMain(onLogout = { loginViewModel.logout() }, caregiverId = caregiverId)
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun AuntieAppMain(
    onLogout:() -> Unit,  //需要登出时调用
    auntieId: Int,  //当前登录的阿姨的ID

){
    /***************
     * 第一部分：初始化
     **************/

    //创建一个导航控制器，管理阿姨端内部的页面跳转
    // rememberNavController() 确保在屏幕旋转等重组操作后，这个控制器还是同一个实例。
    val navController = rememberNavController()

    //获取HomeViewModel的实例
    //NavHost内部所有页面都能获取到这同一个实例，实现数据共享
    val homeViewModel: HomeViewModel = viewModel()

    //定义底部导航栏要显示的条目列表
    val auntieNavItems = listOf(
        NavItem("首页", R.drawable.ic_home,"home")
    )

    //在 auntieId 第一次被传入或发生变化时，执行一次里面的代码块，当阿姨登录后，立即命令 ViewModel 去加载这位阿姨的数据。
    LaunchedEffect(auntieId) {
        homeViewModel.loadDataForAuntie(auntieId)
    }


    /***************
     * 第二部分：UI布局
     ***************/

    //Scaffold 是一个实现了基本 Material Design 布局结构的组件，提供了放置顶部栏、底部栏、浮动按钮等内容的标准位置
    Scaffold(
        //指定了屏幕底部的组件
        bottomBar = {
            // 获取当前导航堆栈的最新条目，这是一个状态(State)，当它变化时会触发重组。
            val navBackStackEntry by navController.currentBackStackEntryAsState()
            //从条目中获取当前页面的路由
            val currentRoute = navBackStackEntry?.destination?.route
            // 只有当当前页面不是 "photo_submit"、"camera"时，
            // 才显示底部导航栏
            if (currentRoute?.startsWith("photo_submit") != true && currentRoute?.startsWith("camera") != true) {
                BottomNavBar(
                    navController = navController,
                    navItems = auntieNavItems
                )
            }
        }
    ) { innerPadding ->  //Scaffold 的内容区域，innerPadding 是为避开顶部/底部栏留出的安全边距。

        /****************
         * 第三部分：导航核心
         ***************/
        //NavHost 是一个容器，负责根据 navController 的状态显示不同的页面
        NavHost(
            navController = navController, //关联之前创建的导航控制器
            startDestination = "home",  //一进来默认界面是home
            modifier = Modifier.padding(innerPadding) //应用安全边距
        ) {
            //定义第一个页面home
            composable(
                route = "home",
                enterTransition = {
                    slideIntoContainer(
                        towards = AnimatedContentTransitionScope.SlideDirection.Left,
                        animationSpec = tween(300)
                    )
                },
                exitTransition = {
                    slideOutOfContainer(
                        towards = AnimatedContentTransitionScope.SlideDirection.Left,
                        animationSpec = tween(300)
                    )
                },
                popEnterTransition = {
                    slideIntoContainer(
                        towards = AnimatedContentTransitionScope.SlideDirection.Right,
                        animationSpec = tween(300)
                    )
                },
                popExitTransition = {
                    slideOutOfContainer(
                        towards = AnimatedContentTransitionScope.SlideDirection.Right,
                        animationSpec = tween(300))
                }
            ) {
                // 当导航到 "home" 时，这里的内容会被显示出来。
                // 调用 HomeScreen 这个Composable，并将 navController 和 viewModel 传递给它。
                HomeScreen(
                    navController = navController,
                    homeViewModel = homeViewModel,
                    onLogout = onLogout
                 )
            }

            //定义第二个界面：camera/{patientId}
            composable(
                route = "camera/{patientId}/{timeSlotName}",
                enterTransition = {
                    slideIntoContainer(
                        towards = AnimatedContentTransitionScope.SlideDirection.Left,
                        animationSpec = tween(300)
                    )
                },
                exitTransition = {
                    slideOutOfContainer(
                        towards = AnimatedContentTransitionScope.SlideDirection.Left,
                        animationSpec = tween(300)
                    )
                },
                popEnterTransition = {
                    slideIntoContainer(
                        towards = AnimatedContentTransitionScope.SlideDirection.Right,
                        animationSpec = tween(300)
                    )
                },
                popExitTransition = {
                    slideOutOfContainer(
                        towards = AnimatedContentTransitionScope.SlideDirection.Right,
                        animationSpec = tween(300))
                }
            ) { backStackEntry-> //backStackEntry 包含了当前页面的所有信息，包括参数。
                //从 backStackEntry 中获取名为 "patientId" 的参数，并转为Int。
                val patientId = backStackEntry.arguments?.getString("patientId")?.toIntOrNull()
                val timeSlotName = backStackEntry.arguments?.getString("timeSlotName")
                // 调用 CameraScreen 这个Composable。
                CameraScreen(navController = navController, patientId = patientId, timeSlotName = timeSlotName)
            }

            //定义第三个界面：photo_submit/{patientId}
            composable(
                route = "photo_submit/{patientId}/{timeSlotName}",
                enterTransition = {
                    slideIntoContainer(
                        towards = AnimatedContentTransitionScope.SlideDirection.Left,
                        animationSpec = tween(300)
                    )
                },
                exitTransition = {
                    slideOutOfContainer(
                        towards = AnimatedContentTransitionScope.SlideDirection.Left,
                        animationSpec = tween(300)
                    )
                },
                popEnterTransition = {
                    slideIntoContainer(
                        towards = AnimatedContentTransitionScope.SlideDirection.Right,
                        animationSpec = tween(300)
                    )
                },
                popExitTransition = {
                    slideOutOfContainer(
                        towards = AnimatedContentTransitionScope.SlideDirection.Right,
                        animationSpec = tween(300)
                    )
                }
            ){ backStackEntry->

                val TAG = "MedSystemDebug"

                val patientId = backStackEntry.arguments?.getString("patientId")?.toIntOrNull()
                val timeSlotName = backStackEntry.arguments?.getString("timeSlotName")
                // 从共享的 homeViewModel 中订阅 UI 状态。
//                val uiState by homeViewModel.uiState.collectAsState()
                // 获取一个与当前 Composable 绑定的协程作用域，用于安全地启动协程
                val scope = rememberCoroutineScope()
                // 确保 patientId 是有效的
                if (patientId != null&& timeSlotName != null) {
                    // 调用 PhotoSubmittingScreen Composable
                    PhotoSubmittingScreen(
                        navController = navController,
                        patientId = patientId,
                        viewModel = viewModel(),
                        onConfirmClick = { confirmedPatientId, remarkText ->
                            val correctTimeSlot = AppRepository.findTimeSlotByName(timeSlotName)
                            if (correctTimeSlot != null){
                                Log.d(TAG, "onConfirmClick: 确认按钮被点击")

                                // ⭐ 4. 【新增】在执行操作前，检查时间段是否有效
                                scope.launch {

                                    Log.d(TAG, "onConfirmClick Coroutine: 协程开始")

                                    Log.d(
                                        TAG,
                                        "onConfirmClick Coroutine: 准备调用并等待 markPatientAsTaken..."
                                    )

                                    // 命令 ViewModel 执行“标记为已服药”的操作，
                                    // 并且使用 .join() 等待这个后台任务彻底完成。
                                    homeViewModel.markPatientAsTaken(
                                        patientId = confirmedPatientId,
                                        timeSlot = correctTimeSlot,
                                        remark = remarkText
                                    )
                                    Log.d(TAG, "onConfirmClick Coroutine: markPatientAsTaken 执行完毕.")

                                    Log.d(TAG, "onConfirmClick Coroutine: 准备执行导航 popBackStack...")

                                    // 操作完成后返回主页
                                    navController.popBackStack("home", inclusive = false)
                                }





                                Log.d(TAG, "onConfirmClick Coroutine: 导航 popBackStack 已调用(如果还能看到这行说明没在这里崩).")
                            }




                        }
//                            else {
//                                // 这是一个保险措施，如果因为某些原因没获取到时间段，就只返回，不执行操作
//                                navController.popBackStack()
//                            }
                    )
                }
            }
        }
    }
}

@Composable
fun CaregiverAppMain(onLogout:() -> Unit, caregiverId: Int){
    val caregiverNavController = rememberNavController()
    val caregiverHomeViewModel: CaregiverViewModel = viewModel()
    val taskMonitorViewModel: TaskMonitorViewModel = viewModel()


    val caregiverNavItems = listOf(
        NavItem("首页", R.drawable.ic_home, "caregiver_home"),
        NavItem("任务", R.drawable.ic_record,"caregiver_monitor")
    )
    LaunchedEffect(caregiverId) {
        caregiverHomeViewModel.loadDataForCaregiver(caregiverId)
        taskMonitorViewModel.loadTasksForCaregiver(caregiverId)
    }

    Scaffold(
        bottomBar = {
            val navBackStackEntry by caregiverNavController.currentBackStackEntryAsState()
            val currentRoute = navBackStackEntry?.destination?.route
            BottomNavBar(
                navController = caregiverNavController,
                navItems = caregiverNavItems
            )
        }

    ){ innerPadding ->
        NavHost(
            navController = caregiverNavController,
            startDestination = "caregiver_home",
            modifier = Modifier.padding(innerPadding)
        ){
            //护工端的主页
            composable("caregiver_home"){
                CaregiverHomeScreen(
                    navController = caregiverNavController,
                    caregiverViewModel = caregiverHomeViewModel,
                    onLogout = onLogout
                )

            }

            //护工端管理页
            composable("caregiver_monitor"){
                 TaskMonitorScreen(
                     navController = caregiverNavController,
                     viewModel = taskMonitorViewModel
                 )
            }
        }

    }
}
