Imports System.Net
Imports System.IO


Public Class Form1
    Dim AppPath As String = My.Application.Info.DirectoryPath & "\"
    Dim XmlFilePath As String = AppPath & "UpFileList.xml"
    Dim UpdateXml As New Xml.XmlDocument
    Dim Url As String = ""
    Dim TempPath As String = ""
    Delegate Sub changeText()
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        BackgroundWorker1.WorkerSupportsCancellation = True
        TempPath = Environment.GetEnvironmentVariable("temp").ToString & "\"

        If FileTrue(XmlFilePath) = False Then
            MessageBox.Show("对不起,更新配置文件未找到!")
            Timer1.Enabled = False
            End
        Else
            Try
                UpdateXml.Load(XmlFilePath)
                Url = UpdateXml.SelectNodes("/Xml/Url").Item(0).InnerText
            Catch ex As Exception
                MessageBox.Show(ex.Message)
                Timer1.Enabled = False
                Exit Sub
            End Try
        End If
    End Sub


    Public Function FileTrue(ByVal str As String) As Boolean
        Try
            If IO.File.Exists(str) = True Then
                Dim FileInfo As IO.FileInfo = New IO.FileInfo(str)
                If FileInfo.Length <= 0 Then
                    FileTrue = False
                Else
                    FileTrue = True
                End If
                FileInfo = Nothing
            Else
                FileTrue = False
            End If

        Catch ex As Exception
            FileTrue = False
        End Try
        GC.Collect()

    End Function

    Sub RunThread()
        If UpdateXml.SelectNodes("/Xml/Files/Itme").Count = 0 Then
            MsgBox("没有更新文件列表!")
        End If
        Try
            BackgroundWorker1.CancelAsync()
            BackgroundWorker1.RunWorkerAsync()
        Catch ex As Exception
            BackgroundWorker1.CancelAsync()
        End Try
    End Sub

    Private Sub BackgroundWorker1_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        For i As Integer = 0 To UpdateXml.SelectNodes("/Xml/Files/Itme").Count - 1
            DownUpdateFile(UpdateXml.SelectNodes("/Xml/Files/Itme")(i).Attributes("Name").Value, i)
        Next
        Invoke(New EventHandler(AddressOf updatecomplete))
        GC.Collect()
        System.Diagnostics.Process.GetCurrentProcess.MinWorkingSet = New System.IntPtr(5)
    End Sub
    Sub UpdateComplete(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Me.Cursor = Cursors.Default
        Dim AppPathN As String = ""

        Dim NodeList As Xml.XmlNodeList = UpdateXml.SelectNodes("/Xml/Files/Itme")
        Dim Source As String = ""
        For i = 0 To NodeList.Count - 1
            AppPathN = AppPath & NodeList.Item(i).Attributes("Name").Value
            AppPathN = AppPathN.Replace("Update\", "")
            AppPathN = AppPathN.Replace("Update/", "")
            Source = ""
            Source = TempPath & NodeList.Item(i).Attributes("Name").Value
            CopyFile(Source, AppPathN)
        Next
        AppPathN = AppPath & "UpFileList.xml"
        AppPathN = AppPathN.Replace("Update\", "")
        AppPathN = AppPathN.Replace("Update/", "")
        Source = ""
        Source = (TempPath & "Update/UpFileList.xml")
        CopyFile(Source, AppPathN)
        StartExe()
    End Sub
    Private Sub StartExe()
        Dim AppPathN As String = AppPath
        If InStr(AppPathN, "Update") > 0 Then
            AppPathN = Mid(AppPathN, 1, InStr(AppPathN, "update") - 1)
        End If
        Dim strExe = AppPathN & UpdateXml.SelectNodes("/Xml/RunExe").Item(0).InnerText
        Try
            System.Diagnostics.Process.Start(strExe)
            Application.Exit()
        Catch ex As Exception
            MessageBox.Show("软件自动更新完毕,但自动启动未成功,需手动启动[" & UpdateXml.SelectNodes("/Xml/RunExe").Item(0).InnerText & "]软件!")
        End Try
    End Sub
    '
    Public Sub CopyFile(ByVal sourcePath As String, ByVal objPath As String)
        'If Strings.Right(Strings.UCase(sourcePath), 4) = ".EXE" Then
        'sourcePath = sourcePath.ToUpper.Replace(".EXE", ".ms")
        'End If
        If FileTrue(sourcePath) = False Then Exit Sub

        CreateDirtory(objPath)
        File.Delete(objPath)
        File.Copy(sourcePath, objPath, True)
    End Sub
    Private Sub Change()

    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        RunThread()
        Me.Button1.Enabled = False
    End Sub
    Private Sub DownUpdateFile(ByVal FileStr As String, ByVal RowIndex As Integer)
        Dim fileLength As Integer = 0
        Try
            FileStr = FileStr.Replace("\", "/")
            'If Strings.Right(Strings.UCase(FileStr), 4) = ".EXE" Then
            'FileStr = FileStr.ToUpper.Replace(".EXE", ".ms")
            'End If

            Dim webReq As WebRequest = WebRequest.Create(Url & FileStr)
            Dim webRes As WebResponse = webReq.GetResponse()
            fileLength = webRes.ContentLength
            If fileLength <= 0 Then
                Invoke(New EventHandler(AddressOf ListViewRef), RowIndex & "|" & "失败")
                Exit Sub
            End If

            Dim ProgressValue As Double = 0
            Invoke(New EventHandler(AddressOf ProgressBarRef), ProgressValue)
            Invoke(New EventHandler(AddressOf ProgressBarMax), fileLength)

            Dim srm As Stream = webRes.GetResponseStream
            Dim srmReader As New StreamReader(srm)
            Dim bufferByte As Byte() = New Byte(fileLength - 1) {}
            Dim allByte As Integer = CInt(bufferByte.Length)
            Dim startByte As Integer = 0

            While fileLength > 0
                Application.DoEvents()
                Dim downByte As Integer = srm.Read(bufferByte, startByte, allByte)
                If downByte = 0 Then
                    Exit While
                End If
                startByte += downByte
                allByte -= downByte
                ProgressValue = ProgressValue + downByte
                Invoke(New EventHandler(AddressOf ProgressBarRef), ProgressValue)

                Dim part As Single = CSng(startByte) / 1024
                Dim total As Single = CSng(bufferByte.Length) / 1024
                Dim percent As Integer = Convert.ToInt32((part / total) * 100)

                Invoke(New EventHandler(AddressOf ListViewRef), RowIndex & "|" & percent.ToString() & "%")
            End While
            FileStr = FileStr.Replace("/", "\")
            CreateDirtory(TempPath & FileStr)
            Dim fs As New FileStream(TempPath & FileStr, FileMode.OpenOrCreate, FileAccess.Write)
            fs.Write(bufferByte, 0, bufferByte.Length)
            srm.Close()
            srmReader.Close()
            fs.Close()
        Catch ex As Exception

        End Try
    End Sub
    '
    Sub ProgressBarRef(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Try
            ProgressBar1.Value = CInt(sender.ToString)
        Catch ex As Exception

        End Try
    End Sub
    Sub ProgressBarMax(ByVal sender As System.Object, ByVal e As System.EventArgs)
        ProgressBar1.Maximum = CInt(sender.ToString)
    End Sub

    Sub ListViewRef(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Try
            Dim BB() As String = Split(sender.ToString(), "|")
            If BB.Length > 1 Then
                If BB(0) >= 0 Then
                    ListView1.Items(CInt(BB(0))).SubItems(2).Text = BB(1).ToString()
                End If
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        BackgroundWorker1.CancelAsync()
        BackgroundWorker1.Dispose()
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Timer1.Enabled = False
        Dim FileListPath = "Update/UpFileList.xml"
        If Url <> "" Then
            Label1.Text = "正在获取更新文件列表信息....."
            DownUpdateFile(FileListPath, -1)
            Label1.Text = "获取更新文件列表信息成功!"
        End If
        If FileTrue(TempPath & FileListPath) = False Then
            MessageBox.Show("对不起,更新的配置文件未找到!" & Chr(13) & TempPath & FileListPath)
            Exit Sub
        End If
        UpdateXml.Load(TempPath & FileListPath)

        Dim ListView(2) As String
        Dim NoteList As System.Xml.XmlNodeList = UpdateXml.SelectNodes("/Xml/Files/Itme")

        For i = 0 To NoteList.Count - 1
            ListView(0) = NoteList(i).Attributes("Name").Value
            ListView(1) = "1.0.0.1"
            ListView(2) = ""
            ListView1.Items.Add(New ListViewItem(ListView))
        Next

    End Sub
    '
    Private Sub CreateDirtory(ByVal path As String)
        If Not File.Exists(path) Then
            Dim dirArray As String() = path.Split("\")
            Dim temp As String = String.Empty
            For i As Integer = 0 To dirArray.Length - 2
                temp += dirArray(i).Trim() & "\"
                If Not Directory.Exists(temp) Then
                    Directory.CreateDirectory(temp)
                End If
            Next
        End If
    End Sub

End Class
