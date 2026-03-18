using UnityEngine;
using UnityEngine.UI;

namespace EZDose.PillCounter
{
    /// <summary>
    /// 药片计数器测试脚本
    /// 用于快速测试药片计数功能
    /// </summary>
    public class PillCounterTest : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private KeyCode captureBackgroundKey = KeyCode.B;
        [SerializeField] private KeyCode resetBackgroundKey = KeyCode.R;
        
        [Header("UI参考（可选）")]
        [SerializeField] private Text debugText;
        
        private PillCounterController controller;
        
        void Start()
        {
            // 获取或添加PillCounterController
            controller = GetComponent<PillCounterController>();
            
            if (controller == null)
            {
                EZLog.E(EZLog.Module.PillCount, "PillCounterController component not found");
                return;
            }
            
            LogDebug("药片计数器测试已启动");
            LogDebug($"按 {captureBackgroundKey} 手动捕捉背景");
            LogDebug($"按 {resetBackgroundKey} 重置背景");
        }
        
        void Update()
        {
            if (controller == null) return;
            
            // 快捷键控制
            if (Input.GetKeyDown(captureBackgroundKey))
            {
                controller.CaptureBackground();
                LogDebug("手动捕捉背景");
            }
            
            if (Input.GetKeyDown(resetBackgroundKey))
            {
                controller.ResetBackground();
                LogDebug("重置背景");
            }
            
            // 显示实时信息
            if (debugText != null)
            {
                int pillCount = controller.GetCurrentPillCount();
                bool bgCaptured = controller.IsBackgroundCaptured();
                
                debugText.text = $"药片计数测试\n" +
                                $"背景状态: {(bgCaptured ? "已捕捉" : "未捕捉")}\n" +
                                $"当前计数: {pillCount}\n\n" +
                                $"快捷键:\n" +
                                $"{captureBackgroundKey} - 捕捉背景\n" +
                                $"{resetBackgroundKey} - 重置背景";
            }
        }
        
        private void LogDebug(string message)
        {
            EZLog.D(EZLog.Module.PillCount, $"[PillCounterTest] {message}");
        }
    }
}
