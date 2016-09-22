(function() {
    angular
        .module('app', ['SignalR'])
        .controller('gameController', gameController);

    gameController.$inject = ['$scope', 'Hub'];

    function gameController($scope, Hub) {
        var vm = this;

        var hub = new Hub('gameHub',
        {
            listeners: {
                error: onError,
                newGuess: onNewGuess,
                newMessage: onNewMessage,
                updateScores: onUpdateScores,
                beginGame: onBeginGame,
                finishGame: onFinishGame,
                joinRoom: onJoinRoom,
                playerConnected: onPlayerConnected,
                playerDisconnected: onPlayerDisconnected,
                setTimer: onSetTimer
            },
            methods: ['setName', 'findRoom', 'guess', 'exitRoom', 'message'],
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
            showSystemMessage(msg);
        }

        function onNewGuess(player, number, strikes, balls) {
            var guess = {
                player: player,
                number: number,
                strikes: strikes,
                balls: balls
            }

            _history.push(guess);
            $scope.$apply();

            var obj = $('#numbers');
            obj.scrollTop(obj[0].scrollHeight);
        }

        function onSetTimer(time) {
            showSystemMessage('게임 시작 까지 ' + time + '초');
        }

        function onNewMessage(player, msg) {
            showMessage(player, msg);
            notification('[' + player.name + '] ' + msg, { icon: player.imageUrl });
        }

        function onUpdateScores(scores) {
            _scores = scores;
            $scope.$apply();
        }

        function onBeginGame() {
            _isGameFinished = false;
            showSystemMessage('게임 시작');
            notification('게임 시작', { icon: '/gameicon.png' });
        }

        function onFinishGame(winner, score) {
            showSystemMessage(winner.name + '님이 ' + score + '점으로 승리했습니다');
            _isGameFinished = true;
            notification(winner.name + '님이 ' + score + '점으로 승리했습니다', { icon: winner.imageUrl });
            $scope.$apply();
        }

        function onJoinRoom(roomId) {
            _isGameFinished = false;
            _status = 'playing';
            $scope.$apply();
        }

        function onPlayerConnected(player) {
            showSystemMessage(player.name + '님이 접속했습니다');
        }

        function onPlayerDisconnected(player) {
            showSystemMessage(player.name + '님이 접속을 종료했습니다');
        }


        function showSystemMessage(msg) {
            _history.push({
                player: {
                    imageUrl: '/gameicon.png'
                },
                systemMessage: msg
            });
            $scope.$apply();
            var obj = $('#numbers');
            obj.scrollTop(obj[0].scrollHeight);
        }

        function showMessage(player, msg) {
            _history.push({
                player: player,
                message: msg
            });
            $scope.$apply();
            var obj = $('#numbers');
            obj.scrollTop(obj[0].scrollHeight);
        }


        var _status = 'init';
        var _playerName = 'Player';
        var _scores = [];
        var _history = [];
        var _isGameFinished = false;

        vm.getStatus = function () {
            return _status;
        }

        vm.getPlayerName = function() {
            return _playerName;
        }

        vm.isGameFinished = function() { return _isGameFinished; }

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
            if (_status === 'init') {
                return;
            }

            if (number.replace(/\s/g, '').length === 0) {
                return;
            }

            if (!/^\d{3}$/.test(number)) {
                hub.message(number);
                vm.inputNumber = '';
                return;
            }

            hub.guess(number);
            vm.inputNumber = '';
        }

        vm.getHistory = function () {
            return _history;
        }

        vm.exitRoom = function () {
            hub.exitRoom();

            _history = [];
            _status = 'init';
            _scores = [];
        }
        function notification(msg) {
            if (!("Notification" in window)) {
                console.log("This browser does not support desktop notification");
            } else if (Notification.permission === "granted") {
                var noti = new Notification(msg);
                noti.onclick = function () {
                    window.focus();
                    this.close();
                }
            } else if (Notification.permission !== 'denied') {
                Notification.requestPermission(function (permission) {
                    if (!('permission' in Notification)) {
                        Notification.permission = permission;
                    }
                });
            }
        }
    }
})();