Imports System.IO
Imports System.Reflection

Module Module1
    Iterator Function ReadStandardInput() As IEnumerable(Of String)
        Using Reader As New StreamReader(Console.OpenStandardInput())
            While Not Reader.EndOfStream
                Yield Reader.ReadLine
            End While
        End Using
    End Function

    Sub Main()
        Dim InputFile = My.Application.CommandLineArgs.FirstOrDefault
        If InputFile Is Nothing Then
            InputFile = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) & ".ini"
        End If

        Dim Input As String()
        If InputFile = "-" Then
            Input = ReadStandardInput().ToArray()
        Else
            Input = File.ReadAllLines(InputFile)
        End If

        Dim Groups As New List(Of CompilationGroup)

        For Each CurrentLine In Input
            If CurrentLine.StartsWith("[") And CurrentLine.EndsWith("]") Then
                Dim Name = CurrentLine.Substring(1, CurrentLine.Length - 2)
                If Not Char.IsLower(Name(0)) Then
                    Throw New Exception("Group names in the INI file must start with a lowercase letter.")
                End If
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

        Dim TypeScriptCompilerPath = FindTypeScriptCompiler()

        If TypeScriptCompilerPath Is Nothing Then
            Throw New Exception("Cannot find tsc.exe")
        Else
            Dim GetVersion = New ProcessStartInfo(TypeScriptCompilerPath, "-v") With {
                .UseShellExecute = False,
                .RedirectStandardOutput = True
            }
            Dim CompilerVersionProcess = Process.Start(GetVersion)
            CompilerVersionProcess.WaitForExit()

            Dim Version = CompilerVersionProcess.StandardOutput.ReadToEnd().Replace("Version ", "").Trim()

            Console.WriteLine($"Using TypeScript {Version}")
            Console.WriteLine($"Compiler path: {TypeScriptCompilerPath}")
            Console.WriteLine()
        End If

        Dim StartTime = Date.UtcNow
        Dim Tasks = (From Group In TopLevelGroups
                     Select StartCompile(TypeScriptCompilerPath, Group)).ToArray()
        Task.WaitAll(Tasks)
        Console.WriteLine()
        Console.WriteLine($"Total: {(Date.UtcNow - StartTime).TotalSeconds}s")

        If Tasks.Any(Function(t) t.Result = False) Then
            Console.WriteLine()
            Console.WriteLine("Press Enter to exit.")
            Console.ReadLine()
        Else
            Threading.Thread.Sleep(1000)
        End If
    End Sub

    Async Function StartCompile(TypeScriptCompilerPath As String, Group As CompilationGroup) As Task(Of Boolean)
        Try
            Dim StartTime = Date.UtcNow
            'Console.WriteLine($"Starting: {Group.Name}")
            Await Group.Compile(TypeScriptCompilerPath)
            Console.WriteLine($"{Group.Name}: {(Date.UtcNow - StartTime).TotalSeconds}s")
            Return True
        Catch ex As Exception
            Console.Error.WriteLine($"{Group.Name}: {ex.Message}")
            Console.Error.WriteLine(ex.StackTrace)
            Console.Error.WriteLine()
            Return False
        End Try
    End Function

End Module
