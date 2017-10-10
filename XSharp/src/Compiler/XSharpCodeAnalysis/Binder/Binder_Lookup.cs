﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        /// <summary>
        /// Look for any symbols in scope with the given name and arity.
        /// </summary>
        /// <remarks>
        /// Makes a second attempt if the results are not viable, in order to produce more detailed failure information (symbols and diagnostics).
        /// </remarks>
        private Binder XSLookupSymbolsWithFallback(LookupResult result, string name, int arity, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConsList<Symbol> basesBeingResolved = null, LookupOptions options = LookupOptions.Default)
        {
            Debug.Assert(options.AreValid());

            // don't create diagnosis instances unless lookup fails
            var binder = this.LookupSymbolsInternal(result, name, arity, basesBeingResolved, options, diagnose: false, useSiteDiagnostics: ref useSiteDiagnostics);
            FilterResults(result, options);
            if (result.Kind != LookupResultKind.Viable && result.Kind != LookupResultKind.Empty)
            {
                result.Clear();
                // retry to get diagnosis
                var otherBinder = this.LookupSymbolsInternal(result, name, arity, basesBeingResolved, options, diagnose: true, useSiteDiagnostics: ref useSiteDiagnostics);
                Debug.Assert(binder == otherBinder);
            }

            Debug.Assert(result.IsMultiViable || result.IsClear || result.Error != null);
            return binder;
        }
        private Binder XSLookupSymbolsInternal(
            LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(result.IsClear);
            Debug.Assert(options.AreValid());

            // Try to look for functions first
            if (Compilation.Options.IsDialectVO)
            {
                // check for function calls method calls outside the current class
                bool check = (options.HasFlag(LookupOptions.MustNotBeInstance) && !options.HasFlag(LookupOptions.MustNotBeMethod));
                if (check)
                {
                    var funcOptions = options;
                    funcOptions |= LookupOptions.MustBeInvocableIfMember;
                    Binder scope = this;
                    while (scope != null)
                    {
                        if (scope is InContainerBinder && scope.ContainingType == null) // at the namespace level, so outside of all types
                        {
                            scope.LookupSymbolsInSingleBinder(result, name, arity, basesBeingResolved, funcOptions, this, diagnose, ref useSiteDiagnostics);
                            FilterResults(result, options);
                            if (!result.IsClear)
                            {
           					    break;
                            }
                        }
                        scope = scope.Next;
                    }
                }
            }
            LookupResult functionResults = LookupResult.GetInstance();

            if (!result.IsClear)
            {
                foreach (var symbol in result.Symbols)
                {
                    if (symbol is MethodSymbol )
                    {
                        var ms = symbol as MethodSymbol;
                        if (ms.IsStatic && ms.ContainingType.Name.EndsWith("Functions",System.StringComparison.OrdinalIgnoreCase))
                        {
                            SingleLookupResult single = new SingleLookupResult(LookupResultKind.Viable, ms, null);
                            functionResults.MergeEqual(result);
                        }
                    }
                }
                result.Clear();
            }
            Binder binder = null;
            for (var scope = this; scope != null && !result.IsMultiViable; scope = scope.Next)
            {
                if (binder != null)
                {
                    var tmp = LookupResult.GetInstance();
                    scope.LookupSymbolsInSingleBinder(tmp, name, arity, basesBeingResolved, options, this, diagnose, ref useSiteDiagnostics);
                    FilterResults(tmp, options);
                    result.MergeEqual(tmp);
                    tmp.Free();
                }
                else
                {
                    scope.LookupSymbolsInSingleBinder(result, name, arity, basesBeingResolved, options, this, diagnose, ref useSiteDiagnostics);
                    FilterResults(result, options);
                    if (!result.IsClear)
                    {
                        binder = scope;
                    }
                }
            }
            if (!functionResults.IsClear)
            {
                // compare the original results with the results found 
                Symbol funcSym, sym;
                for (int i = 0; i < functionResults.Symbols.Count;i++)
                {
                    funcSym = functionResults.Symbols[i];
                    var funcFound = false;
                    for (int j = 0; j < result.Symbols.Count; j++)
                    {
                        sym = result.Symbols[j];
                        if (funcSym == sym)
                        {
                            funcFound = true;
                            break;
                        }
                    }
                    if (! funcFound && functionResults.Symbols.Count == result.Symbols.Count)
                    {
                        // IN VO Dialect we prefer the function over the static method
                        if (Compilation.Options.IsDialectVO)
                        {
                            var temp = functionResults;
                            functionResults = result;
                            result.Clear();
                            result.MergeEqual(temp);
                        }
                    }

                    if (! funcFound && funcSym.Kind == SymbolKind.Method)
                    {
                        for (int j = 0; j < result.Symbols.Count; j++)
                        {
                            sym = result.Symbols[j];
                            if (sym.Kind == SymbolKind.Method)
                            {
                                var info = new CSDiagnosticInfo(ErrorCode.WRN_VulcanAmbiguous,
                                    new object[] {
                                    funcSym.Name,
                                    new FormattedSymbol(sym, SymbolDisplayFormat.CSharpErrorMessageFormat),
                                    new FormattedSymbol(funcSym, SymbolDisplayFormat.CSharpErrorMessageFormat),
                                    sym.Kind.ToString()});
                                if (useSiteDiagnostics == null)
                                {
                                    useSiteDiagnostics = new HashSet<DiagnosticInfo>();
                                }
                                useSiteDiagnostics.Add(info);
                            }
                        }
                    }
                }
            }
            // C563 Make sure the error is generated for Inaccessible types.
            if (! result.IsClear && result.Kind == LookupResultKind.Inaccessible && result.Error != null )
            {
                // we only want to add this for internal fields (globals)
                if (result.Symbols[0].Kind == SymbolKind.Field)
                {
                    if (useSiteDiagnostics == null)
                    {
                        useSiteDiagnostics = new HashSet<DiagnosticInfo>();
                    }
                    useSiteDiagnostics.Add(result.Error);
                }
            }
            return binder;
        }

    }
}