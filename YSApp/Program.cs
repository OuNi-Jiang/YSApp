using System;
using System.Threading;
using System.Windows.Forms;

namespace YSApp
{
    internal static class Program
    {
        // 定义唯一的互斥体名称（建议使用公司/产品唯一标识，避免冲突）
        private const string MutexName = "YSApp_Unique_Mutex_20251218";
        private static Mutex _mutex;

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew = false; // 标记是否是首次创建互斥体（即是否是第一个实例）

            try
            {
                // 创建互斥体，设置为全局（跨会话），并标记是否新创建
                // MutexSecurity 可选：解决权限问题（如管理员/普通用户启动冲突）
                _mutex = new Mutex(true, MutexName, out createdNew);

                if (createdNew)
                {
                    // 首次启动：正常运行程序
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new YSForm());
                }
                else
                {
                    // 已有实例运行：提示用户并退出
                    MessageBox.Show(
                        "程序已在运行中，无法重复启动！",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    // 可选：激活已运行的窗口（进阶功能，见下方扩展）
                    // ActivateExistingInstance();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // 权限不足（如已用管理员权限启动，再次普通启动）
                MessageBox.Show(
                    $"程序已在运行（权限限制）：{ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch (Exception ex)
            {
                // 其他异常
                MessageBox.Show(
                    $"启动失败：{ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                // 释放互斥体（确保程序退出时释放资源）
                if (_mutex != null && createdNew)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                }
            }
        }

        // 【进阶扩展】激活已运行的程序窗口（可选）
        // 需要引入：using System.Runtime.InteropServices;
        // private static void ActivateExistingInstance()
        // {
        //     IntPtr hwnd = FindWindow(null, "YSForm"); // 替换为你的窗体标题
        //     if (hwnd != IntPtr.Zero)
        //     {
        //         // 显示并激活窗口
        //         ShowWindow(hwnd, 9); // SW_RESTORE = 9
        //         SetForegroundWindow(hwnd);
        //     }
        // }

        // // 导入Windows API（用于激活窗口）
        // [DllImport("user32.dll")]
        // private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // [DllImport("user32.dll")]
        // private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // [DllImport("user32.dll")]
        // private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}