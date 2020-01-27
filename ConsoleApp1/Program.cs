using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace ConsoleApp1
{
    class Program
    {
        //Hauptfunktion
        string Step = "";

        static void Main(string[] args)
        {
            new Program().Worker();
        }

        void Worker()
        {
            Console.WriteLine("MTconnect2MQTT by Karl Doreth");
            try
            {
                Step = "Loadconfig";
                LoadConfig();
            }
            catch (Exception e)
            {
                Console.WriteLine("Problen during initialization: ({0}): " + e.Message, Step);
                Console.ReadKey();
                Environment.Exit(0);
            }
            Step = "MQTT_try";
            while (Step == "MQTT_try")
            {
                try
                {
                    ConnectMQTT();
                    Step = "MQTT_done";
                }
                catch (Exception e)
                {
                    Console.WriteLine("Connecting MQTT-Broker failed. Try Again in 10 Sek.");
                    System.Threading.Thread.Sleep(10000);
                }
            }
            Step = "MTconnectProbe_try";
            while (Step == "MTconnectProbe_try")
            {
                try
                {
                    XMLprobe();
                    Step = "MTconnectProbe_done";
                }
                catch (Exception e)
                {
                    Console.WriteLine("Connecting MTconnectProbe failed. Try Again in 10 Sek.");
                    System.Threading.Thread.Sleep(10000);
                }
            }

                int Timout = int.Parse(MyConfigHandling.GetConfigValue("Refreshtime"));
                while (true)
                {
                    try
                    {
                        XMLcurrent();
                        System.Threading.Thread.Sleep(Timout);
                    }
                    catch (Exception e)
                    {
                        Step = "Loop";
                        Console.WriteLine("Problen during loop ({0}): " + e.Message, Step);
                    }
                }
            }

        //Confighandling
        ConfigHandling MyConfigHandling;
        void LoadConfig()
        {
            MyConfigHandling = new ConfigHandling();
            MyConfigHandling.LoadConfig();
        }

        //Funktionen die zum Aufbauen des MTconnectTopics gebraucht werden.
        List<string> Pfadliste = new List<string>();

        Dictionary<string, string> MTConnectTopics = new Dictionary<string, string>();

        void addTopic(string text)
        {
            Pfadliste.Add(text);
        }

        void remTopic()
        {
            Pfadliste.RemoveAt(Pfadliste.Count-1);
        }

        string Pfadbauer()
        {
            string Ausgabe = "";
            foreach (string ST in Pfadliste)
            {
                Ausgabe = Ausgabe + "/" + ST;
            }
            return Ausgabe;
        }

        //Verarbeitung der MTconnectXMLDateien
        public int XMLprobe() //Reads out the Probe
        {
            String Agent = MyConfigHandling.GetConfigValue("MTconnectAgent") + "/probe";
            XmlTextReader reader = new XmlTextReader(Agent);

            while (reader.Read())
            {
                //Console.WriteLine(reader.NodeType.ToString());
                switch (reader.NodeType)
                {
                    //STARTELEMENTE
                    case XmlNodeType.Element: 
                        //Diese Ifschleife ist dazu da um ggf. Nodes zu entfernen.
                        if (reader.Name == "Blacked")
                        {
                            
                        }
                        else
                        {
                            string Knotenname = reader.Name;
                            string NodeID = reader.GetAttribute("name"); //Namen holt sich das system immer. Sie werden in den Topicnamen eingefügt.
                            if (NodeID != null)
                            {
                                Knotenname = Knotenname + "(name:" + NodeID + ")";
                            }

                            if (reader.IsEmptyElement == false) //Hier wird geschaut ob es sich um ein leeres Element handelt dass keine weiteren inhalte hat: <element/>. Ist das nicht der fall wird der Pfad aufgenommen.
                            {
                                addTopic(Knotenname);
                            }
                            else //DataItems sind leere Elemente. Daher wird hier alles Rausgehot.
                            {
                                string Attribut = reader.GetAttribute("type");
                                if (Attribut != null)
                                {
                                    Knotenname = reader.Name + "(type:" + Attribut + ")";
                                }

                                Attribut = reader.GetAttribute("subType");
                                if (Attribut != null)
                                {
                                    Knotenname = Knotenname + "(SubType:" + Attribut + ")";
                                }

                                string dataItemID = reader.GetAttribute("id");

                                if (dataItemID != null)
                                {
                                    string MTconnecttopic = Pfadbauer() + "/" + Knotenname;
                                    MTConnectTopics.Add(dataItemID, MTconnecttopic);
                                }
                                else //Leere Elemente ohne DataitemID werden abgefangen (meistens Header und Description)
                                {
                                    //Console.WriteLine(Knotenname +  " was ignored!");
                                }
                            }
                        }
                        break;

                    //TEXTelemente werden nicht gebraucht da die Probe keine enthält.
                    case XmlNodeType.Text: //Display the text in each element.
                        //Console.WriteLine(Pfadbauer() + ": " + reader.Value);
                        break;

                    //ENDTYPEN
                    case XmlNodeType.EndElement: //Display the end of the element.

                        //Diese Ifschleife ist dazu da um ggf. Nodes zu entfernen.
                        if (reader.Name == "Blacked")
                        {

                        }
                        else
                        {
                            remTopic();
                        }
                        break;
                }
            }
            int Anzahl = MTConnectTopics.Count;

            Console.WriteLine("Probe parsed! {0} Topics available! Start loop... ", Anzahl.ToString());
            return 0;
        }

        public void XMLcurrent() //Reads out the Currents
        {
            int AnzahlWerte = 0;
            String Agent = MyConfigHandling.GetConfigValue("MTconnectAgent") +"/current";
            XmlTextReader reader = new XmlTextReader(Agent);

            string lastitemid = "";

            while (reader.Read())
            {
                //Console.WriteLine(reader.NodeType.ToString());
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.

                        string Knotenname = reader.Name;

                        string Attribut = reader.GetAttribute("dataItemId");
                        if (Attribut != null)
                        {
                            lastitemid = Attribut;
                        }
                        break;

                    case XmlNodeType.Text: //Textelemente sind die Werte. Hier wird die Dataitemide des übergeordneten Items genommen und dann das zugehörige Topic geholt.
                        if (reader.Value != "UNAVAILABLE")
                        {
                            if (MTConnectTopics.ContainsKey(lastitemid) == true)
                            {
                                AnzahlWerte++;
                                string MQTTtopic = MTConnectTopics[lastitemid];
                                SendMQTT(MQTTtopic, reader.Value);
                            }
                        }
                        break;

                    case XmlNodeType.EndElement: //Display the end of the element.
                        break;
                }
            }
            if (AnzahlWerte == 0)
            {
                //Console.WriteLine("It seems as if no DataItems are active.");
            }

        }

        //Funktionen für das MQTT
        MqttClient client;

        void ConnectMQTT()
        {
            //try
            //{
                //verbindungsaufbau beim Laden der Form.
                //client = new MqttClient("mqtt.cumulocity.com");

                client = new MqttClient(MyConfigHandling.GetConfigValue("Broker"));
                if (MyConfigHandling.GetConfigValue("NeedsAuthentication") == "true")
                {
                    string identity = MyConfigHandling.GetConfigValue("Identity");
                    string password = MyConfigHandling.GetConfigValue("Password");
                    string username = MyConfigHandling.GetConfigValue("Username");
                    client.Connect(identity, username, password);
                }
                else
                {
                    string identity = MyConfigHandling.GetConfigValue("Identity");
                    client.Connect(identity);
                }

                //Dieser Befehle sind da falls auch Nachrichten Empfangen werden sollen.
                //client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
                //string[] subscriptions = { "$", "#" };
                //byte[] qoslevels = { 0, 1 };
                //client.Subscribe(subscriptions, qoslevels);

                ////MQTT String zum setzen eines Messwertes (Hier 300)
                //string topic = "hello";
                //string messege = "I am there, so what is up?";
                //ushort test3 = client.Publish(topic, Encoding.ASCII.GetBytes(messege));
            //    //Console.WriteLine(test3.ToString());
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //}
        }

        void SendMQTT(string topic, string message)
        {
            if (client.IsConnected == true)
            {
                client.Publish(topic, Encoding.ASCII.GetBytes(message));
            }
            else
            {
                Console.WriteLine("No connection. Try to reconnect...");
                ConnectMQTT();
            }

        }
    }
}
