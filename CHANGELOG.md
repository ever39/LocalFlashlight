# **V1.4.12**
-just changed the version number, as 11 is higher than 2 (i did not take that into account)

# **V1.4.2**
- Fixed networking not working at all, alongside the two changes.
- Also added ```TerminalApi``` as a dependency. It will be used in future updates.

# **V1.4.11**
- Fixed the flickering when the battery ran out not occuring due to an oversight in the sound playing part of the mod, where it would forget to check for networking before calling the server rpc

# **V1.4.1**
- really quick hotfix because i messed up while trying to clean up part of the code related to the Time recharge method (forgot to cap the battery time so it would go over the max battery time for other recharge options)

# **V1.4**
- Added ```LethalConfig``` as a dependency to make setting configs easier for the player (and also other things besides that)
- Changed color configs to a hex code instead of three separate configs for red, green and blue values (for the flashlight and the indicator)
- Fixed pretty much all the bugs related to the ```Prioritize in-game flashlights``` setting
- Reworked the ```Apparatice``` recharge method to function based on the power inside the facility (and renamed it accordingly to ```FacilityPowered```)
- Made some adjustments to the ```Dynamo``` recharge method (higher total recharge power, though it requires a little time to ramp up)
- Made the sound played when recharging the flashlight using the ```Dynamo``` recharge method ramp up and back down instead of abruptly playing and stopping
- Added option to adjust the in-game's natural "dark area light" intensity percentage to make seeing in dark areas dependent on the flashlight (set to 50% of the normal ambient light intensity by default)
- Added setting to choose if the flashlight should be fully charged when you enter orbit (default: true)
- Fixed some scraps held by player blocking the light if the ```Enable shadows``` config was enabled
- The flashlight no longer needs to be turned on to change its position and rotation (from left to right and vice-versa)
- Changed some existing sounds in some of the sound options
- Fixed vertical rotation inconsistencies when switching flashlight positions for the first time after joining a game
- Reworked how sounds are loaded to account for other players using different sound options
- Removed config related to enemies hearing sound, and with this change some sounds were changed to no longer be heard by enemies
- Added a way for all players to see eachother's lights using networking. This is still something that's worked on, so it's still an experimental feature.
- Overall code improvements, which should solve some issues with lag (if there were any that I wasn't aware of).
- Made some configs even clearer, and added a few caps to their values

**! make sure to remake the config file (deleting and generating it again by opening the game), as i'm pretty sure some configs will no longer apply any changes because they've either had their name changed or have been outright deleted.**

# **V1.3.1**
- Added flashlight dimming (with config to set at what battery percentage the light should stop dimming)
- Added flashlight flickering when the battery runs out or when ghost girl changes behavior state and flickers every other light (default is set to true)
- Hopefully fixed some bugs related to the "Prioritize flashlights in player inventory" config
- Changed the way the mod's assetbundle is loaded to prevent using up RAM while loading it
- Added 5 frames of leniency before the mod sends an error message if it has errors while trying to find the local player controller

# **V1.3**
- Added config option for various battery recharge methods, each with their upsides and downsides (and some with configs).
- Added full recharge sounds to the flashlight when using the time recharge config
- Added config option for sounds to be heard by enemies (default is set to true)
- Cleaned up code for optimization
- Made most logs debug logs to avoid log spam
- Made it so the error caused by the mod not finding the local player only gets sent once instead of every time per frame
- The HUD no longer shows up if the light doesn't work
- Made ```InGameFlashlight``` the default sound option for the mod to use
- Made configs look cleaner (at least when looking at them with LethalConfig)
- Changed some names for the sound options
- Added keybind to change light position from left to right, and vice versa to prevent the light from being covered by any items currently held (default key: H)
- Added keybind to recharge the flashlight (used for Shake and Dynamo recharge methods) (default key: Q)

# **V1.2.21**
- Fixed light not turning on anymore (with the "Prioritize in-game flashlights" config set to true) if the player dies until they pick up and drop another flashlight
- Changed some position and rotation values of the light

# **V1.2.2**
- Added config option to prioritize flashlights in the player's inventory until their battery is depleted
- Added a third sound option (InGameFlashlight)
- Added more logs
- Changed some config descriptions to be more clear about what they do (i recommend deleting the config file as it might cause some issues if the new configs overlap the old ones)

# **V1.2.1**
- Added ```LCInputUtils``` as dependency for easier keybind configuration
- Added sound options (currently only two of them, Default and ActualFlashlight)
- Updated README file to add installation and configuration, and a small note about the new keybind configurations

# **V1.2**
- Added separate config options for light color and HUD color
- Some settings can now be changed without having to rejoin a lobby (specifically the ones that aren't important, like colors or intensity)
- Added configs for indicator position on the screen (IndicatorPositionX and IndicatorPositionY)
- Added config for battery text options (Percentage, AccuratePercentage, Time, All)
- Added an experimental config to enable light shadows
- Fixed indicator not fading in when using the flashlight at abnormally high battery times
- Fixed bugs related to player death not turning off the flashlight, which resulted in battery loss when dead
- More overall code changes
- Added more logs in case something goes wrong
- Updated README

# **V1.1**
- Made changes to the code so set configs can apply when entering a lobby
- Added two HUD styles (CircularBar, VerticalBar)
- Added some debug logs (LightController made, light toggle)
- Fixed flashlight toggling when pressing the toggle key in the pause menu
- Added battery regen cooldown config
- Volume now updates immediately after config change
