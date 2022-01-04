﻿//
// Copyright (c) XSharp B.V.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0.
// See License.txt in the project root for license information.
//
#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using XP = LanguageService.CodeAnalysis.XSharp.SyntaxParser.XSharpParser;
using LanguageService.CodeAnalysis.XSharp.SyntaxParser;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{

    /// <summary>
    /// This portion of the binder converts an <see cref="ExpressionSyntax"/> into a <see cref="BoundExpression"/>.
    /// </summary>
    internal partial class Binder
    {

        ArrayBuilder<BoundExpression> XsGetDefaultArguments(ArrayBuilder<BoundExpression> arguments, ImmutableArray<ParameterSymbol> parameters)
        {
            if (parameters.Length == 0)
            {
                return arguments;
            }
            for (var i = 0; i < arguments.Count; i++)
            {
                var arg = arguments[i];
                if (arg.Syntax.XIsMissingArgument)
                {

                    if (i < parameters.Length && !parameters[i].IsParams)
                    {
                        var parameter = parameters[i];
                        arguments[i] = XsDefaultValue(parameter, arg.Syntax, Compilation);
                    }
                    else
                    {
                        var parameter = parameters[parameters.Length - 1];
                        if (parameter.IsParams && parameter.Type.IsArray())
                        {
                            var at = (ArrayTypeSymbol)parameter.Type;
                            if (!Equals(at.ElementType, arg.Type))
                            {
                                arguments[i] = new BoundDefaultExpression(arg.Syntax, at.ElementType, false);
                            }
                        }
                    }

                }
            }
            return arguments;
        }


        private static BoundExpression XsDefaultValue(ParameterSymbol parameter, SyntaxNode syntax, CSharpCompilation compilation)
        {
            TypeSymbol parameterType = parameter.Type;
            var defaultExpr = parameter.GetVODefaultParameter(syntax, compilation);
            if (defaultExpr == null)
                return null;
            if (!Equals(defaultExpr.Type, parameterType))
            {
                var implicitop = LocalRewriter.getImplicitOperatorByParameterType(parameterType, defaultExpr.Type);
                if (implicitop != null)
                {
                    var args = ImmutableArray.Create(defaultExpr);
                    var mcall = new BoundCall(syntax, null, implicitop, args, default, default, false, false, false, default, default, default, parameterType);
                    return mcall;
                }
            }
            if (parameterType is NamedTypeSymbol nts && nts.ConstructedFrom.IsPszType())
            {


                if (defaultExpr is BoundDefaultExpression)
                {
                    // ToDo
                    // when the parameter is of type PSZ and there was a literal default string
                    // then Vulcan generates a special literal array in the calling code.
                    // For example Foo(s := "abcë" AS PSZ) becomes a
                    // Foo([DefaultParameterValue("abc\x00eb", 0)] __Psz s)
                    //
                    // and in the calling code the default parameter is stored as a field of the <Module> class
                    //
                    // .field assembly static valuetype $ArrayType$5 䌤㘲␵$PSZ$_15_1 = ((61 62 63 EB 00))
                    //
                    // and a type is declared for the array size of 5 bytes. This type is declared in the global namespace:
                    //
                    // [StructLayout(LayoutKind.Explicit, Size=5, Pack=1)]
                    //    public struct $ArrayType$5
                    //    {
                    //    }
                    //
                    // The call to the function becomes
                    // Foo((__Psz) &䌤㘲␵$PSZ$_15_1);
                    // Nikos can you implement something like this ?
                    //
                    // defaultConstantValue = ConstantValue.Null;
                }
            }
            return defaultExpr;
        }
        private BoundExpression BindXsInvocationExpression(
            InvocationExpressionSyntax node,
            BindingDiagnosticBag diagnostics)
        {
            // Handle PCall() and Chr() in this special method
            BoundExpression result;
            if (TryBindNameofOperator(node, diagnostics, out result))
            {
                return result; // all of the binding is done by BindNameofOperator
            }
            // M(__arglist()) is legal, but M(__arglist(__arglist()) is not!
            bool isArglist = node.Expression.Kind() == SyntaxKind.ArgListExpression;
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();

            BindArgumentsAndNames(node.ArgumentList, diagnostics, analyzedArguments, allowArglist: !isArglist);
            BindPCall(node, diagnostics, analyzedArguments);

            if (isArglist)
            {
                result = BindArgListOperator(node, diagnostics, analyzedArguments);
            }
            else
            {
                if (node.XIsChr && analyzedArguments.Arguments.Count == 1 && analyzedArguments.Arguments[0].ConstantValue != null)
                {
                    var value = analyzedArguments.Arguments[0].ConstantValue.Int32Value;
                    var str = new string((char)value, 1);
                    result = new BoundLiteral(node, ConstantValue.Create(str), Compilation.GetSpecialType(SpecialType.System_String));
                }
                else
                {
                    // FoxPro allows () for array indexes
                    if (Compilation.Options.HasOption(CompilerOption.FoxArraySupport, node))
                    {
                        result = BindFoxProArrayPossibleAccess(node, analyzedArguments, diagnostics);
                        if (result != null)
                        {
                            return result;
                        }
                    }

                    BoundExpression boundExpression = BindMethodGroup(node.Expression, invoked: true, indexed: false, diagnostics: diagnostics);
                    boundExpression = CheckValue(boundExpression, BindValueKind.RValueOrMethodGroup, diagnostics);
                    string name = boundExpression.Kind == BoundKind.MethodGroup ? GetName(node.Expression) : null;
                    result = BindInvocationExpression(node, node.Expression, name, boundExpression, analyzedArguments, diagnostics);
                    if (result is BoundCall bc)
                    {

                        // check if MethodSymbol has the NeedAccessToLocals attribute combined with /fox2
                        if (Compilation.Options.Dialect == XSharpDialect.FoxPro &&
                            Compilation.Options.HasOption(CompilerOption.MemVars, node) &&
                            bc.Method.NeedAccessToLocals(out var writeAccess))
                        {
                            var localsymbols = new List<LocalSymbol>();
                            var binder = this;
                            while (binder != null)
                            {
                                localsymbols.AddRange(binder.Locals);
                                if (binder is InMethodBinder)
                                    break;
                                binder = binder.Next;
                            }
                            var root = node.SyntaxTree.GetRoot() as CompilationUnitSyntax;
                            if (localsymbols.Count > 0)
                            {
                                root.RegisterFunctionThatNeedsAccessToLocals(node.CsNode, writeAccess, localsymbols);
                            }
                        }
                    }
                }
            }
            analyzedArguments.Free();
            return result;

        }

        private BoundExpression BindFoxProArrayPossibleAccess(InvocationExpressionSyntax node, AnalyzedArguments analyzedArguments, BindingDiagnosticBag diagnostics)
        {
            if (node.XGenerated)
                return null;
            var xnode = node.XNode as XSharpParserRuleContext;

            var argCount = analyzedArguments.Arguments.Count;
            // Only array access for 1 or 2 arguments that have (possible) numeric indices
            if (argCount == 0 || argCount > 2)
                return null;
            var arg = analyzedArguments.Arguments[0];
            if (!arg.Type.IsPossibleArrayIndex())
            {
                return null;
            }
            if (argCount == 2)
            {
                arg = analyzedArguments.Arguments[1];
                if (!arg.Type.IsPossibleArrayIndex())
                {
                    return null;
                }
            }
            bool lhsOfAssignment = node.Parent is AssignmentExpressionSyntax aes && aes.Left == node;
            if (node.Expression is SimpleNameSyntax simple && !lhsOfAssignment)
            {
                // If the invocationExpression binds to a method or function in the runtime
                // then we NEVER see this as a parenthesized array access
                var idMethod = BindXSIdentifier(simple, invoked: true, indexed: false, diagnostics: diagnostics, bindMethod: true,
                        bindSafe: false);
                if (idMethod != null && idMethod is BoundMethodGroup bmg)
                {
                    if (bmg.Methods.Length > 0 && bmg.Methods[0].ContainingAssembly.IsRT())
                        return null;
                }
                var id1 = BindXSIdentifier(simple, invoked:false, indexed: true, diagnostics: diagnostics, bindMethod: false,
                    bindSafe: Compilation.Options.HasOption(CompilerOption.UndeclaredMemVars, node));
                // id1 will be either bound to a declared local, public or private
                // if undeclared locals are supported then Id1 may also find an undeclared local
                if (id1 == null)
                    return null;

                var args = new List<BoundExpression>();
                string strName = simple.Identifier.Text;
                if (id1 is BoundPropertyAccess bpa)
                    strName = bpa.PropertySymbol.Name;
                var name = ConstantValue.Create(strName);
                args.Add(new BoundLiteral(node, name, Compilation.GetSpecialType(SpecialType.System_String)));
                args.Add(id1);
                args.AddRange(analyzedArguments.Arguments);
                // Generate __FoxArrayAccess(name, value, dim1)
                // or Generate __FoxArrayAccess(name, value, dim1, dim2)
                var type = new BoundTypeExpression(node, null, Compilation.GetWellKnownType(WellKnownType.XSharp_VFP_Functions));
                return MakeInvocationExpression(node, type, ReservedNames.FoxArrayAccess, args.ToImmutableArray(), diagnostics);
            }
            var expression = BindExpression(node.Expression, diagnostics);
            if (expression.Kind != BoundKind.BadExpression)
            {
                var type = expression.Type;
                if (type.IsArrayType() || type.IsUsualType() || type.IsFoxArrayType())
                {
                    return BindIndexerOrVOArrayAccess(node.Expression, expression, analyzedArguments, diagnostics);
                }
            }
            return null;
        }
        private void BindPCall(InvocationExpressionSyntax node, BindingDiagnosticBag diagnostics, AnalyzedArguments analyzedArguments)
        {
            if (node.XPCall && node.Expression is GenericNameSyntax)
            {
                var gns = node.Expression as GenericNameSyntax;
                var arg = gns.TypeArgumentList.Arguments[0];
                var method = arg.ToFullString();
                bool pcallnative = method.IndexOf(XSharpSpecialNames.PCallNativePrefix, XSharpString.Comparison) >= 0;
                if (pcallnative)
                {
                    BindPCallNativeAndDelegate(node, analyzedArguments.Arguments, diagnostics, arg);
                }
                else
                {
                    BindPCallAndDelegate(node, analyzedArguments.Arguments, diagnostics, arg);
                }

            }

        }
        private string GetTypedPtrName(IXParseTree xNode)
        {
            // GLobals and Instance variables are all of type ClassvarContext
            if (xNode is XP.ClassvarContext cvc)
            {
                var pdtc = cvc.DataType as XP.PtrDatatypeContext;
                if (pdtc != null)
                    return pdtc.TypeName.GetText();

            }
            // Locals are of type LocalVarContext
            else if (xNode is XP.LocalvarContext lvc)
            {
                var pdtc = lvc.DataType as XP.PtrDatatypeContext;
                if (pdtc != null)
                    return pdtc.TypeName.GetText();
            }
            else if (xNode is XP.VostructmemberContext smc)
            {
                var pdtc = smc.DataType as XP.PtrDatatypeContext;
                if (pdtc != null)
                    return pdtc.TypeName.GetText();

            }
            return null;
        }
        private void BindPCallAndDelegate(InvocationExpressionSyntax node, ArrayBuilder<BoundExpression> args,
                BindingDiagnosticBag diagnostics, TypeSyntax type)
        {
            var XNode = node.XNode as XP.MethodCallContext;
            string method = XNode?.Expr.GetText();
            if (string.IsNullOrEmpty(method))
                method = "PCALL";
            if (!ValidatePCallArguments(node, args, diagnostics, method))
                return;
            var kind = args[0].Kind;
            if (kind != BoundKind.Local && kind != BoundKind.FieldAccess)
            {
                Error(diagnostics, ErrorCode.ERR_PCallFirstArgument, node, method, "typed function pointer");
                return;
            }
            string methodName = null;
            // Note that this does not get the syntax of the argument itself
            // but the syntax of the place where the symbol (Global, Field or Local) that the argument points to was defined
            SyntaxReference syntaxref = null;
            if (kind == BoundKind.FieldAccess)
            {
                var bfa = args[0] as BoundFieldAccess;  // Global or Field
                if (bfa != null && bfa.ExpressionSymbol.DeclaringSyntaxReferences.Length > 0)
                {
                    syntaxref = bfa.ExpressionSymbol.DeclaringSyntaxReferences[0] as SyntaxReference;
                }
            }
            else if (kind == BoundKind.Local)
            {
                var bl = args[0] as BoundLocal;         // Local
                if (bl != null && bl.LocalSymbol?.DeclaringSyntaxReferences.Length > 0)
                {
                    syntaxref = bl.LocalSymbol.DeclaringSyntaxReferences[0] as SyntaxReference;
                }
            }
            if (syntaxref != null)
            {
                CSharpSyntaxNode syntaxnode = syntaxref.GetSyntax() as CSharpSyntaxNode;
                var xNode = syntaxnode?.XNode;
                methodName = GetTypedPtrName(xNode);
            }

            if (methodName == null)
            {
                // first argument for pcall must be typed ptr
                Error(diagnostics, ErrorCode.ERR_PCallFirstArgument, node, method, "typed function pointer");
                return;
            }
            var lookupResult = LookupResult.GetInstance();
            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
            LookupOptions options = LookupOptions.AllMethodsOnArityZero;
            options |= LookupOptions.MustNotBeInstance;
            this.LookupSymbolsWithFallback(lookupResult, methodName, arity: 0, useSiteInfo: ref useSiteInfo, options: options);
            SourceMethodSymbol methodSym = null;
            if (lookupResult.IsClear)
            {
                // Cannot locate types pointer for pcall 
                Error(diagnostics, ErrorCode.ERR_PCallTypedPointerName, node, method, methodName);
                methodSym = null;
            }
            else if (lookupResult.IsMultiViable)
            {
                foreach (var symbol in lookupResult.Symbols)
                {
                    if (symbol.DeclaringCompilation == this.Compilation && symbol is SourceMethodSymbol)
                    {
                        methodSym = (SourceMethodSymbol)symbol;
                        break;
                    }
                }
            }
            else
            {
                methodSym = (SourceMethodSymbol)lookupResult.Symbols[0];
            }
            if (methodSym != null)
            {
                lookupResult.Clear();
                var ts = FindPCallDelegateType(type as IdentifierNameSyntax, ref useSiteInfo);
                if (ts is { } && ts.IsDelegateType())
                {
                    SourceDelegateMethodSymbol delmeth = ts.DelegateInvokeMethod() as SourceDelegateMethodSymbol;
                    // clone the parameters from the methodSym
                    var builder = ArrayBuilder<ParameterSymbol>.GetInstance();
                    foreach (var par in methodSym.Parameters)
                    {
                        var parameter = new SourceSimpleParameterSymbol(
                            owner: delmeth,
                            parameterType: TypeWithAnnotations.Create(par.Type),
                            ordinal: par.Ordinal,
                            refKind: par.RefKind,
                            name: par.Name,
                            locations: par.Locations);
                        builder.Add(parameter);
                    }
                    delmeth.InitializeParameters(builder.ToImmutableAndFree());
                    delmeth.SetReturnType(TypeWithAnnotations.Create(methodSym.ReturnType));
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_PCallResolveGeneratedDelegate, node, method, type.ToString());
                }
            }
            return;
        }
        private TypeSymbol FindPCallDelegateType(IdentifierNameSyntax type, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if (type == null)
                return null;
            var lookupResult = LookupResult.GetInstance();
            try
            {
                LookupOptions options = LookupOptions.NamespacesOrTypesOnly;
                this.LookupSymbolsSimpleName(lookupResult, null, type.Identifier.Text, 0, null, options, false, ref useSiteInfo);
                if (lookupResult.IsSingleViable)
                {
                    return lookupResult.Symbols[0] as TypeSymbol;
                }

                return null;
            }
            finally
            {
                lookupResult.Free();
            }
        }

        private bool ValidatePCallArguments(InvocationExpressionSyntax node, ArrayBuilder<BoundExpression> args,
                BindingDiagnosticBag diagnostics, string method)
        {
            bool ok = args.Count == 1;
            if (ok)
            {
                var argType = args[0].Type;
                ok = TypeSymbol.Equals(argType, Compilation.GetSpecialType(SpecialType.System_IntPtr));
                ok = ok | argType.IsVoidPointer();
                ok = ok | argType.IsPointerType();
            }
            if (!ok)
            {
                Error(diagnostics, ErrorCode.ERR_PCallFirstArgument, node, method, "pointer");
            }
            return ok;
        }

        private void BindPCallNativeAndDelegate(InvocationExpressionSyntax node, ArrayBuilder<BoundExpression> args,
                BindingDiagnosticBag diagnostics, TypeSyntax type)
        {
            var XNode = node.XNode as XP.MethodCallContext;
            string method = XNode?.Expr.GetText();
            if (string.IsNullOrEmpty(method))
                method = "PCALLNATIVE";
            if (!ValidatePCallArguments(node, args, diagnostics, method))
                return;
            // Our parent is the invocation expression of the delegate
            AnalyzedArguments analyzedArguments = AnalyzedArguments.GetInstance();
            try
            {
                var useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                var ts = FindPCallDelegateType(type as IdentifierNameSyntax, ref useSiteInfo);
                if (ts is { } && ts.IsDelegateType())
                {
                    var delmeth = ts.DelegateInvokeMethod() as SourceDelegateMethodSymbol;
                    // create new parameters based on the parameters from out parent call
                    var invoke = node.Parent as InvocationExpressionSyntax;
                    var realargs = invoke.ArgumentList;
                    var delparams = ts.DelegateParameters();
                    BindArgumentsAndNames(realargs, diagnostics, analyzedArguments);
                    var builder = ArrayBuilder<ParameterSymbol>.GetInstance();
                    int i = 0;
                    foreach (var expr in analyzedArguments.Arguments)
                    {
                        var ptype = expr.Type;
                        if (ptype is null)
                            ptype = new PointerTypeSymbol(TypeWithAnnotations.Create(Compilation.GetSpecialType(SpecialType.System_Void)));
                        var parameter = new SourceSimpleParameterSymbol(
                            owner: delmeth,
                            parameterType: TypeWithAnnotations.Create(ptype),
                            ordinal: i,
                            refKind: delparams[i].RefKind,
                            name: delparams[i].Name,
                            locations: delparams[i].Locations);
                        builder.Add(parameter);
                        i++;
                    }
                    delmeth.InitializeParameters(builder.ToImmutableAndFree());
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_PCallResolveGeneratedDelegate, node, method, type.ToString());
                }

                return;
            }
            finally
            {
                analyzedArguments.Free();
            }
        }

        internal void RemoveNamespacesFromResult(LookupResult result)
        {
            var correctSymbols = ArrayBuilder<Symbol>.GetInstance();
            try
            {
                foreach (var s in result.Symbols)
                {
                    if (s.Kind != SymbolKind.Namespace)
                        correctSymbols.Add(s);
                }
                if (correctSymbols.Count != result.Symbols.Count)
                {
                    result.Clear();
                    result.Symbols.AddRange(correctSymbols);
                }
                return;
            }
            finally
            {
                correctSymbols.Free();
            }
        }

    }
}

