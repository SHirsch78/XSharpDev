﻿//
// Copyright (c) XSharp B.V.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0.
// See License.txt in the project root for license information.
//


[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("xsi,PublicKey=0024000004800000940000000602000000240000525341310004000001000100b16a35b62bb33ce476c595e75bcc83fe4566c0a7cb9c093ce23e7add61fe1fc8a6edca2e542f0dc9ce41ec6b4260a73dda598c81f61a6f9522653ebfeae098a3bdb641020e843cbab825afe1c3910d42d17a1dcf211abb1cba4fc5e19569307c67a11c92b848d2df23f454d5ed1ab8b479afa4ece799445292b11012225aee96")]

// THIS IS A HACK to prevent errors from showing up in the IDE
#if !XSHARP_RUNTIME
namespace Microsoft.CodeAnalysis.Scripting
{
    internal class ScriptingResources : LanguageService.CodeAnalysis.Scripting.ScriptingResources { }
}

namespace Microsoft.CodeAnalysis.CSharp.Scripting
{
    internal class CSharpScriptingResources : LanguageService.CodeAnalysis.Scripting.XSharpScriptingResources { }
}
#endif
// HACK ENDS HERE
