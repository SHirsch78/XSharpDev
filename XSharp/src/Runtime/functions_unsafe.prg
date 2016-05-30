begin namespace XSharp.IO
	#region functions
	/// <summary>
	/// Create a new thread.
	/// </summary>
	/// <param name="pSecAttr"></param>
	/// <param name="nStackSize"></param>
	/// <param name="pFunc"></param>
	/// <param name="pParam"></param>
	/// <param name="dwFlags"></param>
	/// <param name="pdwID"></param>
	/// <returns>
	/// </returns>
	FUNCTION CreateVOThread(pSecAttr AS PTR,nStackSize AS DWORD,pFunc AS PTR,pParam AS PTR,dwFlags AS DWORD,pdwID AS DWORD PTR) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="hWndOwner"></param>
	/// <param name="cFiles"></param>
	/// <param name="fAsynchWork"></param>
	/// <returns>
	/// </returns>
	FUNCTION DoSendMail(hWndOwner AS PTR,cFiles AS STRING,fAsynchWork AS LOGIC) AS VOID
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Encode a file for e-mail transfer.
	/// </summary>
	/// <param name="hfIn"></param>
	/// <param name="hfOut"></param>
	/// <returns>
	/// </returns>
	FUNCTION EncodeBase64(hfIn AS PTR,hfOut AS PTR) AS INT
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// </summary>
	/// <param name="ptrEvInf"></param>
	/// <returns>
	/// </returns>
	FUNCTION EvalLaunch(ptrEvInf AS PTR) AS USUAL
		/// THROW NotImplementedException{}
	RETURN NIL   

	/// <summary>
	/// </summary>
	/// <param name="ptrEvInf"></param>
	/// <param name="ptrAny"></param>
	/// <returns>
	/// </returns>
	FUNCTION EvalNew(ptrEvInf AS PTR,ptrAny AS PTR) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// </summary>
	/// <param name="ptrEvInf"></param>
	/// <param name="ptrAny"></param>
	/// <returns>
	/// </returns>
	FUNCTION EvalPutParam(ptrEvInf AS PTR,ptrAny AS PTR) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// </summary>
	/// <param name="ptrEvInf"></param>
	/// <returns>
	/// </returns>
	FUNCTION EvalRelease(ptrEvInf AS PTR) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// Create a file or open and truncate an existing file.
	/// </summary>
	/// <param name="cFile"></param>
	/// <param name="uxFileAttr"></param>
	/// <returns>
	/// </returns>
	FUNCTION FCreate(cFile AS USUAL,uxFileAttr AS USUAL) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Open a file.
	/// </summary>
	/// <param name="cFile"></param>
	/// <param name="wMode"></param>
	/// <returns>
	/// </returns>
	FUNCTION FOpen(cFile AS USUAL,wMode AS USUAL) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Check to see if a value is in dynamic memory.
	/// </summary>
	/// <param name="ptrVar"></param>
	/// <returns>
	/// </returns>
	FUNCTION IsDynPtr(ptrVar AS PTR) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <param name="dwElem"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemArrayGet(ptrAny AS PTR,dwElem AS DWORD) AS USUAL
		/// THROW NotImplementedException{}
	RETURN NIL   

	/// <summary>
	/// </summary>
	/// <param name="px"></param>
	/// <param name="dw"></param>
	/// <param name="pa"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemArrayPut(px AS PTR,dw AS DWORD,pa AS PTR) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemGetDL(ptrAny AS PTR) AS LONG
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemGetL(ptrAny AS PTR) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemGetND(ptrAny AS PTR) AS REAL8
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemGetNL(ptrAny AS PTR) AS LONG
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <param name="lDate"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemPutDL(ptrAny AS PTR,lDate AS LONG) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <param name="fVal"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemPutL(ptrAny AS PTR,fVal AS LOGIC) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <param name="r8"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemPutND(ptrAny AS PTR,r8 AS REAL8) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <param name="lNum"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemPutNL(ptrAny AS PTR,lNum AS LONG) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemRegister(ptrAny AS PTR) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemRelease(ptrAny AS PTR) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemReturn(ptrAny AS PTR) AS USUAL
		/// THROW NotImplementedException{}
	RETURN NIL   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemSize(ptrAny AS PTR) AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// </summary>
	/// <param name="ptrAny"></param>
	/// <returns>
	/// </returns>
	FUNCTION ItemType(ptrAny AS PTR) AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// Functional form of BEGIN SEQUENCE.
	/// </summary>
	/// <param name="ptrLabel"></param>
	/// <returns>
	/// </returns>
	FUNCTION LabelPush(ptrLabel AS PTR) AS VOID
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Extract a substring of a certain size from the left of a buffer.
	/// </summary>
	/// <param name="ptrSource"></param>
	/// <param name="dwSize"></param>
	/// <returns>
	/// </returns>
	FUNCTION Mem2String(ptrSource AS PTR,dwSize AS DWORD) AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// Get the location of the first special console character in a buffer.
	/// </summary>
	/// <param name="ptrBuff"></param>
	/// <param name="wCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemAtSpecial(ptrBuff AS PTR,wCount AS DWORD) AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// Get a pointer to a byte in a memory buffer.
	/// </summary>
	/// <param name="ptrBuff"></param>
	/// <param name="b"></param>
	/// <param name="dwCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemByte(ptrBuff AS PTR,b AS BYTE,dwCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="ptrDest"></param>
	/// <param name="ptrSrc"></param>
	/// <param name="wCh"></param>
	/// <param name="wCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemCCopy(ptrDest AS PTR,ptrSrc AS PTR,wCh AS DWORD,wCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Check the validity of a pointer to a memory buffer.
	/// </summary>
	/// <param name="ptrMem"></param>
	/// <param name="cb"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemCheckPtr(ptrMem AS PTR,cb AS DWORD) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// Get a pointer to a matching character value in a memory buffer.
	/// </summary>
	/// <param name="ptrBuff"></param>
	/// <param name="wChar"></param>
	/// <param name="dwCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemChr(ptrBuff AS PTR,wChar AS DWORD,dwCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Fill a memory buffer with null characters.
	/// </summary>
	/// <param name="ptrBuff"></param>
	/// <param name="wCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemClear(ptrBuff AS PTR,wCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Compare bytes in two memory buffers.
	/// </summary>
	/// <param name="ptr1"></param>
	/// <param name="ptr2"></param>
	/// <param name="wCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemComp(ptr1 AS PTR,ptr2 AS PTR,wCount AS DWORD) AS INT
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// Copy one memory buffer to another.
	/// </summary>
	/// <param name="ptrDest"></param>
	/// <param name="ptrSrc"></param>
	/// <param name="wCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemCopy(ptrDest AS PTR,ptrSrc AS PTR,wCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Copy one memory buffer to another and fill any remaining spaces with blanks.
	/// </summary>
	/// <param name="ptrDest"></param>
	/// <param name="ptrSrc"></param>
	/// <param name="wCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemCopyString(ptrDest AS PTR,ptrSrc AS STRING,wCount AS DWORD) AS VOID
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Get a pointer to a matching double word in a memory buffer.
	/// </summary>
	/// <param name="ptrBuff"></param>
	/// <param name="dw"></param>
	/// <param name="dwCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemDWord(ptrBuff AS PTR,dw AS DWORD,dwCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="pFunction"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemEnum(pFunction AS PTR) AS LONG
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// Deallocate a specified memory buffer.
	/// </summary>
	/// <param name="ptrMem"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemFree(ptrMem AS PTR) AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// </summary>
	/// <param name="ptrMem"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemFreeBlk(ptrMem AS PTR) AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// </summary>
	/// <param name="pMem"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemGetHeader(pMem AS PTR) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Get a pointer to a matching integer in a memory buffer.
	/// </summary>
	/// <param name="ptrBuff"></param>
	/// <param name="i"></param>
	/// <param name="dwCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemInt(ptrBuff AS PTR,i AS INT,dwCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="ptrMem"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemLen(ptrMem AS PTR) AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// Get a pointer to a matching long integer in a memory buffer.
	/// </summary>
	/// <param name="ptrBuff"></param>
	/// <param name="li"></param>
	/// <param name="dwCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemLong(ptrBuff AS PTR,li AS LONG,dwCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Move one memory buffer to another. 
	/// </summary>
	/// <param name="ptrDest"></param>
	/// <param name="ptrSrc"></param>
	/// <param name="wCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemMove(ptrDest AS PTR,ptrSrc AS PTR,wCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="ptrMem"></param>
	/// <param name="cb"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemRealloc(ptrMem AS PTR,cb AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="ptrMem"></param>
	/// <param name="cb"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemReallocBlk(ptrMem AS PTR,cb AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Fill a memory buffer with a specified character.
	/// </summary>
	/// <param name="ptrBuff"></param>
	/// <param name="b"></param>
	/// <param name="wCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemSet(ptrBuff AS PTR,b AS BYTE,wCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Get a pointer to a matching short integer in a memory buffer.
	/// </summary>
	/// <param name="ptrBuff"></param>
	/// <param name="si"></param>
	/// <param name="dwCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemShort(ptrBuff AS PTR,si AS SHORT,dwCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="ptrBuff"></param>
	/// <param name="wCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemUpper(ptrBuff AS PTR,wCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Get a pointer to a matching word in a memory buffer.
	/// </summary>
	/// <param name="ptrBuff"></param>
	/// <param name="w"></param>
	/// <param name="dwCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION MemWord(ptrBuff AS PTR,w AS WORD,dwCount AS DWORD) AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="ptrBuff"></param>
	/// <param name="nLen"></param>
	/// <param name="nDec"></param>
	/// <returns>
	/// </returns>
	FUNCTION Psz2Float(ptrBuff AS PTR,nLen AS INT,nDec AS INT) AS FLOAT
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// Convert a PSZ to a USUAL with a PSZ tag.
	/// </summary>
	/// <param name="ptrSource"></param>
	/// <returns>
	/// </returns>
	FUNCTION Psz2Usual(ptrSource AS PTR) AS USUAL
		/// THROW NotImplementedException{}
	RETURN NIL   

	/// <summary>
	/// </summary>
	/// <param name="p"></param>
	/// <returns>
	/// </returns>
	FUNCTION Ptr2Bin(p AS PTR) AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// Determine the number of bytes that can be read at a given pointer.
	/// </summary>
	/// <param name="ptrP"></param>
	/// <returns>
	/// </returns>
	FUNCTION PtrLen(ptrP AS PTR) AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// Determine the number of bytes that can be written at a given pointer.
	/// </summary>
	/// <param name="ptrP"></param>
	/// <returns>
	/// </returns>
	FUNCTION PtrLenWrite(ptrP AS PTR) AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// </summary>
	/// <param name="pThis"></param>
	/// <param name="pSuper"></param>
	/// <param name="pSelf"></param>
	/// <param name="dwFuncCount"></param>
	/// <returns>
	/// </returns>
	FUNCTION RddInherit(pThis AS USUAL,pSuper AS USUAL,pSelf AS PTR,dwFuncCount AS DWORD) AS LONG
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// Register a dynamic object for update after a garbage collection.
	/// </summary>
	/// <param name="ptrKid"></param>
	/// <param name="dwCount"></param>
	/// <param name="lItem"></param>
	/// <returns>
	/// </returns>
	FUNCTION RegisterKid(ptrKid AS PTR,dwCount AS DWORD,lItem AS LOGIC) AS VOID
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Display a bitmap in a window or control.
	/// </summary>
	/// <param name="hWnd"></param>
	/// <param name="cFileName"></param>
	/// <param name="cTitle"></param>
	/// <returns>
	/// </returns>
	FUNCTION ShowBitmap(hWnd AS PTR,cFileName AS STRING,cTitle AS STRING) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// Display a bitmap stretched or shrunk to fit a window or control.
	/// </summary>
	/// <param name="hWnd"></param>
	/// <param name="cFileName"></param>
	/// <param name="cTitle"></param>
	/// <returns>
	/// </returns>
	FUNCTION StretchBitmap(hWnd AS PTR,cFileName AS STRING,cTitle AS STRING) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// </summary>
	/// <param name="pH"></param>
	/// <param name="dwCode"></param>
	/// <returns>
	/// </returns>
	FUNCTION TerminateVOThread(pH AS PTR,dwCode AS DWORD) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// Remove the registration of an object that was registered to the garbage collector.
	/// </summary>
	/// <param name="ptrKid"></param>
	/// <returns>
	/// </returns>
	FUNCTION UnRegisterKid(ptrKid AS PTR) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// </summary>
	/// <param name="pItem"></param>
	/// <param name="pvarg"></param>
	/// <returns>
	/// </returns>
	FUNCTION Usual2Variant(pItem AS PTR,pvarg AS PTR) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// </summary>
	/// <param name="pvarg"></param>
	/// <param name="pItem"></param>
	/// <returns>
	/// </returns>
	FUNCTION Variant2Usual(pvarg AS PTR,pItem AS PTR) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// Send the specified message to the specified window.
	/// </summary>
	/// <param name="hwnd"></param>
	/// <param name="nMsg"></param>
	/// <param name="dwParam"></param>
	/// <param name="lParam"></param>
	/// <returns>
	/// </returns>
	FUNCTION VOSendMessage(hwnd AS PTR,nMsg AS DWORD,dwParam AS DWORD,lParam AS LONG) AS LONG
		/// THROW NotImplementedException{}
	RETURN 0   

	#endregion
end namespace