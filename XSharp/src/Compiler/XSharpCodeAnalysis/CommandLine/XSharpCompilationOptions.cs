﻿/*
   Copyright 2016-2017 XSharp B.V.

Licensed under the X# compiler source code License, Version 1.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.xsharp.info/licenses

Unless required by applicable law or agreed to in writing, software
Distributed under the License is distributed on an "as is" basis,
without warranties or conditions of any kind, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
using System.IO;
namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents various options that affect compilation, such as
    /// whether to emit an executable or a library, whether to optimize
    /// generated code, and so on.
    /// </summary>
    public sealed class XSharpSpecificCompilationOptions
    {
        static string _defaultIncludeDir;
        static string _windir;
        static string _sysdir;
        public static void SetDefaultIncludeDir (string dir)
        {
            _defaultIncludeDir = dir;
        }
        public static void SetWinDir(string dir)
        {
            _windir = dir;
        }
        public static void SetSysDir(string dir)
        {
            _sysdir = dir;
        }
        public XSharpSpecificCompilationOptions()
        {
            // All defaults are set at property level
        }

        public bool ArrayZero { get; internal set; } = false;
        public bool CaseSensitive { get; internal set; } = false;
        public bool CompactFramework { get; internal set; } = false;
        public string DefaultIncludeDir { get; internal set; } = _defaultIncludeDir;
        public XSharpDialect Dialect { get; internal set; } = XSharpDialect.Core;
        public string WindowsDir { get; internal set; } = _windir;
        public string SystemDir { get; internal set; } = _sysdir;
        public string IncludePaths { get; internal set; } = "";
        public bool ImplicitNameSpace { get; internal set; } = false;
        public bool LateBinding { get; internal set; } = false;
        public bool NoRun { get; internal set; } = true;
        public bool NoStdDef { get; internal set; } = false;
        public string NameSpace { get; set; } = "";
        public bool ParseOnly { get; internal set; } = false;
        public bool PreProcessorOutput { get; internal set; } = false;
        public bool ShowDefs { get; internal set; } = false;
        public bool ShowIncludes { get; internal set; } = false;
        public bool SyntaxCheck { get; internal set; } = false;
        public bool Verbose { get; internal set; } = false;
        public bool Vo1 { get; internal set; } = false;
        public bool Vo2 { get; internal set; } = false;
        public bool Vo3 { get; internal set; } = false;
        public bool Vo4 { get; internal set; } = false;
        public bool Vo5 { get; internal set; } = false;
        public bool Vo6 { get; internal set; } = false;
        public bool Vo7 { get; internal set; } = false;
        public bool Vo8 { get; internal set; } = false;
        public bool Vo9 { get; internal set; } = false;
        public bool Vo10 { get; internal set; } = false;
        public bool Vo11 { get; internal set; } = false;
        public bool Vo12 { get; internal set; } = false;
        public bool Vo13{ get; internal set; } = false;
        public bool Vo14 { get; internal set; } = false;
        public bool Vo15 { get; internal set; } = false;
        public bool Vo16 { get; internal set; } = false;
        public bool VulcanRTFuncsIncluded => VulcanAssemblies.HasFlag(VulcanAssemblies.VulcanRTFuncs);
        public bool VulcanRTIncluded => VulcanAssemblies.HasFlag(VulcanAssemblies.VulcanRT);
        public bool CreatingRuntime { get; internal set; } = false;
        public bool ExplicitVO15 { get; internal set; } = false;
        internal VulcanAssemblies VulcanAssemblies { get; set; } = VulcanAssemblies.None;
        public bool Overflow { get; internal set; } = false;
        public bool OverflowHasBeenSet { get; internal set; } = false;
        public string PreviousArgument { get; internal set; } = string.Empty;
        public TextWriter ConsoleOutput { get; internal set; }
    }
}