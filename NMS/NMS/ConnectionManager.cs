using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NMS
{
    class ConnectionManager
    {
        private Dictionary<String, Socket> existingConnections;

        public ConnectionManager()
        {
            existingConnections = new Dictionary<string, Socket>();
        }

        public void Add(String agentId, Socket relatedSocket)
        {
            CheckIfConnectionExistsAndRemove(agentId);
            existingConnections.Add(agentId, relatedSocket);
        }

        private void CheckIfConnectionExistsAndRemove(String agentId)
        {
            existingConnections.Remove(agentId);
        }

        public void SendToAgent(String agentId, byte[] message)
        {
            Socket destination = existingConnections[agentId];
            int messageSize = message.Length;
            byte[] sizeArray = BitConverter.GetBytes(messageSize);
            destination.Send(sizeArray);
            destination.Send(message);
        }
    }
}
