/*
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
grammar XSharp;

/*
 * Parser Rules
*/

// Known issues:
// - preprocessor , #region, #using etc

options	{
        language=CSharp;
        tokenVocab=XSharpLexer;
        }

source				: (Entities+=entity)* EOF
                    ;

entity              : namespace_
                    | class_
                    | structure_
                    | interface_
                    | delegate_
                    | event_
                    | enum_
                    | function                  // This will become part of the 'Globals' class
                    | procedure                 // This will become part of the 'Globals' class
                    | method                    // Method xxx Class xxx syntax
                    | globalAttributes          // Assembly attributes, Module attributes etc.
                    | using_                    // Using Namespace
                    | voglobal                  // This will become part of the 'Globals' class
                    | vodefine                  // This will become part of the 'Globals' class
                    | vodll                     // External method of the Globals class
                    | vostruct					// Compatibility (unsafe) structure
                    | vounion					// Compatibility (unsafe) structure with members aligned at FieldOffSet 0
                    ;

eos					: EOS+
					;

function            : (Attributes=attributes)? (Modifiers=funcprocModifiers)?
                        (FUNCTION|FUNC) Id=identifier TypeParameters=typeparameters? (ParamList=parameterList)?
                       (AS Type=datatype)?
                       (ConstraintsClauses+=typeparameterconstraintsclause)*
                       (CallingConvention=callingconvention)?
					   (EXPORT LOCAL)?							// Export Local exists in VO but is ignored in X#
					   (DLLEXPORT STRING_CONST)? end=eos		// The DLLEXPORT clause exists in VO but is ignored in X#
                       StmtBlk=statementBlock
                    ;

procedure           : (Attributes=attributes)? (Modifiers=funcprocModifiers)?
						(PROCEDURE|PROC) Id=identifier TypeParameters=typeparameters? (ParamList=parameterList)? (AS VOID)? // As Void is allowed but ignored
						(ConstraintsClauses+=typeparameterconstraintsclause)*
						(CallingConvention=callingconvention)? InitExit=(INIT1|INIT2|INIT3|EXIT)?
   						(EXPORT LOCAL)?							// Export Local exists in VO but is ignored in X#
   						(DLLEXPORT STRING_CONST)? end=eos		// The DLLEXPORT clause exists in VO but is ignored in X#
						StmtBlk=statementBlock
                    ;

callingconvention	: Convention=(CLIPPER | STRICT | PASCAL | ASPEN | WINCALL | CALLBACK | FASTCALL | THISCALL)
                    ;


                    // there are many variations
                    // Simple:
                    // _DLL FUNCTION SetDebugErrorLevel( dwLevel AS DWORD) AS VOID PASCAL:USER32.SetDebugErrorLevel
                    // With Extension
                    // _DLL FUNC HTMLHelp(hwndCaller AS PTR, pszFile AS PSZ, uCommand AS LONG, dwData AS LONG) AS LONG PASCAL:HHCTRL.OCX.HtmlHelpA
                    // With Hint (which is ignored by Vulcan too)
                    // _DLL FUNC InternetHangUp(dwConnection AS DWORD, dwReserved AS DWORD) AS DWORD PASCAL:WININET.InternetHangUp#247
                    // And with numeric entrypoint, which is supported by VO but not by .NET
                    // We parse the numeric entrypoint here but we will throw an error during the tree transformation
                    // _DLL FUNCTION SetDebugErrorLevel( dwLevel AS DWORD) AS VOID PASCAL:USER32.123

vodll				: (Attributes=attributes)? 
					  (Modifiers=funcprocModifiers)? DLL
                      ( T=(FUNCTION|FUNC) Id=identifier ParamList=parameterList (AS Type=datatype)?
                      | T=(PROCEDURE|PROC) Id=identifier ParamList=parameterList )
                      (CallingConvention=dllcallconv) COLON
                      Dll=identifierString (DOT Extension=identifierString)?
                        ( DOT Entrypoint=identifierString (NEQ2 INT_CONST)?
                        | Ordinal=REAL_CONST)
                      ( CharSet=(AUTO | ANSI | UNICODE) )?
                      eos
                    ;

dllcallconv         : Cc=( CLIPPER | STRICT | PASCAL | THISCALL | FASTCALL | ASPEN | WINCALL | CALLBACK)
                    ;


parameterList		: LPAREN (Params+=parameter (COMMA Params+=parameter)*)? RPAREN
                    ;

parameter			: (Attributes=attributes)? Self=SELF? Id=identifier (ASSIGN_OP Default=expression)? (Modifiers=parameterDeclMods Type=datatype)?
					| Ellipsis=ELLIPSIS
                    ;

parameterDeclMods   : Tokens+=(AS | REF | OUT | IS | PARAMS) Tokens+=CONST?
                    ;

statementBlock      : (Stmts+=statement)*
                    ;


funcprocModifiers	: ( Tokens+=(STATIC | INTERNAL | PUBLIC | EXPORT | UNSAFE) )+
                    ;


using_              : USING (Static=STATIC)? (Alias=identifierName ASSIGN_OP)? Name=name eos
                    ;


voglobal			: (Attributes=attributes)? (Modifiers=funcprocModifiers)? GLOBAL (Const=CONST)? Vars=classVarList garbage? end=eos
                    ;


// Separate method/access/assign with Class name -> convert to partial class with just one method
// And when Class is outside of assembly, convert to Extension Method?
// nvk: we have no knowledge of whether a class is outside of the assembly at the parser stage!
method				: (Attributes=attributes)? (Modifiers=memberModifiers)?
                      T=methodtype (ExplicitIface=nameDot)? Id=identifier TypeParameters=typeparameters? (ParamList=parameterList)? (AS Type=datatype)?
                      (ConstraintsClauses+=typeparameterconstraintsclause)*
                      (CallingConvention=callingconvention)? (CLASS (Namespace=nameDot)? ClassId=identifier)?
					  (EXPORT LOCAL)?						// Export Local exists in VO but is ignored in X#
					  (DLLEXPORT STRING_CONST)? end=eos		// The DLLEXPORT clause exists in VO but is ignored in X#
                      StmtBlk=statementBlock
                    ;

