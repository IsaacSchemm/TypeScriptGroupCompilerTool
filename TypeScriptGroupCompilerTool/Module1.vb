' This Source Code Form is subject to the terms of the Mozilla Public
' License, v. 2.0. If a copy of the MPL was not distributed with this
' file, You can obtain one at http://mozilla.org/MPL/2.0/.

Imports System.IO
Imports System.Reflection
Imports System.Threading

Module Module1
    Private Sub PrintHelp(InputFile As String)
        Console.WriteLine("TypeScriptGroupCompilerTool")
        Console.WriteLine("version 1.0")
        Console.WriteLine()
        Console.WriteLine("If you run this program without any command-line arguments, it will look for a .ini file in the current directory (with the same name as the executable.) You can also specify the path to a .ini file on the command line.")
        Console.WriteLine()
        Console.WriteLine("If an error is encountered, this program will pause for user input before closing.")
        Console.WriteLine()
        Console.WriteLine("This program is subject to the terms of the Mozilla Public License, v. 2.0. Source code is available at:")
        Console.WriteLine("https://github.com/IsaacSchemm/TypeScriptGroupCompilerTool")
    End Sub

    Sub Main()
        Try
            Dim InputFile = My.Application.CommandLineArgs.FirstOrDefault
            If InputFile Is Nothing Then
                InputFile = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) & ".ini"
            End If

            Dim Input As String()
            If InputFile = "-" Then
                Console.Error.WriteLine("Reading from standard input is not supported.")
                Environment.ExitCode = 1
                Exit Sub
            ElseIf InputFile = "--help" OrElse InputFile = "/?" Then
                PrintHelp(InputFile)
                Exit Sub
            Else
                Input = File.ReadAllLines(InputFile)
            End If

            Dim Groups As New List(Of CompilationGroup)

            For Each CurrentLine In Input
                If CurrentLine.StartsWith("[") And CurrentLine.EndsWith("]") Then
                    Exit For
                End If

                If CurrentLine.StartsWith(";") Then
                    Continue For
                End If

                Dim EqIndex = CurrentLine.IndexOf("="c)
                If EqIndex >= 0 Then
                    Dim Name = CurrentLine.Substring(0, EqIndex).Trim()
                    Dim Value = CurrentLine.Substring(EqIndex + 1).Trim()
                    If Name = "TypeScriptCompilerPath" Then
                        SetCompilerPath(Value)
                    End If
                End If
            Next

            For Each CurrentLine In Input
                If CurrentLine.StartsWith("[") And CurrentLine.EndsWith("]") Then
                    Dim Name = CurrentLine.Substring(1, CurrentLine.Length - 2)
                    Groups.Add(New CompilationGroup(Name))
                End If
            Next

            Dim CurrentGroup As CompilationGroup = Nothing
            For Each CurrentLine In Input
                If String.IsNullOrWhiteSpace(CurrentLine) Or CurrentLine.StartsWith(";") Then
                    ' skip
                ElseIf CurrentLine.StartsWith("[") And CurrentLine.EndsWith("]") Then
                    CurrentGroup = Groups.Single(Function(g) g.Name = CurrentLine.Substring(1, CurrentLine.Length - 2))
                ElseIf CurrentGroup IsNot Nothing Then
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

            PrintTypeScriptVersionInfo()

            Dim StartTime = Date.UtcNow
            Dim Tasks = (From Group In TopLevelGroups
                         Select StartCompile(Group)).ToArray()
            Task.WaitAll(Tasks)
            Console.WriteLine()
            Console.WriteLine($"Total: {(Date.UtcNow - StartTime).TotalSeconds}s")

            Dim FailedCount = Tasks.Where(Function(t) t.Result = False).Count()
            If FailedCount > 0 Then
                Throw New Exception(FailedCount & " task(s) failed.")
            Else
                Thread.Sleep(1000)
            End If
        Catch e As Exception
            If e.GetType().Name <> "Exception" Then
                Console.Error.Write(e.GetType().Name)
                Console.Error.Write(": ")
            End If
            Console.Error.WriteLine(e.Message)
            Console.Error.WriteLine()
            Console.Error.WriteLine("Press Enter to exit.")
            Console.ReadLine()
        End Try
    End Sub

    Private Semaphore As New SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount)

    Async Function StartCompile(Group As CompilationGroup) As Task(Of Boolean)
        Try
            Dim StartTime = Date.UtcNow
            Await Semaphore.WaitAsync()
            Await Group.Compile()
            Semaphore.Release()
            Console.WriteLine($"{Group.Name}: {(Date.UtcNow - StartTime).TotalSeconds}s")
            Return True
        Catch ex As Exception
            Console.Error.WriteLine($"{Group.Name}: {ex.Message}")
            Console.Error.WriteLine()
            Return False
        End Try
    End Function

End Module
