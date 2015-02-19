using System;
using System.Linq;
using System.Threading;
using HandshakeEmulator.DataStructures;
using SITCAB.RTDS;


namespace HandshakeEmulator
{
    public class Listener
    {
        private readonly Configuration _config;
        private readonly string _equip;
        private readonly string _root;

        public Thread Thread;
        public event EventHandler EventLog;
        public event EventHandler EventComplete;
        
        private readonly string[] _cmdReqTags;
        private RTDSResult[] _cmdReqTagsValues;
        private readonly string[] _stsReqTags;
        private readonly object[] _stsReqTagsValues;
        private readonly string[] _stsRespTags;
        private object[] _stsRespTagsValues;

        public enum ChannelStatusId 
        {
            Idle = 1,
            Busy = 2,
            Done = 3,
            Error = 4
        };

        public enum ActionTypeId
        {
            InProgress = 9,
            Completed = 12
        };

        public Listener(Configuration config, Equip equip)
        {
            _root = config.UnitName + "\\" + equip.Prefix;
            _config = config;
            _equip = equip.Name;

            _cmdReqTags = config.Parameters.CmdReq.ConvertAll(param => _root + param.Suffix).ToArray();
            _stsReqTags = config.Parameters.StsReq.ConvertAll(param => _root + param.Suffix).ToArray();
            _stsRespTags = config.Parameters.StsResp.ConvertAll(param => _root + param.Suffix).ToArray();

            _stsReqTagsValues = new object[_stsReqTags.Length];
            _stsRespTagsValues = new object[_stsRespTags.Length];
        }

        public void Start(string choice, int equipId=0, string transId=null, string command=null)
        {
            lock (this)
            {
                if (Thread != null && Thread.IsAlive)
                    return;

                if (choice.Equals("HS"))
                {
                    Thread = new Thread(HandshakeSequence);
                    Thread.Start();
                }
                else if (choice.Equals("HSR"))
                {
                    // We are using a lambda to pass two arguments to the newly created thread
                    Thread = new Thread(() => EquipRequestHandshake(equipId, transId, command));
                    Thread.Start();
                }
            }
        }

        public void Stop()
        {
            lock (this)
            {
                if (Thread == null || !Thread.IsAlive)
                    return;

                if (!Thread.Join(10000))
                    Thread.Abort();
            }
        }

        private object WaitForValue(string tag, params object[] inputs)
        {
            DateTime startTime = DateTime.Now;

            // Waiting for 10 seconds before aborting
            while (DateTime.Now.Subtract(startTime).Seconds < 10000)
            {
                try
                {
                    RTDSResult dataRead = RTDS.ReadThrough(tag);

                    if (dataRead != null
                        && dataRead.IsGood
                        && inputs.Contains(dataRead.Values[0]))
                    {
                        return dataRead.Values[0];
                    }
                }
                catch (Exception e)
                {
                    if (EventLog != null)
                        EventLog(new Log(_equip, "CRITICAL", "Exception in waitForValue: " + e.Message), null);
                }
            }

            return null;
        }

        private void HandshakeSequence()
        {
            try
            {
                if (CiRequestHandshake())
                {
                    Thread.Sleep(2000);
                    EquipRequestHandshake();
                }
            }
            catch (Exception e)
            {
                if (EventLog != null)
                    EventLog(new Log(_equip, "CRITICAL", "Exception in handshakeSequence: " + e.Message), null);
            }
        }

        private bool CiRequestHandshake()
        {
            string channelStatusSts = _root + _config.StsRespChannelStatus;
            string channelStatusCmd = _root + _config.CmdReqChannelStatus;
            short? status = null;

            try
            {
                _cmdReqTagsValues = RTDS.ReadThrough(_cmdReqTags);

                if (!_cmdReqTagsValues[0].IsGood
                    && _cmdReqTagsValues[0].Values == null)
                    throw new Exception("Unable to read CI request tags");

                RTDS.WriteThrough(channelStatusSts, (short)ChannelStatusId.Busy);

                // Important
                _stsRespTagsValues[0] = Convert.ToString(-1);
                _stsRespTagsValues[1] = _cmdReqTagsValues[2].Values[0].ToString();
                _stsRespTagsValues[2] = _cmdReqTagsValues[1].Values[0].ToString();

                for (int i = 4 ; i < _cmdReqTagsValues.Length ; i++)
                    _stsRespTagsValues[i - 1] = _cmdReqTagsValues[i].Values[0].ToString();

                if (EventLog != null)
                {
                    string logMsg = string.Format("Equipment {0} has received a HS request: type {1}, transaction {2}",
                                                _cmdReqTagsValues[2].Values[0],
                                                _cmdReqTagsValues[0].Values[0],
                                                _cmdReqTagsValues[1].Values[0]);

                    EventLog(new Log(_equip, "INFO", logMsg), null);

                    RTDS.WriteThrough(_stsRespTags, _stsRespTagsValues);
                    RTDS.WriteThrough(channelStatusSts, (short)ChannelStatusId.Done);

                    // Listen for a reply
                    status = (short)WaitForValue(channelStatusCmd, (short)ChannelStatusId.Idle, (short)ChannelStatusId.Error);

                    EventLog(new Log(_equip, "INFO", "First part of handshake sequence completed"), null);
                }
            }
            catch (Exception e)
            {
                if (EventLog != null)
                    EventLog(new Log(_equip, "CRITICAL", "Exception during the first part of handshake sequence: " + e.Message), null);

                _cmdReqTagsValues = null;
                _stsRespTagsValues = null;
            }
            finally
            {
                // Even if the communication was not successful,
                // the channel status is reset to "Idle"
                RTDS.WriteThrough(channelStatusSts, (short)ChannelStatusId.Idle);
            }

            return status == (short)ChannelStatusId.Idle; 
        }

