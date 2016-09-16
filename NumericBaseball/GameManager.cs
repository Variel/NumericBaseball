using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace NumericBaseball
{
    public class GameManager
    {
        private GameManager() { }
        private static GameManager _instance;
        public static GameManager Instance => _instance ?? (_instance = new GameManager());


        private GameRoom _currentWaitingRoom;

        private readonly Dictionary<string, GameRoom> _gameRooms = new Dictionary<string, GameRoom>();
        private readonly Dictionary<Connection, string> _connectionRoomDictionary = new Dictionary<Connection, string>();

        public GameRoom GetGameRoomByRoomId(string roomId) => _gameRooms[roomId];
        public GameRoom GetGameRoomByConnection(Connection connection)
        {
            if (_connectionRoomDictionary.ContainsKey(connection))
            {
                if (_gameRooms.ContainsKey(_connectionRoomDictionary[connection]))
                {
                    return _gameRooms[_connectionRoomDictionary[connection]];
                }
            }

            return null;
        }


        public void FindRoom(Connection connection)
        {
            lock (_gameRooms)
            {
                if (_currentWaitingRoom == null)
                {
                    _currentWaitingRoom = new GameRoom(Guid.NewGuid().ToString());
                    _gameRooms.Add(_currentWaitingRoom.GroupId, _currentWaitingRoom);

                    var room = _currentWaitingRoom;
                    Task.Delay(10000)
                        .ContinueWith((t) =>
                        {
                            lock (room)
                            {
                                if (room.IsStarted)
                                {
                                    return;
                                }

                                if (room.ConnectionCount > 1)
                                {
                                    room.BeginGame();

                                    if (room == _currentWaitingRoom)
                                        _currentWaitingRoom = null;
                                }
                                else
                                {
                                    if (room == _currentWaitingRoom)
                                    {
                                        room.AskSoloPlay();
                                        room.IsOverWaiting = true;
                                    }
                                }
                            }
                        });
                }
            }

            lock (_currentWaitingRoom)
            {
                _currentWaitingRoom.AddConnection(connection);
                _connectionRoomDictionary.Add(connection, _currentWaitingRoom.GroupId);

                if (_currentWaitingRoom.IsOverWaiting)
                {
                    var room = _currentWaitingRoom;
                    Task.Delay(5000)
                        .ContinueWith((t) =>
                        {
                            lock (room)
                            {
                                if (room.IsStarted)
                                {
                                    return;
                                }

                                if (room.ConnectionCount > 1)
                                {
                                    room.BeginGame();

                                    if (room == _currentWaitingRoom)
                                        _currentWaitingRoom = null;
                                }
                                else
                                {
                                    if (room == _currentWaitingRoom)
                                    {
                                        room.AskSoloPlay();
                                    }
                                }
                            }
                        });
                }

                if (_currentWaitingRoom.ConnectionCount == 4)
                {
                    _currentWaitingRoom.BeginGame();

                    _currentWaitingRoom = null;
                }
            }
        }

        public void Disconnect(Connection connection)
        {
            lock (_gameRooms)
            {
                var room = GetGameRoomByConnection(connection);
                if (room == null)
                    return;

                room.Disconnect(connection);
                _connectionRoomDictionary.Remove(connection);

                if (room.ConnectionCount == 0 && room != _currentWaitingRoom)
                {
                    _gameRooms.Remove(room.GroupId);
                }
            }
        }

        public void FinishGame(GameRoom room)
        {
            foreach (var p in _connectionRoomDictionary.Where(p => p.Value == room.GroupId).Select(p => p.Key))
            {
                _connectionRoomDictionary.Remove(p);
            }

            _gameRooms.Remove(room.GroupId);
        }
    }
}