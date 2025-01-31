﻿Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Xml
Imports System.Globalization
Imports System.Text
Imports System.Web
Imports System.Net

Public Class TFXMLEpubManager

    Private XMLString As String = String.Empty
    Dim xmlMeta As New XmlDocument
    Dim IsBookEndNote As Boolean = False
    Private iChap As Integer = 0, iHead As Integer = 0, iChpSec As Integer = 0
    Public Property sISBN As String = String.Empty
    Private epubISBN As String = String.Empty
    Public Property AppPath As String = String.Empty
    Dim NotesChapter As String = String.Empty
    Public Property ProjectID As String = String.Empty
    Public Property FileSequence As New List(Of String)
    Public Property sXMLFileName As String = String.Empty
    Public Const sMsgTitle = "XML Cleanup"
    Private sXMLFilePath As String = String.Empty
    Private bExecuteOnce As Boolean = False
    Private iDedication As Integer = 0
    Private AbstractXML As String = String.Empty
    Public bNoError As Boolean = False
    Private IsePubXML As Boolean = False

    Public Function MainXMLPro(sXMLPath As String, Optional bxslExecution As Boolean = False, Optional Project_ID As String = "", Optional AbstractXML As String = "") As Boolean
#If CONFIG = "FinalXML" Then
        AppPath = "\\fsdeanta\TechRelease\Accounts\Common\DeantaComposer\Publish\extra"
#Else
        AppPath = GBL.AppPath
