Imports System
Imports System.IO
Imports System.Convert

Module Module1
    Public RNoGen As New Random()

    Sub Main()
        Dim ThisGame As New Breakthrough
        ThisGame.PlayGame()
        Console.ReadLine()
    End Sub

    Class Breakthrough
        Private Deck As CardCollection
        Private Hand As CardCollection
        Private Sequence As CardCollection
        Private Discard As CardCollection
        Private Locks As List(Of Lock)
        
        Private Score As Integer
        Private GameOver As Boolean
        Private CurrentLock As Lock
        Private LockSolved As Boolean

        Sub New()
            Deck = New CardCollection("DECK")
            Hand = New CardCollection("HAND")
            Sequence = New CardCollection("SEQUENCE")
            Discard = New CardCollection("DISCARD")
            Score = 0
            LoadLocks()
        End Sub

        Public Sub PlayGame()
            Dim MenuChoice As String
            If Locks.Count > 0 Then
                GameOver = False
                CurrentLock = New Lock()
                SetupGame()
                While Not GameOver
                    LockSolved = False
                    While Not LockSolved And Not GameOver
                        Console.WriteLine()
                        Console.WriteLine("Current score: " & Score)
                        Console.WriteLine(CurrentLock.GetLockDetails())
                        Console.WriteLine(Sequence.GetCardDisplay())
                        Console.WriteLine(Hand.GetCardDisplay())
                        MenuChoice = GetChoice()
                        Select Case MenuChoice
                            Case "D"
                                Console.WriteLine(Discard.GetCardDisplay())
                            Case "U"
                                Dim CardChoice As Integer = GetCardChoice()
                                Dim DiscardOrPlay As String = GetDiscardOrPlayChoice()
                                If DiscardOrPlay = "D" Then
                                    MoveCard(Hand, Discard, Hand.GetCardNumberAt(CardChoice - 1))
                                    GetCardFromDeck(CardChoice)
                                ElseIf DiscardOrPlay = "P" Then
                                    PlayCardToSequence(CardChoice)
                                End If
                        End Select
                        If CurrentLock.GetLockSolved() Then
                            LockSolved = True
                            ProcessLockSolved()
                        End If
                    End While
                    GameOver = CheckIfPlayerHasLost()
                End While
            Else
                Console.WriteLine("No locks in file.")
            End If
        End Sub

        Private Sub ProcessLockSolved()
            Score += 10
            Console.WriteLine("Lock has been solved.  Your score is now: " & Score)
            While Discard.GetNumberOfCards() > 0
                MoveCard(Discard, Deck, Discard.GetCardNumberAt(0))
            End While
            Deck.Shuffle()
            CurrentLock = GetRandomLock()
        End Sub

        Private Function CheckIfPlayerHasLost() As Boolean
            If Deck.GetNumberOfCards() = 0 Then
                Console.WriteLine("You have run out of cards in your deck.  Your final score is: " & Score)
                Return True
            Else
                Return False
            End If
        End Function

        Private Sub SetupGame()
            Dim Choice As String
            Console.Write("Enter L to load a game from a file, anything else to play a new game:> ")
            Choice = Console.ReadLine().ToUpper()
            If Choice = "L" Then
                If Not LoadGame("game1.txt") Then
                    GameOver = True
                End If
            Else
                CreateStandardDeck()
                Deck.Shuffle()
                For Count = 1 To 5
                    MoveCard(Deck, Hand, Deck.GetCardNumberAt(0))
                Next
                AddDifficultyCardsToDeck()
                Deck.Shuffle()
                CurrentLock = GetRandomLock()
            End If
        End Sub

        Private Sub PlayCardToSequence(ByVal CardChoice As Integer)
            If Sequence.GetNumberOfCards() > 0 Then
                If Hand.GetCardDescriptionAt(CardChoice - 1)(0) <> Sequence.GetCardDescriptionAt(Sequence.GetNumberOfCards() - 1)(0) Then
                    Score += MoveCard(Hand, Sequence, Hand.GetCardNumberAt(CardChoice - 1))
                    GetCardFromDeck(CardChoice)
                End If
            Else
                Score += MoveCard(Hand, Sequence, Hand.GetCardNumberAt(CardChoice - 1))
                GetCardFromDeck(CardChoice)
            End If
        
            If CheckIfLockChallengeMet() Then
                Console.WriteLine()
                Console.WriteLine("A challenge on the lock has been met.")
                Console.WriteLine()
                Score += 5
            End If
        End Sub

        Private Function CheckIfLockChallengeMet() As Boolean
            Dim SequenceAsString As String = ""
            For Count = Sequence.GetNumberOfCards() - 1 To Math.Max(0, Sequence.GetNumberOfCards() - 3) Step -1
                If SequenceAsString.Length > 0 Then
                    SequenceAsString = ", " & SequenceAsString
                End If
                SequenceAsString = Sequence.GetCardDescriptionAt(Count) & SequenceAsString
                If CurrentLock.CheckIfConditionMet(SequenceAsString) Then
                    Return True
                End If
            Next
    
            Return False
        End Function

        Private Sub SetupCardCollectionFromGameFile(ByVal LineFromFile As String, ByVal CardCol As CardCollection)
            Dim SplitLine As List(Of String)
            Dim CardNumber As Integer
            If LineFromFile.Length > 0 Then
                SplitLine = LineFromFile.Split(",").ToList()
                For Each Item In SplitLine
                    If Item.Length = 5 Then
                        CardNumber = ToInt32(Item(4))
                    Else
                        CardNumber = ToInt32(Item.Substring(4, 2))
                    End If
        
                    If Item.Substring(0, 3) = "Dif" Then
                        Dim CurrentCard As New DifficultyCard(CardNumber)
                        CardCol.AddCard(CurrentCard)
                    Else
                        Dim CurrentCard As New ToolCard(Item(0), Item(2), CardNumber)
                        CardCol.AddCard(CurrentCard)
                    End If
                Next
            End If
        End Sub

        Private Sub SetupLock(ByVal Line1 As String, ByVal Line2 As String)
            Dim SplitLine As List(Of String)
            SplitLine = Line1.Split(";").ToList()
            For Each Item In SplitLine
                Dim Conditions As New List(Of String)
                Conditions = Item.Split(",").ToList()
                CurrentLock.AddChallenge(Conditions)
            Next

            SplitLine = Line2.Split(";").ToList()
            For Count = 0 To SplitLine.Count - 1
                If SplitLine(Count) = "Y" Then
                    CurrentLock.SetChallengeMet(Count, True)
                End If
            Next
        End Sub

        Private Function LoadGame(ByVal FileName As String) As Boolean
            Dim LineFromFile As String
            Dim LineFromFile2 As String
            Try
                Using MyStream As New StreamReader(FileName)
                    LineFromFile = MyStream.ReadLine()
                    Score = ToInt32(LineFromFile)
                    LineFromFile = MyStream.ReadLine()
                    LineFromFile2 = MyStream.ReadLine()
                    SetupLock(LineFromFile, LineFromFile2)
                    LineFromFile = MyStream.ReadLine()
                    SetupCardCollectionFromGameFile(LineFromFile, Hand)
                    LineFromFile = MyStream.ReadLine()
                    SetupCardCollectionFromGameFile(LineFromFile, Sequence)
                    LineFromFile = MyStream.ReadLine()
                    SetupCardCollectionFromGameFile(LineFromFile, Discard)
                    LineFromFile = MyStream.ReadLine()
                    SetupCardCollectionFromGameFile(LineFromFile, Deck)
                End Using
                Return True

            Catch
                Console.WriteLine("File not loaded")
                Return False
            End Try
        End Function

        Private Sub LoadLocks()
            Dim FileName As String = "locks.txt"
            Dim LineFromFile As String
            Dim Challenges As List(Of String)
            Locks = New List(Of Lock)
            Try
                Using MyStream As New StreamReader(FileName)
                    LineFromFile = MyStream.ReadLine()
                    While Not LineFromFile Is Nothing
                        Challenges = LineFromFile.Split(";").ToList()
                        Dim LockFromFile As New Lock
    
                        For Each C In Challenges
                            Dim Conditions As New List(Of String)
                            Conditions = C.Split(",").ToList()
                            LockFromFile.AddChallenge(Conditions)
                        Next

                        Locks.Add(LockFromFile)
                        LineFromFile = MyStream.ReadLine()
                    End While
                End Using
            Catch
                Console.WriteLine("File not loaded")
            End Try
        End Sub

        Private Function GetRandomLock() As Lock
            Return Locks(RNoGen.Next(0, Locks.Count))
        End Function

        Private Sub GetCardFromDeck(ByVal CardChoice As Integer)
            If Deck.GetNumberOfCards() > 0 Then
                If Deck.GetCardDescriptionAt(0) = "Dif" Then
                    Dim CurrentCard As Card = Deck.RemoveCard(Deck.GetCardNumberAt(0))
                    Console.WriteLine()
                    Console.WriteLine("Difficulty encountered!")
                    Console.WriteLine(Hand.GetCardDisplay())
                    Console.Write("To deal with this you need to either lose a key ")
                    Console.Write("(enter 1-5 to specify position of key) or (D)iscard five cards from the deck:> ")
                    Dim Choice As String = Console.ReadLine()
                    Console.WriteLine()
                    Discard.AddCard(CurrentCard)
                    CurrentCard.Process(Deck, Discard, Hand, Sequence, CurrentLock, Choice, CardChoice)
                End If
            End If

            While Hand.GetNumberOfCards() < 5 And Deck.GetNumberOfCards() > 0
                If Deck.GetCardDescriptionAt(0) = "Dif" Then
                    MoveCard(Deck, Discard, Deck.GetCardNumberAt(0))
                    Console.WriteLine("A difficulty card was discarded from the deck when refilling the hand.")
                Else
                    MoveCard(Deck, Hand, Deck.GetCardNumberAt(0))
                End If
            End While

            If Deck.GetNumberOfCards() = 0 And Hand.GetNumberOfCards() < 5 Then
                GameOver = True
            End If
        End Sub

        Private Function GetCardChoice() As Integer
            Dim Choice As String
            Dim Value As Integer
            Do
                Console.Write("Enter a number between 1 and 5 to specify card to use:> ")
                Choice = Console.ReadLine()
            Loop Until Integer.TryParse(Choice, Value)
            Return Value
        End Function

        Private Function GetDiscardOrPlayChoice() As String
            Dim Choice As String
            Console.Write("(D)iscard or (P)lay?:> ")
            Choice = Console.ReadLine().ToUpper()
            Return Choice
        End Function

        Private Function GetChoice() As String
            Console.WriteLine()
            Console.Write("(D)iscard inspect, (U)se card:> ")
            Dim Choice As String = Console.ReadLine().ToUpper()
            Return Choice
        End Function

        Private Sub AddDifficultyCardsToDeck()
            For Count = 1 To 5
                Deck.AddCard(New DifficultyCard())
            Next
        End Sub

        Private Sub CreateStandardDeck()
            Dim NewCard As Card
            For Count = 1 To 5
                NewCard = New ToolCard("P", "a")
                Deck.AddCard(NewCard)
                NewCard = New ToolCard("P", "b")
                Deck.AddCard(NewCard)
                NewCard = New ToolCard("P", "c")
                Deck.AddCard(NewCard)
            Next
            For Count = 1 To 3
                NewCard = New ToolCard("F", "a")
                Deck.AddCard(NewCard)
                NewCard = New ToolCard("F", "b")
                Deck.AddCard(NewCard)
                NewCard = New ToolCard("F", "c")
                Deck.AddCard(NewCard)
                NewCard = New ToolCard("K", "a")
                Deck.AddCard(NewCard)
                NewCard = New ToolCard("K", "b")
                Deck.AddCard(NewCard)
                NewCard = New ToolCard("K", "c")
                Deck.AddCard(NewCard)
            Next
        End Sub

        Private Function MoveCard(ByVal FromCollection As CardCollection, ByVal ToCollection As CardCollection, ByVal CardNumber As Integer) As Integer
            Dim Score As Integer = 0
            If FromCollection.GetName() = "HAND" And ToCollection.GetName() = "SEQUENCE" Then
                Dim CardToMove As Card = FromCollection.RemoveCard(CardNumber)
                If CardToMove IsNot Nothing Then
                    ToCollection.AddCard(CardToMove)
                    Score = CardToMove.GetScore()
                End If
            Else
                Dim CardToMove As Card = FromCollection.RemoveCard(CardNumber)
                If CardToMove IsNot Nothing Then
                    ToCollection.AddCard(CardToMove)
                End If
            End If
            Return Score
        End Function
    End Class

    Class Challenge
        Protected Condition As List(Of String)
        Protected Met As Boolean

        Sub New()
            Met = False
        End Sub

        Public Function GetMet() As Boolean
            Return Met
        End Function

        Public Function GetCondition() As List(Of String)
            Return Condition
        End Function

        Public Sub SetMet(ByVal NewValue As Boolean)
            Met = NewValue
        End Sub

        Public Sub SetCondition(ByVal NewCondition As List(Of String))
            Condition = NewCondition
        End Sub
    End Class

    Class Lock
        Protected Challenges As New List(Of Challenge)

        Public Overridable Sub AddChallenge(ByVal Condition As List(Of String))
            Dim C As New Challenge
            C.SetCondition(Condition)
            Challenges.Add(C)
        End Sub

        Private Function ConvertConditionToString(ByVal C As List(Of String)) As String
            Dim ConditionAsString As String = ""
            For Pos = 0 To C.Count - 2
                ConditionAsString &= C(Pos) & ", "
            Next
            ConditionAsString &= C(C.Count - 1)
            Return ConditionAsString
        End Function

        Public Overridable Function GetLockDetails() As String
            Dim LockDetails As String = Environment.NewLine & "CURRENT LOCK" & Environment.NewLine & "------------" & Environment.NewLine
            For Each C In Challenges
                If C.GetMet() Then
                    LockDetails &= "Challenge met: "
                Else
                    LockDetails &= "Not met:       "
                End If
                LockDetails &= ConvertConditionToString(C.GetCondition()) & Environment.NewLine
            Next
            LockDetails &= Environment.NewLine
            Return LockDetails
        End Function

        Public Overridable Function GetLockSolved() As Boolean
            For Each C In Challenges
                If Not C.GetMet() Then
                    Return False
                End If
            Next
            Return True
        End Function

        Public Overridable Function CheckIfConditionMet(ByVal Sequence As String) As Boolean
            For Each C In Challenges
                If Not C.GetMet() And Sequence = ConvertConditionToString(C.GetCondition()) Then
                    C.SetMet(True)
                    Return True
                End If
            Next
            Return False
        End Function

        Public Overridable Sub SetChallengeMet(ByVal Pos As Integer, ByVal Value As Boolean)
            Challenges(Pos).SetMet(Value)
        End Sub

        Public Overridable Function GetChallengeMet(ByVal Pos As Integer) As Boolean
            Return Challenges(Pos).GetMet()
        End Function

        Public Overridable Function GetNumberOfChallenges() As Integer
            Return Challenges.Count
        End Function
    End Class

    Class Card
        Protected CardNumber, Score As Integer
        Protected Shared NextCardNumber As Integer = 1

        Sub New()
            CardNumber = NextCardNumber
            NextCardNumber += 1
            Score = 0
        End Sub

        Public Overridable Function GetScore() As Integer
            Return Score
        End Function

        Public Overridable Sub Process(ByVal Deck As CardCollection, ByVal Discard As CardCollection, ByVal Hand As CardCollection, ByVal Sequence As CardCollection, ByVal CurrentLock As Lock, ByVal Choice As String, ByVal CardChoice As Integer)
        End Sub

        Public Overridable Function GetCardNumber() As Integer
            Return CardNumber
        End Function

        Public Overridable Function GetDescription() As String
            If CardNumber < 10 Then
                Return " " & CardNumber.ToString()
            Else
                Return CardNumber.ToString()
            End If
        End Function
    End Class

    Class ToolCard
        Inherits Card

        Protected ToolType As String
        Protected Kit As String

        Sub New(ByVal T As String, ByVal K As String)
            MyBase.New()
            ToolType = T
            Kit = K
            SetScore()
        End Sub

        Public Sub New(ByVal T As String, ByVal K As String, ByVal CardNo As Integer)
            ToolType = T
            Kit = K
            CardNumber = CardNo
            SetScore()
        End Sub

        Private Sub SetScore()
            Select Case ToolType
                Case "K"
                    Score = 3
                Case "F"
                    Score = 2
                Case "P"
                    Score = 1
            End Select
        End Sub

        Public Overrides Function GetDescription() As String
            Return ToolType & " " & Kit
        End Function
    End Class

    Class DifficultyCard
        Inherits Card

        Protected CardType As String

        Sub New()
            MyBase.New()
            CardType = "Dif"
        End Sub

        Public Sub New(ByVal CardNo As Integer)
            CardType = "Dif"
            CardNumber = CardNo
        End Sub

        Public Overrides Function GetDescription() As String
            Return CardType
        End Function

        Public Overrides Sub Process(ByVal Deck As CardCollection, ByVal Discard As CardCollection, ByVal Hand As CardCollection, ByVal Sequence As CardCollection, ByVal CurrentLock As Lock, ByVal Choice As String, ByVal CardChoice As Integer)
            Dim ChoiceAsInteger As Integer
            If Integer.TryParse(Choice, ChoiceAsInteger) Then
                If ChoiceAsInteger >= 1 And ChoiceAsInteger <= 5 Then
                    If ChoiceAsInteger >= CardChoice Then
                        ChoiceAsInteger -= 1
                    End If
                    If ChoiceAsInteger > 0 Then
                        ChoiceAsInteger -= 1
                    End If
                    If Hand.GetCardDescriptionAt(ChoiceAsInteger)(0) = "K" Then
                        Dim CardToMove As Card = Hand.RemoveCard(Hand.GetCardNumberAt(ChoiceAsInteger))
                        Discard.AddCard(CardToMove)
                        Return
                    End If
                End If
            End If
            Dim Count As Integer = 0
            While Count < 5 And Deck.GetNumberOfCards() > 0
                Dim CardToMove As Card = Deck.RemoveCard(Deck.GetCardNumberAt(0))
                Discard.AddCard(CardToMove)
                Count += 1
            End While
        End Sub
    End Class

    Class CardCollection
        Protected Cards As New List(Of Card)
        Protected Name As String

        Sub New(ByVal N As String)
            Name = N
        End Sub

        Public Function GetName() As String
            Return Name
        End Function

        Public Function GetCardNumberAt(ByVal X As Integer) As Integer
            Return Cards(X).GetCardNumber()
        End Function

        Public Function GetCardDescriptionAt(ByVal X As Integer) As String
            Return Cards(X).GetDescription()
        End Function

        Public Sub AddCard(ByVal C As Card)
            Cards.Add(C)
        End Sub

        Public Function GetNumberOfCards() As Integer
            Return Cards.Count
        End Function

        Public Sub Shuffle()
            Dim TempCard As Card
            Dim RNo1, RNo2 As Integer
            For Count = 1 To 10000
                RNo1 = RNoGen.Next(0, Cards.Count)
                RNo2 = RNoGen.Next(0, Cards.Count)
                TempCard = Cards(RNo1)
                Cards(RNo1) = Cards(RNo2)
                Cards(RNo2) = TempCard
            Next
        End Sub

        Public Function RemoveCard(ByVal CardNumber As Integer) As Card
            Dim CardFound As Boolean = False
            Dim Pos As Integer = 0
            Dim CardToGet As Card = Nothing
            While Pos < Cards.Count And Not CardFound
                If Cards(Pos).GetCardNumber() = CardNumber Then
                    CardToGet = Cards(Pos)
                    CardFound = True
                    Cards.RemoveAt(Pos)
                End If
                Pos += 1
            End While
            Return CardToGet
        End Function

        Private Function CreateLineOfDashes(ByVal Size As Integer) As String
            Dim LineOfDashes As String = ""
            For Count = 1 To Size
                LineOfDashes &= "------"
            Next
            Return LineOfDashes
        End Function

        Public Function GetCardDisplay() As String
            Dim CardDisplay As String = Environment.NewLine & Name & ":"
            If Cards.Count = 0 Then
                Return CardDisplay & " empty" & Environment.NewLine & Environment.NewLine
            Else
                CardDisplay &= Environment.NewLine & Environment.NewLine
            End If
            Dim LineOfDashes As String
            Const CardsPerLine As Integer = 10
            If Cards.Count > CardsPerLine Then
                LineOfDashes = CreateLineOfDashes(CardsPerLine)
            Else
                LineOfDashes = CreateLineOfDashes(Cards.Count)
            End If
            CardDisplay &= LineOfDashes & Environment.NewLine
            Dim Complete As Boolean = False
            Dim Pos As Integer = 0
            While Not Complete
                CardDisplay &= "| " & Cards(Pos).GetDescription() & " "
                Pos += 1
                If Pos Mod CardsPerLine = 0 Then
                    CardDisplay &= "|" & Environment.NewLine & LineOfDashes & Environment.NewLine
                End If
                If Pos = Cards.Count Then
                    Complete = True
                End If
            End While
            If Cards.Count Mod CardsPerLine > 0 Then
                CardDisplay &= "|" & Environment.NewLine
                If Cards.Count > CardsPerLine Then
                    LineOfDashes = CreateLineOfDashes(Cards.Count Mod CardsPerLine)
                End If
                CardDisplay &= LineOfDashes & Environment.NewLine
            End If
            Return CardDisplay
        End Function
    End Class
End Module
