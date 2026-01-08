using System;
using System.Text;
using Excel2Config;

class Program
{
    const string Version = "0.1.0";

    static void Main(string[] args)
    {
        string excelPath = string.Empty;    // excel文件路径
        string pbDataPath = string.Empty;     // pb二进制文件路径
        string protoPath = string.Empty;     // pb描述文件路径
        string outputPath = string.Empty;    // 输出路径
        string protocPath = "protoc";    // protoc文件路径
        string transferPath = "Proto2Json"; // 用于转换proto二进制文件回Excel文件
        string shellPath = "sh";    // shell文件路径
        string protocCmd = string.Empty;    // protoc命令

        bool recursive = false;    // 是否递归
        bool toJson = false;    // 是否生成json文件
        bool toProto = false;
        bool toTextProto = false;
        bool toBinaryProto = false;
        bool clean = false;
        bool asyncTask = true;
        LogLevel logLevel = LogLevel.Debug;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        for (int i = 0; i < args.Length; i++)
        {
            string argItem = args[i];
            Logger.Info($"args {i} is {argItem}");

            if (string.IsNullOrEmpty(argItem))
                continue;

            if (argItem.Equals("--version"))
            {
                Logger.Info(Version);
            }
            else if (argItem.Equals("--clean"))
            {
                clean = true;
            }
            else if (argItem.Equals("--sync"))
            {
                asyncTask = false;
            }
            else if (argItem.StartsWith("--excel_path="))
            {
                if (!string.IsNullOrEmpty(pbDataPath))
                {
                    Logger.Warning("excel_path and pb_path can not be set at the same time.");
                    return;
                }
                excelPath = argItem.Split('=')[1];
            }
            else if (argItem.StartsWith("--log_level="))
            {
                logLevel = (LogLevel)Enum.Parse( typeof(LogLevel), argItem.Split('=')[1]);
            }
            else if (argItem.StartsWith("--pbdata_path="))
            {
                if (!string.IsNullOrEmpty(excelPath))
                {
                    Logger.Warning("excel_path and pb_path can not be set at the same time.");
                    return;
                }
                pbDataPath = argItem.Split('=')[1];
            }
            else if (argItem.StartsWith("--proto_path="))
            {
                protoPath = argItem.Split('=')[1];
            }
            else if (argItem.Equals("--R") || argItem.Equals("--recursive"))
            {
                recursive = true;
            }
            else if (argItem.StartsWith("--output_path="))
            {
                outputPath = argItem.Split('=')[1];
            }
            else if (argItem.Equals("--to_json"))
            {
                toJson = true;
            }
            else if (argItem.StartsWith("--to_protobuf="))
            {
                string toProtobufArg = argItem.Split('=')[1];
                var toProtobufArgs = toProtobufArg.Split('|');
                foreach (var toProtobufArgItem in toProtobufArgs)
                {
                    if (toProtobufArgItem.Equals("proto"))
                    {
                        toProto = true;
                    }
                    else if (toProtobufArgItem.Equals("textproto"))
                    {
                        toTextProto = true;
                    }
                    else
                    {
                        toProto = true;
                        toTextProto = true;
                        toBinaryProto = true;
                    }
                }
            }
            else if (argItem.StartsWith("--protoc="))
            {
                protocPath = argItem.Split('=')[1];
            }
            else if (argItem.StartsWith("--shell="))
            {
                shellPath = argItem.Split('=')[1];
            }
            else if (argItem.StartsWith("--transfer_path="))
            {
                transferPath = argItem.Split('=')[1];
            }
            else if (argItem.StartsWith("--protoc_cmd="))
            {
                toProto = true;
                int cmdIndex = argItem.IndexOf('=') + 1;
                protocCmd = argItem.Substring(cmdIndex, argItem.Length - cmdIndex);
            }
            else if (argItem.StartsWith("--help"))
            {
                StringBuilder helpBuilder = new StringBuilder();
                AddHelpCommand(helpBuilder, "--help", "Show this text.");
                AddHelpCommand(helpBuilder, "--version", $"Show version info. {Version}.");
                AddHelpCommand(helpBuilder, "--excel_path=", "The path to the excel/txt file or folder.");             
                AddHelpCommand(helpBuilder, "--recursive,-R", "Traverse all the subfolders of the excel folder.");
                AddHelpCommand(helpBuilder, "--output_path=", "Setting the output directory. If it is not set, it is the folder path of excel.");
                AddHelpCommand(helpBuilder, "--to_json", "Convert to a json configuration file.");
                AddHelpCommand(helpBuilder, "--clean", "Delete the output directory.");
                AddHelpCommand(helpBuilder, "--sync", "Execute synchronously.");
                AddHelpCommand(helpBuilder, "--log_level=", "Set the log level. Debug|Info|Warning|Error.");
                AddHelpCommand(helpBuilder, "--to_protobuf=", "Convert to a protobuf configuration file. Input parameter proto|textproto|binaryproto|all, all is recommended.");
                AddHelpCommand(helpBuilder, "--protoc=", "Set the path to the protoc execution file.Environment variables are used by default protoc.");
                AddHelpCommand(helpBuilder, "--shell=", "Set the path to the shell execution file.Environment variables are used by default sh.");
                AddHelpCommand(helpBuilder, "--protoc_cmd=", "By default, the output file path of proto is set, and other protoc commands that need to be executed are added.");

                AddHelpCommand(helpBuilder, "--pbdata_path=", "The path to the pbdata file or folder.");
                AddHelpCommand(helpBuilder, "--proto_path=", "The path to the protobuffer file or folder. Ambigous with --excel_path.");
                AddHelpCommand(helpBuilder, "--transfer_path=", "the program used to transfer the proto binary file to excel file.");

                Logger.Info(helpBuilder.ToString());
            }
        }

        Logger.SetLogLevel(logLevel);

        if (!string.IsNullOrEmpty(excelPath))
        {
            new CSVProgram(excelPath, outputPath, protocPath, recursive, toJson, toProto, toTextProto, toBinaryProto, clean, !asyncTask, shellPath, protocCmd);
        }
        else if (!string.IsNullOrEmpty(pbDataPath))
        {
            // 需要提供二进制文件与proto描述
            new ConvertProgram(pbDataPath, protoPath, outputPath, shellPath, transferPath);
        }
    }

    static void AddHelpCommand(StringBuilder stringBuilder, string command, string desc)
    {
        stringBuilder.Append(command);
        stringBuilder.Append(' ', 20 - command.Length);
        stringBuilder.AppendLine(desc);
    }
}
