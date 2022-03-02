using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;
using Editor.EditorTool;
using Newtonsoft.Json;
using Script;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.U2D.Path;
using UnityEngine;
using Object = System.Object;

internal class ExcelInfo
{
    public string ShortName;
    public string Path;
    public string SearchDirectory;

    public ExcelInfo(string rawName, string searchDirectory)
    {
        Path = rawName;
        if (searchDirectory != "" && !searchDirectory.EndsWith("/"))
        {
            searchDirectory += '/';
        }
        this.SearchDirectory = searchDirectory;
        ShortName = rawName.Replace(Constant.FilePath.RawXMLPath + searchDirectory, "");
        ShortName = ShortName.Replace(".csv", "");
    }
}

public class ExcelConvertor : EditorWindow
{
    private string _searchDirectory = "";
    private static EditorWindow _thisWindow;
    private List<ExcelInfo> _excelInfos;
    
    private Vector2 _scrollPos;
    private float _itemHeight = 20;
    private float _itemMinWidth = 100;
    private float _curWidth;
    private float _curHeight;
    private int _suggestRow;
    private int _suggestColumn;
    
    private ExcelInfo _currentInfo;
    private List<List<string>> _tableData;
    private int _tableRow;
    private int _tableColumn;

    private static GUIStyle _hightLightTextRowStyle = new GUIStyle();
    private static GUIStyle _hightLightTextColumnStyle = new GUIStyle();
    private static GUIStyle _hightLightTextSelectedStyle = new GUIStyle();
    private static GUIStyle _normalTextStyle = new GUIStyle();

    private static int _totalThreadCount;
    private static int _successThreadCount;
    
    [MenuItem("Tools/ExcelConvertor")]
    public static void ShowWindow()
    {
        if (!_thisWindow)
        {
            _thisWindow = EditorWindow.GetWindow(typeof(ExcelConvertor));
        }
        
        _hightLightTextRowStyle.normal.textColor = Color.blue;
        _hightLightTextColumnStyle.normal.textColor = Color.red;
        _normalTextStyle.normal.textColor = Color.gray;
        _hightLightTextSelectedStyle.normal.textColor = Color.green;
    }
    private void OnGUI()
    {
        if (_thisWindow == null)
        {
            return;
        }

        if (_curWidth != _thisWindow.position.width || _curHeight != _thisWindow.position.height)
        {
            _curWidth = _thisWindow.position.width - 120;
            _curHeight = _thisWindow.position.height;
            _suggestColumn = (int)(_curWidth / _itemMinWidth);
            _suggestRow = (int)(_curHeight / _itemHeight);
        }
        EditorGUILayout.BeginHorizontal();//Top bar
        EditorGUILayout.LabelField("Search In:" + Constant.FilePath.RawXMLPath, GUILayout.MinWidth(50));
        _searchDirectory = EditorGUILayout.TextField(_searchDirectory, GUILayout.MinWidth(200));

        if (GUILayout.Button("Search", GUILayout.Width(60)))
        {
            _excelInfos = new List<ExcelInfo>();
            foreach (var file in Directory.GetFiles(Constant.FilePath.RawXMLPath + _searchDirectory))
            {
                ExcelInfo excelInfo = new ExcelInfo(file, _searchDirectory);
                if(Regex.IsMatch(file, "^(?!.*\\.~).*(.csv)$"))
                    _excelInfos.Add(excelInfo);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(); //BEGIN Whole Window
        EditorGUILayout.BeginVertical(GUILayout.Width(100)); // BEGIN Sidebar
        if (GUILayout.Button("Generate All"))
        {
            GenerateAllToJson();
            if (_totalThreadCount == _successThreadCount)
            {
                EditorUtility.DisplayDialog("Export Successful",
                    "All " + _totalThreadCount + " export successfully", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Some error exist",
                    "" + _successThreadCount + " out of " + _totalThreadCount +
                    " export successfully, see error in the debug console", "OK");
            }
        }

        if (GUILayout.Button("Export Json To CS"))
        {
            GenerateAllToCsFile();
        }
        if (_excelInfos != null)
        {
            foreach (var excelInfo in _excelInfos)
            {
                if (GUILayout.Button(excelInfo.ShortName))
                {
                    _currentInfo = excelInfo;
                    _scrollPos = Vector2.zero;
                    _tableData = new List<List<string>>();
                }
            }
        }
        EditorGUILayout.EndVertical(); // END Sidebar
        
        Rect rect = EditorGUILayout.GetControlRect(false, _thisWindow.position.height, GUILayout.Width(1));
        EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 1));

        //Board Editor
        EditorGUILayout.BeginVertical(); // BEGIN Board Editor
        SetUpTableController();
        if (_currentInfo != null)
        {
            RenderTable(_currentInfo.Path);
        }
        EditorGUILayout.EndVertical(); // END Row Fill Column
        
        EditorGUILayout.EndHorizontal(); // END Whole Window
    }

