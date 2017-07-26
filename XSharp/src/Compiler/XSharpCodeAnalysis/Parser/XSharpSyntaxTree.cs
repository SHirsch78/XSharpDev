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
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax;
using Antlr4.Runtime;
using LanguageService.CodeAnalysis.XSharp.SyntaxParser;
namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal static partial class SyntaxFactory
    {
        readonly static internal SyntaxTrivia WS = Whitespace(" ");

        internal static SyntaxToken MakeToken(SyntaxKind kind)
        {
            return Token(WS, kind, WS);
        }

        internal static SyntaxToken MakeToken(SyntaxKind kind, string text)
        {
            return Token(WS, kind, text, text, WS);
        }

        internal static SyntaxToken MakeIdentifier(string text)
        {
            return Identifier(WS, text, WS);
        }
    }
    [FlagsAttribute]
    enum XNodeFlags : byte
    {
        None = 0,
        XVodecl = 1 << 0,
        XVoIsDecl = 1 << 1,
        XPCall = 1 << 2,
        XGenerated = 1 << 3,
        XVoIsDim = 1 << 4,
    }

    internal abstract partial class CSharpSyntaxNode
    {
        public IXParseTree XNode { get; internal set; }
        private XNodeFlags xflags = XNodeFlags.None;
        public bool XVoDecl
        {
            get { return xflags.HasFlag(XNodeFlags.XVodecl); }
            set { if (value) xflags |= XNodeFlags.XVodecl; else xflags &= ~XNodeFlags.XVodecl; }
        }

        public bool XVoIsDecl
        {
            get { return xflags.HasFlag(XNodeFlags.XVoIsDecl); }
            set { if (value) xflags |= XNodeFlags.XVoIsDecl; else xflags &= ~XNodeFlags.XVoIsDecl; }
        }
        public bool XPCall
        {
            get { return xflags.HasFlag(XNodeFlags.XPCall); }
            set { if (value) xflags |= XNodeFlags.XPCall; else xflags &= ~XNodeFlags.XPCall; }
        }
        public bool XGenerated
        {
            get { return xflags.HasFlag(XNodeFlags.XGenerated); }
            set { if (value) xflags |= XNodeFlags.XGenerated; else xflags &= ~XNodeFlags.XGenerated; }
        }
        public bool XVoIsDim
        {
            get { return xflags.HasFlag(XNodeFlags.XVoIsDim); }
            set { if (value) xflags |= XNodeFlags.XVoIsDim; else xflags &= ~XNodeFlags.XVoIsDim; }
        }
    }

    internal sealed partial class CompilationUnitSyntax
    {
        public Dictionary<string, SourceText> IncludedFiles { get; internal set; } = new Dictionary<string, SourceText>();
        public ITokenStream XTokens { get; internal set; } = default(ITokenStream);
        public ITokenStream XPPTokens { get; internal set; } = default(ITokenStream);
        public IList<Tuple<int, string>> InitProcedures { get; internal set; } = new List<Tuple<int, string>>();
        public IList<FieldDeclarationSyntax> Globals { get; internal set; } = new List<FieldDeclarationSyntax>();
        public bool HasPCall { get; internal set; } = false;
        public bool NeedsProcessing { get; internal set; } = false;
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;
    public abstract partial class CSharpSyntaxNode
    {
        internal bool XVoDecl { get { return (CsGreen).XVoDecl; } }
        internal bool XVoIsDecl { get { return (CsGreen).XVoIsDecl; } }
        internal bool XPCall { get { return (CsGreen).XPCall; } }
        internal bool XGenerated { get { return (CsGreen).XGenerated; } }
        internal bool XVoIsDim { get { return (CsGreen).XVoIsDim; } }
    }
}

namespace Microsoft.CodeAnalysis
{
    public abstract partial class SyntaxNode
    {
        internal IXParseTree XNode { get { return ((CSharp.CSharpSyntaxNode)this).CsGreen.XNode ?? ((CSharp.CSharpSyntaxNode)this).Parent?.XNode; } }
        internal bool XIsMissingArgument
        {
            get
            {
                var n = XNode;
                if (n != null)
                {
                    if (n is XSharpParser.NamedArgumentContext)
                        return ((XSharpParser.NamedArgumentContext)n).Expr == null;
                    if (n is XSharpParser.UnnamedArgumentContext)
                        return ((XSharpParser.UnnamedArgumentContext)n).Expr == null;
                }
                return false;
            }
        }
        internal bool XIsCodeBlock
        {
            get
            {
                var n = XNode;
                if (n != null)
                {
                    return n.IsRealCodeBlock();
                }
                return false;
            }
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{

    public sealed partial class CompilationUnitSyntax
    {
        private InternalSyntax.CompilationUnitSyntax internalUnit => (InternalSyntax.CompilationUnitSyntax) this.CsGreen;
        public XSharpParser.SourceContext XSource => internalUnit.XNode as XSharpParser.SourceContext;
        public ITokenStream XTokens => internalUnit.XTokens;
        public ITokenStream XPPTokens => internalUnit.XPPTokens;
        public Dictionary<string, SourceText> IncludedFiles => internalUnit.IncludedFiles;
        public bool NeedsProcessing => internalUnit.NeedsProcessing;
    }
}


