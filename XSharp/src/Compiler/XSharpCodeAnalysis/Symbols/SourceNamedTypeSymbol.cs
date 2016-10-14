﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // This is a type symbol associated with a type definition in source code.
    // That is, for a generic type C<T> this is the instance type C<T>.  
    internal sealed partial class SourceNamedTypeSymbol : SourceMemberContainerTypeSymbol, IAttributeTargetSymbol
    {
        private bool _isVoStructOrUnion = false;

        internal bool IsSourceVoStructOrUnion { get { return _isVoStructOrUnion; } }
        private int _voStructSize = -1;
        private int _voStructElementSize = -1;

        internal int VoStructSize { get { if (_voStructSize == -1) EvalVoStructMemberSizes(); return _voStructSize; } }
        internal int VoStructElementSize { get { if (_voStructElementSize == -1) EvalVoStructMemberSizes(); return _voStructElementSize; } }

        private void EvalVoStructMemberSizes()
        {
            if (_isVoStructOrUnion && DeclaringCompilation.Options.IsDialectVO)
            {
                int voStructSize = 0;
                int voStructElementSize = 0;
                int align = this.Layout.Alignment;
                foreach (var m in GetMembers())
                {
                    if (m.Kind == SymbolKind.Field)
                    {
                        var f = (FieldSymbol)m;
                        int sz, elsz;
                        if (f.IsFixed == true)
                        {
                            elsz = (f.Type as PointerTypeSymbol).PointedAtType.VoFixedBufferElementSizeInBytes();
                            sz = f.FixedSize * elsz;
                        }
                        else
                        {
                            sz = f.Type.VoFixedBufferElementSizeInBytes();
                            if ((f.Type as SourceNamedTypeSymbol)?.IsSourceVoStructOrUnion == true)
                            {
                                elsz = (f.Type as SourceNamedTypeSymbol).VoStructElementSize;
                            }
                            else if (f.Type.IsVoStructOrUnion())
                            {
                                elsz = f.Type.VoStructOrUnionLargestElementSizeInBytes();
                            }
                            else
                                elsz = sz;
                        }
                        if (sz != 0)
                        {
                            int al = align;
                            if (elsz < al)
                                al = elsz;
                            if (voStructSize % al != 0)
                            {
                                voStructSize += al - (voStructSize % al);
                            }
                            if (!f.TypeLayoutOffset.HasValue)
                            {
                                // no explicit layout
                                voStructSize += sz;
                            }
                            else
                            {
                                // field offset is set: this is a union
                                int fieldLen = sz + f.TypeLayoutOffset.Value;
                                if (fieldLen > voStructSize)
                                {
                                    voStructSize = fieldLen;
                                }
                            }

                            if (voStructElementSize < sz)
                                voStructElementSize = sz;
                        }
                    }
                }
                _voStructSize = voStructSize;
                _voStructElementSize = voStructElementSize;
            }
        }
        internal SynthesizedAttributeData GetVoStructAttributeData()
        {
            var syntax = ((CSharpSyntaxNode)declaration.SyntaxReferences.FirstOrDefault()?.GetSyntax());
            var attributeType = DeclaringCompilation.GetWellKnownType(WellKnownType.Vulcan_Internal_VOStructAttribute);
            var int32type = DeclaringCompilation.GetSpecialType(SpecialType.System_Int32);
            var attributeConstructor = attributeType.GetMembers(".ctor").FirstOrDefault() as MethodSymbol;
            var constructorArguments = ArrayBuilder<TypedConstant>.GetInstance();
            constructorArguments.Add(new TypedConstant(int32type, TypedConstantKind.Primitive, VoStructSize));
            constructorArguments.Add(new TypedConstant(int32type, TypedConstantKind.Primitive, VoStructElementSize));
            return new SynthesizedAttributeData(attributeConstructor, constructorArguments.ToImmutableAndFree(), ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        }

    }
}