    private void GenerateAllToJson()
    {
        if (Directory.Exists(Constant.FilePath.JsonRootPath))
        {
            Directory.Delete(Constant.FilePath.JsonRootPath, true);
        }
        _totalThreadCount = 0;
        _successThreadCount = 0;
        Stack<string> directories = new Stack<string>();
        List<Thread> threads = new List<Thread>();
        directories.Push(Constant.FilePath.RawXMLPath);
        while (directories.Any())
        {
            string topDirectory = directories.Pop();
            string[] subFile = Directory.GetFiles(topDirectory);
            foreach (var file in subFile)
            {
                string parentDirectoryPath = topDirectory.Replace(Constant.FilePath.RawXMLPath, "");
                ExcelInfo excelInfo = new ExcelInfo(file, parentDirectoryPath);
                if (Regex.IsMatch(file, "^(?!.*\\.~).*(.csv)$"))
                {
                    _totalThreadCount++;
                    List<List<string>> dataTable = GenerateDataTable(excelInfo.Path);
                    if(dataTable == null)
                        continue;
                    Thread temp = new Thread(() => GenerateThreadFunc(dataTable, excelInfo));
                    threads.Add(temp);
                    temp.Start();
                }
            }
            string[] subDirectories = Directory.GetDirectories(topDirectory);
            foreach (var sub in subDirectories)
            {
                directories.Push(sub);
            }
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }
    }

