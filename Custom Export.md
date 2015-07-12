# Custom Export

**This is still under development.**

In the version 2.0.0.10 you can can export your account's to a text file using the variables bellow. To export lists, you need insert the data you wanna export inside their tags (See the example at the bottom of the document).

# Variables
#### Account
* ``%USERNAME%`` - Username
* ``%PASSWORD%`` - Password
* ``%SUMMONERNAME%`` - Summoner Name
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
* ``%PUCHASEDATE%`` - Date and Time when the champion was purchased

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
