﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

namespace XSharp.Build
{
    public class Xsc : ManagedCompiler
    {
        // These are settings
        internal string REG_KEY = @"HKEY_LOCAL_MACHINE\" + XSharp.Constants.RegistryKey;

        #region The values are set through .targets
        // The  fullpath to Compiler

        // Todo: store the values in base.bag

        public Boolean AZ { get; set; }
        public Boolean CS { get; set; }
        public Boolean LB { get; set; }
        public Boolean UnSafe { get; set; }
        public Boolean OVF { get; set; }
        public Boolean PPO { get; set; }
        public Boolean NS { get; set; }
        public Boolean INS { get; set; }
        public String IncludePaths { get; set; }
        public Boolean NoStandardDefs { get; set; }
        public string RootNameSpace{ get; set; }
        public Boolean VO1 { get; set; }
        public Boolean VO2 { get; set; }
        public Boolean VO3 { get; set; }
        public Boolean VO4 { get; set; }
        public Boolean VO5 { get; set; }
        public Boolean VO6 { get; set; }
        public Boolean VO7 { get; set; }
        public Boolean VO8 { get; set; }
        public Boolean VO9 { get; set; }
        public Boolean VO10 { get; set; }
        public Boolean VO11{ get; set; }
        public Boolean VO12 { get; set; }
        public Boolean VO13 { get; set; }
        public Boolean VO14 { get; set; }

        public string CompilerPath { get; set; }
        // Misc. (unknown at that time) CommandLine options
        public string CommandLineOption { get; set; }


        // properties copied from the Csc task
        public string ApplicationConfiguration { get; set; }
        public string BaseAddress { get; set; }
        public bool CheckForOverflowUnderflow { get; set; }
        public string DisabledWarnings { get; set; }
        public string DocumentationFile { get; set; }
        public bool ErrorEndLocation { get; set; }
        public string ErrorReport { get; set; }
        public bool GenerateFullPaths { get; set; }
        public string LangVersion { get; set; }
        public string ModuleAssemblyName { get; set; }


        public bool NoStandardLib { get; set; }


        public string PdbFile { get; set; }


        public string PreferredUILang { get; set; }


        public bool UseHostCompilerIfAvailable { get; set; }


        public string VsSessionGuid { get; set; }


        public int WarningLevel { get; set; }
        public string WarningsAsErrors { get; set; }
        public string WarningsNotAsErrors { get; set; }

















        #endregion


        public Xsc(): base()
        {
            //ystem.Diagnostics.Debugger.Launch();
        }

        protected override string ToolName
        {
            get
            {
                return "xsc.exe";
            }
        }

        protected override string GenerateFullPathToTool()
        {
            return FindXsc(this.ToolName);
        }


        protected override void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
        {
            try
            {
                AddResponseFileCommandsImpl(commandLine);
            }
            catch (Exception ex)
            {
                Trace.Assert(false, ex.ToString());
                throw;
            }
        }

        protected override string GenerateCommandLineCommands()
        {
            //return "/shared";
            return "/noconfig";
        }


        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            int iResult;
            DateTime start = DateTime.Now;
            iResult = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
            var time = DateTime.Now - start;
            var timestring = time.ToString();
            Log.LogMessageFromText("XSharp Compilation time: "+timestring, MessageImportance.High);
            return iResult;
        }

        private string FindXsc(string toolName)
        {
            if (string.IsNullOrEmpty(CompilerPath))
            {
                // If used after MSI Installer, value should be in the Registry
                string InstallPath = String.Empty;
                try
                {
                    InstallPath = (string)Registry.GetValue(REG_KEY, XSharp.Constants.RegistryValue, "");
                    
                }
                catch (Exception) { }
                // Nothing in the Registry ?
                if (!string.IsNullOrEmpty(InstallPath))
                {
                    CompilerPath = AddSlash(InstallPath) + "Bin\\";
                }
                // Allow to override the path when developing.
                // Please note that this must be a complete path, for example "d:\Xsharp\Dev\XSharp\Binaries\Debug"
                string DevPath = System.Environment.GetEnvironmentVariable("XSHARPDEV");
                if (!string.IsNullOrEmpty(DevPath))
                {
                    CompilerPath = AddSlash(DevPath);
                }
                if (string.IsNullOrEmpty(CompilerPath))
                {
                    // get the path of the current DLL
                    CompilerPath = new Uri(typeof(Xsc).Assembly.CodeBase).LocalPath;
                }
            }
            // Search the compiler at the same place
            var xsc_file = Path.Combine(Path.GetDirectoryName(CompilerPath), toolName);
            if (File.Exists(xsc_file))
            {
                // The tool has been found.
                return xsc_file;
            }
            // Return the tool name itself.
            // Windows will search common paths for the tool.
            return toolName;
        }

