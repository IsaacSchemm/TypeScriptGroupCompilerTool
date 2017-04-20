Public Class CompilationGroup
    Public ReadOnly Property Name As String

    Private ReadOnly Paths As HashSet(Of String)
    Private ReadOnly Dependencies As HashSet(Of CompilationGroup)

    Private CompilationTask As Task

    Public Sub New(Name As String)
        Me.Name = Name
        Me.Paths = New HashSet(Of String)
        Me.Dependencies = New HashSet(Of CompilationGroup)
    End Sub

    Public Sub Add(Path As String)
        Paths.Add(Path)
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

    Public Function Compile() As Task
        SyncLock Me
            If CompilationTask Is Nothing Then
                CompilationTask = Me.CompileInternal()
            End If
            Return CompilationTask
        End SyncLock
    End Function

    Private Async Function CompileInternal() As Task
        Console.WriteLine("Start compile " & Name)
        Dim CompilationTasks As New List(Of Task)
        For Each Group In Dependencies
            CompilationTasks.Add(Group.Compile())
        Next
        For Each Task In CompilationTasks
            Await Task
        Next
        Await Task.Delay(2000)
        Console.WriteLine("End compile " & Name)
    End Function

    Public Overrides Function ToString() As String
        Return Name
    End Function
End Class
