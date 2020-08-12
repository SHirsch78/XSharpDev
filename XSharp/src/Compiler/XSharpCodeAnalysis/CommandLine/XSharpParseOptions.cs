﻿//
// Copyright (c) XSharp B.V.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0.
// See License.txt in the project root for license information.
//
using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.IO;
using LanguageService.CodeAnalysis.XSharp.SyntaxParser;

namespace Microsoft.CodeAnalysis.CSharp
{

    [Flags]
    public enum RuntimeAssemblies : int
    {
        None = 0 ,
        VulcanRT = 0x01,
        VulcanRTFuncs = 0x02,
        XSharpCore = 0x04,
        XSharpData = 0x08,
        XSharpRT = 0x10,
        XSharpVO = 0x20,
        XSharpXPP = 0x40,
        XSharpVFP = 0x80,
        SdkDefines = 0x100,
        VoSystem = 0x200,
        VoGui = 0x400,
        VoRdd = 0x800,
        VoSql = 0x1000,
        VoInet = 0x2000,
        VoConsole = 0x4000,
        VoReport = 0x8000,
        VoWin32 = 0x10000
    }

    [Flags]
    public enum ParseLevel : byte
    {
        Lex = 1,
        Parse = 2,
        SyntaxCheck = 3,
        Complete = 4
    }

    public enum XSharpTargetDLL : Byte
    {
        Other =0,
        Core = 1,
        Data = 2,
        RDD = 3,
        RT = 4,
        VO = 5,
        XPP = 6,
        VFP = 7, 
        VulcanRT = 8,    // strictly not a target but we use this in the OverloadResolution
        VulcanRTFuncs= 9,  // strictly not a target but we use this in the OverloadResolution
        VOWin32Api = 10,
        VOSystemClasses = 11,
        VORDDClasses = 12,
        VOSQLClasses = 13,
        VOGuiClasses = 14,
        VOInternetClasses = 15,
        VOConsoleClasses = 16,
    }

    public sealed partial class CSharpParseOptions
    {

        // Options that can be set from the outside
        // Some options are also handled by the parser
        // Other options have flags, for the preprocessor macros, such as __VO1__

        #region private fields (need to be access with HasOption)
        private bool ArrayZero= false;
        private bool FoxInheritUnknown = false;
        private bool InitLocals = false;
        private bool VOAllowMissingReturns = false;
        private bool VOClipperIntegerDivisions = false;
        private bool VOClipperCallingConvention = false;
        private bool VOImplicitCastsAndConversions = false;
        private bool VOFloatConstants = false;
        private bool VONullStrings = false;
        private bool MemVars = false;
        private bool UndeclaredMemVars = false;
        private bool VOStringComparisons = false;
        private bool XPPInheritFromAbstract = false;
        private bool XPPUntypedmain = false;

        #endregion
        public bool AllowUnsafe { get; private set; }
        public bool CaseSensitive { get; private set; }
        public int ClrVersion { get; private set; }
        public bool MacroScript { get; private set; }

        public XSharpTargetDLL TargetDLL { get; private set; }
        public bool DebugEnabled { get; private set; }
        public XSharpDialect Dialect { get; private set; }
        public string DefaultIncludeDir { get; private set; }
        public string WindowsDir { get; private set; }
        public string SystemDir { get; private set; }
        public bool NoStdDef { get; private set; }
        public bool DumpAST { get; private set; }
        public bool ShowDefs { get; private set; }
        public bool ShowIncludes { get; private set; }
        public bool NoClipCall { get; internal set; } 
        public ParseLevel ParseLevel { get; set; } = ParseLevel.Complete;
        public bool AllowNamedArguments { get; private set; }
        public bool PreprocessorOutput { get; private set; }
        public bool SaveAsCSharp { get; private set; }
        public string StdDefs { get; private set; }
        public bool Verbose { get; private set; }
        public bool VirtualInstanceMethods { get; private set; }
        public bool VOArithmeticConversions { get; private set; }
        public bool VOClipperConstructors{ get; private set; }

