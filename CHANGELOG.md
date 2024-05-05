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

# **V1.2.21**
- Fixed light not turning on anymore (with the "Prioritize in-game flashlights" config set to true) if the player dies until they pick up and drop another flashlight
- Changed some position and rotation values of the light

# **V1.2.2**
- Added config option to prioritize flashlights in the player's inventory until their battery is depleted
- Added a third sound option (InGameFlashlight)
- Added more logs
- Changed some config descriptions to be more clear about what they do (i recommend deleting the config file as it might cause some issues if the new configs overlap the old ones)

# **V1.2.1**
- Added LCInputUtils as dependency for easier keybind configuration
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
