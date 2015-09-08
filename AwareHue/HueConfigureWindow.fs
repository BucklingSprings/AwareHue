namespace BucklingSprings.Aware.AddOns.Hue


open System
open System.ComponentModel
open System.Collections.ObjectModel
open System.Windows
open System.Windows.Media
open System.Windows.Controls
open System.Windows.Input

open Q42.HueApi
open Q42.HueApi.NET

[<AllowNullLiteral()>]
type LightViewModel(index: string, name: string) =
    member x.Index = index
    member x.Name = name
    override x.ToString () = sprintf "%s - %s" index name

type HueConfigureWindowViewModel() as vm =
    let okBrush = Brushes.Green :> Brush
    let problemBrush = Brushes.Red :> Brush
    let defaultBrush = Brushes.Black :> Brush
    let ev = new Event<_,_>()
    let bridgeList = ObservableCollection<string>()
    let lightList = ObservableCollection<LightViewModel>()
    let mutable statusText : string = String.Empty
    let mutable statusBrush : Brush = okBrush
    let showStatus s b =
        statusText <- s
        statusBrush <- b
        ev.Trigger(vm, PropertyChangedEventArgs("StatusText"))
        ev.Trigger(vm, PropertyChangedEventArgs("StatusBrush"))

    let createCommand action canExecute =
            let event1 = Event<_, _>()
            {
                new ICommand with
                    member this.CanExecute(obj) = canExecute(obj)
                    member this.Execute(obj) = action(obj)
                    member this.add_CanExecuteChanged(handler) = event1.Publish.AddHandler(handler)
                    member this.remove_CanExecuteChanged(handler) = event1.Publish.AddHandler(handler)
            }
    let fetchBridges _ =
        bridgeList.Clear()
        
        let loc = HttpBridgeLocator()
        let results = loc.LocateBridgesAsync(TimeSpan.FromSeconds(5.0)).Result
        results
            |> Seq.iter (fun s -> bridgeList.Add(s))
        showStatus (sprintf "Located %d bridge(s)." bridgeList.Count) okBrush
    let link _ =
        let bridge = HueConfigurationStore.currentBridge ()
        if (String.IsNullOrWhiteSpace bridge) then
            showStatus "Please select a bridge" problemBrush
        else
            let appNm = HueConfigurationStore.currentApplicationName ()
            let passWord = HueConfigurationStore.currentApplicationPassword ()
            
            let c = new HueClient(bridge, passWord)
            let registered = c.RegisterAsync(appNm, passWord).Result

            if registered then
                showStatus "Registered Sucessfully" okBrush
            else
                showStatus "Unable to register" problemBrush

    let fetchLights _ =
        lightList.Clear()
        let bridge = HueConfigurationStore.currentBridge ()
        let appNm = HueConfigurationStore.currentApplicationName ()
        let passWord = HueConfigurationStore.currentApplicationPassword ()
        let c = new HueClient(bridge, passWord)
        let ls = c.GetLightsAsync().Result
        ls
            |> Seq.iter (fun l -> lightList.Add(LightViewModel(l.Id, l.Name)))
        showStatus (sprintf "Located %d Light(s)." lightList.Count) okBrush

    let currentLight () =
        let lightIndex = HueConfigurationStore.currentLightIndex ()
        let lightNm = HueConfigurationStore.currentLightName ()
        let lightNotOk = String.IsNullOrWhiteSpace(lightIndex) || String.IsNullOrWhiteSpace(lightIndex)
        if (lightNotOk) then
            null
        else
            LightViewModel(lightIndex, lightNm)

    let testLight _ =
        let light = currentLight ()
        if light = Unchecked.defaultof<_> then
            showStatus "Please connect a bridge and select a light." problemBrush
        else
            if (Hue.test ()) then
                showStatus "Light tested ok." okBrush
            else
                showStatus "Unable to connect to light." problemBrush
        
    do
        
        bridgeList.Add(HueConfigurationStore.currentBridge ())
        let light = currentLight ()
        if light = Unchecked.defaultof<_> then
            ()
        else
            lightList.Add(light)

            
        
    member x.BridgeAddresses = bridgeList
    member x.Lights = lightList
    member x.RefreshBridgeList = createCommand fetchBridges (fun _ -> true)
    member x.Connect = createCommand link (fun _ -> true)
    member x.TestLight = createCommand testLight (fun _ -> true)
    member x.RefreshLightList = createCommand fetchLights (fun _ -> true)
    member x.SelectedBridge
        with get () = HueConfigurationStore.currentBridge ()
        and set (v) =
            HueConfigurationStore.storeBridge v
    member x.SelectedLight
        with get () = 
            currentLight ()
        and set (l : LightViewModel) =
            if l = Unchecked.defaultof<_> then
                ()
            else
                HueConfigurationStore.storeLightIndex l.Index
                HueConfigurationStore.storeLightName l.Name

    member x.ApiApplicationName
        with get () = HueConfigurationStore.currentApplicationName ()
        and set (v) =
            HueConfigurationStore.storeApplicationName v
    member x.ApiApplicationPassword
        with get () = HueConfigurationStore.currentApplicationPassword ()
        and set (v) =
            HueConfigurationStore.storeApplicationPassword v
    member x.StatusText = statusText
    member x.StatusBrush = statusBrush
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = ev.Publish

type HueConfigureWindow() as w =
    inherit Window()
    do
        w.Title <- "Aware Hue"
        w.SizeToContent <- SizeToContent.WidthAndHeight
        let content = Application.LoadComponent(Uri("/AwareHue;component/HueConfigureWindow.xaml", UriKind.Relative)) :?> UserControl
        w.Content <- content
        w.DataContext <- HueConfigureWindowViewModel()
        