        /*
         *  Second Phase of HS Sequence
         */
        public void EquipRequestHandshake(int equipId=0, string transId=null, string type=null)
        {
            string channelStatusSts = _root + _config.StsReqChannelStatus;

            try
            {
                RTDS.WriteThrough(channelStatusSts, (short)ChannelStatusId.Busy);

                if (equipId != 0 && transId != null && type != null)
                {
                    _stsReqTagsValues[0] = type;
                    _stsReqTagsValues[1] = transId.Equals("RANDOM") ? new Random().Next().ToString() : transId;
                    _stsReqTagsValues[2] = equipId.ToString();
                    _stsReqTagsValues[10] = "";
                    _stsReqTagsValues[11] = "";
                    for (int i = 12; i < _stsReqTagsValues.Length; i++)
                        _stsReqTagsValues[i] = Convert.ToString(0);
                }
                else
                {
                    for (int i = 0; i < _cmdReqTags.Length; i++)
                        _stsReqTagsValues[i] = _cmdReqTagsValues[i].Values[0].ToString();
                }

                // If we do not receive a good answer there is no need to continue
                // and we return to the main thread
                if (_sendEquipmentRequest((short)ActionTypeId.InProgress) != (short)ChannelStatusId.Done)
                {
                    if (EventLog != null)
                        EventLog(new Log(_equip, "ERROR", "Aborting transmission"), null);
                    return;
                }

                // Simulating passage between "In progress" and "Completed"
                Thread.Sleep(1000);
                RTDS.WriteThrough(channelStatusSts, (short)ChannelStatusId.Idle);
                Thread.Sleep(2000);
                RTDS.WriteThrough(channelStatusSts, (short)ChannelStatusId.Busy);

                _sendEquipmentRequest((short)ActionTypeId.Completed);

            }
            catch (Exception e)
            {
                if (EventLog != null)
                    EventLog(new Log(_equip, "CRITICAL", "Exception during the second part of the handshake sequence: " + e.Message), null);
            }
            finally
            {
                // Whatever response it's received the emulator writes "Idle" as channel status
                RTDS.WriteThrough(channelStatusSts, (short)ChannelStatusId.Idle);

                if (EventComplete != null)
                    EventComplete(_equip, null);
            }
        }

        private short? _sendEquipmentRequest(short type)
        {
            if (EventLog != null)
            {
                EventLog(new Log(_equip, "INFO", string.Format("Equipment {0} is initializing a request: command {1}, type {2}, transaction {3}",
                                                                _stsReqTagsValues[2],
                                                                _stsReqTagsValues[0],
                                                                Convert.ToString(type),
                                                                _stsReqTagsValues[1])), null);
            }

            // This has to be set up as "In Progress"
            _stsReqTagsValues[3] = Convert.ToString(type);

            // Setting the timestamps
            _stsReqTagsValues[4] = DateTime.Now.Year.ToString("yy");
            _stsReqTagsValues[5] = DateTime.Now.Month.ToString();
            _stsReqTagsValues[6] = DateTime.Now.Day.ToString();
            _stsReqTagsValues[7] = DateTime.Now.Hour.ToString();
            _stsReqTagsValues[8] = DateTime.Now.Minute.ToString();
            _stsReqTagsValues[9] = DateTime.Now.Second.ToString();

            if (EventLog != null)
            {
                EventLog(new Log(_equip, "INFO", string.Format("Equipment {0} sending a request: command {1}, type {2}, transaction {3}",
                                                                _stsReqTagsValues[2],
                                                                _stsReqTagsValues[0],
                                                                Convert.ToString((short) ActionTypeId.InProgress),
                                                                _stsReqTagsValues[1])), null);
            }

            RTDS.WriteThrough(_stsReqTags, _stsReqTagsValues);
            Thread.Sleep(1000);
            RTDS.WriteThrough(_root + _config.StsReqChannelStatus, (short)ChannelStatusId.Done);

            short? status = (short?)WaitForValue(_root + _config.CmdRespChannelStatus, (short)ChannelStatusId.Done, (short)ChannelStatusId.Error);

            // Deciding what to do according to the answer from C&I
            // Aborting if time out or error
            if (EventLog != null)
            {
                switch (status)
                {
                    case null:
                        EventLog(new Log(_equip, "ERROR", "Equipment handshake timed out"), null);
                        break;
                    case (short) ChannelStatusId.Error:
                        EventLog(new Log(_equip, "ERROR", "Equipment received error response"), null);
                        break;
                    case (short) ChannelStatusId.Done:
                        EventLog(new Log(_equip, "INFO", "Acknowledgment received"), null);
                        break;
                    default:
                        EventLog(new Log(_equip, "ERROR", "Unkwnown status"), null);
                        break;
                }
            }

            return status;
        }
    }
}
