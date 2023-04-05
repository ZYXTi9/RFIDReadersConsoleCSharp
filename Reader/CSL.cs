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
    class CSL
    {
        static Program p = new();

        MySqlCommand? cmd;

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

        public delegate void TagReadHandler(object sender, CSLibrary.Events.OnAsyncCallbackEventArgs e);
        public event TagReadHandler TagRead;

        private TagGroup tagGroup;

        Result ret;

        public static Hashtable uniqueTags = new Hashtable();
        public static int totalTags;
        public CSL()
        {
            tagGroup = new TagGroup();
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
                            //p.MainMenu();
                            ReadTag();
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

                        db3.OpenConnection();
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
                            db4.OpenConnection();
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
                Console.WriteLine("2. Inventory Config");
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
                            InvConfig(reader);
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

            Console.WriteLine(tagGroup.selected.ToString());
            Console.WriteLine(tagGroup.session.ToString());
            Console.WriteLine(tagGroup.target.ToString());

            try
            {
                Console.WriteLine("Current Inventory Config");
                Console.WriteLine("---------------");

                MySqlDatabase db1 = new();

                string selQuery1 = "SELECT * FROM antenna_tbl a INNER JOIN singulation_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = @ReaderID AND a.Antenna = 1";
                cmd = new MySqlCommand(selQuery1, db1.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader1 = cmd.ExecuteReader();

                if (dataReader1.HasRows)
                {
                    while (dataReader1.Read())
                    {
                        string selected = dataReader1.GetString("SLFlag");
                        string session = dataReader1.GetString("Session");
                        string target = dataReader1.GetString("InventoryState");

                        Console.WriteLine("Selected                                     : {0}", selected);
                        Console.WriteLine("Session                                      : {0}", session);
                        Console.WriteLine("Target                                       : {0}\n", target);
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
        private void InvConfig(HighLevelInterface reader)
        {
            bool isWorking = true;
            int option;

            while (isWorking)
            {
                Console.WriteLine("\n----Inventory Config----");
                Console.WriteLine("1. Search Mode & Session");
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

            AntennaPortConfig antennaPortConfig = new AntennaPortConfig();

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
                            Console.Write("\nAntenna                     : ");
                            antenna = Convert.ToInt32(Console.ReadLine());

                            if (antenna <= 0 || antenna > reader.AntennaList.Count)
                            {
                                Console.WriteLine("Enter a valid Antenna in the range 1-" + reader.AntennaList.Count);
                                continue;
                            }

                            antenna -= 1;

                            reader.GetAntennaPortConfiguration(Convert.ToUInt32(antenna), ref antennaPortConfig);

                            int[] powerValues = new int[301];
                            for (int i = 0; i <= 300; i++)
                            {
                                powerValues[i] = i;
                            }
                            Console.Write("Transmit Power Index  Value : ");
                            int power = Convert.ToInt32(Console.ReadLine());
                            reader.AntennaList[antenna].AntennaConfig.powerLevel = (ushort)power;

                            if (powerValues.Contains(power))
                            {
                                reader.SetAntennaPortConfiguration(Convert.ToUInt32(antenna), reader.AntennaList[antenna].AntennaConfig);

                                MySqlDatabase db1 = new();
                                string selQuery = @"SpCSLAntenna";
                                cmd = new MySqlCommand(selQuery, db1.Con);
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@rID", ReaderID);
                                cmd.Parameters.AddWithValue("@ant", antenna + 1);
                                cmd.Parameters.AddWithValue("@power", reader.AntennaList[antenna].AntennaConfig.powerLevel);

                                db1.OpenConnection();
                                cmd.ExecuteScalar();
                                db1.Con.Close();

                                Console.WriteLine("\nSet Antenna Configuration Successfully");
                            }
                            else
                            {
                                Console.WriteLine("Input is invalid");
                                continue;
                            }
                            break;
                        case 2:
                            //DisplayPower(reader);
                            DisplayPower();
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
        //private void DisplayPower(HighLevelInterface reader)
        //{
        //    for (int i = 0; i < reader.AntennaList.Count; i++)
        //    {
        //        Console.WriteLine(reader.AntennaList[i].PowerLevel);
        //        //Console.WriteLine(reader.AntennaList[i].AntennaConfig.powerLevel);
        //        //Console.WriteLine(reader.AntennaList[i].PowerLevel);
        //    }
        private void DisplayPower()
        {
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
                            Console.Write("\nAntenna : ");
                            antenna = Convert.ToInt32(Console.ReadLine());

                            if (antenna <= 0 || antenna > reader.AntennaList.Count)
                            {
                                Console.WriteLine("Enter a valid Antenna in the range 1-" + reader.AntennaList.Count);
                                continue;
                            }

                            MySqlDatabase db1 = new();

                            string selQuery1 = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID AND Antenna = @Antenna";

                            cmd = new MySqlCommand(selQuery1, db1.Con);
                            cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                            cmd.Parameters.AddWithValue("@Antenna", antenna);

                            db1.OpenConnection();
                            var getAntennaID1 = cmd.ExecuteScalar();
                            if (getAntennaID1 != null)
                            {
                                AntennaID = Convert.ToInt32(getAntennaID1);
                            }
                            db1.Con.Close();

                            Console.WriteLine("\n[0] OFF");
                            Console.WriteLine("[1] ON");
                            Console.Write("Option : ");

                            antennaStatus = Convert.ToInt32(Console.ReadLine());

                            antenna -= 1;

                            if (antennaStatus == 0)
                            {
                                if (reader.AntennaList[antenna].State == AntennaPortState.ENABLED)
                                {
                                    reader.GetAntennaPortStatus(Convert.ToUInt32(antenna), reader.AntennaList[antenna].AntennaStatus);
                                    reader.SetAntennaPortStatus(Convert.ToUInt32(antenna), reader.AntennaList[antenna].AntennaStatus);
                                    reader.SetAntennaPortState(Convert.ToUInt32(antenna), AntennaPortState.DISABLED);

                                    MySqlDatabase db2 = new();
                                    string selQuery = @"SpAntennaInfo";
                                    cmd = new MySqlCommand(selQuery, db2.Con);
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@aID", AntennaID);
                                    cmd.Parameters.AddWithValue("@antStatus", reader.AntennaList[antenna].State.ToString());

                                    db2.OpenConnection();
                                    cmd.ExecuteScalar();
                                    db2.Con.Close();

                                    Console.WriteLine("\nAntenna Port :  {0} ", (antenna + 1));
                                    Console.WriteLine("Status       : OFF");
                                    Console.WriteLine("\nSet Antenna Successfully\n");
                                }
                                else
                                    Console.WriteLine($"Antenna {antenna + 1} is already OFF");
                            }
                            else if (antennaStatus == 1)
                            {
                                if (reader.AntennaList[antenna].State == AntennaPortState.DISABLED)
                                {
                                    reader.GetAntennaPortStatus(Convert.ToUInt32(antenna), reader.AntennaList[antenna].AntennaStatus);
                                    reader.SetAntennaPortStatus(Convert.ToUInt32(antenna), reader.AntennaList[antenna].AntennaStatus);
                                    reader.SetAntennaPortState(Convert.ToUInt32(antenna), AntennaPortState.ENABLED);

                                    MySqlDatabase db3 = new();
                                    string selQuery = @"SpAntennaInfo";
                                    cmd = new MySqlCommand(selQuery, db3.Con);
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@aID", AntennaID);
                                    cmd.Parameters.AddWithValue("@antStatus", reader.AntennaList[antenna].State.ToString());

                                    db3.OpenConnection();
                                    cmd.ExecuteScalar();
                                    db3.Con.Close();

                                    Console.WriteLine("\nAntenna Port :  {0} ", (antenna + 1));
                                    Console.WriteLine("Status       : ON");
                                    Console.WriteLine("\nSet Antenna Successfully\n");
                                }
                                else
                                    Console.WriteLine($"Antenna {antenna + 1} is already ON");
                            }
                            else
                            {
                                Console.WriteLine("Enter a valid integer in the range 0-1");
                                continue;
                            }
                            break;
                        case 2:
                            //DisplayAntennaStatus(reader);
                            DisplayAntennaStatus();
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
        //private void DisplayAntennaStatus(HighLevelInterface reader)
        //{
        //    for (int i = 0; i < reader.AntennaList.Count; i++)
        //    {
        //        Console.WriteLine(reader.AntennaList[i].State);
        //        //Console.WriteLine(reader.AntennaList[i].State.ToString());
        //        //Console.WriteLine(reader.AntennaList[i].State.ToString());
        //        //Console.WriteLine(reader.AntennaList[i].State);
        //        //Console.WriteLine(AntennaConfig[i].PowerLevel);
        //    }
        private void DisplayAntennaStatus()
        {
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
                    if (status.Equals("DISABLED")) Console.WriteLine("Antenna Status:             : OFF\n");
                    else Console.WriteLine("Antenna Status:             : ON\n");
                }
                db.Con.Close();
            }
        }
        private void SetEnableAllAntenna(HighLevelInterface reader)
        {
            try
            {
                MySqlDatabase db1 = new();

                for (int i = 0; i < reader.AntennaList.Count; i++)
                {
                    reader.GetAntennaPortStatus(Convert.ToUInt32(i), reader.AntennaList[i].AntennaStatus);
                    reader.SetAntennaPortStatus(Convert.ToUInt32(i), reader.AntennaList[i].AntennaStatus);
                    reader.SetAntennaPortState(Convert.ToUInt32(i), AntennaPortState.ENABLED);

                    string selQuery1 = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID AND Antenna = @Antenna";
                    cmd = new MySqlCommand(selQuery1, db1.Con);
                    cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                    cmd.Parameters.AddWithValue("@Antenna", (i + 1));

                    db1.OpenConnection();

                    MySqlDataReader dataReader1 = cmd.ExecuteReader();

                    if (dataReader1.HasRows)
                    {
                        dataReader1.Close();
                        var res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            AntennaID = Convert.ToInt32(res);
                        }

                        MySqlDatabase db2 = new();
                        string selQuery2 = "SELECT * FROM antenna_tbl a INNER JOIN antenna_info_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = @ReaderID AND b.AntennaID = @AntennaID AND b.AntennaStatus= 'DISABLED'";

                        cmd = new MySqlCommand(selQuery2, db2.Con);
                        cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                        cmd.Parameters.AddWithValue("@AntennaID", AntennaID);

                        db2.OpenConnection();

                        MySqlDataReader dataReader2 = cmd.ExecuteReader();

                        if (dataReader2.HasRows)
                        {
                            dataReader2.Close();

                            var res2 = cmd.ExecuteScalar();
                            if (res2 != null)
                            {
                                AntennaInfoID = Convert.ToInt32(res2);
                            }

                            MySqlDatabase db3 = new();
                            string updQuery = "UPDATE antenna_info_tbl SET AntennaStatus = 'ENABLED' WHERE AntennaInfoID = @AntennaInfoID";

                            cmd = new MySqlCommand(updQuery, db3.Con);
                            cmd.Parameters.AddWithValue("@AntennaInfoID", AntennaInfoID);
                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                        }
                        else
                        {
                            dataReader2.Close();
                        }
                        db1.Con.Close();
                    }
                }
                Console.WriteLine("\nSuccessfully Enabled All Antennas.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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

                            if (gpiPort <= 0 || gpiPort > 4)
                            {
                                Console.WriteLine("Enter a valid Port in the range 1-4");
                                continue;
                            }


                            break;
                        case 2:
                            DisplayGPI(reader);
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
        private void DisplayGPI(HighLevelInterface reader)
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
            bool isWorking = true, gpoMode = true;
            int option, gpoPort, gpoStatus;

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

                            if (gpoPort <= 0 || gpoPort > 4)
                            {
                                Console.WriteLine("Enter a valid Port in the range 1-4");
                                continue;
                            }

                            Console.WriteLine("\nGPO Status");
                            Console.WriteLine("[0] OFF");
                            Console.WriteLine("[1] ON");
                            Console.Write("Option   : ");

                            gpoStatus = Convert.ToInt32(Console.ReadLine());

                            if (gpoStatus == 0 || gpoStatus == 1)
                            {
                                if (gpoStatus == 0)
                                {
                                    gpoMode = false;

                                    if (gpoPort == 1)
                                        reader.SetGPO0Status(gpoMode);
                                    else if (gpoPort == 2)
                                        reader.SetGPO1Status(gpoMode);
                                    else if (gpoPort == 3)
                                        reader.SetGPO2Status(gpoMode);
                                    else if (gpoPort == 4)
                                        reader.SetGPO3Status(gpoMode);

                                    Console.WriteLine("\nGPI Port : {0} \nMode     : OFF", gpoPort);
                                }
                                else if (gpoStatus == 1)
                                {
                                    gpoMode = true;

                                    if (gpoPort == 1)
                                        reader.SetGPO0Status(gpoMode);
                                    else if (gpoPort == 2)
                                        reader.SetGPO1Status(gpoMode);
                                    else if (gpoPort == 3)
                                        reader.SetGPO2Status(gpoMode);
                                    else if (gpoPort == 4)
                                        reader.SetGPO3Status(gpoMode);

                                    Console.WriteLine("\nGPI Port : {0} \nMode     : ON", gpoPort);
                                }

                                Console.WriteLine("\nSet GPO Successfully");

                                try
                                {
                                    MySqlDatabase db1 = new();
                                    string selQuery = "SELECT * FROM gpo_tbl WHERE ReaderID = @ReaderID AND GPOPort = @GPOPort";
                                    cmd = new MySqlCommand(selQuery, db1.Con);
                                    cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                                    cmd.Parameters.AddWithValue("@GPOPort", gpoPort);

                                    db1.OpenConnection();

                                    MySqlDataReader dataReader2 = cmd.ExecuteReader();

                                    if (dataReader2.HasRows)
                                    {
                                        dataReader2.Close();
                                        var res2 = cmd.ExecuteScalar();
                                        if (res2 != null)
                                        {
                                            GPOID = Convert.ToInt32(res2);
                                        }

                                        MySqlDatabase db2 = new();
                                        string updQuery = "UPDATE gpo_tbl SET GPOMode = @gpoMode WHERE GPOID = @GPOID AND ReaderID = @ReaderID AND GPOPort = @GPOPort";
                                        cmd = new MySqlCommand(updQuery, db2.Con);
                                        cmd.Parameters.AddWithValue("@gpoMode", gpoMode.ToString());
                                        cmd.Parameters.AddWithValue("@GPOID", GPOID);
                                        cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                                        cmd.Parameters.AddWithValue("@GPOPort", gpoPort);
                                        cmd.ExecuteNonQuery();
                                    }
                                    else
                                    {
                                        dataReader2.Close();
                                    }
                                    db1.Con.Close();
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }

                            }
                            else
                            {
                                Console.WriteLine("Invalid Input Format");
                            }
                            break;
                        case 2:
                            DisplayGPO(reader);
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
        private void DisplayGPO(HighLevelInterface reader)
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

                        if (gpoMode.Equals("True")) Console.WriteLine("GPO Mode                    : ON \n");
                        else Console.WriteLine("GPO Mode                    : OFF \n");

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

        static void ConnectionLostEvent(object sender, CSLibrary.Events.OnStateChangedEventArgs e)
        {
            switch (e.state)
            {
                case CSLibrary.Constants.RFState.IDLE:
                    break;
                case CSLibrary.Constants.RFState.BUSY:
                    break;
                case CSLibrary.Constants.RFState.RESET:
                    // Reconnect reader and restart inventory

                    break;
                case CSLibrary.Constants.RFState.ABORT:
                    break;
            }
        }
        private void TagInventoryEvent(object sender, CSLibrary.Events.OnAsyncCallbackEventArgs e)
        {
            DataTable dt = new();

            dt.Columns.Add("EPC");

            foreach (HighLevelInterface reader in Program.cslReaders)
            {
                try
                {
                    TagCallbackInfo tag = e.info;
                    if (tag != null)
                    {
                        for (int nIndex = 0; nIndex < tag.count; nIndex++)
                        {
                            string epc = tag.epc.ToString();
                            bool isFound = false;

                            lock (uniqueTags.SyncRoot)
                            {
                                isFound = uniqueTags.ContainsKey(epc);
                                if (!isFound)
                                {
                                    isFound = uniqueTags.ContainsKey(epc);
                                }
                            }

                            dt.Rows.Add(epc);

                            totalTags += Convert.ToInt32(tag.count);

                            if (!isFound)
                            {
                                Console.WriteLine($"{epc}");

                                MySqlDatabase db1 = new();
                                string selQuery1 = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID AND Antenna = @Antenna";

                                cmd = new MySqlCommand(selQuery1, db1.Con);
                                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                                cmd.Parameters.AddWithValue("@Antenna", tag.antennaPort);

                                db1.OpenConnection();
                                var res = cmd.ExecuteScalar();
                                if (res != null)
                                {
                                    AntennaID = Convert.ToInt32(res);
                                }
                                db1.Con.Close();

                                MySqlDatabase db2 = new();
                                string selQuery2 = @"SpRead";
                                cmd = new MySqlCommand(selQuery2, db2.Con);
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@aID", AntennaID);
                                cmd.Parameters.AddWithValue("@epcTag", epc);
                                db2.OpenConnection();
                                cmd.ExecuteScalar();
                                db2.Con.Close();

                                uniqueTags.Add(epc, dt.Rows);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public void ReadTag()
        {
            try
            {
                foreach (HighLevelInterface reader in Program.cslReaders)
                {
                    uniqueTags.Clear();
                    totalTags = 0;
                    reader.StartOperation(Operation.TAG_RANGING, false);
                    reader.OnAsyncCallback += new EventHandler<CSLibrary.Events.OnAsyncCallbackEventArgs>(TagInventoryEvent);
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

                    for (int i = 0; i < reader.AntennaList.Count; i++)
                    {
                        reader.GetAntennaPortConfiguration(Convert.ToUInt32(i), ref antennaPortConfig);
                        reader.AntennaList[i].AntennaConfig.powerLevel = 200;
                        reader.SetAntennaPortConfiguration(Convert.ToUInt32(i), reader.AntennaList[i].AntennaConfig);

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@rID", ReaderID);
                        cmd.Parameters.AddWithValue("@ant", i + 1);
                        cmd.Parameters.AddWithValue("@power", reader.AntennaList[i].AntennaConfig.powerLevel);

                        //if (db1.Con.State != ConnectionState.Open)
                        //{
                        //    db1.Con.Open();
                        //}
                        db1.OpenConnection();
                        cmd.ExecuteNonQuery();
                        db1.Con.Close();
                    }
                }

                //Inventory Config
                MySqlDatabase db4 = new();

                for (int i = 0; i < reader.AntennaList.Count; i++)
                {
                    string selQuery4 = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID AND Antenna = @Antenna";
                    cmd = new MySqlCommand(selQuery4, db4.Con);
                    cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                    cmd.Parameters.AddWithValue("@Antenna", i + 1);

                    db4.OpenConnection();

                    MySqlDataReader dataReader4 = cmd.ExecuteReader();

                    if (dataReader4.HasRows)
                    {
                        dataReader4.Close();
                        var res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            AntennaID = Convert.ToInt32(res);
                        }

                        tagGroup.selected = Selected.ALL;
                        tagGroup.session = Session.S0;
                        tagGroup.target = SessionTarget.A;

                        string insQuery4 = "INSERT INTO singulation_tbl (AntennaID, Session, InventoryState, SLFlag) VALUES (@aID, @session, @invState, @slFlag)";
                        cmd = new MySqlCommand(insQuery4, db4.Con);

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@aID", AntennaID);
                        cmd.Parameters.AddWithValue("@session", tagGroup.session.ToString());
                        cmd.Parameters.AddWithValue("@invState", tagGroup.target.ToString());
                        cmd.Parameters.AddWithValue("@slFlag", tagGroup.selected.ToString());

                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        dataReader4.Close();
                    }
                    db4.Con.Close();
                }

                //Enabling Antenna
                MySqlDatabase db6 = new();

                for (int i = 0; i < reader.AntennaList.Count; i++)
                {
                    string selQuery6 = "SELECT * FROM antenna_tbl WHERE ReaderID = @ReaderID AND Antenna = @Antenna";
                    cmd = new MySqlCommand(selQuery6, db6.Con);
                    cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                    cmd.Parameters.AddWithValue("@Antenna", i + 1);

                    //if (db6.Con.State != ConnectionState.Open)
                    //{
                    //    db6.Con.Open();
                    //}
                    db6.OpenConnection();
                    MySqlDataReader dataReader6 = cmd.ExecuteReader();

                    if (dataReader6.HasRows)
                    {
                        dataReader6.Close();
                        var res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            AntennaID = Convert.ToInt32(res);
                        }

                        reader.GetAntennaPortStatus(Convert.ToUInt32(i), reader.AntennaList[i].AntennaStatus);
                        reader.SetAntennaPortStatus(Convert.ToUInt32(i), reader.AntennaList[i].AntennaStatus);
                        reader.SetAntennaPortState(Convert.ToUInt32(i), AntennaPortState.ENABLED);

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
                MySqlDatabase db7 = new();

                string selQuery7 = "SELECT * FROM gpi_tbl WHERE ReaderID = @ReaderID";
                cmd = new MySqlCommand(selQuery7, db7.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader7 = cmd.ExecuteReader();

                if (!dataReader7.HasRows)
                {
                    dataReader7.Close();
                    string insQuery7 = "INSERT INTO gpi_tbl (ReaderID, GPIPort, GPIStatus) VALUES (@rID, @gpiPortNo, @gpiStats)";
                    cmd = new MySqlCommand(insQuery7, db7.Con);

                    bool status = false;

                    //reader.GetGPI0Status(ref status);
                    //reader.GetGPI1Status(ref status);
                    //reader.GetGPI2Status(ref status);
                    //reader.GetGPI3Status(ref status);

                    reader.GetGPI0Status(ref status);
                    reader.GetGPI1Status(ref status);
                    reader.GetGPI2Status(ref status);
                    reader.GetGPI3Status(ref status);

                    for (int i = 0; i < 4; i++)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@rID", ReaderID);
                        cmd.Parameters.AddWithValue("@gpiPortNo", (i + 1));
                        cmd.Parameters.AddWithValue("@gpiStats", status.ToString());

                        db7.OpenConnection();
                        cmd.ExecuteNonQuery();
                        db7.Con.Close();
                    }
                }

                //GPO
                MySqlDatabase db8 = new();

                string selQuery8 = "SELECT * FROM gpo_tbl WHERE ReaderID = @ReaderID";
                cmd = new MySqlCommand(selQuery8, db8.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader8 = cmd.ExecuteReader();

                if (!dataReader8.HasRows)
                {
                    dataReader8.Close();
                    string insQuery8 = "INSERT INTO gpo_tbl (ReaderID, GPOPort, GPOMode) VALUES (@rID, @gpoPortNo, @gpoStats)";
                    cmd = new MySqlCommand(insQuery8, db8.Con);

                    for (int i = 0; i < 4; i++)
                    {
                        bool status = false;

                        //reader.GetGPO0Status(ref status);
                        //reader.GetGPO1Status(ref status);
                        //reader.GetGPO2Status(ref status);
                        //reader.GetGPO3Status(ref status);

                        reader.SetGPO0Status(status);
                        reader.SetGPO1Status(status);
                        reader.SetGPO2Status(status);
                        reader.SetGPO3Status(status);

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@rID", ReaderID);
                        cmd.Parameters.AddWithValue("@gpoPortNo", (i + 1));
                        cmd.Parameters.AddWithValue("@gpoStats", status.ToString());

                        db8.OpenConnection();
                        cmd.ExecuteNonQuery();
                        db8.Con.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void LoadDB(HighLevelInterface reader)
        {
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
                            reader.GetAntennaPortConfiguration(Convert.ToUInt32(antennaIndex), ref antennaPortConfig);
                            reader.AntennaList[antennaIndex].AntennaConfig.powerLevel = Convert.ToUInt32(dataReader1.GetInt32("TransmitPower"));
                            reader.SetAntennaPortConfiguration(Convert.ToUInt32(antennaIndex), reader.AntennaList[antennaIndex].AntennaConfig);
                        }
                    }
                    db1.Con.Close();
                }

                //Inventory Config
                MySqlDatabase db2 = new();

                string selQuery2 = "SELECT * FROM antenna_tbl a INNER JOIN singulation_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = @ReaderID AND a.Antenna = @Antenna";
                cmd = new MySqlCommand(selQuery2, db2.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                cmd.Parameters.AddWithValue("@Antenna", 1);
                MySqlDataReader dataReader2 = cmd.ExecuteReader();

                if (dataReader2.HasRows)
                {
                    while (dataReader2.Read())
                    {
                        //string selected = dataReader2.GetString("SLFlag");

                        //if (selected == "ALL") tagGroup.selected = Selected.ALL;
                        //else if (selected == "Asserted") tagGroup.selected = Selected.ASSERTED;
                        //else if (selected == "Deasserted") tagGroup.selected = Selected.DEASSERTED;
                        string selected = dataReader2.GetString("SLFlag");
                        tagGroup.selected = selected == "ALL" ? Selected.ALL :
                                            selected == "Asserted" ? Selected.ASSERTED :
                                            selected == "Deasserted" ? Selected.DEASSERTED :
                                            default(Selected);

                        tagGroup.session = (Session)System.Enum.Parse(typeof(Session), dataReader2.GetString("Session"));
                        tagGroup.target = (SessionTarget)System.Enum.Parse(typeof(SessionTarget), dataReader2.GetString("InventoryState"));
                    }
                    db2.Con.Close();
                }

                //Enabling Antenna
                MySqlDatabase db3 = new();

                for (int c = 0; c < reader.AntennaList.Count; c++)
                {
                    string selQuery3 = "SELECT a.Antenna, b.AntennaStatus FROM antenna_tbl a INNER JOIN antenna_info_tbl b ON a.AntennaID = b.AntennaID WHERE a.ReaderID = @ReaderID AND a.Antenna = @Antenna ORDER BY a.Antenna ASC";
                    cmd = new MySqlCommand(selQuery3, db3.Con);
                    cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                    cmd.Parameters.AddWithValue("@Antenna", (c + 1));

                    //if (db3.Con.State != ConnectionState.Open)
                    //{
                    //    db3.Con.Open();
                    //}
                    db3.OpenConnection();

                    MySqlDataReader dataReader3 = cmd.ExecuteReader();

                    if (dataReader3.HasRows)
                    {
                        while (dataReader3.Read())
                        {
                            int antennaIndex = dataReader3.GetInt32("Antenna") - 1;
                            string antennaStatus = dataReader3.GetString("AntennaStatus");

                            reader.GetAntennaPortStatus(Convert.ToUInt32(antennaIndex), reader.AntennaList[antennaIndex].AntennaStatus);
                            reader.SetAntennaPortStatus(Convert.ToUInt32(antennaIndex), reader.AntennaList[antennaIndex].AntennaStatus);

                            if (antennaStatus == "DISABLED" || antennaStatus == "UNKNOWN")
                            {
                                reader.SetAntennaPortState(Convert.ToUInt32(antennaIndex), AntennaPortState.DISABLED);
                            }

                            else if (antennaStatus == "ENABLED")
                            {
                                reader.SetAntennaPortState(Convert.ToUInt32(antennaIndex), AntennaPortState.ENABLED);
                            }
                        }
                        db3.Con.Close();
                    }
                }

                //GPI
                MySqlDatabase db7 = new();

                string selQuery7 = "SELECT * FROM gpi_tbl WHERE ReaderID = @ReaderID ORDER BY GPIPort ASC";
                cmd = new MySqlCommand(selQuery7, db7.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader7 = cmd.ExecuteReader();

                bool status;

                if (dataReader7.HasRows)
                {
                    while (dataReader7.Read())
                    {
                        int gpiIndex = dataReader7.GetInt32("GPIPort");

                        if (gpiIndex == 0)
                        {
                            status = Convert.ToBoolean(dataReader7.GetString("GPIStatus"));
                            reader.GetGPI0Status(ref status);
                        }
                        else if (gpiIndex == 1)
                        {
                            status = Convert.ToBoolean(dataReader7.GetString("GPIStatus"));
                            reader.GetGPI1Status(ref status);
                        }
                        else if (gpiIndex == 2)
                        {
                            status = Convert.ToBoolean(dataReader7.GetString("GPIStatus"));
                            reader.GetGPI2Status(ref status);
                        }
                        else
                        {
                            status = Convert.ToBoolean(dataReader7.GetString("GPIStatus"));
                            reader.GetGPI3Status(ref status);
                        }
                    }
                    db7.Con.Close();
                }

                //GPO
                MySqlDatabase db8 = new();

                string selQuery8 = "SELECT * FROM gpo_tbl WHERE ReaderID = @ReaderID ORDER BY GPOPort ASC";
                cmd = new MySqlCommand(selQuery8, db8.Con);
                cmd.Parameters.AddWithValue("@ReaderID", ReaderID);
                MySqlDataReader dataReader8 = cmd.ExecuteReader();

                if (dataReader8.HasRows)
                {
                    while (dataReader8.Read())
                    {
                        int gpoIndex = dataReader8.GetInt32("GPOPort");

                        if (gpoIndex == 0)
                        {
                            status = Convert.ToBoolean(dataReader8.GetString("GPOMode"));
                            //reader.GetGPO0Status(ref status);
                            reader.SetGPO0Status(status);
                        }
                        else if (gpoIndex == 1)
                        {
                            status = Convert.ToBoolean(dataReader8.GetString("GPOMode"));
                            //reader.GetGPO1Status(ref status);
                            reader.SetGPO1Status(status);
                        }
                        else if (gpoIndex == 2)
                        {
                            status = Convert.ToBoolean(dataReader8.GetString("GPOMode"));
                            //reader.GetGPO2Status(ref status);
                            reader.SetGPO2Status(status);
                        }
                        else
                        {
                            status = Convert.ToBoolean(dataReader8.GetString("GPOMode"));
                            //reader.GetGPO3Status(ref status);
                            reader.SetGPO3Status(status);
                        }
                    }
                    db8.Con.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
