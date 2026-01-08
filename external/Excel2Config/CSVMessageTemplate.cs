using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ExcelDataReader;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace Excel2Config
{
    public enum VarValidityType
    {
        ErrorTypeNone,
        ErrorTypeZeroOrNull,
        ErrorTypeSound,
        ErrorTypeAtlas,
        ErrorTypeID,
    }

    public enum StorageStructure
    {
        Array,
        Map,
    }

    public class CSVMessageTemplate
    {
        private List<string> m_varList = new List<string>();
        private List<DataVarType> m_varTypeList = new List<DataVarType>();
        private List<string> m_varDescList = new List<string>();
        private List<List<string>> m_dataRows = new List<List<string>>();
        private string m_name;
        private StorageStructure m_structure = StorageStructure.Array;
        private string m_configName;
        private string m_packageName = string.Empty;
        private CSVMessageTemplate m_innerMessage = null;
        private string m_sourcePath;
        private List<string> m_varAtlasList = new List<string>();

        public CSVMessageTemplate()
        {
            
        }

        public void SetSource(String csvPath)
        {
            m_sourcePath = csvPath;
            m_name = $"DT_{Path.GetFileNameWithoutExtension(csvPath)}";
            m_configName = Path.GetFileNameWithoutExtension(csvPath);
            
            ReadCSV(csvPath);
        }

        public void SetName(String name)
        {
            m_name = name;
        }

        private void ReadCSV(string csvPath)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int rowCount = 0;
            int fieldCount = 0;
            
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var stream = File.Open(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                    {
                        FallbackEncoding = Encoding.GetEncoding("GBK"),
                        AutodetectSeparators = new[] { ',', '\t'}
                    }))
                    {
                        if (!reader.Read())
                        {
                            Logger.Error($"CSV file {csvPath} is empty");
                            return;
                        }

                        // 读取变量名(第1行)并转换为符合protobuf命名规范的格式
                        fieldCount = reader.FieldCount;
                        for (int i = 0; i < fieldCount; i++)
                        {
                            var name = reader.GetString(i);
                            if (!string.IsNullOrEmpty(name))
                            {
                                // 使用FileExtension.ConvertVariableName转换变量名
                                string convertedName = FileExtension.ConvertVariableName(name);
                                Logger.Debug($"[FieldName] Original: {name} -> Converted: {convertedName}");
                                m_varList.Add(convertedName);
                            }
                        }

                        // 读取类型(第2行)
                        if (!reader.Read())
                        {
                            Logger.Error($"CSV file {csvPath} missing type definitions");
                            return;
                        }

                        string firstType = null;
                        for (int i = 0; i < fieldCount; i++)
                        {
                            var type = reader.GetString(i);
                            if (!string.IsNullOrEmpty(type))
                            {
                                string convertedType = GetCellType(type);
                                if (firstType == null)
                                {
                                    firstType = convertedType;
                                }
                                m_varTypeList.Add(new DataVarType(convertedType));
                            }
                        }

                        // 读取描述(第3行)
                        if (!reader.Read())
                        {
                            Logger.Error($"CSV file {csvPath} missing descriptions");
                            return;
                        }

                        for (int i = 0; i < fieldCount; i++)
                        {
                            var desc = reader.GetString(i);
                            if (!string.IsNullOrEmpty(desc))
                            {
                                m_varDescList.Add(desc);
                            }
                        }

                        // 读取数据(第4行开始)
                        while (reader.Read())
                        {
                            var rowData = new List<string>();
                            bool hasData = false;
                            
                            for (int i = 0; i < fieldCount; i++)
                            {
                                var value = reader.GetValue(i);
                                var strValue = value?.ToString() ?? string.Empty;
                                //var errorType = isVarValidity(strValue, i, csvPath);
                                //if (errorType == VarValidityType.ErrorTypeZeroOrNull)
                                //{
                                //    Logger.WarningEx($"CSV file {csvPath} warning:Var \"{strValue}\" in {m_dataRows.Count + 4} row, {m_varList[i]} col use of null or 0 is not allowed");
                                //}
                                //else if (errorType == VarValidityType.ErrorTypeSound)
                                //{
                                //    Logger.WarningEx($"CSV file {csvPath} warning: Var \"{strValue}\" in {m_dataRows.Count + 4} row, {m_varList[i]} col need StartsWith\"sound/\" and EndsWith\".aac\"");
                                //}
                                //else if (errorType == VarValidityType.ErrorTypeAtlas)
                                //{
                                //    Logger.WarningEx($"CSV file {csvPath} warning: Var \"{strValue}\" in {m_dataRows.Count + 4} row, {m_varList[i]} col is not find in ..ini\\client\\common\\atlas\\titles.atlas");
                                //}
                                rowData.Add(strValue);
                                if (!string.IsNullOrEmpty(strValue))
                                {
                                    hasData = true;
                                }
                            }

                            if (hasData)
                            {
                                m_dataRows.Add(rowData);
                            }
                        }

                        // 验证数据完整性
                        if (m_varList.Count == 0 || m_varList.Count != m_varTypeList.Count)
                        {
                            Logger.Error($"CSV file {csvPath} format error: variable names and types count mismatch");
                            return;
                        }

                        // 创建包装消息
                        CreateWrapperMessage(firstType);
                    }
                }

                // 在读取完成后添加统计信息
                rowCount = m_dataRows.Count;
                fieldCount = m_varList.Count;
            }
            catch (Exception ex)
            {
                Logger.Error($"Read CSV file error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                stopwatch.Stop();
                Logger.Info($"[Performance] ReadCSV cost: {stopwatch.ElapsedMilliseconds}ms");
                Logger.Info($"[Statistics] Processed {rowCount} rows, {fieldCount} fields");
            }
        }

        private VarValidityType isVarValidity(string type, int Index, string csvPath)
        {
            if (type.Length == 0)
            {
                return VarValidityType.ErrorTypeNone;
            }

            if (VarTypeList[Index].GetMyType().Equals("ani"))
            {
                if (type.Equals("NULL") || type.Equals("null") || type.Equals("0"))
                {
                     return VarValidityType.ErrorTypeZeroOrNull;
                }
            }
            else if (VarTypeList[Index].GetMyType().Equals("sound"))
            {
                if (type.Equals("NULL") || type.Equals("null") || type.Equals("0"))
                {
                     return VarValidityType.ErrorTypeZeroOrNull;
                }

                if (!type.StartsWith("sound/") || !type.EndsWith(".aac"))
                {
                    return VarValidityType.ErrorTypeSound;
                }
            }
            else if (VarTypeList[Index].GetMyType().Equals("effect"))
            {
                if (type.Equals("NULL") || type.Equals("null") || type.Equals("0"))
                {
                     return VarValidityType.ErrorTypeZeroOrNull;
                }
            }
            else if (VarTypeList[Index].GetMyType().Equals("atlassprite"))
            {
                if (type.Equals("NULL") || type.Equals("null") || type.Equals("0"))
                {
                     return VarValidityType.ErrorTypeZeroOrNull;
                }

                if (!isFindInAtlas(type, csvPath))
                {
                    return VarValidityType.ErrorTypeAtlas;
                }
            }
            else if(VarTypeList[Index].GetMyType().Equals("index")
                    || VarTypeList[Index].GetMyType().Equals("key"))
            {
                // 两种id类型只允许正整数
                foreach(char ch in type)
                {
                    if(!Char.IsNumber(ch))
                    {
                        return VarValidityType.ErrorTypeID;
                    }
                }
            }
           
            return VarValidityType.ErrorTypeNone;
        }

        private bool isFindInAtlas(string type, string csvPath)
        {
            if (m_varAtlasList.Count == 0)
            {
                string fileDirectory = Path.GetDirectoryName(csvPath);
                string parentDirectory = Path.GetDirectoryName(fileDirectory);
                string targetFolderName = "atlas";
                string targetFolderPath = Path.Combine(parentDirectory, targetFolderName);
                string targetFileName = "titles.atlas";
                string targetFilePath = Path.Combine(targetFolderPath, targetFileName);
                using (StreamReader reader = new StreamReader(targetFilePath))
                {
                    reader.ReadLine();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!line.Contains(":") && !line.Contains("."))
                        {
                            m_varAtlasList.Add(line);
                        }
                    }
                } 
            }

            if (m_varAtlasList.Contains(type))
            {
                return true;
            }

            return false;
        }

        private string GetCellType(string varType)
        {
            switch (varType)
            {
                case "T_UINT":
                case "T_UCHAR":
                case "T_USHORT":
                    return "uint32";
                case "T_INT":
                case "T_CHAR":
                case "T_SHORT":
                    return "int32";
                case "T_INT64":
                    return "sint64";
                case "T_UINT64":
                    return "uint64";
                case "T_LPCSTR":
                    return "string";
                case "T_ANI":
                    return "ani";
                case "T_EFFECT":
                    return "effect";
                case "T_SOUND":
                    return "sound";
                case "T_ATLASSPRITE":
                    return "atlassprite";
                case "T_INDEX":
                    return "index";
                case "T_KEY":
                    return "key";
                default:
                    {
                        String[] validArrayInnerTypes = { "T_UINT", "T_UCHAR", "T_USHORT", "T_INT", 
                            "T_CHAR", "T_SHORT", "T_INT64", "T_UINT64"};
                        String arrayPattern = @"^T_ARRAY\[(" + String.Join('|', validArrayInnerTypes) + @")\]$";
                        Match arrayMatch = Regex.Match(varType, arrayPattern);

                        if(arrayMatch.Success)
                        {
                            String innerType = arrayMatch.Groups[1].Value;
                            return $"list#{GetCellType(innerType)}";
                        }

                        return varType;
                    }
            }
        }

        private void CreateWrapperMessage(string firstType)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            try
            {
                // 保存原始的消息结构作为内部消息
                var innerMessage = new CSVMessageTemplate();
                innerMessage.m_name = m_name;

                if(firstType == "index")
                {
                    innerMessage.m_structure = StorageStructure.Array;
                }
                else if(firstType == "key")
                {
                    innerMessage.m_structure = StorageStructure.Map;
                }
                else
                {
                    innerMessage.m_structure = StorageStructure.Map;
                    Logger.Warning($"[CSVMessageTemplate] First colume of {m_name} is not 'T_INDEX' or 'T_KEY', now deal it as 'T_KEY'.");
                }

                innerMessage.m_varList = new List<string>(m_varList);
                innerMessage.m_varTypeList = new List<DataVarType>(m_varTypeList);
                innerMessage.m_varDescList = new List<string>(m_varDescList);
                innerMessage.m_dataRows = m_dataRows;

                // 创建外部包装消息
                var wrapperName = Path.GetFileNameWithoutExtension(m_configName);
                m_name = wrapperName;
                m_varList.Clear();
                m_varTypeList.Clear();
                m_varDescList.Clear();
                m_varList.Add("config");

                if(innerMessage.m_structure == StorageStructure.Array)
                {
                    m_varTypeList.Add(new DataVarType($"list#{innerMessage.m_name}"));
                    m_varDescList.Add("config array");
                }
                else
                {
                    var originType = String.Empty;

                    if(firstType == "key" || firstType == "index")
                    {
                        originType = "uint32";
                    }
                    else
                    {
                        originType = firstType;
                    }

                    m_varTypeList.Add(new DataVarType($"map#{originType}:{innerMessage.m_name}"));
                    m_varDescList.Add("config map");
                }

                // 保存内部消息
                m_innerMessage = innerMessage;
            }
            finally
            {
                stopwatch.Stop();
                Logger.Info($"[Performance] CreateWrapperMessage cost: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        public string ToProtobuf()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.AppendLine("syntax = \"proto3\";\n");
                
                // 添加包名
                if (!string.IsNullOrEmpty(m_packageName))
                {
                    sb.Append("package ");
                    sb.Append(m_packageName);
                    sb.AppendLine(";\n");
                }

                // 先生成内部消息
                if (m_innerMessage != null)
                {
                    sb.AppendLine($"message {m_innerMessage.m_name} {{");
                    for (int i = 0; i < m_innerMessage.m_varList.Count; i++)
                    {
                        if (m_innerMessage.m_varDescList.Count > i && !string.IsNullOrEmpty(m_innerMessage.m_varDescList[i]))
                        {
                            sb.AppendLine($"    // {m_innerMessage.m_varDescList[i]}");
                        }
                        sb.AppendLine($"    {m_innerMessage.m_varTypeList[i].ProtobufType} {m_innerMessage.m_varList[i]} = {i + 1};");
                    }
                    sb.AppendLine("}\n");
                }

                // 生成外部包装消息
                sb.AppendLine($"message {m_name} {{");
                for (int i = 0; i < m_varList.Count; i++)
                {
                    if (m_innerMessage.m_varDescList.Count > i && !string.IsNullOrEmpty(m_varDescList[i]))
                    {
                        sb.AppendLine($"    // {m_varDescList[i]}");
                    }
                    sb.AppendLine($"    {m_varTypeList[i].ProtobufType} {m_varList[i]} = {i + 1};");
                }
                sb.AppendLine("}");

                return sb.ToString();
            }
            finally
            {
                stopwatch.Stop();
                Logger.Info($"[Performance] ToProtobuf cost: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        public string ToJson()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            StringBuilder sb = new StringBuilder();
            try
            {
                if (m_structure == StorageStructure.Array)
                {
                    sb.Append("[");
                    for (int i = 0; i < m_dataRows.Count; i++)
                    {
                        sb.Append("{");
                        for (int j = 0; j < m_varList.Count; j++)
                        {
                            sb.Append($"\"{m_varList[j]}\":");
                            sb.Append(FormatJsonValue(m_dataRows[i][j], m_varTypeList[j]));
                            if (j < m_varList.Count - 1)
                            {
                                sb.Append(",");
                            }
                        }
                        sb.Append("}");
                        if (i < m_dataRows.Count - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sb.Append("]");
                }
                else
                {
                    sb.Append("{");
                    for (int i = 0; i < m_dataRows.Count; i++)
                    {
                        sb.Append($"\"{m_dataRows[i][0]}\":");
                        sb.Append("{");
                        for (int j = 0; j < m_varList.Count; j++)
                        {
                            sb.Append($"\"{m_varList[j]}\":");
                            sb.Append(FormatJsonValue(m_dataRows[i][j], m_varTypeList[j]));
                            if (j < m_varList.Count - 1)
                            {
                                sb.Append(",");
                            }
                        }
                        sb.Append("}");
                        if (i < m_dataRows.Count - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    sb.Append("}");
                }
                return sb.ToString();
            }
            finally
            {
                stopwatch.Stop();
                Logger.Info($"[Performance] ToJson cost: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        public string ToTextProto()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.AppendLine("config [");
                for (int i = 0; i < m_dataRows.Count; i++)
                {
                    if (i > 0) sb.AppendLine();
                    sb.AppendLine("  {");
                    
                    if (m_innerMessage.m_structure == StorageStructure.Map)
                    {
                        // 添加键值
                        sb.Append("    key: ");
                        sb.AppendLine(FormatProtoValue(m_dataRows[i][0], m_innerMessage.m_varTypeList[0]) + ",");
                        sb.AppendLine("    value {");
                    }
                    
                    // 添加值
                    for (int j = 0; j < m_innerMessage.m_varList.Count; j++)
                    {
                        // 确保每个字段都写出来，即使值为空
                        sb.Append("      ");
                        sb.Append(m_innerMessage.m_varList[j]);
                        sb.Append(": ");
                        string value = (j < m_dataRows[i].Count) ? m_dataRows[i][j] : string.Empty;
                        // 如果不是最后一个字段，添加逗号
                        if (j < m_innerMessage.m_varList.Count - 1)
                        {
                            sb.AppendLine(FormatProtoValue(value, m_innerMessage.m_varTypeList[j]) + ",");
                        }
                        else
                        {
                            sb.AppendLine(FormatProtoValue(value, m_innerMessage.m_varTypeList[j]));
                        }
                    }

                    if (m_innerMessage.m_structure == StorageStructure.Map)
                    {
                        sb.AppendLine("    }");
                    }
                    
                    // 只有不是最后一个数据项时才添加逗号
                    if (i < m_dataRows.Count - 1)
                    {
                        sb.AppendLine("  },");
                    }
                    else
                    {
                        sb.AppendLine("  }");
                    }
                }

                sb.Append("]");
                return sb.ToString();
            }
            finally
            {
                stopwatch.Stop();
                Logger.Info($"[Performance] ToTextProto cost: {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        private string FormatJsonValue(string value, DataVarType type)
        {
            if (type.IsBaseType)
            {
                if (type.MessageType == "bytes" || type.MessageType == "string")
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        // 处理特殊字符的转义
                        var correctString = Regex.Replace(value, @"(\\(?![nrt\""])|(?<!\\)"")", match =>
                        {
                            if (match.Value == "\\")
                                return "\\\\";
                            else if (match.Value == "\"")
                                return "\\\"";
                            return match.Value;
                        });

                        return $"\"{correctString}\"";
                    }
                    return "\"\"";
                }
                return type.ToJson(value);
            }
            // 处理复杂类型的逻辑
            return "\"\"";
        }

        private string FormatProtoValue(string value, DataVarType type)
        {
            if (type.IsBaseType)
            {
                // 确保空值也有合适的默认值
                if (string.IsNullOrEmpty(value))
                {
                    if(type.GetMyType().StartsWith("list"))
                    {
                        return "[]";
                    }

                    switch (type.MessageType)
                    {
                        case "int32":
                        case "uint32":
                        case "sint64":
                        case "uint64":
                            return "0";
                        case "float":
                            return "0.0";
                        case "bool":
                            return "false";
                        case "bytes":
                        case "string":
                        default:
                            return "\"\"";
                    }
                }

                // 对于字符串类型，处理特殊字符的转义
                if (type.MessageType == "bytes" || type.MessageType == "string")
                {
                    var correctString = Regex.Replace(value, @"(\\(?![nrt\""])|(?<!\\)"")", match =>
                    {
                        if (match.Value == "\\")
                            return "\\\\";
                        else if (match.Value == "\"")
                            return "\\\"";
                        return match.Value;
                    });

                    return $"\"{correctString}\"";
                }

                return type.ToTextProto(value, "");
            }
            // 处???复杂类型的逻辑
            return "\"\"";
        }

        public string Name => m_name;
        public string ConfigName => m_configName;
        public List<string> VarList => m_varList;
        public List<DataVarType> VarTypeList => m_varTypeList;
        public List<string> VarDescList => m_varDescList;
        public List<List<string>> DataRows => m_dataRows;
        public string SourcePath => m_sourcePath;
    }
}
