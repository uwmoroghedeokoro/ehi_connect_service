using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ehi_connect_service
{
    public class utility
    {
        public SqlConnection _sqlcon;
        public utility()
        {
            _sqlcon = new SqlConnection("Data Source=localhost\\mssqlserver14;initial catalog=irat_asterisk;Integrated Security=True;");
            _sqlcon = new SqlConnection("Data Source=localhost\\SQLEXPRESS; initial catalog=ir_connect;user id=ir_connect;Password=7mmT@XAy;Integrated Security=False;");
            try
            {
                _sqlcon.Open();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message.ToString());
            }
        }

        public void consumeResponse(string[] pars)
        {
            try
            {
                foreach (string data in pars)
                {
                    if (Regex.Match(data, "Event: AgentCalled", RegexOptions.IgnoreCase).Success
                         || Regex.Match(data, "Event: AgentConnect", RegexOptions.IgnoreCase).Success
                         || Regex.Match(data, "Event: AgentCalled", RegexOptions.IgnoreCase).Success
                          || Regex.Match(data, "Event: BridgeEnter", RegexOptions.IgnoreCase).Success
                           || Regex.Match(data, "Event: BridgeLeave", RegexOptions.IgnoreCase).Success
                         || Regex.Match(data, "Event: AgentRingNoAnswer", RegexOptions.IgnoreCase).Success
                         || Regex.Match(data, "Event: AgentComplete", RegexOptions.IgnoreCase).Success
                         || Regex.Match(data, "Event: QueueCallerAbandon", RegexOptions.IgnoreCase).Success
                         || Regex.Match(data, "Event: ExtensionStatus", RegexOptions.IgnoreCase).Success
                         || Regex.Match(data, "Event: DialBegin", RegexOptions.IgnoreCase).Success
                          || Regex.Match(data, "Event: QueueMemberStatus", RegexOptions.IgnoreCase).Success
                           || Regex.Match(data, "Event: QueueMemberAdded", RegexOptions.IgnoreCase).Success
                            || Regex.Match(data, "Event: QueueMemberRemoved", RegexOptions.IgnoreCase).Success
                              || Regex.Match(data, "Event: CDR", RegexOptions.IgnoreCase).Success
                         )
                    {
                        // log to agent_log
                        log_to_queue_log(data);
                    }
                    else if (Regex.Match(data, "Event: QueueCallerJoin", RegexOptions.IgnoreCase).Success || Regex.Match(data, "Event: QueueCallerLeave", RegexOptions.IgnoreCase).Success)
                    {
                        // log memb join/leave event
                        log_to_queue_status(data);
                    }
                }

            }
            catch (Exception ex)
            {

            }
        }

        private void log_to_queue_status(string data)
        {
            try
            {
                Hashtable vals = new Hashtable();

                String[] pars = data.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                foreach (string entry in pars)
                {
                    if (entry != "") //not blank data
                    {
                        string[] dataValue = entry.Split(new char[] { ':' });
                        vals.Add(dataValue[0], dataValue[1].Trim());
                    }
                }


                if (Regex.Match(data, "Event: QueueCallerJoin", RegexOptions.IgnoreCase).Success)
                {
                    simpleSave("tblQueue_Status", new string[] { "queue_name", "caller_id", "position", "linkedID" }, new string[] { (string)vals["Queue"], (string)vals["CallerIDNum"], (string)vals["Position"], (string)vals["Linkedid"] });
                    simpleSave("tblQueue_log", new string[] { "event", "caller_id", "queue_name", "queue_position", "channel_id" }, new string[] { (string)vals["Event"], (string)vals["CallerIDNum"], (string)vals["Queue"], (string)vals["Position"], (string)vals["Channel"].ToString() });
                }
                else if (Regex.Match(data, "Event: QueueCallerLeave", RegexOptions.IgnoreCase).Success)
                {
                    simpleSQL("delete from tblQueue_Status where queue_name='" + (string)vals["Queue"] + "' and linkedID='" + (string)vals["Linkedid"] + "'");
                }

            }
            catch (Exception ex)
            {

            }
        }

        private void log_to_queue_log(string data)
        {
            try
            {
                Hashtable vals = new Hashtable();

                String[] pars = data.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                foreach (string entry in pars)
                {
                    if (entry != "") //not blank data
                    {
                        string[] dataValue = entry.Split(new char[] { ':' });
                        vals.Add(dataValue[0], dataValue[1].Trim());
                    }
                }


                if (Regex.Match(data, "Event: AgentCalled", RegexOptions.IgnoreCase).Success)
                {
                    simpleSave("tblQueue_log", new string[] { "event", "caller_id", "agent_caller_id", "queue_name", "linkedid", "channel_id" }, new string[] { (string)vals["Event"], (string)vals["CallerIDNum"], (string)vals["MemberName"].ToString().Substring(4), (string)vals["Queue"], (string)vals["linkedid"], (string)vals["Channel"].ToString() });
                }
                else if (Regex.Match(data, "Event: AgentConnect", RegexOptions.IgnoreCase).Success)
                {
                    simpleSave("tblQueue_log", new string[] { "event", "caller_id", "agent_caller_id", "queue_name", "ring_time", "hold_time", "linkedid", "channel_id" }, new string[] { (string)vals["Event"], (string)vals["CallerIDNum"], (string)vals["MemberName"].ToString().Substring(4), (string)vals["Queue"], (string)vals["RingTime"], (string)vals["HoldTime"], (string)vals["linkedid"], (string)vals["Channel"].ToString() });
                    startMonitor(vals);
                }
                else if (Regex.Match(data, "Event: BridgeEnter", RegexOptions.IgnoreCase).Success)
                {
                    string exten = (string)vals["Channel"].ToString().Substring(4);
                    exten = exten.Substring(0, 4);
                    if (Outbound_record_enabled(exten))
                    {
                        startMonitorOut(vals);
                    }
                    //simpleSave("tblQueue_log", new string[] { "event", "caller_id", "agent_caller_id", "queue_name", "ring_time", "hold_time", "linkedid" }, new string[] { (string)vals["Event"], (string)vals["Exten"], (string)vals["Channel"].ToString().Substring(4), (string)vals["Queue"], (string)vals["RingTime"], (string)vals["HoldTime"], (string)vals["linkedid"] });
                    // startMonitorOut(vals);
                }
                else if (Regex.Match(data, "Event: BridgeLeave", RegexOptions.IgnoreCase).Success)
                {
                    string exten = (string)vals["Channel"].ToString().Substring(4);
                    exten = exten.Substring(0, 4);
                    if (Outbound_record_enabled(exten))
                    {
                        stopMonitorOut(vals);
                    }
                }
                else if (Regex.Match(data, "Event: AgentRingNoAnswer", RegexOptions.IgnoreCase).Success)
                {
                    simpleSave("tblQueue_log", new string[] { "event", "caller_id", "agent_caller_id", "queue_name", "ring_time", "linkedid", "channel_id" }, new string[] { (string)vals["Event"], (string)vals["CallerIDNum"], (string)vals["MemberName"].ToString().Substring(4), (string)vals["Queue"], (string)vals["RingTime"], (string)vals["linkedid"], (string)vals["Channel"].ToString() });
                }
                else if (Regex.Match(data, "Event: AgentComplete", RegexOptions.IgnoreCase).Success)
                {
                    //     simpleSQL("update tblUsers set lastcall='" + DateTime.Now.ToString() + "' where uExt='" + (string)vals["MemberName"].ToString().Substring(4) + "'");
                    simpleSave("tblQueue_log", new string[] { "event", "caller_id", "agent_caller_id", "queue_name", "hold_time", "talk_time", "linkedid", "channel_id" }, new string[] { (string)vals["Event"], (string)vals["CallerIDNum"], (string)vals["MemberName"].ToString().Substring(4), (string)vals["Queue"], (string)vals["HoldTime"], (string)vals["TalkTime"], (string)vals["linkedid"], (string)vals["Channel"].ToString() });
                    stopMonitor(vals);
                }
                else if (Regex.Match(data, "Event: QueueCallerAbandon", RegexOptions.IgnoreCase).Success)
                {
                    simpleSave("tblQueue_log", new string[] { "event", "caller_id", "queue_position", "queue_name", "hold_time", "queue_orig_position", "linkedid", "channel_id" }, new string[] { (string)vals["Event"], (string)vals["CallerIDNum"], (string)vals["Position"], (string)vals["Queue"], (string)vals["HoldTime"], (string)vals["OriginalPosition"], (string)vals["linkedid"], (string)vals["Channel"].ToString() });
                }
                else if (Regex.Match(data, "Event: ExtensionStatus", RegexOptions.IgnoreCase).Success)
                    simpleSQL("update tblUsers set eStatus='" + (string)vals["StatusText"] + "' where uExt='" + (string)vals["Exten"] + "'");
                else if (Regex.Match(data, "Event: DialBegin", RegexOptions.IgnoreCase).Success)
                {
                    simpleSave("tblCall_Logs", new string[] { "call_route", "in_caller_id", "out_caller_id" }, new string[] { (string)vals["DestChannel"], (string)vals["Channel"], (string)vals["DestExten"] });
                }
                else if (Regex.Match(data, "Event: CDR", RegexOptions.IgnoreCase).Success)
                {
                    if ((string)vals["Destination"].ToString() != "s")
                        simpleSave("tblCall_Logs", new string[] { "call_route", "in_caller_id", "out_caller_id", "talk_time", "disposition", "event" }, new string[] { (string)vals["Source"], (string)vals["Channel"].ToString().Substring(4).Substring(0, 4), (string)vals["Destination"], (string)vals["Duration"], (string)vals["Disposition"], "CDR" });
                }
                else if (Regex.Match(data, "Event: QueueMemberStatus", RegexOptions.IgnoreCase).Success)
                {
                    bool incall = false;
                    if ((string)(vals["InCall"]) == "1")
                        incall = true;

                    TimeSpan timeSpan = TimeSpan.FromSeconds(Convert.ToDouble(vals["LastCall"]));
                    DateTime odate = new DateTime(1970, 1, 1, 0, 0, 0);
                    DateTime datetime = odate.Add(timeSpan);
                    datetime = datetime.ToUniversalTime();

                    simpleSQL("update tblUsers set qStatus=" + (string)vals["Status"] + ",InCall='" + incall + "' where uExt='" + (string)vals["MemberName"].ToString().Substring(4) + "'");
                }
                else if (Regex.Match(data, "Event: QueueMemberAdded", RegexOptions.IgnoreCase).Success)
                {
                    simpleSave("tbl_agent_loginout", new string[] { "agent_ext", "queue_name", "action" }, new string[] { (string)vals["MemberName"], (string)vals["Queue"], "Login" });
                    simpleSQL("EXEC updateQueueMember_Status '" + (string)vals["MemberName"] + "','" + (string)vals["Queue"] + "','true'");
                }
                else if (Regex.Match(data, "Event: QueueMemberRemoved", RegexOptions.IgnoreCase).Success)
                {
                    simpleSave("tbl_agent_loginout", new string[] { "agent_ext", "queue_name", "action" }, new string[] { (string)vals["MemberName"], (string)vals["Queue"], "Logoff" });
                    simpleSQL("EXEC updateQueueMember_Status '" + (string)vals["MemberName"] + "','" + (string)vals["Queue"] + "','false'");
                }
            }
            catch (Exception ex)
            {

            }
        }

        private bool Outbound_record_enabled(string exten)
        {
            bool rec = false;
            using (SqlConnection connection = new SqlConnection("Data Source=localhost\\SQLEXPRESS; initial catalog=ir_connect;user id=ir_connect;Password=7mmT@XAy;Integrated Security=False;"))
            {
                connection.Open();
                SqlCommand sqlcom = new SqlCommand("select rec_outgoing from tblusers where uExt='" + exten + "'");
                sqlcom.Connection = connection;
                SqlDataReader sqlread = sqlcom.ExecuteReader();

                while (sqlread.Read())
                {
                    rec = (bool)sqlread["rec_outgoing"];
                }
                sqlread.Close();
            }


            return rec;
        }

        private void startMonitorOut(Hashtable vals)
        {
            IPEndPoint serviceIP = new IPEndPoint(IPAddress.Parse("192.168.66.64"), 9900);
            Socket serviceServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Guid g;
            g = Guid.NewGuid();
            string filename = "/media/recordings/" + g.ToString() + ".wav";
            serviceServer.Connect(serviceIP);
            serviceServer.Send(Encoding.ASCII.GetBytes("Action: MixMonitor\r\nChannel: " + vals["Channel"] + "\r\nFile: " + filename + "\r\noptions: b\r\nActionID: 1\r\n\r\n"));
            serviceServer.Disconnect(true);

            simpleSave("tblRecordings", new string[] { "agent_ext", "queue_name", "from_number", "to_number", "filename" }, new string[] { (string)vals["Channel"].ToString().Substring(4).Substring(0, 4), "", (string)vals["Exten"].ToString(), (string)vals["ConnectedLineNum"], g.ToString() + ".wav" });

        }


        private void stopMonitorOut(Hashtable vals)
        {
            IPEndPoint serviceIP = new IPEndPoint(IPAddress.Parse("192.168.66.64"), 9900);
            Socket serviceServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Guid g;
            g = Guid.NewGuid();
            string filename = "/media/recordings/" + g.ToString() + ".wav";
            serviceServer.Connect(serviceIP);
            serviceServer.Send(Encoding.ASCII.GetBytes("Action: StopMixMonitor\r\nChannel: " + vals["Channel"] + "\r\nActionID: 87\r\n\r\n"));
            serviceServer.Disconnect(true);
        }


        private void startMonitor(Hashtable vals)
        {
            IPEndPoint serviceIP = new IPEndPoint(IPAddress.Parse("192.168.66.64"), 9900);
            Socket serviceServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Guid g;
            g = Guid.NewGuid();
            string filename = "/media/recordings/" + g.ToString() + ".wav";
            serviceServer.Connect(serviceIP);
            serviceServer.Send(Encoding.ASCII.GetBytes("Action: MixMonitor\r\nChannel: " + vals["Channel"] + "\r\nFile: " + filename + "\r\noptions: b\r\nActionID: 1\r\n\r\n"));
            serviceServer.Disconnect(true);

            simpleSave("tblRecordings", new string[] { "agent_ext", "queue_name", "from_number", "to_number", "filename" }, new string[] { (string)vals["MemberName"].ToString().Substring(4), (string)vals["Queue"], (string)vals["CallerIDNum"].ToString(), (string)vals["Exten"], g.ToString() + ".wav" });

        }


        private void stopMonitor(Hashtable vals)
        {
            IPEndPoint serviceIP = new IPEndPoint(IPAddress.Parse("192.168.66.64"), 9900);
            Socket serviceServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Guid g;
            g = Guid.NewGuid();
            string filename = "/media/recordings/" + g.ToString() + ".wav";
            serviceServer.Connect(serviceIP);
            serviceServer.Send(Encoding.ASCII.GetBytes("Action: StopMixMonitor\r\nChannel: " + vals["Channel"] + "\r\nActionID: 87\r\n\r\n"));
            serviceServer.Disconnect(true);
        }

        enum extensionStatus
        {
            Idle = 0,
            InUse = 1,
            Busy = 2,
            Unavailable = 4,
            Ringing = 8,
            Hold = 16
        }
        private void simpleSQL(string qry)
        {
            //List<User> users = new List<User>();

            SqlCommand cmd = new SqlCommand();
            try
            {
                using (SqlConnection connection = new SqlConnection("Data Source=localhost\\SQLEXPRESS; initial catalog=ir_connect;user id=ir_connect;Password=7mmT@XAy;Integrated Security=False;"))
                {
                    cmd.CommandText = qry;
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.Connection = connection;
                    connection.Open();
                    cmd.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred:" + ex.Message);
            }

        }

        public void simpleSave(string tablename, string[] flds, string[] vals)
        {
            SqlCommand cmd = new SqlCommand();

            try
            {

                string fldnames = "";
                string fldvals = "";
                for (int i = 0; i < flds.Length; i++)
                {
                    if (i == flds.Length - 1)
                    {
                        fldnames += flds[i];
                        fldvals += "'" + vals[i] + "'";
                    }
                    else
                    {
                        fldnames += flds[i] + ",";
                        fldvals += "'" + vals[i] + "',";
                    }
                }

                using (SqlConnection connection = new SqlConnection("Data Source=localhost\\SQLEXPRESS; initial catalog=ir_connect;user id=ir_connect;Password=7mmT@XAy;Integrated Security=False;"))
                {
                    cmd.CommandText = "insert into " + tablename + "(" + fldnames + " ) values (" + fldvals + ")";


                    cmd.Connection = connection;
                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ee)
            {
                throw new Exception("Error" + cmd.CommandText);
            }
        }
    }
}
