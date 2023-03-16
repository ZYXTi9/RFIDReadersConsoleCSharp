using Impinj.OctaneSdk;
using MySql.Data.MySqlClient;
using RfidReader.Database;
using Symbol.RFID3;
using System.Runtime.InteropServices;

namespace RfidReader
{
    class Program
    {
        MySqlCommand? cmd;

        public List<ImpinjReader> impinjReaders = new List<ImpinjReader>();
        public List<RFIDReader> zebraReaders = new List<RFIDReader>();
        public List<string> cslReaders = new List<string>();

        delegate bool ConsoleCtrlDelegate(int ctrlType);
        static void Main(string[] args)
        {
            Console.Title = "Lunox Access Control";
            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Program p = new();
            p.MainMenu();
        }
        public void MainMenu()
        {
            Reader.Impinj impinj = new();
            Reader.Zebra zebra = new();
            Reader.CSL csl = new();
            MySqlDatabase db = new();

            try
            {
                string selQuery = "SELECT ReaderType FROM reader_type_tbl";
                cmd = new MySqlCommand(selQuery, db.Con);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                Console.WriteLine("Choose Setup");
                while (dataReader.Read())
                {
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        Console.WriteLine(dataReader.GetValue(i));
                    }
                }
                Console.WriteLine("Inventory");
                Console.WriteLine("Exit");
                db.Con.Close();
                dataReader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            int connectedTo = 0;

            while (connectedTo != 1 && connectedTo != 2 && connectedTo != 3 && connectedTo != 4 && connectedTo != 5)
            {
                try
                {
                    Console.Write("\nOption[1-5]: ");
                    connectedTo = Convert.ToInt32(Console.ReadLine());

                    if (connectedTo == 1)
                    {
                        impinj.ReaderTypeID = connectedTo;
                        impinj.ImpinjMenu();
                    }
                    else if (connectedTo == 2)
                    {
                        zebra.ReaderTypeID = connectedTo;
                        zebra.ZebraMenu();
                    }
                    else if (connectedTo == 3)
                    {
                        csl.ReaderTypeID = connectedTo;
                        csl.CSLMenu();
                    }
                    else if (connectedTo == 4)
                    {
                        using (MySqlDatabase db2 = new MySqlDatabase())
                        {
                            string selQuery2 = "SELECT * FROM reader_tbl WHERE Status = 'Connected'";
                            cmd = new MySqlCommand(selQuery2, db2.Con);
                            MySqlDataReader dataReader2 = cmd.ExecuteReader();

                            if (dataReader2.HasRows)
                            {
                                Console.WriteLine("Inventory Started");
                                Console.WriteLine("Press Enter to stop inventory");

                                while (dataReader2.Read())
                                {
                                    int rTypeID = dataReader2.GetInt32("ReaderTypeID");
                                    string connected = dataReader2.GetString("Status");

                                    if (rTypeID == 1 && connected == "Connected")
                                    {
                                        impinj.ReadTag();
                                    }
                                    if (rTypeID == 2 && connected == "Connected")
                                    {
                                        zebra.ReadTag();
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("No Reader Connected");
                            }
                        }
                    }
                    else if (connectedTo == 5)
                    {
                        Console.WriteLine("Exiting application...");
                        using (MySqlDatabase db3 = new MySqlDatabase())
                        {
                            string updQuery = "UPDATE reader_tbl SET Status = 'Disconnected' WHERE Status = 'Connected'";

                            using (MySqlCommand cmd = new MySqlCommand(updQuery, db3.Con))
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid Input!");
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid Input Format");
                }
            }
        }
        static bool Console_CloseHandler(int ctrlType)
        {
            Console.WriteLine("Are you sure you want to exit? Press Y to confirm or any other key to cancel...");
            if (Console.ReadKey().Key != ConsoleKey.Y)
            {
                return true;
            }

            using (MySqlDatabase db3 = new MySqlDatabase())
            {
                string updQuery = "UPDATE reader_tbl SET Status = 'Disconnected' WHERE Status = 'Connected'";

                using (MySqlCommand cmd = new MySqlCommand(updQuery, db3.Con))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            return false;
        }

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handlerRoutine, bool add);
    }
}