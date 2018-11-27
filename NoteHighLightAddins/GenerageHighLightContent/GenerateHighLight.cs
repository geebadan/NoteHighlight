﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Configuration;
using Helper;

namespace GenerateHighLightContent
{
    public class GenerateHighLight : IGenerateHighLight
    {
        #region -- Field and Property --

        public string Content { get; set; }

        public string CodeType { get; set; }

        public string HighLightStyle { get; set; }

        public bool ShowLineNumber { get; set; }

        public string FileName { get; set; }

        /// <summary> highlight.exe 參數 設定於 App.config 的 HighLightSection 區塊 </summary>
        private HighLightSection _section;
        #endregion

        #region -- IGenerageHighLight Member --

        /// <summary> 呼叫highlight.exe 產生高亮後的html </summary>
        /// <returns>回傳 Html 所在的路徑</returns>
        public string GenerateHighLightCode(HighLightParameter parameter)
        {
            var result = ConfigurationManager.GetSection("HighLightSection");
            _section = result as HighLightSection;

            InitParameter(parameter);

            string tempPath = Path.GetTempPath();
            string inputFileName = Path.Combine(tempPath, FileName);
            string outputFileName = Path.Combine(tempPath, FileName) + ".html";

            //System.Diagnostics.Debug.WriteLine(inputFileName);
            //System.Diagnostics.Debug.WriteLine(Content);
            //System.Diagnostics.Debug.WriteLine(_section);
            //System.Diagnostics.Debug.WriteLine(_section.FolderName);
            //System.Diagnostics.Debug.WriteLine(ProcessHelper.GetAssemblyLocationDirectory);
            File.WriteAllText(inputFileName, Content, Encoding.UTF8);
            
            var workingDirectory = Path.Combine(ProcessHelper.GetAssemblyLocationDirectory, _section.FolderName);

            ProcessHelper helper = new ProcessHelper(workingDirectory, _section.ProcessName);
            helper.Arguments = GenerateArguments(inputFileName, outputFileName);
            helper.IsWaitForInputIdle = false;
            helper.WindowStyle = ProcessWindowStyle.Hidden;

            helper.ProcessStart();

            if (!File.Exists(outputFileName))
                throw new FileNotFoundException("找不到outputFile");

            File.Delete(inputFileName);
            return outputFileName;
        }

        /// <summary> 初始化參數 </summary>
        private void InitParameter(HighLightParameter parameter)
        {
            Content = parameter.Content;
            CodeType = parameter.CodeType;
            HighLightStyle = parameter.HighLightStyle;
            ShowLineNumber = parameter.ShowLineNumber;
            FileName = parameter.FileName;
        }

        /// <summary> 產生HighLight.exe 所需的參數 </summary>
        private string GenerateArguments(string inputFileName, string outputFileName)
        {
            if (_section == null) throw new Exception("Config 內找不到 HighLightSection 區段");

            StringBuilder sb = new StringBuilder();

            ReadConfigCollection(sb, _section.GeneralArguments);
            ReadConfigCollection(sb, _section.OutputArguments);

            if (ShowLineNumber)
                sb.Append(" " + _section.OutputArguments["LineNumbers"].Key);

            string arguments = sb.ToString().TemplateSubstitute(new
            {
                inputFileName = String.Format("\"{0}\"",inputFileName), 
                outputFileName = String.Format("\"{0}\"",outputFileName),
                codeType = CodeType,
                highLightStyle = HighLightStyle
            });

            return arguments;
        }

        /// <summary> 讀取 ConfigurationElementCollection </summary>
        private void ReadConfigCollection(StringBuilder sb, ConfigurationElementCollection collection)
        {
            foreach (Argument item in collection)
            {
                if (item.Option)
                    continue;

                sb.Append(item.Key);
                if (!String.IsNullOrEmpty(item.Value))
                    sb.Append(" " + item.Value);
                sb.Append(" ");
            }
        }

        #endregion
    }
}
