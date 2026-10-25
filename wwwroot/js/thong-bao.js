document.querySelectorAll('[data-thong-bao]').forEach(cum => {
    const nut = cum.querySelector('.nut-chuong-thong-bao');
    const bang = cum.querySelector('.bang-chuong-thong-bao');
    const noiDung = cum.querySelector('.noi-dung-chuong-thong-bao');
    const nhanSoLuong = cum.querySelector('.so-chuong-thong-bao');
    let dangTai = false;

    const hienThiThoiGian = giaTri => {
        const ngay = new Date(giaTri);
        return Number.isNaN(ngay.getTime()) ? '' : new Intl.DateTimeFormat('vi-VN', {
            day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
        }).format(ngay);
    };

    const taoDong = don => {
        const muc = document.createElement('a');
        muc.className = 'muc-thong-bao';
        muc.href = don.url;

        const dauCham = document.createElement('span');
        dauCham.className = `dau-cham-thong-bao ${don.trangThai === 'ChoXacNhan' ? 'dang-cho' : ''}`;
        dauCham.setAttribute('aria-hidden', 'true');

        const vanBan = document.createElement('span');
        vanBan.className = 'van-ban-thong-bao';
        const tieuDe = document.createElement('strong');
        const maDon = `#VT-${String(don.maDon).padStart(5, '0')}`;
        tieuDe.textContent = cum.dataset.cheDo === 'quan-tri'
            ? `Đơn ${maDon} chờ xác nhận`
            : `Đơn ${maDon} · ${({ ChoXacNhan: 'Chờ xác nhận', DaXacNhan: 'Đã xác nhận', DaNhanVay: 'Đã nhận váy', DaHoanTra: 'Hoàn thành', KhongDenNhan: 'Không đến nhận · Mất cọc', DaHuy: 'Đã hủy' })[don.trangThai] || don.trangThai}`;
        const moTa = document.createElement('small');
        moTa.textContent = cum.dataset.cheDo === 'quan-tri'
            ? `${don.khachHang} · thuê ${hienThiThoiGian(don.ngayThue).slice(0, 10)} – ${hienThiThoiGian(don.ngayTra).slice(0, 10)}`
            : `Thời gian thuê: ${hienThiThoiGian(don.ngayThue).slice(0, 10)} – ${hienThiThoiGian(don.ngayTra).slice(0, 10)}`;
        const thoiGian = document.createElement('small');
        thoiGian.className = 'thoi-gian-thong-bao';
        thoiGian.textContent = hienThiThoiGian(don.ngayTao);
        vanBan.append(tieuDe, moTa, thoiGian);
        muc.append(dauCham, vanBan);
        return muc;
    };

    const taiThongBao = async () => {
        if (dangTai) return;
        dangTai = true;
        try {
            const phanHoi = await fetch(cum.dataset.url, {
                headers: { Accept: 'application/json' },
                credentials: 'same-origin'
            });
            if (!phanHoi.ok) throw new Error('Không tải được thông báo.');
            const duLieu = await phanHoi.json();
            nhanSoLuong.textContent = duLieu.soLuong > 99 ? '99+' : String(duLieu.soLuong);
            nhanSoLuong.hidden = duLieu.soLuong < 1;
            nut.setAttribute('aria-label', duLieu.soLuong > 0
                ? `Thông báo, ${duLieu.soLuong} đơn đang chờ xác nhận`
                : 'Mở thông báo');
            noiDung.replaceChildren();

            if (duLieu.danhSach.length === 0) {
                const rong = document.createElement('p');
                rong.className = 'trang-thai-tai-thong-bao';
                rong.textContent = cum.dataset.cheDo === 'quan-tri'
                    ? 'Hiện không có đơn thuê nào chờ xác nhận.'
                    : 'Bạn chưa có đơn thuê nào.';
                noiDung.append(rong);
            } else {
                duLieu.danhSach.forEach(don => noiDung.append(taoDong(don)));
            }
        } catch {
            const loi = document.createElement('p');
            loi.className = 'trang-thai-tai-thong-bao thong-bao-tai-that-bai';
            loi.textContent = 'Chưa tải được thông báo. Vui lòng thử lại.';
            noiDung.replaceChildren(loi);
        } finally {
            dangTai = false;
        }
    };

    nut.addEventListener('click', async () => {
        const dangMo = !bang.hidden;
        bang.hidden = dangMo;
        nut.setAttribute('aria-expanded', String(!dangMo));
        if (!dangMo) await taiThongBao();
    });

    document.addEventListener('click', suKien => {
        if (!cum.contains(suKien.target)) {
            bang.hidden = true;
            nut.setAttribute('aria-expanded', 'false');
        }
    });

    document.addEventListener('keydown', suKien => {
        if (suKien.key === 'Escape') {
            bang.hidden = true;
            nut.setAttribute('aria-expanded', 'false');
            nut.focus();
        }
    });

    taiThongBao();
    window.setInterval(taiThongBao, 60000);
});
