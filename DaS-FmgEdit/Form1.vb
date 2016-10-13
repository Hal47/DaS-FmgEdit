﻿Imports System.IO

Public Class frmDaSFmgEdit
    Shared bigendian = False
    Shared fs As FileStream

    Private Sub btnOpen_Click(sender As Object, e As EventArgs) Handles btnOpen.Click
        dgvTextEntries.Rows.Clear()
        dgvTextEntries.Columns.Clear()

        dgvTextEntries.Columns.Add("ID", "ID")
        dgvTextEntries.Columns.Add("Text", "Text")

        dgvTextEntries.Columns("ID").AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        dgvTextEntries.Columns("Text").AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
        dgvTextEntries.Columns("Text").DefaultCellStyle.WrapMode = DataGridViewTriState.True

        dgvTextEntries.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells

        fs = File.Open(txtFMGfile.Text, FileMode.Open)

        bigendian = (GetInt8(9) = -1)

        Dim numEntries As Integer
        Dim startOffset As Integer


        numEntries = GetInt32(&HC)
        startOffset = GetInt32(&H14)


        For i = 0 To numEntries - 1
            Dim startIndex As Integer
            Dim startID As Integer
            Dim endID As Integer
            Dim txtOffset As Integer
            Dim txt As String

            startIndex = GetInt32(&H1C + i * &HC)
            startID = GetInt32(&H1C + i * &HC + 4)
            endID = GetInt32(&H1C + i * &HC + 8)

            For j = 0 To (endID - startID)
                txtOffset = GetInt32(startOffset + ((startIndex + j) * 4))

                txt = ""
                If txtOffset > 0 Then
                    txt = GetUniString(txtOffset)
                End If
                
                dgvTextEntries.Rows.Add({j + startID, txt})
            Next
        Next

        fs.Close()
    End Sub
    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Try
            If Not File.Exists(txtFMGfile.Text & ".bak") Then
                File.Copy(txtFMGfile.Text, txtFMGfile.Text & ".bak")
            End If

            fs = File.Open(txtFMGfile.Text, FileMode.Create)

            Dim numEntries As Integer = 0
            Dim startOffset As Integer = 0
            Dim txtOffset As Integer = 0

            Dim prevID As Integer = -1
            Dim numChunks As Integer = 0

            For i = 0 To dgvTextEntries.Rows.Count - 1
                If dgvTextEntries.Rows(i).Cells("ID").FormattedValue > (prevID + 1) Then
                    numChunks += 1
                End If

                numEntries += 1
                prevID = dgvTextEntries.Rows(i).Cells("ID").FormattedValue
            Next


            startOffset = &H1C + &HC * numChunks
            txtOffset = startOffset + numentries * 4

            Dim FirstID As Integer = dgvTextEntries.Rows(0).Cells("ID").FormattedValue
            Dim LastID As Integer = FirstID
            Dim str As String
            Dim startEntry As Integer = 0

            numEntries = 0
            numChunks = 0
            For i = 0 To dgvTextEntries.Rows.Count - 1
                If dgvTextEntries.Rows(i).Cells("ID").FormattedValue > (LastID + 1) Then
                    PutInt32(&H1C + numChunks * &HC, startEntry)
                    PutInt32(&H1C + numChunks * &HC + 4, FirstID)
                    PutInt32(&H1C + numChunks * &HC + 8, LastID)
                
                    FirstID = dgvTextEntries.Rows(i).Cells("ID").FormattedValue
                    startEntry = numEntries
                    numChunks += 1
                End If

                str = dgvTextEntries.Rows(i).Cells("Text").FormattedValue

                If Not str = "" Then
                    PutInt32(startOffset + numEntries * 4, txtOffset)

                    str = str.Replace("/n/", ChrW(10))

                    If not str(str.Length-1) = ChrW(0) Then
                        str = str & ChrW(0)
                    End If

                    PutUniString(txtOffset, str)
                    txtOffset += str.Length * 2
                End If
            

                numEntries += 1
                LastID = dgvTextEntries.Rows(i).Cells("ID").FormattedValue
            Next

            if fs.Length Mod 4 = 2 Then PutInt16(txtOffset, 0)

            PutInt32(&H1C + numChunks * &HC, startEntry)
            PutInt32(&H1C + numChunks * &HC + 4, FirstID)
            PutInt32(&H1C + numChunks * &HC + 8, LastID)

            PutInt32(0, &H10000)
            PutInt8(&H8, 1)
            If bigendian Then PutInt8(&H9, -1)
            PutInt32(&H4, fs.Length)
            PutInt32(&HC, numChunks + 1)
            PutInt32(&H10, numEntries)
            PutInt32(&H14, startOffset)
        
            fs.Close

            MsgBox("File saved.")

        Catch ex As Exception
            MsgBox("Unknown error." & Environment.NewLine & ex.Message)
        End Try
    End Sub


    Private Sub txt_Drop(sender As Object, e As System.Windows.Forms.DragEventArgs) Handles txtFMGfile.DragDrop
        Dim file() As String = e.Data.GetData(DataFormats.FileDrop)
        sender.Text = file(0)
    End Sub
    Private Sub txt_DragEnter(sender As Object, e As System.Windows.Forms.DragEventArgs) Handles txtFMGfile.DragEnter
        Dim file() As String = e.Data.GetData(DataFormats.FileDrop)
        If Not (New FileInfo(file(0)).Extension.ToUpper().Equals(".FMG")) Then
            e.Effect = DragDropEffects.None
            Return
        End If
        e.Effect = DragDropEffects.Copy
    End Sub



    Private Function GetInt8(Byval loc As Integer) As SByte
        Dim tmpSByt As SByte
        Dim byt(0) As Byte

        fs.Position = loc
        fs.Read(byt, 0, 1)

        tmpSByt = CSByte(byt(0))

        Return tmpSByt
    End Function
    Private Function GetInt32(ByVal loc As Integer) As Integer
        Dim tmpInt As Integer = 0
        Dim byt = New Byte() {0, 0, 0, 0}

        fs.Position = loc
        fs.Read(byt, 0, 4)

        If bigendian Then
            Array.Reverse(byt)
        End If

        tmpInt = BitConverter.ToInt32(byt, 0)

        Return tmpInt
    End Function
    Private Function GetUInt32(ByVal loc As Integer) As UInteger
        Dim tmpUInt As UInteger = 0
        Dim byt = New Byte() {0, 0, 0, 0}

        fs.Position = loc
        fs.Read(byt, 0, 4)

        If bigendian Then
            Array.Reverse(byt)
        End If

        tmpUInt = BitConverter.ToUInt32(byt, 0)

        Return tmpUInt
    End Function
    Private Function GetUniString(ByVal loc As Integer) As String
        fs.Position = loc

        Dim tmpStr As String = ""
        Dim endstr As Boolean = False
        Dim byt(1) As Byte
        Dim chr As Char

        While Not endstr
            fs.Read(byt, 0, 2)

            If bigendian Then
                Array.Reverse(byt)
            End If

            chr = ChrW(byt(1) * 256 + byt(0))
            If chr = ChrW(0) Then endstr = True

            If chr = ChrW(10) Then
                tmpStr = tmpStr & "/n/"
            Else
                tmpStr = tmpStr & chr
            End If

        End While

        Return tmpStr
    End Function

    Private sub PutInt8(ByVal loc As Integer, ByVal val As SByte) 
        fs.Position = loc
        fs.write({CByte(val)}, 0, 1)
    End sub
    Private sub PutInt16(ByVal loc As Integer, byval val As Int16)
        fs.Position = loc
        Dim byt(1) as Byte
        byt = BitConverter.GetBytes(val)

        If bigendian Then Array.Reverse(byt)

        fs.Write(byt, 0, 2)
    End sub
    Private Sub PutInt32(ByVal loc As integer, ByVal val As Integer)
        fs.Position = loc
        Dim byt(3) as Byte

        byt = BitConverter.GetBytes(val)

        If bigendian Then Array.Reverse(byt)

        fs.Write(byt, 0, 4)
    End Sub
    Private sub PutUniString(ByVal loc As Integer, Byref str As String)
        fs.Position = loc

        Dim byt(1) as Byte
        Dim chr As Char

        For i = 0 To str.Length - 1
            chr = str(i)
            byt = BitConverter.GetBytes(chr)

            If bigendian Then
                Array.Reverse(byt)
            End If

            fs.Write(byt, 0, 2)
        Next

        'fs.Write({0,0},0,2)
    End sub

    Private Sub WriteBytesToStream(ByVal loc As Integer, ByVal byt() As Byte)
        fs.Position = loc
        fs.Write(byt, 0, byt.Length)
    End Sub
End Class