methodtype			: Token=(METHOD | ACCESS | ASSIGN)
                    ;

// Convert to constant on Globals class. Expression must be resolvable at compile time
vodefine			: (Modifiers=funcprocModifiers)? DEFINE Id=identifier ASSIGN_OP Expr=expression (AS DataType=typeName)? garbage? eos
                    ;

vostruct			: (Modifiers=votypeModifiers)?
                      VOSTRUCT (Namespace=nameDot)? Id=identifier (ALIGN Alignment=INT_CONST)? eos
                      (Members+=vostructmember)+
                    ;

vostructmember		: MEMBER Dim=DIM Id=identifier LBRKT ArraySub=arraysub RBRKT (As=(AS | IS) DataType=datatype)? eos
                    | MEMBER Id=identifier (As=(AS | IS) DataType=datatype)? eos
                    ;


vounion				: (Modifiers=votypeModifiers)?
                      UNION (Namespace=nameDot)? Id=identifier eos
                      (Members+=vostructmember)+
                    ;

votypeModifiers		: ( Tokens+=(INTERNAL | PUBLIC | EXPORT | UNSAFE | STATIC ) )+
                    ;


namespace_			: BEGIN NAMESPACE Name=name eos
                      (Entities+=entity)*
                      END NAMESPACE garbage? eos
                    ;

interface_			: (Attributes=attributes)? (Modifiers=interfaceModifiers)?
                      INTERFACE (Namespace=nameDot)? Id=identifier TypeParameters=typeparameters?
                      ((INHERIT|COLON) Parents+=datatype)? (COMMA Parents+=datatype)*
                      (ConstraintsClauses+=typeparameterconstraintsclause)* eos         // Optional typeparameterconstraints for Generic Class
                      (Members+=classmember)*
                      END INTERFACE garbage? eos
					  ;

interfaceModifiers	: ( Tokens+=(NEW | PUBLIC | EXPORT | PROTECTED | INTERNAL | PRIVATE | HIDDEN | UNSAFE | PARTIAL) )+
                    ;

class_				: (Attributes=attributes)? (Modifiers=classModifiers)?
                      CLASS (Namespace=nameDot)? Id=identifier TypeParameters=typeparameters?		// TypeParameters indicate Generic Class
                      (INHERIT BaseType=datatype)?
                      (IMPLEMENTS Implements+=datatype (COMMA Implements+=datatype)*)?
                      (ConstraintsClauses+=typeparameterconstraintsclause)* eos						// Optional typeparameterconstraints for Generic Class
                      (Members+=classmember)*
                      END CLASS garbage? eos
					  ;

classModifiers		: ( Tokens+=(NEW | PUBLIC | EXPORT | PROTECTED | INTERNAL | PRIVATE | HIDDEN | ABSTRACT | SEALED | STATIC | UNSAFE | PARTIAL) )+
                    ;

// Start Extensions for Generic Classes
typeparameters      : LT TypeParams+=typeparameter (COMMA attributes? TypeParams+=typeparameter)* GT
                    ;

typeparameter       : Attributes=attributes? VarianceKeyword=(IN | OUT)? Id=identifier
                    ;

typeparameterconstraintsclause
                    : WHERE Name=identifierName IS Constraints+=typeparameterconstraint (COMMA Constraints+=typeparameterconstraint)*
                    ;

typeparameterconstraint: Key=(CLASS|STRUCTURE)				#classOrStructConstraint	//  Class Foo<t> WHERE T IS (CLASS|STRUCTURE)
                       | Type=typeName						#typeConstraint				//  Class Foo<t> WHERE T IS Customer
                       | NEW LPAREN RPAREN					#constructorConstraint		//  Class Foo<t> WHERE T IS NEW()
                       ;

// End of Extensions for Generic Classes

structure_			: (Attributes=attributes)? (Modifiers=structureModifiers)?
                      STRUCTURE (Namespace=nameDot)? Id=identifier TypeParameters=typeparameters?
                      (IMPLEMENTS Implements+=datatype (COMMA Implements+=datatype)*)?
                      (ConstraintsClauses+=typeparameterconstraintsclause)* eos
                      (Members+=classmember)*
                      END STRUCTURE garbage? eos
					  ;

structureModifiers	: ( Tokens+=(NEW | PUBLIC | EXPORT | PROTECTED | INTERNAL | PRIVATE | HIDDEN | UNSAFE | PARTIAL) )+
                    ;


delegate_			: (Attributes=attributes)? (Modifiers=delegateModifiers)?
                      DELEGATE (Namespace=nameDot)? Id=identifier TypeParameters=typeparameters?
                      ParamList=parameterList? (AS Type=datatype)?
                      (ConstraintsClauses+=typeparameterconstraintsclause)* eos
                    ;

delegateModifiers	: ( Tokens+=(NEW | PUBLIC | EXPORT | PROTECTED | INTERNAL | PRIVATE | HIDDEN | UNSAFE) )+
                    ;


enum_				: (Attributes=attributes)? (Modifiers=enumModifiers)?
                      ENUM (Namespace=nameDot)? Id=identifier (AS Type=datatype)? eos
                      (Members+=enummember)+
                      END ENUM? garbage? eos
                    ;

enumModifiers		: ( Tokens+=(NEW | PUBLIC| EXPORT | PROTECTED | INTERNAL | PRIVATE | HIDDEN) )+
                    ;

enummember			: (Attributes=attributes)? MEMBER? Id=identifier (ASSIGN_OP Expr=expression)? eos
                    ;

event_				:  (Attributes=attributes)? (Modifiers=eventModifiers)?
                       EVENT (ExplicitIface=nameDot)? Id=identifier (AS Type=datatype)?
                       ( end=eos
                        | (LineAccessors += eventLineAccessor)+ end=eos
                        | Multi=eos (Accessors+=eventAccessor)+ END EVENT? garbage? end=eos
                       )
                    ;

