// warning XS0472: The result of the expression is always 'false' since a value of type 'int' is never equal to 'null' of type 'int?'
FUNCTION Start() AS VOID
    LOCAL i AS INT
    LOCAL d AS DateTime
    i := 0
    d := DateTime.Now
    ? i == NULL
    ? NULL == i
    ? d == NULL
    ? NULL == d
