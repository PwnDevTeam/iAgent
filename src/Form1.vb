Imports System
Imports System.IO


Public Class Form1

    ' This is for disable the X button.
    Private Declare Function GetSystemMenu Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal bRevert As Int32) As IntPtr
    Private Declare Function GetMenuItemCount Lib "user32.dll" (ByVal hMenu As IntPtr) As Int32
    Private Declare Function DrawMenuBar Lib "user32.dll" (ByVal hWnd As IntPtr) As Int32
    Private Declare Function RemoveMenu Lib "user32.dll" (ByVal hMenu As IntPtr, ByVal nPosition As Int32, ByVal wFlags As Int32) As Int32

    Private Const MF_BYPOSITION As Int32 = &H400
    Private Const MF_REMOVE As Int32 = &H1000

    Private Sub RemoveCloseButton(ByVal frmForm As Form)
        Dim hMenu As IntPtr, n As Int32
        hMenu = GetSystemMenu(frmForm.Handle, 0)
        If Not hMenu.Equals(IntPtr.Zero) Then
            n = GetMenuItemCount(hMenu)
            If n > 0 Then
                RemoveMenu(hMenu, n - 1, MF_BYPOSITION Or MF_REMOVE)
                RemoveMenu(hMenu, n - 2, MF_BYPOSITION Or MF_REMOVE)
                DrawMenuBar(frmForm.Handle)
            End If
        End If
    End Sub


    Dim device As New MobileDevice(MobileDevice.DeviceMode.Recovery)
    Dim deviceIsRunningRecovery As Boolean

    ' ------------------------------------------------------------------------------------------------------------------------------
    ' Subroutine Name: DoCMD 
    ' Author: Fallensn0w
    ' Description: executes a file with arguement and make the program DoEvents() until the executed is done.
    ' ------------------------------------------------------------------------------------------------------------------------------
    Sub DoCMD(ByVal file As String, ByVal arg As String)
        Dim procNlite As New Process
        winstyle = 1
        procNlite.StartInfo.FileName = file
        procNlite.StartInfo.Arguments = " " & arg
        procNlite.StartInfo.WindowStyle = winstyle
        Application.DoEvents()
        procNlite.Start()
        Do Until procNlite.HasExited
            Application.DoEvents()
            For i = 0 To 5000000
                Application.DoEvents()
            Next
        Loop
        procNlite.WaitForExit()        '
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Button3.Width = 214
        Label2.Visible = False
        Button1.Enabled = False


        Button1.Text = ("Finding your device..")
        If device.IsRunningMode(MobileDevice.DeviceMode.Normal) Then
            Button1.Text = ("Entering Recovery Mode..")
            DoCMD("iphucwin32.exe", "-qo enterrecovery")
        ElseIf device.IsRunningMode(MobileDevice.DeviceMode.Recovery) Then
            Button1.Text = ("Found device in Recovery Mode")
        Else
            Button1.Text = ("No device found!")
            Exit Sub
        End If

        Dim thecontents() As String = Split(device.getDeviceInfo, " ")
        xECID = "Not Found"
        xSerial = "Not Found"

        '
        For i = 0 To UBound(Split(device.getDeviceInfo, " "))
            If InStr(thecontents(i), "ECID:") Then
                xECID = Replace(thecontents(i), "ECID:", "")
            Else
                xSerial = ("Error while gathering ECID")
            End If
        Next

        For i = 0 To UBound(Split(device.getDeviceInfo, " "))
            If InStr(thecontents(i), "SRNM:") Then
                xSerial = Replace(thecontents(i), "SRNM:", "")
                xSerial = Replace(xSerial, "[", "") : xSerial = Replace(xSerial, "]", "")
            Else
                xSerial = ("Error while gathering Serial")
            End If
        Next

        Button1.Text = _
                                   "ECID: " & xECID & _
                                   vbCrLf & "Serial Number: " & _
                                   xSerial & vbCrLf & _
                                    "Copied to clipboard."

        Clipboard.SetText(Button1.Text & vbCrLf & vbCrLf & "iAgent by fallensn0w.")
        deviceIsRunningRecovery = True

        Button2.Visible = True

    End Sub


    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        RemoveCloseButton(Me)
        Button1.Text = "Send a iAgent to my iDevice"
        Button1.Enabled = True
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        If DeviceIsRunningRecovery = True Then
            Button2.Text = ("Booting back into iPhone OS.")
            device.AutoBoot()
            device.Dispose()
            Button2.Visible = False
            deviceIsRunningRecovery = False
        End If
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        Shell("notepad " & Application.StartupPath & "\iAgent.txt", AppWinStyle.NormalFocus)
        End
    End Sub

    Private Sub Label2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label2.Click
        MessageBox.Show("LibUSB is available to download at: http://libusb.org ", Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub Label1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label1.Click
        Process.Start("http://twitter.com/fallensn0w")
    End Sub

End Class

