// SignalR realtime: nhận BillStatusChanged từ hub và cập nhật UI trạng thái vận đơn không cần reload.
(function () {
    if (typeof signalR === "undefined") return;

    var indicator = document.getElementById("rt-indicator");
    function setIndicator(connected) {
        if (!indicator) return;
        indicator.classList.toggle("bg-success", connected);
        indicator.classList.toggle("bg-secondary", !connected);
        indicator.textContent = connected ? "● realtime" : "○ offline";
    }

    var connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/bill-status")
        .withAutomaticReconnect()
        .build();

    connection.onreconnecting(function () { setIndicator(false); });
    connection.onreconnected(function () { setIndicator(true); });
    connection.onclose(function () { setIndicator(false); });

    function fmtTime(unixSeconds) {
        if (!unixSeconds) return "";
        try { return new Date(unixSeconds * 1000).toLocaleString(); } catch (e) { return ""; }
    }

    connection.on("BillStatusChanged", function (u) {
        // u = { billCode, statusId, statusName, statusTime, receivedAt }
        var label = (u.statusName || ("Status " + u.statusId)) + " (#" + u.statusId + ")";

        // 1) Cập nhật mọi badge trạng thái gắn với bill này (danh sách + chi tiết).
        document.querySelectorAll('[data-bill-status][data-bill-code="' + u.billCode + '"]').forEach(function (el) {
            el.textContent = label;
            el.classList.add("bg-info");
            el.classList.remove("bg-secondary");
            el.style.transition = "background-color .3s";
            el.style.backgroundColor = "#ffe08a";
            setTimeout(function () { el.style.backgroundColor = ""; }, 1200);
        });

        // 2) Nếu đang mở đúng trang chi tiết bill này, chèn dòng vào bảng sự kiện.
        var current = document.body.getAttribute("data-current-bill");
        var tbody = document.getElementById("events-body");
        if (tbody && current === u.billCode) {
            var tr = document.createElement("tr");
            tr.className = "table-warning";
            tr.innerHTML =
                '<td><span class="badge bg-info">' + u.statusId + '</span></td>' +
                '<td>' + (u.statusName || "") + '</td>' +
                '<td>' + fmtTime(u.statusTime) + '</td>' +
                '<td><span class="badge bg-success">Webhook</span> live</td>';
            tbody.insertBefore(tr, tbody.firstChild);
        }

        // 3) Toast nhỏ góc màn hình.
        showToast(u.billCode + " → " + label);
    });

    function showToast(text) {
        var host = document.getElementById("toast-host");
        if (!host) {
            host = document.createElement("div");
            host.id = "toast-host";
            host.style.cssText = "position:fixed;bottom:1rem;right:1rem;z-index:1080;display:flex;flex-direction:column;gap:.5rem";
            document.body.appendChild(host);
        }
        var t = document.createElement("div");
        t.className = "alert alert-info shadow-sm mb-0 py-2 px-3";
        t.textContent = "🔔 " + text;
        host.appendChild(t);
        setTimeout(function () { t.remove(); }, 5000);
    }

    connection.start().then(function () { setIndicator(true); }).catch(function () { setIndicator(false); });
})();
