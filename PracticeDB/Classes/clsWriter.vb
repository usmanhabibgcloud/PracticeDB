Imports System.Data.SqlClient



Public Class ClsWriter
    Private _fldNames As String()

    Public Property FldNames() As String()
        Get
            Return _fldNames
        End Get
        Set(ByVal value As String())
            _fldNames = value
        End Set
    End Property
    Private _fldValues As Object()
    Public Property FldValues() As Object()
        Get
            Return _fldValues
        End Get
        Set(ByVal value As Object())
            _fldValues = value
        End Set
    End Property

    Private _tblName As String
    Public Property TblName() As String
        Get
            Return _tblName
        End Get
        Set(ByVal value As String)
            _tblName = value
        End Set
    End Property
    'Private _tblNameWhere As String
    'Public Property tblNameWhere() As String
    '    Get
    '        Return _tblNameWhere
    '    End Get
    '    Set(ByVal value As String)
    '        _tblNameWhere = value
    '    End Set
    'End Property
    Dim cn As New SqlConnection

    Public Sub Runcommand()

        Dim mycommand As New SqlCommand With {
            .CommandText = _myString,
            .Connection = cn
        }
        mycommand.ExecuteScalar()

    End Sub
    Private _myString As String = Nothing
    Public Property CmdText() As String
        Get
            Return _myString
        End Get
        Set(ByVal value As String)
            _myString = value
        End Set
    End Property
    Public Function BuildCommand(ByVal blnEdit As Boolean, Optional ByVal strWhere As String = Nothing) As String

        If blnEdit = False Then GoTo AddMode
        If blnEdit = True Then GoTo EditMode

AddMode:

        _myString = "Insert Into " & _tblName & "("
        For i As Integer = 0 To UBound(_fldNames)
            _myString &= _fldNames(i) & IIf(i = UBound(_fldNames), ")", ", ")
        Next
        _myString &= " Values ("
        For i As Integer = 0 To UBound(_fldValues)
            _myString &= "'" & _fldValues(i) & IIf(i = UBound(_fldValues), "' )", "', ")
        Next
        Return _myString

        Exit Function
EditMode:
        _myString = "update " & TblName & " set "
        For i As Integer = 0 To UBound(_fldNames) - 1
            _myString &= _fldNames(i) & " = '" & _fldValues(i) & "' ,"
        Next
        _myString = Strings.Left(_myString, Len(_myString) - 1)
        _myString &= " " & strWhere

        Return _myString


    End Function


    Public Function GridTable(ByVal dg As DataGridView, ByVal TRows As Integer, ByVal Fields As String(), ByVal Cols As Integer()) As DataTable
        Dim tbl As New DataTable
        For c As Integer = 0 To UBound(Fields)
            Dim col As New DataColumn With {
                .ColumnName = Fields(c)
            }
            tbl.Columns.Add(col)
            col = Nothing
        Next
        Dim myRow As DataRow
        For r As Integer = 0 To TRows - 1
            myRow = tbl.Rows.Add
            For c As Integer = 0 To tbl.Columns.Count - 1
                myRow(c) = Convert.ChangeType(dg.Item(Cols(c), r).Value, TypeCode.String)
            Next
        Next
        Return tbl
    End Function

    Public Sub Runcommand(ByVal tbl As DataTable, ByVal con As SqlConnection, ByVal blnEdit As Boolean, ByVal tblName As String)
        Dim myString As String
        Dim mycmd As New SqlCommand With {
            .Connection = con
        }
        Dim myTran As SqlTransaction = con.BeginTransaction
        Try
            mycmd.Transaction = myTran
            mycmd.CommandText = _myString

            Dim myID As Long = mycmd.ExecuteScalar()
            mycmd.CommandText = Nothing
            If blnEdit = True Then GoTo EditMode
            If blnEdit = True Then GoTo AddMode
EditMode:

