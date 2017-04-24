Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization

Public Class CompilationGroup
    Private Shared Serializer As New JavaScriptSerializer

    Public ReadOnly Property Name As String

    Private ReadOnly Paths As HashSet(Of String)
    Private ReadOnly Dependencies As HashSet(Of CompilationGroup)
    Private ProjectPath As String

    Public Sub New(Name As String)
        Me.Name = Name
        Me.Paths = New HashSet(Of String)
        Me.Dependencies = New HashSet(Of CompilationGroup)
    End Sub

    Public Sub Add(FilePath As String)
        Paths.Add(FilePath)
    End Sub

    Public Sub Add(Group As CompilationGroup)
        If Group.Dependencies.Contains(Me) Then
            Throw New Exception($"Circular dependency detected: {Me} is already a dependency of {Group}")
        End If
        Dependencies.Add(Group)
        For Each Dependency In Group.Dependencies
            Dependencies.Add(Dependency)
        Next
    End Sub

    Public Function DependsOn(group As CompilationGroup) As Boolean
        Return Me.Dependencies.Contains(group)
    End Function

    Public Function GetFullPaths()
        Dim FullPaths = Paths.Select(Function(s) Path.GetFullPath(s)).ToList()

        ' Get *.ts files used by dependencies
        For Each Group In Dependencies
            FullPaths.AddRange(Group.Paths.Select(Function(s) Path.GetFullPath(s)))
        Next

        Return FullPaths
    End Function

    Public Async Function Compile() As Task
        Dim TypeScriptCompilerPath = FindTypeScriptCompiler()

        If TypeScriptCompilerPath Is Nothing Then
            Throw New Exception("Cannot find tsc.exe")
        End If

        ' See if there is a tsconfig.json in the current directory
        Dim BaseConfig = Path.GetFullPath("tsconfig.json")
        If Not File.Exists(BaseConfig) Then
            BaseConfig = Nothing
        End If

        Dim ID = Guid.NewGuid().ToString()

        ' Set up a temporary folder for tsconfig.json
        Dim ProjectPath = Path.Combine(Path.GetTempPath(), "TSC-CustomCompilationGroupTool-" & ID)
        Directory.CreateDirectory(ProjectPath)

        ' Write a new tsconfig.json
        Dim ConfigurationFile = Path.Combine(ProjectPath, "tsconfig.json")
        File.WriteAllText(ConfigurationFile, Serializer.Serialize(New With {
            .extends = BaseConfig,
            .compilerOptions = New With {
                .sourceMap = True},
            .files = GetFullPaths()}))

        ' Run tsc
        Dim TSC = Process.Start(New ProcessStartInfo(TypeScriptCompilerPath) With {
            .RedirectStandardError = True,
            .RedirectStandardOutput = True,
            .UseShellExecute = False,
            .WorkingDirectory = ProjectPath
        })

        Dim PrintToOutputTask = PrintToConsole(Name, TSC.StandardOutput, Console.Out)
        Dim PrintToErrorTask = PrintToConsole(Name, TSC.StandardError, Console.Error)

        While Not TSC.HasExited
            Await Task.Delay(250)
        End While

        For DeletionAttempts = 0 To 4
            Try
                DeletionAttempts += 1
                Directory.Delete(ProjectPath, True)
                Exit For
            Catch ex As Exception When DeletionAttempts < 4
            End Try
            Await Task.Delay(500)
        Next

        Await PrintToOutputTask
        Await PrintToErrorTask

        If TSC.ExitCode <> 0 Then
            Dim Message As New StringBuilder()
            Message.AppendLine($"tsc could not compile group ""{Name}""")
            Throw New Exception(Message.ToString().Trim())
        End If
    End Function

    Private Shared Async Function PrintToConsole(Name As String, Reader As StreamReader, Writer As TextWriter) As Task
        Do
            Dim Line = Await Reader.ReadLineAsync()
            If Line Is Nothing Then
                Exit Do
            ElseIf Line <> "" Then
                Await Writer.WriteLineAsync($"{Name}: {Line}")
            End If
        Loop
    End Function

    Public Overrides Function ToString() As String
        Return Name
    End Function
End Class
