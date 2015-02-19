using System.Collections.Generic;


namespace HandshakeEmulator.DataStructures
{
    public class Parameters
    {
        public List<Param> CmdReq;
        public List<Param> StsResp;
        public List<Param> CmdResp;
        public List<Param> StsReq;
        public List<Param> EquipStatusParameters;
        public List<Param> DowntimesParameters;
    }
}