AddMode:
            For r As Integer = 0 To tbl.Rows.Count - 1
                myString = "Insert into " & tblName & " ("
                For i As Integer = 0 To tbl.Columns.Count - 1
                    myString &= tbl.Columns(i).ColumnName & IIf(i = tbl.Columns.Count - 1, " )", " , ")
                Next
                myString &= " Values ("
                For c As Integer = 0 To tbl.Columns.Count - 1
                    myString &= "'" & tbl.Rows(r)(c) & IIf(tbl.Columns.Count - 1 = c, "' )", "' ,")
                Next
                mycmd.CommandText = myString
                mycmd.ExecuteScalar()
                myString = Nothing
                mycmd.CommandText = Nothing
            Next
            myTran.Commit()
        Catch ex As Exception
            myTran.Rollback()
        End Try
    End Sub
    Public Function FnRunCommand() As Long
        Dim mycommand As New SqlCommand With {
            .CommandText = _myString,
            .Connection = cn
        }
        Return mycommand.ExecuteScalar()

    End Function

    ''' <param name="tbl">Give the name of Detail  table that you have created</param>
    ''' <param name="con">Give the Name of Open Connection</param>
    ''' <param name="fldNames">Field Names of Main Table as String Array</param>
    ''' <param name="fldValues">Field Values of Main Table as Object Array</param>
    Public Sub AddRecord(ByVal tbl As DataTable, ByVal con As SqlConnection, ByVal tblDetailName As String, ByVal tblMainName As String, ByVal IDfldName As String, ByVal fldNames As String(), ByVal fldValues As Object())

        Dim myString As String = Nothing
        Dim intVoucherID As Long = Nothing
        Dim myCmd As New SqlCommand With {
            .Connection = con
        }
        Dim myTran As SqlTransaction = con.BeginTransaction
        myCmd.Transaction = myTran

        '-----------------Geting the Command Text for Saving the Main Table------------

        myString = "Insert Into " & tblMainName & "("
        For i As Integer = 0 To UBound(fldNames)
            myString &= fldNames(i) & IIf(i = UBound(fldNames), ")", ", ")
        Next
        myString &= " Values ("
        For i As Integer = 0 To UBound(fldValues)
            myString &= "'" & fldValues(i) & IIf(i = UBound(fldValues), "' )", "', ")
        Next
        If Not IDfldName Is Nothing Then
            myString &= " Select Ident_Current('" & tblMainName & "')"
        End If

        Try
            '--------------------Saving Main Table and Getting the Available ID of new Voucher----------

            myCmd.CommandText = myString
            If Not IDfldName Is Nothing Then
                intVoucherID = myCmd.ExecuteScalar
                tbl.Columns.Add(IDfldName, GetType(Long))    'Adding Column for Storing the Voucher ID
                tbl.Columns(IDfldName.ToString).SetOrdinal(0)

                For i As Integer = 0 To tbl.Rows.Count - 1
                    tbl.Rows(i)(IDfldName.ToString) = intVoucherID
                Next

            Else
                myCmd.ExecuteNonQuery()
            End If
            myString = Nothing                              'Clearing the Command String for Detail Table
            myCmd.CommandText = Nothing

            '-------------------Saving The Detail Table--------------------------------------

            For r As Integer = 0 To tbl.Rows.Count - 1
                myString = "Insert into " & tblDetailName & " ("
                For i As Integer = 0 To tbl.Columns.Count - 1
                    myString &= tbl.Columns(i).ColumnName & IIf(i = tbl.Columns.Count - 1, " )", " , ")
                Next
                myString &= " Values ("
                For c As Integer = 0 To tbl.Columns.Count - 1
                    myString &= "'" & tbl.Rows(r)(c) & IIf(tbl.Columns.Count - 1 = c, "' )", "' ,")
                Next
                myCmd.CommandText = myString
                myCmd.ExecuteScalar()
                myString = Nothing
                myCmd.CommandText = Nothing
            Next

            myTran.Commit()
            MsgBox("The Record Has Successfully Saved", MsgBoxStyle.Exclamation)
        Catch ex As Exception
            myTran.Rollback()
            MsgBox(ex.Message)
            Exit Sub
        End Try
    End Sub

    Public Sub UpdateRecord(ByVal tbl As DataTable, ByVal con As SqlConnection, ByVal tblDetailName As String, ByVal tblMainName As String, ByVal IDfldName As String, ByVal fldNames As String(), ByVal fldValues As Object(), ByVal intIDValue As Object, Optional ByVal IsIdFieldString As Boolean = False)

        Dim myString As String = Nothing
        Dim intVoucherID As Long = Nothing
        Dim myCmd As New SqlCommand With {
            .Connection = con
        }
        Dim myTran As SqlTransaction = con.BeginTransaction
        myCmd.Transaction = myTran

        '-----------------Geting the Command Text for Updating the Main Table------------

        myString = "Update " & tblMainName & " Set "
        For i As Integer = 0 To UBound(fldNames)
            myString &= fldNames(i) & " = '" & fldValues(i) & "' ,"
        Next
        myString = Strings.Left(myString, Len(myString) - 1)

        If IsIdFieldString = True Then
            myString &= " Where " & IDfldName & " = '" & intIDValue.ToString & "'"
            myString &= " Delete " & tblDetailName & " where " & IDfldName & " = '" & intIDValue.ToString & "'"
        Else
            myString &= " Where " & IDfldName & " = " & intIDValue
            myString &= " Delete " & tblDetailName & " where " & IDfldName & " = " & intIDValue
        End If

        myCmd.CommandText = myString

        Try
            '--------------------Updating Main Table-------------------------------

            myCmd.CommandText = myString
            myCmd.ExecuteScalar()
            myString = Nothing                              'Clearing the Command String for Detail Table
            myCmd.CommandText = Nothing

            '-------------------Updating The Detail Table--------------------------------------

            If IsIdFieldString = False Then
                tbl.Columns.Add(IDfldName, GetType(Long))       'Adding Column for Storing the Voucher ID
                tbl.Columns(IDfldName.ToString).SetOrdinal(0)
                For i As Integer = 0 To tbl.Rows.Count - 1
                    tbl.Rows(i)(IDfldName.ToString) = intIDValue
                Next
            End If


            For r As Integer = 0 To tbl.Rows.Count - 1
                myString = "Insert into " & tblDetailName & " ("
                For i As Integer = 0 To tbl.Columns.Count - 1
                    myString &= tbl.Columns(i).ColumnName & IIf(i = tbl.Columns.Count - 1, " )", " , ")
                Next
                myString &= " Values ("
                For c As Integer = 0 To tbl.Columns.Count - 1
                    myString &= "'" & tbl.Rows(r)(c) & IIf(tbl.Columns.Count - 1 = c, "' )", "' ,")
                Next
                myCmd.CommandText = myString
                myCmd.ExecuteScalar()
                myString = Nothing
                myCmd.CommandText = Nothing
            Next
            myTran.Commit()
            MsgBox("The Record Has Successfully Updated", MsgBoxStyle.Exclamation)
        Catch ex As Exception
            myTran.Rollback()
            MsgBox(ex.Message)
            Exit Sub
        End Try
    End Sub

    Public Sub DeleteRecord(ByVal tblName As String, ByVal fldID As String, ByVal WhereCriteria As Object, ByVal con As SqlConnection)
        Dim cmd As New SqlCommand
        Dim myTran As SqlTransaction = con.BeginTransaction
        Try
            cmd.CommandText = "Delete " & tblName & " Where " & fldID & " = '" & WhereCriteria & "'"
            cmd.Connection = con
            cmd.Transaction = myTran
            cmd.ExecuteNonQuery()
            myTran.Commit()
            MsgBox(" The Record Has Deleted")
        Catch ex As Exception
            myTran.Rollback()
            MsgBox(ex.Message)
        End Try
    End Sub

    Public Sub AddRecord(ByVal tblMainName As String, ByVal fldNames As String(), ByVal fldValues As Object(), ByVal con As SqlConnection)

        Dim myString As String = Nothing
        Dim myCmd As New SqlCommand With {
            .Connection = con
        }
        Dim myTran As SqlTransaction = con.BeginTransaction
        myCmd.Transaction = myTran

        '--------------------------Building the Command for Saving Main Table------------

        myString = "Insert Into " & tblMainName & "("
        For i As Integer = 0 To UBound(fldNames)
            myString &= fldNames(i) & IIf(i = UBound(fldNames), ")", ", ")
        Next
        myString &= " Values ("
        For i As Integer = 0 To UBound(fldValues)
            myString &= "'" & fldValues(i) & IIf(i = UBound(fldValues), "' )", "', ")
        Next

        '--------------------Saving Main Table-----------------------
        Try
            myCmd.CommandText = myString
            myCmd.ExecuteScalar()
            myString = Nothing
            myCmd.CommandText = Nothing
            myTran.Commit()
            MsgBox("The Record Has Successfully Saved", MsgBoxStyle.Exclamation)
        Catch ex As Exception
            myTran.Rollback()
            MsgBox(ex.Message)
            Exit Sub
        End Try
    End Sub

    Public Sub UpdateRecord(ByVal tblMainName As String, ByVal fldNames As String(), ByVal fldValues As Object(), ByVal strWhere As String, ByVal con As SqlConnection)

        Dim myString As String = Nothing
        Dim myCmd As New SqlCommand With {
            .Connection = con
        }
        Dim myTran As SqlTransaction = con.BeginTransaction
        myCmd.Transaction = myTran

        '-----------------Geting the Command Text for Updating the Main Table------------

        myString = "update " & tblMainName & " set "
        For i As Integer = 0 To UBound(fldNames)
            myString &= fldNames(i) & " = '" & fldValues(i) & "' ,"
        Next
        myString = Strings.Left(myString, Len(myString) - 1)
        myString &= " " & strWhere

        Try
            '--------------------Updating Main Table-------------------------------

            myCmd.CommandText = myString
            myCmd.ExecuteScalar()
            myString = Nothing
            myCmd.CommandText = Nothing

            myTran.Commit()
            MsgBox("The Record Has Successfully Updated", MsgBoxStyle.Exclamation)
        Catch ex As Exception
            myTran.Rollback()
            MsgBox(ex.Message)
            Exit Sub
        End Try
    End Sub

End Class