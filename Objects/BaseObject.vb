﻿#Region "License & Contact"
'License:
'   Copyright (c) 2010 Raymond Ellis
'   
'   This software is provided 'as-is', without any express or implied
'   warranty. In no event will the authors be held liable for any damages
'   arising from the use of this software.
'
'   Permission is granted to anyone to use this software for any purpose,
'   including commercial applications, and to alter it and redistribute it
'   freely, subject to the following restrictions:
'
'       1. The origin of this software must not be misrepresented; you must not
'           claim that you wrote the original software. If you use this software
'           in a product, an acknowledgment in the product documentation would be
'           appreciated but is not required.
'
'       2. Altered source versions must be plainly marked as such, and must not be
'           misrepresented as being the original software.
'
'       3. This notice may not be removed or altered from any source
'           distribution.
'
'
'Contact:
'   Raymond Ellis
'   Email: RaymondEllis@live.com
#End Region

Public MustInherit Class BaseObject
    Public Index As Integer = -1

    Private Name As String = "NoName"

    'Output
    Public Output() As DataFlowBase

    'Input
    Public Input() As DataFlowBase


    Public Rect As Rectangle

    Public Title As String = "Title not set"
    Private TitleRect As RectangleF
    Public TitleBar As Rectangle

    Private BackGround As Rectangle


