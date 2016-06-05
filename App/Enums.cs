using System;

namespace KFServerPerks
{

    [AttributeUsage(System.AttributeTargets.Method)]
    public class ENetCommandType : Attribute
    {
        public string cmdType;
        public ENetID cmdInt;

        public ENetCommandType(string name, ENetID cmd)
        {
            cmdType = name;
            cmdInt = cmd;
        }
    }

    public enum ENetID
    {
        ID_None = 0,
        ID_Open,
        ID_RequestPassword,
        ID_HeresPassword,
        ID_ConnectionClosed,
        ID_PasswordCorrect,
        ID_NewPlayer,
        ID_BadNewPlayer,
        ID_SendPlayerData,
        ID_UpdatePlayer,
        ID_KeepAlive
    }

}
