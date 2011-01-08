﻿Imports System.CodeDom.Compiler
Imports System.Reflection

'This will compile the FlowGraphSave files.
Module CompileFGS
    Private Draw As Boolean = True
    Private IncludeBaseObject As Boolean = False

    Private Source As String = "'Compiled using CompileFGS"
    ''' <summary>
    ''' Add some text to the source.
    ''' </summary>
    Private Sub sAdd(ByVal Text As String)
        Source &= Environment.NewLine & Text
    End Sub

#Region "Getting ready to compile"
    Public Sub CompileFGS(ByVal fgsFile As String)
        Environment.CurrentDirectory = IO.Path.GetDirectoryName(Windows.Forms.Application.ExecutablePath)

        sAdd("Imports Microsoft.VisualBasic")
        sAdd("Imports System")
        sAdd("Imports System.Drawing")
        sAdd("Imports System.Collections")
        sAdd("Imports System.Collections.Generic")
        sAdd("Imports System.Windows.Forms")
        sAdd("Imports System.Diagnostics")
        sAdd("Imports Plugins")
        'Create the namespace.
        sAdd("Namespace Plugins")

        Dim Files As New List(Of String)

        Dim sd As New SimpleD.SimpleD(fgsFile, True)
        Dim g As SimpleD.Group = sd.Get_Group("Main")
        Dim numObj As Integer = g.Get_Value("Objects")
        For i As Integer = 0 To numObj 'Loop thrugh each object.
            g = sd.Get_Group("object" & i)
            Dim name As String = g.Get_Value("name")
            Dim file As String = "Plugins\" & g.Get_Value("File")
            If file = "Plugins\" Then
                Console.WriteLine("Object " & name & " does not support compiling.")
                Return
            ElseIf Not IO.File.Exists(file) Then
                Console.WriteLine("Object " & name & " file(" & file & ") does NOT exist.")
                Return
            End If
            Files.Add(file)
        Next

        Log(" Done", False)

        Log("Checking files...")
        Dim NewFiles As New List(Of String)
        Dim StartI As Integer = 0

Restart:
        For i As Integer = StartI To Files.Count - 1
            Dim sr As New IO.StreamReader(Files(i))
            Dim FileSource As String = sr.ReadToEnd
            sr.Close()

            sAdd(FileSource)

            If FileSource.Contains("BaseObject") Then IncludeBaseObject = True

            'Find references.
            Dim ReferencesStart As Integer = -1
            Dim IncludeStart As Integer = -1
            Do
                ReferencesStart = FileSource.IndexOf("AddReferences(", ReferencesStart + 1) + 14
                If ReferencesStart > 13 Then
                    Dim EndIndex As Integer = FileSource.IndexOf(")", ReferencesStart)
                    Dim References() As String = Split(FileSource.Substring(ReferencesStart, EndIndex - ReferencesStart), ",")
                    vbReferences.AddRange(References)
                End If

                IncludeStart = FileSource.IndexOf("Include(", IncludeStart + 1) + 8
                If IncludeStart > 7 Then
                    Dim EndIndex As Integer = FileSource.IndexOf(")", IncludeStart)
                    Dim Include() As String = Split(FileSource.Substring(IncludeStart, EndIndex - IncludeStart), ",")
                    NewFiles.AddRange(Include)
                End If
            Loop Until ReferencesStart = 13 And IncludeStart = 7
        Next
        'If there are any new(included) files. Then we add them to the file list and check them.
        If NewFiles.Count > 0 Then
            StartI = Files.Count - 1
            For Each nFile As String In NewFiles
                nFile = "Plugins\" & nFile
                Dim Exists As Boolean = False
                For Each oFile As String In Files
                    If UCase(oFile) = UCase(nFile) Then
                        Exists = True
                        Exit For
                    End If
                Next
                If Not Exists Then Files.Add(nFile)
            Next
            NewFiles.Clear()
            GoTo Restart
        End If
        'If IncludeBaseObject And StartI = Files.Count - 2 Then
        '    StartI = Files.Count - 1
        '    Files.Add("Plugins\Base\BaseObject.vb")
        '    Files.Add("Plugins\Base\SimpleD.vb")
        '    GoTo Restart
        'End If




        Log("")
        Log("Draw=" & Draw & " BaseObject=" & IncludeBaseObject & " Files:")
        For Each f As String In Files
            Log(f)
        Next

        'End the namespace.
        sAdd("End Namespace")


        'Save the source to a file for debuging.
        Dim sw As New IO.StreamWriter("fgsSource.vb")
        sw.Write(Source)
        sw.Close()

        'Compile the source and get all of the errors.
        Dim Errors As CodeDom.Compiler.CompilerErrorCollection = CompileVbPlugins(fgsFile).Errors

        'Get the errors.
        If Errors.Count > 0 Then
            Dim tErrors = "Errors:", Warnings As String = "Warnings:"
            'Loop thru all of the errors.
            For Each tmp As CodeDom.Compiler.CompilerError In Errors
                'If the error is a warning then.
                If tmp.IsWarning Then
                    'Add the warning.
                    Warnings &= Environment.NewLine & tmp.ErrorText & "  At line:" & tmp.Line
                Else
                    'Othor wise it is a error then add the error.
                    tErrors &= Environment.NewLine & tmp.ErrorText & "  At line:" & tmp.Line
                End If
            Next
            'Show all of the errors/warnings.
            Log(Environment.NewLine & tErrors & Environment.NewLine & Warnings & Environment.NewLine)

            If tErrors = "Errors:" Then
                Log("Compleated with " & Errors.Count & " warnings.")
            Else
                Log("Could not compile there is: " & Errors.Count & " errors")
            End If
        Else
            Log(Environment.NewLine & "Successfully compiled.")
        End If
    End Sub
#End Region

#Region "The real compiling"
    Private vbReferences As New List(Of String)

    Private Function CompileVbPlugins(ByVal fgsFile As String) As CompilerResults
        Dim Params As New CompilerParameters()
        Dim Results As CompilerResults

        'Set the parameters.
        With Params
            .OutputAssembly = IO.Path.GetFileNameWithoutExtension(fgsFile) & ".exe"
            .GenerateExecutable = True
            .GenerateInMemory = False
            .MainClass = "frmMain"
            '.EmbeddedResources.Add()


#If DEBUG Then
            .IncludeDebugInformation = True
#Else
            .IncludeDebugInformation = false
#End If
            'Add the references.
            .ReferencedAssemblies.Add("System.dll")
            .ReferencedAssemblies.Add("System.Core.dll")
            If Draw Then .ReferencedAssemblies.Add("System.Drawing.dll")
            .ReferencedAssemblies.Add("System.Windows.Forms.dll")
            .ReferencedAssemblies.AddRange(vbReferences.ToArray)
        End With

        'Set the provider to VB.
        Dim Provider As CodeDomProvider = New Microsoft.VisualBasic.VBCodeProvider()

        'Compile the plugin.
        Results = Provider.CompileAssemblyFromFile(Params, { _
                                                   "..\Flowgraph\modMain.vb", _
                                                   "..\Flowgraph\frmMain.vb", "..\Flowgraph\frmMain.Designer.vb", "..\Flowgraph\frmMain.resx", _
                                                            "fgsSource.vb"})

        'Return the results.
        Return Results
    End Function
#End Region

End Module