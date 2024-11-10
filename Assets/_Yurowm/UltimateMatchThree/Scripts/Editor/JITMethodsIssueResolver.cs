#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Yurowm.GameCore;

namespace Yurowm.EditorCore {
    [BerryPanelGroup("Development")]
    [BerryPanelTab("JIT Methods Resolver")]
    public class JITMethodIssueResolver : MetaEditor {

        FileInfo scriptFile;
        const string fileName = "JITMethodIssue.cs";

        string pattern = @"// Auto generated script. Don't change it.
#if UNITY_IOS
public static class JITMethodIssue {
    static JITMethodIssue() {
        if (1 == 0) {
            object p = new object();

            *METHODS*
        }
    }
}
#endif
";

        BuildTarget buildTarget;

        public override bool Initialize() {
            buildTarget = EditorUserBuildSettings.activeBuildTarget;

            DirectoryInfo directory = new DirectoryInfo(Path.Combine(Application.dataPath, "Generated Scriptes"));
            if (!directory.Exists) directory.Create();

            scriptFile = new FileInfo(Path.Combine(directory.FullName, fileName));

            return true;
        }

        public override void OnGUI() {
            using (new GUIHelper.Lock(EditorApplication.isCompiling)) {
                if (GUILayout.Button("Generate script", GUILayout.Width(200))) {
                    Debug.Log(string.Join("\r\n", AppDomain.CurrentDomain.GetAssemblies().Select(x => x.FullName).ToArray()));


                    List<MethodInfo> methods = AppDomain.CurrentDomain.GetAssemblies()
                        .First(x => x.FullName.StartsWith("Assembly-CSharp,")).GetTypes()
                        .Where(x => !x.IsAbstract && x.IsPublic && x.GetAttribute<ObsoleteAttribute>() == null)
                        .SelectMany(x => x.GetMethods())
                        .Where(x => x.IsStatic
                            && !(x.Name.StartsWith("get_") || x.Name.StartsWith("set_") || x.Name.StartsWith("op_") ||
                            x.Name.StartsWith("add_") || x.Name.StartsWith("remove_")))
                            .ToList();

                    List<string> lines = new List<string>();

                    foreach (MethodInfo method in methods) {
                        string line = MethodToLine(method);
                        if (line.Contains("&")) continue;
                        line = reGA.Replace(line, ""); 
                        lines.Add(line);
                    } 

                    
                    string code = pattern.Replace("*METHODS*", string.Join(";\r\n\t\t\t", lines.ToArray()) + ";");

                    using (var stream = scriptFile.CreateText())
                        stream.Write(code);

                    AssetDatabase.Refresh(ImportAssetOptions.Default);
                }
            }
        }

        string MethodToLine(MethodInfo method) {
            string result = TypeToLine(method.DeclaringType)
                    + "." + method.Name
                    + "(" + string.Join(", ", method.GetParameters()
                            .Select(x => string.Format("({0}) p", TypeToLine(x.ParameterType))).ToArray()) + ")";
            return result;
        }

        Regex reGA = new Regex(@"`\d+");

        string TypeToLine(Type type) {
            string result = "";

            Type declaringType = type;
            string dType = "";

            while (declaringType != null) {
                dType = declaringType.Name;

                Type[] genericArguments = declaringType.GetGenericArguments();
                if (genericArguments.Length > 0) {
                    dType += string.Format("<{0}>", string.Join(", ",
                        genericArguments.Select(x => TypeToLine(x)).ToArray()));
                }

                result = dType + (result.Length > 0 ? "." : "") + result;

                declaringType = declaringType.DeclaringType;
            }

            if (type.Namespace != null) result = type.Namespace + "." + result;
            
            return result;
        }
    }
}
#endif