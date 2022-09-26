﻿Imports System.Globalization
Imports Microsoft.SmallVisualBasic.Completion
Imports Microsoft.SmallVisualBasic.Expressions

Namespace Microsoft.SmallVisualBasic.Statements
    Public Class ReturnStatement
        Inherits Statement

        Public ReturnExpression As Expression
        Public Subroutine As SubroutineStatement

        Public Overrides Function GetStatementAt(lineNumber As Integer) As Statement
            If lineNumber < StartToken.Line Then Return Nothing
            If lineNumber <= ReturnExpression?.EndToken.Line Then Return Me
            Return Nothing
        End Function

        Public Overrides Sub AddSymbols(symbolTable As SymbolTable)
            MyBase.AddSymbols(symbolTable)
            If ReturnExpression Is Nothing Then
                If Subroutine?.SubToken.Type = TokenType.Function Then
                    symbolTable.Errors.Add(New [Error](StartToken, "Function must return a value"))
                End If
            Else
                ReturnExpression.Parent = Me
                ReturnExpression.AddSymbols(symbolTable)
            End If

            If Subroutine IsNot Nothing Then
                Subroutine.ReturnStatements.Add(Me)
            End If
        End Sub

        Public Overrides Sub EmitIL(scope As CodeGenScope)
            Dim code = ""

            If Subroutine.SubToken.Type = TokenType.Function Then
                code = $"Stack.PushValue(""_sVB_ReturnValues"", {If(ReturnExpression, ChrW(34) & ChrW(34))})" & vbCrLf
            End If

            code &= $"GoTo _EXIT_SUB_{Subroutine.Name.LCaseText}"

            CodeGenerator.LowerAndEmit(code, scope, Subroutine, StartToken.Line)
        End Sub

        Public Overrides Function ToString() As String
            Return $"{StartToken} {ReturnExpression}"
        End Function

        Public Overrides Sub PopulateCompletionItems(
                        bag As CompletionBag,
                        line As Integer,
                        column As Integer,
                        globalScope As Boolean
                   )

            If Me.StartToken.IsBefore(line, column) Then
                CompletionHelper.FillExpressionItems(bag)
                CompletionHelper.FillSubroutines(bag, True)
            End If
        End Sub

    End Class
End Namespace
