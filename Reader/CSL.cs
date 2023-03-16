using MySql.Data.MySqlClient;
using RfidReader.Database;
using System.Collections;
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
        public static string? ReaderName { get; set; }

        public static string ReaderStatus = "";
        public static int AntennaID { get; set; }
        public static int AntennaInfoID { get; set; }
        public static int RadioID { get; set; }
        public static int GPIID { get; set; }
        public static int GPOID { get; set; }

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
        public bool Connected()
        {
            try
            {
                return true;
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
        private void AdjustSettings()
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
                            ReaderInfo();
                            break;
                        case 2:
                            ReaderSettings();
                            break;
                        case 3:
                            AntennaSettings();
                            break;
                        case 4:
                            GPIOConfig();
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
        private void ReaderInfo()
        {
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

                Console.WriteLine("Current Power and Sensitivity Settings");
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
                        double rxSensitivity = dataReader2.GetDouble("ReceiveSensitivity");
                        double txPower = dataReader2.GetDouble("TransmitPower");

                        Console.WriteLine("Antenna                     : {0} ", antenna);
                        Console.WriteLine("ReceiveSensitivityIndex     : {0} ", rxSensitivity);
                        Console.WriteLine("TransmitPowerIndex          : {0} \n", txPower);
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
                        int debounce = dataReader3.GetInt32("Debounce");

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
        private void ReaderSettings()
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
        private void AntennaSettings()
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
                            ConfigurePower();
                            break;
                        case 2:
                            EnableDisableAntenna();
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
        private void ConfigurePower()
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
                        double rxSensitivity = dataReader.GetDouble("ReceiveSensitivity");
                        double txPower = dataReader.GetDouble("TransmitPower");

                        Console.WriteLine("Antenna                     : {0} ", antenna);
                        Console.WriteLine("ReceiveSensitivityIndex     : {0} ", rxSensitivity);
                        Console.WriteLine("TransmitPowerIndex          : {0} \n", txPower);
                    }
                    db.Con.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private void EnableDisableAntenna()
        {

        }
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
                    if (status.Equals("False")) Console.WriteLine("Antenna Status:             : OFF\n");
                    else Console.WriteLine("Antenna Status:             : ON\n");
                }
                db.Con.Close();
            }
        }
        private void SetEnabbleAllAntenna()
        {

        }
        private void GPIOConfig()
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
                            ConfigureGPI();
                            break;
                        case 2:
                            ConfigureGPO();
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
        private void ConfigureGPI()
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
        private void ConfigureGPO()
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
        public void ReadTag()
        {

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
        private void Default()
        {

        }
        private void LoadDB()
        {

        }

    }
}
