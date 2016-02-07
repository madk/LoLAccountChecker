# Custom Export

# Variables
#### Account
* ``%USERNAME%`` - Username
* ``%PASSWORD%`` - Password
* ``%SUMMONERNAME%`` - Summoner Name
* ``%SUMMONERID%`` - Summoner ID
* ``%LEVEL%`` - Level
* ``%RANK%`` - Solo Queue Rank
* ``%EMAILSTATUS%`` - Email status
* ``%RP%`` - Riot Points
* ``%IP%`` - Influence Points
* ``%CHAMPIONS%`` - Number of champions owned
* ``%SKINS%`` - Number of skins owned
* ``%RUNEPAGES%`` - Number of rune pages owned
* ``%REFUNDS%`` - Number of Refund tokens available
* ``%REGION%`` - Region of the Account
* ``%LASTPLAY%`` - Date and Time of the last match played on the account
* ``%CHECKTIME%`` - Date and Time of the check

#### Champion List
Tag: ``[%CHAMPIONLIST%] [/%CHAMPIONLIST%]``
* ``%ID%`` - Champion ID
* ``%NAME%`` - Champion Name
* ``%PURCHASEDATE%`` - Date and Time when the champion was purchased

#### Skin List
Tag: ``[%SKINLIST%] [/%SKINLIST%]``
* ``%ID%`` - Skin ID
* ``%CHAMPION%`` - Champion Name
* ``%NAME%`` - Name of the Skin

#### Rune List 

Tag: ``[%RUNELIST%] [/%RUNELIST%]``

* ``%NAME%`` - Name
* ``%DESCRIPTION%`` - Description
* ``%TIER%`` - Tier
* ``%QUANTITY%`` - Quantity


# Example

```
Summoner: %SUMMONERNAME%
Level: %LEVEL%
Region: %REGION%
Champions: %CHAMPIONS%
Skins: %SKINS%
Rune Pages: %RUNEPAGES%

Champions List:
[%CHAMPIONLIST%]
    > %NAME% - %ID% 
[/%CHAMPIONLIST%]

Skins owned:
[%SKINLIST%]
    > %NAME - %ID%
[/%SKINLIST%]
```