eventModifiers		: ( Tokens+=(NEW | PUBLIC | EXPORT | PROTECTED | INTERNAL | PRIVATE | HIDDEN | STATIC | VIRTUAL | SEALED | ABSTRACT | UNSAFE) )+
                    ;


eventLineAccessor   : Attributes=attributes? Modifiers=eventModifiers?
                      ( {InputStream.La(2) != REMOVE}? Key=ADD ExprList=expressionList?
                      | {InputStream.La(2) != ADD}?    Key=REMOVE ExprList=expressionList?
                      | Key=(ADD|REMOVE) )
                    ;
eventAccessor       : Attributes=attributes? Modifiers=eventModifiers?
                      ( Key=ADD     end=eos StmtBlk=statementBlock END ADD?
                      | Key=REMOVE  end=eos StmtBlk=statementBlock END REMOVE? )
                      garbage? end=eos
                    ;



classvars			: (Attributes=attributes)? (Modifiers=classvarModifiers)? Vars=classVarList eos
                    ;

classvarModifiers	: ( Tokens+=(INSTANCE| STATIC | CONST | INITONLY | PRIVATE | HIDDEN | PROTECTED | PUBLIC | EXPORT | INTERNAL | VOLATILE | UNSAFE | FIXED) )+
                    ;

classVarList		: Var+=classvar (COMMA Var+=classvar)* (As=(AS | IS) DataType=datatype)?
                    ;

classvar			: (Dim=DIM)? Id=identifier (LBRKT ArraySub=arraysub RBRKT)? (ASSIGN_OP Initializer=expression)?
                    ;

arraysub			: ArrayIndex+=expression (RBRKT LBRKT ArrayIndex+=expression)+		// x][y
                    | ArrayIndex+=expression (COMMA ArrayIndex+=expression)+			// x,y
                    | ArrayIndex+=expression
                    ;

property			: (Attributes=attributes)? (Modifiers=memberModifiers)?
                      PROPERTY (SELF ParamList=propertyParameterList | (ExplicitIface=nameDot)? Id=identifier) (ParamList=propertyParameterList)?  (AS Type=datatype)?
                      ( Auto=AUTO (AutoAccessors+=propertyAutoAccessor)* (ASSIGN_OP Initializer=expression)? end=eos	// Auto
                      | (LineAccessors+=propertyLineAccessor)+ end=eos													// Single Line
                      | Multi=eos (Accessors+=propertyAccessor)+  END PROPERTY? garbage? end=eos						// Multi Line
                      )
                    ;

propertyParameterList
                    : LBRKT  (Params+=parameter (COMMA Params+=parameter)*)? RBRKT
                    | LPAREN (Params+=parameter (COMMA Params+=parameter)*)? RPAREN			// Allow Parentheses as well
                    ;

propertyAutoAccessor: Attributes=attributes? Modifiers=memberModifiers? Key=(GET|SET)
                    ;

propertyLineAccessor: Attributes=attributes? Modifiers=memberModifiers?
                      ( {InputStream.La(2) != SET}? Key=GET Expr=expression?
                      | {InputStream.La(2) != GET}? Key=SET ExprList=expressionList?
                      | Key=(GET|SET) )
                    ;

expressionList	    : Exprs+=expression (COMMA Exprs+=expression)*
                    ;

propertyAccessor    : Attributes=attributes? Modifiers=memberModifiers?
                      ( Key=GET end=eos StmtBlk=statementBlock END GET?
                      | Key=SET end=eos StmtBlk=statementBlock END SET? )
                      garbage? end=eos
                    ;

classmember			: Member=method										#clsmethod
				    | decl=declare										#clsdeclare
                    | (Attributes=attributes)?
                      (Modifiers=constructorModifiers)?
                      CONSTRUCTOR (ParamList=parameterList)? (AS VOID)? // As Void is allowed but ignored
					  (CallingConvention=callingconvention)? 
					  (CLASS (Namespace=nameDot)? ClassId=identifier)?		// allowed but ignored
					  end=eos
                      (Chain=(SELF | SUPER)
					  (
						  (LPAREN RPAREN)
						| (LPAREN ArgList=argumentList RPAREN)
					  ) eos)?
                      StmtBlk=statementBlock							#clsctor
                    | (Attributes=attributes)?
                      (Modifiers=destructorModifiers)?
                      DESTRUCTOR (LPAREN RPAREN)? 
					  (CLASS (Namespace=nameDot)? ClassId=identifier)?		// allowed but ignored
					   end=eos
                      StmtBlk=statementBlock							#clsdtor
                    | Member=classvars									#clsvars
                    | Member=property									#clsproperty
                    | Member=operator_									#clsoperator
                    | Member=structure_									#nestedStructure
                    | Member=class_										#nestedClass
                    | Member=delegate_									#nestedDelegate
                    | Member=enum_										#nestedEnum
                    | Member=event_										#nestedEvent
                    | Member=interface_									#nestedInterface
                    | {_ClsFunc}? Member=function						#clsfunction		// Equivalent to static method
                    | {_ClsFunc}? Member=procedure						#clsprocedure		// Equivalent to static method
                    ;


constructorModifiers: ( Tokens+=( PUBLIC | EXPORT | PROTECTED | INTERNAL | PRIVATE | HIDDEN | EXTERN | STATIC ) )+
                    ;

declare				: DECLARE (ACCESS | ASSIGN | METHOD )  Ids+=identifier (COMMA Ids+=identifier)* eos
					;

destructorModifiers : ( Tokens+=EXTERN )+
                    ;
/*
    From the C# syntax guide:
    overloadable-unary-operator:  one of
    +   -   !  ~   ++   --   true   false
    overloadable-binary-operator:
    + - * / % & | ^  << right-shift == != > < >= <=
    // note in C# ^ is binary operator XOR and ~ is bitwise negation (Ones complement)
    // in VO ~is XOR AND bitwise negation. ^is EXP and should not be used for overloaded ops
    // VO uses ^ for Exponent

*/
overloadedOps		: Token= (PLUS | MINUS | NOT | TILDE | INC | DEC | TRUE_CONST | FALSE_CONST |
                              MULT | DIV | MOD | AMP | PIPE | LSHIFT | RSHIFT | EEQ | NEQ | NEQ2 |
                              GT | LT | GTE | LTE |
                              AND | OR )  // these two do not exist in C# and are mapped to & and |
                    ;