        public bool VoInitAxitMethods { get; private set; }
        public bool VOCompatibleIIF { get; private set; }
        public bool VOPreprocessorBehaviour { get; private set; }
        public bool VOResolveTypedFunctionPointersToPtr { get; private set; }
        public bool VOSignedUnsignedConversion { get; private set; }
        public string DefaultNamespace { get; private set; }
        public bool ImplicitNamespace { get; private set; }
        public bool HasRuntime { get { return this.Dialect.HasRuntime(); } }
        public bool SupportsMemvars { get { return this.Dialect.SupportsMemvars() && MemVars; } }
#if !VSPARSER
        public ImmutableArray<string> IncludePaths { get; private set; } = ImmutableArray.Create<string>();
#else
        public IList<string> IncludePaths { get; private set; } = new List<string>();
#endif
        public bool VulcanRTFuncsIncluded => RuntimeAssemblies.HasFlag(RuntimeAssemblies.VulcanRTFuncs);
        public bool VulcanRTIncluded => RuntimeAssemblies.HasFlag(RuntimeAssemblies.VulcanRT);
        public bool XSharpRuntime => RuntimeAssemblies.HasFlag(RuntimeAssemblies.XSharpRT) |
            RuntimeAssemblies.HasFlag(RuntimeAssemblies.XSharpCore);
        public bool VOUntypedAllowed { get; private set; } = true;

        public RuntimeAssemblies RuntimeAssemblies { get; private set; } = RuntimeAssemblies.None;
        public bool Overflow { get; private set; }
        public CSharpCommandLineArguments CommandLineArguments { get; private set; }
        public TextWriter ConsoleOutput { get; private set; }
        public bool cs => CaseSensitive;
        public bool vo1 => VoInitAxitMethods;
        public bool vo2 => VONullStrings;
        public bool vo3 => VirtualInstanceMethods;
        public bool vo4 => VOSignedUnsignedConversion;
        public bool vo5 => VOClipperCallingConvention;
        public bool vo6 => VOResolveTypedFunctionPointersToPtr;
        public bool vo7 => VOImplicitCastsAndConversions;
        public bool vo8 => VOPreprocessorBehaviour;
        public bool vo9 => VOAllowMissingReturns;
        public bool vo10 => VOCompatibleIIF;
        public bool vo11 => VOArithmeticConversions;
        public bool vo12 => VOClipperIntegerDivisions;
        public bool vo13 => VOStringComparisons;
        public bool vo14 => VOFloatConstants;
        public bool vo15 => VOUntypedAllowed;
        public bool vo16 => VOClipperConstructors;
        public bool xpp1 => XPPInheritFromAbstract;
        public bool xpp2 => XPPUntypedmain;
        public bool fox1 => FoxInheritUnknown;
        public void SetXSharpSpecificOptions(XSharpSpecificCompilationOptions opt)
        {
            if (opt != null)
            {
                ArrayZero = opt.ArrayZero;
                CaseSensitive = opt.CaseSensitive;
                ClrVersion = opt.ClrVersion;
                TargetDLL = opt.TargetDLL;
                Dialect = opt.Dialect;
                DefaultNamespace = opt.NameSpace;
                DefaultIncludeDir = opt.DefaultIncludeDir;
                ImplicitNamespace = opt.ImplicitNameSpace;
                DumpAST = opt.DumpAST;
                WindowsDir = opt.WindowsDir;
                SystemDir = opt.SystemDir;
                NoStdDef = opt.NoStdDef;
                NoClipCall = opt.NoClipCall;
                ShowDefs = opt.ShowDefs;
                ShowIncludes = opt.ShowIncludes;
                StdDefs = opt.StdDefs;
                Verbose = opt.Verbose;
                PreprocessorOutput = opt.PreProcessorOutput;
                ParseLevel = opt.ParseLevel;
#if !VSPARSER
                IncludePaths = opt.IncludePaths.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
#else
                IncludePaths = opt.IncludePaths.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
#endif
                VoInitAxitMethods = opt.Vo1;
                VONullStrings = opt.Vo2;
                VirtualInstanceMethods = opt.Vo3;
                VOSignedUnsignedConversion = opt.Vo4;
                VOClipperCallingConvention = opt.Vo5;
                VOResolveTypedFunctionPointersToPtr = opt.Vo6;
                VOImplicitCastsAndConversions = opt.Vo7;
                VOPreprocessorBehaviour = opt.Vo8;
                VOAllowMissingReturns = opt.Vo9;
                VOCompatibleIIF = opt.Vo10;
                VOArithmeticConversions = opt.Vo11;
                VOClipperIntegerDivisions = opt.Vo12;
                VOStringComparisons = opt.Vo13;
                VOFloatConstants = opt.Vo14;
                VOUntypedAllowed = opt.Vo15;
                VOClipperConstructors = opt.Vo16;
                XPPInheritFromAbstract = opt.Xpp1;
                XPPUntypedmain = opt.Xpp2;
                FoxInheritUnknown = opt.Fox1;
                RuntimeAssemblies = opt.RuntimeAssemblies;
                Overflow = opt.Overflow;
                ConsoleOutput = opt.ConsoleOutput;
                ParseLevel = opt.ParseLevel;
                AllowNamedArguments = opt.AllowNamedArguments;
                SaveAsCSharp = opt.SaveAsCSharp;
                MemVars = opt.MemVars;
                InitLocals = opt.InitLocals;
                UndeclaredMemVars = opt.UndeclaredMemVars;
                AllowUnsafe = opt.AllowUnsafe;
            }
            LanguageVersion = LanguageVersion.CSharp7_3;
        }

