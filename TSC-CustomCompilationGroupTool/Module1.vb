Imports System.IO
Imports System.Reflection

Module Module1

    Sub Main()
        Dim InputFile = My.Application.CommandLineArgs.FirstOrDefault
        If InputFile Is Nothing Then
            InputFile = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) & ".ini"
        End If
        If Not File.Exists(InputFile) Then
            Throw New Exception($"Could not find {InputFile}")
            Return
        End If

        Dim Groups As New List(Of CompilationGroup)

        Dim Input = File.ReadAllLines(InputFile)

        For Each CurrentLine In Input
            If CurrentLine.StartsWith("[") And CurrentLine.EndsWith("]") Then
                Dim Name = CurrentLine.Substring(1, CurrentLine.Length - 2)
                Groups.Add(New CompilationGroup(Name))
            End If
        Next

        Dim CurrentGroup As CompilationGroup = Nothing
        For Each CurrentLine In Input
            If CurrentLine.StartsWith("[") And CurrentLine.EndsWith("]") Then
                CurrentGroup = Groups.Single(Function(g) g.Name = CurrentLine.Substring(1, CurrentLine.Length - 2))
            ElseIf Not String.IsNullOrWhiteSpace(CurrentLine) And Not CurrentLine.StartsWith(";") Then
                Dim ExistingGroup = Groups.SingleOrDefault(Function(g) g.Name = CurrentLine)
                If ExistingGroup IsNot Nothing Then
                    CurrentGroup.Add(ExistingGroup)
                Else
                    CurrentGroup.Add(CurrentLine)
                End If
            End If
        Next

        Dim TopLevelGroups As New List(Of CompilationGroup)
        For Each Group In Groups
            If Not Groups.Any(Function(g) g.DependsOn(Group)) Then
                TopLevelGroups.Add(Group)
            End If
        Next

        Dim StartTime = DateTime.UtcNow
        Dim Tasks = TopLevelGroups.Select(Function(g) StartCompile(g)).ToArray()
        Task.WaitAll(Tasks)
        Console.WriteLine()
        Console.WriteLine($"Total: {(DateTime.UtcNow - StartTime).TotalSeconds}s")

        If Tasks.Any(Function(t) t.Result = False) Then
            Console.WriteLine()
            Console.WriteLine("Press Enter to exit.")
            Console.ReadLine()
        Else
            Threading.Thread.Sleep(1000)
        End If
    End Sub

    Async Function StartCompile(Group As CompilationGroup) As Task(Of Boolean)
        Try
            Dim StartTime = DateTime.UtcNow
            'Console.WriteLine($"Starting: {Group.Name}")
            Await Group.Compile()
            Console.WriteLine($"{Group.Name}: {(DateTime.UtcNow - StartTime).TotalSeconds}s")
            Return True
        Catch ex As Exception
            Console.Error.WriteLine($"{Group.Name}: {ex.Message}")
            Console.Error.WriteLine(ex.StackTrace)
            Console.Error.WriteLine()
            Return False
        End Try
    End Function

End Module