conversionOps		: Token=( IMPLICIT | EXPLICIT )
                    ;

operator_			: Attributes=attributes? Modifiers=operatorModifiers?
                      OPERATOR (Operation=overloadedOps | Conversion=conversionOps) Gt=GT?
                      ParamList=parameterList (AS Type=datatype)? end=eos StmtBlk=statementBlock
                    ;

operatorModifiers	: ( Tokens+=(PUBLIC | STATIC | EXTERN) )+
                    ;

memberModifiers		: ( Tokens+=(NEW | PRIVATE | HIDDEN | PROTECTED | PUBLIC | EXPORT | INTERNAL | STATIC | VIRTUAL | SEALED | ABSTRACT | ASYNC | UNSAFE | EXTERN | OVERRIDE) )+
                    ;

attributes			: ( AttrBlk+=attributeBlock )+
                    ;

attributeBlock		: LBRKT Target=attributeTarget? Attributes+=attribute (COMMA Attributes+=attribute)* RBRKT
                    ;

attributeTarget		: Id=identifier COLON
                    | Kw=keyword COLON
                    ;

attribute			: Name=name (LPAREN (Params+=attributeParam (COMMA Params+=attributeParam)* )? RPAREN )?
                    ;

attributeParam		: Name=identifierName ASSIGN_OP Expr=expression						#propertyAttributeParam
                    | Expr=expression													#exprAttributeParam
                    ;

globalAttributes    : LBRKT Target=globalAttributeTarget Attributes+=attribute (COMMA Attributes+=attribute)* RBRKT eos
                    ;

globalAttributeTarget : Token=(ASSEMBLY | MODULE) COLON
                    ;

statement           : Decl=localdecl                                            #declarationStmt
                    | {_xBaseVars}? xbasedecl									#xbasedeclStmt
                    | Decl=fielddecl											#fieldStmt
                    | DO? WHILE Expr=expression end=eos
                      StmtBlk=statementBlock 
					  ((e=END DO? | e=ENDDO) garbage? eos)?						#whileStmt
                    | NOP  end=eos												#nopStmt
                    | FOR
                        ( AssignExpr=expression
                        | (LOCAL? ForDecl=IMPLIED | ForDecl=VAR) ForIter=identifier ASSIGN_OP Expr=expression
                        | ForDecl=LOCAL ForIter=identifier ASSIGN_OP Expr=expression (AS Type=datatype)?
                        )
                      Dir=(TO | UPTO | DOWNTO) FinalExpr=expression
                      (STEP Step=expression)? end=eos
                      StmtBlk=statementBlock (e=NEXT garbage? eos)?				#forStmt
                    | IF IfStmt=ifElseBlock
                      ((e=END IF? | e=ENDIF)  garbage? eos)?					#ifStmt
                    | DO CASE end=eos
                      CaseStmt=caseBlock?
                      ((e=END CASE? | e=ENDCASE) garbage? eos)?					#caseStmt
                    | Key=EXIT end=eos											#jumpStmt
                    | Key=LOOP end=eos											#jumpStmt
                    | Key=BREAK Expr=expression? end=eos						#jumpStmt
                    | RETURN (VOID | Expr=expression)? end=eos					#returnStmt
                    | Q=(QMARK | QQMARK)
                       (Exprs+=expression (COMMA Exprs+=expression)*)? end=eos	#qoutStmt
                    | BEGIN SEQUENCE end=eos
                      StmtBlk=statementBlock
                      (RECOVER RecoverBlock=recoverBlock)?
                      (FINALLY eos FinBlock=statementBlock)?
                      (e=END (SEQUENCE)? garbage? eos)?							#seqStmt
                    //
                    // New in Vulcan
                    //
                    | REPEAT end=eos
                      StmtBlk=statementBlock
                      UNTIL Expr=expression eos									#repeatStmt
                    | FOREACH
                      (IMPLIED Id=identifier | Id=identifier AS Type=datatype| VAR Id=identifier)
                      IN Container=expression end=eos
                      StmtBlk=statementBlock (e=NEXT garbage? eos)?				#foreachStmt
                    | Key=THROW Expr=expression? end=eos						#jumpStmt
                    | TRY end=eos StmtBlk=statementBlock
                      (CATCH CatchBlock+=catchBlock?)*
                      (FINALLY eos FinBlock=statementBlock)?
                      (e=END TRY? eos)?											#tryStmt
                    | BEGIN Key=LOCK Expr=expression end=eos
                      StmtBlk=statementBlock
                      (e=END LOCK? garbage? end=eos)?								#blockStmt
                    | BEGIN Key=SCOPE end=eos
                      StmtBlk=statementBlock
                      (e=END SCOPE? garbage? end=eos)?								#blockStmt
                    //
                    // New XSharp Statements
                    //
                    | YIELD RETURN (VOID | Expr=expression)? end=eos			#yieldStmt
                    | YIELD Break=(BREAK|EXIT) end=eos							#yieldStmt
                    | (BEGIN|DO)? SWITCH Expr=expression end=eos
                      (SwitchBlock+=switchBlock)+
                      (e=END SWITCH?  end=eos)?										#switchStmt
                    | BEGIN Key=USING ( Expr=expression | VarDecl=variableDeclaration ) end=eos
                        StmtBlk=statementBlock
                      (e=END USING? garbage? end=eos)?								#blockStmt
                    | BEGIN Key=UNSAFE end=eos
                      StmtBlk=statementBlock
                      (e=END UNSAFE? eos)?										#blockStmt
                    | BEGIN Key=CHECKED end=eos
                      StmtBlk=statementBlock
                      (e=END CHECKED? garbage? end=eos)?							#blockStmt
                    | BEGIN Key=UNCHECKED end=eos
                      StmtBlk=statementBlock
                      (e=END UNCHECKED? garbage? end=eos)?							#blockStmt
                    | BEGIN Key=FIXED ( VarDecl=variableDeclaration ) end=eos
                      StmtBlk=statementBlock
                      (e=END FIXED? garbage? end=eos)?								#blockStmt

					// Temporary solution for statements missing from the standard header file
					| DEFAULT Variables+=simpleName TO Values+=expression 
						(COMMA Variables+=simpleName TO Values+=expression)* end=eos	#defaultStmt
					| Key=(WAIT|ACCEPT)  (Expr=expression)? (TO Variable=simpleName)? end = eos		#waitAcceptStmt
					| Key=(CANCEL|QUIT) end=eos											#cancelQuitStmt

					// NOTE: The ExpressionStmt rule MUST be last, even though it already existed in VO
                    | {InputStream.La(2) != LPAREN || // This makes sure that CONSTRUCTOR, DESTRUCTOR etc will not enter the expression rule
                       (InputStream.La(1) != CONSTRUCTOR && InputStream.La(1) != DESTRUCTOR) }?
                      Exprs+=expression (COMMA Exprs+=expression)* end=eos		#expressionStmt
                    ;

