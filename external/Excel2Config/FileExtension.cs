using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Excel2Config
{
    public static class FileExtension
    {

        private static string _cacheShellPath;

        public static Encoding GetFileEncoding(string srcFile)
        {
            // 读取文件的前4个字节
            var bom = new byte[4];
            using (var file = new FileStream(srcFile, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // 检查BOM标记
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; // UTF-16 LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; // UTF-16 BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;

            // 如果没有BOM标记，返回系统默认编码
            return Encoding.GetEncoding("GBK");
        }

        public static void DeleteFileSafely(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Delete File occur an exception: {ex.Message}");
            }
        }

        public static void CreateDirectorySafely(string path)
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                else if (directoryPath == null)
                {
                    Logger.Warning("Directory {path} is invalid.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Create Directory occur an exception: {ex.Message}");
            }
        }

        public static void DeleteDirectorySafely(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Delete Directory occur an exception: {ex.Message}");
            }
        }

        public static string DetectShellPath(string defaultShellPath)
        {
            if (!string.IsNullOrEmpty(_cacheShellPath) && File.Exists(_cacheShellPath))
            {
                return _cacheShellPath;
            }

            string shellPath = defaultShellPath;

            if (!File.Exists(shellPath))
            {
                Logger.Info($"{shellPath} not found,  try to use cmd.exe");

                // 1.优先使用 COMSPEC 环境变量
                string cmdPath = Environment.GetEnvironmentVariable("COMSPEC");
                if (string.IsNullOrEmpty(cmdPath) || !File.Exists(cmdPath))
                {
                    Logger.Info("[RunProtocCmd]COMSPEC environment variable not found or invalid, attempting alternative methods.");

                    // 2.通过 SystemRoot 计算 cmd.exe 的路径
                    string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
                    if (!string.IsNullOrEmpty(systemRoot))
                    {
                        cmdPath = Path.Combine(systemRoot, "system32", "cmd.exe");
                        if (File.Exists(cmdPath))
                        {
                            Logger.Info($"Found cmd.exe using SystemRoot: {cmdPath}");
                        }
                        else
                        {
                            Logger.Info("cmd.exe not found in SystemRoot.");
                            cmdPath = null;
                        }
                    }

                    // 3.使用 `where` 命令查找 cmd.exe
                    if (cmdPath == null)
                    {
                        try
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = "where",
                                Arguments = "cmd",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                            };

                            using (Process process = new Process{ StartInfo = startInfo})
                            {
                                process.Start();
                                cmdPath = process.StandardOutput.ReadLine();
                                process.WaitForExit();
                            }

                            if (!string.IsNullOrEmpty(cmdPath) && File.Exists(cmdPath))
                            {
                                Logger.Info($"Found cmd.exe using 'where' command: {cmdPath}");
                            }
                            else
                            {
                                Logger.Info("'where cmd' did not return a valid path.");
                                cmdPath = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Failed to execute 'where cmd': {ex.Message}");
                        }
                    }

                    // 4. 遍历 PATH 变量查找 cmd.exe
                    if (cmdPath == null)
                    {
                        string pathEnv = Environment.GetEnvironmentVariable("PATH");
                        if (!string.IsNullOrEmpty(pathEnv))
                        {
                            foreach (string dir in pathEnv.Split(';'))
                            {
                                string potentialCmdPath = Path.Combine(dir.Trim(), "cmd.exe");
                                if (File.Exists(potentialCmdPath))
                                {
                                    cmdPath = potentialCmdPath;
                                    Logger.Info($"Found cmd.exe in PATH: {cmdPath}");
                                    break;
                                }
                            }
                        }
                    }

                    // 5. 最终尝试默认路径
                    if (cmdPath == null)
                    {
                        cmdPath = @"C:\Windows\System32\cmd.exe";
                        Logger.Info($"Using fallback path: {cmdPath}");
                    }
                }

                shellPath = cmdPath;
             }

            _cacheShellPath = shellPath;
            return shellPath;
        }

        public static string ConvertVariableName(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // 检查是否需要删除第一个字母
            if (input.Length > 1 && char.IsLower(input[0]) && char.IsUpper(input[1]))
            {
                input = input.Substring(1);
            }
            else
            {
                string[] prefixesToRemove = new[] { "sz", "us", "uc"  };
                foreach (var prefix in prefixesToRemove)
                {
                    if (input.Length > prefix.Length &&
                        input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                        char.IsUpper(input[prefix.Length]))
                    {
                        input = input.Substring(prefix.Length);
                        break;
                    }
                }
            }

            var result = new StringBuilder();
            result.Append(char.ToLower(input[0]));

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsLower(input[i - 1]) && char.IsUpper(input[i]))
                {
                    result.Append('_');
                }
                result.Append(char.ToLower(input[i]));
            }
            return result.ToString();
        }
    }
}