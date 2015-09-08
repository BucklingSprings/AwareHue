namespace BucklingSprings.Aware.AddOns.Hue

open Microsoft.Win32

open Q42.HueApi

module HueConfigurationStore =

    let key = "HKEY_CURRENT_USER\Software\AwareHue"

    let getS (valueName : string) =
        Registry.GetValue(key, valueName, System.String.Empty) :?> string

    let storeS (valueName : string) (value : string)  =
        Registry.SetValue(key, valueName, value, RegistryValueKind.String)

    let getStoredOrDefault (valueName : string) (defaultValue : string) =
        let nm = getS valueName
        if (System.String.IsNullOrWhiteSpace(nm)) then
            storeS valueName defaultValue
            defaultValue
        else
            nm
        


    let storeBridge = storeS "BridgeAddress"
    let currentBridge () = getS "BridgeAddress"

    let storeApplicationName = storeS "ApplicationName"
    let currentApplicationName () = getStoredOrDefault "ApplicationName" "AwareHue"

    let storeApplicationPassword = storeS "ApplicationPassword"
    let currentApplicationPassword () = getStoredOrDefault "ApplicationPassword" "AtLeast10Chars"

    let storeLightIndex = storeS "LightIndex"
    let currentLightIndex () = getS "LightIndex"

    let storeLightName = storeS "LightName"
    let currentLightName () = getS "LightName"

    let canTryAndConnect () = System.String.IsNullOrWhiteSpace(currentBridge ())



module Hue =

    let configuredProperly () =
        let bridge = HueConfigurationStore.currentBridge ()
        let lightIndex = HueConfigurationStore.currentLightIndex()
        let key = HueConfigurationStore.currentApplicationPassword ()
        let empty s = System.String.IsNullOrWhiteSpace s
        let provided s = not (empty s)
        (provided bridge && provided lightIndex && provided key)
        
    let lightUp (color : string) (flash : bool) =
        if (configuredProperly ()) then
            let bridge = HueConfigurationStore.currentBridge ()
            let lightIndex = HueConfigurationStore.currentLightIndex()
            let key = HueConfigurationStore.currentApplicationPassword ()
            let c = new HueClient(bridge, key)
            let i = c.IsInitialized
            let command = new LightCommand();
            let command = (command.TurnOn()).SetColor(color)
            command.Effect <- new System.Nullable<Effect>(Effect.None)
            if flash then
                command.Alert <- System.Nullable<Alert>(Alert.Multiple)
            else
                command.Alert <- System.Nullable<Alert>(Alert.None)

            let results = c.SendCommandAsync(command, [| lightIndex |]).Result
            true
        else
            false

    
    let test () = lightUp "0000AA" true

    let flashingRed () = lightUp "AA0000" true
        
    let steadyRed () = lightUp "AA0000" false
        
    let steadyGreen () = lightUp "00AA00" false
        
    let steadyBlue () = lightUp "0000AA" false
        
    let flashingGreen () = lightUp "00AA00"  true
        


        