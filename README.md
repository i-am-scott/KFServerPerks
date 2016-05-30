# KFServerPerks
######A thirdparty application for the 'Per Server Stats' Mutator for killing floor by Marco.
######[Perk Mutator](http://forums.tripwireinteractive.com/showthread.php?t=36065)

This aims to be a full replacement for the C++ Server executable. Enabling you to save your data as json, xml, SQL along with a more clearer
format.
________________________________________________________________________________________________

##Message structure:
First byte is the command (request connection, request password, new user, load user, etc.)
The rest is the data, some commands will not send this information.

Firstly, since they're using WinSocks (wtf) a lot of the code expects a connection to be kept open. Instead we're just going
to pretend that we're open. We're really just listening for anything and whitelisting anything that gave the correct password.

Simple overview of how it connects and sends data:

####CONNECTING:
[KFSERVER] Request to open a connection with ENetID.ID_Open (1)
[PSERVER]  Respond with ENetID.ID_RequestPassword (2)
[KFSERVER] Request with ENetID.ID_HeresPassword (3) followed by the password in plaintext.
[PSERVER]  Response with ENetID.ID_PasswordCorrect (5) if the password is correct, respond with ENetID.ID_ConnectionClosed(4) otherwise.

From here whitelist the IP + Port to allow them to use the other commands.

####KEEPALIVE ENetID.ID_KeepAlive(10):
I'm not sure why this is even needed, maybe for Winsocs to work?
[PSERVER] Send ENetID.ID_KeepAlive
[KFSERVER] Respond ENetID.ID_KeepAlive

So, KF Server will keep responding to this instantly, probably best to keep sending this every x seconds (config) just incase this
is required by the UE UdpLink class.

####RECEIVED ENetID.ID_NewPlayer(6)
Signals that a new player is to be created or fetched.
[steamid64]\*[steamname].

####SEND ENetID.ID_NewPlayer(6)
Send back the players playerindex and steamid64, if the player doesn't exist then send back a unique playerindex along with the steamid.
the playerindex is the id that stats for this player will use.

Seems to return [playerindex]|[steamid64] where playerindex will be the id for this perk object. IE an AI field from the database.

####SEND ENetID.ID_NewPlayer(6)

####RECEIVED ID_UpdatePlayer(9)
Signals that a players stats need saving. This follows the Playerdata format with the char 10 appended.
This is sent in chunks of 512chars with char 10 signifying the end of the stream.

####Playerdata Format
[playerindex]|CurrentPerk:stat,stat[,...],modelId[,...customperks]