    private void GenerateThreadFunc(List<List<string>> tableData, ExcelInfo info)
    {
        if (GenerateThisToJson(tableData, info) == 1)
        {
            Interlocked.Increment(ref _successThreadCount);
        }
    }
    private int GenerateThisToJson(List<List<string>> tableData, ExcelInfo info)
    {
        if (tableData == null)
        {
            EditorHelper.LogError("No table is selected");
            return 0;
        }

        if (tableData.Count <= 2)
        {
            EditorHelper.LogError("Data incomplete:" + info.Path);
            return 0;
        }

        List<string> AttributeName = tableData[0];
        List<string> AttributeType = tableData[1];

        int tableRow = tableData.Count;
        int tableColumn = tableData[0].Count;
        
        if (!Directory.Exists(Constant.FilePath.JsonRootPath + info.SearchDirectory))
        {
            Directory.CreateDirectory(Constant.FilePath.JsonRootPath + info.SearchDirectory);
        }

        using (StreamWriter writer =
               new StreamWriter(Constant.FilePath.JsonRootPath + info.SearchDirectory + "/" + info.ShortName +
                                ".json"))
        {
            writer.WriteLine("[");
            for (int i = 2; i < tableRow; i++)
            {
                writer.WriteLine("  {");
                writer.Write("    \"gid\":" + (i - 2) + ",\n");
                for (int j = 0; j < tableColumn; j++)
                {
                    writer.Write("    \"" + AttributeName[j] + "\":" + ConvertToType(tableData[i][j], AttributeType[j]));
                    if (j != tableColumn - 1)
                    {
                        writer.Write(",");
                    }
                    writer.WriteLine();
                }
                writer.Write("  }");
                if (i != tableRow - 1)
                {
                    writer.Write(",");
                }
                writer.WriteLine();
            }
            writer.WriteLine("]");
            writer.Flush();
            writer.Close();
        }

        return 1;
    }
    private void SetUpTableController()
    {
        if (GUILayout.Button("Generate This"))
        {
            if (GenerateThisToJson(_tableData, _currentInfo) == 1)
            {
                EditorUtility.DisplayDialog("Export Successful",
                    _currentInfo.ShortName + ".json is now available in " + Constant.FilePath.JsonRootPath +
                    _currentInfo.SearchDirectory, "OK");
            }
        }
        
        if (GUILayout.Button("UP Row"))
        {
            _scrollPos.x = _scrollPos.x - 1 < 0 ? _scrollPos.x : _scrollPos.x - 1;
        }

        if (GUILayout.Button("Down Row"))
        {
            _scrollPos.x = _scrollPos.x + 1 >= _tableRow ? _scrollPos.x : _scrollPos.x + 1;
        }

        if (GUILayout.Button("Right Column"))
        {
            _scrollPos.y = _scrollPos.y + 1 >= _tableColumn ? _scrollPos.y : _scrollPos.y + 1;
        }

        if (GUILayout.Button("Left Column"))
        {
            _scrollPos.y = _scrollPos.y - 1 < 0 ? _scrollPos.y : _scrollPos.y - 1;
        }
    }
    private string ConvertToType(string content, string type)
    {
        if (type == Constant.JsonType.Int)
        {
            return content;
        }

        if (type == Constant.JsonType.String)
        {
            return "\"" + content + "\"";
        }

        if (type == Constant.JsonType.Bool)
        {
            return content;
        }

        if (type == Constant.JsonType.ArrayInt) // "[1,2,3,4]"
        {
            string output = "";
            output += "[";
            string substring = content.Substring(2, content.Length - 3);
            output += substring;

            output += "]";
            return output;
        }

        if (type == Constant.JsonType.ArrayString)
        {
            string output = "";
            output += "[";
            string substring = content.Substring(2, content.Length - 3);
            output += substring;
            string[] elements = substring.Split(',');
            for (int i = 0; i < elements.Length; i++)
            {
                output += "\"" +elements[i]+"\"";
                if (i != elements.Length - 1)
                {
                    output += ",";
                }
            }
            output += "]";
            return output;
        }

        throw new Exception("Got Unknown Type:"+type);
    }
    private List<List<string>> GenerateDataTable(string excelInfoPath)
    {
        if (!File.Exists(excelInfoPath))
        {
            EditorHelper.LogError("File not exists" + excelInfoPath);
            return null;
        }
        List<List<string>> dataTable = new List<List<string>>();
        using(var reader = new StreamReader(excelInfoPath))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                List<string> list = new List<string>();
                foreach (var value in values)
                {
                    list.Add(value);
                }
                dataTable.Add(list);
            }
        }

        return dataTable;
    }
    private void RenderTable(string excelInfoPath)
    {
        EditorGUILayout.BeginHorizontal();
        if (!_tableData.Any())
        {
            _tableData = GenerateDataTable(excelInfoPath);
            if (_tableData == null)
            {
                return;
            }

            _tableColumn = _tableData[0].Count;
            _tableRow = _tableData.Count;
        }

        //Start render table
        int alreadyRenderedRow = 0;
        int alreadyRenderedColumn = 0;
        int columnStart = (int)_scrollPos.y;
        if (columnStart + _suggestColumn > _tableColumn)
        {
            columnStart = _tableColumn - _suggestColumn > 0 ? _tableColumn - _suggestColumn : 0;
        }

        for (int i = columnStart; i < _tableColumn; i++)
        {
            EditorGUILayout.BeginVertical(); // BEGIN Sidebar
            alreadyRenderedRow = 0;
            int rowStart = (int)_scrollPos.y;
            if (rowStart + _suggestRow > _tableRow)
            {
                rowStart = _tableRow - _suggestRow > 0 ? _tableRow - _suggestRow : 0;
            }

            for (int j = rowStart; j < _tableRow; j++)
            {
                if (j == (int)_scrollPos.x && i == (int)_scrollPos.y)
                {
                    EditorGUILayout.LabelField(_tableData[j][i], _hightLightTextSelectedStyle, GUILayout.MinWidth(_itemMinWidth), GUILayout.Height(_itemHeight));
                }
                else if(j == (int)_scrollPos.x)
                {
                    EditorGUILayout.LabelField(_tableData[j][i], _hightLightTextRowStyle, GUILayout.MinWidth(_itemMinWidth), GUILayout.Height(_itemHeight));
                } 
                else if (i == (int)_scrollPos.y)
                {
                    EditorGUILayout.LabelField(_tableData[j][i], _hightLightTextColumnStyle, GUILayout.MinWidth(_itemMinWidth), GUILayout.Height(_itemHeight));
                } 
                else
                {
                    EditorGUILayout.LabelField(_tableData[j][i], _normalTextStyle, GUILayout.MinWidth(_itemMinWidth), GUILayout.Height(_itemHeight));
                }
                alreadyRenderedRow++;
                if(alreadyRenderedRow >= _suggestRow) 
                    break;
            }
            EditorGUILayout.EndVertical();
            alreadyRenderedColumn++;
            if(alreadyRenderedColumn >= _suggestColumn)
                break;
        }
        EditorGUILayout.EndHorizontal();
    }
    private void GenerateAllToCsFile()
    {
        if (File.Exists(Constant.FilePath.ConfigManagerFilePath))
        {
            File.Delete(Constant.FilePath.ConfigManagerFilePath);
        }

        StreamWriter writer = new StreamWriter(Constant.FilePath.ConfigManagerFilePath);
        writer.WriteLine(Constant.ConfigManager.Head);

        Stack<string> directories = new Stack<string>();
        Dictionary<string, string> namepairs = new Dictionary<string, string>();
        directories.Push(Constant.FilePath.RawXMLPath);
        while (directories.Any())
        {
            string topDirectory = directories.Pop();
            string[] subFile = Directory.GetFiles(topDirectory);
            foreach (var file in subFile)
            {
                string parentDirectoryPath = topDirectory.Replace(Constant.FilePath.RawXMLPath, "");
                ExcelInfo excelInfo = new ExcelInfo(file, parentDirectoryPath);
                if (Regex.IsMatch(file, "^(?!.*\\.~).*(.csv)$"))
                {
                    List<List<string>> dataTable = GenerateDataTable(excelInfo.Path);
                    if(dataTable == null)
                        continue;
        
                    if (dataTable.Count <= 2)
                    {
                        EditorHelper.LogError("Data incomplete:" + excelInfo.Path);
                        continue;
                    }
                    
                    List<string> AttributeName = dataTable[0];
                    List<string> AttributeType = dataTable[1];
                    namepairs.Add(excelInfo.ShortName, file);
                    writer.WriteLine(Constant.ConfigManager.Declaration + excelInfo.ShortName);
                    writer.WriteLine("        {");
                    writer.WriteLine(Constant.ConfigManager.Int + "gid;");
                    for (int i = 0; i < AttributeName.Count; i++)
                    {
                        if (AttributeType[i] == Constant.JsonType.Int)
                        {
                            writer.WriteLine(Constant.ConfigManager.Int + AttributeName[i] + ";");
                        } 
                        else if (AttributeType[i] == Constant.JsonType.Bool)
                        {
                            writer.WriteLine(Constant.ConfigManager.Bool + AttributeName[i] + ";");
                        } 
                        else if (AttributeType[i] == Constant.JsonType.String)
                        {
                            writer.WriteLine(Constant.ConfigManager.String + AttributeName[i] + ";");
                        } 
                        else if (AttributeType[i] == Constant.JsonType.ArrayInt)
                        {
                            writer.WriteLine(Constant.ConfigManager.IntList + AttributeName[i] + ";");
                        } 
                        else if (AttributeType[i] == Constant.JsonType.ArrayString)
                        {
                            writer.WriteLine(Constant.ConfigManager.StringList + AttributeName[i] + ";");
                        }
                        else
                        {
                            EditorHelper.LogError("Unknown type [" + AttributeType[i] + "] when generate CS file: " +
                                           excelInfo.Path);
                        }
                    }
                    writer.WriteLine("        }");
                }
            }
            
            string[] subDirectories = Directory.GetDirectories(topDirectory);
            foreach (var sub in subDirectories)
            {
                directories.Push(sub);
            }
        }
        writer.WriteLine(Constant.ConfigManager.Mid1);
        foreach (var name in namepairs)
        {
            writer.WriteLine("            public List<" + name.Key + "> " + name.Key + "List;");
        }
        writer.WriteLine(Constant.ConfigManager.Mid2);
        foreach (var name in namepairs)
        {
            string JsonPath = name.Value.Replace(Application.dataPath, "");
            writer.WriteLine("                "+name.Key+"List = JsonConvert.DeserializeObject<List<"+name.Key+">>(GetJsonContent(Application.dataPath+\""+JsonPath.Replace("XML", "Json").Replace(".csv", ".json")+"\"));");
        }
        writer.WriteLine(Constant.ConfigManager.Tail);
        writer.Flush();
        writer.Close();
        EditorUtility.DisplayDialog("Export Successful",
            "CSharp code export successfully", "OK");
    }
}