garbage				: {_allowGarbage}? (~EOS)+
					;

ifElseBlock			: Cond=expression end=eos StmtBlk=statementBlock
                      (ELSEIF ElseIfBlock=ifElseBlock | ELSE eos ElseBlock=statementBlock)?
                    ;

caseBlock			: Key=CASE Cond=expression end=eos StmtBlk=statementBlock NextCase=caseBlock?
                    | Key=OTHERWISE end=eos StmtBlk=statementBlock
                    ;

// Note that literalValue is not enough. We also need to support members of enums
switchBlock         : (Key=CASE Const=expression | Key=OTHERWISE) end=eos StmtBlk=statementBlock
                    ;

catchBlock			: (Id=identifier (AS Type=datatype)?)? end=eos StmtBlk=statementBlock
                    ;

recoverBlock		: (USING Id=identifier)? end=eos StmtBlock=statementBlock
                    ;

variableDeclaration	: (LOCAL? Var=IMPLIED | Var=VAR) Decl+=variableDeclarator (COMMA Decl+=variableDeclarator)*
                    | LOCAL Decl+=variableDeclarator (COMMA Decl+=variableDeclarator)* (AS Type=datatype)?
                    ;

variableDeclarator	: Id=identifier ASSIGN_OP Expr=expression
                    ;

// Variable declarations
// There are many variations in the declarations
// LOCAL a,b                        // USUAL in most languages
// LOCAL a as STRING
// LOCAL a,b as STRING
// LOCAL a,b as STRING, c as INT
// LOCAL a AS STRING, c as INT
// LOCAL a := "Foo" as STRING
// LOCAL a := "Foo" as STRING, c := 123 as INT

// Each Var may have a assignment and/or type
// When the type is missing and the following element has a type
// then the type of the following element propagates forward until for all elements without type

localdecl          : LOCAL						  LocalVars+=localvar (COMMA LocalVars+=localvar)*			end=eos #commonLocalDecl	
				   | Static=STATIC LOCAL		  LocalVars+=localvar (COMMA LocalVars+=localvar)*			end=eos #commonLocalDecl	
				   | {!XSharpLexer.IsKeyword(InputStream.La(2))}?   // STATIC Identifier , but not STATIC <Keyword>
				     Static=STATIC				  LocalVars+=localvar (COMMA LocalVars+=localvar)*			end=eos #commonLocalDecl	
				   // The following rules allow STATIC in the parser, 
				   // but the treetransformation will produce an error 9044 for STATIC implied
                   | Static=STATIC? VAR			  ImpliedVars+=impliedvar (COMMA ImpliedVars+=impliedvar)*	end=eos #varLocalDecl		// VAR special for Robert !
                   | Static=STATIC? LOCAL IMPLIED ImpliedVars+=impliedvar (COMMA ImpliedVars+=impliedvar)*	end=eos #varLocalDecl		
                   | Static=STATIC  IMPLIED		  ImpliedVars+=impliedvar (COMMA ImpliedVars+=impliedvar)*	end=eos #varLocalDecl		
                   ;

localvar           : (Const=CONST)? ( Dim=DIM )? Id=identifier (LBRKT ArraySub=arraysub RBRKT)?
                     (ASSIGN_OP Expression=expression)? (As=(AS | IS) DataType=datatype)?
                   ;

impliedvar         : (Const=CONST)? Id=identifier ASSIGN_OP Expression=expression
                   ;


fielddecl		   : FIELD Fields+=identifierName (COMMA Fields+=identifierName)* (IN Alias=identifierName)? end=eos
                   ;

// Old Style xBase declarations

xbasedecl        : T=(PRIVATE												// PRIVATE Foo, Bar
                      |PUBLIC												// PUBLIC  Foo, Bar
                      |MEMVAR												// MEMVAR  Foo, Bar
                      |PARAMETERS											// PARAMETERS Foo, Bar
                     )   Vars+=identifierName (COMMA Vars+=identifierName)* end=eos
                 ;

// The operators in VO have the following precedence level:
//    lowest (13)  assignment           := *= /= %= ^= += -= <<= >>=
//           (12)  logical or           .OR.
//           (11)  logical and          .AND.
//           (10)  logical negation     .NOT. !
//           ( 9)  bitwise or           |
//           ( 8)  bitwise xor          ~
//           ( 7)  bitwise and          &
//           ( 6)  relational           < <= > >= = == <> # != $
//           ( 5)  shift                << >>
//           ( 4)  additive             + -
//           ( 3)  multiplicative       * / %
//           ( 2)  exponentation        ^ **
//           ( 1)  unary                + - ++ -- ~

