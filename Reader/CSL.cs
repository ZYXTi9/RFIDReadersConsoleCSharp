using CSLibrary;
using CSLibrary.Constants;
using CSLibrary.Structures;
using MySql.Data.MySqlClient;
using RfidReader.Database;
using System.Collections;
using System.Data;
using System.Net.NetworkInformation;
using System.Text;

namespace RfidReader.Reader
{
    public struct reader_info
    {
        public string str_reader_type;
        public string str_ip_addr;
        public string str_epc_len;
        public string str_tid_len;
        public string str_user_data_len;
        public string str_toggleTarget;
        public string str_multiBanks;

        public int read_epc_len;               // EPC Length defined by user (by WORDS)
        public int read_tid_len;               // TID Length defined by user (by WORDS)
        public int read_user_data_len;        // User Data Length defined by user (by WORDS)

        public AntennaSequenceMode antSeqMode;           // Antenna Sequence Mode
        public byte[] antPort_seqTable;                 // Port Sequence Table
        public uint antPort_seqTableSize;               // Port Sequence Table Size

        public bool[] antPort_state;                  // Port Active / Inactive
        public uint[] antPort_power;                 // RF Power defined by user
        public uint[] antPort_dwell;                 // Dwell Time
        public uint[] antPort_Pofile;                // Profile
        public SingulationAlgorithm[] antPort_QAlg;            // Q Algorithm
        public uint[] antPort_startQValue;              // Dynamic Q defined by user
        public uint[] freq_channel;                 // Frequency Channel
        public bool IsNonHopping_usrDef;

        public uint init_toggleTarget;               // Toggle Target defined by user
        public uint init_multiBanks;                  // Multibank defined by user
        public RegionCode regionCode;               // Region Code
                                                    //        public bool freqHopping;                    // Frequency Hopping Enable flag

        public int epc_len_hex;              // number of digit display (defined by user) - EPC
        public int tid_len_hex;              // number of digit display (defined by user) - TID
        public int user_data_len_hex;   // number of digit display (defined by user) - User data

    }
    class CSL
    {
        static Program p = new();

        MySqlCommand? cmd;

        private static int ReadersNo;

        public static string ConnectionResult = "";
        public int ReaderTypeID { get; set; }
        public static int ReaderID { get; set; }
        public static string? HostName { get; set; }
        public const int TimeOut = 3000;
        public static string? ReaderName { get; set; }

        public static string ReaderStatus = "";
        public static int AntennaID { get; set; }
        public static int AntennaInfoID { get; set; }
        public static int RadioID { get; set; }
        public static int GPIID { get; set; }
        public static int GPOID { get; set; }

        public static reader_info[] rdr_info_data = new reader_info[100];

        AntennaList AntennaConfig;
        Result ret;
        public static Hashtable uniqueTags = new Hashtable();
        public static int totalTags;

        public CSL()
        {
            totalTags = 0;
        }

