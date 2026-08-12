Namespace CryptoCoin.Explorer

    ''' <summary>
    ''' Simple JSON serialization helpers (no external dependencies).
    ''' </summary>
    Public NotInheritable Class JsonSerializer

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Wraps a string value in quotes with escaping.
        ''' </summary>
        Public Shared Function QuoteString(value As String) As String
            If value Is Nothing Then Return "null"
            Dim escaped As String = value.Replace("\", "\\").Replace("""", "\""")
            Return $"""{escaped}"""
        End Function

        ''' <summary>
        ''' Creates a JSON object from key-value pairs.
        ''' </summary>
        Public Shared Function CreateObject(ParamArray pairs As String()) As String
            If pairs Is Nothing OrElse pairs.Length = 0 Then Return "{}"
            Return "{" & String.Join(",", pairs) & "}"
        End Function

        ''' <summary>
        ''' Creates a JSON array from items.
        ''' </summary>
        Public Shared Function CreateArray(items As List(Of String)) As String
            If items Is Nothing OrElse items.Count = 0 Then Return "[]"
            Return "[" & String.Join(",", items) & "]"
        End Function

        ''' <summary>
        ''' Creates a JSON property (key:value pair).
        ''' </summary>
        Public Shared Function Prop(key As String, value As String) As String
            Return $"""{key}"":{value}"
        End Function

        ''' <summary>
        ''' Creates a JSON property with a string value.
        ''' </summary>
        Public Shared Function PropStr(key As String, value As String) As String
            Return $"""{key}"":{QuoteString(value)}"
        End Function

        ''' <summary>
        ''' Creates a JSON property with an integer value.
        ''' </summary>
        Public Shared Function PropInt(key As String, value As Integer) As String
            Return $"""{key}"":{value}"
        End Function

        ''' <summary>
        ''' Creates a JSON property with a long value.
        ''' </summary>
        Public Shared Function PropLong(key As String, value As Long) As String
            Return $"""{key}"":{value}"
        End Function

        ''' <summary>
        ''' Creates a JSON property with a double value.
        ''' </summary>
        Public Shared Function PropDbl(key As String, value As Double) As String
            Return $"""{key}"":{value:F8}"
        End Function

        ''' <summary>
        ''' Creates a JSON property with a boolean value.
        ''' </summary>
        Public Shared Function PropBool(key As String, value As Boolean) As String
            Return $"""{key}"":{If(value, "true", "false")}"
        End Function

    End Class

End Namespace