#Region "Setup & Distroy"
    ''' <summary>
    ''' Create rectangles. using the position and size.
    ''' </summary>
    Protected Sub Setup(ByVal ClassName As String, ByVal Position As Point, ByVal Width As Integer, ByVal Height As Integer)
        Name = ClassName
        'Create the main rectangle.
        Rect = New Rectangle(Position, New Size(Width, Height))

        'Set the size of the title.  Used to drag the object around.
        TitleBar = New Rectangle(Rect.Location, New Size(Rect.Width, 15))

        BackGround = New Rectangle(Rect.X, Rect.Y + 15, Rect.Width, Rect.Height - 15)

        Index = Objects.Count


        Menu(0).Setup("Remove", 50)
    End Sub

    Public Overridable Sub Distroy()
        If Output IsNot Nothing Then
            For n As Integer = 0 To Output.Length - 1

                Output(n).Disconnect()

                'If Output(n).obj1 > -1 Then
                '    Objects(Output(n).obj1).Input(Output(n).Index1).obj1 = -1
                '    Objects(Output(n).obj1).Input(Output(n).Index1).index1 = -1
                '    Objects(Output(n).obj1).Input(Output(n).Index1).Connected -= 1
                '    Output(n).obj1 = -1
                '    Output(n).Index1 = -1
                'End If

            Next
        End If

        If Input IsNot Nothing Then
            For n As Integer = 0 To Input.Length - 1
                Input(n).Disconnect()
                'If Input(n).Connected > 0 Then
                '    DisconnectInput(Input(n))
                'End If
            Next
        End If
    End Sub
#End Region

#Region "Load & Save"
    Public Overridable Function Load(ByVal g As SimpleD.Group) As SimpleD.Group

        Dim tmp As String = ""


        If Output IsNot Nothing Then 'If there is output then save Output=(obj1),(index1),(obj1),etc.. for each output
            g.Get_Value("Output", tmp)
            Dim tmpS As String() = Split(tmp, "`")
            For n As Integer = 0 To Output.Length - 1
                'Output(n).SetValues(tmpS(n * 2), tmpS((n * 2) + 1))
                Output(n).Load(Split(tmpS(n), ","))
            Next
        End If

        If Input IsNot Nothing Then 'Same as output^^^ but for inputs...
            g.Get_Value("Input", tmp)
            Dim tmpS As String() = Split(tmp, ",")
            For n As Integer = 0 To Input.Length - 1
                Input(n).Connected = tmpS(n)
            Next
        End If

        Return g
    End Function

    Public Overridable Function Save() As SimpleD.Group
        Dim g As New SimpleD.Group("Object" & Index)
        Dim tmp As String = ""

        g.Add("Name", Name)
        g.Add("Position", Rect.X & "," & Rect.Y)

        If Output IsNot Nothing Then 'If there is output then save Output=(obj1),(index1),(obj1),etc.. for each output
            tmp = Output(0).Save
            For n As Integer = 1 To Output.Length - 1
                tmp &= "`" & Output(n).Save
            Next
            g.Add("Output", tmp)
        End If

        If Input IsNot Nothing Then 'Same as output^^^ but for inputs...
            tmp = Input(0).Connected
            For n As Integer = 1 To Input.Length - 1
                tmp &= "," & Input(n).Connected
            Next
            g.Add("Input", tmp)
        End If

        Return g
    End Function

#End Region

#Region "Draw"

    Public Overridable Sub Draw(ByVal g As Graphics)
        'Draw the title and the background. Then we draw teh border so it is on top.
        g.FillRectangle(SystemBrushes.GradientActiveCaption, TitleBar)

        g.FillRectangle(SystemBrushes.Control, BackGround)
        g.DrawRectangle(SystemPens.WindowFrame, Rect)

        'Draw the inputs. (if any.)
        If Input IsNot Nothing Then
            For n As Integer = 1 To Input.Length
                'g.FillRectangle(Brushes.Red, Rect.X + 1, Rect.Y + 16 * n, 15, 15)
                g.FillEllipse(Brushes.Red, Rect.X + 1, Rect.Y + 15 * n, 15, 15)
            Next
        End If
        'Draw the outputs. (if any.)
        If Output IsNot Nothing Then
            For n As Integer = 1 To Output.Length
                'g.FillRectangle(Brushes.Green, Rect.Right - 15, Rect.Y + 16 * n, 15, 15)
                g.FillEllipse(Brushes.Green, Rect.Right - 15, Rect.Y + 15 * n, 15, 15)
            Next
        End If



        'If title rect is empty then we will set the position and size of the title string.
        If TitleRect.IsEmpty Then
            TitleRect.Size = g.MeasureString(Title, SystemFonts.DefaultFont)
            TitleRect.Location = New PointF(Rect.X + Rect.Width * 0.5 - TitleRect.Width * 0.5, Rect.Y + 1)
        End If
        g.DrawString(Title, SystemFonts.DefaultFont, SystemBrushes.ActiveCaptionText, TitleRect) 'Draw the title string.




    End Sub

    ''' <summary>
    ''' Draw lines connecting outputs to inputs.
    ''' </summary>
    Public Sub DrawConnectors(ByVal g As Graphics)
        If Output Is Nothing Then Return


        For n As Integer = 0 To Output.Length - 1
            If Output(n).IsNotEmpty Then
                For Each fd As DataFlow In Output(n).Flow
                    g.DrawLine(ConnectorPen, GetOutputPosition(n), Objects(fd.obj).GetInputPosition(fd.Index))
                Next
                'g.DrawLine(ConnectorPen, GetOutputPosition(n), Objects(Output(n).obj1).GetInputPosition(Output(n).Index1))

            End If
        Next
    End Sub