        public void CSLMenu()
        {
            bool isWorking = true;
            int option;

            while (isWorking)
            {
                Console.WriteLine("..................................................");
                Console.WriteLine("Welcome to CSL Menu");
                Console.WriteLine("..................................................\n");

                Console.WriteLine("----Command Menu----");
                Console.WriteLine("1. Connect To Existing Reader");
                Console.WriteLine("2. Adjust Settings");
                Console.WriteLine("3. Connect To New Reader");
                Console.WriteLine("4. Back\n");
                Console.Write("[1-4] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            ConnectToExisting();
                            break;
                        case 2:
                            ListConnectedReaders();
                            break;
                        case 3:
                            ConnectToNew();
                            break;
                        case 4:
                            p.MainMenu();
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-4");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-4");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void ConnectToExisting()
        {
            try
            {
                using (MySqlDatabase db = new MySqlDatabase())
                {
                    string selQuery = "SELECT * FROM reader_tbl WHERE ReaderTypeID = @ReaderTypeID";
                    cmd = new MySqlCommand(selQuery, db.Con);
                    cmd.Parameters.AddWithValue("@ReaderTypeID", ReaderTypeID);
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    if (dataReader.HasRows)
                    {
                        while (dataReader.Read())
                        {
                            int readerID = dataReader.GetInt32("ReaderID");
                            string ip = dataReader.GetString("IPAddress");
                            string readerName = dataReader.GetString("DeviceName");
                            string readerStatus = dataReader.GetString("Status");

                            Console.WriteLine("Reader ID                   : {0} ", readerID);
                            Console.WriteLine("IP Address                  : {0} ", ip);
                            Console.WriteLine("Reader Name                 : {0} ", readerName);
                            Console.WriteLine("Reader Status               : {0} \n", readerStatus);
                        }
                        db.Con.Close();

                        Console.Write("Enter Reader ID : ");
                        ReaderID = Convert.ToInt32(Console.ReadLine());

                        using (MySqlDatabase db2 = new MySqlDatabase())
                        {
                            string selQuery2 = "SELECT * FROM reader_tbl WHERE ReaderID = @ReaderID";
                            cmd = new MySqlCommand(selQuery2, db2.Con);
                            cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                            MySqlDataReader dataReader2 = cmd.ExecuteReader();

                            if (dataReader2.HasRows)
                            {
                                while (dataReader2.Read())
                                {
                                    HostName = dataReader2.GetString("IPAddress");
                                    ReaderName = dataReader2.GetString("DeviceName");
                                    ReaderStatus = dataReader2.GetString("Status");

                                    var targetReader = Program.cslReaders.Where(x => x.DeviceNameOrIP == HostName).FirstOrDefault();

                                    if (targetReader != null)
                                    {
                                        if ((ret = targetReader.LastResultCode) == Result.OK)
                                        {
                                            db.Con.Close();
                                            Console.WriteLine("You are already connected in this Reader");
                                        }
                                        else
                                        {
                                            ReaderIsAvailable(HostName);
                                            if (Connected(targetReader))
                                            {
                                                db.Con.Close();
                                                CSLMenu();
                                            }
                                        }
                                    }
                                    else if (ReaderIsAvailable(HostName))
                                    {
                                        if (Connected())
                                        {
                                            db.Con.Close();
                                            CSLMenu();
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Reader is not connected to the network");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Enter Valid Reader ID");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No Existing Reader");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public bool Connected(HighLevelInterface? reader = null)
        {
            try
            {
                if (ConnectionResult == "Success")
                {
                    if (reader == null)
                    {
                        reader = new HighLevelInterface();
                        Program.cslReaders.Add(reader);
                    }

                    reader.Connect(HostName, TimeOut);

                    if ((ret = reader.LastResultCode) == Result.OK)
                    {
                        MySqlDatabase db3 = new();

                        string selQuery3 = @"SpCSLReader";
                        cmd = new MySqlCommand(selQuery3, db3.Con);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@rtypeID", ReaderTypeID);
                        cmd.Parameters.AddWithValue("@ip", HostName);
                        cmd.Parameters.AddWithValue("@device", ReaderName);
                        cmd.Parameters.AddWithValue("@tout", TimeOut);
                        cmd.Parameters.AddWithValue("@readerStatus", "Connected");

                        var getReaderID = cmd.ExecuteScalar();

                        if (db3.Con.State != ConnectionState.Open)
                        {
                            db3.Con.Open();
                        }
                        if (getReaderID != null)
                        {
                            ReaderID = Convert.ToInt32(getReaderID);
                        }
                        db3.Con.Close();

                        MySqlDatabase db4 = new();

                        string selQuery4 = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID";
                        cmd = new MySqlCommand(selQuery4, db4.Con);
                        cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                        MySqlDataReader dataReader4 = cmd.ExecuteReader();

                        var targetReader = Program.cslReaders.Where(x => x.DeviceNameOrIP == HostName).FirstOrDefault();

                        if (dataReader4.HasRows)
                        {
                            dataReader4.Close();
                            if (db4.Con.State != ConnectionState.Open)
                            {
                                db4.Con.Open();
                            }
                            db4.Con.Close();

                            if (targetReader != null)
                            {
                                if ((ret = targetReader.LastResultCode) == Result.OK)
                                {
                                    LoadDB(targetReader);
                                }
                            }
                        }
                        else
                        {
                            Default(targetReader);
                        }
                    }
                    Console.WriteLine("Successfully connected.");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Reader is not connected to the network");
                return false;
            }
        }
        private void ListConnectedReaders()
        {
            try
            {
                using (MySqlDatabase db = new MySqlDatabase())
                {
                    string selQuery = "SELECT * FROM reader_tbl WHERE ReaderTypeID = @ReaderTypeID AND Status = 'Connected'";
                    cmd = new MySqlCommand(selQuery, db.Con);
                    cmd.Parameters.AddWithValue("@ReaderTypeID", ReaderTypeID);
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    if (dataReader.HasRows)
                    {
                        Console.WriteLine("Connected Readers\n");

                        while (dataReader.Read())
                        {
                            int readerID = dataReader.GetInt32("ReaderID");
                            string ip = dataReader.GetString("IPAddress");
                            string readerName = dataReader.GetString("DeviceName");
                            string readerStatus = dataReader.GetString("Status");

                            Console.WriteLine("Reader ID                   : {0} ", readerID);
                            Console.WriteLine("IP Address                  : {0} ", ip);
                            Console.WriteLine("Reader Name                 : {0} \n", readerName);
                        }

                        db.Con.Close();

                        Console.Write("Enter Reader ID : ");
                        ReaderID = Convert.ToInt32(Console.ReadLine());

                        using (MySqlDatabase db2 = new MySqlDatabase())
                        {
                            string selQuery2 = "SELECT * FROM reader_tbl WHERE ReaderID = @ReaderID";
                            cmd = new MySqlCommand(selQuery2, db2.Con);
                            cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                            MySqlDataReader dataReader2 = cmd.ExecuteReader();

                            if (dataReader2.HasRows)
                            {
                                while (dataReader2.Read())
                                {
                                    HostName = dataReader2.GetString("IPAddress");
                                    ReaderName = dataReader2.GetString("DeviceName");
                                    ReaderStatus = dataReader2.GetString("Status");

                                    var targetReader = Program.cslReaders.Where(x => x.IPAddress == HostName).FirstOrDefault();

                                    if (targetReader == null)
                                    {
                                        Console.WriteLine("Please connect the reader");
                                        db.Con.Close();
                                        CSLMenu();
                                    }
                                    else if ((ret = targetReader.LastResultCode) != Result.OK)
                                    {
                                        Console.WriteLine("Please connect the reader");
                                        db.Con.Close();
                                        CSLMenu();
                                    }
                                    else if ((ret = targetReader.LastResultCode) == Result.OK)
                                    {
                                        AdjustSettings(targetReader);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Enter Valid ReaderID");
                                    }
                                }
                                db.Con.Close();
                            }
                            else
                            {
                                Console.WriteLine("Enter Valid Reader ID");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No Reader Connected");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void AdjustSettings(HighLevelInterface reader)
        {
            bool isWorking = true;
            int option;

            while (isWorking)
            {
                Console.WriteLine("..................................................");
                Console.WriteLine("CSL RFID Settings");
                Console.WriteLine("..................................................\n");

                Console.WriteLine("----Command Menu----");
                Console.WriteLine("1. Reader Info");
                Console.WriteLine("2. Reader Settings");
                Console.WriteLine("3. Antenna Settings");
                Console.WriteLine("4. GPIO Configuration");
                Console.WriteLine("5. Back\n");
                Console.Write("[1-5] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            ReaderInfo(reader);
                            break;
                        case 2:
                            ReaderSettings(reader);
                            break;
                        case 3:
                            AntennaSettings(reader);
                            break;
                        case 4:
                            GPIOConfig(reader);
                            break;
                        case 5:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-5");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-5");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void ReaderInfo(HighLevelInterface reader)
        {
            CSLibrary.Structures.Version FirmVers = reader.GetFirmwareVersion();

            Console.WriteLine("\nReader Capabilities");
            Console.WriteLine("---------------");
            Console.WriteLine("Firware Version                              : {0}", FirmVers.ToString());
            Console.WriteLine("Model Name                                   : {0}", reader.OEMDeviceType.ToString());
            Console.WriteLine("Country Code                                 : {0}", reader.OEMCountryCode.ToString());
            Console.WriteLine("Is Hopping Enabled                           : {0} \n", reader.IsHoppingChannelOnly);

            try
            {
                Console.WriteLine("Current Reader Settings");
                Console.WriteLine("---------------");

                MySqlDatabase db1 = new();

                string selQuery1 = "SELECT * FROM reader_settings_tbl WHERE ReaderID = @ReaderID";
                cmd = new MySqlCommand(selQuery1, db1.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader1 = cmd.ExecuteReader();

                if (dataReader1.HasRows)
                {
                    while (dataReader1.Read())
                    {
                        string readerMode = dataReader1.GetString("ReaderMode");
                        string searchMode = dataReader1.GetString("SearchMode");
                        int session = dataReader1.GetInt32("Session");
                        int tagPopulation = dataReader1.GetInt32("TagPopulation");

                        Console.WriteLine("Reader mode                                  : {0}", readerMode);
                        Console.WriteLine("Search mode                                  : {0}", searchMode);
                        Console.WriteLine("Session                                      : {0} ", session);
                        Console.WriteLine("Tag Population                               : {0} \n", tagPopulation);
                    }
                    db1.Con.Close();
                }

                Console.WriteLine("Current Power Settings");
                Console.WriteLine("---------------");

                MySqlDatabase db2 = new();

                string selQuery2 = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID ORDER BY Antenna ASC";
                cmd = new MySqlCommand(selQuery2, db2.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader2 = cmd.ExecuteReader();

                if (dataReader2.HasRows)
                {
                    while (dataReader2.Read())
                    {
                        int antenna = dataReader2.GetInt32("Antenna");
                        double txPower = dataReader2.GetDouble("TransmitPower");

                        Console.WriteLine("Antenna                     : {0} ", antenna);
                        Console.WriteLine("Power                       : {0} \n", txPower);
                    }
                    db2.Con.Close();
                }

                Console.WriteLine("Current GPI Configuration");
                Console.WriteLine("---------------");

                MySqlDatabase db3 = new();

                string selQuery3 = "SELECT * FROM gpi_tbl WHERE ReaderID = @ReaderID ORDER BY GPIPort ASC";
                cmd = new MySqlCommand(selQuery3, db3.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader3 = cmd.ExecuteReader();

                if (dataReader3.HasRows)
                {
                    while (dataReader3.Read())
                    {
                        int gpiPort = dataReader3.GetInt32("GPIPort");
                        string gpiLevel = dataReader3.GetString("GPIStatus");

                        Console.WriteLine("GPI Port                    : {0} ", gpiPort);
                        if (gpiLevel.Equals("True")) Console.WriteLine("GPI Level                   : High");
                        else Console.WriteLine("GPI Level                   : Low");
                    }
                    db3.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private void ReaderSettings(HighLevelInterface reader)
        {
            bool isWorking = true;
            int option;

            while (isWorking)
            {
                Console.WriteLine("\n----Reader Settings----");
                Console.WriteLine("1. Reader Mode, Search Mode & Session");
                Console.WriteLine("2. Go back");
                Console.Write("\n[1-2] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            break;
                        case 2:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-3");
                            break;
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-3");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void AntennaSettings(HighLevelInterface reader)
        {
            int option;
            bool isWorking = true;

            while (isWorking)
            {
                Console.WriteLine("\n----Antenna Settings---");
                Console.WriteLine("1. Power & Sensitivity");
                Console.WriteLine("2. Enable/Disable Antenna");
                Console.WriteLine("3. Back to Settings Menu\n");
                Console.Write("[1-3] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            ConfigurePower(reader);
                            break;
                        case 2:
                            EnableDisableAntenna(reader);
                            break;
                        case 3:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-3");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-5");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void ConfigurePower(HighLevelInterface reader)
        {
            bool isWorking = true;
            int option, antenna;

            while (isWorking)
            {
                Console.WriteLine("\n----Command Menu----");
                Console.WriteLine("1. Set Antenna Power & Sensitivity");
                Console.WriteLine("2. Get Antenna Power & Sensitivity");
                Console.WriteLine("3. Go back\n");
                Console.Write("[1-3] : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());
                    switch (option)
                    {
                        case 1:
                            Console.Write("\nAntenna                          : ");
                            antenna = Convert.ToInt32(Console.ReadLine());

                            break;
                        case 2:
                            DisplayPower(reader);
                            break;
                        case 3:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid integer in the range 1-3");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-3");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void DisplayPower(HighLevelInterface reader)
        {
            for (int i = 0; i < reader.AntennaList.Count; i++)
            {
                Console.WriteLine(reader.AntennaList[(i)].PowerLevel);
            }

            try
            {
                MySqlDatabase db = new();

                string selQuery = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID ORDER BY Antenna ASC";
                cmd = new MySqlCommand(selQuery, db.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        int antenna = dataReader.GetInt32("Antenna");
                        int power = dataReader.GetInt32("TransmitPower");

                        Console.WriteLine("Antenna                     : {0} ", antenna);
                        Console.WriteLine("TransmitPowerIndex          : {0} \n", power);
                    }
                    db.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private void EnableDisableAntenna(HighLevelInterface reader)
        {
            bool isWorking = true;
            int antenna, option, antennaStatus;

            while (isWorking)
            {
                Console.WriteLine("\n----Command Menu----");
                Console.WriteLine("1. Set Antenna Port");
                Console.WriteLine("2. Get Antenna Port Info");
                Console.WriteLine("3. Set Enable All Antenna Port");
                Console.WriteLine("4. Go back\n");
                Console.Write("[1-4] : ");

                try
                {

                    option = Convert.ToInt32(Console.ReadLine());

                    switch (option)
                    {
                        case 1:
                            //try
                            //{
                            //    Console.Write("\nAntenna : ");
                            //    antenna = Convert.ToInt32(Console.ReadLine());

                            //    if (antenna <= 0 || antenna > reader.)
                            //    {
                            //        Console.WriteLine("Enter a valid Antenna in the range 1-" + reader.ReaderCapabilities.NumAntennaSupported);
                            //        continue;
                            //    }

                            //    MySqlDatabase db1 = new();

                            //    string selQuery1 = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID AND Antenna = @Antenna";

                            //    cmd = new MySqlCommand(selQuery1, db1.Con);
                            //    cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                            //    cmd.Parameters.AddWithValue("@Antenna", antenna);

                            //    if (db1.Con.State != ConnectionState.Open)
                            //    {
                            //        db1.Con.Open();
                            //    }
                            //    var getAntennaID1 = cmd.ExecuteScalar();
                            //    if (getAntennaID1 != null)
                            //    {
                            //        AntennaID = Convert.ToInt32(getAntennaID1);
                            //    }
                            //    db1.Con.Close();

                            //    Console.WriteLine("\n[0] OFF");
                            //    Console.WriteLine("[1] ON");
                            //    Console.Write("Option : ");

                            //    antennaStatus = Convert.ToInt32(Console.ReadLine());

                            //    if (antennaStatus == 0)
                            //    {
                            //        if (statusList.Contains(antenna))
                            //        {
                            //            statusList.Remove(Convert.ToInt32(antenna));

                            //            if (statusList.Count == 0)
                            //            {
                            //                foreach (ushort x in antID)
                            //                {
                            //                    statusList.Add(x);
                            //                    statusList.Sort();
                            //                }

                            //                MySqlDatabase db2 = new();

                            //                for (int i = 0; i < reader.ReaderCapabilities.NumAntennaSupported; i++)
                            //                {
                            //                    string selQuery2 = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID AND Antenna = @Antenna";
                            //                    cmd = new MySqlCommand(selQuery2, db2.Con);
                            //                    cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                            //                    cmd.Parameters.AddWithValue("@Antenna", (i + 1));

                            //                    if (db2.Con.State != ConnectionState.Open)
                            //                    {
                            //                        db2.Con.Open();
                            //                    }

                            //                    MySqlDataReader dataReader2 = cmd.ExecuteReader();

                            //                    if (dataReader2.HasRows)
                            //                    {
                            //                        dataReader2.Close();
                            //                        var getAntennaID2 = cmd.ExecuteScalar();
                            //                        if (getAntennaID2 != null)
                            //                        {
                            //                            AntennaID = Convert.ToInt32(getAntennaID2);
                            //                        }

                            //                        MySqlDatabase db3 = new();
                            //                        string selQuery3 = "SELECT * FROM antenna_tbl a INNER JOIN antenna_info_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = @ReaderID AND b.AntennaID = @AntennaID AND b.AntennaStatus= 'Disabled'";
                            //                        cmd = new MySqlCommand(selQuery3, db3.Con);
                            //                        cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                            //                        cmd.Parameters.AddWithValue("@AntennaID", AntennaID);

                            //                        if (db3.Con.State != ConnectionState.Open)
                            //                        {
                            //                            db3.Con.Open();
                            //                        }

                            //                        MySqlDataReader dataReader3 = cmd.ExecuteReader();

                            //                        if (dataReader3.HasRows)
                            //                        {
                            //                            dataReader3.Close();
                            //                            var getAntennaInfoID = cmd.ExecuteScalar();
                            //                            if (getAntennaInfoID != null)
                            //                            {
                            //                                AntennaInfoID = Convert.ToInt32(getAntennaInfoID);
                            //                            }

                            //                            MySqlDatabase db4 = new();
                            //                            string updQuery = "UPDATE antenna_info_tbl SET AntennaStatus = 'Enabled' WHERE AntennaInfoID = @AntennaInfoID";

                            //                            cmd = new MySqlCommand(updQuery, db4.Con);
                            //                            cmd.Parameters.AddWithValue("@AntennaInfoID", AntennaInfoID);
                            //                            cmd.Parameters.Clear();
                            //                            cmd.ExecuteNonQuery();
                            //                        }
                            //                        else
                            //                        {
                            //                            dataReader3.Close();
                            //                        }
                            //                        db3.Con.Close();
                            //                    }
                            //                }

                            //                Console.WriteLine("\nDisabling all antennas is not allowed.");
                            //                Console.WriteLine("All antennas have been reset and are now available.");
                            //            }
                            //            else
                            //            {
                            //                ushort[] antList = new ushort[statusList.Count];
                            //                for (int i = 0; i < statusList.Count; i++)
                            //                {
                            //                    antList[i] = Convert.ToUInt16(statusList[i].ToString());
                            //                }

                            //                if (null == antennaInfo)
                            //                {
                            //                    antennaInfo = new Symbol.RFID3.AntennaInfo(antList);
                            //                }
                            //                else
                            //                {
                            //                    antennaInfo.AntennaID = antList;
                            //                }

                            //                Console.WriteLine("Antenna Port :  {0} ", antenna);
                            //                Console.WriteLine("Status       : OFF\n");
                            //                Console.WriteLine("Set Antenna Successfully\n");

                            //                MySqlDatabase db3 = new();
                            //                string selQuery2 = @"SpAntennaInfo";
                            //                cmd = new MySqlCommand(selQuery2, db3.Con);
                            //                cmd.CommandType = CommandType.StoredProcedure;

                            //                cmd.Parameters.AddWithValue("@aID", AntennaID);
                            //                cmd.Parameters.AddWithValue("@antStatus", "Disabled");

                            //                if (db3.Con.State != ConnectionState.Open)
                            //                {
                            //                    db3.Con.Open();
                            //                }

                            //                cmd.ExecuteScalar();

                            //                db3.Con.Close();
                            //            }
                            //        }
                            //        else
                            //        {
                            //            Console.WriteLine($"Antenna Port {antenna} is already OFF");
                            //        }
                            //    }
                            //    else if (antennaStatus == 1)
                            //    {
                            //        if (statusList.Contains(antenna))
                            //        {
                            //            Console.WriteLine($"Antenna Port {antenna} is already ON");
                            //        }
                            //        else
                            //        {
                            //            statusList.Add(antenna);
                            //            statusList.Sort();

                            //            MySqlDatabase db4 = new();
                            //            string selQuery = @"SpAntennaInfo";
                            //            cmd = new MySqlCommand(selQuery, db4.Con);
                            //            cmd.CommandType = CommandType.StoredProcedure;

                            //            cmd.Parameters.AddWithValue("@aID", AntennaID);
                            //            cmd.Parameters.AddWithValue("@antStatus", "Enabled");

                            //            if (db4.Con.State != ConnectionState.Open)
                            //            {
                            //                db4.Con.Open();
                            //            }

                            //            cmd.ExecuteScalar();

                            //            db4.Con.Close();

                            //            Console.WriteLine("Antenna Port :  {0} ", antenna);
                            //            Console.WriteLine("Status       : ON\n");
                            //            Console.WriteLine("Set Antenna Successfully\n");
                            //        }
                            //    }
                            //    else
                            //    {
                            //        Console.WriteLine("Enter a valid integer in the range 0-1");
                            //        break;
                            //    }
                            //}
                            //catch (Exception)
                            //{
                            //    Console.WriteLine("Zebra Reader Setting Error");
                            //}
                            break;
                        case 2:
                            DisplayAntennaStatus(reader);
                            break;
                        case 3:
                            SetEnableAllAntenna(reader);
                            break;
                        case 4:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid integer in the range 1-4");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (IOException)
                {
                    Console.WriteLine("Enter a valid Integer in the range 1-4");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void DisplayAntennaStatus(HighLevelInterface reader)
        {
            for (uint i = 0; i < reader.AntennaList.Count; i++)
            {
                Console.WriteLine(reader.AntennaList[Convert.ToInt32(i)].AntennaStatus.state);
            }


            MySqlDatabase db = new();

            string selQuery = "SELECT a.Antenna, b.AntennaStatus FROM antenna_tbl a INNER JOIN antenna_info_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = @ReaderID ORDER BY a.Antenna ASC";
            cmd = new MySqlCommand(selQuery, db.Con);
            cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
            MySqlDataReader dataReader = cmd.ExecuteReader();

            if (dataReader.HasRows)
            {
                while (dataReader.Read())
                {
                    int antenna = dataReader.GetInt32("Antenna");
                    string status = dataReader.GetString("AntennaStatus");

                    Console.WriteLine($"Antenna                     : {antenna}");
                    if (status.Equals(AntennaPortState.DISABLED)) Console.WriteLine("Antenna Status:             : OFF\n");
                    else Console.WriteLine("Antenna Status:             : ON\n");
                }
                db.Con.Close();
            }
        }
        private void SetEnableAllAntenna(HighLevelInterface reader)
        {

        }
        private void GPIOConfig(HighLevelInterface reader)
        {
            bool isWorking = true;
            int option;

            while (isWorking)
            {
                Console.WriteLine("\n----GPIO Menu----");
                Console.WriteLine("1. GPI Config");
                Console.WriteLine("2. GPO Config");
                Console.WriteLine("3. Go back\n");
                Console.Write("[1-3]  : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());

                    switch (option)
                    {
                        case 1:
                            ConfigureGPI(reader);
                            break;
                        case 2:
                            ConfigureGPO(reader);
                            break;
                        case 3:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-3");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception : {0}", e.Message);
                }
            }
        }
        private void ConfigureGPI(HighLevelInterface reader)
        {
            bool isWorking = true;
            int option, gpiPort;

            while (isWorking)
            {
                Console.WriteLine("\n----GPI Menu----");
                Console.WriteLine("1. Set GPI State");
                Console.WriteLine("2. Get GPI State");
                Console.WriteLine("3. Go back\n");
                Console.Write("[1-3]  : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());

                    switch (option)
                    {
                        case 1:
                            Console.Write("\nGPI Port : ");
                            gpiPort = Convert.ToInt32(Console.ReadLine());

                            break;
                        case 2:
                            DisplayGPI();
                            break;
                        case 3:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-3");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception : {0}", e.Message);
                }
            }
        }
        private void DisplayGPI()
        {
            try
            {
                MySqlDatabase db = new();

                string selQuery = "SELECT * FROM gpi_tbl WHERE ReaderID = @ReaderID ORDER BY GPIPort ASC";
                cmd = new MySqlCommand(selQuery, db.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        int gpiPort = dataReader.GetInt32("GPIPort");
                        string gpiLevel = dataReader.GetString("GPIStatus");

                        Console.WriteLine("GPI Port                    : {0} ", gpiPort);
                        if (gpiLevel.Equals("True")) Console.WriteLine("GPI Level                   : High\n");
                        else Console.WriteLine("GPI Level                   : Low\n");
                    }
                    db.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }
        private void ConfigureGPO(HighLevelInterface reader)
        {
            bool isWorking = true;
            int option, gpoPort;

            while (isWorking)
            {
                Console.WriteLine("\n----GPO Menu----");
                Console.WriteLine("1. Set GPO State");
                Console.WriteLine("2. Get GPO State");
                Console.WriteLine("3. Go back\n");
                Console.Write("[1-3]  : ");

                try
                {
                    option = Convert.ToInt32(Console.ReadLine());

                    switch (option)
                    {
                        case 1:
                            Console.Write("\nGPO Port : ");
                            gpoPort = Convert.ToInt32(Console.ReadLine());

                            break;
                        case 2:
                            DisplayGPO();
                            break;
                        case 3:
                            isWorking = false;
                            break;
                        default:
                            Console.WriteLine("Enter a valid Integer in the range 1-3");
                            break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception : {0}", e.Message);
                }
            }
        }
        private void DisplayGPO()
        {
            try
            {
                MySqlDatabase db = new();

                string selQuery = "SELECT * FROM gpo_tbl WHERE ReaderID = @ReaderID ORDER BY GPOPort ASC";
                cmd = new MySqlCommand(selQuery, db.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader = cmd.ExecuteReader();

                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        int gpoPort = dataReader.GetInt32("GPOPort");
                        string gpoMode = dataReader.GetString("GPOMode");

                        Console.WriteLine("GPO Port                    : {0} ", gpoPort);
                        Console.WriteLine("GPO Mode                    : {0} \n", gpoMode);
                    }
                    db.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }
        private void ConnectToNew()
        {
            try
            {
                Console.Write("HostName or IP Name   : ");
                HostName = Console.ReadLine();

                Console.Write("Reader Name           : ");
                ReaderName = Console.ReadLine();

                using (MySqlDatabase db = new MySqlDatabase())
                {
                    string insQuery = "INSERT INTO reader_tbl (ReaderTypeID, IPAddress, DeviceName, Status) VALUES (@ReaderTypeID, @IP, @ReaderName, @ReaderStatus)";
                    cmd = new MySqlCommand(insQuery, db.Con);

                    cmd.Parameters.AddWithValue("@ReaderTypeID", ReaderTypeID);
                    cmd.Parameters.AddWithValue("@IP", HostName);
                    cmd.Parameters.AddWithValue("@ReaderName", ReaderName);
                    cmd.Parameters.AddWithValue("@ReaderStatus", "Disconnected");

                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine("\nReader Has Successfully Added");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        static bool ReaderIsAvailable(string address)
        {
            try
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();
                options.DontFragment = true;
                byte[] buffer = Encoding.Default.GetBytes("12345");
                PingReply reply = pingSender.Send(address, 500, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    ConnectionResult = "Success";
                    return true;
                }
                else
                {
                    ConnectionResult = "Error";
                    return false;
                }
            }
            catch (PingException)
            {
                return false;
            }
        }

        //static object StateChangedLock = new object();
        //static void ReaderXP_StateChangedEvent(object sender, CSLibrary.Events.OnStateChangedEventArgs e)
        //{

        //    lock (StateChangedLock)
        //    {
        //        HighLevelInterface t_Reader = (HighLevelInterface)sender;
        //        ReaderCtrlClass t_readerCtrl = new ReaderCtrlClass();
        //        string t_str_readerinfo;

        //        switch (e.state)
        //        {
        //            case CSLibrary.Constants.RFState.IDLE:
        //                break;
        //            case CSLibrary.Constants.RFState.BUSY:
        //                break;
        //            case CSLibrary.Constants.RFState.RESET:
        //                // Reconnect reader and restart inventory

        //                t_str_readerinfo = t_Reader.IPAddress + " : Reader is disconnected";
        //                Console.WriteLine(t_str_readerinfo);
        //                str_dataLogToFile += "\r\n" + t_str_readerinfo + "\r\n";

        //                TextWriter tw = new StreamWriter("ReaderResetLog_" + DateTime.Now.ToString("yyyyMMdd") + ".Txt", true);
        //                tw.WriteLine(t_str_readerinfo + "          " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss UTC zzz"));
        //                tw.Close();

        //                //Use other thread to create progress
        //                t_readerCtrl.Reader = t_Reader;
        //                Thread reset = new Thread(t_readerCtrl.Reset);
        //                reset.Start();

        //                break;
        //            case CSLibrary.Constants.RFState.ABORT:
        //                break;
        //        }
        //    }
        //}
        //static object TagInventoryLock = new object();
        //static void ReaderXP_TagInventoryEvent(object sender, CSLibrary.Events.OnAsyncCallbackEventArgs e)
        //{
        //    string str_reader_info_0;

        //    lock (TagInventoryLock)
        //    {
        //        HighLevelInterface Reader = (HighLevelInterface)sender;

        //        // Display Tag info in Console Window
        //        // str_reader_info_0 = "Reader ID: " +Reader.Name+ "  ( Port= " +e.info.antennaPort.ToString()+ "  Pwr=" +Reader.AntennaList[0].PowerLevel.ToString()+ "  RSSI=" +e.info.rssi.ToString()+ ")";
        //        // sb_datalog.Append(str_reader_info_0);

        //        str_reader_info_0 = Reader.IPAddress + " , " + e.info.epc.ToString();
        //        str_reader_info_0 += " , Port= " + e.info.antennaPort.ToString();
        //        // str_reader_info_0 += " , Time : " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss UTC zzz") + ", ( Port= " +e.info.antennaPort.ToString()+ "  Pwr=" +Reader.AntennaList[0].PowerLevel.ToString()+ "  RSSI=" +e.info.rssi.ToString()+ " RName : " +Reader.Name+ ")" + "\r\n";
        //        str_reader_info_0 += " , Time : " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss UTC zzz") + ", ( Pwr=" + Reader.AntennaList[0].PowerLevel.ToString() + "  RSSI=" + e.info.rssi.ToString() + " RName : " + Reader.Name + ")" + "\r\n";

        //        str_dataLogEvt += str_reader_info_0;


        //        if (DataLog_Sample(Reader.IPAddress) == 1)
        //        {
        //            string test_name = Reader.Name;         // For Test only

        //            str_dataLogToFile = str_dataLogEvt;
        //            str_dataLogEvt = "";
        //        }

        //    }
        //}
        public void ReadTag()
        {
            try
            {
                foreach (HighLevelInterface reader in Program.cslReaders)
                {
                    uniqueTags.Clear();
                    totalTags = 0;
                    reader.StartOperation(Operation.TAG_RANGING, false);
                }

                Console.ReadKey();

                foreach (HighLevelInterface reader in Program.cslReaders)
                {
                    reader.StopOperation(true);
                    Console.WriteLine("\nCSL Total Tags: " + uniqueTags.Count + "(" + totalTags + ")");

                    MySqlDatabase db1 = new();
                    string updQuery = "UPDATE read_tbl SET TimeOut = TIME_FORMAT(NOW(), '%h:%i:%s %p'), LogActive = 'No' WHERE LogActive = 'Yes'";
                    cmd = new MySqlCommand(updQuery, db1.Con);
                    cmd.Parameters.Clear();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while executing the ReadTag method: " + ex.Message);
            }
        }
        private void Default(HighLevelInterface reader)
        {
            AntennaConfig = new AntennaList();

            AntennaPortStatus antennaPortStatus = new AntennaPortStatus();
            AntennaPortConfig antennaPortConfig = new AntennaPortConfig();

            try
            {
                //Antenna
                MySqlDatabase db1 = new();

                string selQuery1 = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID";
                cmd = new MySqlCommand(selQuery1, db1.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader1 = cmd.ExecuteReader();

                if (!dataReader1.HasRows)
                {
                    dataReader1.Close();
                    string insQuery1 = "INSERT INTO antenna_tbl (ReaderID, Antenna, TransmitPower) VALUES (@rID, @ant, @power)";
                    cmd = new MySqlCommand(insQuery1, db1.Con);

                    for (uint i = 0; i < reader.AntennaList.Count; i++)
                    {
                        reader.GetAntennaPortConfiguration(i, ref antennaPortConfig);
                        antennaPortConfig.powerLevel = 100;
                        reader.SetAntennaPortConfiguration(i, antennaPortConfig);

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@rID", ReaderID);
                        cmd.Parameters.AddWithValue("@ant", i + 1);
                        cmd.Parameters.AddWithValue("@power", antennaPortConfig.powerLevel);

                        if (db1.Con.State != ConnectionState.Open)
                        {
                            db1.Con.Open();
                        }
                        cmd.ExecuteNonQuery();
                        db1.Con.Close();
                    }
                }

                //Reader Settings

                //Enabling Antenna
                MySqlDatabase db6 = new();

                for (uint i = 0; i < reader.AntennaList.Count; i++)
                {
                    string selQuery6 = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID AND Antenna = @Antenna";
                    cmd = new MySqlCommand(selQuery6, db6.Con);
                    cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                    cmd.Parameters.AddWithValue("@Antenna", i + 1);

                    if (db6.Con.State != ConnectionState.Open)
                    {
                        db6.Con.Open();
                    }

                    MySqlDataReader dataReader6 = cmd.ExecuteReader();

                    if (dataReader6.HasRows)
                    {
                        dataReader6.Close();
                        var res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            AntennaID = Convert.ToInt32(res);
                        }

                        reader.GetAntennaPortStatus(i, antennaPortStatus);

                        reader.SetAntennaPortStatus(i, antennaPortStatus);

                        reader.SetAntennaPortState(i, AntennaPortState.ENABLED);

                        string insQuery6 = "INSERT INTO antenna_info_tbl (AntennaID, AntennaStatus) VALUES (@aID, @antStatus)";
                        cmd = new MySqlCommand(insQuery6, db6.Con);

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@aID", AntennaID);
                        cmd.Parameters.AddWithValue("@antStatus", AntennaPortState.ENABLED.ToString());

                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        dataReader6.Close();
                    }

                    db6.Con.Close();
                }

                //GPI

                //GPO

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void LoadDB(HighLevelInterface reader)
        {
            AntennaConfig = new AntennaList();

            AntennaPortStatus antennaPortStatus = new AntennaPortStatus();
            AntennaPortConfig antennaPortConfig = new AntennaPortConfig();

            try
            {
                //Antenna
                MySqlDatabase db1 = new();

                string selQuery1 = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID ORDER BY Antenna ASC";
                cmd = new MySqlCommand(selQuery1, db1.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader1 = cmd.ExecuteReader();

                if (dataReader1.HasRows)
                {
                    while (dataReader1.Read())
                    {
                        int antennaIndex = dataReader1.GetInt32("Antenna") - 1;

                        if (antennaIndex < reader.AntennaList.Count)
                        {
                            antennaPortConfig.powerLevel = Convert.ToUInt32(dataReader1.GetInt32("TransmitPower"));

                            reader.GetAntennaPortConfiguration(Convert.ToUInt32(antennaIndex), ref antennaPortConfig);

                            reader.SetAntennaPortConfiguration(Convert.ToUInt32(antennaIndex), antennaPortConfig);
                        }
                        else
                        {
                            Console.WriteLine("Antenna index {0} is out of range", antennaIndex);
                        }
                    }
                    db1.Con.Close();
                }

                //Reader Settings

                //Enabling Antenna

                MySqlDatabase db3 = new();

                for (int c = 0; c < reader.AntennaList.Count; c++)
                {
                    string selQuery3 = "SELECT a.Antenna, b.AntennaStatus FROM antenna_tbl a INNER JOIN antenna_info_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = @ReaderID AND a.Antenna = @Antenna ORDER BY a.Antenna ASC";
                    cmd = new MySqlCommand(selQuery3, db3.Con);
                    cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                    cmd.Parameters.AddWithValue("@Antenna", (c + 1));

                    if (db3.Con.State != ConnectionState.Open)
                    {
                        db3.Con.Open();
                    }

                    MySqlDataReader dataReader3 = cmd.ExecuteReader();

                    if (dataReader3.HasRows)
                    {
                        while (dataReader3.Read())
                        {
                            int antennaIndex = dataReader3.GetInt32("Antenna") - 1;
                            string antennaStatus = dataReader3.GetString("AntennaStatus");

                            if (antennaStatus == "DISABLED" || antennaStatus == "UNKNOWN")
                            {
                                //reader.AntennaList[Convert.ToInt32(c)].AntennaStatus.state = AntennaPortState.DISABLED;
                                antennaPortStatus.state = AntennaPortState.DISABLED;
                            }

                            else if (antennaStatus == "ENABLED")
                            {
                                //reader.AntennaList[Convert.ToInt32(c)].AntennaStatus.state = AntennaPortState.ENABLED;
                                antennaPortStatus.state = AntennaPortState.ENABLED;
                            }

                            reader.GetAntennaPortStatus(Convert.ToUInt32(antennaIndex), antennaPortStatus);

                            reader.SetAntennaPortStatus(Convert.ToUInt32(antennaIndex), antennaPortStatus);

                            reader.SetAntennaPortConfiguration(Convert.ToUInt32(antennaIndex), antennaPortConfig);

                            //if (antennaIndex < reader.AntennaList.Count)
                            //{
                            //    reader.GetAntennaPortStatus(Convert.ToUInt32(antennaIndex), antennaPortStatus);

                            //    reader.SetAntennaPortStatus(Convert.ToUInt32(antennaIndex), antennaPortStatus);

                            //    reader.SetAntennaPortConfiguration(Convert.ToUInt32(antennaIndex), antennaPortConfig);
                            //}
                        }
                        db3.Con.Close();
                    }
                }

                //GPI

                //GPO
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