#End If
        Me.AbstractXML = AbstractXML
        Me.ProjectID = Project_ID
        Try
            Dim di As DirectoryInfo = New DirectoryInfo(sXMLPath.ToString)
            Dim aryFi() As FileInfo = di.GetFiles("*.xml")
            sXMLFilePath = sXMLPath
            IsePubXML = bxslExecution
            epubISBN = Regex.Replace(sISBN, "(\d{3})(\d)(\d{3})(\d{5})(\d)", "$1-$2-$3-$4-$5")
            If Not sXMLFileName.EndsWith(".xml") Then sXMLFileName = sXMLFileName & ".xml"
            If File.Exists(Path.Combine(sXMLFilePath, sXMLFileName.ToString)) AndAlso Not bxslExecution Then
                Dim sText As String = File.ReadAllText(sXMLFilePath & "\" & sXMLFileName)
                Dim sxmlFileName1 As String = sXMLFileName
                If Not sxmlFileName1.ToString.ToLower.Contains(".xml") Then sxmlFileName1 = sxmlFileName1 & ".xml"

                If Not XSLPro(sText, Path.Combine(sXMLFilePath, sXMLFileName), bxslExecution) Then
                    Return False
                End If
                XMLString = File.ReadAllText(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")))
                FinalCleanup(bxslExecution)
                File.WriteAllText(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")), XMLString)

                If File.Exists(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml"))) Then
                    My.Computer.FileSystem.DeleteFile(Path.Combine(sXMLFilePath, sXMLFileName))
                    My.Computer.FileSystem.RenameFile(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")), sXMLFileName)
                End If
                GBL.DeantaBallon("xsl has been executed. Please check the file.", MessageType.MSGINFO)
                Return False
            ElseIf File.Exists(Path.Combine(sXMLFilePath, sXMLFileName.ToString)) AndAlso bxslExecution Then
                Dim sText As String = File.ReadAllText(sXMLFilePath & "\" & sXMLFileName)
                'sText = UpdatePro(sText)
                XSLPro(sText, Path.Combine(sXMLFilePath, sXMLFileName), True)
                GBL.DeantaBallon("EPUB has been generated. Please check.", MessageType.MSGINFO)
                Return False
            End If
            'frmMain.Hide()
            If Not sXMLFileName.ToString.ToLower.EndsWith(".xml") Then sXMLFileName = sXMLFileName & ".xml"
            ' Merging takes place here...
            Dim sBookInfo As String = "<book xmlns=""http://docbook.org/ns/docbook"" version=""5.0"" xml:id=""b-" & sISBN.ToString & """ xmlns:mml=""http://www.w3.org/1998/Math/MathML"" xmlns:xlink=""http://www.w3.org/1999/xlink"" xml:lang=""en"" role=""fullText"">"
            Using XMLWrite As StreamWriter = File.CreateText(sXMLFilePath & "\" & sXMLFileName)
                XMLWrite.WriteLine("<?xml version=""1.0"" encoding=""utf-8""?>")
                XMLWrite.WriteLine("<?oxygen SCHSchema=""../../../docbook-mods.sch""?>")
                XMLWrite.WriteLine("<?oxygen RNGSchema=""../../../bloomsbury-mods.rnc"" type=""compact""?>")
                XMLWrite.WriteLine(sBookInfo.ToString)
            End Using
            Dim iChFnCnt As Integer = 0
            Dim sXMLTxt As String = String.Empty
            iSec = 1 : iVal = 0
            Dim iChap As Integer = 0
            Dim FootnoteType As Boolean = False

            NotesChapter = (From n In FileSequence Where n.ToLower().Contains("_notes_") Select n).FirstOrDefault
            If (Not String.IsNullOrEmpty(NotesChapter)) Then
                NotesChapter = Path.Combine(sXMLFilePath, NotesChapter)
                IsBookEndNote = True
            Else
                IsBookEndNote = False
            End If
            Using XMLWrite As StreamWriter = File.AppendText(Path.Combine(sXMLFilePath, sXMLFileName))
                For i = 0 To FileSequence.Count - 1
                    'If Regex.IsMatch(XMLString, "<chapter", RegexOptions.IgnoreCase Or RegexOptions.Singleline) Then iChap = iChap + 1
                    iChap = iChap + 1

                    Dim xmlFile As String = Path.Combine(Path.Combine(sXMLFilePath, FileSequence(i)))
                    If (Not File.Exists(xmlFile)) Then
                        GBL.DeantaBallon($"Could not able to find the file: {xmlFile}", MessageType.MSGERROR)
                        Continue For
                    End If

                    If ((IsBookEndNote) And (String.Compare(Path.GetFileName(NotesChapter), Path.GetFileName(xmlFile), True) = 0)) Then
                        Continue For
                    End If

                    XMLString = File.ReadAllText(xmlFile)

                    FootnoteType = False
                    XMLString = XMLString.Replace("<emphasis role=""entity"">&amp;</emphasis>", "&#x0026;")
                    XMLString = XMLString.Replace("<emphasis>&#x2013;</emphasis>", "&#x2013;")
                    XMLString = XMLString.Replace("<link role=""page""> </link>", "")
                    XMLString = XMLString.Replace("cols=""0""", "")
                    XMLString = XMLString.Replace("<titleabbrev", "<title").Replace("</titleabbrev>", "</title>")
                    XMLString = Regex.Replace(XMLString, "<tgroup[^>]*>", "<tgroup>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    XMLString = Regex.Replace(XMLString, "<label[^>]*>[\s]*</label>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    XMLString = Regex.Replace(XMLString, "<title[^>]*>[\s]*</title>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    'XMLString = Regex.Replace(XMLString, " role=""""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    'If Regex.IsMatch(XMLString, "(<chapter(?:(?!(-title|>)).)+>)((?:(?:(?!</info>).)+)</author></info>)", RegexOptions.IgnoreCase Or RegexOptions.Singleline) Then
                    '    XMLString = Regex.Replace(XMLString, "(<chapter(?:(?!(-title|>)).)+>)((?:(?:(?!</info>).)+)</info>)", AddressOf ChapterProHC, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    'Else
                    '    XMLString = Regex.Replace(XMLString, "(<chapter(?:(?!(-title|>)).)+>)((?:(?:(?!</title>).)+)</title>)", AddressOf ChapterProHC, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    'End If
                    iChpSec = iChpSec + 1
                    'XMLString = XMLString.Replace("<mml:", "<").Replace("</mml:", "</")

                    XMLString = XMLString.Replace("&#x200B;", "")
                    'XMLString = Regex.Replace(XMLString, "&#x2003;", " ", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                    If (IsePubXML) Then
                        XMLString = Regex.Replace(XMLString, "<para role=""Math_Where"">(((?!<para>).)*)</para>", Function(fMt As Match)
                                                                                                                      Return fMt.Value.Replace(vbTab, " ")
                                                                                                                  End Function, RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    End If

                    XMLString = XMLString.Replace("  ", " ")
                    XMLString = XMLString.Replace("<untag>", "").Replace("</untag>", "")
                    XMLString = Regex.Replace(XMLString, "<untag[\s]*/>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    XMLString = RemoveEmptyElements(XMLString)

                    XMLString = Regex.Replace(XMLString, "<entity>(\t)+</entity>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    XMLString = Regex.Replace(XMLString, "<publisher[\s]*/>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    XMLString = Regex.Replace(XMLString, "<entity[^>]*>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Replace("</entity>", "")
                    XMLString = Regex.Replace(XMLString, " aid:tfooter=""[^""]+""", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    XMLString = Regex.Replace(XMLString, " aid:tfooter=""""", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    'XMLString = RemoveDuplicatePageNum(XMLString)

                    If (Path.GetFileNameWithoutExtension(xmlFile).ToLower().Contains("_index_")) Then
                        XMLString = Regex.Replace(XMLString, "<LRH[^>]*>((?:(?!<\/LRH>).)*)</LRH>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                        XMLString = Regex.Replace(XMLString, "<RRH[^>]*>((?:(?!<\/RRH>).)*)</RRH>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    End If

                    If (IsBookEndNote) Then
                        ''true based on label, false based on ID
                        Try
                            XMLString = MoveFootnotes(XMLString)
                        Catch ex As Exception
                            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                        End Try
                    End If

                    XMLString = Regex.Replace(XMLString, "<emphasis role=""ITALIC"">""", "<emphasis role=""italic"">""", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    XMLString = Regex.Replace(XMLString, "<section[^>]*><title>Notes</title></section>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    XMLString = Regex.Replace(XMLString, "<section[^>]*><title>Note</title></section>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)

                    Try
                        FootnoteType = IdentifyFootnoteType(XMLString)
                    Catch ex As Exception
                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    End Try

                    XMLString = Regex.Replace(XMLString, "<xref [^>]*>", Function(fMt As Match)
                                                                             Dim ChapNum As String = String.Empty
                                                                             ChapNum = Regex.Match(FileSequence(i), "_C([0-9]+)_", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Groups(1).Value
                                                                             Return Regex.Replace(fMt.Value, "rid=""fn", "rid=""ch" & ChapNum.Replace("0", "") & "-fn", RegexOptions.IgnoreCase)
                                                                         End Function, RegexOptions.Singleline Or RegexOptions.IgnoreCase)

                    XMLString = Regex.Replace(XMLString, "<link[^>]*></link>", Function(ft As Match)
                                                                                   Return String.Empty
                                                                               End Function, RegexOptions.Singleline Or RegexOptions.IgnoreCase)

                    XMLString = Regex.Replace(XMLString, "<link[^>]*/>", Function(fMt As Match)
                                                                             Return String.Empty
                                                                         End Function, RegexOptions.Singleline Or RegexOptions.IgnoreCase)

                    XMLString = Regex.Replace(XMLString, "(<sup>((?:(?!<\/sup>).)*)</sup></xref>)(<footnote [^>]*>)", Function(m As Match)
                                                                                                                          If Not m.Value.ToString.EndsWith(">") Then Return m.Value.ToString
                                                                                                                          Dim xrefVal As String = m.Groups(1).Value.ToString().Replace("<sup>", "").Replace("</sup>", "").Replace("</xref>", "")
                                                                                                                          Dim sInput As String = m.Groups(3).Value.ToString
                                                                                                                          Dim RoleValue As String = String.Empty
                                                                                                                          Dim ChapNum As String = String.Empty
                                                                                                                          ChapNum = Regex.Match(FileSequence(i), "_C([0-9]+)_", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Groups(1).Value
                                                                                                                          Dim smt As Match = Regex.Match(sInput, " ?label=""([^""]+)""", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                                                                                                                          sInput = Regex.Replace(sInput, " ?label=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                                                                                                                          RoleValue = Regex.Match(sInput, " ?role=""([^""])+""", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Value
                                                                                                                          RoleValue = RoleValue.Replace("role=""", "").Replace("""", "").Trim()
                                                                                                                          If (FootnoteType) Then
                                                                                                                              RoleValue = RoleValue.Replace("end-bk-note", "end-ch-note")
                                                                                                                          End If
                                                                                                                          sInput = Regex.Replace(sInput, " ?role=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                                                                                                                          sInput = Regex.Replace(sInput, "xml:id=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                                                                                                                          If String.IsNullOrEmpty(smt.Groups(1).Value.ToString) Then
                                                                                                                              sInput = sInput & " role=" & Chr(34) & RoleValue & Chr(34) & " label=" & Chr(34) & iFootnote & Chr(34) & " xml:id=""note" & iFootnote & "-ba-" & String.Format("{0:00000}", iFootnote) & """>"
                                                                                                                          Else
                                                                                                                              sInput = sInput.TrimEnd(">") & " role=" & Chr(34) & RoleValue & Chr(34) & " label=" & Chr(34) & "ch" & ChapNum.TrimStart("0", "") & "-" & smt.Groups(1).Value.ToString & Chr(34) & " xml:id=""note" & iFootnote & "-ba-" & String.Format("{0:00000}", iFootnote) & """>"
                                                                                                                              sInput = sInput & "<label>" & xrefVal & "</label>"
                                                                                                                          End If
                                                                                                                          Return m.Groups(1).Value.ToString() & sInput
                                                                                                                      End Function, RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    XMLString = XMLString.Replace("role=""end-bk-note""", "role=""end-ch-note""")

                    XMLString = Regex.Replace(XMLString, "(<book[^>]*)(>)", "$1 xmlns:mml=""http://www.w3.org/1998/Math/MathML"">", RegexOptions.Singleline Or RegexOptions.IgnoreCase)

                    XMLString = XMLString.Replace(" &amp; ", " &#x0026; ")
                    XMLString = XMLString.Replace(">& ", ">&#x0026; ")
                    XMLString = XMLString.Replace(" &<", " &#x0026;<")
                    XMLString = XMLString.Replace(">&</", ">&#x0026;</")

                    XMLString = Regex.Replace(XMLString, "<thead[\s]*/>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    XMLString = Regex.Replace(XMLString, "<row[\s]*/>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)

                    'XMLString = Regex.Replace(XMLString, "<emphasis role=""smallcaps"">((?:(?!<\/emphasis[^>]*>).)*)</emphasis>", "$1", RegexOptions.Singleline Or RegexOptions.IgnoreCase)

                    XMLString = XMLString.Replace("<chapter><LRH></LRH><RRH></RRH></chapter>", "")
                    XMLString = XMLString.Replace("<LRH></LRH>", "")
                    XMLString = XMLString.Replace("<RRH></RRH>", "")
                    XMLString = Regex.Replace(XMLString, "<chapter[^>]*></chapter>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    'XMLString = Regex.Replace(XMLString, "(<section([^><]+)?)>", AddressOf SectionPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    'If Not bxslExecution Then XMLString = Regex.Replace(XMLString, "(<footnote([^><]+)?)>", AddressOf FootnotePro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    XMLString = Regex.Replace(XMLString, "(<title([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    'XMLString = Regex.Replace(XMLString, "(<bibliomixed xml:id="")([^""]+)""", "$1ch" & iChap & "-$2" & Chr(34), RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    'XMLString = Regex.Replace(XMLString, "(<link role=[^ ]+ linkend="")([^""]+)""", "$1ch" & iChap & "-$2" & Chr(34), RegexOptions.IgnoreCase Or RegexOptions.Singleline)


                    If Not bxslExecution Then

                        Try
                            If ((XMLString.Contains("</chapter>") Or XMLString.Contains("</acknowledgements>") Or XMLString.Contains("</preface>")) And (Not XMLString.Contains("<index>"))) Then
                                Dim mtch As MatchCollection = Regex.Matches(XMLString, "<footnote[^><]+>((?:(?!</footnote>).)+)</footnote>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                                Dim FootList As List(Of String) = mtch.Cast(Of Match)().Select(Function(m) m.Value).ToList
                                If ((FootList IsNot Nothing) AndAlso (FootList.Count > 0)) Then
                                    mtch = Regex.Matches(XMLString, "<footnote[^><]+>((?:(?!</footnote>).)+)</footnote>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                                    XMLString = Regex.Replace(XMLString, "<footnote[^><]+>((?:(?!</footnote>).)+)</footnote>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                                    XMLString = Regex.Replace(XMLString, "</chapter>", "<fn-group><title>Notes</title></chapter>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                                    XMLString = Regex.Replace(XMLString, "</preface>", "<fn-group><title>Notes</title></preface>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                                    XMLString = Regex.Replace(XMLString, "</acknowledgements>", "<fn-group><title>Notes</title></acknowledgements>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                                    Dim in1 As Integer = 0
                                    For Each ft As String In FootList
                                        XMLString = XMLString.Replace("</chapter>", ft & "</chapter>")
                                        XMLString = XMLString.Replace("</preface>", ft & "</preface>")
                                        XMLString = XMLString.Replace("</acknowledgements>", ft & "</acknowledgements>")
                                    Next
                                    XMLString = Regex.Replace(XMLString, "</preface>", "</fn-group></preface>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                                    XMLString = Regex.Replace(XMLString, "</chapter>", "</fn-group></chapter>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                                    XMLString = Regex.Replace(XMLString, "</acknowledgements>", "</fn-group></acknowledgements>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                                End If
                            End If
                        Catch ex As Exception
                        End Try
                    End If
                    XMLString = XMLString.Replace("aid:tfooter=""""", "")
                    XMLString = TandFXmlCleanup(XMLString)
                    XMLString = Regex.Replace(XMLString, "((<biblioid class=""isbn""[^><]+>((?:(?!</biblioid>).)+)</biblioid>)+)+", "<biblioset role=""isbns"" xml:id=""bs-000001"">" & "$1" & "</biblioset>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                    Dim mt As Match = Nothing
                    mt = Regex.Match(XMLString, "<book([^><]+)?>((?:(?!</book>).)+)</book>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    If mt.Success Then
                        'If (FileSequence(i).ToLower().Contains("_prelims_")) Then
                        '    sXMLTxt = sXMLTxt & mt.Value.ToString.Trim & Environment.NewLine
                        'Else
                        'sXMLTxt = sXMLTxt.Replace("<book>", "").Replace("</book>", "")
                        sXMLTxt = sXMLTxt & mt.Groups(2).Value.ToString.Trim & Environment.NewLine
                        'End If


                    ElseIf (Not Regex.Match(XMLString, "<book([^><]+)?></book>", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Success) Then
                        'sXMLTxt = sXMLTxt & XMLString
                    End If
                Next

                If bxslExecution Then
                    sXMLTxt = Regex.Replace(sXMLTxt, "<biblioid class=""isbn""([^><]+)?>((?:(?!</biblioid>).)+)</biblioid>", AddressOf BiblioIdPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    If (Not sXMLTxt.Contains("role=""epub""")) Then
                        sXMLTxt = Regex.Replace(sXMLTxt, "</biblioid></biblioset>", "</biblioid>" & "<biblioid class=""isbn"" role=""epub"">" & epubISBN & "</biblioid>" & "</biblioset>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    End If
                    ' To retain bibliomixed attributes in bibliography
                    'sXMLTxt = Regex.Replace(sXMLTxt, "<bibliography([^><]+)?>((?:(?!</bibliography>).)+)</bibliography>", AddressOf Bibliomixed, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                End If
                sXMLTxt = Regex.Replace(sXMLTxt, "<chapter([^><]+)?>(((?!</chapter>).)+)</chapter>", AddressOf ChapterIDSeq, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                sXMLTxt = Regex.Replace(sXMLTxt, "<title[^>]*[\s]*/>", "")

                sXMLTxt = Regex.Replace(sXMLTxt, "(<info[^><]+>)(" & Environment.NewLine & ")?(<section[^><]+>)", "$3$2$1", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                sXMLTxt = UpdatePro(sXMLTxt, bxslExecution)

                sXMLTxt = IndexXmlCleanup(sXMLTxt)

                sXMLTxt = sXMLTxt.Replace("<em>", "").Replace("</em>", "")
                sXMLTxt = Regex.Replace(sXMLTxt, "<sub[\s]*/>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                sXMLTxt = Regex.Replace(sXMLTxt, "<em[\s]*/>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                sXMLTxt = Regex.Replace(sXMLTxt, "<refbibliomixed ", "<bibliomixed ", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                sXMLTxt = Regex.Replace(sXMLTxt, "([^ ])xml:id=", "$1 xml:id=", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                'If Not bxslExecution Then
                sXMLTxt = Regex.Replace(sXMLTxt, "<caption([^><]+)?>(((?!</caption>).)+)</caption>", AddressOf FigureCaptionParaPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                sXMLTxt = Regex.Replace(sXMLTxt, "<footnote([^><]+)?>(((?!</footnote>).)+)</footnote>", AddressOf FigureCaptionParaPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                sXMLTxt = Regex.Replace(sXMLTxt, "<endnote([^><]+)?>(((?!</endnote>).)+)</endnote>", AddressOf FigureCaptionParaPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                'sXMLTxt = Regex.Replace(sXMLTxt, "(<figure([^><]+)?>(?:(?:(?!</figure>).)+)</figure>)((?:(?!</para>).)+)?</para>", AddressOf FigurePlacementPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                'sXMLTxt = Regex.Replace(sXMLTxt, "(<table([^><]+)?>(?:(?:(?!</table>).)+)</table>)((?:(?!</para>).)+)?</para>", AddressOf FigurePlacementPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                If (bxslExecution) Then
                    sXMLTxt = Regex.Replace(sXMLTxt, "<equation[^><]+>((?:(?!</equation>).)+)</equation>", AddressOf MathCleanUp, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                    sXMLTxt = Regex.Replace(sXMLTxt, "(<mml:math[^>]*)(>)", "$1 alttext="""" $2", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                End If

                sXMLTxt = Regex.Replace(sXMLTxt, "</1para>", "</para>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                ' End If
                sXMLTxt = Regex.Replace(sXMLTxt, "xml:id="""" ", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                'sXMLTxt = sXMLTxt.Replace("mml:", "")
                If (Not bxslExecution) Then
                    sXMLTxt = GruopBodyforChapter(sXMLTxt)
                End If

                XMLWrite.WriteLine(sXMLTxt.ToString)
                XMLWrite.WriteLine("</book>")

            End Using
            ' XML TandF xsl
            'File.Copy(Path.Combine(sXMLFilePath, sXMLFileName), Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")))
            If Not bxslExecution Then
                File.WriteAllText(Path.Combine(sXMLFilePath, sXMLFileName), File.ReadAllText(Path.Combine(sXMLFilePath, sXMLFileName)).Replace("&", "&amp;"))
                If Not XSLPro(sXMLTxt, Path.Combine(sXMLFilePath, sXMLFileName), False) Then
                    Return False
                End If
                XMLString = File.ReadAllText(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")))
                XMLString = XMLString.Replace("&amp;", "&")
                XMLString = ReferenceCleanUp(XMLString)
                XMLString = XMLString.Replace("</nav-pointer-group>, <nav-pointer-group>", ", ").Replace("</nav-pointer-group>&#x2013;<nav-pointer-group>", "&#x2013;")

                GBL.DeantaBallon("Before Final cleanup", MessageType.MSGINFO)
                Try
                    FinalCleanup(bxslExecution)
                Catch ex As Exception
                    Return False
                End Try

                GBL.DeantaBallon("After Final cleanup", MessageType.MSGINFO)

                XMLString = XMLString.Replace("  ", " ")
                XMLString = XMLString.Replace("&#X2002;", "").Replace("&amp;#X2002;", "")

                File.WriteAllText(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")), XMLString)
                If File.Exists(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml"))) Then
                    My.Computer.FileSystem.DeleteFile(Path.Combine(sXMLFilePath, sXMLFileName))
                    My.Computer.FileSystem.RenameFile(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")), sXMLFileName)
                End If

                GBL.DeantaBallon("Before Add metadata", MessageType.MSGINFO)

                Try
                    XMLString = AddMetadataInformation(XMLString)
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try

                File.WriteAllText(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")), XMLString)
            Else
                ' EPUB xsl
                sXMLTxt = EpubCleanup(sXMLTxt)
                File.WriteAllText(Path.Combine(sXMLFilePath, sXMLFileName), sXMLTxt)
                'XSLPro(sXMLTxt, Path.Combine(sXMLFilePath, sXMLFileName), True)
            End If


            If File.Exists(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml"))) Then
                My.Computer.FileSystem.DeleteFile(Path.Combine(sXMLFilePath, sXMLFileName))
                My.Computer.FileSystem.RenameFile(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")), sXMLFileName)
            End If
            GBL.DeantaBallon("Merge and cleanup has been completed. Please check the xml file.", MessageType.MSGERROR)
            Return True
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
        Finally
            'frmMain.Close()
        End Try
    End Function

    Private Function RemoveEmptyElements(ByVal Contents As String) As String
        Dim EmptyElementList As New List(Of String)
        EmptyElementList.AddRange(New String() {"issuenum", "surname", "given-names", "firstname", "publisher-loc", "address", "source", "chapter-title", "title", "collab", "chapter-title", "issue", "volume", "token", "publishername"})
        For e As Int16 = 0 To EmptyElementList.Count - 1
            Dim Tag As String = EmptyElementList(e)
            Contents = Regex.Replace(Contents, $"<{Tag}[\s]*/>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            Contents = Regex.Replace(Contents, $"<{Tag}[^>]*>[\s]*</{Tag}>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
        Next
        Return Contents
    End Function

    Private Function AddMetadataInformation(ByVal FinalXML As String) As String
        Dim TNFMeta As New List(Of TNFMetaDataCollector)
        Dim OldMetaDataNode As XmlNode = Nothing
        xmlMeta.PreserveWhitespace = True
        TNFMeta.Add(New TNFMetaDataCollector With {.ElementName = "doi", .ElementType = TNFMetaDataType.DOI, .MetaSource = MetadataType.DB, .ElementDestXPath = "./book-id[@pub-id-type='doi']", .ElementSourceXPath = "", .DBColumnName = "doi"})
        TNFMeta.Add(New TNFMetaDataCollector With {.ElementName = "ebookisbn", .ElementType = TNFMetaDataType.eBookMasterISBN, .MetaSource = MetadataType.DB, .ElementDestXPath = "./isbn[@pub-type='ebk']", .ElementSourceXPath = "", .DBColumnName = "ebook_master"})
        TNFMeta.Add(New TNFMetaDataCollector With {.ElementName = "edition", .ElementType = TNFMetaDataType.Edition, .MetaSource = MetadataType.DB, .ElementDestXPath = "./edition", .ElementSourceXPath = "", .DBColumnName = "edition"})
        TNFMeta.Add(New TNFMetaDataCollector With {.ElementName = "hardbackisbn", .ElementType = TNFMetaDataType.PublisherName, .MetaSource = MetadataType.DB, .ElementDestXPath = "./isbn[@pub-type='hbk']", .ElementSourceXPath = "", .DBColumnName = "hardback_isbn"})
        TNFMeta.Add(New TNFMetaDataCollector With {.ElementName = "paperbackisbn", .ElementType = TNFMetaDataType.PublisherName, .MetaSource = MetadataType.DB, .ElementDestXPath = "./isbn[@pub-type='pbk']", .ElementSourceXPath = "", .DBColumnName = "paperback_isbn"})
        If ((TNFMeta Is Nothing) OrElse (TNFMeta.Count = 0)) Then
            GBL.DeantaBallon("No metadata info found.", MessageType.MSGERROR)
            Return FinalXML
        End If
        Try
            FinalXML = FinalXML.Replace("&amp;", "&#x0026;")
            FinalXML = FinalXML.Replace("&", "&amp;")
            xmlMeta.LoadXml(FinalXML)
        Catch ex As Exception
            GBL.DeantaBallon("AddMetadataInformation: " & ex.Message, MessageType.MSGERROR)
            Return FinalXML
        End Try

        Dim BookMetaNode As XmlNode = xmlMeta.SelectSingleNode("//book-meta")
        If (BookMetaNode Is Nothing) Then
            Return FinalXML
        End If
        OldMetaDataNode = BookMetaNode.CloneNode(True)
        'BookMetaNode.InnerXml = File.ReadAllText(GBL.TNFMetadataXML) & BookMetaNode.InnerXml
        Dim tblProject As New DataTable("project")
        tblProject = MySqlHelper.ReadSqlData($"select * from tb_projects where project_id={Me.ProjectID}")
        If ((tblProject Is Nothing) OrElse (tblProject.Rows Is Nothing) OrElse (tblProject.Rows.Count = 0)) Then
            GBL.DeantaBallon("No project information found.", MessageType.MSGERROR)
            Return FinalXML
        End If
        For i As Integer = 0 To TNFMeta.Count - 1
            Try
                Dim TmpNode As XmlNode = BookMetaNode.SelectSingleNode($"{TNFMeta(i).ElementDestXPath}")
                If (TmpNode IsNot Nothing) Then
                    If (Not String.IsNullOrEmpty(tblProject.Rows(0).Item($"{TNFMeta(i).DBColumnName}"))) Then
                        TmpNode.InnerText = tblProject.Rows(0).Item($"{TNFMeta(i).DBColumnName}")
                    Else
                        TmpNode.InnerText = TmpNode.InnerText.Replace("&#x2013;", "")
                    End If
                Else
                    GBL.DeantaBallon($"Could not able to find the xml and db columnname-{TNFMeta(i).DBColumnName}", MessageType.MSGERROR)
                End If
            Catch ex As Exception
                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            End Try
        Next

        Dim Impression As XmlNode = BookMetaNode.SelectSingleNode("./impression")
        If (Impression IsNot Nothing) Then
            Impression.InnerText = "1"
        End If

        Try
            AddAuthorInformation()
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message & "Fetch author node", MessageType.MSGERROR)
        End Try

        Try
            PublisherElement(OldMetaDataNode)
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message & "Fetch author node", MessageType.MSGERROR)
        End Try

        Try
            ISBNInformation()
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message & "ISBN Information", MessageType.MSGERROR)
        End Try

        Try
            GetKeywordInsertion(tblProject.Rows(0))
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message & "Keyword Information", MessageType.MSGERROR)
        End Try

        Try
            AddChapterAbstract()
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message & "AddChapterAbstract", MessageType.MSGERROR)
        End Try

        Return xmlMeta.OuterXml.Replace("&amp;", "&")
    End Function

    Private Function AddChapterAbstract() As Boolean
        Dim xmlAbst As XmlDocument = Nothing
        Try
            xmlAbst = LoadAbstractXML()
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
        End Try
        If (xmlAbst Is Nothing) Then
            Return False
        End If
        Dim BookPartNodes As XmlNodeList = xmlMeta.SelectNodes("//book-part/book-part-meta")
        Dim Chapterid As String = String.Empty
        Dim AbstNode As XmlNode = Nothing
        If ((BookPartNodes IsNot Nothing) AndAlso (BookPartNodes.Count > 0)) Then
            For b As Int16 = 0 To BookPartNodes.Count - 1
                Try
                    Chapterid = BookPartNodes(b).ParentNode.Attributes("id").Value
                Catch ex As Exception
                    Chapterid = String.Empty
                End Try
                If (String.IsNullOrEmpty(Chapterid)) Then Continue For
                Try
                    AbstNode = GetChapterAbtract(xmlAbst, Chapterid)
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
                Try
                    If (AbstNode Is Nothing) Then Continue For
                    BookPartNodes(b).InnerXml = BookPartNodes(b).InnerXml & AbstNode.OuterXml
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        End If
        Return True
    End Function

    Private Function GetChapterAbtract(ByVal xmlAbst As XmlDocument, ByVal ChapID As String) As XmlNode
        Dim ChapNodes As XmlNodeList = xmlAbst.SelectNodes("//section/title")
        If ((ChapNodes IsNot Nothing) AndAlso (ChapNodes.Count > 0)) Then
            For c As Int16 = 0 To ChapNodes.Count - 1
                If (String.Compare(ChapNodes(c).InnerText.Replace(" ", ""), ChapID, True) = 0) Then
                    Dim AbstNode As XmlNode = GetChildNode(ChapNodes(c).ParentNode, "abstract")
                    If (AbstNode IsNot Nothing) Then
                        Return AbstNode
                    End If
                End If
            Next
        End If
        Return Nothing
    End Function

    Private Function LoadAbstractXML() As XmlDocument
        Dim xmlAbst As New XmlDocument
        xmlAbst.PreserveWhitespace = True
        If (String.IsNullOrEmpty(Me.AbstractXML)) Then
            GBL.DeantaBallon("Book Abstract XML file is empty.", MessageType.MSGERROR)
            Return Nothing
        End If
        If (Not File.Exists(Me.AbstractXML)) Then
            GBL.DeantaBallon("Book Abstract XML file is not found.", MessageType.MSGERROR)
            Return Nothing
        End If
        Dim AbstrContent As String = File.ReadAllText(Me.AbstractXML)
        AbstrContent = Regex.Replace(Regex.Replace(AbstrContent, "(<!--)+(<!DOCTYPE([^>]+)>)(-->)+", "$2"), "(<!DOCTYPE([^>]+)>)", "<!--$1-->")
        Try
            xmlAbst.LoadXml(AbstrContent.Replace("&", "&amp;"))
        Catch ex As Exception
            GBL.DeantaBallon($"Load abstract XML{ex.Message}", MessageType.MSGERROR)
            Return Nothing
        End Try
        Return xmlAbst
    End Function
    Private Function GetKeywordInsertion(ByVal DtRow As DataRow) As Boolean
        If (DtRow Is Nothing) Then Return False
        If (String.IsNullOrEmpty(Convert.ToString(DtRow.Item("kwd_group")))) Then Return False
        Dim PermissionNode As XmlNode = xmlMeta.SelectSingleNode("//book-meta/permissions")
        If (PermissionNode IsNot Nothing) Then
            Try
                If (PermissionNode.ParentNode IsNot Nothing) Then
                    PermissionNode.ParentNode.InnerXml = PermissionNode.ParentNode.InnerXml.Replace(PermissionNode.OuterXml, $"{PermissionNode.OuterXml}<kwd-group>{Convert.ToString(DtRow.Item("kwd_group"))}</kwd-group>")
                End If
            Catch ex As Exception
                GBL.DeantaBallon("Error occurred while inserting the Book keyword information", MessageType.MSGERROR)
            End Try
        End If
        Return True
    End Function

    Private Function ISBNInformation() As Boolean
        Dim ISBNList As New List(Of ISBNData)
        ISBNList.Add(New ISBNData With {.IsExists = False, .PubType = "ebk"})
        ISBNList.Add(New ISBNData With {.IsExists = False, .PubType = "hbk"})
        ISBNList.Add(New ISBNData With {.IsExists = False, .PubType = "pbk"})
        ISBNList.Add(New ISBNData With {.IsExists = False, .PubType = "epub"})
        ISBNList.Add(New ISBNData With {.IsExists = False, .PubType = "mobi"})
        ISBNList.Add(New ISBNData With {.IsExists = False, .PubType = "web"})
        Dim Publisher As XmlNode = xmlMeta.SelectSingleNode("//publisher[last()]")
        Dim ISBNNodes As XmlNodeList = xmlMeta.SelectNodes("//isbn")
        If (Publisher Is Nothing) Then Return False
        If (Publisher.ParentNode Is Nothing) Then Return False
        If ((ISBNNodes Is Nothing) OrElse (ISBNNodes.Count = 0)) Then Return False
        Dim SourceNode As XmlNode = Nothing
        SourceNode = Publisher
        Dim PubType As String = String.Empty
        For s As Int16 = 0 To ISBNNodes.Count - 1
            Try
                PubType = ISBNNodes(s).Attributes("pub-type").Value
            Catch ex As Exception
                PubType = String.Empty
            End Try
            If (Not String.IsNullOrEmpty(PubType)) Then
                ISBNList.ForEach(Sub(ff)
                                     If (String.Compare(ff.PubType, PubType, True) = 0) Then
                                         ff.IsExists = True
                                     End If
                                 End Sub)
            End If
            If ((SourceNode IsNot Nothing) AndAlso (SourceNode.ParentNode IsNot Nothing)) Then
                SourceNode = SourceNode.ParentNode.InsertAfter(ISBNNodes(s), SourceNode)
            End If
        Next

        If (From m In ISBNList Where Not m.IsExists Select m).Any Then
            For k As Int16 = 0 To ISBNList.Count - 1
                If ISBNList(k).IsExists Then Continue For
                Dim IsbnNode As XmlNode = xmlMeta.CreateNode(XmlNodeType.Element, "isbn", "")
                Dim PubTypeAtt As XmlAttribute = xmlMeta.CreateNode(XmlNodeType.Attribute, "pub-type", "")
                PubTypeAtt.Value = ISBNList(k).PubType
                IsbnNode.Attributes.Append(PubTypeAtt)
                If ((SourceNode IsNot Nothing) AndAlso (SourceNode.ParentNode IsNot Nothing)) Then
                    SourceNode = SourceNode.ParentNode.InsertAfter(IsbnNode, SourceNode)
                End If
            Next
        End If
        Return True
    End Function
    Private Function PublisherElement(ByVal MetadatNode As XmlNode) As Boolean
        Dim IsSinglePublisher As Boolean = False
        Dim BookMetaNode As XmlNode = xmlMeta.SelectSingleNode("//book-meta")
        Dim PublisherNameNodes As XmlNodeList = BookMetaNode.SelectNodes("//publisher/publisher-name")
        Dim PublisherLocNodes As XmlNodeList = BookMetaNode.SelectNodes("//publisher/publisher-loc")
        Dim PublisherXML As String = String.Empty
        Dim PublisherName As String = String.Empty
        If ((PublisherNameNodes Is Nothing) OrElse (PublisherNameNodes.Count = 0)) Then
            Return False
        End If
        If (PublisherNameNodes.Count = 1) Then
            PublisherName = PublisherNameNodes(0).InnerText
            IsSinglePublisher = True
        Else
            If ((PublisherLocNodes IsNot Nothing) AndAlso (PublisherLocNodes.Count > 0)) Then
                If (PublisherLocNodes.Count <> PublisherNameNodes.Count) Then
                    GBL.DeantaBallon("Publisher loc And publisher name count Not matched.", MessageType.MSGERROR)
                    Return False
                End If
            End If
        End If
        If ((PublisherLocNodes Is Nothing) OrElse (PublisherLocNodes.Count = 0)) Then
            Return False
        End If
        For p As Integer = 0 To PublisherLocNodes.Count - 1
            If (IsSinglePublisher) Then
                PublisherXML = PublisherXML & $"<publisher><publisher-name>{PublisherName.Replace("&", "amp;")}</publisher-name><publisher-loc>{PublisherLocNodes(p).InnerText}</publisher-loc></publisher>"
            Else
                PublisherXML = PublisherXML & $"<publisher><publisher-name>{PublisherNameNodes(p).InnerText.Replace("&", "amp;")}</publisher-name><publisher-loc>{PublisherLocNodes(p).InnerText}</publisher-loc></publisher>"
            End If
        Next
        Dim Publisher As XmlNode = xmlMeta.SelectSingleNode("//publisher")
        If ((Publisher IsNot Nothing) AndAlso (Publisher.ParentNode IsNot Nothing)) Then
            Publisher.ParentNode.InnerXml = Publisher.ParentNode.InnerXml.Replace(Publisher.OuterXml, PublisherXML)
        End If
        Return True
    End Function

    Private Function AddAuthorInformation() As Boolean
        Dim AuthorXML As String = String.Empty
        Dim ContribNode As XmlNode = Nothing
        Dim AuMat As Match = Nothing
        ContribNode = xmlMeta.SelectSingleNode("//book-meta/contrib-group/contrib/name")
        AuthorXML = "<contrib-group content-type= ""author"">"
        If (ContribNode Is Nothing) Then
            Return False
        End If

        Dim AffNode As XmlNode = GetChildNode(ContribNode, "affiliation")
        If (AffNode IsNot Nothing) Then
            If (String.IsNullOrEmpty(AffNode.InnerText)) Then
                If (AffNode.ParentNode IsNot Nothing) Then
                    AffNode.ParentNode.RemoveChild(AffNode)
                End If
            End If
        End If

        Dim BioNode As XmlNode = xmlMeta.SelectSingleNode("//p[@role='author_bio']")
        If (BioNode IsNot Nothing) Then
            If (ContribNode.ParentNode IsNot Nothing) Then
                ContribNode.ParentNode.InnerXml = ContribNode.ParentNode.InnerXml.Replace(ContribNode.OuterXml, ContribNode.OuterXml & BioNode.OuterXml.Replace("<p role=""author_bio"">", "<bio id=""bio1"">").Replace("</p>", "</bio>"))
            End If
            If (BioNode.ParentNode IsNot Nothing) Then
                BioNode.ParentNode.RemoveChild(BioNode)
            End If
        End If

        Dim PermissionNode As XmlNode = xmlMeta.SelectSingleNode("//book-meta/permissions")
        Dim CopyYearNode As XmlNode = xmlMeta.SelectSingleNode("//permissions/copyright-year")
        If ((PermissionNode IsNot Nothing) AndAlso (PermissionNode.ParentNode IsNot Nothing)) Then
            If (PermissionNode.ParentNode.InnerXml.Contains("<pub-date><year>")) Then
                If ((CopyYearNode IsNot Nothing) AndAlso (Not String.IsNullOrEmpty(CopyYearNode.InnerText))) Then
                    PermissionNode.ParentNode.InnerXml = PermissionNode.ParentNode.InnerXml.Replace(PermissionNode.OuterXml, $"<pub-date><year>{CopyYearNode.InnerText}</year></pub-date>" & PermissionNode.OuterXml)
                Else
                    PermissionNode.ParentNode.InnerXml = PermissionNode.ParentNode.InnerXml.Replace(PermissionNode.OuterXml, "<pub-date><year>XXXXX</year></pub-date>" & PermissionNode.OuterXml)
                End If
            End If
        End If

        Dim ImprintMetaNode As XmlNode = xmlMeta.SelectSingleNode("//imprint-meta")
        Dim AbstrctNode As XmlNode = xmlMeta.SelectSingleNode("//abstract[@abstract-type='blurb']")
        If ((ImprintMetaNode IsNot Nothing) AndAlso (AbstrctNode IsNot Nothing)) Then
            If (AbstrctNode.ParentNode IsNot Nothing) Then
                AbstrctNode.ParentNode.InsertAfter(AbstrctNode, ImprintMetaNode)
            End If
        End If
        AbstrctNode = xmlMeta.SelectSingleNode("//abstract[@abstract-type='blurb']")
        If (AbstrctNode IsNot Nothing) Then
            If ((AbstrctNode.NextSibling IsNot Nothing) AndAlso (String.Compare(AbstrctNode.NextSibling.Name, "counts", True) <> 0)) Then
                If (AbstrctNode.ParentNode IsNot Nothing) Then
                    AbstrctNode.ParentNode.InnerXml = AbstrctNode.ParentNode.InnerXml.Replace(AbstrctNode.OuterXml, AbstrctNode.OuterXml & "<counts><fig-count count=""0""/><table-count count=""0""/><equation-count count=""0""/><ref-count count=""0""/><page-count count=""0""/></counts>")
                End If
            End If
        End If

        'For i As Integer = 0 To OldAuthorNodes.Count - 1
        '    AuthorXML = AuthorXML & "<contrib contrib-type=""author"">"
        '    AuMat = Regex.Match(OldAuthorNodes(i).InnerText, "([\w]+ [\w]{1,1}\.) ([\w]+)", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
        '    If (AuMat.Success) Then
        '        AuthorXML = AuthorXML & $"<name><surname>{AuMat.Groups(2).Value}</surname><given-names>{AuMat.Groups(1).Value}</given-names></name>"
        '    End If
        '    AuMat = Regex.Match(OldAuthorNodes(i).InnerText, "([\w]{1,1}\.[\w]{1,1}\.) ([\w]+)", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
        '    If (AuMat.Success) Then
        '        AuthorXML = AuthorXML & $"<name><surname>{AuMat.Groups(2).Value}</surname><given-names>{AuMat.Groups(1).Value}</given-names></name>"
        '    End If
        '    AuthorXML = AuthorXML & "</contrib>"
        'Next
        'AuthorXML = AuthorXML & "</contrib-group>"
        'Dim NewContribNode As XmlNode = xmlMeta.SelectSingleNode("//contrib-group")
        'If ((NewContribNode IsNot Nothing) AndAlso (NewContribNode.ParentNode IsNot Nothing)) Then
        '    NewContribNode.ParentNode.InnerXml = NewContribNode.ParentNode.InnerXml.Replace(NewContribNode.OuterXml, AuthorXML)
        'End If
        Return True
    End Function


    Private Function IdentifyFootnoteType(ByVal XContent As String) As Boolean
        Dim TmpDoc As New XmlDocument
        Dim Label As String = String.Empty
        XContent = Regex.Replace(XContent, "<book[^>]*>", "<book>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
        XContent = XContent.Replace("mml:", "mml").Replace("xlink:", "xlink")
        Try
            TmpDoc.LoadXml(XContent.Replace("&", "&amp;"))
        Catch ex As Exception
            GBL.DeantaBallon("IdentifyFootnoteType - " & ex.Message, MessageType.MSGERROR)
            Return False
        End Try
        Dim FootnoteNode As XmlNode = TmpDoc.SelectSingleNode("//xref[@ref-type='fn']") '
        If (FootnoteNode Is Nothing) Then
            Return False
        End If
        Try
            Label = FootnoteNode.Attributes("rid").Value
        Catch ex As Exception
            Label = String.Empty
        End Try
        If (String.IsNullOrEmpty(Label)) Then
            Return False
        End If
        Label = Label.Replace("fn", "")
        Dim XrefNode As XmlNode = TmpDoc.SelectSingleNode("//footnote[@role='end-bk-note'][@label='" & Label.Trim() & "']")
        If (XrefNode Is Nothing) Then
            Return False
        End If
        Return True
    End Function

    Private Function RemoveDuplicatePageNum(xmlPgDoc As XmlDocument) As Boolean
        Dim PageNum As String = String.Empty
        xmlPgDoc.PreserveWhitespace = True
        Dim AvailPageNums As New List(Of String)
        Dim PageTagList As XmlNodeList = xmlPgDoc.SelectNodes("//a[@id]")
        If ((PageTagList IsNot Nothing) AndAlso (PageTagList.Count > 0)) Then
            For p As Int16 = 0 To PageTagList.Count - 1
                PageNum = PageTagList(p).Attributes("id").Value.Replace("page_", "")
                If (AvailPageNums.Count = 0) Then
                    AvailPageNums.Add(PageNum)
                Else
                    If Not (From n In AvailPageNums Where String.Compare(n, PageNum, True) = 0 Select n).Any Then
                        AvailPageNums.Add(PageNum)
                    Else
                        If (PageTagList(p).ParentNode IsNot Nothing) Then
                            PageTagList(p).ParentNode.RemoveChild(PageTagList(p))
                        End If
                    End If
                End If
            Next
        End If
        Return True
    End Function


    Private Function old_RemoveDuplicatePageNum(ByVal xmlContent As String) As String
        Dim PageMt As Match = Nothing
        PageMt = Regex.Match(xmlContent, "(<book[^>]*>)(<a id=""page_[0-9xiv]+""[\s]+/>)(<chapter>)", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
        If (PageMt.Success) Then
            Dim PageMatCnt As MatchCollection = Regex.Matches(xmlContent, PageMt.Groups(2).Value, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            If ((PageMatCnt IsNot Nothing) AndAlso (PageMatCnt.Count > 0)) Then
                xmlContent = xmlContent.Replace(PageMt.Groups(2).Value, "")
                xmlContent = Regex.Replace(xmlContent, "(<title>)((?:(?:(?!<\/title>).)*)</title>(?:<subtitle>(?:(?:(?!<\/subtitle>).)*)</subtitle>)?<info>)", "$1" & PageMt.Groups(2).Value & "$2", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            End If
        End If

        'If (xmlContent.Contains("<chapter><index>")) Then
        '    xmlContent = Regex.Replace(xmlContent, "<book[^>]*><chapter><index>", "<back><index>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
        '    xmlContent = xmlContent.Replace("</index></chapter></book>", "</index></back>")
        'End If

        Return xmlContent
    End Function

    Private Function GetChildNode(ByVal ParNode As XmlNode, ByVal ChildName As String) As XmlNode
        If (String.Compare(ParNode.Name, ChildName, True) = 0) Then
            Return ParNode
        End If
        If ((ParNode IsNot Nothing) AndAlso (ParNode.ChildNodes IsNot Nothing) AndAlso (ParNode.ChildNodes.Count > 0)) Then
            For ch As Integer = 0 To ParNode.ChildNodes.Count - 1
                Dim ChNode As XmlNode = GetChildNode(ParNode.ChildNodes(ch), ChildName)
                If (ChNode IsNot Nothing) Then
                    Return ChNode
                End If
            Next
        End If
        Return Nothing
    End Function


    Private Function GruopBodyforChapter(ByVal xmlString As String) As String
        Dim xmlDoc As New XmlDocument
        Dim NameSpaceManager As New System.Xml.XmlNamespaceManager(xmlDoc.NameTable)
        NameSpaceManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")
        NameSpaceManager.AddNamespace("aid5", "http://ns.adobe.com/AdobeInDesign/5.0/")
        NameSpaceManager.AddNamespace("aid", "http://ns.adobe.com/AdobeInDesign/4.0/")
        NameSpaceManager.AddNamespace("xlink", "http://www.w3.org/1999/xlink/")
        NameSpaceManager.AddNamespace("xml", "http://www.w3.org/XML/1998/namespace")
        xmlDoc.PreserveWhitespace = True
        xmlDoc.XmlResolver = Nothing
        Dim ExcludeList As New List(Of String)
        ExcludeList.AddRange(New String() {"bibliography"})
        Try
            xmlDoc.LoadXml("<root xmlns:xlink=""http://www.w3.org/1999/xlink/"" xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:mml=""http://www.w3.org/1998/Math/MathML"">" & xmlString.Replace("&", "&amp;") & "</root>")
        Catch ex As Exception
            GBL.DeantaBallon("GruopBodyforChapter - " & ex.Message, MessageType.MSGERROR)
            Return xmlString
        End Try
        'Dim FirstSectionList As XmlNodeList = xmlDoc.SelectNodes("//chapter/section[1][preceding-sibling::*[1][local-name()='info']]|//chapter/section[1][preceding-sibling::*[1][local-name()!='info']]|//chapter/para[1][preceding-sibling::*[1][local-name()='info']]", NameSpaceManager)
        Dim FirstSectionList As XmlNodeList = xmlDoc.SelectNodes("//chapter/section[1]|//chapter/para[1]|//chapter/sidebar[1]", NameSpaceManager)
        Dim NextNode As XmlNode = Nothing
        Dim RemoveNodeList As New List(Of XmlNode)
        Dim SectionXML As String = String.Empty
        For i As Integer = 0 To FirstSectionList.Count - 1
            Dim SecNode As XmlNode = FirstSectionList(i)
            RemoveNodeList.Clear()
            SectionXML = SecNode.OuterXml
            NextNode = Nothing
            RemoveNodeList.Add(SecNode)
            Dim BodyNode As XmlNode = xmlDoc.CreateNode(XmlNodeType.Element, "body", "")
            If (SecNode.ParentNode IsNot Nothing) Then
                SecNode.ParentNode.InsertBefore(BodyNode, SecNode)
            End If
            If ((SecNode.NextSibling IsNot Nothing) AndAlso (String.Compare(SecNode.NextSibling.Name, "bibliography", True) <> 0)) Then
                NextNode = SecNode.NextSibling
                'SectionXML = SectionXML & NextNode.OuterXml
                While (NextNode IsNot Nothing)
                    If ((From n In ExcludeList Where String.Compare(n, NextNode.Name, True) = 0 Select n).Any) Then
                        Exit While
                    End If
                    SectionXML = SectionXML & NextNode.OuterXml
                    If (NextNode.NextSibling IsNot Nothing) Then
                        RemoveNodeList.Add(NextNode)
                        NextNode = NextNode.NextSibling
                    Else
                        Exit While
                    End If
                End While
                If (Not (From n In ExcludeList Where String.Compare(n, NextNode.Name, True) = 0 Select n).Any) Then
                    RemoveNodeList.Add(NextNode)
                End If
                BodyNode.InnerXml = SectionXML
                For Each ReNode As XmlNode In RemoveNodeList
                    ReNode.ParentNode.RemoveChild(ReNode)
                Next
            ElseIf ((SecNode.NextSibling IsNot Nothing) AndAlso (String.Compare(SecNode.NextSibling.Name, "bibliography", True) = 0)) Then
                BodyNode.InnerXml = SecNode.OuterXml
            End If
        Next

        Dim BackMatterNodes As XmlNodeList = xmlDoc.SelectNodes("//bibliography")
        Dim BackNode As XmlNode = Nothing
        If ((BackMatterNodes IsNot Nothing) AndAlso (BackMatterNodes.Count > 0)) Then
            For t As Integer = 0 To BackMatterNodes.Count - 1
                'If (BackMatterNodes(t).NextSibling Is Nothing) Then
                BackNode = xmlDoc.CreateNode(XmlNodeType.Element, "back", "")
                BackMatterNodes(t).ParentNode.InsertBefore(BackNode, BackMatterNodes(t))
                BackNode.AppendChild(BackMatterNodes(t))
                'End If
            Next
        End If

        BackMatterNodes = xmlDoc.SelectNodes("//fn-group")
        BackNode = Nothing
        If ((BackMatterNodes IsNot Nothing) AndAlso (BackMatterNodes.Count > 0)) Then
            For t As Integer = 0 To BackMatterNodes.Count - 1
                BackNode = xmlDoc.CreateNode(XmlNodeType.Element, "back", "")
                BackMatterNodes(t).ParentNode.InsertBefore(BackNode, BackMatterNodes(t))
                BackNode.AppendChild(BackMatterNodes(t))
            Next
        End If

        Dim xmlCnt As String = xmlDoc.OuterXml
        xmlCnt = Regex.Replace(xmlCnt.Replace("&amp;", "&"), "<root[^>]*>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Replace("</back><back>", "")
        xmlCnt = xmlCnt.Replace("</root>", "")
        Return xmlCnt
    End Function

    Private Function IndexXmlCleanup(ByVal XmlString As String) As String
        Dim xmlDoc As New XmlDocument
        Dim NameSpaceManager As New System.Xml.XmlNamespaceManager(xmlDoc.NameTable)
        NameSpaceManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")
        NameSpaceManager.AddNamespace("aid5", "http://ns.adobe.com/AdobeInDesign/5.0/")
        NameSpaceManager.AddNamespace("aid", "http://ns.adobe.com/AdobeInDesign/4.0/")
        NameSpaceManager.AddNamespace("xlink", "http://www.w3.org/1999/xlink/")
        NameSpaceManager.AddNamespace("xml", "http://www.w3.org/XML/1998/namespace")
        xmlDoc.PreserveWhitespace = True
        xmlDoc.XmlResolver = Nothing
        XmlString = XmlString.Replace(" xml:id=""""", "")
        XmlString = XmlString.Replace("<primary>", "<primary><term>").Replace("</primary>", "</term></primary>")
        XmlString = XmlString.Replace("<secondary>", "<secondary><term>").Replace("</secondary>", "</term></secondary>")
        XmlString = XmlString.Replace("<tertiary>", "<tertiary><term>").Replace("</tertiary>", "</term></tertiary>")

        Try
            xmlDoc.LoadXml("<inxd xmlns:xlink=""http://www.w3.org/1999/xlink/"" xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:mml=""http://www.w3.org/1998/Math/MathML"">" & XmlString.Replace("&", "&amp;") & "</inxd>")
        Catch ex As Exception
            GBL.DeantaBallon("IndexXmlCleanup -" & ex.Message, MessageType.MSGERROR)
            Return XmlString
        End Try



        Dim IndexTermNodes As XmlNodeList = xmlDoc.SelectNodes("//primary|//tertiary|//secondary")
        Dim DigitMatch As Match = Nothing
        If ((IndexTermNodes IsNot Nothing) AndAlso (IndexTermNodes.Count > 0)) Then
            For ix As Int32 = 0 To IndexTermNodes.Count - 1
                Try
                    DigitMatch = Regex.Match(IndexTermNodes(ix).InnerXml, "^<term><link[^>]*>[0-9.]+</link>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    If (DigitMatch.Success) Then
                        IndexTermNodes(ix).InnerXml = IndexTermNodes(ix).InnerXml.Replace(DigitMatch.Value, Regex.Replace(DigitMatch.Value, "<link[^>]*>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Replace("</link>", ""))
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        End If

        'figure alt
        If (Not IsePubXML) Then
            Dim FloatNodes As XmlNodeList = xmlDoc.SelectNodes("//imagedata[@alt]")
            Dim AltNode As XmlNode = Nothing
            If ((FloatNodes IsNot Nothing) AndAlso (FloatNodes.Count > 0)) Then
                For f As Int32 = 0 To FloatNodes.Count - 1
                    Try
                        AltNode = xmlDoc.CreateNode(XmlNodeType.Element, "alt-text", "")
                        AltNode.InnerText = FloatNodes(f).Attributes("alt").Value
                        'Dim ContentAttrib As XmlAttribute = xmlDoc.CreateAttribute("content-type", "")
                        'ContentAttrib.Value = "presentation"
                        'AltNode.Attributes.Prepend(ContentAttrib)
                    Catch ex As Exception
                        AltNode = Nothing
                    End Try
                    If (AltNode IsNot Nothing) Then
                        Dim FigureNode As XmlNode = FloatNodes(f).SelectSingleNode(".//ancestor::figure/caption")
                        If ((FigureNode IsNot Nothing) AndAlso (FigureNode.ParentNode IsNot Nothing)) Then
                            FigureNode.ParentNode.InsertAfter(AltNode, FigureNode)
                        End If
                        FloatNodes(f).Attributes.Remove(FloatNodes(f).Attributes("alt"))
                    End If
                Next
            End If
        End If

        'K441340
        Dim LinkList As XmlNodeList = xmlDoc.SelectNodes("//link[@role='bibr']")
        'Dim LinkList As XmlNodeList = xmlDoc.SelectNodes("//link")
        If ((LinkList IsNot Nothing) AndAlso (LinkList.Count > 0)) Then
            For l As Integer = 0 To LinkList.Count - 1
                Try
                    If (Regex.Match(LinkList(l).InnerText, "[0-9]+\.[0-9]+", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success) Then
                        If (LinkList(l).ParentNode IsNot Nothing) Then
                            LinkList(l).ParentNode.InnerXml = LinkList(l).ParentNode.InnerXml.Replace(LinkList(l).OuterXml, LinkList(l).InnerXml)
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        End If

        Try
            IndexSeeandSeealsoLinking(xmlDoc, "see also")
            IndexSeeandSeealsoLinking(xmlDoc, "see")
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
        End Try

        Dim IndexCon As String = String.Empty
        Dim Termid As String = String.Empty
        Dim IndexTerm As XmlNode = Nothing
        Dim OrgCon As String = String.Empty
        Dim index As Integer = 0
        Dim PrimaryList As XmlNodeList = Nothing
        'Dim PrimaryList As XmlNodeList = xmlDoc.SelectNodes("//primary|//tertiary|//secondary")
        'For pr As Integer = 0 To PrimaryList.Count - 1
        '    Dim PrimaryNode As XmlNode = PrimaryList(pr)
        '    If (PrimaryNode.ParentNode Is Nothing) Then Continue For
        '    Try
        '        If (PrimaryNode.InnerXml.Contains("see also")) Then
        '            index = PrimaryNode.InnerText.IndexOf("see")
        '            OrgCon = PrimaryNode.InnerText.Substring(index, (PrimaryNode.InnerText.Length - index))
        '            IndexCon = PrimaryNode.InnerText.Substring(index, (PrimaryNode.InnerText.Length - index)).Replace("see also ", "")
        '            If (Not String.IsNullOrEmpty(IndexCon)) Then
        '                Termid = FindPrimaryIndexID(xmlDoc, IndexCon, PrimaryNode.ParentNode)
        '                If (Not String.IsNullOrEmpty(Termid)) Then
        '                    Try
        '                        If (PrimaryNode.InnerXml.Contains("<emphasis role=""italic"">see also</emphasis> " & IndexCon)) Then
        '                            If (bXMLorEpub) Then
        '                                PrimaryNode.ParentNode.InnerXml = PrimaryNode.ParentNode.InnerXml.Replace("<emphasis role=""italic"">see also</emphasis> " & IndexCon, "<emphasis role=""italic"">see also</emphasis>" & " <see-also-entry rid=""" & Termid & """>" & IndexCon & "</see-also-entry>")
        '                            Else
        '                                If (PrimaryNode.ParentNode.InnerXml.Contains(", <emphasis role=""italic"">see also</emphasis> " & IndexCon & "</term>")) Then
        '                                    PrimaryNode.ParentNode.InnerXml = PrimaryNode.ParentNode.InnerXml.Replace(", <emphasis role=""italic"">see also</emphasis> " & IndexCon & "</term>", " </term><see-also-entry rid=""" & Termid & """>" & IndexCon & "</see-also-entry>")
        '                                ElseIf (PrimaryNode.ParentNode.InnerXml.Contains("; <emphasis role=""italic"">see also</emphasis> " & IndexCon & "</term>")) Then
        '                                    PrimaryNode.ParentNode.InnerXml = PrimaryNode.ParentNode.InnerXml.Replace("; <emphasis role=""italic"">see also</emphasis> " & IndexCon & "</term>", " </term><see-also-entry rid=""" & Termid & """>" & IndexCon & "</see-also-entry>")
        '                                End If
        '                            End If
        '                        End If
        '                    Catch ex As Exception
        '                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
        '                        Continue For
        '                    End Try
        '                    'PrimaryNode.InnerXml = PrimaryNode.InnerXml.Replace("<emphasis role=""italic"">see also</emphasis>", "see also").Replace("<emphasis role=""bold"">see also</emphasis>", "see also")
        '                    'PrimaryNode.InnerXml = PrimaryNode.InnerXml.Replace(OrgCon, "")
        '                    'PrimaryNode.ParentNode.InnerXml = PrimaryNode.ParentNode.InnerXml & "<see-also-entry rid=""" & Termid & """>" & OrgCon & "</see-also-entry>"
        '                Else
        '                    GBL.DeantaBallon("Could not able to find the <see-also-entry text " & OrgCon & " link id", MessageType.MSGERROR)
        '                    Continue For
        '                End If
        '            End If
        '        ElseIf (PrimaryNode.InnerXml.Contains("see")) Then
        '            index = PrimaryNode.InnerText.IndexOf("see")
        '            OrgCon = PrimaryNode.InnerText.Substring(index, (PrimaryNode.InnerText.Length - index))
        '            IndexCon = PrimaryNode.InnerText.Substring(index, (PrimaryNode.InnerText.Length - index)).Replace("see ", "")
        '            If (OrgCon.Contains("Intangible benefits")) Then
        '                MsgBox("OK")
        '            End If
        '            If (Not String.IsNullOrEmpty(IndexCon)) Then
        '                Termid = FindPrimaryIndexID(xmlDoc, IndexCon, PrimaryNode.ParentNode)
        '                If (Not String.IsNullOrEmpty(Termid)) Then
        '                    'PrimaryNode.InnerXml = PrimaryNode.InnerXml.Replace("<emphasis role=""italic"">see</emphasis>", "see").Replace("<emphasis role=""bold"">see</emphasis>", "see")
        '                    'PrimaryNode.InnerXml = PrimaryNode.InnerXml.Replace(OrgCon, "")
        '                    'PrimaryNode.ParentNode.InnerXml = PrimaryNode.ParentNode.InnerXml & "<see-entry rid=""" & Termid & """>" & OrgCon & "</see-entry>"
        '                    If (PrimaryNode.InnerXml.Contains("<emphasis role=""italic"">see</emphasis> " & IndexCon & "</term>")) Then
        '                        If (bXMLorEpub) Then
        '                            PrimaryNode.ParentNode.InnerXml = PrimaryNode.ParentNode.InnerXml.Replace("<emphasis role=""italic"">see</emphasis> " & IndexCon, "<emphasis role=""italic"">see</emphasis>" & " <see-entry rid=""" & Termid & """>" & IndexCon & "</see-entry>")
        '                        Else
        '                            PrimaryNode.ParentNode.InnerXml = PrimaryNode.ParentNode.InnerXml.Replace(", <emphasis role=""italic"">see</emphasis> " & IndexCon & "</term>", " </term><see-entry rid=""" & Termid & """>" & IndexCon & "</see-entry>")
        '                        End If
        '                    End If
        '                Else
        '                    GBL.DeantaBallon("Could not able to find the <see-entry text: [" & OrgCon & "]", MessageType.MSGERROR)
        '                    Continue For
        '                End If
        '            End If
        '        End If
        '    Catch ex As Exception
        '        Continue For
        '    End Try
        'Next
        Dim PrimaryLst As XmlNodeList = xmlDoc.SelectNodes("//primary|//secondary|//tertiary")
        If ((PrimaryLst IsNot Nothing) AndAlso (PrimaryLst.Count > 0)) Then
            For pg As Integer = 0 To PrimaryLst.Count - 1
                'If (PrimaryLst(pg).InnerXml.Contains(", <link")) Then
                Try
                    PrimaryLst(pg).InnerXml = Regex.Replace(PrimaryLst(pg).InnerXml.Replace("</term>", ""), "(<term>(((?!<link).)*))", "$1</term>", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Replace("</link>; <link role=""page"">", "</link>, <link role=""page"">")
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
                'End If
            Next
        End If

        If (Not IsePubXML) Then
            '06-09-2019 - ACOP , UNSP
            PrimaryList = xmlDoc.SelectNodes("//primary")
            Dim PrimaryXML As String = String.Empty
            Dim IsValid As Boolean = False
            Dim OrgPrimaryXML As String = String.Empty
            Dim MatchXML As String = String.Empty
            Dim TermXml As String = String.Empty
            For pg As Integer = 0 To PrimaryList.Count - 1
                PrimaryXML = String.Empty
                IsValid = False
                Try
                    If (Regex.Match(PrimaryList(pg).InnerXml, "(</link>)(;(?:(?!<link[^>]*>).)*)", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success) Then
                        If (PrimaryList(pg).InnerXml.EndsWith("</term>")) Then
                            GBL.DeantaBallon("Could not able to process the index term:" & PrimaryList(pg).InnerXml, MessageType.MSGERROR)
                            Continue For
                        End If
                        PrimaryXML = PrimaryList(pg).InnerXml
                        OrgPrimaryXML = PrimaryList(pg).InnerXml
                        Dim LinkRegMatches As MatchCollection = Regex.Matches(PrimaryLst(pg).InnerXml, "(</link>)(;(?:(?!<link[^>]*>).)*)", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                        For lk As Integer = 0 To LinkRegMatches.Count - 1
                            If (LinkRegMatches(lk).Groups(2).Value.Contains("</term>")) Then
                                Continue For
                            End If
                            MatchXML = LinkRegMatches(lk).Value
                            If ((Not String.IsNullOrEmpty(LinkRegMatches(lk).Groups(2).Value.TrimStart(" ;"))) Or (Not String.IsNullOrEmpty(LinkRegMatches(lk).Groups(2).Value.TrimStart("; ")))) Then
                                If (lk = 0) Then
                                    TermXml = String.Format("{0}<indexterm><secondary><term>{1}</term>", LinkRegMatches(lk).Groups(1).Value, LinkRegMatches(lk).Groups(2).Value.TrimStart(";").TrimStart(" "))
                                Else
                                    TermXml = String.Format("{0}</secondary></indexterm><indexterm><secondary><term>{1}</term>", LinkRegMatches(lk).Groups(1).Value, LinkRegMatches(lk).Groups(2).Value.TrimStart(";").TrimStart(" "))
                                End If
                                PrimaryXML = PrimaryXML.Replace(MatchXML, TermXml)
                                IsValid = True
                            End If
                        Next
                        If (IsValid) Then
                            PrimaryXML = PrimaryXML & "</secondary></indexterm>"
                            PrimaryLst(pg).InnerXml = PrimaryLst(pg).InnerXml.Replace(OrgPrimaryXML, PrimaryXML)
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next

            Dim MaxCnt As Integer = 0
            Dim Indexer As Integer = 0
            Dim TertiaryLst As XmlNodeList = xmlDoc.SelectNodes("//indexterm[parent::index]/tertiary")
            Dim TertiaryNode As XmlNode = Nothing
            Dim SecondaryNode As XmlNode = Nothing
            If ((TertiaryLst IsNot Nothing) AndAlso (TertiaryLst.Count > 0)) Then
                MaxCnt = TertiaryLst.Count
                Indexer = 0
                While ((TertiaryLst IsNot Nothing) And (TertiaryLst.Count > 0))
                    If (MaxCnt < Indexer) Then
                        Exit While
                    End If
                    TertiaryNode = TertiaryLst(0).ParentNode
                    If ((TertiaryNode.PreviousSibling IsNot Nothing) AndAlso (String.Compare(TertiaryNode.PreviousSibling.Name, "indexterm", True) = 0)) Then
                        SecondaryNode = GetChildNode(TertiaryNode.PreviousSibling, "secondary")
                        If (SecondaryNode IsNot Nothing) Then
                            SecondaryNode.AppendChild(TertiaryLst(0).ParentNode)
                        Else
                            SecondaryNode = GetChildNode(TertiaryNode.PreviousSibling, "primary")
                            If (SecondaryNode IsNot Nothing) Then
                                SecondaryNode.AppendChild(TertiaryLst(0).ParentNode)
                            End If
                        End If
                    End If
                    TertiaryLst = xmlDoc.SelectNodes("//indexterm[parent::index]/tertiary")
                    Indexer += 1
                End While
            End If

            Dim SecondaryLst As XmlNodeList = xmlDoc.SelectNodes("//indexterm[parent::index]/secondary")
            Dim PrimNode As XmlNode = Nothing
            SecondaryNode = Nothing
            If ((SecondaryLst IsNot Nothing) AndAlso (SecondaryLst.Count > 0)) Then
                MaxCnt = SecondaryLst.Count
                Indexer = 0
                While ((SecondaryLst IsNot Nothing) And (SecondaryLst.Count > 0))
                    If (MaxCnt < Indexer) Then
                        Exit While
                    End If
                    SecondaryNode = SecondaryLst(0).ParentNode
                    If ((SecondaryNode.PreviousSibling IsNot Nothing) AndAlso (String.Compare(SecondaryNode.PreviousSibling.Name, "indexterm", True) = 0)) Then
                        PrimNode = GetChildNode(SecondaryNode.PreviousSibling, "primary")
                        If (PrimNode IsNot Nothing) Then
                            PrimNode.AppendChild(SecondaryLst(0).ParentNode)
                        End If
                    End If
                    SecondaryLst = xmlDoc.SelectNodes("//indexterm[parent::index]/secondary")
                    Indexer += 1
                End While
            End If

        End If

        'need to be update
        'Dim PrimaryNodes As XmlNodeList = xmlDoc.SelectNodes("//primary[contains(text(),'; ')]")
        'Dim LevelTerms As New List(Of String)
        'If ((PrimaryNodes IsNot Nothing) AndAlso (PrimaryNodes.Count > 0)) Then
        '    For pm As Integer = 0 To PrimaryNodes.Count - 1
        '        LevelTerms.AddRange(PrimaryNodes(pm).InnerXml.Split(";"))
        '        If ((LevelTerms IsNot Nothing) AndAlso (LevelTerms.Count > 0)) Then
        '            If (LevelTerms.Count > 1) Then
        '                For ll = 1 To LevelTerms.Count - 1
        '                    PrimaryNodes(pm).InnerXml = PrimaryNodes(pm).InnerXml.Replace(LevelTerms(ll), String.Format("<secondary>{0}</secondary>", LevelTerms(ll)))
        '                Next
        '            End If
        '        End If
        '    Next
        'End If

        Dim UrlPages As XmlNodeList = xmlDoc.SelectNodes("//uri/a[@id]")
        If ((UrlPages IsNot Nothing) AndAlso (UrlPages.Count > 0)) Then
            For u As Int16 = 0 To UrlPages.Count - 1
                If ((UrlPages(u).ParentNode IsNot Nothing) AndAlso (UrlPages(u).ParentNode.ParentNode IsNot Nothing)) Then
                    UrlPages(u).ParentNode.ParentNode.InnerXml = UrlPages(u).ParentNode.ParentNode.InnerXml.Replace(UrlPages(u).OuterXml, "") & UrlPages(u).OuterXml
                End If
            Next
        End If

        Dim OutsidePageNodes As XmlNodeList = xmlDoc.SelectNodes("//inxd/a[@id]")
        If ((OutsidePageNodes IsNot Nothing) AndAlso (OutsidePageNodes.Count > 0)) Then
            For o As Int32 = 0 To OutsidePageNodes.Count - 1
                Try
                    If (OutsidePageNodes(o).NextSibling IsNot Nothing) Then
                        OutsidePageNodes(o).NextSibling.PrependChild(OutsidePageNodes(o))
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        End If


        Dim ListItems As XmlNodeList = xmlDoc.SelectNodes("//orderedlist/a[@id]|//itemizedlist/a[@id]")
        If ((ListItems IsNot Nothing) AndAlso (ListItems.Count > 0)) Then
            For lt As Integer = 0 To ListItems.Count - 1
                If (ListItems(lt).PreviousSibling IsNot Nothing) Then
                    ListItems(lt).PreviousSibling.AppendChild(ListItems(lt))
                End If
            Next
        End If



        Dim PageTagList As XmlNodeList = xmlDoc.SelectNodes("//a[@id]")
        Dim PrevCnt As Int32 = 0
        If ((PageTagList IsNot Nothing) AndAlso (PageTagList.Count > 0)) Then
            For pg As Integer = 0 To PageTagList.Count - 1
                PrevCnt = 0
                If (PageTagList(pg).PreviousSibling Is Nothing) OrElse (PageTagList(pg).PreviousSibling.NodeType <> XmlNodeType.Element) Then
                    Continue For
                End If
                Dim PrevNode As XmlNode = PageTagList(pg).PreviousSibling
                While (PrevCnt <= 4)
                    If ((PrevNode IsNot Nothing) AndAlso (String.Compare(PrevNode.Name, "para", True) = 0)) Then
                        PrevNode.AppendChild(PageTagList(pg))
                        Exit While
                    End If
                    If (PrevNode IsNot Nothing) Then
                        PrevNode = PrevNode.PreviousSibling
                    Else
                        Exit While
                    End If
                    PrevCnt = PrevCnt + 1
                End While
            Next
        End If


        PageTagList = xmlDoc.SelectNodes("//a[@id]")
        PrevCnt = 0
        If ((PageTagList IsNot Nothing) AndAlso (PageTagList.Count > 0)) Then
            For pg As Integer = 0 To PageTagList.Count - 1
                PrevCnt = 0
                If (PageTagList(pg).PreviousSibling Is Nothing) OrElse (PageTagList(pg).PreviousSibling.NodeType <> XmlNodeType.Element) Then
                    Continue For
                End If
                Dim PrevNode As XmlNode = PageTagList(pg).PreviousSibling
                While (PrevCnt <= 1)
                    If ((PrevNode IsNot Nothing) AndAlso (String.Compare(PrevNode.Name, "title", True) = 0)) Then
                        PrevNode.AppendChild(PageTagList(pg))
                        Exit While
                    End If
                    If (PrevNode IsNot Nothing) Then
                        PrevNode = PrevNode.PreviousSibling
                    Else
                        Exit While
                    End If
                    PrevCnt = PrevCnt + 1
                End While
            Next
        End If

        Dim IncludeList As New List(Of String)
        If (IsePubXML) Then
            IncludeList.AddRange(New String() {"chapter", "dedication", "preface"})
        Else
            IncludeList.AddRange(New String() {"title", "chapter", "dedication", "preface"})
        End If
        'IncludeList.AddRange(New String() {"title", "chapter", "dedication", "preface"})
        PageTagList = xmlDoc.SelectNodes("//a[@id]")
        PrevCnt = 0
        If ((PageTagList IsNot Nothing) AndAlso (PageTagList.Count > 0)) Then
            For pg As Integer = 0 To PageTagList.Count - 1
                PrevCnt = 0
                If (PageTagList(pg).NextSibling Is Nothing) OrElse (PageTagList(pg).NextSibling.NodeType <> XmlNodeType.Element) Then
                    Continue For
                End If
                Dim PrevNode As XmlNode = PageTagList(pg).NextSibling
                While (PrevCnt <= 4)
                    If ((PrevNode IsNot Nothing) AndAlso ((From n In IncludeList Where (String.Compare(PrevNode.Name, n, True) = 0) Select n).Any)) Then
                        PrevNode.AppendChild(PageTagList(pg))
                        Exit While
                    End If
                    If (PrevNode IsNot Nothing) Then
                        PrevNode = PrevNode.NextSibling
                    Else
                        Exit While
                    End If
                    PrevCnt = PrevCnt + 1
                End While
            Next
        End If

        PageTagList = xmlDoc.SelectNodes("//a[@id]")
        PrevCnt = 0
        If ((PageTagList IsNot Nothing) AndAlso (PageTagList.Count > 0)) Then
            For pg As Integer = 0 To PageTagList.Count - 1
                PrevCnt = 0
                If (PageTagList(pg).NextSibling Is Nothing) OrElse (PageTagList(pg).NextSibling.NodeType <> XmlNodeType.Element) Then
                    Continue For
                End If
                Dim PrevNode As XmlNode = PageTagList(pg).NextSibling
                While (PrevCnt <= 4)
                    If ((PrevNode IsNot Nothing) AndAlso (String.Compare(PrevNode.Name, "para", True) = 0)) Then
                        PrevNode.AppendChild(PageTagList(pg))
                        Exit While
                    End If
                    If (PrevNode IsNot Nothing) Then
                        PrevNode = PrevNode.NextSibling
                    Else
                        Exit While
                    End If
                    PrevCnt = PrevCnt + 1
                End While
            Next
        End If

        PageTagList = xmlDoc.SelectNodes("//bibliography/a[@id]")
        Dim PagePrevNode As XmlNode = Nothing
        Dim PageXML As String = String.Empty
        If ((PageTagList IsNot Nothing) AndAlso (PageTagList.Count > 0)) Then
            For at As Integer = 0 To PageTagList.Count - 1

                Try
                    If ((PageTagList(at).PreviousSibling IsNot Nothing) AndAlso (PageTagList(at).PreviousSibling.ParentNode IsNot Nothing)) Then
                        PagePrevNode = PageTagList(at).PreviousSibling
                        PageXML = PageTagList(at).OuterXml
                        If ((PageTagList(at).ParentNode IsNot Nothing) AndAlso (String.Compare(PageTagList(at).ParentNode.Name, "bibliography", True) <> 0)) Then
                            PageTagList(at).ParentNode.RemoveChild(PageTagList(at))
                            PagePrevNode.ParentNode.InnerXml = PagePrevNode.ParentNode.InnerXml.Replace(PagePrevNode.InnerXml, PagePrevNode.InnerXml & PageXML)
                        ElseIf ((PageTagList(at).ParentNode IsNot Nothing) AndAlso (String.Compare(PageTagList(at).ParentNode.Name, "bibliography", True) = 0)) Then
                            PagePrevNode = PageTagList(at).NextSibling
                            If (PagePrevNode IsNot Nothing) Then
                                PageTagList(at).ParentNode.RemoveChild(PageTagList(at))
                                PagePrevNode.InnerXml = PageXML & PagePrevNode.InnerXml
                            End If
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        End If

        Dim IsbnNode As XmlNode = xmlDoc.SelectSingleNode("//biblioset[@role='isbns']")
        Dim BibliosetNode As XmlNode = xmlDoc.SelectSingleNode("//biblioset[@role='publisher']")
        If ((IsbnNode IsNot Nothing) AndAlso (BibliosetNode IsNot Nothing)) Then
            If (BibliosetNode.ParentNode IsNot Nothing) Then
                BibliosetNode.ParentNode.InsertBefore(IsbnNode, BibliosetNode)
            End If
        End If

        If (Not IsePubXML) Then

            Dim IndexPageNodes As XmlNodeList = xmlDoc.SelectNodes("//index//a[@id]")
            Dim LastPageNumber As String = String.Empty
            If ((IndexPageNodes IsNot Nothing) AndAlso (IndexPageNodes.Count > 0)) Then
                LastPageNumber = IndexPageNodes(IndexPageNodes.Count - 1).Attributes("id").Value
            End If
            If (Not String.IsNullOrEmpty(LastPageNumber)) Then
                LastPageNumber = LastPageNumber.Replace("page_", "")
                LastPageNumber = Convert.ToInt16(LastPageNumber) + 10
            End If
            Dim IndexNode As XmlNodeList = xmlDoc.SelectNodes("//indexterm")
            If ((IndexNode IsNot Nothing) AndAlso (IndexNode.Count > 0)) Then
                For d As Int16 = 0 To IndexNode.Count - 1
                    IndexNode(d).InnerXml = Regex.Replace(IndexNode(d).InnerXml, "(<a id=""page_[0-9xiv]+""[\s]+/>)", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                Next
            End If
            Dim PrimaryNodeList As XmlNodeList = xmlDoc.SelectNodes("//primary")
            If ((PrimaryNodeList IsNot Nothing) AndAlso (PrimaryNodeList.Count > 0)) Then
                For p As Int16 = 0 To PrimaryNodeList.Count - 1
                    Dim UnWantedPages As XmlNodeList = PrimaryNodeList(p).SelectNodes("//link[@role='page']")
                    If ((UnWantedPages IsNot Nothing) AndAlso (UnWantedPages.Count > 0)) Then
                        For w As Int16 = 0 To UnWantedPages.Count - 1
                            If (String.IsNullOrEmpty(UnWantedPages(w).InnerText)) Then Continue For
                            Try
                                If (Not Regex.Match(UnWantedPages(w).InnerText, "[a-zA-Z,\./]+", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success) Then
                                    If (Convert.ToInt32(UnWantedPages(w).InnerText) > LastPageNumber) Then
                                        If (PrimaryNodeList(p) IsNot Nothing) Then
                                            PrimaryNodeList(p).InnerXml = PrimaryNodeList(p).InnerXml.Replace(UnWantedPages(w).OuterXml, UnWantedPages(w).InnerXml)
                                        End If
                                    End If
                                End If
                            Catch ex As Exception
                                GBL.DeantaBallon($"{UnWantedPages(w).InnerText} - {ex.Message} - remove unwanted page number in index.", MessageType.MSGERROR)
                                Continue For
                            End Try
                        Next
                    End If
                Next
            End If
        End If

        Dim TableNodes As XmlNodeList = xmlDoc.SelectNodes("//table/a[@id]")
        If ((TableNodes IsNot Nothing) AndAlso (TableNodes.Count > 0)) Then
            For t As Int16 = 0 To TableNodes.Count - 1
                If ((TableNodes(t).ParentNode IsNot Nothing) AndAlso ((TableNodes(t).ParentNode.ParentNode IsNot Nothing))) Then
                    TableNodes(t).ParentNode.ParentNode.InnerXml = TableNodes(t).ParentNode.ParentNode.InnerXml.Replace(TableNodes(t).ParentNode.OuterXml, TableNodes(t).ParentNode.OuterXml.Replace(TableNodes(t).OuterXml, "") & TableNodes(t).OuterXml)
                End If
            Next
        End If

        XmlString = xmlDoc.OuterXml.Replace("&amp;", "&").Replace("</inxd>", "").Replace("<inxd xmlns:xlink=""http://www.w3.org/1999/xlink/"" xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:mml=""http://www.w3.org/1998/Math/MathML"">", "")
        If ((Regex.Match(XmlString, "<chapter[^>]*><index>", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Success) And (XmlString.Contains("</index></chapter>"))) Then
            XmlString = Regex.Replace(XmlString, "<chapter[^>]*><index>", "<back><index>", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Replace("</index></chapter>", "</index></back>")
        ElseIf ((Regex.Match(XmlString, "(<chapter[^>]*>)(<a[^>]*/>)(<index>)", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Success) And (XmlString.Contains("</index></chapter>"))) Then
            XmlString = Regex.Replace(XmlString, "(<chapter[^>]*>)(<a[^>]*/>)(<index>)", "<back>$2<index>", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Replace("</index></chapter>", "</index></back>")
        End If
        XmlString = XmlString.Replace("</back><back>", "")
        Return XmlString
    End Function

    Private Function MoveFootnotes(ByVal FtXmlContent As String) As String
        Dim xmlFtNt As New XmlDocument
        xmlFtNt.PreserveWhitespace = True
        Try
            xmlFtNt.LoadXml(FtXmlContent.Replace("&", "&amp;").Replace("xmlns=""http://docbook.org/ns/docbook""", ""))
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            Return FtXmlContent
        End Try
        Dim MaxCount As Int16 = 0
        Dim FootnoteIndex As Int16 = 0
        Dim FootnoteList As XmlNodeList = xmlFtNt.SelectNodes("//footnote[@linkend]")
        Dim FootnoteID As String = String.Empty
        Dim Label As String = String.Empty
        If ((FootnoteList IsNot Nothing) AndAlso (FootnoteList.Count > 0)) Then
            MaxCount = FootnoteList.Count * 3
            While (((FootnoteList IsNot Nothing) AndAlso (FootnoteList.Count > 0)))
                If (FootnoteIndex > MaxCount) Then
                    Exit While
                End If
                Try
                    Dim FtNode As XmlNode = FootnoteList(0)
                    Try
                        FootnoteID = FtNode.Attributes("linkend").Value
                    Catch ex As Exception
                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                        Continue While
                    End Try
                    Try
                        Label = FtNode.Attributes("label").Value
                    Catch ex As Exception
                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                        Continue While
                    End Try
                    If (String.IsNullOrEmpty(FootnoteID)) Then
                        GBL.DeantaBallon("xml:id attribute value not found. Please check." & FtNode.OuterXml, MessageType.MSGERROR)
                        Continue While
                    End If
                    Dim SecFtNote As XmlNode = GetFootnoteSection(FootnoteID, Label, False)
                    If (SecFtNote Is Nothing) Then
                        GBL.DeantaBallon("linkend id found, but the respective footnote xml:id not found." & FootnoteID, MessageType.MSGERROR)
                        Continue While
                    End If
                    If ((String.IsNullOrEmpty(FtNode.InnerText)) OrElse (String.IsNullOrEmpty(SecFtNote.InnerText))) Then
                        Try
                            GBL.DeantaBallon("footntoe removed: " & FtNode.OuterXml, MessageType.MSGERROR)
                            Try
                                FtNode.ParentNode.RemoveChild(FtNode)
                                SecFtNote.ParentNode.RemoveChild(SecFtNote)
                            Catch ex As Exception
                                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                            End Try
                        Catch ex As Exception
                            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                        End Try
                    End If

                    Try
                        Dim ParaXml As String = Regex.Match(SecFtNote.InnerXml, "<para[^>]*>", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Value
                        FtNode.InnerXml = Regex.Replace(SecFtNote.InnerXml, $"^({ParaXml})({Label})", "$1", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    Catch ex As Exception
                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                        Continue While
                    End Try
                    Try
                        FtNode.Attributes.Remove(FtNode.Attributes("linkend"))
                    Catch ex As Exception
                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    End Try
                    If (FtNode.Attributes("xml:id") Is Nothing) Then
                        Try
                            Dim xmlIDAttrib As XmlAttribute = xmlFtNt.CreateNode(XmlNodeType.Attribute, "xml:id", "xml")
                            xmlIDAttrib.Value = FootnoteID
                            FtNode.Attributes.Append(xmlIDAttrib)
                        Catch ex As Exception
                            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                        End Try
                        If (FtNode.ParentNode IsNot Nothing) Then
                            FtNode.ParentNode.InnerXml = FtNode.ParentNode.InnerXml.Replace(FtNode.OuterXml, $"<xref ref-type=""fn"" rid=""fn{Label}""><sup>{Label}</sup></xref>{FtNode.OuterXml}")
                        End If
                    End If
                    SecFtNote.ParentNode.RemoveChild(SecFtNote)
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue While
                End Try
                FootnoteList = xmlFtNt.SelectNodes("//footnote[@linkend]")
                FootnoteIndex = FootnoteIndex + 1
            End While
        End If
        Dim ChapterNodes As XmlNodeList = xmlFtNt.SelectNodes("//chapter|//preface|//acknowledgements")
        Dim FnGroupXML As String = ""
        If ((ChapterNodes IsNot Nothing) AndAlso (ChapterNodes.Count > 0)) Then
            For c As Int16 = 0 To ChapterNodes.Count - 1
                Dim FoonoteNodes As XmlNodeList = ChapterNodes(c).SelectNodes(".//footnote")
            Next
        End If
        Return xmlFtNt.OuterXml.Replace("&amp;", "&")
    End Function

    Private Function GetFootnoteSection(ByVal FootNoteID As String, ByVal Label As String, ByVal IsBasedOnLabel As Boolean) As XmlNode
        Dim xmlSeFec As New XmlDocument
        xmlSeFec.PreserveWhitespace = True
        Dim NameSpaceManager As New System.Xml.XmlNamespaceManager(xmlSeFec.NameTable)
        NameSpaceManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")
        NameSpaceManager.AddNamespace("aid5", "http://ns.adobe.com/AdobeInDesign/5.0/")
        NameSpaceManager.AddNamespace("aid", "http://ns.adobe.com/AdobeInDesign/4.0/")
        NameSpaceManager.AddNamespace("xlink", "http://www.w3.org/1999/xlink/")
        NameSpaceManager.AddNamespace("xml", "http://www.w3.org/XML/1998/namespace")
        Dim FtXMLNode As XmlNode = Nothing
        Dim sFtID As String = String.Empty
        Dim ParticalID As String = String.Empty
        Dim footnoteList As XmlNodeList = Nothing
        Try
            xmlSeFec.LoadXml(File.ReadAllText(NotesChapter).Replace("&", "&amp;").Replace("xmlns=""http://docbook.org/ns/docbook""", ""))
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            Return Nothing
        End Try
        If (IsBasedOnLabel) Then
            Try
                ParticalID = FootNoteID.Split("_")(0)
            Catch ex As Exception
                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                Return Nothing
            End Try
            footnoteList = xmlSeFec.SelectNodes($"//footnote[contains(@xml:id,'{ParticalID}') and @label='{Label}']", NameSpaceManager)
            If ((footnoteList IsNot Nothing) AndAlso (footnoteList.Count > 0)) Then
                Return footnoteList(0)
            End If
        Else
            footnoteList = xmlSeFec.SelectNodes($"//footnote[@xml:id='{FootNoteID}']", NameSpaceManager)
            If ((footnoteList IsNot Nothing) AndAlso (footnoteList.Count > 0)) Then
                Return footnoteList(0)
            End If
        End If
        Return Nothing
    End Function

    Private Function IndexSeeandSeealsoLinking(ByVal xmlIndex As XmlDocument, ByVal IndexSeeText As String) As Boolean
        Dim PrimaryList As XmlNodeList = xmlIndex.SelectNodes("//primary")
        If ((PrimaryList Is Nothing) OrElse (PrimaryList.Count = 0)) Then Return False
        Dim IndexTerms As New List(Of IndexSeeData)
        Dim DestIndexID As String = String.Empty
        Dim IndexSeeIndex As Int32 = 0
        For p As Int32 = 0 To PrimaryList.Count - 1
            If (Not PrimaryList(p).InnerText.Contains(IndexSeeText)) Then Continue For
            DestIndexID = Nothing
#If CONFIG = "FinalXML" Then
            'If (PrimaryList(p).InnerText.Contains("angiotensin converting")) Then
            '    MsgBox("OK")
            'End If
#End If
            Try
                IndexTerms = New_SplitIndexTerms(PrimaryList(p), IndexSeeText)
            Catch ex As Exception
                GBL.DeantaBallon($"New_SplitIndexTerms-{ex.Message}", MessageType.MSGERROR)
                Continue For
            End Try
            If ((IndexTerms Is Nothing) OrElse (IndexTerms.Count = 0)) Then Continue For
            'IndexSeeIndex = PrimaryList(p).InnerXml.IndexOf(IndexSeeText)
            Dim PrimaryXML As String = PrimaryList(p).InnerXml
            For t As Int16 = IndexTerms.Count - 1 To 0 Step -1
                Dim IndexTerm As String = IndexTerms(t).SeeTerm
                IndexSeeIndex = IndexTerms(t).SeeIndex
                If (IndexSeeIndex = -1) Then Continue For
                Try
                    DestIndexID = FindDestinationIndexTerm(xmlIndex, IndexTerm)
                Catch ex As Exception
                    DestIndexID = Nothing
                    GBL.DeantaBallon($"{ex.Message} - {IndexTerm}", MessageType.MSGERROR)
                    Continue For
                End Try
                Try
                    If (String.IsNullOrEmpty(DestIndexID)) Then
                        Continue For
                        GBL.DeantaBallon($"No destination found for the see term: [{IndexTerm}]", MessageType.MSGERROR)
                    End If
                    'Dim SearchRegex As New Regex(IndexTerm.Replace("(", "\(").Replace(")", "\)"), RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    Dim SearchRegex As New Regex(Regex.Escape(IndexTerm), RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                    PrimaryXML = SearchRegex.Replace(PrimaryXML, $"<see-entry rid=""{DestIndexID}"">{IndexTerm}</see-entry>", 1, IndexSeeIndex)
                    PrimaryList(p).InnerXml = PrimaryXML
                Catch ex As Exception
                    GBL.DeantaBallon($"IndexSeeandSeealsoLinking - {ex.Message}", MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        Next
        Return True
    End Function

    Private Function FindDestinationIndexTerm(ByVal xmlIndex As XmlDocument, ByVal IndexTerm As String) As String
        If (xmlIndex Is Nothing) Then Return String.Empty
        If (String.IsNullOrEmpty(IndexTerm)) Then Return String.Empty
        Dim PrimaryNodes As XmlNodeList = xmlIndex.SelectNodes("//primary")
        If ((PrimaryNodes Is Nothing) OrElse (PrimaryNodes.Count = 0)) Then Return String.Empty
        Dim TmpIndxTerm As String = IndexTerm.Trim().ToLower().Replace("\[", "[").Replace("\]", "]").Replace("\(", "(").Replace("\)", ")")
        For u As Int16 = 0 To PrimaryNodes.Count - 1
            If (PrimaryNodes(u).InnerText.ToLower().StartsWith(TmpIndxTerm)) Then
                If (PrimaryNodes(u).ParentNode Is Nothing) Then
                    Return String.Empty
                End If
                Try
                    Return PrimaryNodes(u).ParentNode.Attributes("xml:id").Value
                Catch ex As Exception
                    Return String.Empty
                End Try
            End If
        Next
        Return String.Empty
    End Function

    Private Function New_SplitIndexTerms(ByVal PrimaryNode As XmlNode, ByVal IndexSeeText As String) As List(Of IndexSeeData)
        Dim NodeText As String = PrimaryNode.InnerText
        Dim IndexTermList As New List(Of IndexSeeData)
        Dim IsPageExistsInBetween As Boolean = False
        Dim SeeTextSplit As New List(Of String)
        Dim SeeXMLIndex As Int16 = 0
        Dim NodeXML As String = PrimaryNode.InnerXml
        Dim SeeTextList As New List(Of String)
        'If (NodeText.Contains("National New-type Urbanization Plan")) Then
        '    MsgBox("OK")
        'End If
        If (Not NodeText.Contains(IndexSeeText)) Then Return Nothing
        SeeTextList = SplitBySeeAlso(NodeText, IndexSeeText)
        If ((SeeTextList IsNot Nothing) AndAlso (SeeTextList.Count > 0)) Then
            For s As Int16 = 0 To SeeTextList.Count - 1
                Try
                    Dim SeeText As String = SeeTextList(s)
                    SeeText = HtmlDecode(SeeText)
                    SeeTextSplit = New List(Of String)
                    IsPageExistsInBetween = False
                    If (SeeText.Contains(";") Or SeeText.Contains(",")) Then
                        If (SeeText.Contains(";") And SeeText.Contains(",")) Then
                            SeeTextSplit.AddRange(SeeText.Split(New Char() {";"}))
                        ElseIf (SeeText.Contains(",")) Then
                            SeeTextSplit.AddRange(SeeText.Split(New Char() {","}))
                        ElseIf (SeeText.Contains(";")) Then
                            SeeTextSplit.AddRange(SeeText.Split(New Char() {";"}))
                        End If
                        If ((SeeTextSplit IsNot Nothing) AndAlso (SeeTextSplit.Count > 0)) Then
                            For t As Int16 = 0 To SeeTextSplit.Count - 1
                                If (Regex.Match(SeeTextSplit(t), "x2013", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success) Then
                                    IsPageExistsInBetween = True
                                    Continue For
                                End If
                                If (Regex.Match(SeeTextSplit(t), "[0-9]+$", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success) Then
                                    IsPageExistsInBetween = True
                                    Continue For
                                End If
                                If (Regex.Match(SeeTextSplit(t), "&#x201C", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success) Then
                                    IsPageExistsInBetween = True
                                    Continue For
                                End If
                                If (Regex.Match(SeeTextSplit(t), "[0-9]+ \, [0-9]+", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success) Then
                                    IsPageExistsInBetween = True
                                    Continue For
                                End If
                                'skip the last see term for page number found in between index terms
                                If ((IsPageExistsInBetween) And (t = (SeeTextSplit.Count - 1))) Then
                                    Continue For
                                End If
                                Dim TmpSee As String = HtmlEncode(SeeTextSplit(t))
                                TmpSee = TmpSee.Replace("&", "&amp;")
                                If (SeeTextSplit(t).Contains(IndexSeeText)) Then
                                    SeeXMLIndex = Regex.Match(NodeXML, TmpSee.Replace(IndexSeeText, $"<emphasis role=""italic"">( )?{IndexSeeText}</emphasis>"), RegexOptions.Singleline Or RegexOptions.IgnoreCase).Index
                                    'SeeXMLIndex = NodeXML.IndexOf(TmpSee.Replace(IndexSeeText, $"<emphasis role=""italic"">( )?{IndexSeeText}</emphasis>"))
                                Else
                                    SeeXMLIndex = NodeXML.IndexOf(TmpSee)
                                End If
                                If (SeeXMLIndex <> -1) Then
                                    IndexTermList.Add(New IndexSeeData With {.SeeTerm = TmpSee.Replace($"{IndexSeeText} ", "").Trim(), .SeeIndex = SeeXMLIndex})
                                End If
                            Next
                        End If
                    Else
                        SeeText = HtmlEncode(SeeText)
                        'SeeXMLIndex = NodeXML.IndexOf(SeeText.Replace(IndexSeeText, $"<emphasis role=""italic"">{IndexSeeText}</emphasis>"))
                        'SeeText = SeeText.Replace("[", "\[").Replace("]", "\]").Replace("(", "\(").Replace(")", "\)")
                        SeeXMLIndex = Regex.Match(NodeXML, Regex.Escape(SeeText.Replace(IndexSeeText, $"<emphasis role=""italic"">( )?{IndexSeeText}</emphasis>")), RegexOptions.Singleline Or RegexOptions.IgnoreCase).Index
                        If (SeeXMLIndex <> -1) Then
                            IndexTermList.Add(New IndexSeeData With {.SeeTerm = SeeText.Replace($"{IndexSeeText} ", "").Trim(), .SeeIndex = SeeXMLIndex})
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon($"New_SplitIndexTerms - {ex.Message}", MessageType.MSGERROR)
                End Try
            Next
        End If
        Return IndexTermList
    End Function

    Public Function HtmlDecode(text As String) As String
        Dim Input As String = String.Empty
        Input = text.ToString().Replace("&lt;", "<").Replace("&gt;", ">")
        For Each Dit As System.Text.RegularExpressions.Match In Regex.Matches(Input, "(&#x)([0-9A-F]{1,4})(;)")
            If (Dit.Success) Then
                Input = Input.Replace(Dit.Value, String.Format("{1}", Dit.Groups(1).Value, ChrW(Integer.Parse(Dit.Groups(2).Value, Globalization.NumberStyles.HexNumber))))
            End If
        Next
        Return Input
    End Function

    Public Function HtmlEncode(text As String) As String
        Dim Input As String = String.Empty
        Dim chars As Char() = HttpUtility.HtmlEncode(text).ToCharArray()
        Dim result As New StringBuilder(text.Length + CInt(text.Length * 0.1))

        For Each c As Char In chars
            Dim value As Integer = Convert.ToInt32(c)
            If value > 127 Then
                result.AppendFormat("&#x{0};", Hex(value).PadLeft(4, "0"))
            Else
                result.Append(c)
            End If
        Next
        Input = result.ToString().Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", """")
        For Each Dit As System.Text.RegularExpressions.Match In Regex.Matches(result.ToString(), "(&#)([0-9]{1,3})(;)")
            If (Dit.Success) Then
                Input = Input.Replace(Dit.Value, String.Format("{0}x{1}{2}", Dit.Groups(1).Value, Hex(Dit.Groups(2).Value).PadLeft(4, "0000"), Dit.Groups(3).Value))
            End If
        Next
        Return Input
    End Function

    Private Function SplitBySeeAlso(ByVal NodeText As String, FindText As String) As List(Of String)
        Dim SeeAlsoList As New List(Of String)

        Dim SeeAlsoIndexList As New List(Of Int16)
        Dim Index As Int16 = 0
        Dim StartPos As Int16 = 0
        Dim StartIndex As Int16 = 0
        Dim EndIndex As Int16 = 0
        'Index = NodeText.IndexOf(FindText, StartPos)
        While (Index <> -1)
            Index = NodeText.IndexOf(FindText, StartPos)
            If (Index <> -1) Then
                SeeAlsoIndexList.Add(Index)
            End If
            StartPos = (Index + FindText.Length)
        End While
        If ((SeeAlsoIndexList IsNot Nothing) AndAlso (SeeAlsoIndexList.Count > 0)) Then
            Select Case SeeAlsoIndexList.Count
                Case 1
                    SeeAlsoList.Add(NodeText.Substring(SeeAlsoIndexList(0), (NodeText.Length - SeeAlsoIndexList(0))))
                Case > 1
                    For e As Int16 = 0 To SeeAlsoIndexList.Count - 1
                        StartIndex = Convert.ToInt16(SeeAlsoIndexList(e))
                        If (e = SeeAlsoIndexList.Count - 1) Then
                            EndIndex = NodeText.Length
                        Else
                            EndIndex = Convert.ToInt16(SeeAlsoIndexList(e + 1))
                        End If
                        SeeAlsoList.Add(NodeText.Substring(StartIndex, (EndIndex - StartIndex)))
                    Next
            End Select
        End If
        Return SeeAlsoList
    End Function
    Private Function SplitIndexTerms(ByVal PrimaryNode As XmlNode, ByVal IndexSeeText As String) As List(Of IndexSeeData)
        Dim NodeText As String = PrimaryNode.InnerText
        Dim NodeXML As String = PrimaryNode.InnerXml
        Dim IsSeeFound As Boolean = False
        Dim SeeXMLIndex As Integer = 0
        Dim IndexTermList As New List(Of IndexSeeData)
        Dim SeeSplitList As New List(Of String)
        Dim SeeTextIndex As Int16 = 0
        Dim IndexTerm As String = String.Empty
        If (Not NodeText.Contains(IndexSeeText)) Then Return Nothing
        SeeSplitList.AddRange(Regex.Split(NodeText, ";"))
        If ((SeeSplitList IsNot Nothing) AndAlso (SeeSplitList.Count > 0)) Then
            SeeTextIndex = 0
            IsSeeFound = False
            For t As Int32 = 0 To SeeSplitList.Count - 1
                SeeTextIndex = SeeSplitList(t).IndexOf(IndexSeeText)
                If ((SeeTextIndex = -1) And (Not IsSeeFound)) Then
                    Continue For
                End If
                IsSeeFound = True
                IndexTerm = SeeSplitList(t).Substring(SeeTextIndex, (SeeSplitList(t).Length - SeeTextIndex))
                If String.IsNullOrEmpty(IndexTerm) Then Continue For
                SeeXMLIndex = NodeXML.IndexOf(IndexTerm.Replace(IndexSeeText, $"<emphasis role=""italic"">{IndexSeeText}</emphasis>"))
                IndexTerm = IndexTerm.Replace($"{IndexSeeText} ", "")
                If (Not IndexTerm.Contains(";")) Then
                    IndexTermList.Add(New IndexSeeData With {.SeeTerm = IndexTerm, .SeeIndex = SeeXMLIndex})
                Else
                    IndexTerm.Split(";").ToList().ForEach(Sub(sp)
                                                              IndexTermList.AddRange(New IndexSeeData With {.SeeTerm = sp, .SeeIndex = SeeXMLIndex})
                                                          End Sub)
                End If
            Next
        Else
            SeeTextIndex = NodeText.IndexOf(IndexSeeText)
            If (SeeTextIndex = -1) Then Return Nothing
            IndexTerm = NodeText.Substring(SeeTextIndex, (NodeText.Length - SeeTextIndex))
            If String.IsNullOrEmpty(IndexTerm) Then Return Nothing
            SeeXMLIndex = NodeText.IndexOf(IndexTerm)
            IndexTerm = IndexTerm.Replace($"{IndexSeeText} ", "")
            If (Not IndexTerm.Contains(";")) Then
                IndexTermList.Add(New IndexSeeData With {.SeeTerm = IndexTerm, .SeeIndex = SeeXMLIndex})
            Else
                IndexTerm.Split(";").ToList().ForEach(Sub(sp)
                                                          IndexTermList.AddRange(New IndexSeeData With {.SeeTerm = sp, .SeeIndex = SeeXMLIndex})
                                                      End Sub)
            End If
        End If
        If ((IndexTermList IsNot Nothing) AndAlso (IndexTermList.Count > 0)) Then
            Return IndexTermList
        End If
        Return Nothing
    End Function

    Private Function ReferenceCleanUp(ByVal XMLString As String) As String
        Dim xmlDoc As New XmlDocument
        Dim NameSpaceManager As New System.Xml.XmlNamespaceManager(xmlDoc.NameTable)
        NameSpaceManager.AddNamespace("xsi", "http: //www.w3.org/2001/XMLSchema-instance")
        NameSpaceManager.AddNamespace("aid5", "http://ns.adobe.com/AdobeInDesign/5.0/")
        NameSpaceManager.AddNamespace("aid", "http://ns.adobe.com/AdobeInDesign/4.0/")
        NameSpaceManager.AddNamespace("xlink", "http://www.w3.org/1999/xlink/")
        NameSpaceManager.AddNamespace("xml", "http://www.w3.org/XML/1998/namespace")
        xmlDoc.PreserveWhitespace = True
        xmlDoc.XmlResolver = Nothing
        Try
            xmlDoc.LoadXml(XMLString.Replace("&", "&amp;"))
        Catch ex As Exception
            GBL.DeantaBallon("ReferenceCleanUp - " & ex.Message, MessageType.MSGERROR)
            Return XMLString
        End Try
        Dim BiblioList As XmlNodeList = xmlDoc.SelectNodes("//mixed-citation")
        For bb As Integer = 0 To BiblioList.Count - 1
            Try
                'If (Regex.IsMatch(BiblioList(bb).InnerXml, "<source>[\s]*<italic>") AndAlso (BiblioList(bb).InnerXml.Contains("</italic></source>"))) Then
                '    BiblioList(bb).InnerXml = Regex.Replace(BiblioList(bb).InnerXml, "<source>[\s]*<italic>", "<source>", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Replace("</italic></source>", "</source>").Replace("</italic>, <italic>", ", ")
                'End If

                BiblioList(bb).InnerXml = BiblioList(bb).InnerXml.Replace("<italic>", "").Replace("</italic>", "").Replace("et al.", "<etal>et al.</etal>")
                BiblioList(bb).InnerXml = Regex.Replace(BiblioList(bb).InnerXml, "<label([^><]+)?>(((?!</label>).)+)</label>", AddressOf LabelPunctuation, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                If (BiblioList(bb).InnerXml.Contains("<publisher-loc>") Or BiblioList(bb).InnerXml.Contains("<publisher-name>") Or BiblioList(bb).InnerXml.Contains("<chapter-title>")) Then

                    If (BiblioList(bb).InnerXml.Contains("<title><italic>") AndAlso BiblioList(bb).InnerXml.Contains("</italic></title>")) Then
                        BiblioList(bb).InnerXml = Regex.Replace(BiblioList(bb).InnerXml, "<title[^>]*>[\s]*<italic>", "<source>").Replace("</italic></title>", "</source>")
                    Else
                        BiblioList(bb).InnerXml = BiblioList(bb).InnerXml.Replace("<title>", "<chapter-title>").Replace("</title>", "</chapter-title>")
                    End If
                    Try
                        BiblioList(bb).Attributes("publication-type").Value = "book"
                    Catch ex As Exception
                        Dim xmlSTr As XmlAttribute = xmlDoc.CreateAttribute("publication-type")
                        xmlSTr.Value = "book"
                        BiblioList(bb).Attributes.Append(xmlSTr)
                        Continue For
                    End Try
                ElseIf (BiblioList(bb).InnerXml.Contains("<volume") Or BiblioList(bb).InnerXml.Contains("<issue>") Or BiblioList(bb).InnerXml.Contains("article-title")) Then
                    If (BiblioList(bb).InnerXml.Contains("<title><italic>") AndAlso BiblioList(bb).InnerXml.Contains("</italic></title>")) Then
                        BiblioList(bb).InnerXml = Regex.Replace(BiblioList(bb).InnerXml, "<title[^>]*>[\s]*<italic>", "<source>").Replace("</italic></title>", "</source>")
                    Else
                        BiblioList(bb).InnerXml = BiblioList(bb).InnerXml.Replace("<title>", "<article-title>").Replace("</title>", "</article-title>")
                    End If
                    Try
                        BiblioList(bb).Attributes("publication-type").Value = "journal"
                    Catch ex As Exception
                        Dim xmlSTr As XmlAttribute = xmlDoc.CreateAttribute("publication-type")
                        xmlSTr.Value = "journal"
                        BiblioList(bb).Attributes.Append(xmlSTr)
                        Continue For
                    End Try
                Else
                    Try
                        BiblioList(bb).Attributes("publication-type").Value = "other"
                    Catch ex As Exception
                        Dim xmlSTr As XmlAttribute = xmlDoc.CreateAttribute("publication-type")
                        xmlSTr.Value = "other"
                        BiblioList(bb).Attributes.Append(xmlSTr)
                        Continue For
                    End Try
                End If
            Catch ex As Exception
                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                Continue For
            End Try
        Next

        Dim SourceList As XmlNodeList = xmlDoc.SelectNodes("//source")


        'Dim StringNameList As XmlNodeList = xmlDoc.SelectNodes("//string-name")
        'For st As Integer = 0 To StringNameList.Count - 1
        '    If ((StringNameList(st).InnerXml.Contains("<surname>")) And (StringNameList(st).InnerXml.Contains("<given-names>"))) Then
        '        Dim SurName As String = Regex.Match(StringNameList(st).InnerXml, "(<surname([^><]+)?>(?:(?:(?!</surname>).)+)</surname>)", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Value
        '        If (SurName.Contains(".")) Then
        '            StringNameList(st).InnerXml = StringNameList(st).InnerXml.Replace("<surname>", "<surnameee>").Replace("</surname>", "</surnameee>")
        '            StringNameList(st).InnerXml = StringNameList(st).InnerXml.Replace("<given-names>", "<surname>").Replace("</given-names>", "</surname>").Replace("<surnameee>", "<given-names>").Replace("</surnameee>", "</given-names>")
        '        End If
        '        StringNameList(st).InnerXml = StringNameList(st).InnerXml.Replace(".</surname>", "</surname>.").Replace(".,</given-names>", ".</given-names>,")
        '    End If
        'Next

        BiblioList = xmlDoc.SelectNodes("//mixed-citation/label")
        If ((BiblioList IsNot Nothing) AndAlso (BiblioList.Count > 0)) Then
            For b As Int32 = 0 To BiblioList.Count - 1
                BiblioList(b).InnerText = BiblioList(b).InnerText.Trim()
            Next
        End If


        XMLString = xmlDoc.OuterXml.Replace("&amp;", "&").Replace("</inxd>", "").Replace("<inxd xmlns:xlink=""http://www.w3.org/1999/xlink/"">", "")
        XMLString = XMLString.Replace("<tp>", "").Replace("</tp>", "<break/>").Replace("<tp />", "")
        XMLString = XMLString.Replace("<break/></p>", "</p>")
        'XMLString = Regex.Replace(XMLString, "(</[^>]*>)?(</[^>]*>)(<a>.*?</a>)", AddressOf PageNumberCleanUp, RegexOptions.Singleline Or RegexOptions.IgnoreCase)
        Return XMLString
    End Function

    Private Function LabelPunctuation(ByVal Mt As Match) As String
        Dim sResult As String = Mt.Value.Replace(".", "")
        Return sResult
    End Function

    Private Function PageNumberCleanUp(ByVal Mt As Match) As String
        Dim sResult As String = Mt.Groups(1).Value & Mt.Groups(2).Value
        Dim sOrg As String = Mt.Value
        If (sResult.Contains("</p>") Or sResult.Contains("</sec>") Or sResult.Contains("</attrib>")) Then
            sResult = Mt.Groups(3).Value & Mt.Groups(2).Value & Mt.Groups(1).Value
        ElseIf sResult.Contains("</ref>") Then
            sResult = Mt.Groups(3).Value & Mt.Groups(1).Value & Mt.Groups(2).Value
        Else
            Return Mt.Value
        End If
        sOrg = sOrg.Replace(sOrg, sResult)
        Return sOrg
    End Function

    Private Function FindPrimaryIndexID(ByVal xmlDoc As XmlDocument, ByVal SeeContent As String, ByVal TermNode As XmlNode) As String
        Dim SeeNodeList As XmlNodeList = Nothing
        Dim IndexTerm As XmlNode = Nothing
        SeeNodeList = xmlDoc.SelectNodes("//primary")
        If ((SeeNodeList Is Nothing) OrElse (SeeNodeList.Count = 0)) Then
            Return String.Empty
        End If
        For pp As Integer = 0 To SeeNodeList.Count - 1
            Try
                If (SeeNodeList(pp).InnerText.Contains(SeeContent)) Then
                    If (SeeNodeList(pp).InnerText.Contains("see " & SeeContent)) Then
                        Continue For
                    End If
                    If (SeeNodeList(pp).InnerText.Contains("see also " & SeeContent)) Then
                        Continue For
                    End If
                    If (SeeNodeList(pp).ParentNode IsNot Nothing) AndAlso (TermNode.OuterXml <> SeeNodeList(pp).ParentNode.OuterXml) Then
                        IndexTerm = SeeNodeList(pp).ParentNode
                        Return IndexTerm.Attributes("xml:id").Value
                    End If
                End If
            Catch ex As Exception
                Continue For
            End Try
        Next
        Return String.Empty
    End Function

    Private Function TandFXmlCleanup(ByVal XmlString As String) As String
        Dim xmlDoc As New XmlDocument
        xmlDoc.PreserveWhitespace = True
        xmlDoc.XmlResolver = Nothing
        XmlString = XmlString.Replace("xmlns=""http://docbook.org/ns/docbook""", "")
        While (XmlString.Contains(vbLf))
            XmlString = XmlString.Replace(vbLf, "")
        End While

        Try
            If (Not IsePubXML) Then
                XmlString = XmlString.Replace("<LRH>", "<alt-title alt-title-type=""running-head-verso"">").Replace("</LRH>", "</alt-title>")
                XmlString = XmlString.Replace("<RRH>", "<alt-title alt-title-type=""running-head-recto"">").Replace("</RRH>", "</alt-title>")
            End If

            XmlString = XmlString.Replace("<superscript></superscript>", "").Replace("<superscript>.</superscript>", ".")
            If (Regex.Match(XmlString, "</superscript></link><superscript>[0-9]+", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Success) Then
                XmlString = Regex.Replace(XmlString, "</superscript></link><superscript>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                XmlString = Regex.Replace(XmlString, "</superscript><link[^>]*><superscript>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            End If

            XmlString = Regex.Replace(XmlString, "<dialogue[^>]*>(((?!<\/dialogue>).)*)</dialogue>", Function(mt As Match)
                                                                                                         Dim TmpDialogue As String = mt.Value
                                                                                                         TmpDialogue = TmpDialogue.Replace("<entity>" & vbTab & "</entity>", " ")
                                                                                                         TmpDialogue = TmpDialogue.Replace("<line role=""noindent"">", "<line>")
                                                                                                         TmpDialogue = TmpDialogue.Replace(vbTab, "")
                                                                                                         TmpDialogue = TmpDialogue.Replace("</speaker> <line", "</speaker><line")
                                                                                                         TmpDialogue = TmpDialogue.Replace("<line>", "<speech><line>").Replace("</line>", "</line></speech>")
                                                                                                         TmpDialogue = TmpDialogue.Replace("<speaker>", "<speech><speaker>").Replace("</speaker>", "</speaker></speech>")
                                                                                                         TmpDialogue = TmpDialogue.Replace("</speech><speech><line>", "<line>").Replace("</speaker></speech><speech>", "</speaker>")
                                                                                                         Return TmpDialogue
                                                                                                     End Function, RegexOptions.Singleline Or RegexOptions.IgnoreCase)
        Catch ex As Exception
            GBL.DeantaBallon("TandFXmlCleanup - " & ex.Message, MessageType.MSGERROR)
            Return XmlString
        End Try



        Try
            xmlDoc.LoadXml(XmlString.Replace("&", "&amp;"))
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message & " - TandFXmlCleanup function is not working", MessageType.MSGERROR)
            Return XmlString
        End Try

        Dim NameSpaceManager As New System.Xml.XmlNamespaceManager(xmlDoc.NameTable)
        NameSpaceManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")
        NameSpaceManager.AddNamespace("aid5", "http://ns.adobe.com/AdobeInDesign/5.0/")
        NameSpaceManager.AddNamespace("aid", "http://ns.adobe.com/AdobeInDesign/4.0/")
        NameSpaceManager.AddNamespace("xlink", "http://www.w3.org/1999/xlink/")
        NameSpaceManager.AddNamespace("mml", "http://www.w3.org/1998/Math/MathML")
        NameSpaceManager.AddNamespace("xml", "http://www.w3.org/XML/1998/namespace")

        'check whether the URL is valid.
        'Dim uriResult As Uri
        Dim Urls As XmlNodeList = xmlDoc.SelectNodes("//link/uri")
        Dim result As Boolean = False
        If ((Urls IsNot Nothing) AndAlso (Urls.Count > 0)) Then
            For u As Int16 = 0 To Urls.Count - 1
                Try
                    'result = Uri.TryCreate(Urls(u).InnerText, UriKind.Absolute, uriResult) And (uriResult.Scheme = Uri.UriSchemeHttp Or uriResult.Scheme = Uri.UriSchemeHttps Or uriResult.Scheme = Uri.UriSchemeFtp Or uriResult.Scheme = Uri.UriSchemeFile Or uriResult.Scheme = Uri.UriSchemeNntp)
                    'result = Uri.IsWellFormedUriString(Urls(u).InnerText, UriKind.Absolute)
                    result = urlExists(Urls(u).InnerText)
                Catch ex As Exception
                    result = False
                End Try
                Try
                    If (Not result) Then
                        If (Urls(u).ParentNode IsNot Nothing) Then
                            GBL.DeantaBallon($"Invalid url found: {Urls(u).InnerXml}", MessageType.MSGERROR)
                            Urls(u).ParentNode.InnerXml = Urls(u).ParentNode.InnerXml.Replace(Urls(u).OuterXml, Urls(u).InnerXml)
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon($"Invalid url valdiation.{ex.Message}", MessageType.MSGERROR)
                End Try
            Next
        End If

        Try
            RemoveDuplicatePageNum(xmlDoc)
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
        End Try

        Dim IndexLinkNodes As XmlNodeList = xmlDoc.SelectNodes("//index//link[not (@role)]")
        If ((IndexLinkNodes IsNot Nothing) AndAlso (IndexLinkNodes.Count > 0)) Then
            For x As Integer = 0 To IndexLinkNodes.Count - 1
                Dim PageAttrib As XmlAttribute = xmlDoc.CreateAttribute("role", "")
                PageAttrib.Value = "page"
                IndexLinkNodes(x).Attributes.Prepend(PageAttrib)
            Next
        End If

        Dim EmphasisLst As XmlNodeList = xmlDoc.SelectNodes("//emphasis[not(@role)]")
        If ((EmphasisLst IsNot Nothing) AndAlso (EmphasisLst.Count > 0)) Then
            For m As Int16 = 0 To EmphasisLst.Count - 1
                Dim RoleAttrib As XmlAttribute = xmlDoc.CreateNode(XmlNodeType.Attribute, "role", "")
                RoleAttrib.Value = "italic"
                EmphasisLst(m).Attributes.Append(RoleAttrib)
            Next
        End If

        Dim CourierNewLst As XmlNodeList = xmlDoc.SelectNodes("//emphasis[@role='courier New']")
        If ((CourierNewLst IsNot Nothing) AndAlso (CourierNewLst.Count > 0)) Then
            While ((CourierNewLst IsNot Nothing) AndAlso (CourierNewLst.Count > 0))
                Dim CourierNode As XmlNode = CourierNewLst(0)
                Try
                    If (CourierNode.ParentNode IsNot Nothing) Then
                        CourierNode.ParentNode.InnerXml = CourierNode.ParentNode.InnerXml.Replace(CourierNode.OuterXml, CourierNode.InnerXml)
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
                CourierNewLst = xmlDoc.SelectNodes("//emphasis[@role='courier New']")
            End While
        End If

        Dim TableParaList As XmlNodeList = xmlDoc.SelectNodes("//tp")
        If ((TableParaList IsNot Nothing) AndAlso (TableParaList.Count > 0)) Then
            For t As Int32 = 0 To TableParaList.Count - 1
                Try
                    If (TableParaList(t).ParentNode IsNot Nothing) Then
                        If (String.Compare(TableParaList(t).ParentNode.InnerXml, TableParaList(t).OuterXml, True) = 0) Then
                            TableParaList(t).ParentNode.InnerXml = TableParaList(t).ParentNode.InnerXml.Replace(TableParaList(t).OuterXml, TableParaList(t).InnerXml)
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        End If

        Dim PrevCnt As Int32 = 0
        Dim PageTagList As XmlNodeList = xmlDoc.SelectNodes("//book/a[@id]|//info/a[@id]|//part/a[@id]")
        Dim ChapterPartNode As XmlNode = Nothing
        If ((PageTagList IsNot Nothing) AndAlso (PageTagList.Count > 0)) Then
            ChapterPartNode = xmlDoc.SelectSingleNode("//preface/para[1]|//primary[1]|//chapter/title|//part/title|//preface/title|//index/title")
            If (ChapterPartNode IsNot Nothing) Then
                For pg As Integer = PageTagList.Count - 1 To 0 Step -1
                    ChapterPartNode.PrependChild(PageTagList(pg))
                Next
            End If
        End If

        PageTagList = xmlDoc.SelectNodes("//indexterm/a[@id]")
        If ((PageTagList IsNot Nothing) AndAlso (PageTagList.Count > 0)) Then
            For pg As Integer = PageTagList.Count - 1 To 0 Step -1
                Try
                    ChapterPartNode = PageTagList(pg).ParentNode.SelectSingleNode(".//primary")
                    If (ChapterPartNode IsNot Nothing) Then
                        ChapterPartNode.PrependChild(PageTagList(pg))
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        End If


        Dim SeparatorLst As New List(Of String)
        SeparatorLst.AddRange(New String() {"//section/title", "//listitem/para"})
        For s As Int32 = 0 To SeparatorLst.Count - 1
            Dim ElemLst As XmlNodeList = xmlDoc.SelectNodes(SeparatorLst(s))
            For e As Int32 = 0 To ElemLst.Count - 1
                Try
                    ElemLst(e).InnerXml = ElemLst(e).InnerXml.Replace("&amp;#x2002;", " ").Replace("&amp;#x2003;", "")
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        Next

        Dim BibliomixedList As XmlNodeList = xmlDoc.SelectNodes("//Bibliomixed[@role=""""]", NameSpaceManager)
        Dim titleNode As XmlNode = Nothing
        If ((BibliomixedList IsNot Nothing) AndAlso (BibliomixedList.Count > 0)) Then
            For i As Integer = 0 To BibliomixedList.Count - 1
                titleNode = BibliomixedList(i).SelectSingleNode("./title[@role='booktitle']")
                If (titleNode IsNot Nothing) Then
                    Try
                        BibliomixedList(i).Attributes("role").Value = "book"
                    Catch ex As Exception
                    End Try
                Else
                    titleNode = BibliomixedList(i).SelectSingleNode("./title[@role='chapter-title']")
                    If (titleNode IsNot Nothing) Then
                        Try
                            BibliomixedList(i).Attributes("role").Value = "book"
                        Catch ex As Exception
                        End Try
                    End If
                End If
                titleNode = BibliomixedList(i).SelectSingleNode("./bibliomset[@relation='journal']")
                If (titleNode IsNot Nothing) Then
                    BibliomixedList(i).Attributes("role").Value = "journal"
                End If
            Next
        End If

        Dim IncludeList As New List(Of String)
        IncludeList.AddRange(New String() {"title", "chapter", "dedication", "preface"})
        PageTagList = xmlDoc.SelectNodes("//a[@id]")
        PrevCnt = 0
        If ((PageTagList IsNot Nothing) AndAlso (PageTagList.Count > 0)) Then
            For pg As Integer = 0 To PageTagList.Count - 1
                PrevCnt = 0
                If (PageTagList(pg).NextSibling Is Nothing) OrElse (PageTagList(pg).NextSibling.NodeType <> XmlNodeType.Element) Then
                    Continue For
                End If
                Dim PrevNode As XmlNode = PageTagList(pg).NextSibling
                While (PrevCnt <= 1)
                    If ((PrevNode IsNot Nothing) AndAlso ((From n In IncludeList Where (String.Compare(PrevNode.Name, n, True) = 0) Select n).Any)) Then
                        If (PrevNode.InnerXml.StartsWith("<a id=""")) Then
                            PrevNode.AppendChild(PageTagList(pg))
                        Else
                            PrevNode.PrependChild(PageTagList(pg))
                        End If

                        Exit While
                    End If
                    If (PrevNode IsNot Nothing) Then
                        PrevNode = PrevNode.NextSibling
                    Else
                        Exit While
                    End If
                    PrevCnt = PrevCnt + 1
                End While
            Next
        End If

        If (Not IsePubXML) Then
            BibliomixedList = xmlDoc.SelectNodes("//bibliomixed[@role=""proceedings""]", NameSpaceManager)
            Try
                ChangeAttributeName(BibliomixedList, "role", "book")
            Catch ex As Exception
                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            End Try
        End If

        'Move sub list into previous para 
        Dim SubItemLst As XmlNodeList = xmlDoc.SelectNodes("//para[parent::listitem][following-sibling::orderedlist]")
        If ((SubItemLst IsNot Nothing) AndAlso (SubItemLst.Count > 0)) Then
            For s As Integer = 0 To SubItemLst.Count - 1
                Try
                    If ((SubItemLst(s).NextSibling IsNot Nothing) AndAlso (String.Compare(SubItemLst(s).NextSibling.Name, "orderedlist", True) = 0)) Then
                        SubItemLst(s).AppendChild(SubItemLst(s).NextSibling)
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        End If


        If (IsePubXML) Then
            Dim SpeakerLst As XmlNodeList = xmlDoc.SelectNodes("//speaker")
            If ((SpeakerLst IsNot Nothing) AndAlso (SpeakerLst.Count > 0)) Then
                For s As Integer = 0 To SpeakerLst.Count - 1
                    SpeakerLst(s).InnerXml = SpeakerLst(s).InnerXml.Replace("&amp;#x2002;", " ")
                    If (Not SpeakerLst(s).InnerXml.Contains("<emphasis role=""italic""")) Then
                        SpeakerLst(s).InnerXml = String.Format("<emphasis role=""italic"">{0}</emphasis>", SpeakerLst(s).InnerXml)
                    End If
                Next
            End If


            Dim Alttitles As XmlNodeList = xmlDoc.SelectNodes("//LRH|//RRH")
            If ((Alttitles IsNot Nothing) AndAlso (Alttitles.Count > 0)) Then
                For al As Integer = 0 To Alttitles.Count - 1
                    If (Alttitles(al).ParentNode IsNot Nothing) Then
                        Try
                            Alttitles(al).ParentNode.RemoveChild(Alttitles(al))
                        Catch ex As Exception
                            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                        End Try
                    End If
                Next
            End If
        End If

        Dim FigureList As XmlNodeList = xmlDoc.SelectNodes("//figure|//table")
        If ((FigureList IsNot Nothing) AndAlso (FigureList.Count > 0)) Then
            For f As Integer = 0 To FigureList.Count - 1
                FigureList(f).InnerXml = FigureList(f).InnerXml.Replace("</label>&amp;#x2003;<caption", "</label><caption").Replace("</label>&amp;#x2003;<title", "</label><title")
                FigureList(f).InnerXml = FigureList(f).InnerXml.Replace("</label>&amp;#x2002;<caption", "</label><caption").Replace("</label>&amp;#x2002;<title", "</label><title")
                FigureList(f).InnerXml = FigureList(f).InnerXml.Replace("</label>&amp;#x2002;<colspec", "</label><colspec").Replace("</label>&amp;#x2003;<colspec", "</label><colspec")
            Next
        End If
        Dim Dispquotes As XmlNodeList = xmlDoc.SelectNodes("//example/section")
        If ((Dispquotes IsNot Nothing) AndAlso (Dispquotes.Count > 0)) Then
            For d As Int16 = 0 To Dispquotes.Count - 1
                Try
                    If (Dispquotes(d).ParentNode IsNot Nothing) Then
                        Dispquotes(d).ParentNode.InnerXml = Dispquotes(d).ParentNode.InnerXml.Replace(Dispquotes(d).OuterXml, String.Format("<example>{0}</example>", Dispquotes(d).InnerXml))
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        End If


        Dim TitleList As XmlNodeList = xmlDoc.SelectNodes("//book/title")
        For uu As Integer = 0 To TitleList.Count - 1
            If ((TitleList(uu).NextSibling IsNot Nothing) AndAlso (String.Compare(TitleList(uu).NextSibling.Name, "title", True) = 0)) Then
                TitleList(uu).NextSibling.ParentNode.RemoveChild(TitleList(uu).NextSibling)
            End If
        Next

        For u As Int16 = 0 To 3
            Dim txtList As XmlNodeList = xmlDoc.SelectNodes("//text()", NameSpaceManager)
            For t As Integer = 0 To txtList.Count - 1
                If (txtList(t).ParentNode Is Nothing) Then
                    Continue For
                End If
                If ((String.Compare(txtList(t).ParentNode.Name, "uri", True) = 0) Or (String.Compare(txtList(t).ParentNode.Name, "link", True) = 0)) Then
                    Continue For
                End If
                If (Regex.IsMatch(txtList(t).InnerText, "((http:|https:|www\.)([^ ><)]+))", RegexOptions.IgnoreCase Or RegexOptions.Singleline)) Then
                    Try
                        Dim tmpUrl As String = Regex.Replace(txtList(t).InnerText, "((http:|https:|www\.)([^ ><)]+))", "<uri>$1</uri>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                        txtList(t).ParentNode.InnerXml = txtList(t).ParentNode.InnerXml.Replace(txtList(t).InnerText.Replace("&", "&amp;"), tmpUrl.Replace("&", "&amp;"))
                    Catch ex As Exception
                        Continue For
                    End Try
                End If
            Next
        Next

        Dim SourceList As XmlNodeList = xmlDoc.SelectNodes("//source", NameSpaceManager)
        If ((SourceList IsNot Nothing) AndAlso (SourceList.Count > 0)) Then
            For g As Integer = 0 To SourceList.Count - 1
                SourceList(g).InnerXml = SourceList(g).InnerXml.Replace("<italic>", "").Replace("</italic>", "")
            Next
        End If


        Dim AlttitleList As XmlNodeList = xmlDoc.SelectNodes("//alt-title")
        Dim InfoNode As XmlNode = xmlDoc.SelectSingleNode("//info")
        Dim AltContent As String = String.Empty
        If ((AlttitleList IsNot Nothing) AndAlso (AlttitleList.Count > 0)) Then
            If ((InfoNode IsNot Nothing) AndAlso (InfoNode.ParentNode IsNot Nothing)) Then
                For lt As Integer = 0 To AlttitleList.Count - 1
                    AltContent = AltContent & AlttitleList(lt).OuterXml
                    AlttitleList(lt).ParentNode.RemoveChild(AlttitleList(lt))
                Next
                InfoNode.ParentNode.InnerXml = InfoNode.ParentNode.InnerXml.Replace(InfoNode.OuterXml, AltContent & InfoNode.OuterXml)
            End If
        End If

        Dim LinkLst As XmlNodeList = xmlDoc.SelectNodes("//link[contains(text(),',')][@role='page']")
        Dim LinkTexts As New List(Of String)
        Dim OrgLink As String = String.Empty
        Dim SeparateLink As String = String.Empty
        If ((LinkLst IsNot Nothing) AndAlso (LinkLst.Count > 0)) Then
            For ll As Integer = 0 To LinkLst.Count - 1
                LinkTexts.Clear()
                SeparateLink = String.Empty
                LinkTexts.AddRange(LinkLst(ll).InnerText.Split(","))
                If ((LinkTexts IsNot Nothing) AndAlso (LinkTexts.Count > 0)) Then
                    OrgLink = LinkLst(ll).OuterXml
                    'LinkLst(ll).InnerText = LinkTexts(0).Trim()
                    SeparateLink = "<link role=""page"">" & LinkTexts(0).Trim() & "</link>"
                    For mm As Integer = 1 To LinkTexts.Count - 1
                        SeparateLink = SeparateLink & ", <link role=""page"">" & LinkTexts(mm).Trim() & "</link>"
                    Next
                    If (LinkLst(ll).ParentNode IsNot Nothing) Then
                        LinkLst(ll).ParentNode.InnerXml = LinkLst(ll).ParentNode.InnerXml.Replace(OrgLink, SeparateLink)
                    End If
                End If
            Next
        End If

        Dim InlineEquationLst As XmlNodeList = xmlDoc.SelectNodes("//inlineequation[@id]")
        If ((InlineEquationLst IsNot Nothing) AndAlso (InlineEquationLst.Count > 0)) Then
            For ff As Integer = 0 To InlineEquationLst.Count - 1
                Try
                    InlineEquationLst(ff).Attributes.Remove(InlineEquationLst(ff).Attributes("id"))
                Catch ex As Exception
                End Try
            Next
        End If

        Dim informalfigureList As XmlNodeList = xmlDoc.SelectNodes("//informalfigure")
        Dim ImageName As String = String.Empty
        Dim InformalParent As XmlNode = Nothing
        If ((informalfigureList IsNot Nothing) AndAlso (informalfigureList.Count > 0)) Then
            While ((informalfigureList IsNot Nothing) AndAlso (informalfigureList.Count > 0))
                Try
                    InformalParent = informalfigureList(0)
                    If (InformalParent Is Nothing) Then
                        Exit While
                    End If
                    If ((informalfigureList(0).ChildNodes IsNot Nothing) AndAlso (informalfigureList(0).ChildNodes.Count > 0)) Then
                        Dim ImageData As XmlNode = GetChildNode(informalfigureList(0), "imagedata")
                        If (ImageData IsNot Nothing) Then
                            Try
                                ImageName = ImageData.Attributes("fileref").Value
                            Catch ex As Exception
                                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                                Exit While
                            End Try
                            If (Not String.IsNullOrEmpty(ImageName)) Then
                                ImageName = Path.GetFileNameWithoutExtension(ImageName).Replace("images/", "").Replace(".tifimage/jpeg", "")
                            End If
                        End If
                    End If
                    If (Not String.IsNullOrEmpty(ImageName)) Then
                        If (InformalParent.ParentNode IsNot Nothing) Then
                            InformalParent.ParentNode.InnerXml = InformalParent.ParentNode.InnerXml.Replace(InformalParent.OuterXml, "<p><graphic xmlns:xlink=""http://www.w3.org/1999/xlink"" xlink:href=""" & ImageName & """ mime-subtype=""tif""/></p>")
                        End If
                        'InformalParent.ParentNode.RemoveChild(InformalParent)
                    Else
                        Exit While
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Exit While
                End Try
                informalfigureList = xmlDoc.SelectNodes("//informalfigure")
            End While
        End If

        Dim MiniTocLst As XmlNodeList = xmlDoc.SelectNodes("//minitoc")
        If (Not IsePubXML) Then
            If ((MiniTocLst IsNot Nothing) AndAlso (MiniTocLst.Count > 0)) Then
                For mi As Integer = 0 To MiniTocLst.Count - 1
                    If (MiniTocLst(mi).ParentNode IsNot Nothing) Then
                        MiniTocLst(mi).ParentNode.RemoveChild(MiniTocLst(mi))
                    End If
                Next
            End If
        Else

            MiniTocLst = xmlDoc.SelectNodes("//minitoc/para")
            If ((MiniTocLst IsNot Nothing) AndAlso (MiniTocLst.Count > 0)) Then
                For mi As Integer = 0 To MiniTocLst.Count - 1
                    MiniTocLst(mi).InnerXml = Regex.Replace(MiniTocLst(mi).InnerXml, "^([0-9\.]+)(\t)", "$1 ", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    MiniTocLst(mi).InnerXml = Regex.Replace(MiniTocLst(mi).InnerXml, "(\t[0-9]+$)", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    MiniTocLst(mi).InnerXml = Regex.Replace(MiniTocLst(mi).InnerXml, "(" & ChrW(8233) & "[0-9]+$)", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                Next
            End If
        End If

        Dim IsNumerRef As Boolean = False
        Try
            IsNumerRef = IdentifyReferenceType(xmlDoc)
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
        End Try

        Dim CrossLinks As New List(Of String)
        Dim NewCrossLinkText As String = String.Empty
        Dim LinkNode1 As XmlNode = Nothing
        Dim IsSuperScriptFound As Boolean = True
        Dim IDAttrib As String = String.Empty

        Dim LinkList As XmlNodeList = xmlDoc.SelectNodes("//link[@role='bibr']")

        'split number crosslink
        If (IsePubXML) Then
            If (IsNumerRef) Then
                If ((LinkList IsNot Nothing) AndAlso (LinkList.Count > 0)) Then
                    While ((LinkList IsNot Nothing) AndAlso (LinkList.Count > 0))
                        LinkNode1 = LinkList(0)
                        IDAttrib = String.Empty
                        NewCrossLinkText = String.Empty
                        IsSuperScriptFound = False
                        CrossLinks.Clear()
                        Try
                            If (LinkNode1.InnerText.Contains(",") Or LinkNode1.InnerText.Contains("&#x2013;")) Then
                                If (LinkNode1.OuterXml.Contains("<superscript>")) Then
                                    IsSuperScriptFound = True
                                End If

                                Try
                                    IDAttrib = LinkNode1.Attributes("linkend").Value.Split("_")(0)
                                Catch ex As Exception
                                    IDAttrib = LinkNode1.Attributes("linkend").Value
                                End Try
                                'CrossLinks = SplitNumberCrossLinks(LinkNode1.InnerText)
                                Dim TmpCitText As String = LinkNode1.InnerText.Replace("&#x2013;", "-")
                                Dim TmpCrossLinks As New List(Of String)
                                TmpCrossLinks.AddRange(TmpCitText.Split(","))
                                If ((TmpCrossLinks IsNot Nothing) AndAlso (TmpCrossLinks.Count > 0)) Then
                                    For c As Integer = 0 To TmpCrossLinks.Count - 1
                                        CrossLinks.Clear()
                                        ExpandCitationLabel(TmpCrossLinks(c), CrossLinks)
                                        If (IsePubXML) Then
                                            If ((TmpCrossLinks(c).Contains("-")) AndAlso (c <> TmpCrossLinks.Count - 1)) Then
                                                If (IsSuperScriptFound) Then
                                                    NewCrossLinkText = NewCrossLinkText & GenerateCrossLinkText(CrossLinks, IDAttrib, IsSuperScriptFound) & "<superscript>,</superscript>"
                                                Else
                                                    NewCrossLinkText = NewCrossLinkText & GenerateCrossLinkText(CrossLinks, IDAttrib, IsSuperScriptFound) & ","
                                                End If
                                            Else
                                                NewCrossLinkText = NewCrossLinkText & GenerateCrossLinkText(CrossLinks, IDAttrib, IsSuperScriptFound)
                                            End If
                                        End If
                                    Next
                                End If
                                If (Not String.IsNullOrEmpty(NewCrossLinkText)) Then
                                    NewCrossLinkText = Regex.Replace(NewCrossLinkText, "<superscript>,</superscript>$", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                                    NewCrossLinkText = NewCrossLinkText.TrimEnd(", ").TrimEnd(",")
                                    'NewCrossLinkText = NewCrossLinkText.Replace("<superscript>,</superscript>", "").Replace(",", "")
                                End If
                                If (Not String.IsNullOrEmpty(NewCrossLinkText)) Then
                                    If (LinkNode1.ParentNode IsNot Nothing) Then
                                        LinkNode1.ParentNode.InnerXml = LinkNode1.ParentNode.InnerXml.Replace(LinkNode1.OuterXml, NewCrossLinkText)
                                    End If
                                End If
                            End If
                        Catch ex As Exception
                            GBL.DeantaBallon(ex.Message & " - " & LinkNode1.InnerText, MessageType.MSGERROR)
                        End Try
                        LinkNode1.Attributes("role").Value = "bibrr"
                        LinkList = xmlDoc.SelectNodes("//link[@role='bibr']")
                    End While
                End If
            End If
        Else ' for XML
            If (IsNumerRef) Then
                LinkList = xmlDoc.SelectNodes("//link[@role='bibr']")
                If ((LinkList IsNot Nothing) AndAlso (LinkList.Count > 0)) Then
                    While ((LinkList IsNot Nothing) AndAlso (LinkList.Count > 0))
                        LinkNode1 = LinkList(0)
                        NewCrossLinkText = String.Empty
                        CrossLinks.Clear()
                        Try
                            If (LinkNode1.InnerText.Contains(",") Or LinkNode1.InnerText.Contains("&#x2013;")) Then
                                If (LinkNode1.OuterXml.Contains("<superscript>")) Then
                                    IsSuperScriptFound = True
                                End If

                                Try
                                    IDAttrib = LinkNode1.Attributes("linkend").Value.Split("_")(0)
                                Catch ex As Exception
                                    IDAttrib = LinkNode1.Attributes("linkend").Value
                                End Try
                                'CrossLinks = SplitNumberCrossLinks(LinkNode1.InnerText.Trim())
                                Dim TmpCitText As String = LinkNode1.InnerText.Replace("&#x2013;", "-")
                                Dim TmpCrossLinks As New List(Of String)
                                TmpCrossLinks.AddRange(TmpCitText.Split(","))
                                If ((TmpCrossLinks IsNot Nothing) AndAlso (TmpCrossLinks.Count > 0)) Then
                                    For c As Integer = 0 To TmpCrossLinks.Count - 1
                                        CrossLinks.Clear()
                                        ExpandCitationLabel(TmpCrossLinks(c), CrossLinks)
                                        If ((TmpCrossLinks(c).Contains("-")) AndAlso (c <> TmpCrossLinks.Count - 1)) Then
                                            NewCrossLinkText = NewCrossLinkText & GenerateCrossLinkXMLText(CrossLinks, IDAttrib) & ","
                                        Else
                                            NewCrossLinkText = NewCrossLinkText & GenerateCrossLinkXMLText(CrossLinks, IDAttrib) & ","
                                        End If
                                    Next
                                End If
                                If (Not String.IsNullOrEmpty(NewCrossLinkText)) Then
                                    NewCrossLinkText = NewCrossLinkText.TrimEnd(",")
                                    If (LinkNode1.ParentNode IsNot Nothing) Then
                                        LinkNode1.ParentNode.InnerXml = LinkNode1.ParentNode.InnerXml.Replace(LinkNode1.OuterXml, NewCrossLinkText)
                                    End If
                                End If
                            End If
                        Catch ex As Exception
                            GBL.DeantaBallon(ex.Message & " - " & LinkNode1.InnerText, MessageType.MSGERROR)
                        End Try
                        LinkNode1.Attributes("role").Value = "bibrr"
                        LinkList = xmlDoc.SelectNodes("//link[@role='bibr']")
                    End While
                End If
            End If
        End If


        'for XML
        'If (IsNumerRef) Then
        '    LinkList = xmlDoc.SelectNodes("//link[@role='bibr']")
        '    If ((LinkList IsNot Nothing) AndAlso (LinkList.Count > 0)) Then
        '        While ((LinkList IsNot Nothing) AndAlso (LinkList.Count > 0))
        '            LinkNode1 = LinkList(0)
        '            NewCrossLinkText = String.Empty
        '            CrossLinks.Clear()
        '            Try
        '                If (LinkNode1.InnerText.Contains(",") Or LinkNode1.InnerText.Contains("&#x2013;")) Then
        '                    If (LinkNode1.OuterXml.Contains("<superscript>")) Then
        '                        IsSuperScriptFound = True
        '                    End If

        '                    Try
        '                        IDAttrib = LinkNode1.Attributes("linkend").Value.Split("_")(0)
        '                    Catch ex As Exception
        '                        IDAttrib = LinkNode1.Attributes("linkend").Value
        '                    End Try
        '                    CrossLinks = SplitNumberCrossLinks(LinkNode1.InnerText.Trim())
        '                    If (LinkNode1.InnerText.Contains("&#x2013;")) Then
        '                        NewCrossLinkText = GenerateCrossLinkAttrib(CrossLinks, IDAttrib)
        '                        If (Not String.IsNullOrEmpty(NewCrossLinkText)) Then
        '                            LinkNode1.Attributes("linkend").Value = NewCrossLinkText.Trim()
        '                        End If
        '                    Else
        '                        NewCrossLinkText = GenerateCrossLinkText(CrossLinks, IDAttrib, IsSuperScriptFound)
        '                        If (Not String.IsNullOrEmpty(NewCrossLinkText)) Then
        '                            If (LinkNode1.ParentNode IsNot Nothing) Then
        '                                LinkNode1.ParentNode.InnerXml = LinkNode1.ParentNode.InnerXml.Replace(LinkNode1.OuterXml, NewCrossLinkText)
        '                            End If
        '                        End If
        '                    End If
        '                End If
        '            Catch ex As Exception
        '                GBL.DeantaBallon(ex.Message & " - " & LinkNode1.InnerText, MessageType.MSGERROR)
        '            End Try
        '            LinkNode1.Attributes("role").Value = "bibrr"
        '            LinkList = xmlDoc.SelectNodes("//link[@role='bibr']")
        '        End While
        '    End If
        'End If
        'End If

        LinkList = xmlDoc.SelectNodes("//link[@role='bibrr']")
        While ((LinkList IsNot Nothing) AndAlso (LinkList.Count > 0))
            Dim LinkNode As XmlNode = LinkList(0)
            If (String.IsNullOrEmpty(LinkNode.InnerText)) Then
                LinkNode.Attributes("role").Value = "bibr"
            ElseIf (Regex.Match(LinkNode.InnerText, "[^0-9]+", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success) Then
                LinkNode.Attributes("role").Value = "bibr"
            Else
                Try
                    IDAttrib = LinkNode1.Attributes("linkend").Value.Split("_")(0)
                Catch ex As Exception
                    IDAttrib = LinkNode1.Attributes("linkend").Value
                End Try
                Try
                    LinkNode.Attributes("linkend").Value = String.Format("{0}_CIT{1}", IDAttrib, Convert.ToInt32(LinkNode.InnerText).ToString("00000"))
                    LinkNode.Attributes("role").Value = "bibr"
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            End If
            LinkList = xmlDoc.SelectNodes("//link[@role='bibrr']")
        End While


        If (IsePubXML) Then
            LinkList = xmlDoc.SelectNodes("//link[@role='bibr']")
            If (Not IsNumerRef) Then
                While ((LinkList IsNot Nothing) AndAlso (LinkList.Count > 0))
                    Dim LinkNode As XmlNode = LinkList(0)
                    Try
                        If (LinkNode.ParentNode IsNot Nothing) Then
                            LinkNode.ParentNode.InnerXml = LinkNode.ParentNode.InnerXml.Replace(LinkNode.OuterXml, LinkNode.InnerXml)
                        End If
                    Catch ex As Exception
                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    End Try
                    LinkList = xmlDoc.SelectNodes("//link[@role='bibr']")
                End While
            End If

            Dim LinkEndIDs As New List(Of String)
            LinkList = xmlDoc.SelectNodes("//link[@role='bibr']")
            If ((LinkList IsNot Nothing) AndAlso (LinkList.Count > 0)) Then
                If (IsNumerRef) Then
                    For lk As Integer = 0 To LinkList.Count - 1
                        Dim LinkNode As XmlNode = LinkList(lk)
                        Dim LinkEnd As String = LinkNode.Attributes("linkend").Value
                        Try
                            If ((From n In LinkEndIDs Where String.Compare(n, LinkEnd, True) = 0 Select n).Any) Then
                                Dim Href As XmlAttribute = xmlDoc.CreateNode(XmlNodeType.Attribute, "href", "")
                                Href.Value = LinkEnd
                                LinkNode.Attributes.Remove(LinkNode.Attributes("linkend"))
                                LinkNode.Attributes.Append(Href)
                            Else
                                LinkEndIDs.Add(LinkEnd)
                            End If
                        Catch ex As Exception
                            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                        End Try
                    Next
                End If
            End If
        End If

        If (IsNumerRef) Then
            Dim Bibliomixed As XmlNodeList = xmlDoc.SelectNodes("//bibliomixed/label")
            Dim BiblioNode As XmlNode = Nothing
            Dim LabelNum As String = String.Empty
            If ((Bibliomixed IsNot Nothing) AndAlso (Bibliomixed.Count > 0)) Then
                For b As Integer = 0 To Bibliomixed.Count - 1
                    IDAttrib = String.Empty
                    LabelNum = String.Empty
                    Try
                        If (Bibliomixed(b).ParentNode IsNot Nothing) Then
                            BiblioNode = Bibliomixed(b).ParentNode
                        End If
                        If (BiblioNode IsNot Nothing) Then
                            Try
                                IDAttrib = BiblioNode.Attributes("xml:id").Value.Split("_")(0)
                            Catch ex As Exception
                                IDAttrib = BiblioNode.Attributes("xml:id").Value
                            End Try
                            LabelNum = Regex.Replace(Bibliomixed(b).InnerText, "[^0-9]", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                            If (Not String.IsNullOrEmpty(LabelNum)) Then
                                BiblioNode.Attributes("xml:id").Value = String.Format("{0}_CIT{1}", IDAttrib, Convert.ToInt32(LabelNum).ToString("00000"))
                            End If
                        End If
                    Catch ex As Exception
                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                        Continue For
                    End Try
                Next
            End If
        End If


        Dim UriList As XmlNodeList = xmlDoc.SelectNodes("//uri")
        If ((UriList IsNot Nothing) AndAlso (UriList.Count > 0)) Then
            For u As Integer = 0 To UriList.Count - 1
                Try
                    If (String.IsNullOrEmpty(UriList(u).InnerText.Trim())) Then
                        If (UriList(u).ParentNode IsNot Nothing) Then
                            UriList(u).ParentNode.RemoveChild(UriList(u))
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        End If

        XmlString = xmlDoc.OuterXml.Replace("&amp;", "&")
        XmlString = XmlString.Replace("role=""bibrr""", "role=""bibr""")
        'XmlString = Regex.Replace(XmlString, "<mml:mstyle[^>]*>", "<mml:mstyle>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        XmlString = Regex.Replace(XmlString, "(<book)([^><]*)(>)", "$1$3", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Replace(vbTab, "")
        XmlString = XmlString.Replace(vbTab, "")
        'XmlString = XmlString.Replace("</alt-title><info>", "</alt-title></title-group><info>").Replace("</label><title", "</label><title-group><title")
        Return XmlString
    End Function

    Public Function urlExists(url As String) As Boolean
        'Dim req As HttpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
        'req.AllowAutoRedirect = False
        'Dim Res As HttpWebResponse = CType(req.GetResponse(), HttpWebResponse)
        'If (Res.StatusCode = HttpStatusCode.OK) Then
        '    Return True
        'Else
        '    Return False
        'End If
        If (String.IsNullOrEmpty(url)) Then Return False
        Return Regex.IsMatch(url, "(?:(?:(?:[A-Za-z]{3,9}:(?:\/\/)?)(?:[-;:&=\+\$,\w]+@)?[A-Za-z0-9.-]+|(?:www.|[-;:&=\+\$,\w\.]+@)[A-Za-z0-9.-]+)(?:(?:\/[\+~%\/.\w-_]*)?\??(?:[-\+=&;%@.\w_]*)#?(?:[\w]*))?)")
    End Function

    Public Function ChangeAttributeName(ByVal NodeList As XmlNodeList, ByVal FindAttributeName As String, ByVal ReplaceAttributeValue As String) As Boolean
        Dim EleNode As XmlElement = Nothing
        If ((NodeList Is Nothing) OrElse (NodeList.Count = 0)) Then
            Return True
        End If
        For n As Integer = 0 To NodeList.Count - 1
            Try
                EleNode = CType(NodeList(n), XmlElement)
            Catch ex As Exception
                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                EleNode = Nothing
            End Try
            If (EleNode Is Nothing) Then Continue For
            Try
                EleNode.SetAttribute(FindAttributeName, ReplaceAttributeValue)
            Catch ex As Exception
                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            End Try
        Next
        Return True
    End Function
    Private Function IdentifyReferenceType(ByVal xmlTpDoc As XmlDocument) As Boolean
        Dim IsNumerRef As Boolean = False
        Dim RefNode As XmlNode = xmlTpDoc.SelectSingleNode("//bibliomixed/label")
        If (RefNode IsNot Nothing) Then
            Return True
        End If
        Return False
    End Function

    Private Function old_IdentifyReferenceType(ByVal xmlTpDoc As XmlDocument) As Boolean
        Dim IsNumerRef As Boolean = False
        Dim LinkLst As XmlNodeList = Nothing
        LinkLst = xmlTpDoc.SelectNodes("//link[@role='bibr']")
        If ((LinkLst IsNot Nothing) AndAlso (LinkLst.Count > 0)) Then
            If (Regex.Match(LinkLst(0).OuterXml, "\>[0-9]+\<", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success) Then
                If (LinkLst(0).InnerText.Length = 1) Then
                    IsNumerRef = True
                End If
            End If
            If (Regex.Match(LinkLst(0).OuterXml, "\>[0-9]+(&amp;#x[A-F0-9]{1,4};)[0-9]+", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success) Then
                IsNumerRef = True
            End If
            If (Regex.Match(LinkLst(0).OuterXml, "\>[0-9\, ]+\<", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success) Then
                If (LinkLst(0).InnerText.Contains(",")) Then
                    IsNumerRef = True
                Else
                    If (LinkLst(0).InnerText.Length = 4) Then
                        IsNumerRef = False
                    Else
                        IsNumerRef = True
                    End If
                End If
            End If
            If (Regex.Match(LinkLst(0).OuterXml, "\>[\s]+\[[0-9]\]+\<", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success) Then
                IsNumerRef = True
            End If
        End If
        Return IsNumerRef
    End Function

    Private Sub ExpandCitationLabel(Label As String, ExpandList As List(Of String))
        If (Not String.IsNullOrEmpty(Label)) Then
            If (Not Label.Contains(",") And Not Label.Contains("-")) Then
                ExpandList.Add(Label)
            End If
            If (Label.Contains(",")) Then
                For Each Lbl As String In Label.Split(",")
                    If (Not ExpandList.Contains(Lbl)) Then
                        If (Lbl.Contains("-")) Then
                            ExpandCitationLabel(Lbl, ExpandList)
                        Else
                            ExpandList.Add(Lbl)
                        End If
                    End If
                Next
            End If
            If (Label.Contains("-")) Then
                Dim TmpLbl As String = Regex.Match(Label, "[0-9]+\-[0-9]+", RegexOptions.IgnoreCase).Value
                If (Not String.IsNullOrEmpty(TmpLbl)) Then
                    Dim StartLbl As Integer = TmpLbl.Split("-")(0)
                    Dim EndLbl As Integer = TmpLbl.Split("-")(1)
                    For i As Integer = StartLbl To EndLbl
                        If (Not ExpandList.Contains(i)) Then
                            ExpandList.Add(i)
                        End If
                    Next
                End If
            End If
        End If
    End Sub

    Private Function GenerateCrossLinkAttrib1(ByVal CrossLinks As List(Of String), ByVal IDAttrib As String) As String
        Dim CrossText As String = String.Empty
        If ((CrossLinks IsNot Nothing) AndAlso (CrossLinks.Count > 0)) Then
            For i As Integer = 0 To CrossLinks.Count - 1
                CrossText = CrossText & String.Format(" {0}_CIT{1}", IDAttrib, Convert.ToInt32(CrossLinks(i)).ToString("00000"))
            Next
        End If
        Return CrossText
    End Function

    Private Function GenerateCrossLinkAttrib(ByVal CrossLinks As List(Of String), ByVal IDAttrib As String) As String
        Dim CrossText As String = String.Empty
        If ((CrossLinks IsNot Nothing) AndAlso (CrossLinks.Count > 0)) Then
            For i As Integer = 0 To CrossLinks.Count - 1
                CrossText = CrossText & String.Format(" {0}_CIT{1}", IDAttrib, Convert.ToInt32(CrossLinks(i)).ToString("00000"))
            Next
        End If
        Return CrossText
    End Function

    Private Function GenerateCrossLinkXMLText(ByVal CrossLinks As List(Of String), ByVal IDAttrib As String) As String
        Dim CrossText As String = String.Empty
        If ((CrossLinks IsNot Nothing) AndAlso (CrossLinks.Count > 0)) Then
            If (CrossLinks.Count = 2) Then
                CrossText = String.Format("<link role=""bibrr"" linkend=""XXX"">{2}</link>", IDAttrib, Convert.ToInt32(CrossLinks(0).Trim()).ToString("00000"), CrossLinks(0).Trim()) & "," & String.Format("<link role=""bibrr"" linkend=""XXX"">{2}</link>", IDAttrib, Convert.ToInt32(CrossLinks(1).Trim()).ToString("00000"), CrossLinks(1).Trim())
            ElseIf (CrossLinks.Count > 2) Then
                For i As Integer = 0 To CrossLinks.Count - 1
                    If (i = 0) Then
                        CrossText = String.Format("<link role=""bibrr"" linkend=""XXX"">{2}", IDAttrib, Convert.ToInt32(CrossLinks(i).Trim()).ToString("00000"), CrossLinks(i).Trim())
                    ElseIf (i = CrossLinks.Count - 1) Then
                        CrossText = CrossText & "&amp;#x2013;" & String.Format("{2}</link>", IDAttrib, Convert.ToInt32(CrossLinks(i).Trim()).ToString("00000"), CrossLinks(i).Trim())
                    End If
                Next
            ElseIf (CrossLinks.Count = 1) Then
                CrossText = String.Format("<link role=""bibrr"" linkend=""XXX"">{2}</link>", IDAttrib, Convert.ToInt32(CrossLinks(0).Trim()).ToString("00000"), CrossLinks(0).Trim())
            End If
        End If
        Dim AttribText As String = GenerateCrossLinkAttrib(CrossLinks, IDAttrib)
        CrossText = CrossText.Replace("XXX", AttribText.Trim())
        Return CrossText
    End Function

    Private Function GenerateCrossLinkText(ByVal CrossLinks As List(Of String), ByVal IDAttrib As String, ByVal IsSuperScriptFound As Boolean) As String
        Dim CrossText As String = String.Empty
        If ((CrossLinks IsNot Nothing) AndAlso (CrossLinks.Count > 0)) Then
            If (CrossLinks.Count = 2) Then
                If (IsSuperScriptFound) Then
                    CrossText = String.Format("<link role=""bibr"" linkend=""{0}_CIT{1}""><superscript>{2}</superscript></link>", IDAttrib, Convert.ToInt32(CrossLinks(0).Trim()).ToString("00000"), CrossLinks(0).Trim()) & "<superscript>,</superscript>" & String.Format("<link role=""bibr"" linkend=""{0}_CIT{1}""><superscript>{2}</superscript></link>", IDAttrib, Convert.ToInt32(CrossLinks(1).Trim()).ToString("00000"), CrossLinks(1).Trim())
                Else
                    CrossText = String.Format("<link role=""bibr"" linkend=""{0}_CIT{1}"">{2}</link>", IDAttrib, Convert.ToInt32(CrossLinks(0).Trim()).ToString("00000"), CrossLinks(0).Trim()) & "," & String.Format("<link role=""bibr"" linkend=""{0}_CIT{1}"">{2}</link>", IDAttrib, Convert.ToInt32(CrossLinks(1).Trim()).ToString("00000"), CrossLinks(1).Trim())
                End If

            ElseIf (CrossLinks.Count > 2) Then
                For i As Integer = 0 To CrossLinks.Count - 1
                    If (i = 0) Then
                        If (IsSuperScriptFound) Then
                            CrossText = String.Format("<link role=""bibr"" linkend=""{0}_CIT{1}""><superscript>{2}</superscript></link>", IDAttrib, Convert.ToInt32(CrossLinks(i).Trim()).ToString("00000"), CrossLinks(i).Trim())
                        Else
                            CrossText = String.Format("<link role=""bibr"" linkend=""{0}_CIT{1}"">{2}</link>", IDAttrib, Convert.ToInt32(CrossLinks(i).Trim()).ToString("00000"), CrossLinks(i).Trim())
                        End If
                    ElseIf ((i > 0) And (i <> CrossLinks.Count - 1)) Then
                        CrossText = CrossText & String.Format("<link role=""bibr"" linkend=""{0}_CIT{1}""/>", IDAttrib, Convert.ToInt32(CrossLinks(i).Trim()).ToString("00000"), CrossLinks(i).Trim())
                    ElseIf (i = CrossLinks.Count - 1) Then
                        If (IsSuperScriptFound) Then
                            CrossText = CrossText & "<superscript>&amp;#x2013;</superscript>" & String.Format("<link role=""bibr"" linkend=""{0}_CIT{1}""><superscript>{2}</superscript></link>", IDAttrib, Convert.ToInt32(CrossLinks(i).Trim()).ToString("00000"), CrossLinks(i).Trim())
                        Else
                            CrossText = CrossText & "&amp;#x2013;" & String.Format("<link role=""bibr"" linkend=""{0}_CIT{1}"">{2}</link>", IDAttrib, Convert.ToInt32(CrossLinks(i).Trim()).ToString("00000"), CrossLinks(i).Trim())
                        End If
                    End If
                Next
            ElseIf (CrossLinks.Count = 1) Then
                If (IsSuperScriptFound) Then
                    CrossText = String.Format("<link role=""bibr"" linkend=""{0}_CIT{1}""><superscript>{2}</superscript></link>", IDAttrib, Convert.ToInt32(CrossLinks(0).Trim()).ToString("00000"), CrossLinks(0).Trim()) & "<superscript>,</superscript>"
                Else
                    CrossText = String.Format("<link role=""bibr"" linkend=""{0}_CIT{1}"">{2}</link>", IDAttrib, Convert.ToInt32(CrossLinks(0).Trim()).ToString("00000"), CrossLinks(0).Trim()) & ", "
                End If
            End If
        End If
        Return CrossText
    End Function

    Private Function SplitNumberCrossLinks(ByVal LinkText As String) As List(Of String)
        Dim Links As New List(Of String)
        Dim lnk As New List(Of Integer)
        Try
            If (LinkText.Contains(",")) Then
                Links.AddRange(LinkText.Split(","))
            ElseIf (LinkText.Contains("&#x2013;")) Then
                LinkText = LinkText.Replace("&#x2013;", "-")
                lnk.AddRange(Enumerable.Range(LinkText.Split("-")(0), ((LinkText.Split("-")(1) - LinkText.Split("-")(0)) + 1)).ToArray())
                lnk.ForEach(Sub(a As Integer)
                                Links.Add(a.ToString())
                            End Sub)
            End If
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            Return New List(Of String)
        End Try
        Return Links
    End Function

    Private Function MathCleanUp(ByVal EqnMat As Match) As String
        Dim Content As String = EqnMat.Value
        Dim MatCotn As String = Regex.Match(Content, "<mml:math([^><]+)?>(((?!</mml:math>).)+)</mml:math>", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Value
        Dim text As String = "<table class=""equation""><tr><td class=""t-eqn"" id="""">MathXXX</td><td class=""t-eqn1""><p class=""eqn-r"">CaptionXXX</p></td></tr></table>"
        Dim CapMath As Match = Regex.Match(Content, "<caption([^><]+)?>(((?!</caption>).)+)</caption>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'If (Not CapMath.Success) Then
        '    Return EqnMat.Value
        'End If
        Dim Caption As String = Regex.Replace(CapMath.Value, "<caption[^><]*>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Replace("</caption>", "")
        If (Not String.IsNullOrEmpty(Caption.Trim())) Then
            text = text.Replace("MathXXX", MatCotn).Replace("CaptionXXX", Caption)
        Else
            text = text.Replace("MathXXX", MatCotn).Replace("CaptionXXX", Caption)
        End If

        'If (Not String.IsNullOrEmpty(Caption.Trim())) Then
        '    text = text.Replace("MathXXX", MatCotn).Replace("CaptionXXX", Caption)
        'Else
        '    Return EqnMat.Value
        'End If
        Return text
    End Function

    Private iChapteridSeq As Integer = 1

    Private Function ChapterIDSeq(m As Match)
        Dim sResult As String = m.Value.ToString
        iSec = 1
        sResult = Regex.Replace(sResult, "(<title([^><]+)?>(((?!</title>).)+)</title>)?(<section([^><]+)?)>", AddressOf SectionPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iFootnote = 1
        'sResult = Regex.Replace(sResult, "(<footnote([^><]+)?)>", AddressOf FootnotePro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sResult = Regex.Replace(sResult, "(<bibliomixed xml:id="")([^""]+)""", "$1ch" & iChapteridSeq & "-$2" & Chr(34), RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sResult = Regex.Replace(sResult, "(<link role=""bibr"" xml:id="")([^""]+)""", "$1ch" & iChapteridSeq & "-$2" & Chr(34), RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iChapteridSeq = iChapteridSeq + 1
        Return sResult
    End Function

    Private Function FootnotePro1(m As Match)
        Dim sresult As String = m.Value.ToString
        If sresult.ToString.Contains("figure") Then
            sresult = Regex.Replace(sresult, "ch\d+\-", "", RegexOptions.IgnoreCase)
            Return sresult
        End If
        If Not sresult.ToString.Contains("rid=""fn") Then Return sresult
        sresult = Regex.Replace(sresult, "rid=""fn", "rid=""ch" & iChpSec & "-fn", RegexOptions.IgnoreCase)
        Return sresult
    End Function

    Private Function FootntSeqPro(m As Match)
        Dim sresult As String = m.Value.ToString
        If Not sresult.ToString.Contains("label=") Then Return sresult
        sresult = m.Groups(1).Value.ToString
        sresult = Regex.Replace(sresult, "label=""([^""]+)""", "label=""" & m.Groups(5).Value.ToString & """")
        Return sresult
    End Function

    Private Function FigureCaptionParaPro(m As Match)
        Dim sResult As String = m.Value.ToString
        sResult = Regex.Replace(sResult, "</para>", "</1para>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
        Return sResult
    End Function

    Private Function Bibliomixed(m As Match)
        Dim sResult As String = m.Value.ToString
        sResult = Regex.Replace(sResult, "<bibliomixed ", "<refbibliomixed ", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Return sResult
    End Function

    Private Sub FinalCleanup(Optional ByVal bXslExec As Boolean = False)
        Try
            XMLString = XMLString.Replace("</book-title-group><book-title-group>", "")
            XMLString = XMLString.Replace("<subtitle><subtitle", "<subtitle").Replace("</subtitle></subtitle>", "</subtitle>")
            XMLString = Regex.Replace(XMLString, "</book-front>" & vbLf & "<book-front[^>]*>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            XMLString = Regex.Replace(XMLString, "<title[\s]*/>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            XMLString = Regex.Replace(XMLString, "xmlns:fo=""http://www.w3.org/1999/XSL/Format"" xmlns:xlink=""http://www.w3.org/1999/xlink"" xmlns:msxsl=""urn:schemas-microsoft-com:xslt"" xmlns:d=""http://docbook.org/ns/docbook"" xmlns:aid=""http://ns.adobe.com/AdobeInDesign/4.0/"" xmlns:aid5=""http://ns.adobe.com/AdobeInDesign/5.0/"" xmlns:code=""urn:schemas-test-code""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "<a>page_([^<>]+)</a>", "<xref ref-type=""page"" id=""page_$1""/>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            ' To retain bibliomixed attributes in bibliography
            XMLString = Regex.Replace(XMLString, "<bibliography([^><]+)?>((?:(?!</bibliography>).)+)</bibliography>", AddressOf Bibliomixed, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            XMLString = Regex.Replace(XMLString, "<subtitle[\s]*/>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            XMLString = UpdatePro(XMLString, bXslExec)
            XMLString = Regex.Replace(XMLString, "<refbibliomixed ", "<bibliomixed ", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "<biblioset([^><]+)?>((?:(?!</biblioset>).)+)</biblioset>", AddressOf BibliosetPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "([^ ])xml:id=", "$1 xml:id=", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "(<?xml version=""1.0""[^><]+>)", "$1" & Environment.NewLine & "<!--<!DOCTYPE book SYSTEM ""TFB.dtd"">-->", RegexOptions.IgnoreCase Or RegexOptions.Singleline)


            XMLString = Regex.Replace(XMLString, "<title((?:(?!(-group|>)).)+)>", "<title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "<book-part[^><]+>", AddressOf ChapNos, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            ' Removing unnecessary text
            XMLString = Regex.Replace(XMLString, "(5.0b-\d+enfullText|\t)", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "<sec >|<biblioid>doi10.5040[^><]+</biblioid>|<imagedata>pdfs/[^><]+</imagedata>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'XMLString = Regex.Replace(XMLString, "<([^><]+) >", "<$1>", RegexOptions.IgnoreCase Or RegexOptions.Singleline) '01-08-2019
            XMLString = Regex.Replace(XMLString, "<toc([^><]+)?>((?:(?!</toc>).)+)</toc>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            XMLString = XMLString.Replace("<fn-group><title>Notes</title></fn-group>", "")

            iChapp = 0
            'XMLString = Regex.Replace(XMLString, "<sec([^><]+)?>", AddressOf SecNos, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "<book-part ?book-part-type=""(chapter|part)""><label>(((?!</label>).)+)</label>", AddressOf ChapterPro1, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            'XMLString = Regex.Replace(XMLString, "<book-part([^><]+)?book-part-type=""chapter[^><]+>((?:(?!</book-part>).)+)</book-part>", AddressOf BodyTagInto, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            'XMLString = Regex.Replace(XMLString, "<book-part ?book-part-type=""part[^><]+>((?:(?!(<book-part|<back)).)+)", AddressOf BodyTagIntroPart, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            XMLString = Regex.Replace(XMLString, "([^ ])(book-part-type=)", "$1 $2", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "( publication-type=[^><]+)><mixed-citation>", "><mixed-citation$1>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'XMLString = Regex.Replace(XMLString, "(<fn id=[^><]+>)<p>(\d+)( +)?", "$1<label>$2</label><p>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "<sec([^><]+)?><title([^><]+)?>Notes</title>((?:(?!</sec>).)+)(</sec>)</sec>", "<notes><title>Notes</title>$3$4</notes>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            'XMLString = Regex.Replace(XMLString, "</disp-quote>(" & Environment.NewLine & ")?<disp-quote([^><]+)?>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            XMLString = Regex.Replace(XMLString, "(<disp-quote[^>]*>(?:(?:(?!<\/disp-quote>).)*))(</disp-quote><disp-quote[^>]*>)((?:(?:(?!<\/disp-quote>).)*)</disp-quote>)", Function(mt As Match)
                                                                                                                                                                                   Dim Section As String = mt.Groups(1).Value
                                                                                                                                                                                   Dim Section1 As String = mt.Groups(3).Value
                                                                                                                                                                                   If ((Not String.IsNullOrEmpty(Section)) Or (Not String.IsNullOrEmpty(Section1))) Then
                                                                                                                                                                                       If ((Section.Contains("<title>")) Or (Section1.Contains("<title>"))) Then
                                                                                                                                                                                           Return mt.Value
                                                                                                                                                                                       ElseIf (Not (Section.Contains("<title>")) And (Not Section1.Contains("<title>"))) Then
                                                                                                                                                                                           Return String.Format("{0}{1}", mt.Groups(1).Value, mt.Groups(3).Value)
                                                                                                                                                                                       End If
                                                                                                                                                                                   Else
                                                                                                                                                                                       Return mt.Value
                                                                                                                                                                                   End If
                                                                                                                                                                                   Return mt.Value
                                                                                                                                                                               End Function, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            'XMLString = Regex.Replace(XMLString, "(<fn([^><]+)?><label>((?:(?!</fn>).)+)</fn>(" & Environment.NewLine & ")?)+", "<fn-group>$1</fn-group>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            XMLString = Regex.Replace(XMLString, "(<notes><title>Notes</title>)((?:(?!</notes>).)+)(</notes>)", "$1<fn-group>$2</fn-group>$3", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "</title [^><]+>", "</title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'XMLString = Regex.Replace(XMLString, "</(\w+) [^><]+>", "</$1>", RegexOptions.IgnoreCase Or RegexOptions.Singleline) '01-08-2019
            XMLString = Regex.Replace(XMLString, "<subtitle[^><]*>", "<subtitle>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "<book-part-meta[^><]+>", "<book-part-meta>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)


            'muthu
            'XMLString = Regex.Replace(XMLString, "</p>(<list([^><]+)?>(((?!</list>).)+)</list>)", "$1</p>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "<mixed-citation publication-type=""journal"">(((?!</mixed-citation>).)+)</mixed-citation>", AddressOf JnlVolume, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "<mixed-citation publication-type=""(journal|book)"">(((?!</mixed-citation>).)+)</mixed-citation>", AddressOf RemoveItalics, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "<string-name>(((?!</string-name>).)+)</string-name>", AddressOf EtalReplace, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, """fnch(\d+\-)(\d+"")", Chr(34) & "ch$1fn$2", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            GC.Collect()

            Dim mtt1 As Match = Regex.Match(XMLString, "<phrase>((?:(?!</phrase>).)+)</phrase>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            If mtt1.Success Then
                XMLString = Regex.Replace(XMLString, mtt1.Value.ToString, "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                XMLString = Regex.Replace(XMLString, "(<imprint-meta>)", "$1<imprint-text type=""ImprintStatement"">" & mtt1.Groups(1).Value.ToString & "</imprint-text>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            End If
            XMLString = Regex.Replace(XMLString, "<imprint-text>((?:(?!</imprint-text>).)+)</imprint-text>", AddressOf ImprintPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'XMLString = Regex.Replace(XMLString, "<fig ([^><]+)?>(((?!</fig>).)+)</fig>", AddressOf FigureChangePro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "<sec([^><])+>(<title>(<[^><]+>)?preface</title>)", "<sec sec-type=""fm-chapter"">$2", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'XMLString = Regex.Replace(XMLString, "(<book-part-meta><title-group><title([^><])?>(((?!</title>).)+)</title>)", "$1<alt-title alt-title-type=""running-head-verso""></alt-title><alt-title alt-title-type=""running-head-recto""></alt-title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            XMLString = Regex.Replace(XMLString, "<graphic[^><]+>", AddressOf GraphicPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            XMLString = Regex.Replace(XMLString, "<book-part[^><]+>((?:(?!</book-part>).)+)</book-part>", AddressOf SecidGeneration, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            'XMLString = Regex.Replace(XMLString, "<term[^><]+>", "<term>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'XMLString = Regex.Replace(XMLString, "</term>(</index-entry>)", "$1", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'XMLString = Regex.Replace(XMLString, "<term>(((?!<nav-pointer-group>).)+)<nav-pointer-group>", "<term>$1</term><nav-pointer-group>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            XMLString = Regex.Replace(XMLString, "(<xref ref-type=""page[^><]+>)<p>", "<p>$1", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "</p>(<xref ref-type=""page[^><]+>)", "$1</p>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'XMLString = Regex.Replace(XMLString, "</fig>(<xref ref-type=""page[^><]+>)", "$1</fig>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "</attrib>(<xref ref-type=""page[^><]+>)", "$1</attrib>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "</list-item>(<xref ref-type=""page[^><]+>)", "$1</list-item>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            'XMLString = Regex.Replace(XMLString, "<speaker><italic>(((?!</italic>).)+)</italic>", "<speaker>$1</speaker><p>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'XMLString = Regex.Replace(XMLString, "</speaker></speech>", "</p></speech>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            GC.Collect()
            XMLString = Regex.Replace(XMLString, "(<sec([^><]+)?>)<title>((<(b|bold|i|italic)>)+)?(((\d+\.)+)?\d+)( |&#x\d+;)?", "$1<label>$6</label><title>$3", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'XMLString = Regex.Replace(XMLString, "<title>( )+", "<title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'XMLString = Regex.Replace(XMLString, "( )+(</[^><]+>)", "$2$1", RegexOptions.IgnoreCase Or RegexOptions.Singleline) '01-08-2019
            'XMLString = Regex.Replace(XMLString, "(<[^><]+>)( )+", "$1$2", RegexOptions.IgnoreCase Or RegexOptions.Singleline) '01-08-2019
            Dim sRepTxt As String = "xmlns:fo=~http://www.w3.org/1999/XSL/Format~ xmlns:xlink=~http://www.w3.org/1999/xlink~ xmlns:mml=~http://www.w3.org/1998/Math/MathML~ " &
                "xmlns:msxsl=~urn:schemas-microsoft-com:xslt~ xmlns:d=~http://docbook.org/ns/docbook~ xmlns:aid=~http://ns.adobe.com/AdobeInDesign/4.0/~ xmlns:aid5=~" &
                "http://ns.adobe.com/AdobeInDesign/5.0/~ xmlns:code=~urn:schemas-test-code~"
            XMLString = Regex.Replace(XMLString, sRepTxt.ToString.Replace("~", Chr(34)), "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'XMLString = Regex.Replace(XMLString, "(  )+", " ", RegexOptions.IgnoreCase Or RegexOptions.Singleline) '01-08-2019
            XMLString = Regex.Replace(XMLString, "(<attrib>(?:(?:(?!</attrib>).)+)</attrib>)(<graphic([^><]+)?>)", "$2$1", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "(doi:?)<uri(?:[^><]+)?>((?:(?!</uri>).)+)</uri>", "$1$2", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "(<given-names>)(\S+)(</given-names>)(\.)", "$1$2$4$3", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "(<given-names>)([A-z])(\.)(-)([A-z])(\.)(</given-names>)", "$1$2$3 $5$6$7", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "(<string-name>)(<surname>)(\S+)(</surname>, )(<given-names>)(\S+)(</given-names>)(\s)(<surname>)(\S+)(</surname>)(</string-name>)", "$1$2$3$4$5$6 $10$7$12", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "(<string-name>)(<surname>)(([A-z+])(\.))+(\s)?(\S+)(</surname>)(</string-name>)", "$1<given-names>$3</given-names> $2$7$8$9", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

            XMLString = XMLString.Replace("<book>", "<book xmlns:xlink=""http://www.w3.org/1999/xlink"" xmlns:mml=""http://www.w3.org/1998/Math/MathML"">")
            XMLString = XMLString.Replace(" & ", " &#x0026; ")
            XMLString = Regex.Replace(XMLString, "(<[^>]*>)", AddressOf EntityCleanup, RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            XMLString = Regex.Replace(XMLString, "&([a-zA-Z])", "&#x0026;$1", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            XMLString = Regex.Replace(XMLString, "(<oxy_comment_start([^>]*)>)(((?!<(\/)?oxy_comment_start).)*)(<\/oxy_comment_start>)", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            XMLString = XMLString.Replace("ref-type=""figure""", "ref-type=""fig""")
            XMLString = XMLString.Replace("</etal></etal>", "</etal>").Replace("<etal><etal>", "<etal>")
            GC.Collect()
            'XMLString = XMLString.Replace("</index-entry></sec></body></book-part>", "</index-entry></sec></book-part>")
            XMLString = Regex.Replace(XMLString, "(<annotation([^>]*)>)(((?!<(\/)?annotation).)*)(<\/annotation>)", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)

            XMLString = Regex.Replace(XMLString, "(<biblioset([^>]*)>)(((?!<(\/)?biblioset).)*)(<\/biblioset>)", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)

            XMLString = XMLString.Replace("<semantics>", "").Replace("</semantics>", "")
            XMLString = XMLString.Replace("<semantics>", "").Replace("</semantics>", "")

            XMLString = Regex.Replace(XMLString, "<disp-formula[^>]*>", "<disp-formula>")

            XMLString = Regex.Replace(XMLString, "<book-meta[\s]*><book-id pub-id-type=""doi""[\s]*/>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = Regex.Replace(XMLString, "(<book [^>]*>)", "$1<book-meta>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = XMLString.Replace("<publisher-name>COPYRIGHT PAGE</publisher-name>", "")
            XMLString = XMLString.Replace("<publisher><publisher-name>CRC Press</publisher-name><publisher-loc>Boca Raton and London</publisher-loc></publisher>", "")
            XMLString = XMLString.Replace("<publisher-name>CRC Press</publisher-name><publisher-name>Taylor &#x0026; Francis Group</publisher-name><publisher-loc>6000 Broken Sound Parkway NW, Suite 300</publisher-loc><publisher-loc>Boca Raton, FL 33487-2742</publisher-loc>", "<publisher><publisher-name>CRC Press</publisher-name><publisher-loc>6000 Broken Sound Parkway NW, Suite 300 Boca Raton, FL 33487-2742</publisher-loc></publisher>")
            XMLString = XMLString.Replace("<disp-quote><body>", "<body><disp-quote>")
            'XMLString = XMLString.Replace("</p></sec></boxed-text>", "</p></boxed-text></sec>")
            XMLString = XMLString.Replace("</list-item></sec></list>", "</list-item></list></sec>")
            XMLString = XMLString.Replace("</p></bold></list-item>", "</bold></p></list-item>")
            XMLString = Regex.Replace(XMLString, "<title[^>]*[\s]*/>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            XMLString = XMLString.Replace("<token>", "<!--<token>-->").Replace("</token>", "<!--</token>-->")
            XMLString = XMLString.Replace("</title-group></book-part-meta><book-part-meta><title-group>", "").Replace("<subtitle><subtitle>", "<subtitle>").Replace("</subtitle></subtitle>", "</subtitle>")

            'If (Not Regex.IsMatch(XMLString, "<book-meta[^>]*>((?:(?!<\/book-meta>).)*)</book-meta>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)) Then
            '    XMLString = XMLString.Replace("<book-meta>", "<book-meta></book-meta>")
            'End If

            File.WriteAllText(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")), XMLString)
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            XMLString = String.Empty
        End Try

        GBL.DeantaBallon("Before MoveFigureandTable", MessageType.MSGINFO)

        Try
            XMLString = MoveFigureandTable(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")))
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            XMLString = String.Empty
        End Try

        GBL.DeantaBallon("After MoveFigureandTable", MessageType.MSGINFO)

    End Sub
    Private iChappp As Integer = 0
    Private iSe As Integer = 1

    Private Function MoveFigureandTable(ByVal xmlPath As String) As String
        Dim xmlFloat As New XmlDocument
        xmlFloat.PreserveWhitespace = True
        Dim ElementList As New List(Of MovePageNumData)
        Dim MaxCount As Int32 = 0
        ElementList.Add(New MovePageNumData With {.ElementName = "table-wrap", .Position = PagePosition.SUFFIX})
        ElementList.Add(New MovePageNumData With {.ElementName = "boxed-text", .Position = PagePosition.SUFFIX})


        Try
            xmlFloat.LoadXml(File.ReadAllText(xmlPath).Replace("&", "&amp;"))
        Catch ex As Exception
            GBL.DeantaBallon("MoveFigureandTable - " & ex.Message, MessageType.MSGERROR)
            Return String.Empty
        End Try

        Dim NameSpaceManager As New System.Xml.XmlNamespaceManager(xmlFloat.NameTable)
        NameSpaceManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")
        NameSpaceManager.AddNamespace("aid5", "http://ns.adobe.com/AdobeInDesign/5.0/")
        NameSpaceManager.AddNamespace("aid", "http://ns.adobe.com/AdobeInDesign/4.0/")
        NameSpaceManager.AddNamespace("xlink", "http://www.w3.org/1999/xlink/")
        NameSpaceManager.AddNamespace("xml", "http://www.w3.org/XML/1998/namespace")
        NameSpaceManager.AddNamespace("mml", "http://www.w3.org/1998/Math/MathML")

        Dim FigureLst As XmlNodeList = xmlFloat.SelectNodes("//fig")
        Dim GraphicNode As XmlNode = Nothing
        Dim CaptionNode As XmlNode = Nothing
        Dim AltNode As XmlNode = Nothing
        Dim sGraphic As String = String.Empty
        If ((FigureLst IsNot Nothing) AndAlso (FigureLst.Count > 0)) Then
            For g As Integer = 0 To FigureLst.Count - 1
                Try
                    GraphicNode = GetChildNode(FigureLst(g), "graphic")
                    CaptionNode = GetChildNode(FigureLst(g), "caption")
                    AltNode = GetChildNode(FigureLst(g), "alt-text")
                    If (GraphicNode IsNot Nothing) Then
                        sGraphic = Regex.Replace(GraphicNode.Attributes("xlink:href").Value, "f(\d+)\.(\d+)", "fig$1_$2.tif", RegexOptions.IgnoreCase)
                        GraphicNode.InnerText = ""
                        If (Not String.IsNullOrEmpty(sGraphic)) Then
                            GraphicNode.Attributes("xlink:href").Value = sGraphic
                        End If
                        If (AltNode IsNot Nothing) Then
                            FigureLst(g).InsertAfter(GraphicNode, AltNode)
                        Else
                            If (CaptionNode IsNot Nothing) Then
                                FigureLst(g).InsertAfter(GraphicNode, CaptionNode)
                            End If
                        End If

                        'FigureLst(g).AppendChild(GraphicNode)
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try

            Next
        End If

        Try
            Dim ColspecList As XmlNodeList = xmlFloat.SelectNodes("//colspec")
            If ((ColspecList IsNot Nothing) AndAlso (ColspecList.Count > 0)) Then
                For c As Integer = ColspecList.Count - 1 To 0 Step -1
                    Try
                        If (ColspecList(c).ParentNode IsNot Nothing) Then
                            ColspecList(c).ParentNode.RemoveChild(ColspecList(c))
                        End If
                    Catch ex As Exception
                    End Try
                Next
            End If
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
        End Try

        Dim TableNodes As XmlNodeList = xmlFloat.SelectNodes("//tgroup")
        Dim ColCount As Int16 = 0
        Dim FirstRow As XmlNode = Nothing
        Dim Innerxml As String = String.Empty
        If ((TableNodes IsNot Nothing) AndAlso (TableNodes.Count > 0)) Then
            For t As Int16 = 0 To TableNodes.Count - 1
                Try
                    'Dim FirstRow As XmlNode = TableNodes(t).SelectSingleNode(".//row")
                    Try
                        FirstRow = GetMaxCountTdInTable(TableNodes(t), "row", "entry")
                    Catch ex As Exception
                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                        Continue For
                    End Try
                    Innerxml = String.Empty
                    If (FirstRow IsNot Nothing) Then
                        ColCount = GetTableColumnCount(FirstRow)
                        If (ColCount > 0) Then
                            For tl As Integer = 1 To ColCount
                                Innerxml = IIf(String.IsNullOrEmpty(Innerxml), "<colspec colnum=""" & tl & """ colname=""col" & tl & """ align=""left""/>", Innerxml & "<colspec colnum=""" & tl & """ colname=""col" & tl & """ align=""left""/>")
                            Next
                        End If
                        Try
                            TableNodes(t).Attributes("cols").Value = ColCount
                        Catch ex As Exception
                        End Try
                        If (Not String.IsNullOrEmpty(Innerxml)) Then
                            TableNodes(t).InnerXml = Innerxml & TableNodes(t).InnerXml
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        End If


        Dim TablesList As XmlNodeList = xmlFloat.SelectNodes("//table-wrap")
        Dim TGroupNode As XmlNode = Nothing
        If ((TablesList IsNot Nothing) AndAlso (TablesList.Count > 0)) Then
            For t As Int16 = 0 To TablesList.Count - 1
                Try
                    Dim ColspecLst As XmlNodeList = TablesList(t).SelectNodes(".//colspec")
                    TGroupNode = GetChildNode(TablesList(t), "tgroup")
                    If (TGroupNode IsNot Nothing) Then
                        If ((ColspecLst IsNot Nothing) AndAlso (ColspecLst.Count > 0)) Then
                            For c As Int16 = ColspecLst.Count - 1 To 0 Step -1
                                TGroupNode.PrependChild(ColspecLst(c))
                            Next
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        End If

        Dim ListItemTags As XmlNodeList = xmlFloat.SelectNodes("//list-item/fig|//list-item/table-wrap")
        While ((ListItemTags IsNot Nothing) AndAlso (ListItemTags.Count > 0))
            Try
                Dim tmp As String = ListItemTags(0).ParentNode.ParentNode.OuterXml.Replace(ListItemTags(0).OuterXml, "")
                ListItemTags(0).ParentNode.ParentNode.ParentNode.InnerXml = ListItemTags(0).ParentNode.ParentNode.ParentNode.InnerXml.Replace(ListItemTags(0).ParentNode.ParentNode.OuterXml, tmp & ListItemTags(0).OuterXml)
                ListItemTags = xmlFloat.SelectNodes("//list-item/fig|//list-item/table-wrap")
            Catch ex As Exception
                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                Continue While
            End Try
        End While

        Dim RefList As XmlNodeList = xmlFloat.SelectNodes("//mixed-citation[@publication-type='other']")
        If ((RefList IsNot Nothing) AndAlso (RefList.Count > 0)) Then
            For r As Integer = 0 To RefList.Count - 1
                Try
                    RefList(r).InnerXml = RefList(r).InnerXml.Replace("<title>", "<article-title>").Replace("</title>", "</article-title>")
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        End If

        Dim contribList As XmlNodeList = xmlFloat.SelectNodes("//contrib/name")
        If ((contribList IsNot Nothing) AndAlso (contribList.Count > 0)) Then
            For Each Contrib As XmlNode In contribList
                Try
                    If (Contrib.InnerXml.Contains("and")) Then
                        Contrib.InnerXml = Contrib.InnerXml.Replace(" and ", "").Replace(" and", "").Replace("and ", "").Replace(">, and", ">")
                        'Contrib.InnerXml = Contrib.InnerXml.Replace(" and ", "").Replace(" and", "").Replace("and ", "").Replace(">, and", ">").Replace("and", "")
                    Else
                        Contrib.InnerXml = Contrib.InnerXml.Replace(",", "").Replace(" , ", "").Replace(" ,", "").Replace(", ", "")
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        End If

        Dim Dispquotes As XmlNodeList = xmlFloat.SelectNodes("//disp-quote/sec")
        If ((Dispquotes IsNot Nothing) AndAlso (Dispquotes.Count > 0)) Then
            While ((Dispquotes IsNot Nothing) AndAlso (Dispquotes.Count > 0))
                If (Dispquotes(0).ParentNode IsNot Nothing) Then
                    Dispquotes(0).ParentNode.InnerXml = Dispquotes(0).ParentNode.InnerXml.Replace(Dispquotes(0).OuterXml, String.Format("<disp-quote>{0}</disp-quote>", Dispquotes(0).InnerXml))
                End If
                Dispquotes = xmlFloat.SelectNodes("//disp-quote/sec")
            End While
        End If

        'Dim DisplayEquation As XmlNodeList = xmlFloat.SelectNodes("//disp-formula", NameSpaceManager)
        'If ((DisplayEquation IsNot Nothing) AndAlso (DisplayEquation.Count > 0)) Then
        '    For Each Eqn As XmlNode In DisplayEquation
        '        Try
        '            Eqn.InnerXml = Eqn.InnerXml.Replace("<m", "<mml:m").Replace("</m", "</mml:m").Replace("<mml:mml:math ", "<mml:math ").Replace("</mml:mml:math", "</mml:math")
        '        Catch ex As Exception
        '            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
        '            Continue For
        '        End Try
        '    Next
        'End If
        Dim ContribGroupList As XmlNodeList = Nothing
        If (Not IsePubXML) Then
            ContribGroupList = xmlFloat.SelectNodes("//con-group/text()")
            If ((ContribGroupList IsNot Nothing) AndAlso (ContribGroupList.Count > 0)) Then
                For jj As Integer = 0 To ContribGroupList.Count - 1
                    If (ContribGroupList(jj).ParentNode IsNot Nothing) Then
                        ContribGroupList(jj).ParentNode.RemoveChild(ContribGroupList(jj))
                    End If
                Next
            End If
        End If
        Dim BookParts As XmlNodeList = xmlFloat.SelectNodes("//book-part")
        If ((BookParts IsNot Nothing) AndAlso (BookParts.Count > 0)) Then
            For b As Int32 = 0 To BookParts.Count - 1
                Dim BookPart As XmlNode = BookParts(b)
                ContribGroupList = BookPart.SelectNodes(".//con-group")
                Dim BookPartNode As XmlNode = BookPart.SelectSingleNode(".//book-part-meta")
                If (BookPartNode Is Nothing) Then Continue For
                If ((ContribGroupList IsNot Nothing) AndAlso (ContribGroupList.Count > 0)) Then
                    For jj As Integer = 0 To ContribGroupList.Count - 1
                        Try
                            BookPartNode.AppendChild(ContribGroupList(jj))
                        Catch ex As Exception
                            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                            Continue For
                        End Try
                    Next
                End If
            Next
        End If

        Dim LabelList As XmlNodeList = xmlFloat.SelectNodes("//label")
        If ((LabelList IsNot Nothing) AndAlso (LabelList.Count > 0)) Then
            For j As Integer = 0 To LabelList.Count - 1
                Try
                    If ((LabelList(j).ParentNode IsNot Nothing) AndAlso (String.Compare(LabelList(j).ParentNode.Name, "book-part", True) = 0)) Then
                        LabelList(j).ParentNode.RemoveChild(LabelList(j))
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        End If

        Dim IndexEntry As XmlNodeList = xmlFloat.SelectNodes("//index-entry/term")
        If ((IndexEntry IsNot Nothing) AndAlso (IndexEntry.Count > 0)) Then
            For tx As Integer = 0 To IndexEntry.Count - 1
                If (IndexEntry(tx).InnerXml.Trim().EndsWith(",")) Then
                    IndexEntry(tx).InnerXml = IndexEntry(tx).InnerXml.Trim().TrimEnd(",")
                End If
            Next
        End If

        'Dim IndexEntry As XmlNodeList = xmlFloat.SelectNodes("//index-entry")
        ''Dim IndexEntry As XmlNodeList = xmlFloat.SelectNodes("//index-entry[not (descendant::index-entry)]")
        'If ((IndexEntry IsNot Nothing) AndAlso (IndexEntry.Count > 0)) Then
        '    For tx As Integer = 0 To IndexEntry.Count - 1
        '        Try
        '            If (IndexEntry(tx).InnerXml.Contains("<term") AndAlso (IndexEntry(tx).InnerXml.Contains("<nav-pointer-group"))) Then
        '                If (IndexEntry(tx).InnerXml.Contains("</term><nav-pointer-group>")) Then
        '                    Continue For
        '                End If
        '                'File.AppendAllText("D:\Support\TandF_Cleanup\01-04-2019\K23046\Test\log.xml", IndexEntry(tx).InnerXml & vbNewLine)
        '                Try
        '                    'IndexEntry(tx).InnerXml = IndexEntry(tx).InnerXml.Replace("</nav-pointer-group></term>", "</nav-pointer-group>").Replace("<nav-pointer-group>", "</term><nav-pointer-group>")
        '                    IndexEntry(tx).InnerXml = Regex.Replace(IndexEntry(tx).InnerXml.Replace("</term>", ""), "(<term>(((?!<nav-pointer).)*))", "$1</term>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
        '                Catch ex As Exception
        '                    'File.AppendAllText("D:\Support\TandF_Cleanup\01-04-2019\K23046\Test\log.xml", "Error" & vbNewLine)
        '                    MessageBox.Show(ex.Message)
        '                End Try
        '            End If
        '        Catch ex As Exception
        '            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
        '            Continue For
        '        End Try
        '    Next
        'End If

        Dim ContribLst As XmlNodeList = xmlFloat.SelectNodes("//contrib-group/contrib/name")
        If ((ContribLst IsNot Nothing) AndAlso (ContribLst.Count > 0)) Then
            For Each Con As XmlNode In ContribLst
                Try
                    Con.InnerXml = Con.InnerXml.Replace("Edited by ", "")
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        End If

        Dim Permissions As XmlNodeList = xmlFloat.SelectNodes("//publisher/permissions")
        If ((Permissions IsNot Nothing) AndAlso (Permissions.Count > 0)) Then
            For pm As Integer = 0 To Permissions.Count - 1
                Try
                    Permissions(pm).ParentNode.ParentNode.InsertAfter(Permissions(pm), Permissions(pm).ParentNode)
                    'Permissions(pm).ParentNode.RemoveChild(Permissions(pm))
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        End If

        Dim AckList As XmlNodeList = xmlFloat.SelectNodes("//ack")
        If ((AckList IsNot Nothing) AndAlso (AckList.Count > 0)) Then
            For Each ackNode As XmlNode In AckList
                Try
                    If ((ackNode.PreviousSibling IsNot Nothing) AndAlso (ackNode.PreviousSibling.NodeType = XmlNodeType.Element) AndAlso (String.Compare(ackNode.PreviousSibling.Name, "notes", True) <> 0)) Then
                        ackNode.PreviousSibling.PrependChild(ackNode)
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        End If

        'Dim IndexEntryList As XmlNodeList = xmlFloat.SelectNodes("//index-entry")
        'If ((IndexEntryList IsNot Nothing) AndAlso (IndexEntryList.Count > 0)) Then
        '    For Each indeNode As XmlNode In IndexEntryList
        '        Try
        '            indeNode.Attributes.Remove(indeNode.Attributes("xml:id"))
        '        Catch ex As Exception
        '            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
        '            Continue For
        '        End Try
        '    Next
        'End If

        If (Not IsePubXML) Then
            Dim UrlCont As String = String.Empty
            Dim UriList As XmlNodeList = xmlFloat.SelectNodes("//uri")
            If ((UriList IsNot Nothing) AndAlso (UriList.Count > 0)) Then
                For Each UrlNode As XmlNode In UriList
                    Try
                        UrlCont = UrlNode.Attributes("xlink:href").Value.TrimEnd(".")
                    Catch ex As Exception
                        Continue For
                    End Try
                    If (Not String.IsNullOrEmpty(UrlCont)) Then
                        UrlCont = Regex.Replace(UrlCont, "(.*?)(http)", "$2", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    End If
                    If (Not String.IsNullOrEmpty(UrlCont.Trim()) And (Not UrlCont.Trim().StartsWith("http"))) Then
                        UrlCont = String.Format("http://{0}", UrlCont)
                    End If
                    UrlNode.Attributes("xlink:href").Value = UrlCont.Trim()
                Next
            End If
        End If

        Dim BookPartList As XmlNodeList = xmlFloat.SelectNodes("//book/book-part")
        Dim BookFrontNode As XmlNode = xmlFloat.SelectSingleNode("//book/book-front[count(following-sibling::book-front)=0]")
        If ((BookPartList IsNot Nothing) AndAlso (BookPartList.Count > 0)) Then
            If (BookFrontNode IsNot Nothing) Then
                Dim BodyNode As XmlNode = xmlFloat.CreateNode(XmlNodeType.Element, "body", "")
                BookFrontNode.ParentNode.InsertAfter(BodyNode, BookFrontNode)
                For e As Integer = 0 To BookPartList.Count - 1
                    BodyNode.AppendChild(BookPartList(e))
                Next
            End If
        End If

        Dim TableFoot As XmlNodeList = xmlFloat.SelectNodes("//table-wrap-foot")
        If ((TableFoot IsNot Nothing) AndAlso (TableFoot.Count > 0)) Then
            For rt As Integer = 0 To TableFoot.Count - 1
                TableFoot(rt).InnerXml = String.Format("<attrib>{0}</attrib>", TableFoot(rt).InnerXml)
            Next
        End If

        Dim PossibleEleList As New List(Of String)
        PossibleEleList.AddRange(New String() {"disp-formula", "list", "fig", "table-wrap", "disp-quote", "boxed-text"})

        For pt As Integer = 0 To PossibleEleList.Count
            Dim ParagraphList As XmlNodeList = xmlFloat.SelectNodes("//p")
            If ((ParagraphList IsNot Nothing) AndAlso (ParagraphList.Count > 0)) Then
                For e As Integer = 0 To ParagraphList.Count - 1
                    'If (ParaList(e).InnerText.Trim().EndsWith(":")) Then
                    If ((ParagraphList(e).NextSibling IsNot Nothing) AndAlso (ParagraphList(e).NextSibling.NodeType = XmlNodeType.Element)) Then
                        Dim NextNode As XmlNode = ParagraphList(e).NextSibling
                        If ((From n In PossibleEleList Where String.Compare(n, NextNode.Name, True) = 0 Select n).Any) Then
                            ParagraphList(e).AppendChild(NextNode)
                        End If
                    End If
                    'End If
                Next
            End If
        Next

        Dim ParaList As XmlNodeList = xmlFloat.SelectNodes("//p")
        Dim ParaXML As String = String.Empty
        If ((ParaList IsNot Nothing) AndAlso (ParaList.Count > 0)) Then
            For p As Integer = 0 To ParaList.Count - 1
                ParaXML = ParaList(p).InnerXml
                Dim FloatNodes As XmlNodeList = ParaList(p).SelectNodes(".//fig|.//table-wrap")
                If ((FloatNodes IsNot Nothing) AndAlso (FloatNodes.Count > 0)) Then
                    For f As Integer = 0 To FloatNodes.Count - 1
                        ParaXML = ParaXML.Replace(FloatNodes(f).OuterXml, "") & FloatNodes(f).OuterXml
                    Next
                End If
                ParaList(p).InnerXml = ParaXML
            Next
        End If

        Dim curCulture As CultureInfo = Thread.CurrentThread.CurrentCulture
        Dim tInfo As TextInfo = curCulture.TextInfo()



        Dim PageIDLst As XmlNodeList = xmlFloat.SelectNodes("//xref[@ref-type='page']")
        Dim CurrentID As Integer = 0
        Dim NextID As Integer = 0
        For i As Integer = 0 To PageIDLst.Count - 1
            Try
                If (i <> (PageIDLst.Count - 1)) Then
                    If (Regex.Match(PageIDLst(i).Attributes("id").Value.Replace("page_", ""), "[^0-9]").Success) Then
                        Continue For
                    End If
                    CurrentID = PageIDLst(i).Attributes("id").Value.Replace("page_", "")
                    If (Not String.IsNullOrEmpty(CurrentID)) Then
                        NextID = (CurrentID + 1)
                        If (Not (From n In PageIDLst Where (String.Compare(n.Attributes("id").value, String.Format("page_" & NextID), True) = 0) Select n).Any) Then
                            If (PageIDLst(i).ParentNode IsNot Nothing) Then
                                PageIDLst(i).ParentNode.InnerXml = PageIDLst(i).ParentNode.InnerXml.Replace(PageIDLst(i).OuterXml, PageIDLst(i).OuterXml & "<xref ref-type=""page"" id=""page_" & NextID & """/>")
                            End If
                            GBL.DeantaBallon("New page : " & NextID, MessageType.MSGINFO)
                        End If
                    Else
                        GBL.DeantaBallon("Invalid page format" & PageIDLst(i).OuterXml, MessageType.MSGERROR)
                        Continue For
                    End If
                End If
            Catch ex As Exception
                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            End Try
        Next



        Dim GrammerList As XmlNodeList = xmlFloat.SelectNodes("//xref[@ref-type='grammer']")
        If ((GrammerList IsNot Nothing) AndAlso (GrammerList.Count > 0)) Then
            For g As Integer = 0 To GrammerList.Count - 1
                If (GrammerList(g).ParentNode IsNot Nothing) Then
                    GrammerList(g).ParentNode.InnerXml = GrammerList(g).ParentNode.InnerXml.Replace(GrammerList(g).OuterXml, GrammerList(g).InnerXml)
                End If
            Next
        End If

        GrammerList = xmlFloat.SelectNodes("//xref[@ref-type='grammer2']")
        If ((GrammerList IsNot Nothing) AndAlso (GrammerList.Count > 0)) Then
            For g As Integer = 0 To GrammerList.Count - 1
                If (GrammerList(g).ParentNode IsNot Nothing) Then
                    GrammerList(g).ParentNode.InnerXml = GrammerList(g).ParentNode.InnerXml.Replace(GrammerList(g).OuterXml, GrammerList(g).InnerXml)
                End If
            Next
        End If


        Dim Spellingerrors As XmlNodeList = xmlFloat.SelectNodes("//spellingerrors")
        If ((Spellingerrors IsNot Nothing) AndAlso (Spellingerrors.Count > 0)) Then
            For s As Integer = 0 To Spellingerrors.Count - 1
                If (Spellingerrors(s).ParentNode IsNot Nothing) Then
                    Spellingerrors(s).ParentNode.RemoveChild(Spellingerrors(s))
                End If
            Next
        End If

        BookPartList = xmlFloat.SelectNodes("//book-part[@book-part-type='part']")
        Dim IDAttrib As String = String.Empty
        If ((BookPartList IsNot Nothing) AndAlso (BookPartList.Count > 0)) Then
            For pt As Integer = 0 To BookPartList.Count - 1
                Try
                    IDAttrib = BookPartList(pt).Attributes("id").Value
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
                If (IsePubXML) Then
                    If (Not String.IsNullOrEmpty(IDAttrib)) Then
                        BookPartList(pt).Attributes("id").Value = $"{IDAttrib}{pt + 1}"
                    End If
                Else
                    If (Not String.IsNullOrEmpty(IDAttrib)) Then
                        BookPartList(pt).Attributes("id").Value = $"part{pt + 1}"
                    End If
                End If

            Next
        End If



        Dim PageTagList As XmlNodeList = xmlFloat.SelectNodes("//xref[@ref-type='page']")
        Dim IncludeList As New List(Of String)
        IncludeList.AddRange(New String() {"title", "chapter", "dedication", "preface"})
        Dim PrevCnt As Int16 = 0
        If ((PageTagList IsNot Nothing) AndAlso (PageTagList.Count > 0)) Then
            For pg As Integer = 0 To PageTagList.Count - 1
                PrevCnt = 0
                If (PageTagList(pg).NextSibling Is Nothing) OrElse (PageTagList(pg).NextSibling.NodeType <> XmlNodeType.Element) Then
                    Continue For
                End If
                Dim PrevNode As XmlNode = PageTagList(pg).NextSibling
                While (PrevCnt <= 4)
                    If ((PrevNode IsNot Nothing) AndAlso ((From n In IncludeList Where (String.Compare(PrevNode.Name, n, True) = 0) Select n).Any)) Then
                        PrevNode.PrependChild(PageTagList(pg))
                        Exit While
                    End If
                    If (PrevNode IsNot Nothing) Then
                        PrevNode = PrevNode.NextSibling
                    Else
                        Exit While
                    End If
                    PrevCnt = PrevCnt + 1
                End While
            Next
        End If

        Dim GivenNamesLst As XmlNodeList = xmlFloat.SelectNodes("//mixed-citation//given-names")
        If ((GivenNamesLst IsNot Nothing) AndAlso (GivenNamesLst.Count > 0)) Then
            For t As Integer = 0 To GivenNamesLst.Count - 1
                Try
                    If (Not GivenNamesLst(t).InnerText.Trim().EndsWith(".")) Then
                        GivenNamesLst(t).InnerXml = String.Format("{0}.", GivenNamesLst(t).InnerXml)
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        End If

        GivenNamesLst = xmlFloat.SelectNodes("//mixed-citation//surname")

        If ((GivenNamesLst IsNot Nothing) AndAlso (GivenNamesLst.Count > 0)) Then
            For t As Integer = 0 To GivenNamesLst.Count - 1
                Try
                    If (GivenNamesLst(t).InnerXml.EndsWith(",")) Then
                        If (GivenNamesLst(t).ParentNode IsNot Nothing) Then
                            GivenNamesLst(t).ParentNode.InnerXml = GivenNamesLst(t).ParentNode.InnerXml.Replace(GivenNamesLst(t).OuterXml, String.Format("<surname>{0}</surname>,", GivenNamesLst(t).InnerXml.Replace(",", "")))
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        End If

        Dim LabelLst As XmlNodeList = xmlFloat.SelectNodes("//label|//title|//def-item/term")
        If ((LabelLst IsNot Nothing) AndAlso (LabelLst.Count > 0)) Then
            For ll As Integer = 0 To LabelLst.Count - 1
                If (LabelLst(ll).NodeType = XmlNodeType.Element) Then
                    Try
                        LabelLst(ll).InnerXml = LabelLst(ll).InnerXml.Replace("<bold>", "").Replace("</bold>", "")
                        If (String.Compare(LabelLst(ll).Name, "label", True) = 0) Then
                            LabelLst(ll).InnerXml = LabelLst(ll).InnerXml.Replace("(", "").Replace(")", "")
                        End If
                    Catch ex As Exception
                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    End Try
                End If
            Next
        End If

        Dim GraphicList As XmlNodeList = xmlFloat.SelectNodes("//disp-formula/graphic|//inline-formula/inline-graphic")
        If ((GraphicList IsNot Nothing) AndAlso (GraphicList.Count > 0)) Then
            For g As Integer = 0 To GraphicList.Count - 1
                If ((GraphicList(g).PreviousSibling IsNot Nothing) AndAlso (String.Compare(GraphicList(g).PreviousSibling.Name, "alternatives", True) = 0)) Then
                    GraphicList(g).PreviousSibling.AppendChild(GraphicList(g))
                End If
            Next
        End If


        LabelLst = xmlFloat.SelectNodes("//fig/label|//table-wrap/label")
        If ((LabelLst IsNot Nothing) AndAlso (LabelLst.Count > 0)) Then
            For ll As Integer = 0 To LabelLst.Count - 1
                LabelLst(ll).InnerText = curCulture.TextInfo.ToTitleCase(LabelLst(ll).InnerText.ToLower())
            Next
        End If
        LabelLst = xmlFloat.SelectNodes("//label")
        If ((LabelLst IsNot Nothing) AndAlso (LabelLst.Count > 0)) Then
            For l As Integer = LabelLst.Count - 1 To 0 Step -1
                Try
                    If (String.IsNullOrEmpty(LabelList(l).InnerText)) Then
                        If (LabelLst(l).ParentNode IsNot Nothing) Then
                            LabelLst(l).ParentNode.RemoveChild(LabelLst(l))
                        End If
                    End If
                Catch ex As Exception
                End Try
            Next
        End If

        Dim YearList As XmlNodeList = xmlFloat.SelectNodes("//mixed-citation/year")
        If ((YearList IsNot Nothing) AndAlso (YearList.Count > 0)) Then
            Dim YearMat As Match = Nothing
            For y As Integer = 0 To YearList.Count - 1
                Try
                    YearMat = Regex.Match(YearList(y).InnerText, "[^0-9]+", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                    If ((YearMat.Success) And (YearMat.Value.Length = 1)) Then
                        If (YearList(y).ParentNode IsNot Nothing) Then
                            YearList(y).ParentNode.InnerXml = YearList(y).ParentNode.InnerXml.Replace(YearList(y).OuterXml, String.Format("<year>{0}</year>", YearList(y).InnerXml.Replace(YearMat.Value, "")) & YearMat.Value)
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        End If

        If (IsePubXML) Then
            Dim XrefList As XmlNodeList = xmlFloat.SelectNodes("//xref[@rid]")
            Dim RidAttr As XmlAttribute = Nothing
            If ((XrefList IsNot Nothing) AndAlso (XrefList.Count > 0)) Then
                For x As Integer = 0 To XrefList.Count - 1
                    Try
                        RidAttr = XrefList(x).Attributes("rid")
                    Catch ex As Exception
                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    End Try
                    If (RidAttr IsNot Nothing) Then
                        RidAttr.Value = RidAttr.Value.Replace(" ", "")
                    End If
                Next
            End If
        End If

        Dim EditionLst As XmlNodeList = xmlFloat.SelectNodes("//edition")
        If ((EditionLst IsNot Nothing) AndAlso (EditionLst.Count > 0)) Then
            For i As Integer = 0 To EditionLst.Count - 1
                If ((Not String.IsNullOrEmpty(EditionLst(i).InnerText)) AndAlso (EditionLst(i).InnerText.Contains(" ")) AndAlso (Regex.Match(EditionLst(i).InnerText, "^[0-9a-zA-Z]{1,3}", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Success)) Then
                    Dim EditionText As String = Regex.Match(EditionLst(i).InnerText, "^[0-9a-zA-Z]{1,3}", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Value
                    EditionText = EditionLst(i).InnerText.Replace(EditionText & " ", "")
                    If (Not String.IsNullOrEmpty(EditionText)) Then
                        If (EditionLst(i).ParentNode IsNot Nothing) Then
                            EditionLst(i).ParentNode.InnerXml = EditionLst(i).ParentNode.InnerXml.Replace(EditionLst(i).OuterXml, EditionLst(i).OuterXml.Replace(" " & EditionText, "") & " " & EditionText)
                        End If
                    End If
                End If
            Next
        End If

        Dim EntityList As XmlNodeList = xmlFloat.SelectNodes("//entry[@namest]|//entry[@nameend]")
        If ((EntityList IsNot Nothing) AndAlso (EntityList.Count > 0)) Then
            For t As Integer = 0 To EntityList.Count - 1
                Try
                    EntityList(t).Attributes("namest").Value = String.Format("col{0}", EntityList(t).Attributes("namest").Value.Replace("col", ""))
                Catch ex As Exception
                End Try
                Try
                    EntityList(t).Attributes("nameend").Value = String.Format("col{0}", EntityList(t).Attributes("nameend").Value.Replace("col", ""))
                Catch ex As Exception
                End Try
            Next
        End If

        Dim MoreRowList As XmlNodeList = xmlFloat.SelectNodes("//entry[@rowspan]")
        If ((MoreRowList IsNot Nothing) AndAlso (MoreRowList.Count > 0)) Then
            For tw As Integer = 0 To MoreRowList.Count - 1
                Try
                    MoreRowList(tw).Attributes("rowspan").Value = Convert.ToInt32(MoreRowList(tw).Attributes("rowspan").Value) - 1
                Catch ex As Exception
                End Try

                If (MoreRowList(tw).ParentNode IsNot Nothing) Then
                    Try
                        MoreRowList(tw).ParentNode.InnerXml = MoreRowList(tw).ParentNode.InnerXml.Replace(" rowspan=""", " morerows=""")
                    Catch ex As Exception
                    End Try
                End If
            Next
        End If

        Dim TGroupLst As XmlNodeList = xmlFloat.SelectNodes("//tgroup")
        Dim RowNode As XmlNode = Nothing
        If ((TGroupLst IsNot Nothing) AndAlso (TGroupLst.Count > 0)) Then
            For t As Integer = 0 To TGroupLst.Count - 1
                Try
                    RowNode = GetMaxCountTdInTable(TGroupLst(t), "row", "entry")
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
                If (RowNode IsNot Nothing) Then
                    Try
                        TGroupLst(t).Attributes("cols").Value = RowNode.ChildNodes.Count
                    Catch ex As Exception
                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                        Continue For
                    End Try
                End If
            Next
        End If

        Dim SimpleList As XmlNodeList = xmlFloat.SelectNodes("//list[not (@list-type)]")
        If ((SimpleList IsNot Nothing) AndAlso (SimpleList.Count > 0)) Then
            For s As Integer = 0 To SimpleList.Count - 1
                Dim SlAttrib As XmlAttribute = xmlFloat.CreateNode(XmlNodeType.Attribute, "list-type", "")
                SlAttrib.Value = "simple"
                Try
                    SimpleList(s).Attributes.Append(SlAttrib)
                Catch ex As Exception
                End Try
            Next
        End If


        Dim DefItemList As XmlNodeList = xmlFloat.SelectNodes("//def-item/term")
        If ((DefItemList IsNot Nothing) AndAlso (DefItemList.Count - 1)) Then
            For d As Integer = 0 To DefItemList.Count - 1
                Try
                    If (Not String.IsNullOrEmpty(DefItemList(d).InnerXml) AndAlso (DefItemList(d).InnerXml.EndsWith("#x2013;"))) Then
                        If ((DefItemList(d).NextSibling IsNot Nothing) AndAlso (DefItemList(d).NextSibling.ChildNodes IsNot Nothing) AndAlso (DefItemList(d).NextSibling.ChildNodes.Count > 0)) Then
                            If (String.Compare(DefItemList(d).NextSibling.ChildNodes(0).Name, "p", True) = 0) Then
                                DefItemList(d).NextSibling.ChildNodes(0).InnerXml = "&amp;#x2013; " & DefItemList(d).NextSibling.ChildNodes(0).InnerXml
                                DefItemList(d).InnerXml = DefItemList(d).InnerXml.Replace(" &amp;#x2013;", "")
                            End If
                        End If
                    ElseIf (Not String.IsNullOrEmpty(DefItemList(d).InnerXml) AndAlso (DefItemList(d).InnerXml.EndsWith(":"))) Then
                        If ((DefItemList(d).NextSibling IsNot Nothing) AndAlso (DefItemList(d).NextSibling.ChildNodes IsNot Nothing) AndAlso (DefItemList(d).NextSibling.ChildNodes.Count > 0)) Then
                            If (String.Compare(DefItemList(d).NextSibling.ChildNodes(0).Name, "p", True) = 0) Then
                                DefItemList(d).NextSibling.ChildNodes(0).InnerXml = ": " & DefItemList(d).NextSibling.ChildNodes(0).InnerXml
                                DefItemList(d).InnerXml = Regex.Replace(DefItemList(d).InnerXml, " :$", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                            End If
                        End If
                    ElseIf (Not String.IsNullOrEmpty(DefItemList(d).InnerXml) AndAlso (DefItemList(d).InnerXml.EndsWith("-"))) Then
                        If ((DefItemList(d).NextSibling IsNot Nothing) AndAlso (DefItemList(d).NextSibling.ChildNodes IsNot Nothing) AndAlso (DefItemList(d).NextSibling.ChildNodes.Count > 0)) Then
                            If (String.Compare(DefItemList(d).NextSibling.ChildNodes(0).Name, "p", True) = 0) Then
                                DefItemList(d).NextSibling.ChildNodes(0).InnerXml = "- " & DefItemList(d).NextSibling.ChildNodes(0).InnerXml
                                DefItemList(d).InnerXml = Regex.Replace(DefItemList(d).InnerXml, " -$", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                            End If
                        End If
                    End If
                Catch ex As Exception
                End Try
            Next
        End If

        Dim CommaList As New List(Of String)
        CommaList.AddRange(New String() {"source", "chaptertitle", "article-title", "collab", "publisher-name", "location"})

        For a As Integer = 0 To CommaList.Count - 1
            Try
                Dim RemoveCommaLst As XmlNodeList = xmlFloat.SelectNodes("//" & CommaList(a))
                If ((RemoveCommaLst IsNot Nothing) AndAlso (RemoveCommaLst.Count > 0)) Then
                    For n As Integer = 0 To RemoveCommaLst.Count - 1
                        Try
                            If (RemoveCommaLst(n).InnerXml.EndsWith(",")) Then
                                If (RemoveCommaLst(n).ParentNode IsNot Nothing) Then
                                    RemoveCommaLst(n).ParentNode.InnerXml = RemoveCommaLst(n).ParentNode.InnerXml.Replace(RemoveCommaLst(n).OuterXml, RemoveCommaLst(n).OuterXml.Replace(",", "") & ",")
                                End If
                            ElseIf (RemoveCommaLst(n).InnerXml.EndsWith(".")) Then
                                If (RemoveCommaLst(n).ParentNode IsNot Nothing) Then
                                    RemoveCommaLst(n).ParentNode.InnerXml = RemoveCommaLst(n).ParentNode.InnerXml.Replace(RemoveCommaLst(n).OuterXml, RemoveCommaLst(n).OuterXml.Replace(".", "") & ".")
                                End If
                            End If
                        Catch ex As Exception
                            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                            Continue For
                        End Try
                    Next
                End If
            Catch ex As Exception
                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                Continue For
            End Try
        Next
        Dim HolderNode As XmlNode = xmlFloat.SelectSingleNode("//permissions/copyright-statement")
        Dim CopyText As String = String.Empty
        If (HolderNode IsNot Nothing) Then
            CopyText = HolderNode.InnerText.Replace("&#x200A;", "")
            If (Not String.IsNullOrEmpty(CopyText)) Then
                Dim CopyMatch As Match = Regex.Match(CopyText, "&#x00A9; ([\d]{4,4}) (.*?)$", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
                If (CopyMatch.Success) Then
                    If (HolderNode.ParentNode IsNot Nothing) Then
                        HolderNode.ParentNode.InnerXml = HolderNode.ParentNode.InnerXml.Replace(HolderNode.OuterXml, HolderNode.OuterXml & String.Format("<copyright-year>{0}</copyright-year><copyright-holder>{1}</copyright-holder>", CopyMatch.Groups(1).Value, CopyMatch.Groups(2).Value.Replace("&", "&amp;")))
                    End If

                End If
            End If
        End If

        Dim BookMetaNode As XmlNodeList = xmlFloat.SelectNodes("//book-meta//xref[@ref-type='page']")
        If ((BookMetaNode IsNot Nothing) AndAlso (BookMetaNode.Count > 0)) Then
            For aa As Integer = 0 To BookMetaNode.Count - 1
                Try
                    If (BookMetaNode(aa).ParentNode IsNot Nothing) Then
                        BookMetaNode(aa).ParentNode.RemoveChild(BookMetaNode(aa))
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        End If

        Dim XrefLst As XmlNodeList = xmlFloat.SelectNodes("//xref[@ref-type='bibr']/xref[@ref-type='page']")
        If ((XrefLst IsNot Nothing) AndAlso (XrefLst.Count > 0)) Then
            For xf As Integer = 0 To XrefLst.Count - 1
                If (XrefLst(xf).ParentNode IsNot Nothing) Then
                    If (XrefLst(xf).ParentNode.ParentNode IsNot Nothing) Then
                        XrefLst(xf).ParentNode.ParentNode.InsertBefore(XrefLst(xf), XrefLst(xf).ParentNode)
                    End If
                End If
            Next
        End If

        Dim Navpointers As XmlNodeList = xmlFloat.SelectNodes("//nav-pointer[@rid]")
        If ((Navpointers IsNot Nothing) AndAlso (Navpointers.Count > 0)) Then
            Dim RidAttrib As XmlAttribute = Nothing
            For i As Integer = 0 To Navpointers.Count - 1
                Try
                    RidAttrib = Navpointers(i).Attributes("rid")
                Catch ex As Exception
                End Try
                If (RidAttrib IsNot Nothing) Then
                    Try
                        RidAttrib.Value = RidAttrib.Value.Split("n")(0)
                    Catch ex As Exception
                    End Try
                End If
            Next
        End If

        Dim BookMeta As XmlNode = xmlFloat.SelectSingleNode("//book-meta")
        If (BookMeta IsNot Nothing) Then
            BookMeta.InnerXml = String.Format("{0}<notes notes-type=""supplier""><p>Deanta</p></notes>", BookMeta.InnerXml)
        End If

        Dim IsbnNodes As XmlNodeList = xmlFloat.SelectNodes("//isbn[@pub-type='hbk']|//isbn[@pub-type='ebk']")
        If ((IsbnNodes IsNot Nothing) AndAlso (IsbnNodes.Count > 0)) Then
            For i As Integer = 0 To IsbnNodes.Count - 1
                IsbnNodes(i).InnerText = IsbnNodes(i).InnerText.Replace("ISBN: ", "").Replace("-", "").Replace("(hbk)", "").Replace("(ebk)", "").TrimEnd(" ")
            Next
        End If

        Try
            For i As Int16 = 0 To ElementList.Count - 1
                Dim Elements As XmlNodeList = xmlFloat.SelectNodes(String.Format("//{0}", ElementList(i).ElementName))
                MaxCount = 0
                Try
                    If ((Elements IsNot Nothing) AndAlso (Elements.Count > 0)) Then
                        For e As Int16 = 0 To Elements.Count - 1
                            Dim PageList As XmlNodeList = Elements(e).SelectNodes(".//xref[@ref-type='page']")
                            If ((PageList IsNot Nothing) AndAlso (PageList.Count > 0)) Then
                                MaxCount = PageList.Count * 2
                                While ((PageList IsNot Nothing) AndAlso (PageList.Count > 0))
                                    If (MaxCount = 0) Then Exit While
                                    Dim PageNode As XmlNode = PageList(0)
                                    If (Elements(e).ParentNode IsNot Nothing) Then
                                        If (ElementList(i).Position = PagePosition.SUFFIX) Then
                                            Elements(e).ParentNode.InnerXml = Elements(e).ParentNode.InnerXml.Replace(Elements(e).OuterXml, Elements(e).OuterXml.Replace(PageNode.OuterXml, "") & PageNode.OuterXml)
                                        ElseIf (ElementList(i).Position = PagePosition.PREFIX) Then
                                            Elements(e).ParentNode.InnerXml = Elements(e).ParentNode.InnerXml.Replace(Elements(e).OuterXml, PageNode.OuterXml & Elements(e).OuterXml.Replace(PageNode.OuterXml, ""))
                                        End If
                                    End If
                                    PageList = Elements(e).SelectNodes(".//xref[@ref-type='page']")
                                    MaxCount = MaxCount - 1
                                End While
                            End If
                        Next
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Continue For
                End Try
            Next
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
        End Try

        Dim SectionDispNodes As XmlNodeList = xmlFloat.SelectNodes("//sec[@disp-level='']")
        If ((SectionDispNodes IsNot Nothing) AndAlso (SectionDispNodes.Count > 0)) Then
            For s As Int16 = 0 To SectionDispNodes.Count - 1
                Dim Index As Int16 = SectionDispNodes(s).SelectNodes(".//ancestor::sec").Count
                If (Index = 0) Then
                    SectionDispNodes(s).Attributes("disp-level").Value = 1
                Else
                    SectionDispNodes(s).Attributes("disp-level").Value = Index
                End If
            Next
        End If

        Dim RemoveXrefNodes As XmlNodeList = xmlFloat.SelectNodes("//xref[@ref-type='bibr']")
        Dim XrefID As String = String.Empty
        Dim BibNode As XmlNode = Nothing
        If ((RemoveXrefNodes IsNot Nothing) AndAlso (RemoveXrefNodes.Count > 0)) Then
            For r As Int32 = 0 To RemoveXrefNodes.Count - 1
                Try
                    XrefID = RemoveXrefNodes(r).Attributes("rid").Value
                Catch ex As Exception
                    XrefID = String.Empty
                End Try
                Try
                    If (Not String.IsNullOrEmpty(XrefID)) Then
                        BibNode = xmlFloat.SelectSingleNode($"//ref[@id='{XrefID}']")
                        If (BibNode Is Nothing) Then
                            If (RemoveXrefNodes(r).ParentNode IsNot Nothing) Then
                                RemoveXrefNodes(r).ParentNode.InnerXml = RemoveXrefNodes(r).ParentNode.InnerXml.Replace(RemoveXrefNodes(r).OuterXml, RemoveXrefNodes(r).InnerXml)
                            End If
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
            Next
        End If


        Dim BookPartNodes As XmlNodeList = xmlFloat.SelectNodes("//book-part[@book-part-type='part']/book-part[@book-part-type='chapter']")
        If ((BookPartNodes IsNot Nothing) AndAlso (BookPartNodes.Count > 0)) Then
            For b As Int16 = 0 To BookPartNodes.Count - 1
                If (BookPartNodes(b).ParentNode IsNot Nothing) Then
                    BookPartNodes(b).ParentNode.InnerXml = BookPartNodes(b).ParentNode.InnerXml.Replace(BookPartNodes(b).OuterXml, BookPartNodes(b).InnerXml)
                End If
            Next
        End If

        BookPartList = xmlFloat.SelectNodes("//book-part[@book-part-type='chapter']")
        IDAttrib = String.Empty
        If ((BookPartList IsNot Nothing) AndAlso (BookPartList.Count > 0)) Then
            For pt As Integer = 0 To BookPartList.Count - 1
                Try
                    IDAttrib = BookPartList(pt).Attributes("id").Value
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                End Try
                BookPartList(pt).Attributes("id").Value = $"chapter{pt + 1}"
            Next
        End If

        BookPartList = xmlFloat.SelectNodes("//book-part/book-part-meta/title-group")
        If ((BookPartList IsNot Nothing) AndAlso (BookPartList.Count > 0)) Then
            For dd As Integer = 0 To BookPartList.Count - 1
                Dim TitleNode As XmlNode = BookPartList(dd)
                If (TitleNode.Attributes("label") Is Nothing) Then
                    If ((TitleNode.ParentNode IsNot Nothing) AndAlso (TitleNode.ParentNode.ParentNode IsNot Nothing)) Then
                        Try
                            Dim MatLabel As Match = Regex.Match(TitleNode.ParentNode.ParentNode.Attributes("id").Value, "(chapter)([0-9]+)", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                            If ((MatLabel IsNot Nothing) AndAlso (MatLabel.Success)) Then
                                Dim LabelNode As XmlNode = xmlFloat.CreateNode(XmlNodeType.Element, "label", "")
                                'LabelNode.InnerText = String.Format("{0} {1}", tInfo.ToTitleCase(MatLabel.Groups(1).Value), MatLabel.Groups(2).Value)
                                LabelNode.InnerText = String.Format("{0}", MatLabel.Groups(2).Value)
                                TitleNode.PrependChild(LabelNode)
                            End If
                        Catch ex As Exception
                            Continue For
                        End Try
                    End If
                End If
            Next
        End If

        BookPartNodes = xmlFloat.SelectNodes("//book-part")
        If ((BookPartNodes IsNot Nothing) AndAlso (BookPartNodes.Count > 0)) Then
            For b As Int16 = 0 To BookPartNodes.Count - 1
                Dim BodyNode As XmlNode = BookPartNodes(b).SelectSingleNode(".//body")
                Dim BackftGroupNode As XmlNode = BookPartNodes(b).SelectSingleNode(".//back[child::fn-group]")
                If ((BodyNode IsNot Nothing) AndAlso (BackftGroupNode IsNot Nothing)) Then
                    BodyNode.ParentNode.InsertAfter(BackftGroupNode, BodyNode)
                End If
            Next
        End If

        If (Not IsePubXML) Then
            BookPartNodes = xmlFloat.SelectNodes("//book-part[@book-part-type='part']//title[count(child::xref)>1]")
            If ((BookPartNodes IsNot Nothing) AndAlso (BookPartNodes.Count > 0)) Then
                For p As Int16 = 0 To BookPartNodes.Count - 1
                    Dim lastXrefNode As XmlNode = BookPartNodes(p).SelectSingleNode(".//xref[last()]")
                    If ((lastXrefNode IsNot Nothing) And (lastXrefNode.ParentNode IsNot Nothing)) Then
                        lastXrefNode.ParentNode.RemoveChild(lastXrefNode)
                    End If
                Next
            End If
        End If

        Dim RefLableNodes As XmlNodeList = xmlFloat.SelectNodes("//mixed-citation/label")
        If ((RefLableNodes IsNot Nothing) AndAlso (RefLableNodes.Count > 0)) Then
            For r As Int16 = 0 To RefLableNodes.Count - 1
                If (RefLableNodes(r).ParentNode IsNot Nothing) AndAlso (RefLableNodes(r).ParentNode.ParentNode IsNot Nothing) Then
                    RefLableNodes(r).ParentNode.ParentNode.InsertBefore(RefLableNodes(r), RefLableNodes(r).ParentNode)
                End If
            Next
        End If

        If (Not IsePubXML) Then
            BookPartNodes = xmlFloat.SelectNodes("//book-part[@book-part-type='part']")
            Dim ChapNode As XmlNode = Nothing
            Dim TmpChapNode As XmlNode = Nothing
            Dim PartBody As XmlNode = Nothing
            If ((BookPartNodes IsNot Nothing) AndAlso (BookPartNodes.Count > 0)) Then
                While ((BookPartNodes IsNot Nothing) AndAlso (BookPartNodes.Count > 0))
                    Try
                        Dim PartNode As XmlNode = BookPartNodes(0)
                        PartBody = BookPartNodes(0).SelectSingleNode("./body")
                        ChapNode = PartNode.NextSibling
                        While ((ChapNode IsNot Nothing) AndAlso (String.Compare(ChapNode.Attributes("book-part-type").Value, "chapter", True) = 0))
                            TmpChapNode = ChapNode.NextSibling
                            PartBody.AppendChild(ChapNode)
                            ChapNode = TmpChapNode
                        End While
                        PartNode.Attributes("book-part-type").Value = "partt"
                    Catch ex As Exception
                        GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    End Try
                    BookPartNodes = xmlFloat.SelectNodes("//book-part[@book-part-type='part']")
                End While
            End If
        End If

        BookPartList = xmlFloat.SelectNodes("//book-part")
        If ((BookPartList IsNot Nothing) AndAlso (BookPartList.Count > 0)) Then
            For pt As Integer = 0 To BookPartList.Count - 1
                Dim XrefPageList As XmlNodeList = BookPartList(pt).SelectNodes(".//xref[@ref-type='page']")
                Dim BodyNode As XmlNode = BookPartList(pt).SelectSingleNode(".//book-part-meta")
                If ((XrefPageList IsNot Nothing) AndAlso (XrefPageList.Count > 0) AndAlso (BodyNode IsNot Nothing)) Then
                    Dim FirstPageNode As XmlNode = xmlFloat.CreateNode(XmlNodeType.Element, "fpage", "")
                    Dim LastPageNode As XmlNode = xmlFloat.CreateNode(XmlNodeType.Element, "lpage", "")
                    Try
                        FirstPageNode.InnerText = XrefPageList(0).Attributes("id").Value.ToLower().Replace("page_", "")
                    Catch ex As Exception
                        Continue For
                    End Try
                    Try
                        LastPageNode.InnerText = XrefPageList(XrefPageList.Count - 1).Attributes("id").Value.ToLower().Replace("page_", "")
                    Catch ex As Exception
                        Continue For
                    End Try
                    If (BodyNode.ParentNode IsNot Nothing) Then
                        BodyNode.AppendChild(FirstPageNode)
                        BodyNode.AppendChild(LastPageNode)
                    End If
                End If
            Next
        End If


        If (Not IsePubXML) Then
            Dim FngGroupList As XmlNodeList = xmlFloat.SelectNodes("//fn-group")
            If ((FngGroupList IsNot Nothing) AndAlso (FngGroupList.Count > 0)) Then
                For a As Int16 = 0 To FngGroupList.Count - 1
                    If ((FngGroupList(a).PreviousSibling IsNot Nothing) AndAlso (String.Compare(FngGroupList(a).PreviousSibling.Name, "ref-list", True) = 0)) Then
                        If (FngGroupList(a).ParentNode IsNot Nothing) Then
                            FngGroupList(a).ParentNode.InsertBefore(FngGroupList(a), FngGroupList(a).PreviousSibling)
                        End If
                    End If
                Next
            End If
        End If

        '18-09-2020

        Dim GraphicNodes As XmlNodeList = xmlFloat.SelectNodes("//p[child::p/graphic]")
        If ((GraphicNodes IsNot Nothing) AndAlso (GraphicNodes.Count > 0)) Then
            MaxCount = GraphicNodes.Count * 2
            Dim GrphCount As Int16 = 0
            While ((GraphicNodes IsNot Nothing) AndAlso (GraphicNodes.Count > 0))
                If (GrphCount = MaxCount) Then
                    Exit While
                End If
                Try
                    If (GraphicNodes(0).ParentNode IsNot Nothing) Then
                        If (GraphicNodes(0).ParentNode.InnerXml.Contains("<p><p><graphic")) Then
                            'GraphicNodes(0).ParentNode.InnerXml = GraphicNodes(0).ParentNode.InnerXml.Replace("<p><p><graphic", "<p><graphic").Replace("</p></p>", "</p>")  '12-10-2020
                            GraphicNodes(0).ParentNode.InnerXml = GraphicNodes(0).ParentNode.InnerXml.Replace(GraphicNodes(0).OuterXml, GraphicNodes(0).InnerXml)
                        End If
                    End If
                Catch ex As Exception
                    GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
                    Return False
                End Try
                GraphicNodes = xmlFloat.SelectNodes("//p[child::p/graphic]")
                GrphCount = GrphCount + 1
            End While
        End If

        'this is wrongly renumbered the footnote label . LPOM chapter 7 - 118 footnote
        'Dim FnGroupLst As XmlNodeList = xmlFloat.SelectNodes("//fn-group/fn")
        'Dim FnID As String = String.Empty
        'Dim LablNode As XmlNode = Nothing
        'Dim XrefNode As XmlNode = Nothing
        'If ((FnGroupLst IsNot Nothing) AndAlso (FnGroupLst.Count > 0)) Then
        '    For g As Integer = 0 To FnGroupLst.Count - 1
        '        LablNode = Nothing
        '        XrefNode = Nothing
        '        FnID = String.Empty
        '        Try
        '            If ((FnGroupLst(g).Attributes IsNot Nothing) AndAlso (FnGroupLst(g).Attributes.Count > 0)) Then
        '                FnID = FnGroupLst(g).Attributes("id").Value
        '            End If
        '            If (Not String.IsNullOrEmpty(FnID)) Then
        '                XrefNode = xmlFloat.SelectSingleNode("//xref[@ref-type='fn'][@rid='" & FnID & "']")
        '            End If
        '            If (XrefNode IsNot Nothing) Then
        '                LablNode = GetChildNode(FnGroupLst(g), "label")
        '            Else
        '                GBL.DeantaBallon("Footnote xref not found. " & FnID, MessageType.MSGERROR)
        '                Continue For
        '            End If
        '            If ((LablNode IsNot Nothing) AndAlso (XrefNode IsNot Nothing)) Then
        '                If (XrefNode.InnerText <> LablNode.InnerText) Then
        '                    LablNode.InnerText = XrefNode.InnerText
        '                End If
        '            Else
        '                GBL.DeantaBallon("Footnote xref and label not found. " & FnID, MessageType.MSGERROR)
        '                Continue For
        '            End If
        '        Catch ex As Exception
        '            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
        '            Continue For
        '        End Try
        '    Next
        'End If

        Dim xmlTemp As String = xmlFloat.OuterXml.Replace("&amp;", "&")

        Dim IsbnEbkNode As XmlNode = xmlFloat.SelectSingleNode("//isbn[@pub-type='ebk']")

        If (IsbnEbkNode IsNot Nothing) Then
            xmlTemp = Regex.Replace(xmlTemp, "<book-meta>", "<book-meta><book-id pub-id-type=""doi"">10.4324/" & IsbnEbkNode.InnerText & "</book-id>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Else
            xmlTemp = Regex.Replace(xmlTemp, "<book-meta>", "<book-meta><book-id pub-id-type=""doi"">10.4324/</book-id>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        If ((xmlTemp.Contains("</permissions><imprint-text>")) And (xmlTemp.Contains("</imprint-text><notes"))) Then
            xmlTemp = xmlTemp.Replace("</permissions><imprint-text>", "</permissions><imprint-meta><imprint-text>").Replace("</imprint-text><notes", "</imprint-text></imprint-meta><notes")
        End If
        xmlTemp = Regex.Replace(xmlTemp, "</contrib-group><contrib-group[^>]*>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase).Replace("</con-group>", "").Replace("<con-group>", "")
        xmlTemp = xmlTemp.Replace("</table-wrap-foot><table-wrap-foot>", "")
        xmlTemp = Regex.Replace(xmlTemp, "(<imprint-text[^>]*>)(<imprint-text[^>]*>)", "$1", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
        xmlTemp = xmlTemp.Replace("</imprint-text></imprint-text>", "</imprint-text>")
        xmlTemp = xmlTemp.Replace("<publisher><publisher>", "<publisher>").Replace("</publisher></publisher>", "</publisher>")
        ''18-09-2020
        'If (xmlTemp.Contains("<p><p><graphic")) Then
        '    xmlTemp = xmlTemp.Replace("<p><p><graphic", "<p><graphic").Replace("/></p></p>", "/></p>")
        'End If
        xmlTemp = xmlTemp.Replace("</book-front><book-front>", "")
        xmlTemp = Regex.Replace(xmlTemp, "(<uri[^>]*>)(<uri[^>]*>)", "$1").Replace("</uri></uri>", "</uri>")
        xmlTemp = Regex.Replace(xmlTemp, "<label[\s]*/>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
        xmlTemp = Regex.Replace(xmlTemp, "<year[\s]*/>", "", RegexOptions.Singleline Or RegexOptions.IgnoreCase)


        '22-03-2019 ' DEPS_C002
        'xmlTemp = Regex.Replace(xmlTemp, "(</xref>)( and )(<xref[^>]*>)", Function(xMat As Match)
        '                                                                      Try
        '                                                                          If ((xMat.Groups IsNot Nothing) AndAlso (xMat.Groups.Count > 0)) Then
        '                                                                              If ((xMat.Groups(3) IsNot Nothing) AndAlso (Not String.IsNullOrEmpty(xMat.Groups(3).Value))) Then
        '                                                                                  If (xMat.Groups(3).Value.Contains("ref-type=""bibr""")) Then
        '                                                                                      Return xMat.Groups(2).Value
        '                                                                                  End If
        '                                                                              End If
        '                                                                          End If
        '                                                                          Return xMat.Value
        '                                                                      Catch ex As Exception
        '                                                                          Return xMat.Value
        '                                                                      End Try
        '                                                                  End Function, RegexOptions.Singleline Or RegexOptions.IgnoreCase)


        'xmlTemp = Regex.Replace(xmlTemp, "(</book-front>[\r\n]+)(<book-part )", "$1<body>$2", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'xmlTemp = Regex.Replace(xmlTemp, "(</book-part>[\r\n]+)(</book>)", "$1</body>$2", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'xmlTemp = Regex.Replace(xmlTemp, "(<book-part[^>]*>)(<sec sec-type=""index"">)", "<back><index>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'xmlTemp = xmlTemp.Replace("</index-entry></sec></book-part>", "</index-entry></index></back>")
        'xmlTemp = xmlTemp.Replace("</body></book-part><back>", "</body></book-part></body><back>")
        Return xmlTemp
    End Function

    Private Function GetTableColumnCount(ByVal FirstRowNode As XmlNode) As Int16
        Dim iColCount As Integer = 0
        If ((FirstRowNode IsNot Nothing) AndAlso (FirstRowNode.ChildNodes IsNot Nothing) AndAlso (FirstRowNode.ChildNodes.Count > 0)) Then
            For Each RwNode As XmlNode In FirstRowNode.ChildNodes
                If ((RwNode.Attributes("colspan") IsNot Nothing) AndAlso (Convert.ToInt32(RwNode.Attributes("colspan").Value) > 1)) Then
                    iColCount = IIf(iColCount = 0, Convert.ToInt32(RwNode.Attributes("colspan").Value), iColCount + Convert.ToInt32(RwNode.Attributes("colspan").Value))
                Else
                    iColCount = iColCount + 1
                End If
            Next
        End If
        Return iColCount
    End Function

    Private Function GetMaxCountTdInTable(ByVal TableNode As XmlNode, ByVal RowNodeName As String, ByVal ColNodeName As String) As XmlNode
        Dim MaxCnt As Integer = 0
        Dim MdxNode As XmlNode = Nothing
        Try
            Dim trList As XmlNodeList = TableNode.SelectNodes(".//" & RowNodeName)
            For t As Integer = 0 To trList.Count - 1
                Dim Cnt As Integer = trList(t).SelectNodes(".//" & ColNodeName).Count
                If (Cnt > MaxCnt) Then
                    MaxCnt = Cnt
                    MdxNode = trList(t)
                End If
            Next
            Return MdxNode
        Catch ex As Exception
            GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            Return Nothing
        End Try
    End Function

    Private Function EntityCleanup(m As Match) As String
        Dim sResult As String = m.Value
        Dim attributeVal As String = m.Groups(1).Value
        Dim Content As String = m.Groups(2).Value
        If (Not String.IsNullOrEmpty(attributeVal)) Then
            attributeVal = Regex.Replace(attributeVal, "&([a-zA-Z])", "&#38;$1", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            sResult = sResult.Replace(m.Groups(1).Value, attributeVal)
        End If
        Return sResult
    End Function

    Private Function SecidGeneration(m As Match)
        Dim sResult As String = m.Value.ToString
        Dim mtch As MatchCollection = Regex.Matches(sResult, "<sec[^><]+>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iChappp = iChappp + 1
        iSe = 1
        sResult = Regex.Replace(sResult, "(<sec id="")ch\-(\d+)\-sec\-(\d+)([^><]+>)", AddressOf SecidGenerationNew, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Return sResult
    End Function

    Private Function EtalReplace(m As Match)
        Dim sResult As String = m.Value.ToString
        Dim sMt As Match = Regex.Match(sResult, "<etal>(((?!</etal>).)+)</etal>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        If Not sMt.Success Then Return sResult
        sResult = sResult.Replace(sMt.Value.ToString, "")
        Return sResult & sMt.Value.ToString
    End Function

    Private Function SecidGenerationNew(m As Match)
        Dim sResult As String = m.Value.ToString
        sResult = m.Groups(1).Value.ToString & "sec" & iChappp & "_" & iSe & m.Groups(4).Value.ToString
        iSe = iSe + 1
        Return sResult
    End Function

    Private Function GraphicPro(m As Match)
        Dim sResult As String = m.Value.ToString
        'sResult = Regex.Replace(sResult, "\.(tif|jpg|jpeg|gif)", """ mime-subtype=""$1", RegexOptions.IgnoreCase)
        sResult = Regex.Replace(sResult, " xmlns:xlink=""http://www.w3.org/1999/xlink""", " ", RegexOptions.IgnoreCase)
        sResult = Regex.Replace(sResult, "<graphic ", "<graphic xmlns:xlink=""http://www.w3.org/1999/xlink"" ", RegexOptions.IgnoreCase)
        If (Not sResult.Contains("xlink:href")) Then
            sResult = Regex.Replace(sResult, "href=""([^\.>]+)\.", " xlink:href=""$1", RegexOptions.IgnoreCase)
        End If
        Return sResult
    End Function

    Private Function JnlVolume(m As Match)
        If m.Value.ToString.Contains("<volume>") Then Return m.Value.ToString
        Dim sResult As String = m.Value.ToString
        sResult = Regex.Replace(sResult, "publication-type=""journal""", "publication-type=""other""", RegexOptions.IgnoreCase)
        Return sResult
    End Function

    Private Function RemoveItalics(m As Match)
        Dim sResult As String = m.Value.ToString
        sResult = Regex.Replace(sResult, "</italic><italic>", "", RegexOptions.IgnoreCase)
        'sResult = Regex.Replace(sResult, "(<source>)<italic>|</italic>(</source>)", "$1$2", RegexOptions.IgnoreCase)
        'sResult = Regex.Replace(sResult, "\b(et ?al\.)", "<etal>$1</etal>", RegexOptions.IgnoreCase)
        Return sResult
    End Function

    Private Function FigureChangePro(m As Match)
        Dim sResult As String = m.Value.ToString
        Dim mt As Match = Regex.Match(sResult, "<graphic[^><]+>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        If mt.Success Then
            Dim sGraphic As String = mt.Value.ToString
            sResult = Regex.Replace(sResult, sGraphic, "")
            sGraphic = Regex.Replace(sGraphic, "mime-subtype=""tif"" ", "")
            sGraphic = Regex.Replace(sGraphic, "f(\d+)\.(\d+)", "fig$1_$2.tif", RegexOptions.IgnoreCase)
            sResult = Regex.Replace(sResult, "</fig>", sGraphic & "</fig>")
            sResult = Regex.Replace(sResult, "<p>(((?!</p>).)+)</p>", "<caption><title>$1</title></caption>")
        End If
        Return sResult
    End Function

    Private Function ImprintPro(m As Match)
        Dim sResult As String = m.Value.ToString
        If sResult.ToLower.Contains("the right of scott wisor") Then
            sResult = Regex.Replace(sResult, "<imprint-text>", "<imprint-text type=""MoralRightsAssertion"">", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        ElseIf sResult.ToLower.Contains("all rights reserved") Then
            sResult = Regex.Replace(sResult, "<imprint-text>", "<imprint-text type=""ReproductionStatement"">", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        ElseIf sResult.ToLower.Contains("trademark notice") Then
            sResult = Regex.Replace(sResult, "<imprint-text>", "<imprint-text type=""TrademarkNotice"">", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        ElseIf sResult.ToLower.Contains("british library") Then
            sResult = Regex.Replace(sResult, "<imprint-text>", "<imprint-text type=""BritishLibrary"">", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        Return sResult
    End Function

    Private iChapp As Integer = 1
    Private Function ChapNos(m As Match)
        Dim sInp As String = m.Value.ToString
        Dim mt As Match = Regex.Match(sInp, "(id=""chapter)""", RegexOptions.IgnoreCase)
        If mt.Success Then
            sInp = sInp.Replace(mt.Value.ToString, mt.Groups(1).Value.ToString & iChapp & Chr(34))
            iChapp = iChapp + 1
        End If
        Return sInp
    End Function

    Private Function ChapterPro1(m As Match)
        Dim sResult As String = m.Value.ToString
        Dim mt As Match = Regex.Match(sResult, "<label>((?:(?!(</label>|<title>|<p>)).)+)</label>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        If mt.Success Then
            Dim sLbl As String = mt.Groups(1).Value.ToString
            sLbl = Regex.Replace(sLbl, "<[^><]+>|part|chapter", "", RegexOptions.IgnoreCase).Trim
            sResult = Regex.Replace(sResult, "<label>((?:(?!</label>).)+)</label>", "", RegexOptions.IgnoreCase)
            sResult = Regex.Replace(sResult, ">", " id=""" & m.Groups(1).Value.ToString & sLbl & """ book-part-number=""" & sLbl & """>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'sResult = Regex.Replace(sResult, "<book-part ", "<book-part label=""" & sLbl & """", RegexOptions.IgnoreCase)
            'sResult = Regex.Replace(sResult, m.Groups(1).Value.ToString & "(\d+)?"">", m.Groups(1).Value.ToString & sLbl & """>", RegexOptions.IgnoreCase)
        End If
        Return sResult
    End Function

    Private Function SecNos(m As Match)
        iChapp = iChapp + 1
        Return "<sec id=""sec_" & iChapp & """ disp-level="""">"
    End Function

    Private Function BodyTagIntroPart(m As Match)
        Dim sResult As String = m.Value.ToString
        sResult = Regex.Replace(sResult, "(<book-part([^><]+)?>((?:(?!(<p>|<sec>)).)+))", "$1<body>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        If sResult.ToString.Contains("<body>") Then sResult = sResult & "</body>"
        Return sResult
    End Function

    Dim iFtnote As String = String.Empty

    Private Function BodyTagInto(m As Match)
        Dim sResult As String = m.Value.ToString
        If sResult.Contains("book-part-type=""part""") Then Return sResult
        'sResult = Regex.Replace(sResult, "(<book-part([^><]+)?>((?:(?!(<p>|<sec>)).)+))", "$1<body>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sResult = Regex.Replace(sResult, "(<book-part([^><]+)?>((?:(?!(<p[^>-]*>|<sec[^>]*>)).)+))", "$1<body>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

        'muthu
        sResult = Regex.Replace(sResult, "(<book-part([^><]+)?>((?:(?!(<p[^>-]*>|<sec[^>]*>).))+))", "$1<body>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Dim mt As Match = Regex.Match(sResult, "((</p>|</sec>)((?:(?!(<back>|</book-part>|</p>|</sec>)).)+)?(<back>|</book-part>))", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'Dim mt As Match = Regex.Match(sResult, "((</p>|</sec>)?(?:(?:(?!</back>|</book-part>|</p>|</sec>).)+)?(</back>|</book-part>))", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Dim mtResult As String = mt.Value.ToString
        If (String.IsNullOrEmpty(mtResult)) Then
            GBL.DeantaBallon("Could not able to identify the closing for </body>.", MessageType.MSGERROR)
            Return sResult.Replace("<body>", "")
        End If
        If mtResult.StartsWith("</sec>") AndAlso mtResult.Contains("</p>") Then
            mtResult = mtResult.Replace("</p>", "</p></body>")
        ElseIf mtResult.StartsWith("</p>") AndAlso mtResult.Contains("</sec>") Then
            mtResult = mtResult.Replace("</sec>", "</sec></body>")
        ElseIf mtResult.StartsWith("</p>") AndAlso mtResult.Contains("</fn-group>") Then
            mtResult = mtResult.Replace("</fn-group>", "</fn-group></body>")
        ElseIf mtResult.StartsWith("</p>") Then
            mtResult = mtResult.Replace("</p>", "</p></body>")
        ElseIf mtResult.StartsWith("</sec>") Then
            mtResult = mtResult.Replace("</sec>", "</sec></body>")
        ElseIf mtResult.EndsWith("</back>") Then
            mtResult = mtResult.Replace("</back>", "</back></body>")
        Else
            GBL.DeantaBallon("Could not able to identify the closing for </body>.", MessageType.MSGERROR)
            Return sResult.Replace("<body>", "")
        End If
        sResult = Regex.Replace(sResult, "( )+", " ", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Dim mtt As Match = Regex.Match(sResult, "<book-part book-part-type=""chapter"" id=""chapter(\d+)""", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        If mtt.Success Then
            iFtnote = mtt.Groups(1).Value.ToString
            sResult = Regex.Replace(sResult, "(<fn id=""fn)(\d+"">)", AddressOf FootntChapSeq, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        sResult = sResult.Replace(mt.Value.ToString, mtResult)
        Return sResult
    End Function

    Private Function FootntChapSeq(m As Match)
        If String.IsNullOrEmpty(iFtnote) Then Return m.Value.ToString
        Return m.Groups(1).Value.ToString & iFtnote & "_" & m.Groups(2).Value.ToString
    End Function

    Private Function FigurePlacementPro(m As Match)
        Dim sResult As String = m.Value.ToString
        Dim mtch As MatchCollection
        If sResult.ToString.Contains("<figure") Then
            mtch = Regex.Matches(sResult, "<figure([^><]+)?>(?:(?:(?!</figure>).)+)</figure>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        ElseIf sResult.ToString.Contains("<table") Then
            mtch = Regex.Matches(sResult, "<table([^><]+)?>(?:(?:(?!</table>).)+)</table>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Else
            Return sResult
        End If
        Dim FigureList As List(Of String) = mtch.Cast(Of Match)().Select(Function(m1) m1.Value).ToList
        For Each m2 As Match In mtch
            sResult = sResult.Replace(m2.Value.ToString, "")
        Next
        For Each lst In FigureList
            sResult = sResult & lst.ToString
        Next
        Return sResult
    End Function

    'Private Function FigurePlacementPro(m As Match)
    '    Dim sResult As String = m.Value.ToString
    '    If Not Regex.IsMatch(sResult, "<figure([^><]+)?>") Then Return sResult
    '    Dim mt As MatchCollection = Regex.Matches(sResult, "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
    '    Dim sFigs As String = String.Empty
    '    For Each mc As Match In mt
    '        sFigs = sFigs & mc.Value.ToString
    '        sResult = Regex.Replace(sResult, mc.Value.ToString, "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
    '    Next
    '    sResult = sResult & sFigs
    '    Return sResult
    'End Function

    Private Function XSLPro(xmlText As String, XMLPath As String, Optional ByVal isEpub As Boolean = False) As Boolean
        Thread.Sleep(500)
        If Not isEpub Then
            If Not CheckValidXML(Path.Combine(sXMLFilePath, XMLPath), "TandF") Then
                GBL.DeantaBallon("XML validation failed.", MessageType.MSGERROR)
                Return False
            End If
            CallingXSLPro(Path.Combine(sXMLFilePath, XMLPath), "TNF-XML.xsl")
            xmlText = File.ReadAllText(Path.Combine(sXMLFilePath, XMLPath.Replace(".xml", "_xsl.xml")))
            xmlText = Regex.Replace(xmlText, "xmlns:fo=""http://www\.w3\.org/1999/XSL/Format"" xmlns:xlink=""http://www\.w3\.org/1999/xlink"" xmlns:msxsl=""urn:schemas-microsoft-com:xslt"" xmlns:d=""http://docbook\.org/ns/docbook"" xmlns:aid=""http://ns\.adobe\.com/AdobeInDesign/4\.0/"" xmlns:aid5=""http://ns\.adobe\.com/AdobeInDesign/5\.0/"" xmlns:code=""urn:schemas-test-code""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            xmlText = UpdatePro(xmlText, isEpub)
            xmlText = Regex.Replace(xmlText, "<biblioset([^><]+)?>((?:(?!</biblioset>).)+)</biblioset>", AddressOf BibliosetPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            File.WriteAllText(Path.Combine(sXMLFilePath, XMLPath.Replace(".xml", "_xsl.xml")), xmlText)
        Else
            If Not CheckValidXML(Path.Combine(sXMLFilePath, XMLPath), "EPUB") Then Return False
            CallingXSLPro(Path.Combine(sXMLFilePath, XMLPath), "epub.xsl")
            Return False
        End If
        Return True
    End Function

    Private Function BibliosetPro(m As Match)
        Dim sResult As String = m.Value.ToString
        sResult = Regex.Replace(sResult, "</biblioset>", "<biblioid class=""isbn"" role=""epub"">ISBN:1234567891011</biblioid></biblioset>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Return sResult
    End Function

    Private Function BiblioIdPro(m As Match)
        Dim sResult As String = m.Value.ToString
        Dim sTxt As Match = Regex.Match(m.Groups(2).Value.ToString, "\d+[0-9\./\-]+", RegexOptions.IgnoreCase)
        sResult = sResult.Replace(m.Groups(2).Value.ToString, sTxt.Value.ToString.Trim)
        Return sResult
    End Function

    Private Sub TandFXMLPro()

        Dim sXMLFileName As String = Me.sXMLFileName
        File.Copy(Path.Combine(sXMLFilePath, sXMLFileName), Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")))
        Thread.Sleep(500)
        CallingXSLPro(Path.Combine(sXMLFilePath, sXMLFileName), "epub.xsl")
        Dim sXMLTxt As String = File.ReadAllText(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")))


        'sXMLTxt = Regex.Replace(sXMLTxt, "</legalnotice>((?:(?!<legalnotice([^><]+)?>).)+)(</colophon([^><]+)?>)", AddressOf HardCorePro, RegexOptions.IgnoreCase Or RegexOptions.Singleline Or RegexOptions.RightToLeft)
        Dim smtchcol As MatchCollection = Regex.Matches(sXMLTxt, "(<chapter(?:(?!(-title|>)).)+>)((?:(?!</chapter>).)+)</chapter>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        For Each mc As Match In smtchcol
            sXMLTxt = sXMLTxt.Replace(smtchcol.Item(1).Value.ToString, "</part>" & smtchcol.Item(1).Value.ToString)
            Exit For
        Next
        sXMLTxt = UpdatePro(sXMLTxt, False)
        iChap = 50
        sXMLTxt = Regex.Replace(sXMLTxt, "(  )+", " ")
        ''sXMLTxt = Regex.Replace(sXMLTxt, "<book [^><]+>", sBookInfo.ToString, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sXMLTxt = Regex.Replace(sXMLTxt, "([^""])((http:| www\.| mailto:)([^ ><]+))", "$1<link xlink:href=""$2""><uri>$2</uri></link>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sXMLTxt = Regex.Replace(sXMLTxt, "<emphasis>(&#x201(9|8);)</emphasis>", "$1", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sXMLTxt = Regex.Replace(sXMLTxt, "([^ ])xml:id=", "$1 xml:id=", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        File.WriteAllText(Path.Combine(sXMLFilePath, sXMLFileName.Replace(".xml", "_xsl.xml")), sXMLTxt)
    End Sub

    Private Function ChapInfoPro(m As Match)
        Dim sResult As String = m.Value.ToString
        iChap = +1
        sResult = Regex.Replace(sResult, "<info>", "<info xml:id=""ch" & iChap & "-ba-00000" & iChap & """>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Return sResult
    End Function

    Private Function HardCorePro(m As Match)
        Dim sTxt As String = "</legalnotice>|<biblioid class=~doi~>10.5040/" & sISBN & "</biblioid>|<biblioid class=~other~ otherclass=~schemaVersion~>1</biblioid>|" &
            "<biblioid class=~other~ otherclass=~schematronVersion~>4</biblioid>|<abstract role=~blurb~ xml:id=~ba-blurb1~>|<para></para>|</abstract>|"
        sTxt = sTxt & m.Groups(1).Value.ToString & Environment.NewLine & m.Groups(3).Value.ToString
        sTxt = sTxt & "<part xml:id=~ba-FM-front~ role=~front~>|<info xml:id=~in-0002~>|<title xml:id=~tt-0002~>Front matter</title>|</info>|" &
            "<preface role=~prelims~ xml:id=~b-" & sISBN & "-title~>|<info xml:id=~ba-FM-" & sISBN & "-prelim-id~>|<title xml:id=~ba-FM-" & sISBN & "-prelim-id~>Title Pages</title>|" &
            "<pagenums/>|<mediaobject xml:id=~ba-FM-" & sISBN & "-prelim-id~>|<imageobject xml:id=~ba-FM-" & sISBN & "-prelim-id~>|<imagedata fileref=~pdfs/" & sISBN & ".0001.pdf~ format=~application/pdf~/>|" &
            "</imageobject>|</mediaobject>|</info>|<remark condition=~hidden~>Note that this is a placeholder for the pdf of the prelims and no full text content is included at this point</remark>|" &
            "</preface>|<dedication xml:id=~b-" & sISBN & "-dedi~>|<info xml:id=~bo-id~>|<title outputformat=~e-Only~ xml:id=~tt-003~>Dedication</title>|<pagenums/>|" &
            "<mediaobject xml:id=~ba-000000d4~>|<imageobject xml:id=~ba-000df0005~>|<imagedata fileref=~pdfs/" & sISBN & ".0002.pdf~ format=~application/pdf~/>|" &
            "</imageobject>|</mediaobject>|</info>|<para></para>|</dedication>|<toc xml:id=~b-" & sISBN & "-toc~>|<info xml:id=~in-0006~>|<title xml:id=~tt-00zsdf06~>" &
            "<?page value=~vii~?>Contents</title>|<pagenums>vii</pagenums>|<mediaobject xml:id=~ba-FM-toc-001c~>|<imageobject xml:id=~ba-FM-toc-001d~>|" &
            "<imagedata fileref=~pdfs/9781844864041.0003.pdf~ format=~application/pdf~/>|</imageobject>|</mediaobject>|</info>|</toc>"
        sTxt = sTxt.Replace("|", Environment.NewLine).Replace("~", Chr(34))
        Return sTxt
    End Function

    Private iVal As Integer = 0
    ' Updated on Sep 27, 2016 based on Jaffar request
    Private Function UpdatePro(ByVal sChapterTxt As String, ByVal bxslExecution As Boolean) As String
        '
        sChapterTxt = Regex.Replace(sChapterTxt, "(<acknowledgements([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sChapterTxt = Regex.Replace(sChapterTxt, "(<toc([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<abstract([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<indexterm([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<address([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<bibliodiv([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<bibliolist([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        'If bXMLorEpub Then sChapterTxt = Regex.Replace(sChapterTxt, "(<bibliomixed([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<bibliography([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<para([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<legalnotice([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<part([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<partintro([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        If (bxslExecution) Then
            iVal = 0
            sChapterTxt = Regex.Replace(sChapterTxt, "(<preface([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<poetry([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<blockquote([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        'sChapterTxt = Regex.Replace(sChapterTxt, "(<caption([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<line([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<linegroup([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<subtitle([^><]*)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<personblurb([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<printhistory([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<colophon([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<inlinemediaobject([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<imageobject([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<mediaobject([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<informaltable([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'iVal = 0
        'sChapterTxt = Regex.Replace(sChapterTxt, "(<entry([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<informalfigure([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<epigraph([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<sidebar([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<keywordset([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<keyword([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<itermset([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<tfoot([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<glossary([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<glosslist([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<glossentry([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<glossterm([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal = 0
        sChapterTxt = Regex.Replace(sChapterTxt, "(<glossdef([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'iVal = 0
        'sChapterTxt = Regex.Replace(sChapterTxt, "(<index([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'iVal = 0
        'sChapterTxt = Regex.Replace(sChapterTxt, "(<table([^><]+)?)>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        If (bxslExecution) Then
            iVal = 0
            sChapterTxt = Regex.Replace(sChapterTxt, "(<chapter((?:(?!(-title|>)).)+)?)>", AddressOf ChapterProEpub, RegexOptions.IgnoreCase Or RegexOptions.Singleline) ' muthu)
        Else
            iVal = 0
            sChapterTxt = Regex.Replace(sChapterTxt, "(<chapter((?:(?!(-title|>)).)+)?)>", AddressOf ChapterPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline) ' muthu)
        End If

        'sChapterTxt = Regex.Replace(sChapterTxt, "((<chapter((?:(?!(-title|>)).)+)?)>)((<label([^><]+)?>((?:(?!</label>).)+)</label>)?<title([^><]+)?>((?:(?!</title>).)+)</title>)", AddressOf ChapterPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sChapterTxt = Regex.Replace(sChapterTxt, "(<part([^><]+)?)>", AddressOf PartPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        If Not bExecuteOnce Then
            iVal = 0
            'sChapterTxt = Regex.Replace(sChapterTxt, "<preface([^><]+)?>((?:(?!</preface>).)+)</preface>", AddressOf PrefacePro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Else
            iVal = 0
            'sChapterTxt = Regex.Replace(sChapterTxt, "(<title((?:(?!-group>).)+))>", AddressOf IDGen, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        bExecuteOnce = True
        Return sChapterTxt
    End Function

    Private Function ChapterProHC(m As Match)
        Dim IsBiblio As Boolean = False
        Dim IsIndex As Boolean = False
        Dim sAuthors As Match = Regex.Match(m.Value.ToString, "<author>(.+)</author>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'If Regex.IsMatch(m.Value.ToString, "<author>(.+)</author>", RegexOptions.IgnoreCase Or RegexOptions.Singleline) Then
        'End If

        If (m.Value.ToString.Contains("<glossary>")) Then
            Return m.Value.ToString()
        End If

        Dim sResult As String = m.Groups(1).Value.ToString & Environment.NewLine & "<info xml:id=""ch" & iChap & "-ba-00000" & iChap & """>" & Environment.NewLine & m.Groups(3).Value.ToString

        Dim sHardcode As String = "|<pagenums></pagenums>|<biblioid class=~doi~>10.5040/" & sISBN & ".000" & iChap & "</biblioid>|<mediaobject xml:id=~ch" & iChap & "-ba-000000" & iChap & "~>|" &
            "<imageobject xml:id=~ch" & iChap & "-ba-0000005~>|<imagedata fileref=~pdfs/" & sISBN & ".0006.pdf~ format=~application/pdf~></imagedata>|</imageobject>|" &
            "</mediaobject>|"
        If sAuthors.Success Then
            'sResult = Regex.Replace(sResult, sAuthors.Value.ToString, "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            sResult = sResult.Replace(sAuthors.Value.ToString.Trim(), "")
            sHardcode = Regex.Replace(sHardcode, "<pagenums>", "<authorgroup>|" & sAuthors.Value.ToString & "</authorgroup>|<pagenums>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        sHardcode = sHardcode.Replace("~", Chr(34)).Replace("|", Environment.NewLine)
        sResult = sResult & sHardcode
        If Regex.IsMatch(sResult, "<title([^><]+)?>((?:(?!</title>).)+)</title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline) Then
            Dim smtch As Match = Regex.Match(sResult, "<title([^><]+)?>((?:(?!</title>).)+)</title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            If smtch.Value.ToString.ToLower.Contains("introduction") Then
                sResult = Regex.Replace(sResult, "-chapter\d+", "-Introduction", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            End If
        End If
        'If iChap = 1 Then sResult = sResult.Replace("<chapter", "</part><chapter")
        sResult = Regex.Replace(sResult, "<info([^><]+)?></info>", "", RegexOptions.IgnoreCase)
        If Not sResult.Contains("</info") Then sResult = sResult & "</info>" & Environment.NewLine

        If (Regex.IsMatch(sResult, "(<section>)(<title([^><]+)?>((?:(?!</title>).)+)</title>)", RegexOptions.IgnoreCase Or RegexOptions.Singleline)) Then
            sResult = Regex.Replace(sResult, "(<section>)(<title([^><]+)?>((?:(?!</title>).)+)</title>)", "$2", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            sResult = sResult & "<section>"
        End If

        If (Regex.Match(sResult, "(<bibliography>)(<title([^><]+)?>((?:(?!</title>).)+)</title>)", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Success) Then
            IsBiblio = True
        End If
        If (Regex.Match(sResult, "(<index>)(<title([^><]+)?>((?:(?!</title>).)+)</title>)", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Success) Then
            IsIndex = True
        End If
        sResult = Regex.Replace(sResult, "(<bibliography>)(<title([^><]+)?>((?:(?!</title>).)+)</title>)", "$2", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sResult = Regex.Replace(sResult, "(<index>)(<title([^><]+)?>((?:(?!</title>).)+)</title>)", "$2", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        If (IsBiblio) Then
            sResult = sResult & "<bibliography>"
        End If
        If (IsIndex) Then
            sResult = sResult & "<index>"
        End If
        Return sResult
        'Return sResult
    End Function

    Private Function PrefacePro(m As Match)
        Dim sInput As String = m.Value.ToString
        Dim sResults As String = String.Empty
        Dim sTxt As String = "<info xml:id=~ba-0000004e~>|<title xml:id=~b-0003g~></title>|<pagenums></pagenums>|<mediaobject xml:id=~ba-0000004f~>|" &
            "<imageobject xml:id=~ba-0000005f~>|<imagedata fileref=~pdfs/" & sISBN & ".0004.pdf~ format=~application/pdf~/>|</imageobject>|</mediaobject>|</info>"
        If Not String.IsNullOrEmpty(sInput) Then
            If Regex.IsMatch(sInput, "<title([^><]+)?>((?:(?!</title>).)+)</title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline) Then
                Dim smt As Match = Regex.Match(sInput, "<title([^><]+)?>((?:(?!</title>).)+)</title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                If smt.Groups(2).Value.ToString.ToLower.Contains("contributors") Then
                    sResults = "<preface xml:id=""b-" & sISBN & "-contributors"">"
                Else
                    Dim sTit As String = smt.Groups(2).Value.ToString
                    sTit = Regex.Replace(sTit, "<emphasis[^>]*>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Replace("</emphasis>", "")
                    sTit = Regex.Replace(sTit, "(<superscript>(.+)</superscript>|<(/)?emphasis>)", "", RegexOptions.IgnoreCase)
                    sResults = "<preface xml:id=""b-" & sISBN & "-" & sTit & Chr(34) & ">"
                End If
                Dim sMtch As Match = Regex.Match(sInput, "<title([^><]+)?>((?:(?!</title>).)+)</title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                If sMtch.Success Then
                    Dim sTit As String = sMtch.Groups(2).Value.ToString
                    sTit = Regex.Replace(sTit, "(<superscript>(.+)</superscript>|<(/)?superscript>)", "", RegexOptions.IgnoreCase)
                    sTxt = sTxt.Replace("</title>", sTit & "</title>")
                End If
                sResults = sResults & sTxt.Replace("|", Environment.NewLine).Replace("~", Chr(34))
                'sResults = Regex.Replace(sResults, " xml:id=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

                sInput = Regex.Replace(sInput, "<preface([^><]+)?>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                'sResults = sResults & " xml:id=" & Chr(34) & "b-" & sISBN & "-" & sTag & Chr(34) & ">"
                'sInput = Regex.Replace(sInput, smt.Value.ToString.Trim(), sResults.ToString, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
                sInput = sInput.Replace(smt.Value.ToString.Trim(), sResults.ToString)
            End If
        End If
        Return sInput
    End Function

    Private Function PartPro(m As Match)
        Dim sInput As String = m.Groups(1).Value.ToString
        If Not String.IsNullOrEmpty(sInput) Then
            sInput = Regex.Replace(sInput, " xml:id=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            sInput = Regex.Replace(sInput, " label=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        iVal = iVal + 1
        'Return "<part xml:id=""part"">"
        Return "<part xml:id=""b-" & sISBN & "-part" & iVal & """>" ''20-03-2020
    End Function

    Private Function ChapterProEpub(m As Match)
        If m.Value.ToString.Contains("/>") OrElse m.Value.ToString.Contains("chapter-title") Then Return m.Value.ToString
        Dim TmpStr As String = m.Value
        Dim sInput As String = m.Groups(1).Value.ToString
        Dim Title As String = m.Groups(5).Value.ToString
        If Not String.IsNullOrEmpty(sInput) Then
            sInput = Regex.Replace(sInput, " xml:id=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            sInput = Regex.Replace(sInput, " label=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        TmpStr = TmpStr.Replace(sInput, "")
        sInput = sInput.TrimEnd(">")
        iVal = iVal + 1
        Return sInput & " xml:id=""b-" & sISBN & "-chapter" & iVal & """" & TmpStr.Replace(" > ", "")
    End Function

    Private Function ChapterPro(m As Match)
        If m.Value.ToString.Contains("/>") OrElse m.Value.ToString.Contains("chapter-title") Then Return m.Value.ToString
        Dim TmpStr As String = m.Value
        Dim sInput As String = m.Groups(1).Value.ToString
        Dim Title As String = m.Groups(5).Value.ToString
        If Not String.IsNullOrEmpty(sInput) Then
            sInput = Regex.Replace(sInput, " xml:id=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            sInput = Regex.Replace(sInput, " label=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        TmpStr = TmpStr.Replace(sInput, "")
        sInput = sInput.TrimEnd(">")
        If Title.ToString.ToLower.Contains("introduction") Then

            Return sInput & " xml:id=""b-" & sISBN & "-chapter0"">" & TmpStr
        Else
            iVal = iVal + 1
            Return sInput & " xml:id=""chapter" & iVal & """>" & TmpStr.Replace(">", "")
        End If
        'Return sInput & " xml:id=""b-" & sISBN & "-chapter" & iVal & """>"
    End Function

    Private Function old_ChapterPro(m As Match)
        If m.Value.ToString.Contains("/>") OrElse m.Value.ToString.Contains("chapter-title") Then Return m.Value.ToString
        Dim sInput As String = m.Groups(1).Value.ToString
        If Not String.IsNullOrEmpty(sInput) Then
            sInput = Regex.Replace(sInput, " xml:id=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            sInput = Regex.Replace(sInput, " label=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        iVal = iVal + 1
        'If Regex.IsMatch(m.Value, "<title([^><]+)?>((?:(?!</title>).)+)</title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline) Then
        '    Dim smtch As Match = Regex.Match(m.Value, "<title([^><]+)?>((?:(?!</title>).)+)</title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        '    If smtch.Value.ToString.ToLower.Contains("introduction") Then
        '        Return sInput & " xml:id=""b-" & sISBN & "-chapter0"">"
        '    Else
        '        Return sInput & " xml:id=""b-" & sISBN & "-chapter" & iVal & """>"
        '    End If
        'End If
        Return sInput & " xml:id=""b-" & sISBN & "-chapter" & iVal & """>"
    End Function

    Private iSec As Integer = 1
    Private iFootnote As Integer = 1

    Private Function FootnotePro(m As Match)
        If Not m.Value.ToString.EndsWith(">") Then Return m.Value.ToString
        Dim sInput As String = m.Groups(1).Value.ToString
        Dim smt As Match = Regex.Match(sInput, " ?label=""([^""]+)""", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sInput = Regex.Replace(sInput, " ?label=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sInput = Regex.Replace(sInput, " ?role=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sInput = Regex.Replace(sInput, "xml:id=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        If String.IsNullOrEmpty(smt.Groups(1).Value.ToString) Then
            sInput = sInput & " role=" & Chr(34) & "end-bk-note" & Chr(34) & " label=" & Chr(34) & iFootnote & Chr(34) & " xml:id=""note" & iFootnote & "-ba-" & String.Format("{0:00000}", iFootnote) & """>"
        Else
            sInput = sInput & " role=" & Chr(34) & "end-bk-note" & Chr(34) & " label=" & Chr(34) & "ch" & iChapteridSeq & "-" & smt.Groups(1).Value.ToString & Chr(34) & " xml:id=""note" & iFootnote & "-ba-" & String.Format("{0:00000}", iFootnote) & """>"
            sInput = sInput & "<label>" & smt.Groups(1).Value.ToString & "</label>"
        End If
        iFootnote += 1
        Return sInput
    End Function

    Private Function SectionPro(m As Match)
        Dim sInput As String = String.Empty
        If Not m.Value.ToString.EndsWith(">") Then Return m.Value.ToString
        Dim Title As String = m.Groups(1).Value.ToString
        If (Not m.Value.Contains("title")) Then
            sInput = m.Groups(0).Value.ToString
        Else
            sInput = m.Groups(5).Value.ToString
        End If
        sInput = Regex.Replace(sInput, " xml:id=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sInput = sInput.Trim().TrimEnd(">")
        'iVal = iVal + 1
        If (Not String.IsNullOrEmpty(Title) AndAlso (Title.ToLower().Contains("introduction"))) Then
            'sInput = sInput & " xml:id=" & Chr(34) & "ch-0-sec-" & iSec & Chr(34) & ">" 'k33388
            'iChapteridSeq = iChapteridSeq - 1
            sInput = sInput & " xml:id=" & Chr(34) & "ch-" & iChapteridSeq & "-sec-" & iSec & Chr(34) & ">"
            'iChapteridSeq = iChapteridSeq - 1
        Else
            sInput = sInput & " xml:id=" & Chr(34) & "ch-" & iChapteridSeq & "-sec-" & iSec & Chr(34) & ">"
        End If
        iSec = iSec + 1
        Return Title & sInput
    End Function

    Private Function IDGen(m As Match)
        If Not m.Value.ToString.EndsWith(">") OrElse m.Value.ToString.Contains("/>") Then Return m.Value.ToString
        Dim sResults As String = String.Empty
        Dim sInput As String = m.Groups(1).Value.ToString
        sInput = Regex.Replace(sInput, " xml:id=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        iVal += 1
        iVal = String.Format("{0:0000000}", iVal)
        Dim sDigit As String = String.Empty
        If Convert.ToString(iVal).Length = 1 Then
            sDigit = "000000"
        ElseIf Convert.ToString(iVal).Length = 2 Then
            sDigit = "00000"
        ElseIf Convert.ToString(iVal).Length = 3 Then
            sDigit = "0000"
        ElseIf Convert.ToString(iVal).Length = 4 Then
            sDigit = "000"
        ElseIf Convert.ToString(iVal).Length = 5 Then
            sDigit = "00"
        ElseIf Convert.ToString(iVal).Length = 6 Then
            sDigit = "0"
        End If
        If m.Groups(1).Value.ToString.Contains("<para") Then
            sResults = sInput & " xml:id=" & Chr(34) & "pa-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<index") Then
            sResults = sInput & " xml:id=" & Chr(34) & "b-" & sISBN & "-index" & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<bibliography") Then
            sResults = sInput & " xml:id=" & Chr(34) & "b-" & sISBN & "-bib" & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<glossary") Then
            sResults = sInput & " xml:id=" & Chr(34) & "b-" & sISBN & "-glossary" & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<glosslist") Then
            sResults = sInput & " xml:id=" & Chr(34) & "glossl-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<glossentry") Then
            sResults = sInput & " xml:id=" & Chr(34) & "glosse-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<glossterm") Then
            sResults = sInput & " xml:id=" & Chr(34) & "glosst-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<glossdef") Then
            sResults = sInput & " xml:id=" & Chr(34) & "glossd-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<bibliolist") Then
            sResults = sInput & " xml:id=" & Chr(34) & "bibl-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<partintro") Then
            sResults = sInput & " xml:id=" & Chr(34) & "ptint-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<keyword") Then
            sResults = sInput & " xml:id=" & Chr(34) & "key-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<keywordset") Then
            sResults = sInput & " xml:id=" & Chr(34) & "key-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<itemset") Then
            sResults = sInput & " xml:id=" & Chr(34) & "itms-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<tfoot") Then
            sResults = sInput & " xml:id=" & Chr(34) & "tfoot-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<sidebar") Then
            sResults = sInput & " xml:id=" & Chr(34) & "side-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<line") Then
            sResults = sInput & " xml:id=" & Chr(34) & "line-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<linegroup") Then
            sResults = sInput & " xml:id=" & Chr(34) & "lineg-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<epigraph") Then
            sResults = sInput & " xml:id=" & Chr(34) & "epig-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<inlinemediaobject") Then
            sResults = sInput & " xml:id=" & Chr(34) & "inlinemedo-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<imageobject") Then
            sResults = sInput & " xml:id=" & Chr(34) & "imgo-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<mediaobject") Then
            sResults = sInput & " xml:id=" & Chr(34) & "medo-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<informaltable") Then
            sResults = sInput & " xml:id=" & Chr(34) & "infotab-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<poetry") Then
            sResults = sInput & " xml:id=" & Chr(34) & "poet-" & sDigit & iVal & Chr(34) & ">"
            'ElseIf m.Groups(1).Value.ToString.Contains("<entry") Then
            '    sResults = sInput & " xml:id=" & Chr(34) & "entr-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<informalfigure") Then
            sResults = sInput & " xml:id=" & Chr(34) & "infofig-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<colophon") Then
            sResults = sInput & " xml:id=" & Chr(34) & "colph-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<bibliodiv") Then
            sResults = sInput & " xml:id=" & Chr(34) & "bibd-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<address") Then
            sResults = sInput & " xml:id=" & Chr(34) & "adr-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<biblioset") Then
            sInput = Regex.Replace(sInput, " role=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            sResults = sInput & " role=""publisher"" xml:id=" & Chr(34) & "bibs-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<bibliomixed") Then
            sInput = Regex.Replace(sInput, " role=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            sResults = sInput & " role=""series"" xml:id=" & Chr(34) & "bibm-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<abstract") Then
            'sInput = Regex.Replace(sInput, " role=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            'sResults = sInput & " role=""blurb"" xml:id=" & Chr(34) & "abs-" & sDigit & iVal & Chr(34) & ">"
            sInput = Regex.Replace(sInput, " role=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            sResults = sInput & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<legalnotice") Then
            sResults = sInput & " xml:id=" & Chr(34) & "ba-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<preface") Then
            sResults = sInput & " xml:id=" & Chr(34) & "b-" & sISBN & "-preface" & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<acknowledgements") Then
            sResults = sInput & " xml:id=" & Chr(34) & "b-" & sISBN & "-ack" & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<toc") Then
            sResults = sInput & " xml:id=" & Chr(34) & "b-" & sISBN & "-toc" & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<part") Then
            sResults = sInput & " xml:id=" & Chr(34) & "b-" & sISBN & "-part" & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<preface") Then
            sResults = sInput & " xml:id=" & Chr(34) & "b-" & sISBN & "-preface" & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<blockquote") Then
            sResults = sInput & " xml:id=" & Chr(34) & "bloq-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<caption") Then
            'sResults = sInput & " xml:id=" & Chr(34) & "capt-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<title") Then
            If Not m.Groups(1).Value.ToString.Contains("<title-group") Then sResults = sInput & " xml:id=" & Chr(34) & "ti-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<printhistory") Then
            sResults = sInput & " xml:id=" & Chr(34) & "prih-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<subtitle") Then
            sResults = sInput & " xml:id=" & Chr(34) & "suti-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<table") Then
            sResults = "<table label=""" & iVal & """ frame=""all""" & "xml:id=" & Chr(34) & "tab-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<personblurb") Then
            sResults = sInput & " xml:id=" & Chr(34) & "pbl-" & sDigit & iVal & Chr(34) & ">"
        ElseIf m.Groups(1).Value.ToString.Contains("<cover") Then
            sInput = Regex.Replace(sInput, " role=""([^""])+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            sResults = sInput & " role=""series"" xml:id=" & Chr(34) & "co-" & sDigit & iVal & Chr(34) & ">"
        End If

        Return sResults
    End Function

    Private Function CleanupPro(sXMLContent As String) As String
        Dim sTxt2Remove As String = "<book-meta xmlns:fo=~http://www.w3.org/1999/XSL/Format~ xmlns:xlink=~http://www.w3.org/1999/xlink~ xmlns:msxsl=~urn:schemas-microsoft-com:xslt~ " &
                "xmlns:d=~http://docbook.org/ns/docbook~ xmlns:aid=~http://ns.adobe.com/AdobeInDesign/4.0/~ xmlns:aid5=~http://ns.adobe.com/AdobeInDesign/5.0/~ xmlns:code=~urn:schemas-test-code~>"
        sXMLContent = Regex.Replace(sXMLContent, sTxt2Remove.ToString.Replace("~", Chr(34).ToString), "<book-meta>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sTxt2Remove = "<!DOCTYPE book SYSTEM ""\\fsdeanta\TechRelease\Accounts\Common\DeantaComposer\Publish\extra\DTD\TFB-DTD\TFB\TFB.dtd"">"
        If Regex.IsMatch(sXMLContent, "<!DOCTYPE book SYSTEM[^><]+><book>", RegexOptions.IgnoreCase Or RegexOptions.Singleline) Then
            sXMLContent = Regex.Replace(sXMLContent, "<!DOCTYPE book SYSTEM[^><]+><book>", sTxt2Remove & "<book>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Else
            sXMLContent = Regex.Replace(sXMLContent, "<book>", sTxt2Remove & "<book>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        sXMLContent = Regex.Replace(sXMLContent, "<(ext-link|graphic)( [^><]+>)", AddressOf CiteAttribute, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sXMLContent = Regex.Replace(sXMLContent, "(<xref rid=""F[0-9]+"" ref-type="")figure("">)", "$1fig$2", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

        sXMLContent = Regex.Replace(sXMLContent, "<fig(?:ure)?([^><]+)?>((?:(?!</fig(ure)?>).)+)</fig(ure)?>", AddressOf FigurePro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sXMLContent = Regex.Replace(sXMLContent, "</book-meta>((?:(?!</body>).)+)</body>", "</book-meta><book-front>$1</book-front></body>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sXMLContent = Regex.Replace(sXMLContent, "<fpage>((?:(?!</fpage>).)+)</fpage>", AddressOf PageRangePro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sXMLContent = sXMLContent.Replace(" xmlns:fo=""http://www.w3.org/1999/XSL/Format""", "")
        sXMLContent = sXMLContent.Replace(" xmlns:xlink=""http://www.w3.org/1999/xlink""", "")
        sXMLContent = sXMLContent.Replace(" xmlns:msxsl=""urn:schemas-microsoft-com:xslt""", "")
        sXMLContent = sXMLContent.Replace(" xmlns:d=""http://docbook.org/ns/docbook""", "")
        sXMLContent = sXMLContent.Replace(" xmlns:aid=""http://ns.adobe.com/AdobeInDesign/4.0/""", "")
        sXMLContent = sXMLContent.Replace(" xmlns:aid5=""http://ns.adobe.com/AdobeInDesign/5.0/""", "")
        sXMLContent = sXMLContent.Replace(" xmlns:code=""urn:schemas-test-code""", "")
        Return sXMLContent
    End Function

    Private Function PageRangePro(m As Match)
        Dim sPage As String = m.Value
        Dim smtch As Match = Regex.Match(sPage, "(\d+)( ?" & Chr(45).ToString & "|" & ChrW(8212).ToString & "|" & ChrW(8211).ToString & " ?)(\d+)", RegexOptions.IgnoreCase)
        If smtch.Success Then
            sPage = "<fpage>" & smtch.Groups(1).Value.ToString & "</fpage>" & smtch.Groups(2).Value.ToString & "<lpage>" & smtch.Groups(2).Value.ToString & "</fpage>"
        End If
        Return sPage
    End Function

    Private Function FigurePro(m As Match)
        Dim sGraphic As String = m.Value.ToString
        Dim sMtch As Match = Regex.Match(sGraphic, "<graphic([^><]+)?>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        If sMtch.Success Then
            sGraphic = Regex.Replace(sGraphic, sMtch.Value, "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            sGraphic = Regex.Replace(sGraphic, "</fig>", sMtch.Value & "</fig>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        Return sGraphic
    End Function

    Private Function CiteAttribute(m As Match) As String
        If Not m.Value.ToString.Contains("http://www.w3.org/1999/xlink") Then
            Return "<" & m.Groups(1).Value.ToString & " xmlns:xlink=""http://www.w3.org/1999/xlink""" & m.Groups(2).Value.ToString
        Else
            Return m.Value
        End If

    End Function

    Private Function CheckValidXML(sFilePath As String, Optional sEpub As String = "") As Boolean
        Try
            Dim m_xmld As New XmlDocument
            m_xmld.Load(sFilePath)
            Return True
        Catch ex As Exception
            GBL.DeantaBallon("The XML file is not well formed and hence " & sEpub & " xsl has not been executed. Please check.", MessageType.MSGERROR)
            Return False
        End Try
    End Function

    ' Execute XSL file code from Muthu
    Private Sub CallingXSLPro(sXMLFile As String, ByVal xslName As String)
        Dim OutputPath As String = String.Empty
        Dim BatFileContent As String = String.Empty
        OutputPath = Path.Combine(Path.GetDirectoryName(sXMLFile), "TnfConversion")
        If (Directory.Exists(OutputPath)) Then
            Array.ForEach(Directory.GetFiles(OutputPath), Sub(sfile As String)
                                                              Try
                                                                  File.Delete(sfile)
                                                              Catch ex As Exception
                                                              End Try
                                                          End Sub)
        Else
            Directory.CreateDirectory(OutputPath)
        End If
        If File.Exists(Path.Combine(AppPath, "saxon9.jar")) Then File.Copy(Path.Combine(AppPath, "saxon9.jar"), OutputPath & "\saxon9.jar")
        If File.Exists(Path.Combine(AppPath, xslName)) Then File.Copy(Path.Combine(AppPath, xslName), OutputPath & "\" & xslName)
        'If File.Exists(Path.Combine(AppPath, "TNF-XML.xsl")) Then File.Copy(Path.Combine(AppPath, "TNF-XML.xsl"), OutputPath & "\TNF-XML.xsl") 
        If File.Exists(sXMLFile) Then File.Copy(sXMLFile, Path.Combine(OutputPath, Path.GetFileName(sXMLFile)))
        If File.Exists(Path.Combine(AppPath, "ent.xsl")) Then File.Copy(Path.Combine(AppPath, "ent.xsl"), OutputPath & "\ent.xsl")

        BatFileContent = "java -jar """ & Path.GetFileName(Path.Combine(AppPath, "saxon9.jar")) & """ -s:""" & Path.GetFileName(sXMLFile) & """ -xsl:""" &
                                                           Path.GetFileName(Path.Combine(AppPath, xslName)) & """ -o:""" & Path.GetFileNameWithoutExtension(sXMLFile) & "_xsl.xml" & """"
        'File.Copy(Path.Combine(sXMLFilePath, sXMLFile), Path.Combine(sXMLFilePath, sXMLFile.Replace(".xml", "_xsl.xml")))
        If (Not CreateBatAndRunFile(BatFileContent, OutputPath)) Then
            'GBL.DeantaBallon("Error occur while create bat file.", MessageType.MSGERROR)
            GBL.DeantaBallon("Error occur while create bat file." & "XML Merging", MessageType.MSGERROR)
        End If
        If File.Exists(OutputPath & "\" & Path.GetFileNameWithoutExtension(sXMLFile) & "_xsl.xml") Then
            File.Copy(OutputPath & "\" & Path.GetFileNameWithoutExtension(sXMLFile) & "_xsl.xml", Path.Combine(Path.GetDirectoryName(sXMLFile), Path.GetFileNameWithoutExtension(sXMLFile) & "_xsl.xml"), True)
            If Directory.Exists(Path.Combine(OutputPath, sISBN)) Then
                My.Computer.FileSystem.CopyDirectory(Path.Combine(OutputPath, sISBN), Path.Combine(sXMLFilePath, sISBN), True)
            ElseIf Directory.Exists(Path.Combine(OutputPath, "1234567890000")) Then
                My.Computer.FileSystem.CopyDirectory(Path.Combine(OutputPath, "1234567890000"), Path.Combine(sXMLFilePath, sISBN), True)
            End If
        End If
        'Dim TmpDir As New DirectoryInfo(OutputPath)
        'TmpDir.Delete(True)
    End Sub

    ' Epub Cleaning
    Private Function EpubCleanup(sContent As String)
        'sContent = Regex.Replace(sContent, "</?info([^><]+)?>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sContent = Regex.Replace(sContent, "<footnote([^><]+)?>(((?!</footnote>).)+)</footnote>", AddressOf FootnoteInfo, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sContent = Regex.Replace(sContent, "<xref [^><]+><sup>(((?!</sup>).)+)</sup></xref>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sContent = Regex.Replace(sContent, "(<chapter[^><]+>)((?:(?!<para([^><]+)?>).)+)(<para([^><]+)?>)", AddressOf ChapterInfo, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sContent = Regex.Replace(sContent, "(<section[^><]+>)((?:(?!<para([^><]+)?>).)+)(<para([^><]+)?>)", AddressOf ChapterInfo, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sContent = Regex.Replace(sContent, "<(/)?ppara", "<$1para", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        ' ((<label>(((?!</label>).)+)</label>)?<title([^><]+)?>(((?!</title>).)+)</title>((<subtitle[^><]+>(((?!</subtitle>).)+)</subtitle>|<footnote([^><]+)?>(((?!</footnote>).)+)</footnote>|(<authorgroup>(((?!</authorgroup>).)+)</authorgroup>|<author>(((?!</author>).)+)</author>))+)?)
        sContent = Regex.Replace(sContent, "(<cover([^><]+)?>)(<bibliolist([^><]+)?>)?(<bibliomixed([^><]+)?>)?([^><]+)(<para([^><]+)?>)", "$1$3$5<para xml:id=""pa-000000001"">$7</para>$8", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sContent = Regex.Replace(sContent, "(<title([^><]+)?>(?:(?:(?!(<info>|<cover[^><]+>)).)+))(<info><cover[^><]+>|<cover[^><]+>)(<bibliolist([^><]+)?>)?(<bibliomixed([^><]+)?>)?", AddressOf TitleBib, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sContent = Regex.Replace(sContent, "(<title([^><]+)?>(((?!</title>).)+)</title>((<subtitle[^><]+>(((?!</subtitle>).)+)</subtitle>|<footnote([^><]+)?>(((?!</footnote>).)+)</footnote>|(<authorgroup>(((?!</authorgroup>).)+)</authorgroup>|<author>(((?!</author>).)+)</author>))+)?)",
                                 AddressOf InfoTags, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

        'sContent = Regex.Replace(sContent, "<[^><]+((http://|www\.)[^><]+)", AddressOf AttriWeb, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sContent = Regex.Replace(sContent, "((http://|www\.)+[^ <]+)", "<link xlink:href=""$1""><uri>$1</uri></link>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sContent = Regex.Replace(sContent, "<link xlink:href=""www", "<link xlink:href=""http://www", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sContent = Regex.Replace(sContent, "[^ ><]+(\.(com|org))", AddressOf WebLinkPro, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sContent = Regex.Replace(sContent, "h123ttp", "http", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sContent = Regex.Replace(sContent, "w123ww", "www", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sContent = Regex.Replace(sContent, "<a id=""page_([^""]+)""([^><]+)>", "<?page value=""$1""?>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sContent = Regex.Replace(sContent, "( )+", " ", RegexOptions.IgnoreCase Or RegexOptions.Singleline)

        'sContent = Regex.Replace(sContent, "</cover>", "</cover></info>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sContent = Regex.Replace(sContent, "<info([^><]+)?>(((?!</info>).)+)</info>", AddressOf InfoRepeat, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sContent = Regex.Replace(sContent, "<bibliography([^><]+)?>(((?!</bibliography>).)+)</bibliography>", AddressOf InfoRepeatinBib, RegexOptions.IgnoreCase Or RegexOptions.Singleline)

        If (IsePubXML) Then
            Dim EpubMgr As New ePubCleanupManager(sXMLFilePath)
            Try
                If (Not EpubMgr.DoEpubCleanup(sContent)) Then
                    Return String.Empty
                End If
            Catch ex As Exception
                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            End Try

            Dim PreContent As String = "<?xml version=~1.0~ encoding=~UTF-8~ standalone=~yes~?><?oxygen SCHSchema=~docbook-mods.sch~?><?oxygen RNGSchema=~bloomsbury-mods.rnc~ type=~compact~?>" &
                "<book xmlns=~http://docbook.org/ns/docbook~ version=~5.0~ xml:id=~b-9781474279437~ xmlns:xlink=~http://www.w3.org/1999/xlink~ xml:lang=~en~ role=~fullText~ xmlns:mml=~http://www.w3.org/1998/Math/MathML~>" & Environment.NewLine
            PreContent = PreContent.Replace("~", Chr(34).ToString) & EpubMgr.XmlContent & "</book>"

            Try
                File.WriteAllText(Path.Combine(sXMLFilePath, "pagelist.xml"), EpubMgr.TocPageContent)
            Catch ex As Exception
                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            End Try
            Try
                File.WriteAllText(Path.Combine(sXMLFilePath, "oebps-page-map.xml"), EpubMgr.EbpsPageMapContent)
            Catch ex As Exception
                GBL.DeantaBallon(ex.Message, MessageType.MSGERROR)
            End Try
            Return PreContent
        Else
            Return sContent
        End If
    End Function

    Private iXMLid As Integer = 0

    Private Function InfoRepeatinBib(m As Match)
        Dim sResult As String = m.Value.ToString
        sResult = Regex.Replace(sResult, "</?info([^><]+)?>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Return sResult
    End Function
    Private Function InfoRepeat(m As Match)
        Dim sResult As String = m.Value.ToString
        If Regex.Matches(sResult, "<info([^><]+)?>", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Count > 1 Then
            sResult = m.Groups(2).Value.ToString
            sResult = Regex.Replace(sResult, "<info([^><]+)?>|</info>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            Return "<info" & m.Groups(1).Value.ToString & ">" & sResult
        End If
        Return sResult
    End Function

    Private Function InfoTags(m As Match)
        iXMLid = iXMLid + 1
        Return "<info xml:id=""ba-000000" & iXMLid & "e"">" & m.Value.ToString & "</info>"
    End Function

    Private Function AttriWeb(m As Match)
        Dim sResult As String = m.Value.ToString
        sResult = sResult.Replace("http", "h123ttp").Replace("www", "w123ww")
        Return sResult
    End Function

    Private Function WebLinkPro(m As Match)
        If Regex.IsMatch(m.Value.ToString, "http|www") Then Return m.Value.ToString
        Dim sResult As String = m.Value.ToString
        sResult = Regex.Replace(sResult, "[^ ><]+(\.com)", "<link xlink:href=""http://" & m.Value.ToString & """><uri>" & m.Value.ToString & "</uri></link>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Return sResult
    End Function

    Private Function ChapterInfo(m As Match)
        Dim sResult As String = m.Value.ToString
        If (sResult.Contains("<bibliography ")) Then
            Return sResult
        End If
        'Dim sDelInfo As String = m.Groups(2).Value.ToString
        'sDelInfo = Regex.Replace(sDelInfo, "</?info([^><]+)?>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sResult = Regex.Replace(sResult, "<chapter ([^><]+)?(xml:id=""[^""]+"")([^><]+)?><label>((?:(?!</label>).)+)</label>", "<chapter $2 label=""$4"">", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sResult = m.Groups(1).Value.ToString & "<info xml:id=""ba-000000" & iXMLid & "e"">" & sDelInfo & "</info>" & m.Groups(4).Value.ToString
        If Regex.Match(sResult, "<title([^><]+)?>((?:(?!</title>).)+)</title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline).Groups(2).Value.ToString.ToLower.Contains("introduction") Then
            sResult = Regex.Replace(sResult, "(xml:id=""[^""]+\-)(chapter(\d+)?)"">", "$1intro"">", RegexOptions.IgnoreCase)
        End If
        Dim AppMatch As Match = Regex.Match(sResult, "<title([^><]+)?>((?:(?!</title>).)+)</title>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        If AppMatch.Success Then
            Dim AppNumMatch As Match = Regex.Match(AppMatch.Groups(2).Value, "appendix ([0-9]+)", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
            If (AppNumMatch.Success) Then
                sResult = Regex.Replace(sResult, "(xml:id=""[^""]+\-)(chapter(\d+)?)"">", "$1appendix" & AppNumMatch.Groups(1).Value & """>", RegexOptions.IgnoreCase)
            End If
        End If
        Dim st As Match = Regex.Match(sResult, "<label>((?:(?!</label>).)+)</label>", RegexOptions.IgnoreCase)
        If st.Success Then
            sResult = Regex.Replace(sResult, "(xml:id=""[^""]+\-)(chapter(\d+)?)"">", " label=""" & st.Groups(1).Value.ToString & """ $1chapter" & st.Groups(1).Value.ToString & """>", RegexOptions.IgnoreCase)
            sResult = Regex.Replace(sResult, st.Value.ToString & "(( )+)?", "", RegexOptions.IgnoreCase)
        End If
        If Regex.IsMatch(sResult, "<section", RegexOptions.IgnoreCase) Then
            sResult = Regex.Replace(sResult, "disp-level=""[^""]+""", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        End If
        Return sResult
    End Function

    Private Function FootnoteInfo(m As Match)
        Dim sResult As String = m.Value.ToString
        sResult = Regex.Replace(sResult, "<footnote([^><]+)? (role=""[^""]+"")([^><]+)?(xml:id=""[^""]+"")([^><]+)?><label>((?:(?!</label>).)+)</label>", "<footnote $2 label=""$6"" $4>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sResult = Regex.Replace(sResult, "<para([^><]+)?>", "<ppara$1>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        'sResult = Regex.Replace(sResult, "</para>", "</ppara>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Return sResult
    End Function

    Private Function TitleBib(m As Match) ' >(((?!>).)+)</para>
        Dim sResult As String = m.Value.ToString
        sResult = Regex.Replace(sResult, "<info([^><]+)?>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sResult = "<info xml:id=""ti-000001"">" & m.Groups(1).Value.ToString & m.Groups(4).Value.ToString & m.Groups(5).Value.ToString & m.Groups(7).Value.ToString
        sResult = Regex.Replace(sResult, "<info>", "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sResult = Regex.Replace(sResult, "<para([^><]+)?>", "<bibliomisc role=""description"">", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sResult = Regex.Replace(sResult, "</para>", "</bibliomisc>", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Dim dt As Match = Regex.Match(sResult, "(<cover([^><]+)?>)(<bibliolist([^><]+)?>)?(<bibliomixed([^><]+)?>)?", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sResult = Regex.Replace(sResult, dt.Value.ToString, "", RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        sResult = Regex.Replace(sResult, "(<info([^><]+)?>)", "$1" & dt.Value.ToString, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
        Return sResult
    End Function

    Private Function CreateBatAndRunFile(BatFileContent As String, OutputPath As String) As Boolean
        Try
            If (File.Exists(Path.Combine(OutputPath, "run.bat"))) Then File.Delete(Path.Combine(OutputPath, "run.bat"))
            File.WriteAllText(Path.Combine(OutputPath, "run.bat"), BatFileContent)
            While (File.Exists(Path.Combine(OutputPath, "run.bat")))
                Exit While
            End While
            Dim SaxjanProcessInfo As New ProcessStartInfo(Path.Combine(OutputPath, "run.bat"))
            SaxjanProcessInfo.WorkingDirectory = OutputPath
            SaxjanProcessInfo.RedirectStandardError = True
            SaxjanProcessInfo.RedirectStandardOutput = True
            SaxjanProcessInfo.CreateNoWindow = True
            SaxjanProcessInfo.WindowStyle = ProcessWindowStyle.Hidden
            SaxjanProcessInfo.UseShellExecute = False
            Dim SaxjanProcess As Process = Process.Start(SaxjanProcessInfo)
            SaxjanProcess.WaitForExit()
            'Threading.Thread.Sleep(2000)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function
    ' Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "extra")
End Class


Public Class MovePageNumData
    Public Property ElementName As String = String.Empty
    Public Property Position As PagePosition = PagePosition.NONE
End Class

Public Enum PagePosition
    NONE = 0
    PREFIX = 1
    SUFFIX = 2
End Enum


Public Class IndexSeeData
    Public Property SeeTerm As String = String.Empty
    Public Property SeeIndex As Integer = 0

End Class

Public Class ISBNData
    Public Property PubType As String = String.Empty
    Public Property IsExists As Boolean = False
End Class