#End Region


    ''' <summary>
    ''' Is called when the object is moving.
    ''' </summary>
    Public Overridable Sub Moved()


    End Sub

    Public Overridable Sub DoubleClicked()
    End Sub

    Public Sub SetPosition(ByVal x As Integer, ByVal y As Integer)
        Rect.Location = New Point(Math.Round(x / GridSize) * GridSize, Math.Round(y / GridSize) * GridSize)

        'Update the title position.
        TitleRect.Location = New PointF(Rect.X + Rect.Width * 0.5 - TitleRect.Width * 0.5, Rect.Y + 1)
        TitleBar.Location = Rect.Location
        BackGround.Location = New Point(Rect.X, Rect.Y + 15)

        Moved()
    End Sub

#Region "Send & Receive"
    Public Sub Send(ByVal Data As Object, ByVal ID As Integer)
        If Output Is Nothing Then Return

        'If Output(ID).IsNotEmpty Then Objects(Output(ID).obj1).Receive(Data, Output(ID))
        Output(ID).Send(Data)
    End Sub
    Public Sub Send(ByVal Data As Object)
        If Output Is Nothing Then Return

        For Each obj As DataFlowBase In Output
            'If obj.IsNotEmpty Then Objects(obj.obj1).Receive(Data, obj)
            obj.Send(Data)
        Next
    End Sub

    Public Overridable Sub Receive(ByVal Data As Object, ByVal sender As DataFlow)
    End Sub
#End Region

#Region "Inputs & Outputs"

    Protected Sub Inputs(ByVal Names As String())
        'InputNames = Names
        ReDim Input(Names.Length - 1)
        For n As Integer = 0 To Names.Length - 1
            Input(n) = New DataFlowBase(Index, n, Names(n))

        Next
    End Sub
    Protected Sub Outputs(ByVal Names As String())
        ReDim Output(Names.Length - 1)
        For n As Integer = 0 To Names.Length - 1
            Output(n) = New DataFlowBase(Index, n, Names(n), True)
        Next
    End Sub

    Public Intersection As Integer
    Public Function IntersectsWithInput(ByVal rect As Rectangle) As Boolean
        If Input Is Nothing Then Return False

        For n As Integer = 1 To Input.Length
            Dim r As New Rectangle(Me.Rect.X, Me.Rect.Y + 16 * n, 16, 15)
            If rect.IntersectsWith(r) Then
                Intersection = n - 1
                Return True
            End If
        Next

        Return False
    End Function
    Public Function IntersectsWithOutput(ByVal rect As Rectangle) As Boolean
        If Output Is Nothing Then Return False

        For n As Integer = 1 To Output.Length
            Dim r As New Rectangle(Me.Rect.Right - 15, Me.Rect.Y + 16 * n, 16, 15)
            If rect.IntersectsWith(r) Then
                Intersection = n - 1
                Return True
            End If
        Next

        Return False
    End Function

    Public Function GetInputPosition(ByVal ID As Integer) As PointF
        Return New PointF(Rect.X + 7.5, Rect.Y + (15 * (ID + 1)) + 7.5)
    End Function
    Public Function GetOutputPosition(ByVal ID As Integer) As PointF
        Return New PointF(Rect.Right - 7.5, Rect.Y + (15 * (ID + 1)) + 7.5)
    End Function

#End Region


#Region "Mouse & Menu"
    Private Menu(0) As MenuNode

    Public Overridable Sub MenuSelected(ByVal Result As MenuNode)


        Select Case LCase(Result.Name)
            Case "remove"
                RemoveAt(Index)

        End Select
    End Sub

    Public Overridable Sub MouseMove(ByVal e As MouseEventArgs)

    End Sub
    Public Overridable Sub MouseDown(ByVal e As MouseEventArgs)

    End Sub
    Public Overridable Sub MouseUp(ByVal e As MouseEventArgs)
        If e.Button = MouseButtons.Right Then

            Menu_Open(Index, Menu)

        End If
    End Sub

#End Region


    Public Overrides Function ToString() As String
        Return Title
    End Function

End Class

Public Class DataFlowBase

    'Base object and the output/input index.
    Public obj As Integer = -1
    Public Index As Integer = -1
    Public Name As String = "DataFlowBase"

    'We do not create new because Inputs do not use it.
    Friend Flow As List(Of DataFlow)

    Public MaxConnected As Integer = -1
    Public Connected As Integer = 0

    Public Sub New(ByVal obj As Integer, ByVal Index As Integer, ByVal Name As String, Optional ByVal IsOutput As Boolean = False)
        Me.obj = obj
        Me.Index = Index
        Me.Name = Name
        If IsOutput Then Flow = New List(Of DataFlow)
    End Sub

#Region "Add & Disconnect"
    Public Function Add(ByVal obj1 As Integer, ByVal Index1 As Integer) As Boolean
        If Flow Is Nothing Then Return False

        If Connected = MaxConnected Then Return False

        For Each df As DataFlow In Flow
            If df.obj = obj1 And df.Index = Index1 Then
                Return False
            End If
        Next

        Flow.Add(New DataFlow(obj1, Index1, Me))

        Connected += 1

        Return True
    End Function

    Public Sub Disconnect()
        If Flow Is Nothing Then
            'Disconnect input.

            For Each obj As Object In Objects
                If obj.Output IsNot Nothing Then
                    For Each out As DataFlowBase In obj.Output
                        'If Flow Is Nothing Then Continue For

                        Dim n As Integer = 0
                        Do
                            If n > out.Flow.Count - 1 Then Exit Do
                            If out.Flow(n).obj = Me.obj And out.Flow(n).Index = Index Then
                                out.Flow(n) = Nothing
                                out.Flow.RemoveAt(n)
                                out.Connected -= 1
                                Connected -= 1
                            Else
                                n += 1
                            End If
                        Loop Until n = out.Flow.Count


                    Next
                End If
            Next


        Else 'Disconnect output.

            For Each fd As DataFlow In Flow

                Objects(fd.obj).Input(fd.Index).Connected -= 1

            Next

            Flow.Clear()

        End If

        'Connected = 0

    End Sub
#End Region

#Region "Send"
    Public Function Send(ByVal Data As Object, ByVal subIndex As Integer) As Boolean
        If Flow Is Nothing Then Return False

        If Flow.Count > subIndex Then Return False

        Objects(Flow(subIndex).obj).Receive(Data, Flow(subIndex))

        Return True
    End Function
    Public Function Send(ByVal Data As Object) As Boolean
        If Flow Is Nothing Then Return False

        For Each fd As DataFlow In Flow
            Objects(fd.obj).Receive(Data, fd)
        Next

        Return True
    End Function
#End Region

    Public Sub Load(ByVal data() As String)
        'Connected = data(0)

        If Flow Is Nothing Then Return
        For i As Integer = 1 To data.Length - 1 Step 2
            Add(data(i), data(i + 1))
        Next
        If Connected <> data(0) Then
            Throw New Exception("Error loading")
        End If
    End Sub
    Public Function Save() As String
        Dim data As String = Connected

        If Flow Is Nothing Then Return data
        For i As Integer = 0 To Flow.Count - 1
            data &= "," & Flow(i).obj & "," & Flow(i).Index
        Next
        Return data
    End Function

    Public Function IsEmpty() As Boolean
        If Flow Is Nothing Then Return True

        If Flow.Count = 0 Then Return True

        Return False
    End Function
    Public Function IsNotEmpty() As Boolean
        Return Not IsEmpty()
    End Function

    Shared Operator =(ByVal left As DataFlowBase, ByVal right As DataFlowBase) As Boolean
        If left Is Nothing Or right Is Nothing Then Return False

        If right.obj <> left.obj Or right.Index <> left.Index Or right.Connected <> left.Connected Then Return False

        Return True
    End Operator
    Shared Operator <>(ByVal left As DataFlowBase, ByVal right As DataFlowBase) As Boolean
        Return Not left = right
    End Operator
End Class

Public Structure DataFlow

    Public obj, Index As Integer

    Public Base As DataFlowBase

    Public Sub New(ByVal obj As Integer, ByVal Index As Integer, ByVal Base As DataFlowBase)
        Me.obj = obj
        Me.Index = Index
        Me.Base = Base
    End Sub

    Public Sub AddToObj(ByVal value As Integer)
        obj += value
    End Sub

    Shared Operator =(ByVal left As DataFlow, ByVal right As DataFlow) As Boolean
        If right.obj <> left.obj Or right.Index <> left.Index Then Return False

        Return True
    End Operator
    Shared Operator <>(ByVal left As DataFlow, ByVal right As DataFlow) As Boolean
        Return Not left = right
    End Operator
End Structure