        public void SetOptions(CSharpCommandLineArguments opt)
        {
            if (opt != null)
            {
                DebugEnabled = opt.EmitPdb;
                CommandLineArguments = opt;
            }
            LanguageVersion = LanguageVersion.CSharp7_3;
        }

        public void SetXSharpSpecificOptions(CSharpParseOptions opt)
        {
            ArrayZero = opt.ArrayZero;
            ClrVersion = opt.ClrVersion;
            CaseSensitive = opt.CaseSensitive;
            TargetDLL = opt.TargetDLL;
            MacroScript = opt.MacroScript;
            DebugEnabled = opt.DebugEnabled;
            DefaultIncludeDir = opt.DefaultIncludeDir;
            Dialect = opt.Dialect;
            DumpAST = opt.DumpAST;
            WindowsDir = opt.WindowsDir;
            SystemDir = opt.SystemDir;
            DefaultNamespace = opt.DefaultNamespace;
            ImplicitNamespace = opt.ImplicitNamespace;
            IncludePaths = opt.IncludePaths;
            ShowDefs = opt.ShowDefs;
            ShowIncludes = opt.ShowIncludes;
            NoStdDef = opt.NoStdDef;
            NoClipCall = opt.NoClipCall;
            PreprocessorOutput = opt.PreprocessorOutput;
            ParseLevel = opt.ParseLevel;
            SaveAsCSharp = opt.SaveAsCSharp;
            StdDefs = opt.StdDefs;
            Verbose = opt.Verbose;
            AllowNamedArguments = opt.AllowNamedArguments;
            VoInitAxitMethods = opt.VoInitAxitMethods; // vo1
            VONullStrings = opt.VONullStrings; // vo2
            VirtualInstanceMethods = opt.VirtualInstanceMethods; // vo3
            VOSignedUnsignedConversion = opt.VOSignedUnsignedConversion; // vo4
            VOClipperCallingConvention = opt.VOClipperCallingConvention;  // vo5
            VOResolveTypedFunctionPointersToPtr = opt.VOResolveTypedFunctionPointersToPtr; // vo6
            VOImplicitCastsAndConversions = opt.VOImplicitCastsAndConversions; // vo7
            VOPreprocessorBehaviour = opt.VOPreprocessorBehaviour; // vo8
            VOAllowMissingReturns = opt.VOAllowMissingReturns; // vo9
            VOCompatibleIIF = opt.VOCompatibleIIF; // vo10
            VOArithmeticConversions = opt.VOArithmeticConversions; // vo11
            VOClipperIntegerDivisions = opt.VOClipperIntegerDivisions; // vo12
            VOStringComparisons = opt.VOStringComparisons; // vo13
            VOFloatConstants = opt.VOFloatConstants; // vo14
            VOUntypedAllowed = opt.VOUntypedAllowed; // vo15
            VOClipperConstructors = opt.VOClipperConstructors; // vo16
            XPPInheritFromAbstract = opt.XPPInheritFromAbstract; // xpp1
            XPPUntypedmain = opt.XPPUntypedmain;    // xpp2
            FoxInheritUnknown = opt.FoxInheritUnknown;  // fox1

            RuntimeAssemblies = opt.RuntimeAssemblies;
            Overflow = opt.Overflow;
            ConsoleOutput = opt.ConsoleOutput;

            ParseLevel = opt.ParseLevel;
#if !VSPARSER
            CommandLineArguments = opt.CommandLineArguments;
#endif
            LanguageVersion = LanguageVersion.CSharp7_3;
            MemVars = opt.MemVars;
            UndeclaredMemVars = opt.UndeclaredMemVars;
            InitLocals = opt.InitLocals;
            AllowUnsafe = opt.AllowUnsafe;
        }