expression			: Expr=expression Op=(DOT | COLON) Name=simpleName			#accessMember			// member access The ? is new
                    | Expr=expression LPAREN                       RPAREN		#methodCall				// method call, no params
                    | Expr=expression LPAREN ArgList=argumentList  RPAREN		#methodCall				// method call, with params
                    | Expr=expression LBRKT ArgList=bracketedArgumentList  RBRKT #arrayAccess			// Array element access
                    | Left=expression Op=QMARK Right=boundExpression			#condAccessExpr			// expr ? expr
                    | LPAREN Type=datatype RPAREN Expr=expression				#typeCast			    // (typename) expr
                    | Expr=expression Op=(INC | DEC)							#postfixExpression		// expr ++/--
                    | Op=AWAIT Expr=expression									#awaitExpression		// AWAIT expr
                    | Op=(PLUS | MINUS | TILDE| ADDROF | INC | DEC)
                      Expr=expression											#prefixExpression		// +/-/~/&/++/-- expr
                    | Expr=expression Op=IS Type=datatype						#typeCheckExpression	// expr IS typeORid
                    | Expr=expression Op=ASTYPE Type=datatype					#typeCheckExpression	// expr AS TYPE typeORid
                    | Left=expression Op=EXP Right=expression					#binaryExpression		// expr ^ expr
                    | Left=expression Op=(MULT | DIV | MOD) Right=expression	#binaryExpression		// expr * expr
                    | Left=expression Op=(PLUS | MINUS) Right=expression		#binaryExpression		// expr +/- expr
                    | Left=expression Op=LSHIFT Right=expression				#binaryExpression		// expr << expr (shift)
                    | Left=expression Op=GT	Gt=GT Right=expression				#binaryExpression		// expr >> expr (shift)
                    | Left=expression
                      Op=( LT | LTE | GT | GTE | EQ | EEQ | SUBSTR | NEQ | NEQ2)
                      Right=expression											#binaryExpression		// expr >= expr (relational)
                    | Left=expression Op=AMP Right=expression					#binaryExpression		// expr & expr (bitwise and)
                    | Left=expression Op=TILDE Right=expression					#binaryExpression		// expr ~ expr (bitwise xor)
                    | Left=expression Op=PIPE Right=expression					#binaryExpression		// expr | expr (bitwise or)
                    | Op=(LOGIC_NOT|NOT) Expr=expression						#prefixExpression		// .not. expr (logical not)  also  !
                    | Left=expression Op=(LOGIC_AND | AND) Right=expression		#binaryExpression		// expr .and. expr (logical and) also &&
                    | Left=expression Op=LOGIC_XOR Right=expression				#binaryExpression		// expr .xor. expr (logical xor)
                    | Left=expression Op=(LOGIC_OR | OR) Right=expression		#binaryExpression		// expr .or. expr (logical or)  also ||
                    | Left=expression Op=DEFAULT Right=expression				#binaryExpression		// expr DEFAULT expr
                    | <assoc=right> Left=expression
                      Op=( ASSIGN_OP | ASSIGN_ADD | ASSIGN_SUB | ASSIGN_EXP
                            | ASSIGN_MUL | ASSIGN_DIV | ASSIGN_MOD
                            | ASSIGN_BITAND | ASSIGN_BITOR | ASSIGN_LSHIFT
                            | ASSIGN_RSHIFT | ASSIGN_XOR )
                      Right=expression											#assignmentExpression	// expr := expr, also expr += expr etc.
                    | Expr=primary												#primaryExpression
                    ;

                    // Primary expressions
primary				: Key=SELF													#selfExpression
                    | Key=SUPER													#superExpression
                    | Literal=literalValue										#literalExpression		// literals
                    | LiteralArray=literalArray									#literalArrayExpression	// { expr [, expr] }
                    | AnonType=anonType											#anonTypeExpression		// { .id := expr [, .id := expr] }
                    | CbExpr=codeblock											#codeblockExpression	// {| [id [, id...] | expr [, expr...] }
                    | Query=linqQuery											#queryExpression        // LINQ
                    | Type=datatype LCURLY Obj=expression COMMA
                      ADDROF Func=name LPAREN RPAREN RCURLY						#delegateCtorCall		// delegate{ obj , @func() }
                    | Type=datatype LCURLY RCURLY Init=objectOrCollectioninitializer?	#ctorCall		// id{  } with optional { Name1 := Expr1, [Name<n> := Expr<n>]}
                    | Type=datatype LCURLY ArgList=argumentList  RCURLY			#ctorCall				// id{ expr [, expr...] }
                    | ch=CHECKED LPAREN ( Expr=expression ) RPAREN				#checkedExpression		// checked( expression )
                    | ch=UNCHECKED LPAREN ( Expr=expression ) RPAREN			#checkedExpression		// unchecked( expression )
                    | TYPEOF LPAREN Type=datatype RPAREN						#typeOfExpression		// typeof( typeORid )
                    | SIZEOF LPAREN Type=datatype RPAREN						#sizeOfExpression		// sizeof( typeORid )
                    | DEFAULT LPAREN Type=datatype RPAREN						#defaultExpression		// default( typeORid )
                    | Name=simpleName											#nameExpression			// generic name
                    | Type=nativeType LPAREN Expr=expression RPAREN				#voConversionExpression	// nativetype( expr )
                    | XType=xbaseType LPAREN Expr=expression RPAREN				#voConversionExpression	// xbaseType( expr )
                    | Type=nativeType LPAREN CAST COMMA Expr=expression RPAREN	#voCastExpression		// nativetype(_CAST, expr )
                    | XType=xbaseType LPAREN CAST COMMA Expr=expression RPAREN	#voCastExpression		// xbaseType(_CAST, expr )
                    | PTR LPAREN Type=datatype COMMA Expr=expression RPAREN		#voCastPtrExpression	// PTR( typeName, expr )
					| Name=voTypeName											#voTypeNameExpression	// LONG, STRING etc., used as NUMERIC in expressions
                    | Type=typeName											    #typeExpression			// Standard DotNet Types
                    | Expr=iif													#iifExpression			// iif( expr, expr, expr )
                    | Op=(VO_AND | VO_OR | VO_XOR | VO_NOT) LPAREN Exprs+=expression
                      (COMMA Exprs+=expression)* RPAREN							#intrinsicExpression	// _Or(expr, expr, expr)
                    | FIELD_ ALIAS (Alias=identifier ALIAS)? Field=identifier   #aliasedField		    // _FIELD->CUSTOMER->NAME is equal to CUSTOMER->NAME
                    | {InputStream.La(4) != LPAREN}?                            // this makes sure that CUSTOMER->NAME() is not matched
                          Alias=identifier ALIAS Field=identifier               #aliasedField		    // CUSTOMER->NAME
                    | Id=identifier ALIAS Expr=expression                       #aliasedExpr            // id -> expr
                    | LPAREN Alias=expression RPAREN ALIAS Expr=expression      #aliasedExpr            // (expr) -> expr
                    | AMP LPAREN Expr=expression RPAREN							#macro					// &( expr )
                    | AMP Id=identifierName										#macro					// &id
                    | LPAREN Expr=expression RPAREN							    #parenExpression		// ( expr )
					| Key=ARGLIST												#argListExpression		// __ARGLIST
                    ;

