(function() {
    angular
        .module('app', ['SignalR'])
        .controller('gameController', gameController);

    gameController.$inject = ['Hub'];

    function gameController(Hub) {
        var vm = this;

        var hub = new Hub('gameHub',
        {
            listeners: {
                error: onError,
                newGuess: onNewGuess,
                updateScores: onUpdateScores,
                beginGame: onBeginGame,
                finishGame: onFinishGame,
                joinRoom: onJoinRoom,
                playerConnected: onPlayerConnected,
                playerDisconnected: onPlayerDisconnected,
            },
            methods: ['setName', 'findRoom', 'guess', 'exitRoom'],
            stateChanged: stateChanged
        });

        function stateChanged(state) {
            switch (state.newState) {
                case $.signalR.connectionState.connecting:
                    break;
                case $.signalR.connectionState.connected:
                    onConnected();
                    break;
                case $.signalR.connectionState.reconnecting:
                    //your code here
                    break;
                case $.signalR.connectionState.disconnected:
                    onDisconnected();
                    break;
            }
        }

        function onConnected() {
            console.log('connected');
        }

        function onDisconnected() {
            
        }

        function onError(msg) {
            
        }

        function onNewGuess(player, number, strikes, balls) {
            var guess = {
                player: player,
                number: number,
                strikes: strikes,
                balls: balls
            }

            _history.push(guess);
        }

        function onUpdateScores(scores) {
            _scores = scores;
        }

        function onBeginGame() {
            
        }

        function onFinishGame(winner, score, answerer, number) {
            
        }

        function onJoinRoom(roomId) {
            _status = 'playing';
        }

        function onPlayerConnected(player) {
            
        }

        function onPlayerDisconnected(player) {
            
        }


        var _status = 'init';
        var _playerName = 'Player';
        var _scores = [];
        var _history = [];

        vm.getStatus = function () {
            return _status;
        }

        vm.getPlayerName = function() {
            return _playerName;
        }

        var hashCache = { key: null, value: null };
        vm.getPictureHash = function() {
            if (hashCache.key !== _playerName) {
                hashCache.key = _playerName;
                hashCache.value = md5(_playerName);
            }

            return hashCache.value;
        }

        vm.getScores = function() {
            return _scores;
        }

        vm.submitName = function(name) {
            _playerName = name;

            hub.setName(name, 'https://www.gravatar.com/avatar/' + md5(_playerName) + '?d=retro');
            hub.findRoom();

            _status = 'finding';
        }

        vm.submitNumber = function (number) {
            if (_status !== 'playing')
                return;

            if (!/^\d{3}$/.test(number)) {
                vm.inputNumber = '';

                return false;
            }

            hub.guess(number);
            vm.inputNumber = '';
        }

        vm.getHistory = function () {
            return _history;
        }
    }
})();