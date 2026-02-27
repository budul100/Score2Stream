$(document).ready(function () {
    $.ajaxSetup({ cache: false });

    const ellog = document.getElementById('log');

    function log(m) {
        ellog.innerHTML += m + '\n';
        ellog.scrollTop = ellog.scrollHeight;
    }

    $.getJSON("config.json", function (config) {
        const socketPort = config.socketPort || 9000;
        const updateInterval = config.updateInterval || 50;
        init(socketPort, updateInterval);
    }).fail(function () {
        console.warn("config.json not found, using default values.");
        init(9000, 50);
    });

    function init(socketPort, updateInterval) {
        let sock = null;

        const wsuri = (window.location.protocol === "file:")
            ? `ws://localhost:${socketPort}`
            : `ws://${window.location.hostname}:${socketPort}`;

        log(wsuri);

        if ("WebSocket" in window) {
            sock = new WebSocket(wsuri);
        } else {
            log("Browser does not support WebSocket!");
            return;
        }

        const vm = {
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
        };

        const viewModel = ko.mapping.fromJS(vm);

        viewModel.computedPeriod = ko.computed(function () {
            if (/^\d+$/.test(this.game.period()) && /^\d+$/.test(this.game.periods())) {
                const period = parseInt(this.game.period());
                const periods = parseInt(this.game.periods());
                if (period === 0) return "";
                else if (period <= periods) return `${period}/${periods}`;
                else return `E${Math.abs(period - periods)}`;
            } else {
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

        const interval = setInterval(function () {
            if (!sock || sock.readyState !== WebSocket.OPEN) {
                clearInterval(interval);
            }
        }, updateInterval);

        sock.onopen = function () {
            log("Connected to " + wsuri);
        };

        sock.onclose = function (e) {
            log("Connection closed (wasClean = " + e.wasClean +
                ", code = " + e.code + ", reason = '" + e.reason + "')");
            clearInterval(interval);
            sock = null;
        };

        sock.onmessage = function (e) {
            if (!e.data) return;
            try {
                ko.mapping.fromJS(JSON.parse(e.data), viewModel);
            } catch (err) {
                console.error("Invalid message:", err);
            }
        };

        sock.onerror = function (e) {
            log("WebSocket error: " + e);
            clearInterval(interval);
        };
    }
});