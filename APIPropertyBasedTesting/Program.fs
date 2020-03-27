open System
open FsCheck
open FSharp.Data
 


type Counter() =
    let mutable n = 0
    member __.Inc() = n <- n + 1

    member __.Dec() = n <- n - 1
    member __.Get = n
    member __.Reset() = n <- 0
    member __.ToString() = sprintf "Counter=%i" n

let spec =
    let inc =
        { new Command<Counter, int>() with

            member __.RunActual counter =
                counter.Inc()
                counter

            member __.RunModel m = m + 1
            member __.Post(counter, m) = counter.Get = m |@ sprintf "model: %i <> %A" m counter
            member __.ToString() = "inc" }

    let dec =
        { new Command<Counter, int>() with

            member __.RunActual counter =
                counter.Dec()
                counter
            member __.RunModel m = m - 1
            member __.Post(counter, m) = counter.Get = m |@ sprintf "model: %i <> %A" m counter
            member __.ToString() = "dec" }

    { new ICommandGenerator<Counter, int> with
        member __.InitialActual = Counter()
        member __.InitialModel = 0
        member __.Next model = Gen.elements [ inc; dec ] }

[<EntryPoint>]
let main argv =
    Check.Quick (Command.toProperty spec)
    0 // return an integer exit code
