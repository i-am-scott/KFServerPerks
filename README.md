# KFServerPerks
##### A replacement server for the 'Per Server Stats' Mutator on Killing Floor by Marco found [here](https://forums.tripwireinteractive.com/index.php?threads/mut-per-server-stats.36898/)

With the original version using flatfiles it makes using it on remote servers (such as websites) a pain. Therefore I remade the server to use MySQL.
Supports custom perks too. Please create a new MySQL user and not use root. Run sqlstructure.sql on your MySQL database to create the table. 

Create a config.json (or let it create an empty one for you) with these settings:

##### config.json
```json
{
    "ServerPort": 6000,
    "ServerPassword": "mutatorPassword",
    "MySQLHost": "127.0.0.1",
    "MySQLUsername": "databaseUsername",
    "MySQLPasswword": "databasePassword",
    "MySQLDatabase": "killingfloor",
    "MySQLPerksTable": "perks",
	"MySQLPort": 3306,
	"AllowAll": false,
    "Whitelist": [
        "127.0.0.1"
    ]
}
```