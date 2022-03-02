using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Editor.EditorTool
{
    public class EditorHelper
    {
        private static string GenerateOutput(string callingFunction, params string[] args)
        {
            string output = "[" + callingFunction + "] ";
            foreach (var arg in args)
            {
                output += arg;
                output += " ";
            }

            return output;
        }
        public static void Log(params string[] args)
        {
            StackTrace trace = new StackTrace();
            string callingFunction = trace.GetFrame(1).GetMethod().Name;
            string output = GenerateOutput(callingFunction, args);
            Debug.Log(output);
        }

        public static void LogWarning(params string[] args)
        {
            StackTrace trace = new StackTrace();
            string callingFunction = trace.GetFrame(1).GetMethod().Name;
            string output = GenerateOutput(callingFunction, args);
            Debug.LogWarning(output);
        }
        
        public static void LogError(params string[] args)
        {
            StackTrace trace = new StackTrace();
            string callingFunction = trace.GetFrame(1).GetMethod().Name;
            string output = GenerateOutput(callingFunction, args);
            Debug.LogError(output);
        }
    }
}