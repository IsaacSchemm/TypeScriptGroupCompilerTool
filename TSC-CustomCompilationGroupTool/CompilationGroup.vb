Imports System.IO
Imports System.Text
Imports System.Web
Imports System.Web.Script.Serialization
Imports RunProcessAsTask

Public Class CompilationGroup
    Private Shared Serializer As New JavaScriptSerializer

    Public ReadOnly Property Name As String

    Private ReadOnly Paths As HashSet(Of String)
    Private ReadOnly Dependencies As HashSet(Of CompilationGroup)

    Private CompilationTask As Task

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

    Public Function Compile() As Task(Of IEnumerable(Of String))
        SyncLock Me
            If CompilationTask Is Nothing Then
                CompilationTask = Me.CompileInternal()
            End If
            Return CompilationTask
        End SyncLock
    End Function

    Private Async Function CompileInternal() As Task(Of IEnumerable(Of String))
        Dim FullPaths = Paths.Select(Function(s) Path.GetFullPath(s)).ToList()

        ' Get *.ts files used by dependencies
        Dim CompilationTasks As New List(Of Task(Of IEnumerable(Of String)))
        For Each Group In Dependencies
            CompilationTasks.Add(Group.Compile())
        Next
        For Each Task In CompilationTasks
            FullPaths.AddRange(Await Task)
        Next

        Dim dt = DateTime.UtcNow
        If Paths.Any Then
            ' See if there is a tsconfig.json in the current directory
            Dim BaseConfig = Path.GetFullPath("tsconfig.json")
            If Not File.Exists(BaseConfig) Then
                BaseConfig = Nothing
            End If

            ' Write a new tsconfig.json
            Dim ProjectPath = Path.Combine(Path.GetTempPath(), "TSC-CustomCompilationGroupTool-" & Guid.NewGuid().ToString())
            Directory.CreateDirectory(ProjectPath)
            Dim ConfigurationFile = Path.Combine(ProjectPath, "tsconfig.json")
            File.WriteAllText(ConfigurationFile, Serializer.Serialize(New With {
                .extends = BaseConfig,
                .compilerOptions = New With {
                    .sourceMap = True},
                .files = FullPaths}))

            ' Run tsc
            Dim Results = Await ProcessEx.RunAsync(New ProcessStartInfo("C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.1/tsc") With {
                .WorkingDirectory = ProjectPath
            })

            ' Print output of tsc to console
            For Each Line In Results.StandardOutput
                Console.WriteLine(Line)
            Next
            For Each Line In Results.StandardError
                Console.Error.WriteLine(Line)
            Next

            If Results.ExitCode <> 0 Then
                Throw New Exception($"The TypeScript compiler was unable to compile {Name} successfully.")
            End If
        End If

        Console.WriteLine(Name & ": " & (DateTime.UtcNow - dt).TotalSeconds)
        Return FullPaths
    End Function

    Public Overrides Function ToString() As String
        Return Name
    End Function
End Class
