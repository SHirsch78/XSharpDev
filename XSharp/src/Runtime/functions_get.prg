begin namespace XSharp.Runtime
	#region functions
	/// <summary>
	/// Returns a string representing the morning extension for time strings in 12-hour format.
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetAMExt() AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// Gets the locale ID that the runtime uses for comparing strings when running in Windows collation mode (SetCollation(#Windows)).
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetAppLocaleID() AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// </summary>
	/// <param name="pBuffer"></param>
	/// <param name="nSize"></param>
	/// <returns>
	/// </returns>
	FUNCTION GetCallStack(pBuffer AS PSZ,nSize AS INT) AS LOGIC
		/// THROW NotImplementedException{}
	RETURN FALSE   

	/// <summary>
	/// </summary>
	/// <param name="b1"></param>
	/// <param name="b2"></param>
	/// <param name="b3"></param>
	/// <param name="nPad"></param>
	/// <returns>
	/// </returns>
	FUNCTION GetChunkBase64(b1 AS BYTE,b2 AS BYTE,b3 AS BYTE,nPad AS INT) AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// Get the current <%APP%> search path for opening file.
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetCurPath() AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetDASPtr() AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// Return the current date format.
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetDateFormat() AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// Return the <%APP%> default drive and directory.
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetDefault() AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// Return the current SetDefaultDir() setting.
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetDefaultDir() AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// Return the DOS error code from any application.
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetDosError() AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// Retrieve the contents of a DOS environment variable.
	/// </summary>
	/// <param name="c"></param>
	/// <returns>
	/// </returns>
	FUNCTION GetEnv(c AS STRING) AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// Convert file attributes to numbers.
	/// </summary>
	/// <param name="uxFileAttr"></param>
	/// <returns>
	/// </returns>
	FUNCTION GetFAttr(uxFileAttr AS USUAL) AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// Prepare a file specification for wildcard searching.
	/// </summary>
	/// <param name="cFileMask"></param>
	/// <returns>
	/// </returns>
	FUNCTION GetFMask(cFileMask AS USUAL) AS PSZ
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <param name="c"></param>
	/// <returns>
	/// </returns>
	FUNCTION GetMimType(c AS STRING) AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// Get the current DLL for nation-dependent operations and messages.
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetNatDLL() AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// Returns a string representing the evening extension for time strings in 12-hour format.
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetPMExt() AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetPrivPtr() AS PTR
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetRTFullPath() AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// </summary>
	/// <param name="pStgRoot"></param>
	/// <param name="cSubStorage"></param>
	/// <returns>
	/// </returns>
	FUNCTION GetStgServer(pStgRoot AS PTR,cSubStorage AS STRING) AS STRING
		/// THROW NotImplementedException{}
	RETURN NULL_STRING   

	/// <summary>
	/// </summary>
	/// <param name="dwRes"></param>
	/// <returns>
	/// </returns>
	FUNCTION GetStringDXAX(dwRes AS DWORD) AS PSZ
		/// THROW NotImplementedException{}
	RETURN NULL   

	/// <summary>
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetThreadCount() AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// Get the number of 1/10000 seconds that have elapsed since Windows was started.
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetTickCountLow() AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// Return the current separation character used in time strings.
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetTimeSep() AS DWORD
		/// THROW NotImplementedException{}
	RETURN 0   

	/// <summary>
	/// </summary>
	/// <returns>
	/// </returns>
	FUNCTION GetTimeZoneDiff() AS INT
		/// THROW NotImplementedException{}
	RETURN 0   

	#endregion
end namespace