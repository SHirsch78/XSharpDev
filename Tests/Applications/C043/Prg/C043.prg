// 43. error XS1620: Argument 2 must be passed with the 'out' keyword
// vulcan compiles both
FUNCTION Start() AS VOID
LOCAL r AS REAL8
System.Double.TryParse("1.2" , r) // this should probably be allowed only in the VO/Vulcan dialect
? r
System.Double.TryParse("1.3" , REF r)
? r
