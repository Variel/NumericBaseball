using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace NumericBaseball
{
    public class GameHub : Hub
    {
        private static Dictionary<string, Connection> _connections { get; } = new Dictionary<string, Connection>();

        public void Guess(string numbers)
        {
            if (Clients.CallerState.LastInput != null
                && Clients.CallerState.LastInput > DateTime.Now.AddSeconds(-5))
            {
                Clients.Caller.error("5초에 한 번씩 입력할 수 있습니다");
                return;
            }

            Clients.CallerState.LastInput = DateTime.Now;

            if (!Regex.IsMatch(numbers, "^\\d{3}$")
                || numbers[0] == numbers[1]
                || numbers[1] == numbers[2]
                || numbers[2] == numbers[0])
            {
                Clients.Caller.error("올바른 입력을 해주세요");
                return;
            }

            var connection = _connections[Context.ConnectionId];
            GameManager.Instance.GetGameRoomByConnection(connection)
                .Guess(connection, numbers.ToCharArray());
        }

        public void FindRoom()
        {
            var connection = _connections[Context.ConnectionId];
            GameManager.Instance.FindRoom(connection);
        }

        public void SetName(string name)
        {
            var connection = _connections[Context.ConnectionId];
            connection.Name = name;
        }

        public override Task OnConnected()
        {
            var connection = new Connection(Context.ConnectionId, Context.ConnectionId.Substring(0, 6));
            _connections.Add(connection.Id, connection);

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var connection = _connections[Context.ConnectionId];

            GameManager.Instance.Disconnect(connection);
            return base.OnDisconnected(stopCalled);
        }
    }
}