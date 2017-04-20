Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization

Public Class CompilationGroup
    Private Shared Serializer As New JavaScriptSerializer

    Public ReadOnly Property Name As String

    Private ReadOnly Paths As HashSet(Of String)
    Private ReadOnly Dependencies As HashSet(Of CompilationGroup)

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

    Public Function Compile() As Task
        Return CompileInternal(Name, Paths, Dependencies)
    End Function

    Private Shared Async Function CompileInternal(Name As String,
                                                  Paths As IEnumerable(Of String),
                                                  Dependencies As IEnumerable(Of CompilationGroup)) As Task
        Dim FullPaths = Paths.Select(Function(s) Path.GetFullPath(s)).ToList()

        ' Get *.ts files used by dependencies
        For Each Group In Dependencies
            FullPaths.AddRange(Group.Paths.Select(Function(s) Path.GetFullPath(s)))
        Next

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
        Dim TSC = Process.Start(New ProcessStartInfo("C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.1/tsc") With {
            .WorkingDirectory = ProjectPath,
            .UseShellExecute = False
        })

        While Not TSC.HasExited
            Await Task.Delay(250)
        End While

        If TSC.ExitCode <> 0 Then
            Dim Message As New StringBuilder()
            Message.AppendLine($"tsc could not compile group ""{Name}""")
            Throw New Exception(Message.ToString())
        End If
    End Function

    Public Overrides Function ToString() As String
        Return Name
    End Function
End Class