        public CSharpParseOptions WithXSharpSpecificOptions(XSharpSpecificCompilationOptions opt)
        {
            var result = new CSharpParseOptions(this);
            result.SetXSharpSpecificOptions(opt);
            return result;
        }

        public CSharpParseOptions WithOutput(TextWriter consoleOutput)
        {
            if (consoleOutput == this.ConsoleOutput)
            {
                return this;
            }
            var result = new CSharpParseOptions(this);
            result.SetXSharpSpecificOptions(this);
            result.ConsoleOutput = consoleOutput;
            result.LanguageVersion = LanguageVersion.CSharp7_3;
            return result;
        }
        public CSharpParseOptions WithMacroScript(bool macroScript)
        {
            if (macroScript == this.MacroScript)
            {
                return this;
            }
            var result = new CSharpParseOptions(this);
            result.SetXSharpSpecificOptions(this);
            result.MacroScript = macroScript;
            result.LanguageVersion = LanguageVersion.CSharp7_3;
            return result;
        }

        public bool HasOption (CompilerOption option, XSharpParserRuleContext context, IList<PragmaOption> options )
        {
            switch (option)
            {
                case CompilerOption.ArrayZero: // az
                    return CheckOption(option, ArrayZero, context, options);

                case CompilerOption.InitLocals: // initlocals
                    return CheckOption(option, InitLocals, context, options);

                case CompilerOption.LateBinding:  // lb is handled in cde generation
                    return false;

                case CompilerOption.MemVars: // memvar
                    return CheckOption(option, MemVars, context, options);

                case CompilerOption.UndeclaredMemVars: // memvar
                    return CheckOption(option, UndeclaredMemVars, context, options);

                case CompilerOption.NullStrings: // vo2
                    return CheckOption(option, VONullStrings, context, options);

                case CompilerOption.ClipperCallingConvention: // vo5
                    return CheckOption(option, VOClipperCallingConvention, context, options);

                case CompilerOption.ImplicitCastsAndConversions: // vo7
                    return CheckOption(option, VOImplicitCastsAndConversions, context, options);

                case CompilerOption.AllowMissingReturns: // vo9
                    return CheckOption(option, VOAllowMissingReturns, context, options);

                case CompilerOption.ClipperIntegerDivisions: // vo12
                    return CheckOption(option, VOClipperIntegerDivisions, context, options);

                case CompilerOption.FloatConstants: // vo14
                    return CheckOption(option, VOFloatConstants, context, options);
            }
            return false;
        }

        public bool CheckOption(CompilerOption option, bool defaultValue, XSharpParserRuleContext context, IList<PragmaOption> options)
        {
            bool result = defaultValue;
            if (context != null && options != null && options.Count > 0)
            {
                int line = context.Start.Line;
                foreach (var pragmaoption in options)
                {
                    if (pragmaoption.Line > line)
                        break;
                    if (pragmaoption.Option == option || pragmaoption.Option == CompilerOption.All)
                    {
                        switch (pragmaoption.State)
                        {
                            case Pragmastate.On:
                                result = true;
                                break;
                            case Pragmastate.Off:
                                result = false;
                                break;
                            case Pragmastate.Default:
                                result = defaultValue;
                                break;
                        }
                    }
                }
            }
            return result;
        }

    }
}
