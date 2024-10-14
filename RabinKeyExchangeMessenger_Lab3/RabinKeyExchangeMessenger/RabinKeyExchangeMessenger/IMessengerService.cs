using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace RabinKeyExchangeMessenger
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IMessengerService" in both code and config file together.
    [ServiceContract(CallbackContract = typeof(IClientCallback))]
    public interface IMessengerService
    {
        // The client registration function returns the client identifier
        [OperationContract]
        string RegisterClient(string ClientName);

        // The sending client message function
        [OperationContract(IsOneWay = true)]
        void SendMessage(string message, string fromClient); 
    }

    public interface IClientCallback
    {
        // The Callback function for clients to receive messages
        [OperationContract(IsOneWay = true)]
        void ReceiveMessage(string message, string fromClient);
    }
}
