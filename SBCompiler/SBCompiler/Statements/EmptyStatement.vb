﻿Namespace Microsoft.SmallVisualBasic.Statements
    Public Class EmptyStatement
        Inherits Statement

        Public Overrides Function GetStatementAt(lineNumber As Integer) As Statement
            If lineNumber = StartToken.Line Then Return Me
            Return Nothing
        End Function

        Public Overrides Function ToString() As String
            Return vbCrLf
        End Function
    End Class
End Namespace
