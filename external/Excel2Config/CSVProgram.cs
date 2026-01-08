using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Excel2Config
{
    public class CSVProgram
    {
        private readonly string m_sourcePath;
        private readonly string m_outputDir;
        private string m_protocPath;
        private readonly string m_shellPath;
        private readonly string m_protocCmd;
        private readonly bool m_recursive;
        private readonly bool m_toJson;
        private readonly bool m_toProto;
        private readonly bool m_toTextProto;
        private readonly bool m_toBinaryProto;
        private readonly bool m_sync;
        private static readonly object s_fileLock = new object();
        private string TEMP_DIR = "D:\\ProtoTemp\\";

        public CSVProgram(string sourcePath, string outputDir, string protocPath, bool recursive,
            bool toJson, bool toProto, bool toTextProto, bool toBinaryProto, bool clean, bool sync, string shellPath, string protocCmd)
        {
            Stopwatch totalStopwatch = new Stopwatch();
            totalStopwatch.Start();

            try 
            {
                m_sourcePath = sourcePath;
                m_outputDir = outputDir;
                m_protocPath = protocPath;
                m_recursive = recursive;
                m_toJson = toJson;
                m_toProto = toProto;
                m_toTextProto = toTextProto;
                m_toBinaryProto = toBinaryProto;
                m_sync = sync;
                m_shellPath = shellPath;
                m_protocCmd = protocCmd;

                ProcessFiles(clean);
            }
            finally
            {
                totalStopwatch.Stop();
                Logger.ForceLog($"[Performance] Total process time: {totalStopwatch.ElapsedMilliseconds}ms");
            }
        }

        private string DetectTempPath()
        {
            // 判断临时文件夹是否存在，不存在则创建
            try
            {
                if (!Directory.Exists(TEMP_DIR))
                {
                    Directory.CreateDirectory(TEMP_DIR);
                }
            }
            catch (System.Exception)
            {
                if (!Directory.Exists(TEMP_DIR))
                {
                    // 动态选取其他分区
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady && drive.DriveType == DriveType.Fixed && drive.Name != "C:\\")
                        {
                            TEMP_DIR = drive.Name + "\\ProtoTemp\\";
                            break;
                        }
                    }
                }
            }
            return TEMP_DIR;
        }

        private void ProcessFiles(bool clean)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            long initialMemory = GC.GetTotalMemory(false);
            
            List<string> csvFiles = new List<string>();
            if (File.Exists(m_sourcePath))
            {
                csvFiles.Add(m_sourcePath);
            }
            else if (Directory.Exists(m_sourcePath))
            {
                csvFiles.AddRange(GetCSVFiles(m_sourcePath, m_recursive));
            }

            if (csvFiles.Count == 0)
            {
                Logger.Warning("No CSV files found.");
                return;
            }

            try
            {
                int processedCount = 0;
                int totalCount = csvFiles.Count;

                Action<string> processFile = file =>
                {
                    Logger.Info($"==================== Start converting file: {file} ({Interlocked.Increment(ref processedCount)}/{totalCount}) ====================");
                    var csvTemplate = new CSVMessageTemplate();
                    csvTemplate.SetSource(file);
                    
                    if (m_toJson) WriteJson(csvTemplate);
                    if (m_toProto) WriteProtobuf(csvTemplate);
                    if (m_toTextProto) WriteTextProto(csvTemplate);
                    if (m_toBinaryProto) WriteBinaryProto(csvTemplate);
                    if (!string.IsNullOrEmpty(m_protocCmd)) RunProtocCmd(csvTemplate);
                    
                    Logger.Info($"==================== Finished converting file: {file} ====================");
                };

                if (m_sync)
                {
                    foreach (var file in csvFiles)
                    {
                        processFile(file);
                    }
                }
                else
                {
                    Parallel.ForEach(csvFiles, processFile);
                }

                Clear(clean);
            }
            finally
            {
                long finalMemory = GC.GetTotalMemory(false);
                long memoryDiff = finalMemory - initialMemory;
                
                stopwatch.Stop();
                Logger.ForceLog($"[Performance] Total process time: {stopwatch.ElapsedMilliseconds}ms");
                Logger.ForceLog($"[Memory] Memory usage: {memoryDiff / 1024 / 1024}MB");
            }
        }

        private List<string> GetCSVFiles(string dir, bool recursive)
        {
            List<string> files = new List<string>();
            try
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    if (file.EndsWith(".txt") || file.EndsWith(".csv"))
                    {
                        files.Add(file);
                    }
                }

                if (recursive)
                {
                    foreach (var subDir in Directory.GetDirectories(dir))
                    {
                        files.AddRange(GetCSVFiles(subDir, recursive));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"GetCSVFiles Exception: {e.Message}");
            }
            return files;
        }

        private void WriteJson(CSVMessageTemplate template)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            try
            {
                string jsonPath = Path.Combine(m_outputDir, "json", template.ConfigName + ".json");
                Logger.Info($"[ToJson]WriteJson to {jsonPath} start.");

                FileExtension.DeleteFileSafely(jsonPath);
                FileExtension.CreateDirectorySafely(jsonPath);

                string json = template.ToJson();
                File.WriteAllText(jsonPath, json);
                Logger.Info($"[ToJson]{jsonPath} write success.");
            }
            finally
            {
                stopwatch.Stop();
                Logger.Info($"[Performance] WriteJson total cost: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        private void WriteProtobuf(CSVMessageTemplate template)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            try
            {
                string inputFileName = Path.GetFileName(template.SourcePath);
                string protoPath = Path.Combine(m_outputDir, "proto", Path.GetFileNameWithoutExtension(inputFileName) + ".proto");
                Logger.Info($"[ToProtobuf]WriteProtobuf to {protoPath} start.");

                FileExtension.CreateDirectorySafely(protoPath);

                // 直接写入目标文件
                lock (s_fileLock)
                {
                    string proto = template.ToProtobuf();
                    byte[] gbkBytes = Encoding.GetEncoding("gbk").GetBytes(proto);
                    File.WriteAllBytes(protoPath, gbkBytes);
                }
                
                Logger.Info($"[ToProtobuf]{protoPath} write success.");
            }
            catch (Exception e)
            {
                Logger.Error($"WriteProtobuf Exception: {e.Message}");
            }
            finally
            {
                stopwatch.Stop();
                Logger.Info($"[Performance] WriteProtobuf total cost: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        private void WriteTextProto(CSVMessageTemplate template)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            try
            {
                string textProtoPath = Path.Combine(m_outputDir, "proto_temp", template.ConfigName + ".textproto");
                Logger.Info($"[ToTextProto]WriteTextProto to {textProtoPath} start.");

                FileExtension.DeleteFileSafely(textProtoPath);
                FileExtension.CreateDirectorySafely(textProtoPath);

                string textProto = template.ToTextProto();
                byte[] gbkBytes = Encoding.GetEncoding("gbk").GetBytes(textProto);
                File.WriteAllBytes(textProtoPath, gbkBytes);
                Logger.Info($"[ToTextProto]{textProtoPath} write success.");
            }
            finally
            {
                stopwatch.Stop();
                Logger.Info($"[Performance] WriteTextProto total cost: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        private void WriteBinaryProto(CSVMessageTemplate template)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            try
            {
                FileExtension.CreateDirectorySafely(DetectTempPath());
                
                Task.Delay(200).Wait();
                string protoFileName = Path.Combine(m_outputDir, "proto", Path.GetFileNameWithoutExtension(template.SourcePath) + ".proto");
                string binaryProtoPath = Path.Combine(m_outputDir, "binary", template.ConfigName + ".bytes");
                string targetBinaryProtoPath = Path.Combine(m_outputDir, template.ConfigName + ".bytes");

                Logger.Info($"[ToBinaryProto]WriteBinaryProto to {binaryProtoPath} start......");

                FileExtension.DeleteFileSafely(binaryProtoPath);
                FileExtension.CreateDirectorySafely(binaryProtoPath);

                string protocPath = m_protocPath.Replace("\\", "/");
                Process.Start(protocPath, "--version").WaitForExit();

                string shellPath = FileExtension.DetectShellPath(m_shellPath);

                string messageType = template.Name;
                string textProtoPath = Path.Combine(m_outputDir, "proto_temp", template.ConfigName + ".textproto");
                Logger.Info($"[ToBinaryProto]textProtoPath exists: {File.Exists(textProtoPath)}");

                string protoPath = Path.GetDirectoryName(protoFileName);
                string protoFile = Path.GetFileName(protoFileName);

                File.Copy(Path.Combine(protoPath, protoFile), Path.Combine(DetectTempPath(), protoFile), true);

                string tempTextProtoPath = Path.Combine(DetectTempPath(), template.ConfigName + ".textproto");
                string tempBinaryProtoPath = Path.Combine(DetectTempPath(), template.ConfigName + ".bytes");
                File.Copy(textProtoPath, tempTextProtoPath, true);

                string command = $"{protocPath} --encode={messageType} --proto_path={DetectTempPath()} {protoFile} < {tempTextProtoPath} > {tempBinaryProtoPath}";
                command = command.Replace("\\", "/");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = shellPath,
                    Arguments = $"/c {command}",
                    RedirectStandardError = true,
                    RedirectStandardOutput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                try
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.Start();

                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        File.Copy(tempBinaryProtoPath, binaryProtoPath, true);

                        if (!string.IsNullOrEmpty(error))
                        {
                            Logger.Error($"{error}");
                        }
                        else
                        {
                            Logger.Info($"[ToBinaryProto]{binaryProtoPath} write success.");
                            File.Copy(binaryProtoPath, targetBinaryProtoPath, true);
                            Logger.Info($"[ToBinaryProto]{binaryProtoPath} copied to {targetBinaryProtoPath}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"[ToBinaryProto]Process protoc error: {e.Message}");
                }
            }
            finally
            {
                stopwatch.Stop();
                Logger.Info($"[Performance] WriteBinaryProto total cost: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        private void RunProtocCmd(CSVMessageTemplate template)
        {
            try
            {
                Task.Delay(100).Wait();
                string protoFileName = Path.Combine(m_outputDir, "proto", Path.GetFileNameWithoutExtension(template.SourcePath) + ".proto");
                string shellPath = FileExtension.DetectShellPath(m_shellPath);

                string protocPath = m_protocPath.Replace("\\", "/");
                Process.Start(protocPath, "--version").WaitForExit();

                // 检查是否设置了源码输出目录
                string srcOut = string.Empty;
                int index = m_protocCmd.IndexOf("cpp_out=");
                if (index != -1)
                {
                    srcOut = m_protocCmd.Substring(index + 8);
                    int nextEndIndex = srcOut.IndexOf(' ');
                    if (nextEndIndex != -1)
                    {
                        srcOut = srcOut.Substring(0, nextEndIndex).Trim();
                    }
                }
                
                string cmd = m_protocCmd;
                if (string.IsNullOrEmpty(srcOut))
                {
                    FileExtension.CreateDirectorySafely($"{m_outputDir}/src");
                    cmd += $" --cpp_out={m_outputDir}/src";
                }

                string protoPath = Path.GetDirectoryName(protoFileName);
                string command = $"{protocPath} --proto_path {protoPath} {cmd} {protoFileName}";
                command = command.Replace("\\", "/");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = shellPath,
                    Arguments = $"/c {command}",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                try
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.Start();

                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (!string.IsNullOrEmpty(error))
                        {
                            Logger.Error($"{error}");
                        }
                        else
                        {
                            string exitCode = process.ExitCode == 0 ? "success" : "fail";
                            Logger.Info($"[RunProtocCmd] Process.Start {shellPath} -c {command} \n-->{process.ExitCode} {exitCode}.");
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"[RunProtocCmd]Process protoc error: {e.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[RunProtocCmd]Exception:{ex.Message}");
            }
        }

        private void Clear(bool clean)
        {
            if (clean)
            {
                string tempPath = Path.Combine(m_outputDir, "temp");
                string protoPath = Path.Combine(m_outputDir, "proto");
                string jsonPath = Path.Combine(m_outputDir, "json");
                string protoBinaryPath = Path.Combine(m_outputDir, "binary");
                string protoTempPath = Path.Combine(m_outputDir, "proto_temp");
                string srcPath = Path.Combine(m_outputDir, "src");

                FileExtension.DeleteDirectorySafely(tempPath);
                FileExtension.DeleteDirectorySafely(protoPath);
                FileExtension.DeleteDirectorySafely(jsonPath);
                FileExtension.DeleteDirectorySafely(protoBinaryPath);
                FileExtension.DeleteDirectorySafely(protoTempPath);
                FileExtension.DeleteDirectorySafely(srcPath);
                FileExtension.DeleteDirectorySafely(DetectTempPath());
            }
        }
    }
} 