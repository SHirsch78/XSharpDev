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
#define UDCSUPPORT
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using LanguageService.CodeAnalysis.XSharp.SyntaxParser;
using System.Diagnostics;
namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal class XSharpPreprocessor 
    {
        const string PPOPrefix = "//PP ";
        #region Static Properties
        static Dictionary<String, CachedIncludeFile> includecache = new Dictionary<string, CachedIncludeFile>(StringComparer.OrdinalIgnoreCase);


        static void clearOldIncludes()
        {
            // Remove old includes that have not been used in the last 1 minutes
            lock (includecache)
            {
                var oldkeys = new List<string>();
                var compare = DateTime.Now.Subtract(new TimeSpan(0, 1, 0));
                foreach (var include in includecache.Values)
                {
                    if (include.LastUsed < compare)
                    {
                        oldkeys.Add(include.FileName);
                    }
                }
                foreach (var key in oldkeys)
                {
                    includecache.Remove(key);
                }
            }
        }

        static CachedIncludeFile getIncludeFile(string fileName)
        {
            CachedIncludeFile file = null;
            lock (includecache)
            {
                if (includecache.ContainsKey(fileName))
                {
                    file = includecache[fileName];
                    if (file.LastWritten != PortableShim.File.GetLastWriteTimeUtc(fileName))
                    {
                        includecache.Remove(fileName);
                        return null;
                    }
                    //DebugOutput("Found include file in cache: {0}", fileName);
                }
            }
            if (file != null)
            {
                file.LastUsed = DateTime.Now;
                // Now clone the file so the tokens may be manipulated
                file = file.Clone();
            }
            return file;
        }
        static CachedIncludeFile addIncludeFile(string fileName, XSharpToken[] tokens, SourceText text)
        {
            lock (includecache)
            {
                CachedIncludeFile file = getIncludeFile(fileName);
                if (file == null)
                {
                    file = new CachedIncludeFile();
                    includecache.Add(fileName, file);
                    file.LastUsed = DateTime.Now;
                }
                file.Tokens = tokens;
                file.Text = text;
                file.FileName = fileName;
                //DebugOutput("Add include file to cache: {0}", fileName);
                file.LastWritten = PortableShim.File.GetLastWriteTimeUtc(fileName);
                return file;
            }
        }
        #endregion

        class InputState
        {
            internal ITokenStream Tokens;
            internal int Index;
            internal string SourceFileName;
            internal string MappedFileName;
            internal int MappedLineDiff;
            internal bool isSymbol;
            internal string SymbolName;
            internal XSharpToken Symbol;
            internal InputState parent;
            internal PPRule udc;
            internal InputState(ITokenStream tokens)
            {
                Tokens = tokens;
                Index = 0;
                MappedLineDiff = 0;
                SourceFileName = null;
                parent = null;
                isSymbol = false;
            }

            internal int La()
            {
                if (Eof() && parent != null)
                    return parent.La();
                return Tokens.Get(Index).Type;
            }

            internal XSharpToken Lt()
            {
                if (Eof() && parent != null)
                    return parent.Lt();
                return (XSharpToken) Tokens.Get(Index);
            }

            internal bool Eof()
            {
                return Index >= Tokens.Size || Tokens.Get(Index).Type == IntStreamConstants.Eof;
            }

            internal bool Consume()
            {
                if (Eof())
                    return false;
                Index++;
                return true;
            }
        }

        internal class CachedIncludeFile
        {
            internal DateTime LastWritten { get; set; }
            internal String FileName { get; set; }
            internal XSharpToken[] Tokens { get; set; }
            internal SourceText Text { get; set; }
            internal DateTime LastUsed { get; set; }

            internal CachedIncludeFile Clone()
            {
                var clone = new CachedIncludeFile();
                clone.LastUsed = LastUsed;
                clone.FileName = FileName;
                clone.Text = Text;
                clone.LastWritten = LastWritten;
                clone.Tokens = new XSharpToken[Tokens.Length];
                for (int i = 0; i < Tokens.Length; i++)
                {
                    clone.Tokens[i] = new XSharpToken(Tokens[i]);
                }
                return clone;
            }


        }

        ITokenStream _lexerStream;
        CSharpParseOptions _options;

        Encoding _encoding;

        SourceHashAlgorithm _checksumAlgorithm;

        IList<ParseErrorData> _parseErrors;

        IList<string> includeDirs;

        Dictionary<string, IList<XSharpToken>> symbolDefines ;

        Dictionary<string, Func<XSharpToken>> macroDefines = new Dictionary<string, Func<XSharpToken>>(/*CaseInsensitiveComparison.Comparer*/);

        Stack<bool> defStates = new Stack<bool> ();
        Stack<XSharpToken> regions = new Stack<XSharpToken>();
        string _fileName = null;
        InputState inputs;
        IToken lastToken = null;

        PPRuleDictionary cmdRules = new PPRuleDictionary();
        PPRuleDictionary transRules = new PPRuleDictionary();
        bool _hasCommandrules = false;
        bool _hasTransrules = false;
        int rulesApplied = 0;
        int defsApplied = 0;
        HashSet<string> activeSymbols = new HashSet<string>(/*CaseInsensitiveComparison.Comparer*/);

        bool _preprocessorOutput = false;
        System.IO.Stream _ppoStream;

        internal Dictionary<string, SourceText> IncludedFiles = new Dictionary<string, SourceText>(CaseInsensitiveComparison.Comparer);

        public int MaxIncludeDepth { get; set; } = 16;

        public int MaxSymbolDepth { get; set; } = 16;

        public int MaxUDCDepth { get; set; } = 256;

        public string StdDefs { get; set; } = string.Empty;
        private void initStdDefines(CSharpParseOptions options, string fileName)
        {
            // Note Macros such as __ENTITY__ and  __SIG__ are handled in the transformation phase
            macroDefines.Add("__ARRAYBASE__", () => new XSharpToken(XSharpLexer.INT_CONST, _options.ArrayZero ? "0" : "1"));
            macroDefines.Add("__CLR2__", () => new XSharpToken(XSharpLexer.STRING_CONST, "\"__CLR2__\""));
            macroDefines.Add("__CLR4__", () => new XSharpToken(XSharpLexer.STRING_CONST, "\"__CLR4__\""));
            macroDefines.Add("__CLRVERSION__", () => new XSharpToken(XSharpLexer.STRING_CONST, "\"__CLRVERSION__\""));
            macroDefines.Add("__DATE__", () => new XSharpToken(XSharpLexer.STRING_CONST, '"' + DateTime.Now.Date.ToString("yyyyMMdd") + '"'));
            macroDefines.Add("__DATETIME__", () => new XSharpToken(XSharpLexer.STRING_CONST, '"' + DateTime.Now.ToString() + '"'));
            if (_options.DebugEnabled)
                macroDefines.Add("__DEBUG__", () => new XSharpToken(XSharpLexer.TRUE_CONST));
            macroDefines.Add("__DIALECT__", () => new XSharpToken(XSharpLexer.STRING_CONST, '"' + options.Dialect.ToString() + '"'));
            switch (_options.Dialect)
            {
                case XSharpDialect.Core:
                    macroDefines.Add("__DIALECT_CORE__", () => new XSharpToken(XSharpLexer.TRUE_CONST));
                    break;
                case XSharpDialect.VO:
                    macroDefines.Add("__DIALECT_VO__", () => new XSharpToken(XSharpLexer.TRUE_CONST));
                    break;
                case XSharpDialect.Vulcan:
                    macroDefines.Add("__DIALECT_VULCAN__", () => new XSharpToken(XSharpLexer.TRUE_CONST));
                    break;
                default:
                    break;
            }
            macroDefines.Add("__ENTITY__", () => new XSharpToken(XSharpLexer.STRING_CONST, "\"__ENTITY__\""));  // Handled later in Transformation phase
            macroDefines.Add("__FILE__", () => new XSharpToken(XSharpLexer.STRING_CONST, '"' + (inputs.SourceFileName ?? fileName) + '"'));
            macroDefines.Add("__LINE__", () => new XSharpToken(XSharpLexer.INT_CONST, inputs.Lt().Line.ToString()));
            macroDefines.Add("__MODULE__", () => new XSharpToken(XSharpLexer.STRING_CONST, '"' + (inputs.SourceFileName ?? fileName) + '"'));
            macroDefines.Add("__SIG__", () => new XSharpToken(XSharpLexer.STRING_CONST, "\"__SIG__\"")); // Handled later in Transformation phase
            macroDefines.Add("__SRCLOC__", () => new XSharpToken(XSharpLexer.STRING_CONST, '"' + (inputs.SourceFileName ?? fileName) + " line " + inputs.Lt().Line.ToString() + '"'));
            macroDefines.Add("__SYSDIR__", () => new XSharpToken(XSharpLexer.STRING_CONST, '"' + options.SystemDir + '"'));
            macroDefines.Add("__TIME__", () => new XSharpToken(XSharpLexer.STRING_CONST, '"' + DateTime.Now.ToString("HH:mm:ss") + '"'));
            macroDefines.Add("__UTCTIME__", () => new XSharpToken(XSharpLexer.STRING_CONST, '"' + DateTime.Now.ToUniversalTime().ToString("HH:mm:ss") + '"'));
            macroDefines.Add("__VERSION__", () => new XSharpToken(XSharpLexer.STRING_CONST, '"' + global::XSharp.Constants.Version + '"'));
            macroDefines.Add("__WINDIR__", () => new XSharpToken(XSharpLexer.STRING_CONST, '"' + options.WindowsDir + '"'));
            macroDefines.Add("__WINDRIVE__", () => new XSharpToken(XSharpLexer.STRING_CONST, '"' + options.WindowsDir?.Substring(0, 2) + '"'));
            macroDefines.Add("__XSHARP__", () => new XSharpToken(XSharpLexer.TRUE_CONST));

            bool[] flags  = { options.vo1,  options.vo2, options.vo3, options.vo4, options.vo5, options.vo6, options.vo7, options.vo8,
                                options.vo9, options.vo10, options.vo11, options.vo12, options.vo13, options.vo14, options.vo15, options.vo16 };
            for (int iOpt = 0; iOpt < flags.Length; iOpt++)
            {
                string flagName = String.Format("__VO{0}__", iOpt + 1);
                if (flags[iOpt])
                    macroDefines.Add(flagName, () => new XSharpToken(XSharpLexer.TRUE_CONST));
                else
                    macroDefines.Add(flagName, () => new XSharpToken(XSharpLexer.FALSE_CONST));
            }
            if (!options.NoStdDef)
            {
                // Todo: when the compiler option nostddefs is not set: read XSharpDefs.xh from the XSharp Include folder,//
                // and automatically include it.
                // read XsharpDefs.xh
                StdDefs = "xSharpDefs.xh";
                ProcessIncludeFile(null, StdDefs,true);   
            }
        }


        internal void DumpStats()
        {
            DebugOutput("Preprocessor statistics");
            DebugOutput("-----------------------");
            DebugOutput("# of #defines    : {0}", this.symbolDefines.Count);
            DebugOutput("# of #translates : {0}", this.transRules.Count);
            DebugOutput("# of #commands   : {0}", this.cmdRules.Count);
            DebugOutput("# of macros      : {0}", this.macroDefines.Count);
            DebugOutput("# of defines used: {0}", this.defsApplied);
            DebugOutput("# of UDCs used   : {0}", this.rulesApplied);
        }

        private void _writeToPPO(String text)
        {
            // do not call t.Text when not needed.
            if (_preprocessorOutput)
            {
                text = text.TrimAllWithInplaceCharArray();
                var buffer = _encoding.GetBytes(text);
                _ppoStream.Write(buffer, 0, buffer.Length);
                buffer = _encoding.GetBytes("\r\n");
                _ppoStream.Write(buffer, 0, buffer.Length);
            }
        }

        private bool mustWriteToPPO()
        {
            return _preprocessorOutput && _ppoStream != null && inputs.parent == null;
        }

        private void writeToPPO(string text)
        {
            if (mustWriteToPPO())
            {
                _writeToPPO(text);
            }
        }
        private void writeToPPO(IList<XSharpToken> tokens, bool prefix = false, bool prefixNewLines = false)
        {
            if (mustWriteToPPO())
            {
                if (tokens?.Count == 0)
                {
                    _writeToPPO("");
                    return;
                }
                // We cannot use the interval and fetch the text from the source stream,
                // because some tokens may come out of an include file or otherwise
                // so concatenate text on the fly
                var bld = new System.Text.StringBuilder(1024);
                if (prefix)
                {
                    bld.Append(PPOPrefix);
                }
                foreach (var t in tokens)
                {
                    bld.Append(t.Text);
                    bld.Append(t.TrailingWs());
                }
                if (prefixNewLines)
                {
                    bld.Replace("\n", "\n" + PPOPrefix);
                }
                _writeToPPO(bld.ToString());
            }
        }

        internal void Close()
        {
            if (_ppoStream != null)
            {
                _ppoStream.Flush();
                _ppoStream.Dispose();
            }
            _ppoStream = null;
        }

        internal XSharpPreprocessor(ITokenStream lexerStream, CSharpParseOptions options, string fileName, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, IList<ParseErrorData> parseErrors)
        {
            clearOldIncludes();
            _lexerStream = lexerStream;
            _options = options;
            _fileName = fileName;
            if (_options.VOPreprocessorBehaviour)
                symbolDefines = new Dictionary<string, IList<XSharpToken>>(CaseInsensitiveComparison.Comparer);
            else
                symbolDefines = new Dictionary<string, IList<XSharpToken>>(/* case sensitive */);
            _encoding = encoding;
            _checksumAlgorithm = checksumAlgorithm;
            _parseErrors = parseErrors;
            includeDirs = new List<string>(options.IncludePaths);
            if ( !String.IsNullOrEmpty(fileName) && PortableShim.File.Exists(fileName))
            {
                includeDirs.Add(System.IO.Path.GetDirectoryName(fileName));
                var ppoFile = FileNameUtilities.ChangeExtension(fileName, ".ppo");
                try
                {
                    _ppoStream = null;
                    _preprocessorOutput = _options.PreprocessorOutput;
                    if (FileNameUtilities.GetExtension(fileName).ToLower() == ".ppo")
                    {
                        _preprocessorOutput = false;
                    }
                    else 
                    {
                        if (_preprocessorOutput)
                        {
                            _ppoStream = FileUtilities.CreateFileStreamChecked(PortableShim.File.Create, ppoFile, "PPO file");
                        }
                        else if (PortableShim.File.Exists(ppoFile))
                        {
                            PortableShim.File.Delete(ppoFile);
                        }
                    }
                }
                catch (Exception e)
                {
                    _parseErrors.Add(new ParseErrorData(ErrorCode.ERR_PreProcessorError, "Error processing PPO file: " + e.Message));
                }
            }
            // Add default IncludeDirs;
            if (!String.IsNullOrEmpty(options.DefaultIncludeDir))
            {
                string[] paths = options.DefaultIncludeDir.Split( new[] { ';' },StringSplitOptions.RemoveEmptyEntries);
                foreach (var path in paths)
                {
                    includeDirs.Add(path);
                }
            }

            inputs = new InputState(lexerStream);
            foreach (var symbol in options.PreprocessorSymbols)
                symbolDefines[symbol] = null;

            initStdDefines(options, fileName);
        }

        internal void DebugOutput(string format, params object[] objects)
        {
            _options.ConsoleOutput.WriteLine("PP: " + format, objects);
        }


        /// <summary>
        /// Pre-processes the input stream. Reads #Include files, processes #ifdef commands and translations from #defines, macros and UDCs
        /// </summary>
        /// <returns>Translated input stream</returns>
        internal IList<IToken> PreProcess()
        {
            var result = new List<IToken>(); ;
            XSharpToken t = Lt();
            List<XSharpToken> omitted = new List<XSharpToken>(); ;
            while (t.Type != IntStreamConstants.Eof)
            {
                // read until the next EOS
                var line = ReadLine(omitted);
                t = Lt();   // CRLF or EOS. Must consume now, because #include may otherwise add a new inputs
                Consume();
                if (line.Count > 0) 
                {
                    line = ProcessLine(line);
                    if (line!= null && line.Count > 0)
                    {
                        result.AddRange(line);
                    }
                }
                else
                {
                    if (omitted.Count > 0)
                        writeToPPO(omitted, false);
                    else
                        writeToPPO("");
                }
                result.Add(t);
            }
            doEOFChecks();
            return result;
        }

        List<XSharpToken> ProcessLine(List<XSharpToken> line)
        {
            Debug.Assert(line.Count > 0);
            var nextType = line[0].Type;
            switch (nextType)
            {
                case XSharpLexer.PP_UNDEF:
                    doUnDefDirective(line);
                    line = null;
                    break;
                case XSharpLexer.PP_IFDEF:
                    doIfDefDirective(line, true);
                    line = null;
                    break;
                case XSharpLexer.PP_IFNDEF:
                    doIfDefDirective(line, false);
                    line = null;
                    break;
                case XSharpLexer.PP_ENDIF:
                    doEndifDirective(line);
                    line = null;
                    break;
                case XSharpLexer.PP_ELSE:
                    doElseDirective(line);
                    line = null;
                    break;
                case XSharpLexer.PP_LINE:
                    doLineDirective(line);
                    line = null;
                    break;
                case XSharpLexer.PP_ERROR:
                case XSharpLexer.PP_WARNING:
                    doErrorWarningDirective(line);
                    line = null;
                    break;
                case XSharpLexer.PP_INCLUDE:
                    doIncludeDirective(line);
                    line = null;
                    break;
                case XSharpLexer.PP_COMMAND:
                case XSharpLexer.PP_TRANSLATE:
                    doUDCDirective(line);
                    line = null;
                    break;
                case XSharpLexer.PP_DEFINE:
                    doDefineDirective(line);
                    line = null;
                    break;
                case XSharpLexer.PP_ENDREGION:
                    doEndRegionDirective(line);
                    line = null;
                    break;
                case XSharpLexer.PP_REGION:
                    doRegionDirective(line);
                    line = null;
                    break;
                case XSharpLexer.UDCSEP:
                    doUnexpectedUDCSeparator(line);
                    line = null;
                    break;
                default:
                    line = doNormalLine(line);
                    break;
            }
            return line;
        }

        /// <summary>
        /// Reads the a line from the input stream until the EOS token and skips hidden tokens
        /// </summary>
        /// <returns>List of tokens EXCLUDING the EOS but including statement separator char ;</returns>
        List<XSharpToken> ReadLine( List<XSharpToken> omitted)
        {
            Debug.Assert(omitted != null);
            var res = new List<XSharpToken>();
            omitted.Clear();
            XSharpToken t = Lt();
            while (t.Type != IntStreamConstants.Eof)
            {
                if (t.IsEOS() && t.Text != ";")
                    break;
                if (t.Channel != XSharpLexer.Hidden && t.Channel != XSharpLexer.XMLDOCCHANNEL)
                {
                    var nt = FixToken(t);
                    res.Add(nt);
                }
                else
                {
                    omitted.Add(t);
                }
                Consume();
                t = Lt();
            }
            return res;
        }

        /// <summary>
        /// Returns the name of the active source. Can be the main prg file, but also an active #include file
        /// </summary>
        string SourceName
        {
            get
            {
                return _fileName;
            }
        }


        XSharpToken GetSourceSymbol()
        {
            XSharpToken s = null;
            if (inputs.isSymbol)
            {
                var baseInputState = inputs;
                while (baseInputState.parent?.isSymbol == true)
                    baseInputState = baseInputState.parent;
                s = baseInputState.Symbol;
            }
            return s;
        }

        XSharpToken FixToken(XSharpToken  token)
        {
            if (inputs.MappedLineDiff != 0)
                token.MappedLine = token.Line + inputs.MappedLineDiff;
            if (!string.IsNullOrEmpty(inputs.MappedFileName))
                token.MappedFileName = inputs.MappedFileName;
            //if (!string.IsNullOrEmpty(inputs.SourceFileName))
            //    token.SourceFileName = inputs.SourceFileName;
            if (inputs.isSymbol)
            {
                token.SourceSymbol = GetSourceSymbol();
                //token.SourceFileName = (token.SourceSymbol as XSharpToken).SourceFileName;
            }
            return token;
        }

        XSharpToken Lt()
        {
            return inputs.Lt();
        }

        void Consume()
        {
            while (!inputs.Consume() && inputs.parent != null)
            {
                if (inputs.isSymbol)
                    activeSymbols.Remove(inputs.SymbolName);
                inputs = inputs.parent;
            }
        }

        void InsertStream(string filename, ITokenStream input, XSharpToken symbol = null)
        {
            if ( _options.ShowDefs)
            {
                if (symbol != null)
                {
                    var tokens = new List<XSharpToken>();
                    for (int i = 0; i < input.Size-1; i++)
                    {
                        tokens.Add(new XSharpToken(input.Get(i)));
                    }
                    string text = tokens.AsString();
                    //if (text.Length > 20)
                    //    text = text.Substring(0, 20) + "...";
                    DebugOutput("File {0} line {1}:", _fileName, symbol.Line);
                    DebugOutput("Input stack: Insert value of token Symbol {0}, {1} tokens => {2}", symbol.Text, input.Size-1, text);
                }
                else
                    DebugOutput("Input stack: Insert Stream {0}, # of tokens {1}", filename, input.Size-1);
            }
            InputState s = new InputState(input);
            s.parent = inputs;
            s.SourceFileName = filename;
            s.SymbolName = symbol?.Text;
            s.Symbol = symbol;
            s.isSymbol = symbol != null;
            if (s.isSymbol)
            {
                activeSymbols.Add(s.SymbolName);
                s.MappedLineDiff = inputs.MappedLineDiff;
            }
            inputs = s;
        }

        bool IsActive()
        {
            return defStates.Count == 0 || defStates.Peek();
        }

          int IncludeDepth()
        {
            int d = 1;
            var o = inputs;
            while (o.parent != null)
            {
                if (!o.isSymbol)
                    d += 1;
                o = o.parent;
            }
            return d;
        }

        int SymbolDepth()
        {
            int d = 0;
            var o = inputs;
            while (o.parent != null && o.isSymbol)
            {
                d += 1;
                o = o.parent;
            }
            return d;
        }

        bool IsDefinedMacro(XSharpToken t)
        {
            return (t.Type == XSharpLexer.MACRO) ? macroDefines.ContainsKey(t.Text) : false;
        }

        void addDefine(IList<XSharpToken> line)
        {
            // Check to see if the define contains a LPAREN, and there is no space in between them. 
            // Then it is a pseudo function that we will store as a #xtranslate UDC
            // this returns a list that includes #define and the ID
            if (line.Count < 2)
            {
                var token = line[0];
                _parseErrors.Add(new ParseErrorData(token, ErrorCode.ERR_PreProcessorError, "Identifier expected"));
                return;
            }
            // token 1 is the Identifier
            // other tokens are optional and may contain a value
            XSharpToken def = line[1];
            if (line.Count > 2)
            {
                var first = line[2];
                if (first.Type == XSharpLexer.LPAREN
                    && first.StartIndex == def.StopIndex + 1)
                {
                    doUDCDirective(line);
                    return;
                }
            }
            if (XSharpLexer.IsIdentifier(def.Type) || XSharpLexer.IsKeyword(def.Type))
            {
                line.RemoveAt(0);  // remove #define
                line.RemoveAt(0);  // remove ID
                if (symbolDefines.ContainsKey(def.Text))
                {
                    // check to see if this is a new definition or a duplicate definition
                    var oldtokens = symbolDefines[def.Text];
                    var cOld = oldtokens.AsString();
                    var cNew = line.AsString();
                    if (cOld == cNew)
                        _parseErrors.Add(new ParseErrorData(def, ErrorCode.WRN_DuplicateDefineSame, def.Text));
                    else
                        _parseErrors.Add(new ParseErrorData(def, ErrorCode.WRN_DuplicateDefineDiff, def.Text, cOld, cNew));
                }
                symbolDefines[def.Text] = line;
                if (_options.ShowDefs)
                {
                    DebugOutput("{0}:{1} add DEFINE {2} => {3}", def.FileName(), def.Line, def.Text, line.AsString() );
                }
            }
            else
            {
                _parseErrors.Add(new ParseErrorData(def, ErrorCode.ERR_PreProcessorError, "Identifier expected"));
                return;
            }
        }

        void removeDefine(IList<XSharpToken> line)
        {
            var errToken = line[0];
            bool ok = true;
            if (line.Count < 2)
            {
                ok = false;
            }
            XSharpToken def = line[1];
            if (XSharpLexer.IsIdentifier(def.Type) || XSharpLexer.IsKeyword(def.Type))
            {
                if (symbolDefines.ContainsKey(def.Text))
                    symbolDefines.Remove(def.Text);
            }
            else
            {
                errToken = def;
                ok = false;
            }
            if (! ok)
            {
                _parseErrors.Add(new ParseErrorData(errToken, ErrorCode.ERR_PreProcessorError, "Identifier expected"));
            }
        }

        void doUDCDirective(IList<XSharpToken> udc)
        {
            Debug.Assert(udc?.Count > 0);
            writeToPPO(udc, true, true);
            if (udc.Count < 3)
            {
                _parseErrors.Add(new ParseErrorData(udc[0], ErrorCode.ERR_PreProcessorError, "Invalid UDC:{0}", udc.AsString()));
                return;
            }
            var cmd = udc[0];
            PPErrorMessages errorMsgs;
            var rule = new PPRule(cmd, udc, out errorMsgs);
            if (rule.Type == PPUDCType.None)
            {
                if (errorMsgs.Count > 0)
                {
                    foreach (var s in errorMsgs)
                    {
                        _parseErrors.Add(new ParseErrorData(s.Token, ErrorCode.ERR_PreProcessorError, s.Message));
                    }
                }
                else
                {
                    _parseErrors.Add(new ParseErrorData(cmd, ErrorCode.ERR_PreProcessorError, "Invalid directive '" + cmd.Text + "' (are you missing the => operator?)"));
                }
            }
            else
            {
                if (cmd.Type == XSharpLexer.PP_COMMAND )
                {
                    // COMMAND and XCOMMAND can only match from beginning of line
                    cmdRules.Add(rule);
                    _hasCommandrules = true;
                }
                else
                {
                    // TRANSLATE and XTRANSLATE can also match from beginning of line
                    transRules.Add(rule);
                    _hasTransrules = true;
                    if (cmd.Type == XSharpLexer.PP_DEFINE)
                    {
                        rule.CaseInsensitive = _options.VOPreprocessorBehaviour;
                    }
                }
                if (_options.ShowDefs)
                {
                    DebugOutput("{0}:{1} add {2} {3}", cmd.FileName(), cmd.Line, cmd.Type == XSharpLexer.PP_DEFINE ? "DEFINE" : "UDC" ,rule.Name);
                }
                
            }
        }

        private bool ProcessIncludeFile(XSharpToken ln, string fn, bool StdDefine = false)
        {
            string nfp = null;
            SourceText text = null;
            Exception fileReadException = null;
            CachedIncludeFile cachedFile = null;
            List<String> dirs = new List<String>();
            dirs.Add(PathUtilities.GetDirectoryName(_fileName));
            foreach (var p in includeDirs)
            {
                dirs.Add(p);
            }
            foreach (var p in dirs)
            {
                bool rooted = System.IO.Path.IsPathRooted(fn);
                string fp;
                try
                {
                    fp = rooted ? fn : System.IO.Path.Combine(p, fn);
                }
                catch (Exception e)
                {
                    _parseErrors.Add(new ParseErrorData(ln, ErrorCode.ERR_PreProcessorError, "Error combining path " + p + " and filename  " + fn + " " + e.Message));
                    continue;
                }
                try
                {
                    using (var data = PortableShim.FileStream.Create(fp, PortableShim.FileMode.Open, PortableShim.FileAccess.Read, PortableShim.FileShare.ReadWrite, bufferSize: 1, options: PortableShim.FileOptions.None))
                    {
                        nfp = (string)PortableShim.FileStream.Name.GetValue(data);
                        cachedFile = getIncludeFile(nfp);
                        if (cachedFile != null)
                        {
                            text = cachedFile.Text;
                        }
                        else
                        {
                            nfp = (string)PortableShim.FileStream.Name.GetValue(data);
                            try
                            {
                                text = EncodedStringText.Create(data, _encoding, _checksumAlgorithm);
                            }
                            catch (Exception)
                            {
                                text = null;
                            }
                            if (text == null)
                            {
                                // Encoding problem ?
                                text = EncodedStringText.Create(data);
                            }
                        }
                        if (! IncludedFiles.ContainsKey(nfp))
                        {
                            IncludedFiles.Add(nfp, text);
                        }
                        break;
                    }
                }
                catch (Exception e)
                {
                    if (fileReadException == null)
                        fileReadException = e;
                    nfp = null;
                }
                if (rooted)
                    break;
            }
            if (nfp == null)
            {
                if (fileReadException != null)
                {
                    _parseErrors.Add(new ParseErrorData(ln, ErrorCode.ERR_PreProcessorError, "Error Reading include file '" + fn + "': " + fileReadException.Message));
                }
                else
                {
                    _parseErrors.Add(new ParseErrorData(ln, ErrorCode.ERR_PreProcessorError, "Include file not found: '" + fn + "'"));
                }

                return false;
            }
            if (_options.ShowIncludes )
            {
                var fname = PathUtilities.GetFileName(this.SourceName);
                if (ln != null)
                {
                    fname = PathUtilities.GetFileName(ln.InputStream.SourceName);
                    DebugOutput("{0} line {1} Include {2}", fname, ln.Line, nfp);
                }
                else
                {
                    DebugOutput("{0} line {1} Include {2}", fname, 0, nfp);
                }

            }
            if (cachedFile == null)
            {
                // we have nfp and text with the file contents
                // now parse the stuff and insert in the cache
                //Debug.WriteLine("Uncached file {0} ", nfp);
                var stream = new AntlrInputStream(text.ToString());
                stream.name = nfp;
                var lexer = new XSharpLexer(stream);
                lexer.TokenFactory = XSharpTokenFactory.Default;
                var tokens = new CommonTokenStream(lexer);
                tokens.Fill();
                InsertStream(nfp, tokens);
                foreach (var e in lexer.LexErrors)
                {
                    _parseErrors.Add(e);
                }
                var clone = tokens.GetTokens().ToArrayXSharpToken();
                addIncludeFile(nfp, clone, text);
            }
            else
            {
                // we have a file cache item with the Tokens etc
                // Create a stream from the cached text and tokens
                // Clone the tokens to avoid problems when concurrently using the tokens
                var clone = cachedFile.Tokens.ToIListIToken();
                var tokenSource = new ListTokenSource(clone, cachedFile.FileName);
                var tokenStream = new BufferedTokenStream(tokenSource);
                tokenStream.Fill();
                InsertStream(cachedFile.FileName, tokenStream);
            }
            return true;

        }

        private bool IsDefined(string define)
        {
            // Handle /VO8 compiler option:
            // When /VO8 is active and the variable is defined and has a value of FALSE or a numeric value = 0
            // Then #ifdef is FALSE
            // otherwise #ifdef is TRUE
            // and when there is more than one token, then #ifdef is also TRUE
            bool isdefined= symbolDefines.ContainsKey(define);
            if (isdefined && _options.VOPreprocessorBehaviour)
            {
                var value = symbolDefines[define];
                if (value?.Count == 1)
                {
                    var deftoken = value[0];
                    if (deftoken.Type == XSharpLexer.FALSE_CONST)
                    {
                        isdefined = false;
                    }
                    else if (deftoken.Type == XSharpLexer.INT_CONST)
                    {
                        isdefined = Convert.ToInt64(deftoken.Text) != 0;
                    }
                }
            }
            return isdefined;
        }

        private bool isDefineAllowed(IList<XSharpToken> line, int iPos)
        {
            // DEFINE will not be accepted immediately after or before a DOT
            // So this will not be recognized:
            // #define Console
            // System.Console.WriteLine("zxc")
            // But this will , since there are spaces around the token
            // System. Console .WriteLine("zxc")
            Debug.Assert(line?.Count > 0);
            if (iPos > 0 && line[iPos-1].Type == XSharpLexer.DOT )
            {
                return false;
            }
            if (iPos < line.Count-1)
            {
                var token = line[iPos + 1];
                if (token.Type == XSharpParser.DOT )
                    return false;
            }
            return true;
        }
        #region Preprocessor Directives


        private void checkForUnexpectedPPInput(IList<XSharpToken> line, int nMax)
        {
            if (line.Count > nMax)
            {
                _parseErrors.Add(new ParseErrorData(line[nMax], ErrorCode.ERR_EndOfPPLineExpected));
            }
        }
        private void doRegionDirective(List<XSharpToken> line)
        {
            Debug.Assert(line?.Count > 0);
            if (IsActive())
            {
                var token = line[0];
                regions.Push(token);
                writeToPPO(line,  true);
            }
            else
            {
                writeToPPO("");
            }
        }

        private void doEndRegionDirective(List<XSharpToken> line)
        {
            Debug.Assert(line?.Count > 0);
            if (IsActive())
            {
                var token = line[0];
                if (regions.Count > 0)
                    regions.Pop();
                else
                    _parseErrors.Add(new ParseErrorData(token, ErrorCode.ERR_PreProcessorError, "#endregion directive without matching #region found"));
                writeToPPO(line, true);
            }
            else
            {
                writeToPPO("");
            }
            // ignore comments after #endregion
            //checkForUnexpectedPPInput(line, 1);
        }

        private void doDefineDirective(List<XSharpToken> line)
        {
            Debug.Assert(line?.Count > 0);
            if (IsActive())
            {
                writeToPPO(line, true);
                addDefine(line);
            }
            else
            {
                writeToPPO("");
            }

        }

        private void doUnDefDirective(List<XSharpToken> line)
        {
            Debug.Assert(line?.Count > 0);
            if (IsActive())
            {
                removeDefine(line);
                writeToPPO(line, true);
            }
            else
            {
                writeToPPO("");
            }
            checkForUnexpectedPPInput(line, 2);
        }

        private void doErrorWarningDirective(List<XSharpToken> line)
        {
            Debug.Assert(line?.Count > 0);
            int nextType = line[0].Type;
            if (IsActive())
            {
                string text;
                XSharpToken ln;
                writeToPPO(line, true);
                ln = line[0];
                if (line.Count > 1)
                {
                    text = "";
                    for (int i = 1; i < line.Count; i++)
                    {
                        text += line[i].Text;
                    }
                    text = text.Trim();
                }
                else
                {
                    if (nextType == XSharpLexer.PP_ERROR)
                        text = "Empty error clause";
                    else
                        text = "Empty warning clause";

                }
                if (ln.SourceSymbol != null)
                    ln = ln.SourceSymbol;
                if (nextType == XSharpLexer.PP_WARNING)
                    _parseErrors.Add(new ParseErrorData(ln, ErrorCode.WRN_WarningDirective, text));
                else
                    _parseErrors.Add(new ParseErrorData(ln, ErrorCode.ERR_ErrorDirective, text));
                lastToken = ln;
            }
            else
            {
                writeToPPO( "");
            }
        }

        private void doIfDefDirective(List<XSharpToken> line, bool isIfDef)
        {
            Debug.Assert(line?.Count > 0);
            if (IsActive())
            {
                var def = line[1];
                if (XSharpLexer.IsIdentifier(def.Type) || XSharpLexer.IsKeyword(def.Type))
                {
                    if (isIfDef)
                        defStates.Push(IsDefined(def.Text));
                    else
                        defStates.Push(!IsDefined(def.Text));
                }
                else if (def.Type == XSharpLexer.MACRO)
                {
                    if (isIfDef)
                        defStates.Push(IsDefinedMacro(def));
                    else
                        defStates.Push(!IsDefinedMacro(def));
                }
                else
                {
                    _parseErrors.Add(new ParseErrorData(def, ErrorCode.ERR_PreProcessorError, "Identifier expected"));
                }
                writeToPPO(line,  true);

            }
            else
            {
                defStates.Push(false);
                writeToPPO( "");
            }
            checkForUnexpectedPPInput(line, 2);
        }

        private void doElseDirective(List<XSharpToken> line)
        {
            Debug.Assert(line?.Count > 0);
            writeToPPO(line, true);
            if (defStates.Count > 0)
            {
                bool a = defStates.Pop();
                if (IsActive())
                {
                    defStates.Push(!a);
                }
                else
                    defStates.Push(false);
            }
            else
            {
                _parseErrors.Add(new ParseErrorData(Lt(), ErrorCode.ERR_PreProcessorError, "Unexpected #else"));
            }
            checkForUnexpectedPPInput(line, 1);
        }

        private void doEndifDirective(List<XSharpToken> line)
        {
            Debug.Assert(line?.Count > 0);
            if (defStates.Count > 0)
            {
                defStates.Pop();
                if (IsActive())
                {
                    writeToPPO(line, true);
                }
                else
                {
                    writeToPPO("");
                }
            }
            else
            {
                _parseErrors.Add(new ParseErrorData(Lt(), ErrorCode.ERR_UnexpectedDirective));
                writeToPPO(line, true);
            }
            checkForUnexpectedPPInput(line, 1);
        }

        private void doIncludeDirective(List<XSharpToken> line)
        {
            Debug.Assert(line?.Count > 0);
            if (IsActive())
            {
                writeToPPO(line, true);
                if (IncludeDepth() == MaxIncludeDepth)
                {
                    _parseErrors.Add(new ParseErrorData(line[0], ErrorCode.ERR_PreProcessorError, "Reached max include depth: " + MaxIncludeDepth));
                }
                else
                {
                    var ln = line[1];
                    if (ln.Type == XSharpLexer.STRING_CONST)
                    {
                        string fn = ln.Text.Substring(1, ln.Text.Length - 2);
                        lock (includecache)
                        {
                            ProcessIncludeFile(ln, fn);
                        }

                    }
                    else
                    {
                        _parseErrors.Add(new ParseErrorData(ln, ErrorCode.ERR_PreProcessorError, "String literal expected"));
                    }
                }
            }
            else
            {
                writeToPPO("");
            }
            checkForUnexpectedPPInput(line, 2);
        }

        private void doLineDirective(List<XSharpToken> line)
        {
            Debug.Assert(line?.Count > 0);
            if (IsActive())
            {
                writeToPPO(line, true);
                var ln = line[1];
                if (ln.Type == XSharpLexer.INT_CONST)
                {
                    inputs.MappedLineDiff = (int)ln.SyntaxLiteralValue(_options).Value - (ln.Line + 1);
                    ln = line[2];
                    if (ln.Type == XSharpLexer.STRING_CONST)
                    {
                        inputs.SourceFileName = ln.Text.Substring(1, ln.Text.Length - 2);
                    }
                    else
                    {
                        _parseErrors.Add(new ParseErrorData(ln, ErrorCode.ERR_PreProcessorError, "String literal expected"));
                    }
                }
                else
                {
                    _parseErrors.Add(new ParseErrorData(ln, ErrorCode.ERR_PreProcessorError, "Integer literal expected"));
                }
            }
            else
            {
                writeToPPO("");
            }
            checkForUnexpectedPPInput(line, 3);
        }

        private void doUnexpectedUDCSeparator(List<XSharpToken> line)
        {
            Debug.Assert(line?.Count > 0);
            var ln = line[0];
            writeToPPO(line, true);
            _parseErrors.Add(new ParseErrorData(ln, ErrorCode.ERR_PreProcessorError, "Unexpected UDC separator character found"));
        }

        private List<XSharpToken> doNormalLine(List<XSharpToken> line, bool write2PPO = true)
        {
            // Process the whole line in one go and apply the defines, macros and udcs
            // This is modeled after the way it is done in Harbour
            // 1) Look for and replace defines
            // 2) Look for and replace Macros (combined with 1) for performance)
            // 3) look for and replace (x)translates
            // 4) look for and replace (x)commands
            if (IsActive())
            {
                Debug.Assert(line?.Count > 0);
                List<XSharpToken> result;
                bool changed = true;
                // repeat this loop as long as there are matches
                while (changed)
                {
                    changed = false;
                    if (line.Count > 0)
                    {
                        if (doProcessDefinesAndMacros(line, out result))
                        {
                            changed = true;
                            line = result;
                        }
                    }
                    if (_hasTransrules && line.Count > 0)
                    {
                        if (doProcessTranslates(line, out result))
                        {
                            changed = true;
                            line = result;
                        }
                    }
                    if (_hasCommandrules && line.Count > 0)
                    {
                        if (doProcessCommands(line, out result))
                        {
                            changed = true;
                            line = result;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < line.Count; i++)
                {
                    var t = line[i];
                    t.Original.Channel = XSharpLexer.DEFOUTCHANNEL;
                }
                line.Clear();
            }
            if (write2PPO)
            {
                if (line.Count > 0)
                    writeToPPO(line, false);
                else
                    writeToPPO("");
            }
            return line;
        }


        private List<XSharpToken> copySource(List<XSharpToken> line, int nCount)
        {
            var result = new List<XSharpToken>(line.Count);
            var temp = new XSharpToken[nCount];
            line.CopyTo(0, temp, 0, temp.Length);
            result.AddRange(temp);
            return result;
        }
        private bool doProcessDefinesAndMacros(List<XSharpToken> line, out List<XSharpToken> result)
        {
            Debug.Assert(line?.Count > 0);
            // we loop in here because one define may add tokens that are defined by another
            // such as:
            // #define FOO 1
            // #define BAR FOO + 1
            // when the code is "? BAR" then we need to translate this to "? 1 + 1"
            // For performance reasons we assume there is nothing to do, so we only 
            // start allocating a result collection when a define is detected
            // otherwise we will simply return the original string
            bool hasChanged = false;
            List<XSharpToken> tempResult = line;
            result = null;
            while (tempResult != null)
            {
                tempResult = null;
                // in a second iteration line will be the changed line
                for (int i = 0; i < line.Count; i++)
                {
                    var token = line[i];
                    IList<XSharpToken> deflist = null;
                    if (isDefineAllowed(line, i) && symbolDefines.TryGetValue(token.Text, out deflist))
                    {
                        if (tempResult == null)
                        {
                            // this is the first define in the list
                            // allocate a result and copy the items 0 .. i-1 to the result
                            tempResult = copySource(line, i);
                        }
                        foreach (var t in deflist)
                        {
                            var t2 = new XSharpToken(t);
                            t2.Channel = XSharpLexer.DefaultTokenChannel;
                            t2.SourceSymbol = token;
                            tempResult.Add(t2);
                        }
                    }
                    else if (token.Type == XSharpLexer.MACRO)
                    {
                        // Macros that cannot be found are changed to ID
                        Func<XSharpToken> ft;
                        if (macroDefines.TryGetValue(token.Text, out ft))
                        {
                            var nt = ft();
                            if (nt != null)
                            {
                                nt.Line = token.Line;
                                nt.Column = token.Column;
                                nt.StartIndex = token.StartIndex;
                                nt.StopIndex = token.StopIndex;
                                nt.SourceSymbol = token;
                                if (tempResult == null)
                                {
                                    // this is the first macro in the list
                                    // allocate a result and copy the items 0 .. i-1 to the result
                                    tempResult = copySource(line, i);
                                }
                                tempResult.Add(nt);
                            }
                        }
                    }
                    else if (tempResult != null)
                    {
                        tempResult.Add(token);
                    }
                }
                if (tempResult != null)
                {
                    // copy temporary result to line for next iteration
                    line = tempResult;
                    result = line;
                    hasChanged = true;
                }
            }
            return hasChanged; ;
        }

        internal void AddParseError(ParseErrorData data)
        {
            _parseErrors.Add(data);
        }
        private bool doProcessTranslates(List<XSharpToken> line, out List<XSharpToken> result)
        {
            Debug.Assert(line?.Count > 0);
            var temp = new List<XSharpToken>();
            temp.AddRange(line);
            result = new List<XSharpToken>();
            var usedRules = new PPUsedRules(this, MaxUDCDepth);
            while (temp.Count > 0)
            {
                PPMatchRange[] matchInfo = null;
                var rule = transRules.FindMatchingRule(temp, out matchInfo);
                if (rule != null)
                {
                    temp = doReplace(temp, rule, matchInfo);
                    if (usedRules.HasRecursion(rule, temp))
                    {
                        // duplicate, so exit now
                        result.Clear();
                        return false;
                    }
                    // note that we do not add the result of the replacement to processed
                    // because it will processed further
                }
                else
                {
                    // first token of temp is not start of a #(x)translate. So add it to the result
                    // and try from second token etc
                    result.Add(temp[0]);
                    temp.RemoveAt(0);
                }
            }
            if (usedRules.Count > 0)
            {
                result.TrimLeadingSpaces();
                return true;
            }
            result = null;
            return false;
        }

        private List<List<XSharpToken>> splitCommands(IList<XSharpToken> tokens, out List<XSharpToken> separators)
        {
            var result = new List<List<XSharpToken>>(10); 
            var current = new List<XSharpToken>(tokens.Count);
            separators = new List<XSharpToken>();
            foreach (var t in tokens)
            {
                if (t.Type == XSharpLexer.EOS)
                {
                    current.TrimLeadingSpaces();
                    result.Add(current);
                    current = new List<XSharpToken>();
                    separators.Add(t);
                }
                else
                {
                    current.Add(t);
                }
            }
            result.Add(current);
            return result;
        }
        private bool doProcessCommands(List<XSharpToken> line, out List<XSharpToken>  result)
        {
            Debug.Assert(line?.Count > 0);
            line.TrimLeadingSpaces();
            result = null;
            if (line.Count == 0)
                return false;
            result = line;
            var usedRules = new PPUsedRules(this, MaxUDCDepth);
            while (true)
            {
                PPMatchRange[] matchInfo = null;
                var rule = cmdRules.FindMatchingRule(result, out matchInfo);
                if (rule == null)
                {
                    // nothing to do, so exit. Leave changed the way it is. This does not have to be the first iteration
                    break; 
                }
                result = doReplace(result, rule, matchInfo);
                if (usedRules.HasRecursion(rule, result))
                {
                    // duplicate so exit now
                    result.Clear();
                    return false;
                }
                // the UDC may have introduced a new semi colon and created more than one sub statement
                // so check to see and then process every statement
                List<XSharpToken> separators;
                var cmds = splitCommands(result, out separators);
                Debug.Assert(cmds.Count == separators.Count + 1);
                if (cmds.Count <= 1)
                {
                    // single statement result. Try again to see if the new statement matches another UDC rule
                    continue; 
                }
                else
                {
                    // multi statement result. Process each statement separately (recursively) as a 'normal line'
                    // the replacement may have introduced the usage of a define, translate or macro
                    result.Clear();
                    for (int i = 0; i < cmds.Count; i++)
                    {
                        if (cmds[i].Count > 0)
                        {
                            cmds[i] = doNormalLine(cmds[i], false);
                            result.AddRange(cmds[i]);
                            if (i < cmds.Count - 1)
                            {
                                result.Add(separators[i]);
                            }
                        }
                    }
                    // recursive processing should have done everything, so exit
                    break;
                }
            }
            if (usedRules.Count > 0)
            {
                result.TrimLeadingSpaces();
                return true;
            }
            result = null;
            return false;
        }

        private void doEOFChecks()
        {
            if (defStates.Count > 0)
            {
                _parseErrors.Add(new ParseErrorData(Lt(), ErrorCode.ERR_EndifDirectiveExpected));
            }
            while (regions.Count > 0)
            {
                var token = regions.Pop();
                _parseErrors.Add(new ParseErrorData(token, ErrorCode.ERR_EndRegionDirectiveExpected));
            }
        }

        #endregion

  
        private List<XSharpToken> doReplace(IList<XSharpToken> line, PPRule rule, PPMatchRange[] matchInfo)
        {
            Debug.Assert(line?.Count > 0);
            var res = rule.Replace(line, matchInfo);
            rulesApplied += 1;
            List<XSharpToken> result = new List<XSharpToken>();
            result.AddRange(res);
            if (_options.Verbose)
            {
                int lineNo;
                if (line[0].SourceSymbol != null)
                    lineNo = line[0].SourceSymbol.Line;
                else
                    lineNo = line[0].Line;
                DebugOutput("----------------------");
                DebugOutput("File {0} line {1}:", _fileName, lineNo);
                DebugOutput("   UDC   : {0}", rule.GetDebuggerDisplay());
                DebugOutput("   Input : {0}", line.AsString());
                DebugOutput("   Output: {0}", res.AsString());
            }
            return result;
        }
    }
}

