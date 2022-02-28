using UnityEngine;

namespace Script
{
    public class Constant
    {
        public static string RawXMLPath = Application.dataPath + "/Design/XML/";
        public static string JsonRootPath = Application.dataPath + "/Design/Json/";
        public static string ConfigManagerFilePath = Application.dataPath + "/Script/SystemManager/ConfigManager.cs";
        public struct ConfigManager
        {
            public static string Head = @"using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System.IO;

namespace Script.SystemManager
{
    public class ConfigManager
    {
        private static Configuration _config;
        ";

            public static string Mid1 = @"        public static Configuration GetInstance()
        {
            if (_config != null)
                return _config;
            _config = new Configuration();
            return _config;
        }
        
        public class Configuration
        {";
            
            public static string Mid2 = @"            public Configuration()
            {";

            public static string Tail = @"            }

            public string GetJsonContent(string path)
            {
                FileStream fs = new FileStream(path, FileMode.Open);
                StreamReader fileStream = new StreamReader(fs);
                string str = """";
                string line;
                while ((line = fileStream.ReadLine()) != null)
                    str += line;
                return str;
            }
        }
    }
}";

            public static string Int = "            public int ";
            public static string String = "            public string ";
            public static string Bool = "            public bool ";
            public static string IntList = "            public int[] ";
            public static string StringList = "            public string[] ";
            public static string Declaration = "        public class ";
        }

        public struct JsonType
        {
            public static string Int = "int";
            public static string String = "string";
            public static string Bool = "bool";
            public static string ArrayInt = "array.int";
            public static string ArrayString = "array.string";
        }
    }
}