boundExpression		: Expr=boundExpression Op=(DOT | COLON) Name=simpleName		#boundAccessMember		// member access The ? is new
                    | Expr=boundExpression LPAREN						RPAREN	#boundMethodCall		// method call, no params
                    | Expr=boundExpression LPAREN ArgList=argumentList  RPAREN	#boundMethodCall		// method call, with params
                    | Expr=boundExpression
                      LBRKT ArgList=bracketedArgumentList RBRKT					#boundArrayAccess		// Array element access
                    | <assoc=right> Left=boundExpression
                      Op=QMARK Right=boundExpression							#boundCondAccessExpr	// expr ? expr
                    | Op=(DOT | COLON) Name=simpleName							#bindMemberAccess
                    | LBRKT ArgList=bracketedArgumentList RBRKT					#bindArrayAccess
                    ;


objectOrCollectioninitializer :	ObjInit=objectinitializer
                              | CollInit=collectioninitializer
                              ;

objectinitializer		: LCURLY (Members+=memberinitializer (COMMA Members+=memberinitializer)*)? RCURLY
						;

memberinitializer		: Name=identifierName ASSIGN_OP Expr=initializervalue
						;

initializervalue		: Init=objectOrCollectioninitializer // Put this first to make sure we are not matching a literal array for { expr [, expr] }
						| Expr=expression
						;

collectioninitializer	: LCURLY Members+=expression (COMMA Members+=expression)* RCURLY
						;

bracketedArgumentList	: Args+=unnamedArgument (COMMA Args+=unnamedArgument)*
						;


unnamedArgument	   // NOTE: Separate rule for bracketedarguments because they cannot use idendifierName syntax
					:  Expr=expression?
                    ;

argumentList		  // NOTE: Optional argumentlist is handled in the rules that use this rule
					:  Args+=namedArgument (COMMA Args+=namedArgument)*
                    ;

namedArgument		// NOTE: Expression is optional so we can skip arguments for VO/Vulcan compatibility
					:  {_namedArgs}?  Name=identifierName ASSIGN_OP  ( RefOut=(REF | OUT) )? Expr=expression?
					|  ( RefOut=(REF | OUT) )? Expr=expression?
                    ;


iif					: (IIF|IF) LPAREN Cond=expression COMMA TrueExpr=expression COMMA FalseExpr=expression RPAREN
                    ;

nameDot				: Left=nameDot Right=simpleName DOT								#qualifiedNameDot
                    | Name=aliasedName DOT											#simpleOrAliasedNameDot
                    ;

name				: Left=name Op=DOT Right=simpleName								#qualifiedName
                    | Name=aliasedName												#simpleOrAliasedName
                    ;

aliasedName			: Global=GLOBAL Op=COLONCOLON Right=simpleName					#globalQualifiedName
                    | Alias=identifierName Op=COLONCOLON Right=simpleName			#aliasQualifiedName
                    | Name=simpleName												#identifierOrGenericName
                    ;

simpleName			: Id=identifier	GenericArgList=genericArgumentList?
                    ;

genericArgumentList : LT GenericArgs+=datatype (COMMA GenericArgs+=datatype)* GT
                    ;

identifierName		: Id=identifier
                    ;

datatype			: TypeName=typeName PTR											#ptrDatatype
                    | TypeName=typeName (Ranks+=arrayRank)+							#arrayDatatype
                    | TypeName=typeName 											#simpleDatatype
                    | TypeName=typeName QMARK 										#nullableDatatype
                    ;

arrayRank			: LBRKT (Commas+=COMMA)* RBRKT
                    ;

typeName			: NativeType=nativeType
                    | XType=xbaseType
                    | Name=name
                    ;

                    // Separate rule for Array with zero elements, to prevent entering the first arrayElement rule
                    // with a missing Expression which would not work for the core dialect
literalArray		: (LT Type=datatype GT)? LCURLY RCURLY															// {}
					| (LT Type=datatype GT)? LCURLY Elements+=arrayElement (COMMA Elements+=arrayElement)* RCURLY   // {e,e,e} or {e,,e} or {,e,} etc
                    ;

arrayElement        : Expr=expression?      // VO Array elements are optional
                    ;

anonType			: CLASS LCURLY (Members+=anonMember (COMMA Members+=anonMember)*)? RCURLY
                    ;

anonMember			: Name=identifierName ASSIGN_OP Expr=expression
					| Expr=expression
                    ;





codeblock			: LCURLY (OR | PIPE CbParamList=codeblockParamList? PIPE)
                      ( Expr=expression?
                      | eos StmtBlk=statementBlock
                      | ExprList=codeblockExprList )
                      RCURLY
                    ;

codeblockParamList	: Ids+=identifier (COMMA Ids+=identifier)*
                    ;

codeblockExprList	: (Exprs+=expression COMMA)+ ReturnExpr=expression
                    ;

// LINQ Support

linqQuery			: From=fromClause Body=queryBody
                    ;

fromClause          : FROM Id=identifier (AS Type=typeName)? IN Expr=expression
                    ;

queryBody           : (Bodyclauses+=queryBodyClause)* SorG=selectOrGroupclause (Continuation=queryContinuation)?
                    ;

