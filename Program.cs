using CSLibrary;
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

        Reader.Impinj impinj = new();
        Reader.Zebra zebra = new();
        Reader.CSL csl = new();

        public static List<ImpinjReader> impinjReaders = new List<ImpinjReader>();
        public static List<RFIDReader> zebraReaders = new List<RFIDReader>();
        public static List<HighLevelInterface> cslReaders = new List<HighLevelInterface>();

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
            try
            {
                MySqlDatabase db = new();
                string selQuery = "SELECT ReaderType FROM reader_type_tbl";
                cmd = new MySqlCommand(selQuery, db.Con);
                MySqlDataReader dataReader = cmd.ExecuteReader();
                Console.WriteLine("\nChoose Setup");
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

                Console.Write("\nOption[1-5]: ");
                int connectedTo = Convert.ToInt32(Console.ReadLine());
                ResultMenu(connectedTo);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private void ResultMenu(int connectedTo)
        {
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

                        impinj.ReadTag();
                        zebra.ReadTag();
                        csl.ReadTag();

                        Console.ReadKey();

                        impinj.StopRead();
                        zebra.StopRead();
                        csl.StopRead();
                    }
                    else
                    {
                        Console.WriteLine("\nNo Reader Connected\n");
                    }
                    MainMenu();
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