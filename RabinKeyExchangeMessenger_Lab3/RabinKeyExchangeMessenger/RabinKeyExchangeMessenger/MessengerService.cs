using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace RabinKeyExchangeMessenger
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "MessengerService" in both code and config file together.
    public class MessengerService : IMessengerService
    {
        private Dictionary<string, IClientCallback> clients = new Dictionary<string, IClientCallback>(); // Dictionary of connected clients
        private Dictionary<string, string> clientsIDs = new Dictionary<string, string>(); // Dictionary of client names and ids
        
        // The registration function for new client
        string IMessengerService.RegisterClient(string clientName)
        {
            IClientCallback callback = OperationContext.Current.GetCallbackChannel<IClientCallback>();
            if (!clients.ContainsKey(clientName))
            {
                string clientID = Guid.NewGuid().ToString(); // unique ID generation
                clients.Add(clientName, callback);
                clientsIDs.Add(clientName, clientID);
                Console.WriteLine($"Client {clientName} is registered with ID: {clientID}");
                return clientID;
            }
            return null; // The client is already registered
        }

        void IMessengerService.SendMessage(string message, string fromClient)
        {
            foreach (var client in clients)
            {
                if (client.Key != fromClient)
                {
                    client.Value.ReceiveMessage(message, fromClient); // sending message to other clients
                }
            }
        }
    }
}
