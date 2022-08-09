﻿Imports System.ComponentModel.Composition
Imports System.Windows.Input
Imports Microsoft.Nautilus.Core.Undo
Imports Microsoft.Nautilus.Text
Imports Microsoft.Nautilus.Text.Editor
Imports Microsoft.Nautilus.Text.Operations

Namespace Microsoft.SmallBasic.LanguageService
    Public Class CompletionKeyboardFilter
        Inherits KeyboardFilter

        <Import>
        Public Property EditorOperationsProvider As IEditorOperationsProvider
        <Import>
        Public Property UndoHistoryRegistry As IUndoHistoryRegistry



        Public Overrides Sub KeyDown(textView As IAvalonTextView, args As KeyEventArgs)
            Dim completionProvider As CompletionAdornmentProvider
            Dim completionSurface As CompletionAdornmentSurface = Nothing

            If textView.Properties.TryGetProperty(GetType(CompletionAdornmentSurface), completionSurface) AndAlso completionSurface.IsAdornmentVisible Then
                Dim completionListBox = completionSurface.CompletionListBox
                Dim __ = completionListBox.SelectedIndex

                Select Case args.Key
                    Case Key.LeftCtrl, Key.RightCtrl
                        completionSurface.FadeCompletionList()

                    Case Key.Escape
                        completionSurface.Adornment.Dismiss(force:=True)
                        textView.VisualElement.Focus()
                        args.Handled = True

                    Case Key.Up
                        completionListBox.MoveUp()
                        args.Handled = True

                    Case Key.Down
                        completionListBox.MoveDown()
                        args.Handled = True

                    Case Key.Tab, Key.Return
                        args.Handled = CommitItem(textView, completionSurface)

                    Case Key.Space, Key.OemPeriod, Key.Oem4
                        CommitConditionally(textView, completionSurface)
                End Select

            ElseIf textView.Properties.TryGetProperty(GetType(CompletionAdornmentProvider), completionProvider) AndAlso
                       args.Key = Key.Space AndAlso args.KeyboardDevice.Modifiers = ModifierKeys.Control Then
                completionProvider.ShowCompletionAdornment(textView.TextSnapshot, textView.Caret.Position.TextInsertionIndex)
                args.Handled = True
            End If
        End Sub

        Public Overrides Sub KeyUp(textView As IAvalonTextView, args As KeyEventArgs)
            Dim [property] As CompletionAdornmentSurface = Nothing

            If (args.Key = Key.LeftCtrl OrElse args.Key = Key.RightCtrl) AndAlso textView.Properties.TryGetProperty(GetType(CompletionAdornmentSurface), [property]) AndAlso [property].IsAdornmentVisible Then
                [property].UnfadeCompletionList()
            End If

            MyBase.KeyUp(textView, args)
        End Sub

        Public Overrides Sub TextInput(textView As IAvalonTextView, args As TextCompositionEventArgs)
            Dim completionSurface As CompletionAdornmentSurface = Nothing

            If textView.Properties.TryGetProperty(GetType(CompletionAdornmentSurface), completionSurface) AndAlso completionSurface.IsAdornmentVisible Then
                Select Case args.Text
                    Case "+", "-", "*", "/", "="
                        args.Handled = CommitConditionally(textView, completionSurface, " " & args.Text & " ")

                    Case "!"
                        CommitConditionally(textView, completionSurface)

                    Case ","
                        CommitConditionally(textView, completionSurface, ", ")
                        args.Handled = True

                    Case "("
                        CommitConditionally(textView, completionSurface)
                End Select
            End If

            MyBase.TextInputUpdate(textView, args)
        End Sub

        Private Function CommitConditionally(textView As IAvalonTextView, adornmentSurface As CompletionAdornmentSurface, Optional extraText As String = "") As Boolean
            If adornmentSurface.CompletionListBox.SelectedItem IsNot Nothing Then
                Dim editorOperations = EditorOperationsProvider.GetEditorOperations(textView)
                Dim name = CType(adornmentSurface.CompletionListBox.SelectedItem, CompletionItemWrapper).Name
                Dim replaceSpan As Span = adornmentSurface.Adornment.ReplaceSpan.GetSpan(textView.TextSnapshot)
                editorOperations.ReplaceText(
                    replaceSpan,
                    name & extraText,
                    UndoHistoryRegistry.GetHistory(textView.TextBuffer))

                CommitConditionally = True
            End If

            If adornmentSurface.Adornment IsNot Nothing Then
                adornmentSurface.Adornment.Dismiss(force:=False)
            End If

            textView.VisualElement.Focus()
        End Function

        Private Function CommitItem(textView As IAvalonTextView, adornmentSurface As CompletionAdornmentSurface) As Boolean
            Dim result = False
            Dim completionItemWrapper As CompletionItemWrapper = TryCast(adornmentSurface.CompletionListBox.SelectedItem, CompletionItemWrapper)
            Dim [property] As CompletionAdornmentProvider = Nothing

            If completionItemWrapper IsNot Nothing Then
                If textView.Properties.TryGetProperty(GetType(CompletionAdornmentProvider), [property]) Then
                    [property].CommitItem(completionItemWrapper.CompletionItem)
                End If

                result = True
            ElseIf adornmentSurface.Adornment IsNot Nothing Then
                adornmentSurface.Adornment.Dismiss(force:=False)
            End If

            Return result
        End Function
    End Class
End Namespace
