Imports Microsoft.VisualBasic.DateAndTime
Imports System.ComponentModel
Imports System.Data.SqlClient


Public Class Form1

    Dim con As New SqlConnection("server=" & "DESKTOP-HOQPMQC" & ";database=PracticeDB;user=sa;pwd=disaster;Connect Timeout=200; pooling='true'; Max Pool Size=200")
    Dim CurrentID As Long = 0

    Dim IsEditMode As Boolean = False

    '4. ADD Button (New Record)

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        ClearForm()
        IsEditMode = False
        CurrentID = 0
    End Sub

    Private Sub ClearForm()
        txtFIRNo.Clear()
        txtFIRDate.Clear()
        txtClaimantName.Clear()

        grdDetail.Rows.Clear()
    End Sub
    '5. SAVE Button (Insert / Update)

    Dim tblMainName As String = "tblFIRMain"
    Dim tblDetailName As String = "tblFIRDetail"
    Dim MainTbl_IDFieldName As String = "FIRID"

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click

        '-------------------------------Converting DataGrid into Data table---------------------

        Dim tblDetail As New DataTable

        tblDetail.Columns.Add("EmployerName", GetType(String))
        tblDetail.Columns.Add("PeriodFrom", GetType(Date))

        For Each row As DataGridViewRow In grdDetail.Rows
            If row.IsNewRow Then Continue For
            Dim dr As DataRow = tblDetail.NewRow()  'dr = data row

            dr("EmployerName") = row.Cells("EmployerName").Value.ToString()
            dr("PeriodFrom") = Date.ParseExact(row.Cells("PeriodFrom").Value.ToString(), "dd-MM-yyyy", Nothing)

            tblDetail.Rows.Add(dr)

        Next


        'Dim tran As SqlTransaction = con.BeginTransaction()
        Dim tblMainFields() As String = {"FIRNo", "FIRDate", "ClaimantName"}
        Dim tblMainValues() As Object = {txtFIRNo.Text, Date.ParseExact(txtFIRDate.Text, "dd-MM-yyyy", Nothing), txtClaimantName.Text}

        con.Open()

        If IsEditMode = False Then

            Dim cls As New ClsWriter
            cls.AddRecord(tblDetail, con, "tblFIRDetail", "tblFIRMain", "FIRID", tblMainFields, tblMainValues)
            con.Close()

            Exit Sub

        Else

            Dim cls As New ClsWriter
            cls.UpdateRecord(tblDetail, con, "tblFIRDetail", "tblFIRMain", "FIRID", tblMainFields, tblMainValues, CurrentID)
            con.Close()
            Exit Sub

        End If
    End Sub

    '6. EDIT Button (Load Record)

    Private Sub btnEdit_Click(sender As Object, e As EventArgs) Handles btnEdit.Click
        'Assume FIRID is selected from another form or textbox
        Dim IdFieldToEdit As Long = InputBox("Enter FIRID")

        con.Open()
        IsEditMode = True
        CurrentID = IdFieldToEdit

        'Load Master
        Dim cmdMain As New SqlCommand("SELECT * FROM tblFIRMain WHERE FIRID=@FIRID", con)
        cmdMain.Parameters.AddWithValue("@FIRID", IdFieldToEdit)
        Dim rdr = cmdMain.ExecuteReader()

        If rdr.Read() Then
            txtFIRNo.Text = rdr("FIRNO").ToString()
            txtFIRDate.Text = CType(rdr("FIRDate"), Date).ToString("dd-MM-yyyy")
            txtClaimantName.Text = rdr("ClaimantName").ToString()

        End If
        rdr.Close()

        'Load Detail
        grdDetail.Rows.Clear()
        Dim cmdDetail As New SqlCommand("SELECT * FROM tblFIRDetail WHERE FIRID=@FIRID", con)
        cmdDetail.Parameters.AddWithValue("@FIRID", IdFieldToEdit)
        rdr = cmdDetail.ExecuteReader()

        While rdr.Read()
            grdDetail.Rows.Add(
            rdr("EmployerName"),
            CType(rdr("PeriodFrom"), Date).ToString("dd-MM-yyyy"))

        End While

        con.Close()
    End Sub

    ' 7. DELETE Button (Only in Edit Mode)

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If IsEditMode = False Then
            MessageBox.Show("Edit mode required")
            Return
        End If

        If MessageBox.Show("Are you sure?", "Confirm", MessageBoxButtons.YesNo) = DialogResult.No Then Exit Sub

        con.Open()
        Dim cls As New ClsWriter
        cls.DeleteRecord("tblFIRMain", "tblFIRDetail", "FIRID", CurrentID, con)
        ClearForm()

        con.Close()

    End Sub

End Class
