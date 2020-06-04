fx_version 'bodacious'
games { 'gta5' }

-- Source: https://github.com/michaelcerne/Spotlight

author 'Michael Cerne' -- (FiveTwelve)
description 'Spotlight is a Fivem/FivePD modification that allows players to utilize fully controllable spotlights on their vehicles.'
version '1.1.0'

client_scripts {
    'Spotlight_client.net.dll'
}

server_scripts {
    'Spotlight.net.dll'
}

--
-- Config
-- Only edit values under this line (script must be restarted to apply changes)
--

emergency_only 'true' -- Only allow spotlight usage in emergency vehicles ('true'/'false')
remote_control 'true' -- Allow players to aim the spotlight in the last vehicle they entered ('true'/'false')
brightness_level '30' -- Brightness of the spotlight once it's "warmed up" ('{positive number}')
aim_range_left '90' -- How far left from center the spotlight can be aimed in degrees ('{positive number}')
aim_range_right '30' -- How far right from center the spotlight can be aimed in degrees ('{positive number}')