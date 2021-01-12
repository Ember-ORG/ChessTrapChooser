// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open TimHanewich.Chess
open System.IO
open TimHanewich.Chess.Pgn
open FSharp.Data
open FSharpPlus.Operators
open FSharp.Collections.ParallelSeq

type Trap = First
            | Other

type Output = Map<int, Trap>
type Move = string

module Player = 
    type T = White | Black
    let fromString x =
        match x with
        | "White" -> Some White
        | "Black" -> Some Black
        | _ -> None



module Result =
    type T = White | Black | Draw
    let fromString x =
        match x with
        | "1-0" -> Some White
        | "0-1" -> Some Black
        | "1/2-1/2" -> Some Draw
        | _ -> None

module Trap = 
    type Variant = {Attempt: Move list; SuccessCondition: Move list; From: Player.T option;}
    type T = {Name: string; Variants: Variant list}

    let loadTraps filename =
        let isNewTrap (row: CsvRow) = 
            row.Item(0) <> ""
        
        let chunkToTuple3 chunk =
            match chunk with
            | [|a;b;c|] -> (*printfn "\n\n\n%A" chunk; *)(a, b, c)
            | _ -> failwith <| sprintf "unexpected row count %A" chunk

        let tuple3ToColumns (x: CsvRow, y: CsvRow, z: CsvRow) =
            let noEmpty x = x |> Array.filter (fun x -> x <> "") 

            let f (x: CsvRow) = noEmpty <| Array.tail x.Columns
            (x.Columns.[0], f x, f y, f z)

        let columnsToTuples (name, xs, ys, zs) = Array.zip3 xs ys zs |> Array.map (fun (x, y, z) -> (name, z, x, y))
        let parseTuple (name, trapper,  x, y) = (name, trapper, PgnParserLite.ParsePgn x, PgnParserLite.ParsePgn y)
        let toVariant (name, trapper, x: PgnParserLite, y: PgnParserLite) = 
            (name, {
                Attempt = Array.toList x.Moves;
                SuccessCondition = Array.toList y.Moves;
                From = Player.fromString trapper;
            })

        CsvFile.Load(uri = filename).Rows
        |> Seq.chunkBySize 3
        |> PSeq.collect (chunkToTuple3 >> tuple3ToColumns >> columnsToTuples)
        |> PSeq.map (parseTuple >> toVariant)
        |> PSeq.groupBy (fun (name,_) -> name)
        |> PSeq.map (fun (name, xs) ->
            {
                Name = name;
                Variants = xs |> Seq.map (fun (_, vs) -> vs) |> Seq.toList
            }
        )

    let Traps = loadTraps "/home/davis/Documents/Personal/CSProjects/Ongoing/ChessTrapper/dbgen/trapdb.csv"

    type Found = {Used: T; Variant: Variant; Accepted: bool}

    let check moveList =
        //printfn "%A" (Seq.length moveList)

        let startsWith x y =
            PSeq.zip x y
            |> PSeq.forall (fun (x,y) ->
            //printfn "%A %A" x y
            x = y)


        let hasTrap {Name=_; Variants=vs} =
            //printfn "%A" <| Seq.length vs
            vs
            |> PSeq.filter (fun {Attempt=x; SuccessCondition=_; From=_} -> 
                startsWith x moveList
            )
            |> Seq.tryHead

        let wasAccepted {Attempt=_; SuccessCondition=x; From=_} = startsWith x moveList

        Traps
        |> PSeq.map (fun x -> (x, hasTrap x))
        |> PSeq.filter (fun(_, y) -> y.IsSome)
        |> PSeq.map (fun (x, Some y) ->
                {
                    Used = x;
                    Variant = y;
                    Accepted = wasAccepted y;
                }
        )


module ReducedGame =
    type T = {AverageElo: int; Win: Result.T option; Moves: string list}
    let fromPgn (x: PgnParserLite) =
        {
            AverageElo = (x.WhiteElo+x.BlackElo)/2;
            Win = x.Result |> Result.fromString;
            Moves = x.Moves |> Array.toList
        }


let splitAndParse pgnStream =
    let splitter = BatchAnalysis.MassivePgnFileSplitter pgnStream
    let skipBy = 500
    let doneAt = 9999999

    let resetTime = 10000 / (skipBy/3)

    let mutable timer = System.Diagnostics.Stopwatch()
    timer.Start()

    Seq.unfold (
        fun (st, secper, calc) ->
            let newsecper, calc =
                if secper = resetTime then
                    let calc = (Math.Pow(((float(timer.ElapsedMilliseconds) / 1000.0) / float(resetTime)),-1.0)) * float(skipBy)

                    timer <- Diagnostics.Stopwatch()
                    timer.Start()
                    (0, calc)
                else
                    (secper + 1, calc)

            
            printfn "%A %A %A %A" st secper calc (((89422803.0-float(st))/calc)/60.0)
            [0..skipBy] |> List.iter (fun _ -> splitter.GetNextGame () |> ignore)
            if st > doneAt then
                None
            else
                try
                    Some (PgnParserLite.ParsePgn <| splitter.GetNextGame (), (st + skipBy + 1, newsecper, calc))
                with
                | :? NullReferenceException -> None

    ) (0, 0, 0.0)

open Trap
open Nessos.LinqOptimizer.FSharp

type ELO = int

type MapType = Map<ELO, Map<Trap.T, (int * int)>>

[<EntryPoint>]
let main argv =

    let mutable i = 0

    
    let folder (map: MapType) (x: ReducedGame.T)  =

        let roundElo x =
            let roundpoint = 300
            int(Math.Round (a=float(x)/float(roundpoint)) * float(roundpoint))


        let changer {Used=y; Variant={Attempt=_;SuccessCondition=_;From=z}; Accepted=_} (a: Map<T,(int * int)> option) =
            i <- i + 1
            printfn "%A" i
            let trapWorked =
                x.Win = (Result.fromString <| z.ToString ())

            let map = match a with
                        | Some map -> map
                        | None -> (Map Seq.empty)

            Some <| map.Change(y, (fun a -> 
                Some <| match a with
                        | Some (b, c) when trapWorked -> printfn "%A" (b,c); (b+1, c+1)
                        | Some (b, c) -> (b, c+1)
                        | None when trapWorked -> (1, 1)
                        | None -> (0, 1)
                )
            )



        check x.Moves
        |> PSeq.fold (fun (newmap: MapType) y -> newmap.Change(roundElo x.AverageElo, changer y)) map
        //        map.Change(y, changer)



    use stream = new StreamReader "db.pgn"
    let final = splitAndParse stream.BaseStream
                |> PSeq.map ReducedGame.fromPgn
                |> PSeq.fold folder (Map Seq.empty)
    
    printfn "%A" final

    final
    |> Map.toSeq
    |> PSeq.map (fun (elo, map) ->
                map
                |> Map.toSeq
                |> PSeq.map (fun (trap, (won, total)) -> printfn "%A %A: %A %A" elo trap won total)
    ) |> ignore

    0 // return an integer exit code