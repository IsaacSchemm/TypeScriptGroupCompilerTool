Imports System.IO

Module CompilerPathSearch
    Dim CachedPath As String = Nothing

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
End Module
