using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace YSApp
{
    public partial class YSForm : Form
    {
        public YSForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 源目录
        /// </summary>
        string YMLStr = ConfigurationManager.AppSettings["YML"];
        /// <summary>
        /// 目标目录
        /// </summary>
        string MBMLStr = ConfigurationManager.AppSettings["MBML"];
        /// <summary>
        /// 是否删除源文件：true代表删除、false代表不删除
        /// </summary>
        string IsDeleteStr = ConfigurationManager.AppSettings["IsDelete"];
        /// <summary>
        /// 压缩包已存在时，是否覆盖：1代表覆盖
        /// </summary>
        string IsCoverStr = ConfigurationManager.AppSettings["IsCover"];

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void YSForm_Load(object sender, System.EventArgs e)
        {
            ReadLoad();
        }

        /// <summary>
        /// 读取加载基本配置
        /// </summary>
        public void ReadLoad()
        {
            YMLTextBox.Text = YMLStr;
            MBMLTextBox.Text = MBMLStr;

            //是否启动定时执行：1已启动
            var IsStar = ConfigurationManager.AppSettings["IsStar"];
            if (IsStar == "1")
            {
                timer1.Enabled = true;
                //StartPauseBtn.Text = "已启动";
            }
            else
            {
                timer1.Enabled = false;
                //StartPauseBtn.Text = "已停止";
            }
            //多久执行一次（分钟）
            var time = ConfigurationManager.AppSettings["TimeCount"];
            timer1.Interval = Convert.ToInt32(time) * 1000 * 60;// * 60
        }

        /// <summary>
        /// 加载RichTextBox内容
        /// </summary>
        /// <param name="richText"></param>
        /// <param name="message"></param>
        public void LoadRichTextBox(RichTextBox richText, object message)
        {
            //richText.Enabled = true;
            string s = message + "\n";
            richText.AppendText(s);
            richText.SelectionStart = richText.TextLength;
            int startIndex = Math.Max(0, richText.TextLength - s.Length); // 确保 startIndex 不小于 0
            richText.Select(startIndex, s.Length);
            if (s.IndexOf("成功") >= 0 || s.IndexOf("OK") >= 0 || s.IndexOf("已删除") >= 0 || s.IndexOf("提示") >= 0)
                richText.SelectionColor = System.Drawing.Color.Green;
            else if (s.IndexOf("错误") >= 0 || s.IndexOf("失败") >= 0)
                richText.SelectionColor = System.Drawing.Color.Red;
            else
                richText.SelectionColor = System.Drawing.Color.Black;
            richText.ScrollToCaret(); //将控件的内容滚动到当前插入符号位置
            //richText.Enabled = false;
        }


        #region 核心压缩方法
        /// <summary>
        /// 压缩文件/文件夹到指定Zip包
        /// </summary>
        /// <param name="sourcePath">源文件/文件夹路径</param>
        /// <param name="targetZipPath">目标Zip包路径</param>
        /// <returns>是否成功</returns>
        private bool CompressFiles(string sourcePath, string targetZipPath)
        {
            bool compressSuccess = false;
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath) && !Directory.Exists(sourcePath))
            {
                //Log("源路径不存在或为空");
                LoadRichTextBox(richTextBox1, "源路径不存在或为空"); //加载RichTextBox内容
                return false;
            }

            try
            {
                // 确保目标目录存在
                string targetDir = Path.GetDirectoryName(targetZipPath);
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                // 如果Zip包已存在，删除或覆盖
                if (File.Exists(targetZipPath))
                {
                    if (IsCoverStr == "1")
                    {
                        File.Delete(targetZipPath);
                        //Log($"已删除已存在的压缩包：{targetZipPath}");
                        LoadRichTextBox(richTextBox1, $"已删除已存在的压缩包：{targetZipPath}"); //加载RichTextBox内容
                    }
                    else
                    {
                        //Log("压缩包已存在且未勾选覆盖，任务终止");
                        LoadRichTextBox(richTextBox1, "压缩包已存在且未设置覆盖，任务终止"); //加载RichTextBox内容
                        return false;
                    }
                }

                // 压缩文件夹
                if (Directory.Exists(sourcePath))
                {
                    ZipFile.CreateFromDirectory(sourcePath, targetZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
                    //Log($"成功压缩文件夹：{sourcePath} 到 {targetZipPath}");
                    LoadRichTextBox(richTextBox1, $"成功压缩文件夹：{sourcePath} 到 {targetZipPath}"); //加载RichTextBox内容
                    compressSuccess = true;
                }
                // 压缩单个文件
                else if (File.Exists(sourcePath))
                {
                    using (ZipArchive archive = ZipFile.Open(targetZipPath, ZipArchiveMode.Create))
                    {
                        archive.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath), CompressionLevel.Optimal);
                    }
                    //Log($"成功压缩文件：{sourcePath} 到 {targetZipPath}");
                    LoadRichTextBox(richTextBox1, $"成功压缩文件：{sourcePath} 到 {targetZipPath}"); //加载RichTextBox内容
                    compressSuccess = true;
                }

                // 压缩成功后删除源文件/文件夹
                if (compressSuccess && IsDeleteStr == "true")
                {
                    DeleteSource(sourcePath);
                }
                return compressSuccess;
            }
            catch (Exception ex)
            {
                //Log($"压缩失败：{ex.Message}");
                LoadRichTextBox(richTextBox1, $"压缩失败：{ex.Message}"); //加载RichTextBox内容
                return false;
            }
        }
        #endregion

        /// <summary>
        /// 删除文件/文件夹
        /// </summary>
        /// <param name="path">文件/文件夹路径</param>
        /// <returns>是否删除成功</returns>
        private bool DeleteSource(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    // 删除文件夹（包含所有子文件和子文件夹）
                    Directory.Delete(path, true);
                    LoadRichTextBox(richTextBox1, $"已删除源文件夹：{path}");
                }
                else if (File.Exists(path))
                {
                    // 删除单个文件
                    File.Delete(path);
                    LoadRichTextBox(richTextBox1, $"已删除源文件：{path}");
                }
                else
                {
                    LoadRichTextBox(richTextBox1, $"源路径不存在，无需删除：{path}");
                }
                return true;
            }
            catch (Exception ex)
            {
                LoadRichTextBox(richTextBox1, $"删除源文件/文件夹失败：{path}，错误：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 立即执行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LJZXBtn_Click(object sender, EventArgs e)
        {
            BatchCompress();
        }

        /// <summary>
        /// 定时执行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            BatchCompress();
        }

        /// <summary>
        /// 压缩方法
        /// </summary>
        private void YSFF()
        {
            CompressFiles(YMLStr, MBMLStr);
        }

        /// <summary>
        /// 遍历源目录，批量压缩所有文件/子文件夹
        /// </summary>
        private void BatchCompress()
        {
            string sourceDir = YMLTextBox.Text.Trim(); // 读取界面最新的源目录
            string targetDir = MBMLTextBox.Text.Trim(); // 读取界面最新的目标目录

            // 校验源目录
            if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
            {
                LoadRichTextBox(richTextBox1, $"【错误】源目录不存在或为空：{sourceDir}");
                return;
            }

            // 校验目标目录
            if (string.IsNullOrEmpty(targetDir))
            {
                LoadRichTextBox(richTextBox1, "【错误】目标目录为空");
                return;
            }
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
                LoadRichTextBox(richTextBox1, $"【提示】已创建目标目录：{targetDir}");
            }

            LoadRichTextBox(richTextBox1, DateTime.Now + "========== 开始批量压缩 ==========");

            // 1. 获取源目录下的所有子文件夹
            string[] subDirs = Directory.GetDirectories(sourceDir);
            foreach (string subDir in subDirs)
            {
                string dirName = Path.GetFileName(subDir); // 获取文件夹名
                string zipPath = Path.Combine(targetDir, $"{dirName}.zip"); // 压缩包名=文件夹名.zip
                CompressFiles(subDir, zipPath);
            }

            // 2. 获取源目录下的所有文件（排除子文件夹里的文件）
            string[] files = Directory.GetFiles(sourceDir);
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file); // 文件名（不含后缀）
                string fileExt = Path.GetExtension(file); // 文件后缀
                // 压缩包名=原文件名.zip（避免压缩包覆盖原文件）
                string zipPath = Path.Combine(targetDir, $"{fileName}{fileExt}.zip");
                CompressFiles(file, zipPath);
            }

            LoadRichTextBox(richTextBox1, DateTime.Now + "========== 批量压缩结束 ==========\n");
        }


    }
}
