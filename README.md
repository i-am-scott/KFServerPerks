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

###CONNECTING:
[KFSERVER] Request to open a connection with ENetID.ID_Open (1)
[PSERVER]  Respond with ENetID.ID_RequestPassword (2)
[KFSERVER] Request with ENetID.ID_HeresPassword (3) followed by the password in plaintext.
[PSERVER]  Response with ENetID.ID_PasswordCorrect (5) if the password is correct, respond with ENetID.ID_ConnectionClosed(4) otherwise.

From here whitelist the IP + Port to allow them to use the other commands.

###KEEPALIVE:
I'm not sure why this is even needed, maybe for Winsocs to work?
[PSERVER] Send ENetID.ID_KeepAlive
[KFSERVER] Respond ENetID.ID_KeepAlive

So, KF Server will keep responding to this instantly, probably best to keep sending this every x seconds (config) just incase this
is required by the UE UdpLink class.

###RECEIVED ENetID.ID_NewPlayer(6)
Signals that a new player is to be created.
[steamid64]\*[steamname].

###SEND ENetID.ID_NewPlayer(6)
Sends new player data. Not sure why this is needed. Maybe an unfinished feature or is sent to confirm that we got the new player request.
Seems to return [playerindex]|[steamid64] where playerindex will be the id for this perk object. IE an AI key from the database.

###Playerdata Format
[ID]|CurrentPerk:stat,stat[,...],modelId[,...customperks]