        protected string AddSlash(string Path)
        {
            if (!String.IsNullOrEmpty(Path) && !Path.EndsWith("\\"))
                Path += "\\";
            return Path;

        }
        protected void AddResponseFileCommandsImpl(CommandLineBuilderExtension commandLine)
        {
            if (OutputAssembly == null && Sources != null && Sources.Length > 0 && ResponseFiles == null)
            {
                try
                {
                    OutputAssembly = new TaskItem(Path.GetFileNameWithoutExtension(Sources[0].ItemSpec));
                }
                catch (ArgumentException exception)
                {
                    throw new ArgumentException(exception.Message, "Sources", exception);
                }

                var outputAssembly = OutputAssembly;
                switch (TargetType.ToLowerInvariant())
                {
                    case "library":
                        outputAssembly.ItemSpec = outputAssembly.ItemSpec + ".dll";
                        break;

                    default:
                        outputAssembly.ItemSpec = outputAssembly.ItemSpec + ".exe";
                        break;
                }
            }

            // Add sources
            if (this.Sources != null)
            {
                commandLine.AppendFileNamesIfNotNull(this.Sources, "\n");
            }

            if (null != base.References)
            {
                foreach (var it in base.References)
                    commandLine.AppendSwitchIfNotNull("\n/reference:", it.ItemSpec);
            }
            // noconfig and fullpaths
            commandLine.AppendTextUnquoted("\n/fullpaths");

            // target and platform
            commandLine.AppendSwitch("\n/target:" + this.TargetType);
            commandLine.AppendSwitchIfNotNull("\n/platform:", this.Platform);
            commandLine.AppendSwitchIfNotNull("\n/baseaddress:", this.BaseAddress);
            if (this.Optimize) { 
                commandLine.AppendSwitch("\n/optimize");
            }

            if (String.IsNullOrEmpty(DebugType) || DebugType.ToLower() == "none")
                commandLine.AppendSwitch("\n/debug-");
            else
                commandLine.AppendSwitch("\n/debug:"+ this.DebugType);

            if (!String.IsNullOrEmpty(PdbFile))
                commandLine.AppendSwitch("\n/pdb:" + this.PdbFile);
            // Default Namespace
            if (NS)
            {
                commandLine.AppendSwitch("\n/ns:" + this.RootNameSpace);
            }
            if (WarningLevel < 0 || WarningLevel > 4)
            {
                WarningLevel = 4;
            }
            commandLine.AppendSwitch("/warn:" + WarningLevel.ToString());
            AppendLogicSwitch(commandLine, "/warnaserror", TreatWarningsAsErrors);
            if (!String.IsNullOrEmpty(DisabledWarnings))
            {
                string[] warnings = DisabledWarnings.Split(new char[] { ' ', ',', ';' });
                string warninglist = String.Empty;
                foreach (string s in warnings)
                {
                    if (warninglist.Length > 0)
                        warninglist += ";";
                    warninglist += s;
                }
                if (warninglist.Length > 0)
                    commandLine.AppendSwitch("/nowarn:" + warninglist);
            }
            // Compatibility
            AppendLogicSwitch(commandLine, "/az", AZ);
            AppendLogicSwitch(commandLine, "/cs", CS);
            AppendLogicSwitch(commandLine, "/ins", INS);
            AppendLogicSwitch(commandLine, "/lb", LB);
            AppendLogicSwitch(commandLine, "/ovf", OVF);
            AppendLogicSwitch(commandLine, "/ppo", PPO);
            AppendLogicSwitch(commandLine, "/unsafe", UnSafe);
            AppendLogicSwitch(commandLine, "/vo1", VO1);
            AppendLogicSwitch(commandLine, "/vo2", VO2);
            AppendLogicSwitch(commandLine, "/vo3", VO3);
            AppendLogicSwitch(commandLine, "/vo4", VO4);
            AppendLogicSwitch(commandLine, "/vo5", VO5);
            AppendLogicSwitch(commandLine, "/vo6", VO6);
            AppendLogicSwitch(commandLine, "/vo7", VO7);
            AppendLogicSwitch(commandLine, "/vo8", VO8);
            AppendLogicSwitch(commandLine, "/vo9", VO9);
            AppendLogicSwitch(commandLine, "/vo10", VO10);
            AppendLogicSwitch(commandLine, "/vo11", VO11);
            AppendLogicSwitch(commandLine, "/vo12", VO12);
            AppendLogicSwitch(commandLine, "/vo13", VO13);
            AppendLogicSwitch(commandLine, "/vo14", VO14);

            // Output assembly name
            commandLine.AppendSwitchIfNotNull("\n/out:", OutputAssembly);
            // User-defined CommandLine Option (in order to support switches unknown at that time)
            // cannot use appendswitch because it will quote the string when there are embedded spaces
            if (!String.IsNullOrEmpty(this.CommandLineOption))
            {
                commandLine.AppendTextUnquoted("\n"+this.CommandLineOption);
            }

            // From C# Build tool
            /*
            commandLine.AppendSwitchIfNotNull("/lib:", base.AdditionalLibPaths, ",");
            commandLine.AppendPlusOrMinusSwitch("/checked", base._store, "CheckForOverflowUnderflow");
            char[] splitOn = new char[] { ';', ',' };
            commandLine.AppendSwitchWithSplitting("/nowarn:", this.DisabledWarnings, ",", splitOn);
            commandLine.AppendWhenTrue("/fullpaths", base._store, "GenerateFullPaths");
            commandLine.AppendSwitchIfNotNull("/langversion:", this.LangVersion);
            commandLine.AppendSwitchIfNotNull("/moduleassemblyname:", this.ModuleAssemblyName);
            commandLine.AppendSwitchIfNotNull("/pdb:", this.PdbFile);
            commandLine.AppendPlusOrMinusSwitch("/nostdlib", base._store, "NoStandardLib");
            commandLine.AppendSwitchIfNotNull("/platform:", base.PlatformWith32BitPreference);
            commandLine.AppendSwitchIfNotNull("/errorreport:", this.ErrorReport);
            commandLine.AppendSwitchIfNotNull("/warn:", this.WarningLevel);
            commandLine.AppendSwitchIfNotNull("/doc:", this.DocumentationFile);
            commandLine.AppendSwitchIfNotNull("/baseaddress:", this.BaseAddress);
            commandLine.AppendSwitchUnquotedIfNotNull("/define:", GetDefineConstantsSwitch(base.DefineConstants, base.Log));
            commandLine.AppendSwitchIfNotNull("/win32res:", base.Win32Resource);
            commandLine.AppendSwitchIfNotNull("/main:", base.MainEntryPoint);
            commandLine.AppendSwitchIfNotNull("/appconfig:", this.ApplicationConfiguration);
            commandLine.AppendLogicSwitch("/errorendlocation", this.ErrorEndLocation);
            commandLine.AppendSwitchIfNotNull("/preferreduilang:", this.PreferredUILang);
            commandLine.AppendPlusOrMinusSwitch("/highentropyva", base._store, "HighEntropyVA");
            */

            //
        }

        protected void AppendLogicSwitch(CommandLineBuilderExtension commandLine, string Switch, Boolean Option)
        {
            
            if (Option)
            {
                commandLine.AppendSwitch(Switch+"+");
            }
            else
            {
                commandLine.AppendSwitch(Switch + "-");

            }
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            try
            {
                base.LogEventsFromTextOutput(singleLine, messageImportance);
            }
            catch (Exception e)
            {
                object[] messageArgs = new object[0];
                base.Log.LogMessage(MessageImportance.High, singleLine, messageArgs);
                base.Log.LogErrorFromException(e, true);
            }
        }
    }

}
