﻿//
// Copyright (c) XSharp B.V.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0.
// See License.txt in the project root for license information.
//
#nullable disable
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using XP = LanguageService.CodeAnalysis.XSharp.SyntaxParser.XSharpParser;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceMemberFieldSymbol : SourceFieldSymbolWithSyntaxReference
    {
        string currentDefine = null; // to detect recursion
        internal TypeWithAnnotations GetVOGlobalType(CSharpCompilation compilation, TypeSyntax typeSyntax, Binder binder, ConsList<FieldSymbol> fieldsBeingBound)
        {
            var xNode = this.SyntaxNode.XNode;
            if (compilation.Options.HasOption(CompilerOption.ResolveTypedFunctionPointersToPtr,this.SyntaxNode))
            {
                XP.DatatypeContext dt = null;
                if (xNode is XP.ClassvarContext cvc)
                {
                    dt = cvc.DataType;
                    // This is a typed PTR in a Class Var / VO Global
                }
                if (xNode is XP.VostructmemberContext smc)
                {
                    dt = smc.DataType;
                    // This is a typed PTR in a Structure or Union Member
                }
                if (xNode is XP.LocalvarContext lvc)
                {
                    dt = lvc.DataType;
                    // This is a typed PTR in a Local Variable
                }
                // User Define Typename PTR
                // change the type from typed ptr to just ptr
                if (dt is XP.PtrDatatypeContext ptrdtc && ptrdtc.TypeName.Name != null)
                {
                    // So we have a global as typed ptr
                    string name = ptrdtc.TypeName.Name.GetText();
                    // Lookup name ?
                    return TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_IntPtr));
                }

            }
            TypeSymbol type;
            if (xNode is XP.VodefineContext)
            {
                var vodef = xNode as XP.VodefineContext;
                DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
                // detect recursion
                if (XSharpString.Equals(vodef.Id.GetText(), currentDefine))
                {
                    currentDefine = null;
                    return default;
                }
                currentDefine = vodef.Id.GetText();
                type = binder.BindType(typeSyntax, diagnostics).Type;
                // parser could not determine the type
                fieldsBeingBound = new ConsList<FieldSymbol>(this, fieldsBeingBound);
                var declarator = (VariableDeclaratorSyntax)this.DeclaringSyntaxReferences.AsSingleton().GetSyntax();
                var initializerBinder = new ImplicitlyTypedFieldBinder(binder, fieldsBeingBound);
                var initializerOpt = initializerBinder.BindInferredVariableInitializer(diagnostics, RefKind.None, declarator.Initializer, declarator);
                if (initializerOpt != null && !type.IsPszType())
                {
                    if (initializerOpt.Type is { } && !initializerOpt.Type.IsErrorType())
                    {
                        if (vodef.DataType == null)
                        {
                            type = initializerOpt.Type;
                        }
                        if (!type.IsVoidPointer() && initializerOpt.ConstantValue != null
                            && !this.IsConst && type.CanBeConst() && !type.IsObjectType())
                        {
                            this._modifiers |= DeclarationModifiers.Const;
                            this._modifiers |= DeclarationModifiers.Static;
                            this._modifiers &= ~DeclarationModifiers.ReadOnly;
                        }
                        if (type.IsEnumType())
                        {
                            type = type.GetEnumUnderlyingType();
                        }
                    }
                }
                if (type is null)
                {
                    type = compilation.GetSpecialType(SpecialType.System_Object);
                }
                //System.Diagnostics.Debug.WriteLine($"Looking for type of define {vodef.Name.ToString()}, found {type.ToString()}, const: {IsConst}");
                currentDefine = null;
                return TypeWithAnnotations.Create(type);
            }
            return default;
        }
    }
}
