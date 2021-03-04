VERSION 5.00
Begin {C62A69F0-16DC-11CE-9E98-00AA00574A4F} frmDuplicate 
   Caption         =   "Duplicate"
   ClientHeight    =   5940
   ClientLeft      =   120
   ClientTop       =   465
   ClientWidth     =   7545
   OleObjectBlob   =   "frmDuplicate.frx":0000
   StartUpPosition =   1  'CenterOwner
End
Attribute VB_Name = "frmDuplicate"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit




' ----------------------- IMPORTANT - READ CAREFULLY: COPYRIGHT NOTICE -------------------
' -- THIS SOFTWARE IS THE PROPERTY OF CTStation S.A.S. IN ANY COUNTRY                   --
' -- (WWW.CTSTATION.NET). ANY COPY, CHANGE OR DERIVATIVE WORK                           --
' -- IS SUBJECT TO CTSTATION S.A.S.’S PRIOR WRITTEN CONSENT.                            --
' -- THIS SOFTWARE IS REGISTERED TO THE FRENCH ANTI-PIRACY AGENCY (APP).                --
' -- COPYRIGHT 2019-2020 CTSTATTION S.A.S. – ALL RIGHTS RESERVED.                         --
' ----------------------------------------------------------------------------------------

'
' Cre.  PBEN    2020 11 29  Show BFC data pickers
'


'----------------------------------------
'       Closing the window
'----------------------------------------

'Closing the form prohibits reading the tag, thus hide it and cancel closing, unless necessary
' See https://docs.microsoft.com/fr-fr/office/vba/language/reference/user-interface-help/queryclose-event
'   for other closmode values
Private Sub UserForm_QueryClose(Cancel As Integer, CloseMode As Integer)
    S_Cancel
    Cancel = (CloseMode = vbFormControlMenu)
End Sub


Private Sub cmdCancel_Click()
    S_Cancel
End Sub


Private Sub S_Cancel()
    Me.Tag = ""
    Me.Hide
End Sub


Private Sub cmdOK_Click()
    Debug.Assert S_IsValid()
    Me.Tag = "OK"
    Me.Hide
End Sub




'----------------------------------------
'       Period picker
'----------------------------------------


Private Sub cmdUpdPer_Click()
    Dim s As String
    
    s = BFC_PickPeriod(Me.txtUpdPer.Text)
    If s <> "" Then Me.txtUpdPer.Text = s
End Sub



'----------------------------------------
'       Change actions
'----------------------------------------


Private Sub cmbPhase_Change()
    Dim s As String
    
    s = BFC_GetDesc(iDimCategory, Me.cmbPhase.Text)
    If s = "" Then s = "?"
    Me.lblDescPhase.Caption = s
    
    BFC_LoadList Me.cmbCatVersion, iSubCategoryVersion, Me.cmbPhase.Text
    
    cmdOK.Enabled = S_IsValid()
    
    S_SignalAlreadyExists
End Sub



Private Sub cmbCatVersion_Change()
    Dim s As String
    
    s = BFC_GetDesc(iSubCategoryVersion, Me.cmbCatVersion.Text, Me.cmbPhase.Text)
    If s = "" Then s = "?"
    Me.lblDescCatVersion.Caption = s
    
    cmdOK.Enabled = S_IsValid()
End Sub


Private Sub txtUpdPer_Change()
    cmdOK.Enabled = S_IsValid()
    
    S_SignalAlreadyExists
End Sub



Private Function S_IsValid( _
) As Boolean
    S_IsValid = BFC_IsValid(iDimCategory, Me.cmbPhase.Text) _
                And BFC_IsValid(iDimPeriod, Me.txtUpdPer) _
                And BFC_IsValid(iSubCategoryVersion, Me.cmbCatVersion, Me.cmbPhase.Text) _
                And Not S_ReportingExists()
End Function


Private Function S_ReportingExists( _
) As Boolean
    S_ReportingExists = RF_GetExistingReportings().Exists(cmbPhase.Text & " - " & txtUpdPer.Text)
End Function


Private Sub S_SignalAlreadyExists()
    Dim s As String
    
    With lblAlreadyExists
        If S_ReportingExists() Then
            s = cmbPhase.Text & " - " & txtUpdPer.Text
            .Caption = "** Reporting " & s & " already exists **"
            .Visible = True
        Else
            .Visible = False
        End If
    End With
End Sub