queryBodyClause     : From=fromClause                                                                                           #fromBodyClause
                    | LET Id=identifier ASSIGN_OP Expr=expression                                                               #letClause
                    | WHERE Expr=expression                                                                                     #whereClause        // expression must be Boolean
                    | JOIN Id=identifier (AS Type=typeName)? IN Expr=expression ON OnExpr=expression EQUALS EqExpr=expression
                      Into=joinIntoClause?																						#joinClause
                    | ORDERBY Orders+=ordering (COMMA Orders+=ordering)*                                                        #orderbyClause
                    ;

joinIntoClause		: INTO Id=identifier
                    ;

ordering            : Expr=expression Direction=(ASCENDING|DESCENDING)?
                    ;

selectOrGroupclause : SELECT Expr=expression                                #selectClause
                    | GROUP Expr=expression BY ByExpr=expression            #groupClause
                    ;

queryContinuation   : INTO Id=identifier Body=queryBody
                    ;
// -- End of LINQ


// All New Vulcan and X# keywords can also be recognized as Identifier
identifier			: Token=(ID  | KWID)
                    | VnToken=keywordvn
                    | XsToken=keywordxs
                    ;

identifierString	: Token=(ID | KWID | STRING_CONST)
                    | VnToken=keywordvn
                    | XsToken=keywordxs
                    ;

// xBaseTypes are NOT available in the Core dialect and therefore separated here.
xbaseType			: Token=
                    ( ARRAY
                    | CODEBLOCK
                    | DATE
                    | FLOAT
                    | PSZ
                    | SYMBOL
                    | USUAL)
                    ;

nativeType			: Token=
                    ( BYTE
                    | DWORD
                    | DYNAMIC
                    | SHORTINT
                    | INT
                    | INT64
                    | LOGIC
                    | LONGINT
                    | OBJECT
                    | PTR
                    | REAL4
                    | REAL8
                    | STRING
                    | UINT64
                    | WORD
                    | VOID
                    | CHAR )
                    ;

voTypeName			: Token=
					( ARRAY
					| BYTE
					| CHAR				// New in XSharp
					| CODEBLOCK
					| DATE
					| DWORD
					| DYNAMIC			// new in XSharp
					| FLOAT
					| SHORTINT
					| INT
					| INT64				// New in Vulcan
					| LOGIC
					| LONGINT
					| OBJECT
					| PSZ
					| PTR
					| REAL4
					| REAL8
					| SHORTINT
					| STRING
					| SYMBOL
					| UINT64			// New in Vulcan
					| USUAL
					| VOID
					| WORD)
					;
literalValue		: Token=
                    ( TRUE_CONST
                    | FALSE_CONST
					| CHAR_CONST
                    | STRING_CONST
                    | ESCAPED_STRING_CONST
                    | INTERPOLATED_STRING_CONST
                    | SYMBOL_CONST
                    | HEX_CONST
                    | BIN_CONST
                    | REAL_CONST
                    | INT_CONST
                    | DATE_CONST
                    | NIL
                    | NULL
                    | NULL_ARRAY
                    | NULL_CODEBLOCK
                    | NULL_DATE
                    | NULL_OBJECT
                    | NULL_PSZ
                    | NULL_PTR
                    | NULL_STRING
                    | NULL_SYMBOL )
                    ;


keyword             : (KwVo=keywordvo | KwVn=keywordvn | KwXs=keywordxs) ;

keywordvo           : Token=(ACCESS | AS | ASSIGN | BEGIN | BREAK | CASE | CAST | CLASS | DLL | DO 
                    | ELSE | ELSEIF | END | ENDCASE | ENDDO | ENDIF | EXIT | EXPORT | FOR | FUNCTION 
                    | HIDDEN | IF | IIF | IS | LOCAL | LOOP | MEMBER | METHOD | NEXT | OTHERWISE
                    | PRIVATE | PROCEDURE | PROTECTED | PTR | PUBLIC | RECOVER | RETURN | SELF| SIZEOF | SUPER
                    | TO | TYPEOF | WHILE | TRY | VO_AND | VO_NOT | VO_OR | VO_XOR
					// The following new keywords cannot be in the keywordVN list because it will match an expression when used on their own
					| REPEAT | CONSTRUCTOR | CATCH | DESTRUCTOR | FINALLY 
					)
                    ;


keywordvn           : Token=(ABSTRACT | ANSI | AUTO | CHAR | CONST |  DEFAULT | EXPLICIT | FOREACH | GET | IMPLEMENTS | IMPLICIT | IMPLIED | INITONLY | INTERNAL
                    | LOCK | NAMESPACE | NEW | OUT | PARTIAL | SCOPE | SEALED | SET |  TRY | UNICODE |  VALUE | VIRTUAL  
   					)
                    ;

keywordxs           : Token=( ADD | ARGLIST | ASCENDING | ASSEMBLY | ASYNC | AWAIT | BY | CHECKED | DESCENDING | DYNAMIC | EQUALS | EXTERN | FIELD_ | FIXED | FROM 
                    | GROUP | INTO | JOIN | LET | MODULE | NAMEOF | NOP | ON | ORDERBY | OVERRIDE |PARAMS | REMOVE 
                    | SELECT | SWITCH | UNCHECKED | UNSAFE | VAR | VOLATILE | WHERE | YIELD | CHAR 
                    | MEMVAR | PARAMETERS  // Added as XS keywords to allow them to be treated as IDs
                    // the following entity keywords will be never used 'alone' and can therefore be safely defined as identifiers
					| DEFINE| DELEGATE | ENUM | GLOBAL | INHERIT | INTERFACE | OPERATOR	| PROPERTY | STRUCTURE | VOSTRUCT   
					// The following are never used 'alone' and are harmless as identifiers
					| ALIGN | CALLBACK | CLIPPER  | DECLARE | DIM | DOWNTO | DLLEXPORT | EVENT 
					| FASTCALL | FIELD | FUNC | IN | INSTANCE | PASCAL | PROC | SEQUENCE 
					| STEP | STRICT | THISCALL | UNION | UNTIL | UPTO | USING | WINCALL 
					| WAIT | ACCEPT | CANCEL | QUIT // UDCs 
					)
                    ;
