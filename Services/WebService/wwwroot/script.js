$(document).ready(

    function () {
        $.ajaxSetup({
            cache: false
        });

        var sock = null;
        var ellog = null;

        window.onload = function () {

            ellog = document.getElementById('log');

            var wsuri;

            if (window.location.protocol === "file:") {
                wsuri = "ws://localhost:9000";
            }
            else {
                wsuri = `ws://${window.location.hostname}:9000`;
            }

            log(wsuri);

            if ("WebSocket" in window) {
                sock = new WebSocket(wsuri);
            } else if ("MozWebSocket" in window) {
                sock = new MozWebSocket(wsuri);
            } else {
                log("Browser does not support WebSocket!");
                window.location = "http://autobahn.ws/unsupportedbrowser";
            }

            if (sock) {
                sock.onopen = function () {
                    log("Connected to " + wsuri);
                }

                sock.onclose = function (e) {
                    log("Connection closed (wasClean = " + e.wasClean + ", code = " + e.code + ", reason = '" + e.reason + "')");
                    sock = null;
                }

                sock.onmessage = function (e) {
                    //log("Got echo: " + e.data);
                    console.log(e.data);
                    ko.mapping.fromJS(JSON.parse(e.data), viewModel);
                }
            }
        };

        function broadcast() {
            var msg = document.getElementById('message').value;
            if (sock) {
                sock.send(msg);
                log("Sent: " + msg);
            } else {
                log("Not connected.");
            }
        };

        function log(m) {
            ellog.innerHTML += m + '\n';
            ellog.scrollTop = ellog.scrollHeight;
        };

        vm = {
            ticker: ko.observable(""),
            gameID: ko.observable(""),
            game_over: ko.observable(false),
            game: {
                clock: ko.observable("12:00"),
                shot_clock: ko.observable("24"),
                period: ko.observable("1"),
                periods: ko.observable("4"),
                possesion: ko.observable(""),
            },
            guest: {
                score: ko.observable("0"),
                fouls: ko.observable(""),
                name: ko.observable(""),
                imagePath: ko.observable(""),
                color: ko.observable("#6C6C6C")
            },
            home: {
                score: ko.observable("0"),
                fouls: ko.observable(""),
                name: ko.observable(""),
                imagePath: ko.observable(""),
                color: ko.observable("#6C6C6C")
            }
        }

        var viewModel = ko.mapping.fromJS(vm);

        viewModel.computedPeriod = ko.computed(function () {
            if (/^\d+$/.test(this.game.period()) && /^\d+$/.test(this.game.periods())) {
                var period = parseInt(this.game.period());
                var periods = parseInt(this.game.periods());

                if (period == 0) {
                    return "";
                }
                else if (period <= periods) {
                    return `${period}/${periods}`;
                }
                else {
                    return `E${Math.abs(period - periods)}`;
                }
            }
            else {
                return this.game.period();
            }
        }, viewModel);

        viewModel.guestFouls = ko.computed(function () {
            return parseInt(this.guest.fouls());
        }, viewModel);

        viewModel.homeFouls = ko.computed(function () {
            return parseInt(this.home.fouls());
        }, viewModel);

        ko.applyBindings(viewModel);

        var getUpdates = setInterval(function () {
            $.getJSON(
                "", {},
                function (model) {
                    ko.mapping.fromJS(model, viewModel);
                });
        }, 50);
    }
);