$(document).ready(function () {
    $.ajaxSetup({ cache: false });

    const ellog = document.getElementById('log');

    function log(m) {
        ellog.innerHTML += m + '\n';
        ellog.scrollTop = ellog.scrollHeight;
    }

    $.getJSON("config.json", function (config) {
        const socketPort = config.socketPort || 9000;
        init(socketPort);
    }).fail(function () {
        console.warn("config.json not found, using default values.");
        init(9000);
    });

    function init(socketPort) {
        let sock = null;

        const wsuri = (window.location.protocol === "file:")
            ? `ws://localhost:${socketPort}`
            : `ws://${window.location.hostname}:${socketPort}`;

        log(wsuri);

        if (!("WebSocket" in window)) {
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
                possession: ko.observable(""),
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
            const period = this.game.period();
            const periods = this.game.periods();

            if (/^\d+$/.test(period) && /^\d+$/.test(periods)) {
                const p = parseInt(period, 10);
                const ps = parseInt(periods, 10);

                if (p === 0) return "";
                if (p <= ps) return `${p}/${ps}`;
                return `E${Math.abs(p - ps)}`;
            }

            return period;
        }, viewModel);

        viewModel.guestFouls = ko.computed(function () {
            return parseInt(this.guest.fouls(), 10);
        }, viewModel);

        viewModel.homeFouls = ko.computed(function () {
            return parseInt(this.home.fouls(), 10);
        }, viewModel);

        ko.applyBindings(viewModel);

        function connect() {
            sock = new WebSocket(wsuri);

            sock.onopen = function () {
                log("Connected to " + wsuri);
            };

            sock.onclose = function (e) {
                log("Connection closed (wasClean = " + e.wasClean +
                    ", code = " + e.code + ", reason = '" + e.reason + "')");
                sock = null;
                setTimeout(connect, 3000);
            };

            sock.onerror = function (e) {
                log("WebSocket error: " + e);
            };

            sock.onmessage = function (e) {
                if (!e.data) return;

                try {
                    const data = JSON.parse(e.data);

                    if (data.ticker !== undefined) viewModel.ticker(data.ticker);
                    if (data.gameID !== undefined) viewModel.gameID(data.gameID);
                    if (data.game_over !== undefined) viewModel.game_over(data.game_over);

                    if (data.game) {
                        if (data.game.clock !== undefined) viewModel.game.clock(data.game.clock);
                        if (data.game.shot_clock !== undefined) viewModel.game.shot_clock(data.game.shot_clock);
                        if (data.game.period !== undefined) viewModel.game.period(data.game.period);
                        if (data.game.periods !== undefined) viewModel.game.periods(data.game.periods);
                        if (data.game.possession !== undefined) viewModel.game.possession(data.game.possession);
                    }

                    if (data.guest) {
                        if (data.guest.score !== undefined) viewModel.guest.score(data.guest.score);
                        if (data.guest.fouls !== undefined) viewModel.guest.fouls(data.guest.fouls);
                        if (data.guest.name !== undefined) viewModel.guest.name(data.guest.name);
                        if (data.guest.imagePath !== undefined) viewModel.guest.imagePath(data.guest.imagePath);
                        if (data.guest.color !== undefined) viewModel.guest.color(data.guest.color);
                    }

                    if (data.home) {
                        if (data.home.score !== undefined) viewModel.home.score(data.home.score);
                        if (data.home.fouls !== undefined) viewModel.home.fouls(data.home.fouls);
                        if (data.home.name !== undefined) viewModel.home.name(data.home.name);
                        if (data.home.imagePath !== undefined) viewModel.home.imagePath(data.home.imagePath);
                        if (data.home.color !== undefined) viewModel.home.color(data.home.color);
                    }

                } catch (err) {
                    console.error("Invalid message:", err);
                }
            };
        }

        connect();
    }
});
