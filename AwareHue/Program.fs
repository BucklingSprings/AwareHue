namespace BucklingSprings.Aware.AddOns.Hue

open System
open System.Threading
open System.Windows

open Nessos.UnionArgParser

module Program =

    type Arguments =
        | More_Or_Less of string
        | Goal_Likely of string // Likely - Unlikely
        | Goal_Words of int // What is the goal
    with
        interface IArgParserTemplate with
            member s.Usage = 
                match s with
                    | Arguments.More_Or_Less _ -> "More, Less or Neutral. Should you be doing More or Less of this task." 
                    | Arguments.Goal_Likely _ -> "Is the goal likely to be met - Likely\Unlikely"
                    | Arguments.Goal_Words _ -> "Goal Words - Can be zero"
    
    
    [<EntryPoint>]
    [<STAThread>]
    let main argv = 

        let createdNew = ref false
        use mutex = new Mutex(true, "C932F1F1-FB37-426C-BF84-1591CAB9F50F", createdNew)
        if !createdNew then
            let parser = UnionArgParser.Create<Arguments>()
            let results = parser.Parse(argv)
            let launch = if results.Contains(<@ Arguments.More_Or_Less  @>) && Hue.configuredProperly() then
                            if results.GetResult(<@ Arguments.Goal_Words @>, 0) > 0 then
                                let likely = results.GetResult(<@ Arguments.Goal_Likely @>, "Likely").ToUpperInvariant()
                                match likely with
                                    | "LIKELY" -> Hue.steadyGreen() |> ignore
                                    | "UNLIKELY" -> Hue.steadyRed () |> ignore
                                    | _ -> Hue.steadyBlue() |> ignore
                            else
                                let moreOrLess = results.GetResult(<@ Arguments.More_Or_Less @>, "Neutral").ToUpperInvariant()
                                match moreOrLess with
                                    | "LESS" -> Hue.steadyRed() |> ignore
                                    | "MORE" -> Hue.steadyGreen () |> ignore
                                    | _ -> Hue.steadyBlue() |> ignore
                            false
                         else
                            true
            if launch then
                let a = Application()
                a.Run(HueConfigureWindow())
            else
                -1
        else
            -2
        