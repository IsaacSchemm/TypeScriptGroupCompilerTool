Imports System.IO
Imports System.Reflection

Module Module1

    Sub Main()
        Try
            Dim InputFile = My.Application.CommandLineArgs.FirstOrDefault
            If InputFile Is Nothing Then
                InputFile = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) & ".ini"
            End If
            If Not File.Exists(InputFile) Then
                Throw New Exception($"Could not find {InputFile}")
                Return
            End If

            Dim Groups As New List(Of CompilationGroup)

            Using InputStream As New FileStream(InputFile, FileMode.Open, FileAccess.Read)
                Using Reader As New StreamReader(InputStream)
                    Dim CurrentGroup As CompilationGroup = Nothing
                    Dim CurrentLine = Reader.ReadLine()

                    While CurrentLine IsNot Nothing
                        If CurrentLine.StartsWith("[") And CurrentLine.EndsWith("]") Then
                            CurrentGroup = New CompilationGroup(CurrentLine.Substring(1, CurrentLine.Length - 2))
                            Groups.Add(CurrentGroup)
                        ElseIf Not String.IsNullOrWhiteSpace(CurrentLine) And Not CurrentLine.StartsWith(";") Then
                            Dim ExistingGroup = Groups.SingleOrDefault(Function(g) g.Name = CurrentLine)
                            If ExistingGroup IsNot Nothing Then
                                CurrentGroup.Add(ExistingGroup)
                            ElseIf File.Exists(CurrentLine) Then
                                CurrentGroup.Add(CurrentLine)
                            Else
                                Throw New Exception($"{CurrentLine} is not a TypeScript file or the name of a group earlier in the configuration file")
                            End If
                        End If

                        CurrentLine = Reader.ReadLine()
                    End While
                End Using
            End Using

            Dim TopLevelGroups As New List(Of CompilationGroup)
            For Each Group In Groups
                If Not Groups.Any(Function(g) g.DependsOn(Group)) Then
                    TopLevelGroups.Add(Group)
                End If
            Next
            Task.WaitAll(TopLevelGroups.Select(Function(g) g.Compile()).ToArray())
        Catch ae As AggregateException
            For Each e In ae.InnerExceptions
                Console.Error.WriteLine(e.Message)
                Console.Error.WriteLine(e.StackTrace)
                MsgBox(e.Message, MsgBoxStyle.Critical, e.GetType().Name)
            Next
        Catch e As Exception
            Console.Error.WriteLine(e.Message)
            Console.Error.WriteLine(e.StackTrace)
            MsgBox(e.Message, MsgBoxStyle.Critical, e.GetType().Name)
        End Try
    End Sub

End Module
