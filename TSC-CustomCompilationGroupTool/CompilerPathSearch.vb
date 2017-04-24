Imports System.IO

Module CompilerPathSearch
    Dim CachedPath As String = Nothing

    Sub SetCompilerPath(Path As String)
        CachedPath = Path
    End Sub

    Function FindTypeScriptCompiler() As String
        If CachedPath Is Nothing Then
            CachedPath = FindTypeScriptCompilerInternal()
        End If
        Return CachedPath
    End Function

    Private Function GetVersionFromString(Value As String) As Version
        Try
            Return New Version(Value)
        Catch ex As Exception
            Return New Version(0, 0)
        End Try
    End Function

    Private Function FindTypeScriptCompilerInternal() As String
        If File.Exists("tsc.exe") Then
            Return "tsc.exe"
        Else
            For Each ProgramFilesDir In New String() {
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}
                Dim TypeScriptPath = Path.Combine(ProgramFilesDir, "Microsoft SDKs", "TypeScript")
                If Directory.Exists(TypeScriptPath) Then
                    Dim Versions = From V In Directory.EnumerateDirectories(TypeScriptPath)
                                   Order By GetVersionFromString(Path.GetFileName(V)) Descending
                                   Select V
                    For Each Subdirectory In Versions
                        Dim PossibleCompilerPath = Path.Combine(Subdirectory, "tsc.exe")
                        If File.Exists(PossibleCompilerPath) Then
                            Return PossibleCompilerPath
                        End If
                    Next
                End If
            Next
            Return Nothing
        End If
    End Function

    Sub PrintTypeScriptVersionInfo()
        Dim TypeScriptCompilerPath = FindTypeScriptCompiler()

        If TypeScriptCompilerPath Is Nothing Or Not File.Exists(TypeScriptCompilerPath) Then
            Throw New Exception("Cannot find tsc.exe")
        Else
            Dim GetVersion = New ProcessStartInfo(TypeScriptCompilerPath, "-v") With {
                .UseShellExecute = False,
                .RedirectStandardOutput = True
            }
            Dim CompilerVersionProcess = Process.Start(GetVersion)
            CompilerVersionProcess.WaitForExit()

            Dim Version = CompilerVersionProcess.StandardOutput.ReadToEnd().Split(" ").Last().Trim()

            Console.WriteLine($"Using TypeScript {Version}")
            Console.WriteLine($"Compiler path: {TypeScriptCompilerPath}")
            Console.WriteLine()
        End If
    End Sub
End Module
