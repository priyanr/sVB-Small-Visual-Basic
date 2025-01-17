﻿Imports System.Windows.Markup
Imports Microsoft.SmallVisualBasic.Library
Imports Microsoft.SmallVisualBasic.Library.Internal
Imports Wpf = System.Windows.Controls
Imports ControlsDictionay = System.Collections.Generic.Dictionary(Of String, System.Windows.FrameworkElement)
Imports System.Windows.Controls
Imports System.Windows
Imports System.ComponentModel
Imports App = Microsoft.SmallVisualBasic.Library.Internal.SmallBasicApplication

Namespace WinForms
    <SmallVisualBasicType>
    Public NotInheritable Class Forms

        Friend Shared _forms As New ControlsDictionay
        Friend Shared _controls As New ControlsDictionay


        Friend Shared Function GetForm(name As String) As Window
            name = name.ToLower()
            Dim wnd As Window
            Dim dictionary = If(name.Contains("."), _controls, _forms)

            If Not dictionary.ContainsKey(name) Then Return Nothing
            wnd = dictionary(name)

            If wnd Is Nothing Then
                ReportError($"`{name}` is not a form or it is closed", Nothing)
            End If

            Return wnd
        End Function

        ''' <summary>
        ''' Returns an array containing the names of all forms you created.
        ''' </summary>
        ''' <returns></returns>
        <ReturnValueType(VariableType.Array)>
        Public Shared Function GetForms() As Primitive
            Dim map = New Dictionary(Of Primitive, Primitive)
            Dim num = 1
            For Each key In _forms.Keys
                map(num) = key
                num += 1
            Next
            Return Primitive.ConvertFromMap(map)
        End Function

        <ReturnValueType(VariableType.String)>
        Public Shared Property AppPath As Primitive

        Private Shared _syncLock As New Object

        ''' <summary>
        ''' Loads a form from its xaml file.
        ''' </summary>
        ''' <param name="formName">the name of the form</param>
        ''' <param name="xamlPath">the path pf the xaml file that contains the form design</param>
        ''' <returns>The name of the form</returns>
        <ReturnValueType(VariableType.Form)>
        Public Shared Function LoadForm(formName As Primitive, xamlPath As Primitive) As Primitive
            Dim form_Name = CStr(formName).ToLower()
            If form_Name = "" Then
                Dim ex As New ArgumentException("Form name can't be an empty string.")
                ReportError(ex.Message, ex)
            End If

            If _forms.ContainsKey(form_Name) Then Return form_Name

            SyncLock _syncLock
                SmallBasicApplication.Invoke(
                    Sub()
                        Try
                            Dim canvas As Canvas
                            Dim xaml = CStr(xamlPath)

                            If xaml = "" Then
                                canvas = LoadContent(form_Name & ".xaml")
                            ElseIf xaml.StartsWith("<") Then
                                canvas = XamlReader.Load(Xml.XmlReader.Create(New IO.StringReader(xaml)))
                            Else
                                canvas = LoadContent(xamlPath)
                            End If

                            Dim wnd As New Window() With {
                                   .SizeToContent = SizeToContent.WidthAndHeight,
                                   .WindowStartupLocation = WindowStartupLocation.CenterScreen,
                                   .Name = form_Name,
                                   .Title = Automation.AutomationProperties.GetHelpText(canvas),
                                   .Content = canvas,
                                   .ResizeMode = ResizeMode.CanMinimize
                            }

                            AddHandler wnd.Closed, AddressOf Form_Closed

                            _forms(form_Name) = wnd

                            ' Add control names:
                            Dim controls = canvas.GetChildren().ToList()
                            For Each ui In controls
                                Dim fw = TryCast(ui, FrameworkElement)
                                Dim controlName = Automation.AutomationProperties.GetName(ui)

                                If controlName = "" Then
                                    controlName = fw.Name
                                    If controlName = "" Then Continue For
                                End If

                                If TypeOf ui Is Wpf.Control Then
                                    _controls(form_Name & "." & controlName.ToLower()) = ui
                                    CType(ui, Wpf.Control).Name = controlName
                                Else
                                    Dim left = Canvas.GetLeft(ui)
                                    Dim top = Canvas.GetTop(ui)
                                    canvas.Children.Remove(ui)
                                    Dim lb As New Wpf.Label() With {
                                            .Name = controlName,
                                            .Width = fw.Width,
                                            .Height = fw.Height,
                                            .HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                            .VerticalContentAlignment = VerticalAlignment.Stretch,
                                            .Content = ui,
                                            .Background = Nothing
                                    }
                                    fw.ClearValue(FrameworkElement.WidthProperty)
                                    fw.ClearValue(FrameworkElement.HeightProperty)

                                    canvas.Children.Add(lb)
                                    Canvas.SetLeft(lb, left)
                                    Canvas.SetTop(lb, top)
                                    _controls(form_Name & "." & controlName.ToLower()) = lb
                                End If

                                SetControlText(ui, GetControlText(ui))
                            Next

                        Catch ex As Exception
                            ReportError("LoadForm caused an error: " & ex.Message, ex)
                        End Try
                    End Sub)

            End SyncLock

            ' Ensure Keyboard Module is loaded, 
            ' to create a global hanler for the PreviewKeyDown event
            Dim __ = Keyboard.LastKey

            Return form_Name
        End Function

        Private Shared Sub SetControlText(control As UIElement, value As String)
            Try
                CObj(control).Text = value
            Catch
                Try
                    Dim x = CObj(control).Content
                    If x Is Nothing OrElse TypeOf x Is String Then
                        CObj(control).Content = value
                    End If
                Catch
                    ' TODO: Add a label to hold the text
                End Try

            End Try
        End Sub

        Private Shared Function GetControlText(control As UIElement) As String
            Try
                Return CObj(control).Text
            Catch
                Try
                    Dim x = CObj(control).Content
                    If x IsNot Nothing AndAlso TypeOf x Is String Then
                        Return CStr(CObj(control).Content)
                    End If
                Catch
                End Try
            End Try

            Return Automation.AutomationProperties.GetHelpText(control)
        End Function

        ''' <summary>
        ''' Creats a new form with the given name, and adds it to the forms collection.
        ''' </summary>
        ''' <param name="formName">the name of the form</param>
        ''' <returns>the name of the form</returns>
        <ReturnValueType(VariableType.Form)>
        Public Shared Function AddForm(formName As Primitive) As Primitive
            Try
                Dim frm = GetForm(formName)
                If frm Is Nothing Then
                    Dim xaml As String = $"<Canvas Name=""{formName}"" Width=""700"" Height=""500"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""/>"
                    LoadForm(formName, xaml)
                End If

            Catch ex As Exception
                ReportError("AddForm cuased this error: " & ex.Message, ex)
            End Try
            Return formName
        End Function

        Private Shared Sub Form_Closed(sender As Object, e As EventArgs)
            Dim win = CType(sender, Window)
            Dim formName = win.Name
            _forms.Remove(formName)
        End Sub

        Private Shared Function LoadContent(xamlPath As String) As Canvas
            If IO.Path.GetPathRoot(xamlPath) = "" Then
                If AppPath.ToString() <> "" Then
                    xamlPath = IO.Path.Combine(AppPath, xamlPath)
                Else
                    Dim d = Environment.GetCommandLineArgs(0)
                    d = IO.Path.GetDirectoryName(d)
                    Dim xamlPath2 = IO.Path.Combine(d, xamlPath)
                    If IO.File.Exists(xamlPath2) Then xamlPath = xamlPath2
                End If
            End If

            Return GetCanvas(xamlPath)
        End Function

        Private Shared Function GetCanvas(xamlPath As String) As Canvas
            Dim xaml = IO.File.ReadAllText(xamlPath, System.Text.Encoding.UTF8)
            If xaml.Contains("wpf/xaml/WpfDialogs") Then
                xaml = "<Canvas " & "xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"" mc:Ignorable=""c""" & xaml.Substring(7)
            End If

            Try
                Dim stream = New IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(xaml))
                Dim canvas = CType(XamlReader.Load(stream), Canvas)
                Return canvas
            Catch
            End Try

            Return Nothing
        End Function


        ''' <summary>
        ''' Shows a message box dialog.
        ''' </summary>
        ''' <param name="message">the text to dislpay on the message box</param>
        ''' <param name="title">the title to display of the dialog box</param>
        Public Shared Sub ShowMessage(message As Primitive, title As Primitive)
            Try
                SmallBasicApplication.Invoke(Sub() System.Windows.MessageBox.Show(message.ToString(), title.ToString()))
            Catch ex As Exception
                ReportError("ShowMessage caused this error: " & ex.Message, ex)
            End Try
        End Sub

        ''' <summary>
        ''' Shows the form that has the given name if it exists.
        ''' </summary>
        ''' <param name="formName">the name of the form.</param>
        ''' <param name="argsArr">any additional data, array, or a dynamic object you want to pass to the form. It will be stored in the ArgsArr property of the form, so you can use it as you want</param>
        ''' <returns>the form name</returns>
        <ReturnValueType(VariableType.Form)>
        Public Shared Function ShowForm(formName As Primitive, argsArr As Primitive) As Primitive
            Dim asm = System.Reflection.Assembly.GetCallingAssembly()
            App.Invoke(
                  Sub()
                      Try
                          If Form.GetIsLoaded(formName) Then
                              Form.SetArgsArr(formName, argsArr)
                              Form.Show(formName)
                              Dim wind = GetForm(formName)
                              wind.RaiseEvent(New RoutedEventArgs(Form.OnFormShownEvent))

                          Else
                              Stack.PushValue("_" & formName.AsString().ToLower() & "_argsArr", argsArr)
                              Form.Initialize(formName, asm)
                          End If

                      Catch ex As Exception
                          Form.ReportSubError(formName, "ShowForm", ex)
                      End Try
                  End Sub)

            Return formName
        End Function

        ''' <summary>
        ''' Shows the form that has the given name if it exists.
        ''' </summary>
        ''' <param name="formName">the name of the form.</param>
        ''' <param name="argsArr">any additional data, array, or a dynamic object you want to pass to the form. It will be stored in the Form.ArgsArr property, so you can use it as you want</param>
        ''' <returns>the dialog result that represnts the type of the button that user clicked, like OK, Yes, No, ... etc.</returns>
        <ReturnValueType(VariableType.DialogResult)>
        Public Shared Function ShowDialog(formName As Primitive, argsArr As Primitive) As Primitive
            Dim asm = System.Reflection.Assembly.GetCallingAssembly()
            App.Invoke(
                Sub()
                    Try
                        If Form.GetIsLoaded(formName) Then
                            Form.SetArgsArr(formName, argsArr)
                            ShowDialog = Form.ShowDialog(formName)
                        Else
                            Stack.PushValue("_" & formName.AsString().ToLower() & "_argsArr", argsArr)
                            Form.Initialize(formName, asm)
                            Control.SetVisible(formName, False)
                            ShowDialog = Form.ShowDialog(formName)
                        End If
                    Catch ex As Exception
                        Form.ReportSubError(formName, "ShowForm", ex)
                    End Try
                End Sub)
        End Function
    End Class
End Namespace