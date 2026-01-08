using System;
using System.Diagnostics;
using System.IO;

namespace Excel2Config
{
    public class ConvertProgram
    {
        string m_protoPath = string.Empty;
        string m_shellPath = string.Empty;
        string m_outputDir = string.Empty;

        /***
         * Convert protobuf binary to excel
         * @param protoPath proto describe files directory
         * @param pbBinaryPath protobuf binary data directory
         * @param outputDir stores the converted excel files
         * @param shellPath shell path
         * @param transferCmd transfer command
         */
        public ConvertProgram(string pbBinaryPath, string protoPath, string outputDir, string shellPath, string transferCmd)
        {
            if (!Directory.Exists(protoPath))
            {
                Logger.Warning($"[ConvertProgram]The directory {protoPath} with .proto files does not exist.");
                return;
            }

            m_protoPath = protoPath;
            m_shellPath = FileExtension.DetectShellPath(shellPath);
            m_outputDir = outputDir;

            FileExtension.CreateDirectorySafely(outputDir);

            // 如果pbBinaryPath是文件，则直接转换
            if (File.Exists(pbBinaryPath))
            {
                PB2Excel(pbBinaryPath, transferCmd);
            }
            // 如果是文件夹，则遍历文件夹
            else if (Directory.Exists(pbBinaryPath) && Directory.Exists(protoPath))
            {
                foreach (string file in Directory.GetFiles(pbBinaryPath))
                {
                    PB2Excel(file, transferCmd);
                }
            }
        }

        private void PB2Excel(string pbPath, string transferPath)
        {
            string protoName = Path.GetFileNameWithoutExtension(pbPath);
            string command = $"{transferPath} -I {m_protoPath} -P {protoName}.proto {protoName} @{pbPath} -O {m_outputDir}/{protoName}.txt";
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = m_shellPath,
                Arguments = $"/c {command}",
                RedirectStandardError = true,
                RedirectStandardOutput = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();

                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        Logger.Error($"{error}");
                    }
                    else
                    {
                        Logger.Info($"[ConvertProgram]{pbPath} convert to excel {m_outputDir}/{protoName}.txt success.");
                    }
                }
            }
            catch (System.Exception ex) 
            {               
                Logger.Error($"[ConvertProgram]Failed to convert protobuf binary {pbPath} to excel: {ex.Message}");
            }
        }
    }
}