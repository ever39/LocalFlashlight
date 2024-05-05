# LocalFlashlight
A LC mod which adds a flashlight that has a recharging battery, with visual indicators, custom colors, and multiple recharge options.

## Mod capabilities
The mod adds a flashlight that uses a battery system which recharges when not in use. It also adds an indicator that has multiple styles, and custom colors for both the HUD and light. Some values for the light and battery are also configurable.

## Installation and configuration

The mod can be installed through either Thunderstore Mod Manager or r2modman, or it can be installed manually by installing BepInEx for the game, downloading the mod manually (either from the Thunderstore page of the mod or the release on GitHub), then extracting the zip file inside the ```BepInEx/plugins``` folder after starting the game once with BepInEx installed.

Configuration of the mod can be done after starting up the game with the mod enabled through the Config editor (Thunderstore Mod Manager or r2modman) or by opening the ```command.localFlashlight.cfg``` file made by the mod in the in ```BepInEx/config```.
Alternatively, you could use [LethalConfig](https://thunderstore.io/c/lethal-company/p/AinaVT/LethalConfig/) if you want to change some config values while in-game.

## Mod configs

-Intensity, range and spot angle of the light can be configured

-The battery's max time and recharge multiplier can also be configured

-The HUD has six different styles (Low battery warning, Bar, Percentage, Circular Bar, Vertical Bar, All)

-Custom colors can be set for the light and HUD

-The light's shadows can be enabled or disabled

-Option to prioritize in-game flashlights before the local flashlight

-Multiple recharge options for the flashlight, each with their own upsides and downsides

## Indicator config styles

-Bar

![bar style](https://github.com/ever39/LocalFlashlight/raw/main/assets/readmeAssets/barStyle.gif)

-Percentage

![percent style](https://github.com/ever39/LocalFlashlight/raw/main/assets/readmeAssets/percentageStyle.gif)

-Circular bar

![circular style](https://github.com/ever39/LocalFlashlight/raw/main/assets/readmeAssets/circularBar.gif)

-Vertical bar

![vertical style](https://github.com/ever39/LocalFlashlight/raw/main/assets/readmeAssets/verticalBar.gif)

-Full info

![full style](https://github.com/ever39/LocalFlashlight/raw/main/assets/readmeAssets/fullStyle.gif)

-Low battery warning (can be disabled in config)

![low battery warning](https://github.com/ever39/LocalFlashlight/raw/main/assets/readmeAssets/disabledWarning.png)

##

>[!NOTE]
>The mod's keybinds are now in ```Settings -> Change keybinds```, as I've added LCInputUtils as a dependency to make setting custom keybinds easier.


>[!IMPORTANT]
>This mod is CLIENT-SIDED, so everyone who wants to use these features must have the mod installed.
>This also means that other players will not be able to see your light, nor you will be able to see other players' lights if they use the mod (hence the mod being called **Local**Flashlight).
>I am planning to add a way to make it so it is a sort of "crew upgrade" for people with the mod installed to have their own light instead of just having it enabled by default (assuming the host also has the mod installed).

If you found a bug, then feel free to open a new issue on the [Github page](https://github.com/ever39/LocalFlashlight/issues) to report it.
