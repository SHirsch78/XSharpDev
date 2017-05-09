﻿using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageService.CodeAnalysis.XSharp.SyntaxParser
{
    public class XSharpToken : CommonToken, Microsoft.CodeAnalysis.IMessageSerializable
    {
        //internal string SourceFileName;
        internal string MappedFileName;
        internal int OriginalChannel;
        internal int MappedLine = -1;
        internal XSharpToken SourceSymbol;
        private XSharpToken _original = null;

        internal XSharpToken(IToken t) : base(t)
        {
            if (t is XSharpToken && t != this)
            {
                var t2 = t as XSharpToken;
                _original = t2.Original;
                OriginalChannel = t2.OriginalChannel;
                Channel = t2.OriginalChannel;
            }
        }
        internal XSharpToken(IToken t, int type, string text) : base(t)
        {
            Type = type;
            Text = text;
        }
        internal XSharpToken(int type, string text) : base(type, text)
        {
        }
        internal XSharpToken(int type) : base(type)
        {
        }
        internal XSharpToken(Tuple<ITokenSource, ICharStream> source, int type, int channel, int start, int stop) :
            base(source, type, channel, start, stop)
        {
            OriginalChannel = channel;
        }

        internal XSharpToken Original
        {
            get
            {
                // There could be several replacements, so walk up the tree
                var org = _original;
                while (org != null && org.Original != null && org.Original != org)
                {
                    org = org.Original;
                }
                if (org != null)
                {
                    return org;
                }
                return this;
            }
            set
            {
                _original = value;
            }
        }
        public string SourceName
        {
            get
            {
                return Original.TokenSource?.SourceName;
            }
        }
        public override int Line
        {
            get
            {
                var org = this.Original;
                if (org == this)
                    return base.Line;
                return org.Line;
            }
        }
        public int FullWidth
        {
            get
            {
                if (StopIndex > StartIndex)
                    return StopIndex + StartIndex + 1;
                return 1;
            }
        }
        public int Position => StartIndex;

    }
}
