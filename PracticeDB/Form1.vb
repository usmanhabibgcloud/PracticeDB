Imports Microsoft.VisualBasic.DateAndTime
Imports System.ComponentModel
Imports System.Data.SqlClient


Public Class Form1

    Dim con As New SqlConnection("server=" & "DESKTOP-HOQPMQC" & ";database=PracticeDB;user=sa;pwd=disaster;Connect Timeout=200; pooling='true'; Max Pool Size=200")
    Dim CurrentFIRID As Long = 0
    Dim IsEditMode As Boolean = False

    '4. ADD Button (New Record)

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        ClearForm()
        IsEditMode = False
        CurrentFIRID = 0
    End Sub

    Private Sub ClearForm()
        txtFIRNo.Clear()
        txtFIRDate.Clear()
        txtClaimantName.Clear()

        grdDetail.Rows.Clear()
    End Sub
    '5. SAVE Button (Insert / Update)
    Dim tblMain As String = "tblFIRMain"
    Dim tblDetail As String = "tblFIRDetail"


    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click

        con.Open()
        Dim tran As SqlTransaction = con.BeginTransaction()
        Dim tblMainFields() As String = {"FIRNo", "FIRDate", "ClaimantName"}
        Dim tblMainValues() As Object = {txtFIRNo.Text, Date.ParseExact(txtFIRDate.Text, "dd-MM-yyyy", Nothing), txtClaimantName.Text}



        Dim mystring As String = Nothing
        Dim myvaluesvar As String = Nothing
        For i As Integer = 0 To UBound(tblMainFields)
            mystring &= tblMainFields(i) & IIf(i < UBound(tblMainFields), ", ", " ")
            myvaluesvar &= "@" & tblMainFields(i) & IIf(i < UBound(tblMainFields), ", ", " ")
        Next

        Try
            If IsEditMode = False Then
                '---------------- INSERT MASTER ----------------
                Dim cmdMain As New SqlCommand(
                "INSERT INTO " & tblMain & " ( " & mystring & " )
                 VALUES (" & myvaluesvar & ");
                 SELECT SCOPE_IDENTITY()", con, tran)

                For l As Integer = 0 To UBound(tblMainFields)
                    cmdMain.Parameters.AddWithValue("@" & tblMainFields(l), tblMainValues(l))
                Next
                CurrentFIRID = Convert.ToInt64(cmdMain.ExecuteScalar())
            Else
                '---------------- UPDATE MASTER ----------------
                Dim cmdMain As New SqlCommand(
                "UPDATE tblFIRMain SET  FIRNo=@FIRNo, FIRDate=@FIRDate, ClaimantName=@ClaimantName 
                 WHERE FIRID=@FIRID", con, tran)

                cmdMain.Parameters.AddWithValue("@FIRNo", txtFIRNo.Text)
                cmdMain.Parameters.AddWithValue("@FIRDate", Date.ParseExact(txtFIRDate.Text, "dd-MM-yyyy", Nothing))
                cmdMain.Parameters.AddWithValue("@ClaimantName", txtClaimantName.Text)
                cmdMain.Parameters.AddWithValue("@FIRID", CurrentFIRID)
                cmdMain.ExecuteNonQuery()

                'Delete old details
                Dim cmdDel As New SqlCommand("DELETE FROM tblFIRDetail WHERE FIRID=@FIRID", con, tran)
                cmdDel.Parameters.AddWithValue("@FIRID", CurrentFIRID)
                cmdDel.ExecuteNonQuery()
            End If



            '-------------------------------Converting DataGrid into Data table---------------------

            Dim tblDetail As New DataTable

            Dim strFldNames As String() = New String() {"EmployerName", "PeriodFrom"}

            For i As Integer = 0 To UBound(strFldNames)
                tblDetail.Columns.Add(strFldNames(i).ToString)
            Next

            'For r As Integer = 0 To grdVoucher.RowCount - 2
            'If Not (grdVoucher.Item("EmployerName", r).Value = Nothing) Then

            For Each row As DataGridViewRow In grdDetail.Rows
                If row.IsNewRow Then Continue For

                tblDetail.Rows.Add()
                tblDetail.Rows(tblDetail.Rows.Count - 1)("EmployerName") = row.Cells("EmployerName").Value
                tblDetail.Rows(tblDetail.Rows.Count - 1)("PeriodFrom") = Date.ParseExact(row.Cells("PeriodFrom").Value, "dd-MM-yyyy", Nothing)

            Next

            '---------------------------------------------

            '---------------- INSERT DETAILS ----------------

            For Each row As DataRow In tblDetail.Rows

                ' Optional: skip deleted rows
                'If row.RowState = DataRowState.Deleted Then Continue For

                Dim cmdDetail As New SqlCommand(
                "INSERT INTO tblFIRDetail (FIRID, EmployerName, PeriodFrom)
                    VALUES (@FIRID, @EmployerName, @PeriodFrom)", con, tran)

                cmdDetail.Parameters.AddWithValue("@FIRID", CurrentFIRID)
                cmdDetail.Parameters.AddWithValue("@EmployerName", row("EmployerName").ToString())
                cmdDetail.Parameters.AddWithValue("@PeriodFrom", row("PeriodFrom").ToString)

                cmdDetail.ExecuteNonQuery()
            Next


            tran.Commit()
            MessageBox.Show("Record saved successfully")

        Catch ex As Exception
            tran.Rollback()
            MessageBox.Show(ex.Message)
        Finally
            con.Close()
        End Try
    End Sub

    '6. EDIT Button (Load Record)

    Private Sub btnEdit_Click(sender As Object, e As EventArgs) Handles btnEdit.Click
        'Assume FIRID is selected from another form or textbox
        Dim firIdToEdit As Long = InputBox("Enter FIRID")

        con.Open()
        IsEditMode = True
        CurrentFIRID = firIdToEdit

        'Load Master
        Dim cmdMain As New SqlCommand("SELECT * FROM tblFIRMain WHERE FIRID=@FIRID", con)
        cmdMain.Parameters.AddWithValue("@FIRID", firIdToEdit)
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
        cmdDetail.Parameters.AddWithValue("@FIRID", firIdToEdit)
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
        Dim tran = con.BeginTransaction()

        Try
            Dim cmdDetail As New SqlCommand("DELETE FROM tblFIRDetail WHERE FIRID=@FIRID", con, tran)
            cmdDetail.Parameters.AddWithValue("@FIRID", CurrentFIRID)
            cmdDetail.ExecuteNonQuery()

            Dim cmdMain As New SqlCommand("DELETE FROM tblFIRMain WHERE FIRID=@FIRID", con, tran)
            cmdMain.Parameters.AddWithValue("@FIRID", CurrentFIRID)
            cmdMain.ExecuteNonQuery()

            tran.Commit()
            ClearForm()
            MessageBox.Show("Record deleted")

        Catch ex As Exception
            tran.Rollback()
            MessageBox.Show(ex.Message)
        Finally
            con.Close()
        End Try
    End Sub

End Class
