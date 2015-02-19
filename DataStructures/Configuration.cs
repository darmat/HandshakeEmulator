using System.Collections.Generic;


namespace HandshakeEmulator.DataStructures
{
    public class Configuration
    {
        // Lists and aggregates
        public List<Equip> EquipmentList;
        public List<Command> CommandList;
        public Parameters Parameters;

        // Channel status
        public string CmdReqChannelStatus;
        public string StsRespChannelStatus;
        public string StsReqChannelStatus;
        public string CmdRespChannelStatus;

        // Various
        public string UnitName;
        public string ReqErrorCodes;
    }
}
