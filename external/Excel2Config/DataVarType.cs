using System;
using System.Linq;

namespace Excel2Config
{
    public class DataVarType
    {
        private string m_type;
        public string ProtobufType { get; private set; }
        public bool IsBaseType { get; private set; }
        public bool IsMap { get; private set; }
        public string MapTypeKey { get; private set; }
        public string MapTypeValue { get; private set; }
        public bool IsList { get; private set; }
        public string MessageType { get; private set; }
        private char m_splitChar;

        public DataVarType(string type)
        {
            m_type = type;
            ProtobufType = string.Empty;
            MapTypeKey = string.Empty;
            MapTypeValue = string.Empty;
            MessageType = string.Empty;
            ParseType();
        }

        private void ParseType()
        {
            try
            {
                ProtobufType = m_type;
                m_splitChar = ',';
                if (m_type.Contains("#"))
                {
                    var typeArgs = m_type.Split('#');
                    switch (typeArgs[0])
                    {
                        case "map":
                            var mapArgs = typeArgs[1].Split(':');
                            ProtobufType = $"map<{mapArgs[0]},{mapArgs[1]}>";
                            MapTypeKey = mapArgs[0];
                            MapTypeValue = mapArgs[1];
                            IsBaseType = IsBasicType(MapTypeKey) && IsBasicType(MapTypeValue);
                            IsMap = true;
                            MessageType = mapArgs[1];
                            break;
                        case "list":
                            ProtobufType = $"repeated {typeArgs[1]}";
                            IsBaseType = IsBasicType(typeArgs[1]);
                            MessageType = typeArgs[1];
                            IsList = true;
                            break;
                    }

                    if (typeArgs.Length > 3)
                    {
                        var split = typeArgs[2].Split('=');
                        m_splitChar = split[1].ToCharArray()[0];
                    }
                }
                else
                {
                    if (ProtobufType.Equals("string") ||
                        ProtobufType.Equals("ani") ||
                        ProtobufType.Equals("effect") ||
                        ProtobufType.Equals("sound") ||
                        ProtobufType.Equals("atlassprite") ||
                        ProtobufType.Equals("fmt_string"))
                    {
                        ProtobufType = "bytes";
                    }

                    if(ProtobufType.Equals("index")
                        || ProtobufType.Equals("key"))
                    {
                        ProtobufType = "uint32";
                    }

				    IsList = ProtobufType.Contains("repeated");
                    if (!IsMap)
                    {
                        MessageType = ProtobufType.Replace("repeated ", "");
                    }

                    if (!IsBaseType)
                    {
                        IsBaseType = IsBasicType(MessageType);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"ParseType Exception: {e.Message}");
            }
        }

        private bool IsBasicType(string type)
        {
            switch (type)
            {
                case "int32":
                case "uint32":
                case "sint64":
                case "uint64":
                case "float":
                case "bool":
                case "bytes":
                case "string":
                    return true;
                default:
                    return false;
            }
        }

        public string ToJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return GetDefaultValue(true);
            }

            return FormatValue(value, true);
        }

        public string ToTextProto(string value, string varName)
        {
            if (string.IsNullOrEmpty(value))
            {
                return GetDefaultValue(false);
            }

            return FormatValue(value, false);
        }

        private string GetDefaultValue(bool isJson)
        {
            if(IsList)
            {
                return "[]";
            }

            switch (MessageType)
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
                    {
                        return "\"\"";
                    }
            }
        }

        private string FormatValue(string value, bool isJson)
        {
            if(IsList)
            {
                return FormatValueForList(value, isJson);
            }

            return FormatValueForBasicType(value, isJson);
        }

        private string FormatValueForList(string value, bool isJson)
        {
            String[] values = value.Split('|');
            int count = values.Count();

            if(count > 0)
            {
                String res = "[";

                for(int index = 0; index < count; index++)
                {
                    bool appendedValue = false;

                    switch (MessageType)
                    {
                        case "int32":
                        {
                            if (!int.TryParse(values[index], out int i))
                            {
                                Logger.Error($"FormatValueForList {MessageType} Exception: {values[index]} in {value}");
                            }
                            else
                            {
                                appendedValue = true;
                                res += i.ToString();
                            }

                            break;
                        }
                        case "uint32":
                        {
                            if (!uint.TryParse(values[index], out uint ui))
                            {
                                Logger.Error($"FormatValueForList {MessageType} Exception: {values[index]} in {value}");
                            }
                            else
                            {
                                appendedValue = true;
                                res += ui.ToString();
                            }

                            break;
                        }
                        case "sint64":
                        {
                            if (!long.TryParse(values[index], out long l))
                            {
                                Logger.Error($"FormatValueForList {MessageType} Exception: {values[index]} in {value}");
                            }
                            else
                            {
                                appendedValue = true;
                                res += l.ToString();
                            }

                            break;
                        }
                        case "uint64":
                        {
                            if (!ulong.TryParse(values[index], out ulong ul))
                            {
                                Logger.Error($"FormatValueForList {MessageType} Exception: {values[index]} in {value}");
                            }
                            else
                            {
                                appendedValue = true;
                                res += ul.ToString();
                            }

                            break;
                        }
                        default:
                            break;
                    }

                    if(appendedValue && index != (count - 1))
                    {
                        res += ',';
                    }
                }

                res += ']';
                return res;
            }

            return "[]";
        }

        private string FormatValueForBasicType(string value, bool isJson)
        {
            switch (MessageType)
            {
                case "int32":
                    if (!int.TryParse(value, out int i))
                    {
                        Logger.Error($"FormatValueForBasicType {MessageType} Exception: {value}");
                        return "0";
                    }
                    else
                    {
                        return i.ToString();
                    }
                case "uint32":
                    // 需要保证数字
                    // 如果出问题，打印一下日志
                    if (!uint.TryParse(value, out uint ui))
                    {
                        Logger.Error($"FormatValueForBasicType {MessageType} Exception: {value}");
                        return "0";
                    }
                    else
                    {
                        return ui.ToString();
                    }
                case "sint64":
                    if (!long.TryParse(value, out long l))
                    {
                        Logger.Error($"FormatValueForBasicType {MessageType} Exception: {value}");
                        return "0";
                    }
                    else
                    {
                        return l.ToString();
                    }
                case "uint64":
                    if (!ulong.TryParse(value, out ulong ul))
                    {
                        Logger.Error($"FormatValueForBasicType {MessageType} Exception: {value}");
                        return "0";
                    }
                    else
                    {
                        return ul.ToString();
                    }
                case "float":
                    return float.TryParse(value, out float f) ? f.ToString() : "0.0";
                case "bool":
                    return bool.TryParse(value, out bool b) ? b.ToString().ToLower() : "false";
                case "bytes":
                case "string":
                default:
                    return $"\"{value}\"";
            }
        }

        public string GetMyType()
        {
            return m_type;
        }
    }
}