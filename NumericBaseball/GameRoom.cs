using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace NumericBaseball
{
    public class GameRoom
    {
        private readonly Lazy<IHubContext> _context = new Lazy<IHubContext>(() => GlobalHost.ConnectionManager.GetHubContext("GameHub"));
        private readonly List<Connection> _connections = new List<Connection>();
        private char[] _theNumber;
        private Dictionary<Connection, int> _scores = new Dictionary<Connection, int>();
        private Dictionary<Connection, List<string>> _history = new Dictionary<Connection, List<string>>();

        public string GroupId { get; }

        public bool IsStarted { get; private set; }
        public bool IsOverWaiting { get; set; }

        public int ConnectionCount => _connections.Count;
        public DateTime CreatedAt { get; } = DateTime.Now;

        public GameRoom(string groupId)
        {
            GroupId = groupId;
        }

        public void BeginGame()
        {
            IsStarted = true;

            var check = new bool[9];
            var rnd = new Random();
            var theNumberStr = "";

            for (int i = 0; i < 3; i++)
            {
                int num;

                do
                {
                    num = rnd.Next(1, 10);
                } while (check[num - 1]);

                check[num - 1] = true;

                theNumberStr += num;
            }

            _theNumber = theNumberStr.ToCharArray();

            _context.Value.Clients.Group(GroupId).beginGame();
        }

        public void Guess(Connection connection, char[] numbers)
        {
            if (!IsStarted)
            {
                _context.Value.Clients.Client(connection.Id).error("다른 플레이어를 기다리는 중입니다");
                return;
            }

            int strike = 0, ball = 0;

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (numbers[i] == _theNumber[j])
                    {
                        if (i == j)
                            strike++;
                        else
                            ball++;
                    }
                }
            }

            var numbersString = new string(numbers);


            if (!_history[connection].Contains(numbersString))
            {
                _scores[connection] += (strike*2 + ball);
            }

            if (strike == 3)
            {
                _scores[connection] += 2;

                _context.Value.Clients.Group(GroupId)
                    .updateScores(_scores.OrderByDescending(s => s.Value).Select(s => $"{s.Key.Name}: {s.Value}"));

                FinishGame(_scores.OrderByDescending(s => s.Value).First().Key);

                return;
            }


            _history[connection].Add(numbersString);

            _context.Value.Clients.Group(GroupId)
                .newGuess(connection.Name, $"{new string(numbers)}: {strike}S {ball}B");

            _context.Value.Clients.Group(GroupId)
                .updateScores(_scores.OrderByDescending(s => s.Value).Select(s => $"{s.Key.Name}: {s.Value}"));
        }

        public void AddConnection(Connection connection)
        {
            _context.Value.Groups.Add(connection.Id, GroupId);
            _connections.Add(connection);
            _context.Value.Clients.Client(connection.Id).joinRoom(GroupId.Substring(0, 4));
            _context.Value.Clients.Group(GroupId, connection.Id).playerConnected(connection.Name);

            _scores.Add(connection, 0);
            _history.Add(connection, new List<string>());

            _context.Value.Clients.Client(connection.Id)
                .updateScores(_scores.OrderByDescending(s => s.Value).Select(s => $"{s.Key.Name}: {s.Value}"));
            _context.Value.Clients.Group(GroupId, connection.Id)
                .updateScores(_scores.OrderByDescending(s => s.Value).Select(s => $"{s.Key.Name}: {s.Value}"));
        }

        public void Disconnect(Connection connection)
        {
            _context.Value.Clients.Group(GroupId, connection.Id).playerDisconnected(connection.Name);
            _context.Value.Groups.Remove(connection.Id, GroupId);
            _connections.Remove(connection);
            _scores.Remove(connection);
            _history.Remove(connection);
        }

        public void AskSoloPlay()
        {
            if (_connections.Count == 1)
            {
                _context.Value.Clients.Client(_connections[0].Id).askSoloPlay();
            }
        }

        private void FinishGame(Connection winner)
        {
            _context.Value.Clients.Group(GroupId).finishGame(winner.Name, _scores[winner]);

            foreach (var connection in _connections)
                _context.Value.Groups.Remove(connection.Id, GroupId);

            GameManager.Instance.FinishGame(this);
        }
    }
}