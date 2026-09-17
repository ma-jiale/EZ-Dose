using System;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
#endif

namespace EZDose.Diagnostics
{
    /// <summary>
    /// Windows 独立运行版本 (Standalone .exe) 实时控制台终端日志管理器。
    /// 在打包出来的 Windows 应用程序启动时自动弹出独立的控制台窗口（CMD Terminal），
    /// 实时输出 Unity Debug.Log 及 EZLog 的所有彩色调试日志。
    /// </summary>
    public static class WindowsConsoleManager
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleTitle(string lpConsoleTitle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool DeleteMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        private const int STD_INPUT_HANDLE = -10;
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;
        private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;
        private const uint SC_CLOSE = 0xF060;
        private const uint MF_BYCOMMAND = 0x00000000;

        private static bool isConsoleAllocated = false;
        private static readonly object logLock = new object();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void InitializeConsole()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                // 检查启动参数中是否包含 --noconsole 禁用终端
                string[] args = Environment.GetCommandLineArgs();
                foreach (var arg in args)
                {
                    if (string.Equals(arg, "--noconsole", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "-noconsole", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                // 1. 分配并打开 Windows 控制台
                if (!AllocConsole())
                {
                    return;
                }
                isConsoleAllocated = true;

                // 2. 设置 UTF-8 编码 (CP 65001)，彻底防止中文药名、患者名乱码
                SetConsoleOutputCP(65001);
                Console.OutputEncoding = Encoding.UTF8;

                // 3. 重定向标准输出与错误流
                IntPtr stdOutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                SafeFileHandle safeOut = new SafeFileHandle(stdOutHandle, true);
                FileStream fsOut = new FileStream(safeOut, FileAccess.Write);
                StreamWriter swOut = new StreamWriter(fsOut, Encoding.UTF8) { AutoFlush = true };
                Console.SetOut(swOut);
                Console.SetError(swOut);

                // 4. 禁用 QuickEdit 模式（防止鼠标左键点击终端导致 Unity 游戏主线程卡死挂起）
                IntPtr stdInHandle = GetStdHandle(STD_INPUT_HANDLE);
                if (GetConsoleMode(stdInHandle, out uint inMode))
                {
                    inMode &= ~ENABLE_QUICK_EDIT_MODE;
                    inMode |= ENABLE_EXTENDED_FLAGS;
                    SetConsoleMode(stdInHandle, inMode);
                }

                // 5. 设置终端标题
                SetConsoleTitle("EZ-Dose 智能分药系统 - 实时调试终端 (Live Debug Console)");

                // 6. 禁用控制台的 'X' 关闭按钮（防止测试人员误点关闭导致整个软件直接退出崩溃）
                IntPtr hwnd = GetConsoleWindow();
                if (hwnd != IntPtr.Zero)
                {
                    IntPtr hMenu = GetSystemMenu(hwnd, false);
                    if (hMenu != IntPtr.Zero)
                    {
                        DeleteMenu(hMenu, SC_CLOSE, MF_BYCOMMAND);
                    }
                }

                // 7. 打印启动横幅
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("================================================================================");
                Console.WriteLine("        EZ-Dose 智能分药管理系统 - Windows 实时调试终端 (Live Log)");
                Console.WriteLine("================================================================================");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"[系统启动] 启动时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Unity版本: {Application.unityVersion}");
                Console.WriteLine($"[日志路径] {Path.Combine(Application.persistentDataPath, "Logs")}");
                Console.WriteLine("--------------------------------------------------------------------------------\n");
                Console.ResetColor();

                // 8. 注册 Unity 全局日志监听器
                Application.logMessageReceivedThreaded += OnLogMessageReceived;
                Application.quitting += OnApplicationQuitting;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WindowsConsoleManager] 初始化 Windows 控制台失败: {ex.Message}");
            }
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!isConsoleAllocated) return;

            lock (logLock)
            {
                ConsoleColor prevColor = Console.ForegroundColor;

                switch (type)
                {
                    case LogType.Error:
                    case LogType.Exception:
                    case LogType.Assert:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(condition);
                        if (!string.IsNullOrEmpty(stackTrace))
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine(stackTrace);
                        }
                        break;

                    case LogType.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(condition);
                        break;

                    case LogType.Log:
                        // 识别 EZLog 模块标签并高亮
                        if (condition.Contains("[EROR]"))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                        else if (condition.Contains("[WARN]"))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                        }
                        else if (condition.Contains("[INFO]"))
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        else if (condition.Contains("[DEBG]") || condition.Contains("[VERB]"))
                        {
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                        Console.WriteLine(condition);
                        break;
                }

                Console.ForegroundColor = prevColor;
            }
        }

        private static void OnApplicationQuitting()
        {
            if (isConsoleAllocated)
            {
                try
                {
                    Application.logMessageReceivedThreaded -= OnLogMessageReceived;
                    FreeConsole();
                    isConsoleAllocated = false;
                }
                catch { }
            }
        }
#endif
    }
}
