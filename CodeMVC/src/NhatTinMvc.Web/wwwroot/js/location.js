// Helper dropdown địa chỉ phụ thuộc: tỉnh -> phường/xã (đơn vị hành chính "mới", bỏ cấp huyện).
// Gọi endpoint JSON của LocationController. Dùng chung cho form Tính phí và Tạo vận đơn.
window.NT = window.NT || {};

(function (NT) {
    function fill(select, items, placeholder, selected) {
        if (!select) return;
        select.innerHTML = "";
        var opt0 = document.createElement("option");
        opt0.value = "";
        opt0.textContent = placeholder || "-- chọn --";
        select.appendChild(opt0);
        (items || []).forEach(function (it) {
            var o = document.createElement("option");
            o.value = it.id;
            o.textContent = it.name || it.id;
            if (selected && String(selected) === String(it.id)) o.selected = true;
            select.appendChild(o);
        });
    }

    NT.loadProvinces = function (select, isNew, selected) {
        return fetch("/Location/Provinces?isNew=" + (isNew ? "true" : "false"))
            .then(function (r) { return r.json(); })
            .then(function (d) { fill(select, d.items, "-- Tỉnh/Thành --", selected); })
            .catch(function () { fill(select, [], "(lỗi tải tỉnh)"); });
    };

    NT.loadWards = function (select, provinceId, isNew, selected) {
        if (!provinceId) { fill(select, [], "-- Phường/Xã --"); return Promise.resolve(); }
        return fetch("/Location/Wards?provinceId=" + encodeURIComponent(provinceId) + "&isNew=" + (isNew ? "true" : "false"))
            .then(function (r) { return r.json(); })
            .then(function (d) { fill(select, d.items, "-- Phường/Xã --", selected); })
            .catch(function () { fill(select, [], "(lỗi tải phường/xã)"); });
    };

    // Nối 1 cặp select tỉnh->phường. selectedProvince/selectedWard để prefill khi form post lại.
    NT.bindProvinceWard = function (provinceSelect, wardSelect, opts) {
        opts = opts || {};
        var isNew = opts.isNew !== false; // mặc định dùng đơn vị mới
        NT.loadProvinces(provinceSelect, isNew, opts.selectedProvince).then(function () {
            if (opts.selectedProvince) NT.loadWards(wardSelect, opts.selectedProvince, isNew, opts.selectedWard);
        });
        provinceSelect.addEventListener("change", function () {
            NT.loadWards(wardSelect, provinceSelect.value, isNew, null);
        });
    };
})(window.NT);
